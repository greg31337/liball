namespace gltf.Tests.Internal.Evaluation;

[TestFixture]
public class SamplerEvaluatorTests
{
    [Test]
    public void EvaluateSampler_UsesRepeatWrappingAndAutomaticFilteringWhenAbsent()
    {
        SamplerEvaluation result = new SamplerEvaluator().EvaluateSampler(null);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsesDefaultSampler, Is.True);
            Assert.That(result.AutomaticMagnificationFilter, Is.True);
            Assert.That(result.AutomaticMinificationFilter, Is.True);
            Assert.That(result.WrapS, Is.EqualTo(GltfSamplerWrap.Repeat));
            Assert.That(result.WrapT, Is.EqualTo(GltfSamplerWrap.Repeat));
            Assert.That(result.MinificationUsesMipmaps, Is.False);
            Assert.That(result.Valid, Is.True);
        });
    }

    [Test]
    public void EvaluateSampler_IgnoresBackingValuesWhosePresenceFlagsAreFalse()
    {
        GltfSampler sampler = new GltfSampler();
        sampler.MagnificationFilter = GltfSamplerFilter.NearestMipmapNearest;
        sampler.MinificationFilter = (GltfSamplerFilter)1;
        sampler.WrapS = GltfSamplerWrap.ClampToEdge;
        sampler.WrapT = GltfSamplerWrap.MirroredRepeat;

        SamplerEvaluation result = new SamplerEvaluator().EvaluateSampler(sampler);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsesDefaultSampler, Is.False);
            Assert.That(result.AutomaticMagnificationFilter, Is.True);
            Assert.That(result.AutomaticMinificationFilter, Is.True);
            Assert.That(result.WrapS, Is.EqualTo(GltfSamplerWrap.Repeat));
            Assert.That(result.WrapT, Is.EqualTo(GltfSamplerWrap.Repeat));
            Assert.That(result.Valid, Is.True);
        });
    }

    [TestCase(GltfSamplerFilter.Nearest, false)]
    [TestCase(GltfSamplerFilter.Linear, false)]
    [TestCase(GltfSamplerFilter.NearestMipmapNearest, true)]
    [TestCase(GltfSamplerFilter.LinearMipmapNearest, true)]
    [TestCase(GltfSamplerFilter.NearestMipmapLinear, true)]
    [TestCase(GltfSamplerFilter.LinearMipmapLinear, true)]
    public void EvaluateSampler_AcceptsEveryLegalMinificationFilter(
        GltfSamplerFilter filter,
        bool usesMipmaps)
    {
        GltfSampler sampler = new GltfSampler();
        sampler.HasMinificationFilter = true;
        sampler.MinificationFilter = filter;

        SamplerEvaluation result = new SamplerEvaluator().EvaluateSampler(sampler);

        Assert.Multiple(() =>
        {
            Assert.That(result.AutomaticMinificationFilter, Is.False);
            Assert.That(result.MinificationFilter, Is.EqualTo(filter));
            Assert.That(result.MinificationFilterValid, Is.True);
            Assert.That(result.MinificationUsesMipmaps, Is.EqualTo(usesMipmaps));
            Assert.That(result.Valid, Is.True);
        });
    }

    [TestCase(GltfSamplerFilter.Nearest)]
    [TestCase(GltfSamplerFilter.Linear)]
    public void EvaluateSampler_AcceptsLegalMagnificationFilters(GltfSamplerFilter filter)
    {
        GltfSampler sampler = new GltfSampler();
        sampler.HasMagnificationFilter = true;
        sampler.MagnificationFilter = filter;

        SamplerEvaluation result = new SamplerEvaluator().EvaluateSampler(sampler);

        Assert.Multiple(() =>
        {
            Assert.That(result.AutomaticMagnificationFilter, Is.False);
            Assert.That(result.MagnificationFilter, Is.EqualTo(filter));
            Assert.That(result.MagnificationFilterValid, Is.True);
            Assert.That(result.Valid, Is.True);
        });
    }

    [TestCase(GltfSamplerWrap.ClampToEdge)]
    [TestCase(GltfSamplerWrap.MirroredRepeat)]
    [TestCase(GltfSamplerWrap.Repeat)]
    public void EvaluateSampler_AcceptsEveryLegalWrappingMode(GltfSamplerWrap wrap)
    {
        GltfSampler sampler = new GltfSampler();
        sampler.HasWrapS = true;
        sampler.WrapS = wrap;
        sampler.HasWrapT = true;
        sampler.WrapT = wrap;

        SamplerEvaluation result = new SamplerEvaluator().EvaluateSampler(sampler);

        Assert.Multiple(() =>
        {
            Assert.That(result.WrapS, Is.EqualTo(wrap));
            Assert.That(result.WrapT, Is.EqualTo(wrap));
            Assert.That(result.WrapSValid, Is.True);
            Assert.That(result.WrapTValid, Is.True);
            Assert.That(result.Valid, Is.True);
        });
    }

    [Test]
    public void EvaluateAndValidateSampler_ReportEveryIllegalExplicitValue()
    {
        GltfSampler sampler = new GltfSampler();
        sampler.HasMagnificationFilter = true;
        sampler.MagnificationFilter = GltfSamplerFilter.LinearMipmapLinear;
        sampler.HasMinificationFilter = true;
        sampler.MinificationFilter = (GltfSamplerFilter)100;
        sampler.HasWrapS = true;
        sampler.WrapS = (GltfSamplerWrap)200;
        sampler.HasWrapT = true;
        sampler.WrapT = (GltfSamplerWrap)300;
        GltfDocument document = new GltfDocument();
        document.Samplers.Add(sampler);
        SamplerEvaluator evaluator = new SamplerEvaluator();

        SamplerEvaluation result = evaluator.EvaluateSampler(sampler);
        GltfValidationReport report = evaluator.ValidateDocumentSamplers(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.MagnificationFilterValid, Is.False);
            Assert.That(result.MinificationFilterValid, Is.False);
            Assert.That(result.WrapSValid, Is.False);
            Assert.That(result.WrapTValid, Is.False);
            Assert.That(result.Valid, Is.False);
            Assert.That(report.Issues, Has.Count.EqualTo(4));
            Assert.That(report.Issues[0].Code, Is.EqualTo(GltfIssueCodes.ValueNotInList));
            Assert.That(report.Issues[0].JsonPointer, Is.EqualTo("/samplers/0/magFilter"));
            Assert.That(report.Issues[1].JsonPointer, Is.EqualTo("/samplers/0/minFilter"));
            Assert.That(report.Issues[2].JsonPointer, Is.EqualTo("/samplers/0/wrapS"));
            Assert.That(report.Issues[3].JsonPointer, Is.EqualTo("/samplers/0/wrapT"));
            Assert.That(report.Issues[0].Severity, Is.EqualTo(GltfValidationSeverity.Warning));
            Assert.That(report.IsValid, Is.True);
        });
    }

    [Test]
    public void ValidateDocumentSamplers_DoesNotValidateAbsentBackingValues()
    {
        GltfSampler sampler = new GltfSampler();
        sampler.MagnificationFilter = GltfSamplerFilter.LinearMipmapLinear;
        sampler.MinificationFilter = (GltfSamplerFilter)100;
        sampler.WrapS = (GltfSamplerWrap)200;
        sampler.WrapT = (GltfSamplerWrap)300;
        GltfDocument document = new GltfDocument();
        document.Samplers.Add(sampler);

        GltfValidationReport report = new SamplerEvaluator().ValidateDocumentSamplers(document);

        Assert.Multiple(() =>
        {
            Assert.That(report.Issues, Is.Empty);
            Assert.That(report.IsValid, Is.True);
        });
    }
}
