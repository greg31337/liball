namespace gltf.Tests.Api.Static;

[TestFixture]
public class GltfSemanticsTests
{
    [Test]
    public void AttributeSemantics_MatchGltfTokens()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GltfSemantics.Position, Is.EqualTo("POSITION"));
            Assert.That(GltfSemantics.Normal, Is.EqualTo("NORMAL"));
            Assert.That(GltfSemantics.Tangent, Is.EqualTo("TANGENT"));
            Assert.That(GltfSemantics.TextureCoordinatePrefix, Is.EqualTo("TEXCOORD_"));
            Assert.That(GltfSemantics.ColorPrefix, Is.EqualTo("COLOR_"));
            Assert.That(GltfSemantics.JointsPrefix, Is.EqualTo("JOINTS_"));
            Assert.That(GltfSemantics.WeightsPrefix, Is.EqualTo("WEIGHTS_"));
            Assert.That(GltfSemantics.CustomAttributePrefix, Is.EqualTo("_"));
        });
    }

    [Test]
    public void AnimationPaths_MatchGltfTokens()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GltfSemantics.TranslationPath, Is.EqualTo("translation"));
            Assert.That(GltfSemantics.RotationPath, Is.EqualTo("rotation"));
            Assert.That(GltfSemantics.ScalePath, Is.EqualTo("scale"));
            Assert.That(GltfSemantics.WeightsPath, Is.EqualTo("weights"));
            Assert.That(GltfSemantics.PointerPath, Is.EqualTo("pointer"));
        });
    }
}
