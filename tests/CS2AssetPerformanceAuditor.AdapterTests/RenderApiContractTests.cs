using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.AdapterTests
{
    [TestFixture]
    public sealed class RenderApiContractTests
    {
        private MetadataLoadContext? _metadata;
        private string[] _assemblyPaths = Array.Empty<string>();

        [SetUp]
        public void SetUp()
        {
            var managedPath = Environment.GetEnvironmentVariable("CSII_MANAGEDPATH");
            if (string.IsNullOrWhiteSpace(managedPath)) managedPath = Environment.GetEnvironmentVariable("CSII_TOOLPATH");
            if (string.IsNullOrWhiteSpace(managedPath) || !Directory.Exists(managedPath)) Assert.Fail("Set CSII_MANAGEDPATH or CSII_TOOLPATH to the local CS2 managed reference directory.");
            _assemblyPaths = Directory.GetFiles(managedPath!, "*.dll");
            var frameworkPath = Environment.GetEnvironmentVariable("CS2APA_NET48_REFERENCE_PATH");
            if (!string.IsNullOrWhiteSpace(frameworkPath) && Directory.Exists(frameworkPath))
                _assemblyPaths = _assemblyPaths.Concat(Directory.GetFiles(frameworkPath, "*.dll")).ToArray();
            else
            {
                var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location);
                if (!string.IsNullOrWhiteSpace(runtimePath) && Directory.Exists(runtimePath)) _assemblyPaths = _assemblyPaths.Concat(Directory.GetFiles(runtimePath, "*.dll")).ToArray();
            }
            var platformPaths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Where(path => !Path.GetFileName(path).Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));
            _metadata = new MetadataLoadContext(new PathAssemblyResolver(_assemblyPaths.Concat(platformPaths)), "mscorlib");
        }

        [TearDown]
        public void TearDown() { _metadata?.Dispose(); _metadata = null; }

        [Test]
        public void Object_geometry_and_lod_public_relations_match_phase_two_resolver()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var objectGeometry = GameType("Game.Prefabs.ObjectGeometryPrefab");
            var objectMeshInfo = GameType("Game.Prefabs.ObjectMeshInfo");
            var lodProperties = GameType("Game.Prefabs.LodProperties");
            Assert.That(objectGeometry.GetField("m_Meshes", flags)?.FieldType.GetElementType()?.FullName, Is.EqualTo("Game.Prefabs.ObjectMeshInfo"));
            Assert.That(objectMeshInfo.GetField("m_Mesh", flags)?.FieldType.FullName, Is.EqualTo("Game.Prefabs.RenderPrefabBase"));
            Assert.That(lodProperties.GetField("m_LodMeshes", flags)?.FieldType.GetElementType()?.FullName, Is.EqualTo("Game.Prefabs.RenderPrefab"));
        }

        private Type GameType(string fullName)
        {
            foreach (var path in _assemblyPaths)
            {
                try
                {
                    var type = _metadata!.LoadFromAssemblyPath(path).GetType(fullName, throwOnError: false);
                    if (type != null) return type;
                }
                catch (BadImageFormatException) { }
                catch (InvalidOperationException) { }
                catch (FileNotFoundException) { }
                catch (TypeLoadException) { }
            }
            throw new InvalidOperationException($"Game API type {fullName} was not found in the supplied managed references.");
        }
    }
}
