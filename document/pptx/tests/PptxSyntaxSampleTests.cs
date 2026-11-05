using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxSyntaxSampleTests
{
    [Test]
    public void SyntaxSample_UsesAllowedConstructsAndBehavesPredictably()
    {
        PptxSyntaxSample sample = PptxSyntaxSample.CreateDefault();

        Assert.That(sample.Name, Is.EqualTo(PptxSyntaxSample.DefaultName));
        Assert.That(sample.NullableName, Is.Null);
        Assert.That(sample.NullableSample, Is.Null);
        Assert.That(sample.NullableItems, Is.Null);
        Assert.That(sample.Count(), Is.EqualTo(0));
        Assert.That(sample.State(), Is.EqualTo(PptxSyntaxSampleState.Empty));
        Assert.That(sample.DescribeCount(), Is.EqualTo("empty"));

        sample.Add("alpha");

        Assert.That(sample.Count(), Is.EqualTo(1));
        Assert.That(sample.State(), Is.EqualTo(PptxSyntaxSampleState.Ready));
        Assert.That(sample.DescribeCount(), Is.EqualTo("single"));
        Assert.That(sample.JoinWith(","), Is.EqualTo("alpha"));

        sample.Add("beta");

        Assert.That(sample.Count(), Is.EqualTo(2));
        Assert.That(sample.DescribeCount(), Is.EqualTo("multiple"));
        Assert.That(sample.JoinWith(","), Is.EqualTo("alpha,beta"));
    }
}
