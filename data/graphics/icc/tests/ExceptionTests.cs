using icc;

namespace icc.Tests;

public class ExceptionTests
{
    [Test]
    public void ExceptionConstructorsPreserveFailureMessages()
    {
        System.Exception[] exceptions =
        {
            new IccProfileFormatException("Invalid profile format."),
            new IccProfileValidationException("Profile validation failed."),
            new IccProfileWriteException("Profile write failed."),
            new IccTransformException("Transform failed."),
            new IccUnsupportedFeatureException("Feature is unsupported."),
            new IccEmbeddingException("Embedding failed.")
        };

        Assert.Multiple(() =>
        {
            Assert.That(exceptions[0].Message, Is.EqualTo("Invalid profile format."));
            Assert.That(exceptions[1].Message, Is.EqualTo("Profile validation failed."));
            Assert.That(exceptions[2].Message, Is.EqualTo("Profile write failed."));
            Assert.That(exceptions[3].Message, Is.EqualTo("Transform failed."));
            Assert.That(exceptions[4].Message, Is.EqualTo("Feature is unsupported."));
            Assert.That(exceptions[5].Message, Is.EqualTo("Embedding failed."));
        });
    }

    [Test]
    public void ExceptionsRetainTheirOperationCategoriesWhenThrown()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() => throw new IccProfileFormatException("format"));
            Assert.Throws<IccProfileValidationException>(() => throw new IccProfileValidationException("validation"));
            Assert.Throws<IccProfileWriteException>(() => throw new IccProfileWriteException("write"));
            Assert.Throws<IccTransformException>(() => throw new IccTransformException("transform"));
            Assert.Throws<IccUnsupportedFeatureException>(() => throw new IccUnsupportedFeatureException("unsupported"));
            Assert.Throws<IccEmbeddingException>(() => throw new IccEmbeddingException("embedding"));
        });
    }
}
