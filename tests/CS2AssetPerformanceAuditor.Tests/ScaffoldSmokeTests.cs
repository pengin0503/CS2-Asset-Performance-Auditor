using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.Tests;

public sealed class ScaffoldSmokeTests
{
    [Test]
    public void ProjectInfo_exposes_product_name_and_declares_no_harmony()
    {
        var projectInfo = typeof(ScaffoldSmokeTests).Assembly.GetType(
            "CS2AssetPerformanceAuditor.Core.ProjectInfo",
            throwOnError: false);

        Assert.That(projectInfo, Is.Not.Null, "Core ProjectInfo type is missing.");
        Assert.That(projectInfo!.GetProperty("ProductName", BindingFlags.Public | BindingFlags.Static)?.GetValue(null),
            Is.EqualTo("CS2 Asset Performance Auditor"));
        Assert.That(projectInfo.GetProperty("UsesHarmony", BindingFlags.Public | BindingFlags.Static)?.GetValue(null),
            Is.EqualTo(false));
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
    public void Census_buffers_use_long_lived_allocator_for_frame_sliced_reduction()
    {
        var source = ReadRepoFile("src/CS2AssetPerformanceAuditor/GameIntegration/Census/CensusCaptureBuffers.cs");

        Assert.That(source, Does.Contain("Allocator.Persistent"));
        Assert.That(source, Does.Not.Contain("Allocator.TempJob"));
    }

    [Test]
    public void Phase_one_does_not_claim_unimplemented_metadata_collectors_are_supported()
    {
        var source = ReadRepoFile("src/CS2AssetPerformanceAuditor/GameIntegration/Capabilities/CapabilityProbe.cs");

        Assert.That(source, Does.Not.Contain("CapabilityId.GeometryMetadata, CapabilityState.Supported"));
        Assert.That(source, Does.Not.Contain("CapabilityId.SubmeshMetadata, CapabilityState.Supported"));
        Assert.That(source, Does.Not.Contain("CapabilityId.SurfaceMetadata, CapabilityState.Supported"));
        Assert.That(source, Does.Not.Contain("CapabilityId.TextureMetadata, CapabilityState.Supported"));
        Assert.That(source, Does.Contain("collector_not_implemented_phase_1"));
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
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CS2AssetPerformanceAuditor.sln")))
            {
                var path = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
                Assert.That(File.Exists(path), Is.True, $"Expected repository file was not found: {relativePath}");
                return File.ReadAllText(path);
            }
            directory = directory.Parent;
        }

        Assert.Fail("Could not locate the repository root from the NUnit test directory.");
        return string.Empty;
    }

    private static int CountOccurrences(string text, string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Search value is required.", nameof(value));

        var count = 0;
        var offset = 0;
        while ((offset = text.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
    }
}
