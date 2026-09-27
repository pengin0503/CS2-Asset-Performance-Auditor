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
}
