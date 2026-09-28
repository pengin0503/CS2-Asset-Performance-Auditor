using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.Tests;

public sealed class ScaffoldSmokeTests
{
    [Test]
    public void ProjectInfo_exposes_product_name_and_declares_no_harmony()
    {
        var projectInfo = typeof(ScaffoldSmokeTests).Assembly.GetType("CS2AssetPerformanceAuditor.Core.ProjectInfo", false);
        Assert.That(projectInfo, Is.Not.Null);
        Assert.That(projectInfo!.GetProperty("ProductName", BindingFlags.Public | BindingFlags.Static)?.GetValue(null), Is.EqualTo("CS2 Asset Performance Auditor"));
        Assert.That(projectInfo.GetProperty("UsesHarmony", BindingFlags.Public | BindingFlags.Static)?.GetValue(null), Is.EqualTo(false));
    }

    [Test]
    public void Mod_project_builds_UI_and_copies_bundle_before_DeployWIP()
    {
        var project = ReadRepoFile("src/CS2AssetPerformanceAuditor/CS2AssetPerformanceAuditor.csproj");
        Assert.That(project, Does.Contain("BuildAssetAuditorUI"));
        Assert.That(project, Does.Contain("BeforeTargets=\"DeployWIP\""));
        Assert.That(project, Does.Contain("npm.cmd run build"));
        Assert.That(project, Does.Contain("AssetAuditorUiFiles"));
        Assert.That(project, Does.Contain("$(OutDir)"));
    }

    [Test]
    public void CS2_systems_are_partial_for_entities_source_generation()
    {
        // The Unity Entities source generator (only present in the CS2 toolchain build, not in CI) fails with
        // EA0007 for any SystemBase-derived class that is not partial. Check every system class in the mod, not a
        // fixed list, so a newly added system is covered too.
        var systemDeclaration = new Regex(@"\bclass\s+(\w+)\s*:\s*(?:GameSystemBase|UISystemBase|SystemBase|ComponentSystemBase)\b");
        var sourceRoot = Path.Combine(FindRepoRoot(), "src", "CS2AssetPerformanceAuditor");
        var systems = Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .SelectMany(path => File.ReadAllLines(path).Select(line => (path, line)))
            .Where(item => systemDeclaration.IsMatch(item.line))
            .ToArray();

        Assert.That(systems.Select(item => systemDeclaration.Match(item.line).Groups[1].Value),
            Is.SupersetOf(new[] { "AssetAuditSystem", "AssetAuditUISystem" }));
        foreach (var (path, line) in systems)
            Assert.That(Regex.IsMatch(line, @"\bpartial\s+class\b"), Is.True, $"{Path.GetFileName(path)}: {line.Trim()} must be partial.");
    }

    [Test]
    public void Census_buffers_use_long_lived_allocator_for_frame_sliced_reduction()
    {
        var source = ReadRepoFile("src/CS2AssetPerformanceAuditor/GameIntegration/Census/CensusCaptureBuffers.cs");
        Assert.That(source, Does.Contain("Allocator.Persistent"));
        Assert.That(source, Does.Not.Contain("Allocator.TempJob"));
    }

    [Test]
    public void Phase_three_claims_only_implemented_metadata_collectors_are_supported()
    {
        var source = ReadRepoFile("src/CS2AssetPerformanceAuditor/GameIntegration/Capabilities/CapabilityProbe.cs");
        Assert.That(source, Does.Contain("CapabilityId.GeometryMetadata, CapabilityState.Supported"));
        Assert.That(source, Does.Contain("CapabilityId.SubmeshMetadata, CapabilityState.Supported"));
        Assert.That(source, Does.Contain("CapabilityId.SurfaceMetadata, CapabilityState.Supported"));
        Assert.That(source, Does.Contain("CapabilityId.TextureMetadata, CapabilityState.Supported"));
        Assert.That(source, Does.Contain("CapabilityId.RuntimeGpuResidency, CapabilityState.Unsupported"));
    }

