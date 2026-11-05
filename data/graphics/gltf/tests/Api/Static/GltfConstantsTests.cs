namespace gltf.Tests.Api.Static;

[TestFixture]
public class GltfConstantsTests
{
    [Test]
    public void VersionAndGlbConstants_MatchGltfSpecification()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GltfConstants.CoreVersion, Is.EqualTo("2.0"));
            Assert.That(GltfConstants.SpecificationVersion, Is.EqualTo("2.0.1"));
            Assert.That(GltfConstants.GlbMagic, Is.EqualTo(0x46546C67U));
            Assert.That(GltfConstants.GlbVersion, Is.EqualTo(2));
            Assert.That(GltfConstants.GlbJsonChunkType, Is.EqualTo(0x4E4F534AU));
            Assert.That(GltfConstants.GlbBinChunkType, Is.EqualTo(0x004E4942U));
            Assert.That(GltfConstants.GlbHeaderByteLength, Is.EqualTo(12));
            Assert.That(GltfConstants.GlbChunkHeaderByteLength, Is.EqualTo(8));
            Assert.That(GltfConstants.MinimumGlbByteLength, Is.EqualTo(20));
            Assert.That(GltfConstants.AlignmentByteLength, Is.EqualTo(4));
            Assert.That(GltfConstants.MaximumPaddingByteLength, Is.EqualTo(3));
            Assert.That(GltfConstants.GlbJsonPaddingByte, Is.EqualTo(0x20));
            Assert.That(GltfConstants.GlbBinPaddingByte, Is.Zero);
        });
    }

    [Test]
    public void SentinelAndLayoutConstants_AreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GltfConstants.AbsentIndex, Is.EqualTo(-1));
            Assert.That(GltfConstants.DefaultByteOffset, Is.Zero);
            Assert.That(GltfConstants.MinimumByteStride, Is.EqualTo(4));
            Assert.That(GltfConstants.MaximumByteStride, Is.EqualTo(252));
        });
    }

    [Test]
    public void DefaultLimits_ArePositiveAndInternallyOrdered()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GltfConstants.DefaultMaximumJsonDepth, Is.EqualTo(128));
            Assert.That(GltfConstants.DefaultMaximumJsonTokenCount, Is.EqualTo(10000000));
            Assert.That(GltfConstants.DefaultMaximumJsonStringByteLength, Is.EqualTo(16777216));
            Assert.That(GltfConstants.DefaultMaximumJsonNumberByteLength, Is.EqualTo(1024));
            Assert.That(GltfConstants.DefaultMaximumCollectionCount, Is.EqualTo(1000000));
            Assert.That(GltfConstants.DefaultMaximumInputByteLength, Is.EqualTo(536870912));
            Assert.That(GltfConstants.DefaultMaximumJsonByteLength, Is.EqualTo(268435456));
            Assert.That(GltfConstants.DefaultMaximumDataUriByteLength, Is.EqualTo(268435456));
            Assert.That(GltfConstants.DefaultMaximumResourceByteLength, Is.EqualTo(536870912));
            Assert.That(GltfConstants.DefaultMaximumTotalResourceByteLength, Is.EqualTo(1073741824));
            Assert.That(GltfConstants.DefaultMaximumIssueCount, Is.EqualTo(10000));
            Assert.That(GltfConstants.DefaultMaximumJsonByteLength, Is.LessThanOrEqualTo(GltfConstants.DefaultMaximumInputByteLength));
            Assert.That(GltfConstants.DefaultMaximumResourceByteLength, Is.LessThanOrEqualTo(GltfConstants.DefaultMaximumTotalResourceByteLength));
        });
    }
}
