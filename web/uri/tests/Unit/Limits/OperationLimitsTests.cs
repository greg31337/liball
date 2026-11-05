namespace uri.Tests;

public class OperationLimitsTests
{
    [Test]
    public void ConstructorProvidesBoundedPositiveDefaults()
    {
        OperationLimits limits = new OperationLimits();

        Assert.Multiple(() =>
        {
            Assert.That(limits.MaxInputCharacters, Is.EqualTo(1048576));
            Assert.That(limits.MaxComponentCharacters, Is.EqualTo(262144));
            Assert.That(limits.MaxPathSegments, Is.EqualTo(16384));
            Assert.That(limits.MaxQueryItems, Is.EqualTo(16384));
            Assert.That(limits.MaxTemplateExpressions, Is.EqualTo(4096));
            Assert.That(limits.MaxTemplateVariables, Is.EqualTo(16384));
            Assert.That(limits.MaxTemplateCompositeMembers, Is.EqualTo(65536));
            Assert.That(limits.MaxWebLinks, Is.EqualTo(4096));
            Assert.That(limits.MaxWebLinkParameters, Is.EqualTo(16384));
            Assert.That(limits.MaxNestingDepth, Is.EqualTo(64));
            Assert.That(limits.MaxOutputCharacters, Is.EqualTo(4194304));
        });
    }

    [Test]
    public void FieldsCanBeConfiguredForAnIndividualOperation()
    {
        OperationLimits limits = new OperationLimits();

        limits.MaxInputCharacters = 10;
        limits.MaxComponentCharacters = 9;
        limits.MaxPathSegments = 8;
        limits.MaxQueryItems = 7;
        limits.MaxTemplateExpressions = 6;
        limits.MaxTemplateVariables = 5;
        limits.MaxTemplateCompositeMembers = 4;
        limits.MaxWebLinks = 3;
        limits.MaxWebLinkParameters = 2;
        limits.MaxNestingDepth = 1;
        limits.MaxOutputCharacters = 0;

        Assert.Multiple(() =>
        {
            Assert.That(limits.MaxInputCharacters, Is.EqualTo(10));
            Assert.That(limits.MaxComponentCharacters, Is.EqualTo(9));
            Assert.That(limits.MaxPathSegments, Is.EqualTo(8));
            Assert.That(limits.MaxQueryItems, Is.EqualTo(7));
            Assert.That(limits.MaxTemplateExpressions, Is.EqualTo(6));
            Assert.That(limits.MaxTemplateVariables, Is.EqualTo(5));
            Assert.That(limits.MaxTemplateCompositeMembers, Is.EqualTo(4));
            Assert.That(limits.MaxWebLinks, Is.EqualTo(3));
            Assert.That(limits.MaxWebLinkParameters, Is.EqualTo(2));
            Assert.That(limits.MaxNestingDepth, Is.EqualTo(1));
            Assert.That(limits.MaxOutputCharacters, Is.EqualTo(0));
        });
    }

    [Test]
    public void OperationOptionsReceiveIndependentDefaultLimits()
    {
        UriParseOptions uriParse = new UriParseOptions();
        UriValidationOptions uriValidation = new UriValidationOptions();
        UriNormalizationOptions uriNormalization = new UriNormalizationOptions();
        UriResolutionOptions uriResolution = new UriResolutionOptions();
        UriRelativizationOptions uriRelativization = new UriRelativizationOptions();
        UriComparisonOptions uriComparison = new UriComparisonOptions();
        PercentCodecOptions percentCodec = new PercentCodecOptions();
        UriQueryOptions uriQuery = new UriQueryOptions();
        IriParseOptions iriParse = new IriParseOptions();
        IriConversionOptions iriConversion = new IriConversionOptions();
        UriTemplateParseOptions templateParse = new UriTemplateParseOptions();
        UriTemplateExpansionOptions templateExpansion = new UriTemplateExpansionOptions();
        UrnParseOptions urnParse = new UrnParseOptions();
        UrnLocatorMergeOptions urnLocatorMerge = new UrnLocatorMergeOptions();
        WebLinkParseOptions webLinkParse = new WebLinkParseOptions();

        uriParse.Limits.MaxInputCharacters = 1;

        Assert.Multiple(() =>
        {
            Assert.That(uriParse.Limits.MaxInputCharacters, Is.EqualTo(1));
            Assert.That(uriValidation.Limits.MaxInputCharacters, Is.EqualTo(1048576));
            Assert.That(uriNormalization.Limits, Is.Not.SameAs(uriResolution.Limits));
            Assert.That(uriRelativization.Limits, Is.Not.SameAs(uriComparison.Limits));
            Assert.That(percentCodec.Limits, Is.Not.SameAs(uriQuery.Limits));
            Assert.That(iriParse.Limits, Is.Not.SameAs(iriConversion.Limits));
            Assert.That(templateParse.Limits, Is.Not.SameAs(templateExpansion.Limits));
            Assert.That(urnParse.Limits, Is.Not.SameAs(urnLocatorMerge.Limits));
            Assert.That(webLinkParse.Limits.MaxOutputCharacters, Is.EqualTo(4194304));
        });
    }
}