    [Test]
    public void Normal_geometry_audit_never_materializes_Unity_meshes()
    {
        var source = ReadRepoFile("src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/GeometryAssetReader.cs");
        Assert.That(source, Does.Not.Contain("ObtainMeshes("));
        Assert.That(source, Does.Contain("GetVertexCount("));
        Assert.That(source, Does.Contain("GetSubMeshDesc("));
    }

    [Test]
    public void Surface_and_texture_audit_stays_metadata_first()
    {
        var surface = ReadRepoFile("src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/SurfaceAssetReader.cs");
        var texture = ReadRepoFile("src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/TextureAssetReader.cs");
        Assert.That(surface, Does.Contain("LoadProperties(false)"));
        Assert.That(surface, Does.Contain("UnloadProperties(false)"));
        Assert.That(surface, Does.Not.Contain("GetTemplateMaterial("));
        Assert.That(texture, Does.Not.Contain(".Load("));
        Assert.That(texture, Does.Not.Contain("rawData"));
    }

    [Test]
    public void Deep_inspection_releases_only_materials_acquired_by_the_mod_and_never_destroys_game_owned_objects()
    {
        var source = ReadRepoFile("src/CS2AssetPerformanceAuditor/GameIntegration/Rendering/DeepInspectionReader.cs");
        Assert.That(source, Does.Contain("var acquired = false"));
        Assert.That(source, Does.Contain("renderPrefab.ObtainMaterials(false)"));
        Assert.That(source, Does.Contain("acquired = true"));
        Assert.That(source, Does.Contain("finally"));
        Assert.That(source, Does.Contain("if (acquired)"));
        Assert.That(source, Does.Contain("renderPrefab.ReleaseMaterials()"));
        Assert.That(source, Does.Not.Contain("Destroy("));
        Assert.That(source, Does.Not.Contain("UnloadAsset("));
    }

    [Test]
    public void Census_catalog_publication_is_deferred_until_successful_finalization()
    {
        var catalog = ReadRepoFile("src/CS2AssetPerformanceAuditor/GameIntegration/Prefabs/PrefabCatalogAccess.cs");
        var coordinator = ReadRepoFile("src/CS2AssetPerformanceAuditor/GameIntegration/AssetAuditSystem.cs");
        Assert.That(catalog, Does.Contain("HasPendingPublication"));
        Assert.That(catalog, Does.Contain("StageWorkingCapture"));
        Assert.That(catalog, Does.Contain("CommitPendingCapture"));
        Assert.That(coordinator, Does.Contain("catalog.PendingRecords"));
        Assert.That(coordinator, Does.Contain("catalog.PendingRuntimeEntityKeys"));
        Assert.That(coordinator, Does.Contain("catalog.CommitPendingCapture()"));
    }

    [Test]
    public void UI_invalidates_prepared_export_when_scan_or_published_data_changes()
    {
        var source = ReadRepoFile("src/CS2AssetPerformanceAuditor/UI/AssetAuditUISystem.cs");
        Assert.That(source, Does.Contain("private void InvalidateExport()"));
        Assert.That(CountOccurrences(source, "InvalidateExport();"), Is.GreaterThanOrEqualTo(2));
    }

    private static string ReadRepoFile(string relativePath)
    {
        var path = Path.Combine(FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.That(File.Exists(path), Is.True, $"Expected repository file was not found: {relativePath}");
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CS2AssetPerformanceAuditor.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        Assert.Fail("Could not locate the repository root from the NUnit test directory.");
        return string.Empty;
    }

    private static int CountOccurrences(string text, string value)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentException("Search value is required.", nameof(value));
        var count = 0; var offset = 0;
        while ((offset = text.IndexOf(value, offset, StringComparison.Ordinal)) >= 0) { count++; offset += value.Length; }
        return count;
    }
}