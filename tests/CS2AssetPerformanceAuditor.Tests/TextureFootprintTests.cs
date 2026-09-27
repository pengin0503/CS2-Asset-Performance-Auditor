using System;
using CS2AssetPerformanceAuditor.Core.Observations;
using CS2AssetPerformanceAuditor.Core.Rendering;
using NUnit.Framework;

namespace CS2AssetPerformanceAuditor.Tests
{
    [TestFixture]
    public sealed class TextureFootprintTests
    {
        private static readonly DateTimeOffset CapturedAt = new DateTimeOffset(2026, 9, 27, 7, 10, 0, TimeSpan.Zero);

        [TestCase("R8G8B8A8_UNorm", 4, 4, 1, 1, 64L)]
        [TestCase("BC1_RGB_UNorm", 4, 4, 1, 1, 8L)]
        [TestCase("BC7_UNorm", 4, 4, 1, 1, 16L)]
        public void Representative_formats_have_documented_logical_payload(string format, int width, int height, int depth, int mips, long expected)
        {
            var result = TextureFootprintEstimator.Estimate(width, height, depth, mips, format, CapturedAt);
            Assert.That(result.Availability, Is.EqualTo(Availability.Available));
            Assert.That(result.Value, Is.EqualTo(expected));
            Assert.That(result.Origin, Is.EqualTo(ObservationOrigin.Estimated));
        }

        [Test]
        public void Block_compression_rounds_up_partial_blocks()
        {
            var result = TextureFootprintEstimator.Estimate(5, 5, 1, 1, "BC1_RGB_UNorm", CapturedAt);
            Assert.That(result.Value, Is.EqualTo(32L));
        }

        [Test]
        public void Mip_chain_sums_each_level_without_claiming_gpu_residency()
        {
            var result = TextureFootprintEstimator.Estimate(4, 4, 1, 3, "R8G8B8A8_UNorm", CapturedAt);
            Assert.That(result.Value, Is.EqualTo(84L));
            Assert.That(TextureFootprintEstimator.MetricName, Does.Contain("logical"));
            Assert.That(TextureFootprintEstimator.MetricName, Does.Not.Contain("VRAM").IgnoreCase);
        }

        [Test]
        public void Unsupported_format_is_not_reported_as_zero()
        {
            var result = TextureFootprintEstimator.Estimate(1024, 1024, 1, 1, "Unknown_Custom_Format", CapturedAt);
            Assert.That(result.Availability, Is.EqualTo(Availability.Unsupported));
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void Shared_texture_keys_are_deduplicated_for_unique_payload()
        {
            var observations = new[]
            {
                TextureObservation.Available("texture:shared", 4, 4, 1, "R8G8B8A8_UNorm", "Tex2D", 1, "Bilinear", "Repeat", 1, 64, CapturedAt),
                TextureObservation.Available("texture:shared", 4, 4, 1, "R8G8B8A8_UNorm", "Tex2D", 1, "Bilinear", "Repeat", 1, 64, CapturedAt),
            };
            var unique = TextureObservation.Deduplicate(observations);
            Assert.That(unique.Count, Is.EqualTo(1));
            Assert.That(unique[0].EstimatedLogicalPayload.Value, Is.EqualTo(64));
        }
    }
}
