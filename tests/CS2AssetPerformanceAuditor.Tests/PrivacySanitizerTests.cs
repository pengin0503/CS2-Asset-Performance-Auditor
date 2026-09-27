using CS2AssetPerformanceAuditor.Export;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.Tests
{
    public sealed class PrivacySanitizerTests
    {
        [Test]
        public void RemovesWindowsAndUnixAbsolutePathsAndKnownMachineIdentifiers()
        {
            var sanitizer = new PrivacySanitizer("alice", "HOST-7");
            var sanitized = sanitizer.SanitizeText(
                @"from C:\Users\alice\Mods\City asset; /home/alice/.local/share/CS2; \\HOST-7\Users\alice\Mods\private.mod; machine HOST-7");

            Assert.That(sanitized, Does.Not.Contain("C:\\Users\\"));
            Assert.That(sanitized, Does.Not.Contain("/home/"));
            Assert.That(sanitized, Does.Not.Contain(@"\Users\"));
            Assert.That(sanitized, Does.Not.Contain("alice"));
            Assert.That(sanitized, Does.Not.Contain("HOST-7"));
            Assert.That(sanitized, Does.Contain("redacted"));
        }

        [Test]
        public void LeavesStableRelativeAssetIdentifiersReadable()
        {
            var sanitizer = new PrivacySanitizer("alice", "HOST-7");

            Assert.That(sanitizer.SanitizeText("assets/Buildings/SmallHouse.prefab"),
                Is.EqualTo("assets/Buildings/SmallHouse.prefab"));
        }
    }
}
