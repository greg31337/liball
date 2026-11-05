namespace uri.Tests;

public class PublicApiCoverageGapTests
{
    [Test]
    public void AbsoluteIriParserReportsNullInputWithoutThrowing()
    {
        IIriParser parser = new IriParser();

        IriParseResult result = parser.TryParseAbsoluteIri(
            null!,
            new IriParseOptions());

        AssertDiagnosticFailure(
            result.Success,
            result.Diagnostic,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
    }

    [Test]
    public void PercentCodecReportsEveryPublicPreconditionAndLimitBranch()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions missingLimits = new PercentCodecOptions();
        missingLimits.Limits = null!;
        PercentCodecOptions negativeEncodeSet = new PercentCodecOptions();
        negativeEncodeSet.EncodeSet = (PercentEncodeSet)(-1);
        PercentCodecOptions negativeDecodeMode = new PercentCodecOptions();
        negativeDecodeMode.DecodeMode = (PercentDecodeMode)(-1);
        PercentCodecOptions encodeInputLimit = new PercentCodecOptions();
        encodeInputLimit.Limits.MaxInputCharacters = 0;
        PercentCodecOptions encodeOutputLimit = new PercentCodecOptions();
        encodeOutputLimit.Limits.MaxOutputCharacters = 0;
        PercentCodecOptions decodeInputLimit = new PercentCodecOptions();
        decodeInputLimit.Limits.MaxInputCharacters = 0;

        UriDiagnostic? validationOptions = codec.ValidatePercentEncoding(
            "text",
            missingLimits);
        PercentEncodingException nullText = Assert.Throws<
            PercentEncodingException>(() => codec.EncodeText(
                null!,
                new PercentCodecOptions()))!;
        PercentEncodingException byteOptions = Assert.Throws<
            PercentEncodingException>(() => codec.EncodeBytes(
                new byte[] { 1 },
                missingLimits))!;
        PercentEncodingException byteOutput = Assert.Throws<
            PercentEncodingException>(() => codec.EncodeBytes(
                new byte[] { 1 },
                encodeOutputLimit))!;
        PercentEncodingException spanOptions = Assert.Throws<
            PercentEncodingException>(() => codec.EncodeSpan(
                "text".AsSpan(),
                negativeEncodeSet))!;
        PercentEncodingException textInput = Assert.Throws<
            PercentEncodingException>(() => codec.EncodeText(
                "text",
                encodeInputLimit))!;
        PercentDecodeResult byteDecodeOptions = codec.DecodeBytes(
            "text",
            missingLimits);
        PercentDecodeResult byteDecodeInput = codec.DecodeBytes(
            "text",
            decodeInputLimit);
        PercentDecodeResult nullDecodeText = codec.DecodeText(
            null!,
            new PercentCodecOptions());
        PercentDecodeResult decodeMode = codec.DecodeText(
            "text",
            negativeDecodeMode);
        PercentEncodingException nullNormalization = Assert.Throws<
            PercentEncodingException>(() => codec.NormalizePercentEncoding(
                null!,
                new PercentCodecOptions()))!;
        PercentEncodingException normalizationInput = Assert.Throws<
            PercentEncodingException>(() => codec.NormalizePercentEncoding(
                "text",
                decodeInputLimit))!;

        AssertInvalidInput(validationOptions);
        AssertInvalidInput(nullText.Diagnostic);
        AssertInvalidInput(byteOptions.Diagnostic);
        AssertLimit(byteOutput.Diagnostic, UriComponent.Output);
        AssertInvalidInput(spanOptions.Diagnostic);
        AssertLimit(textInput.Diagnostic, UriComponent.Input);
        AssertDiagnosticFailure(
            byteDecodeOptions.Success,
            byteDecodeOptions.Diagnostic,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertDiagnosticFailure(
            byteDecodeInput.Success,
            byteDecodeInput.Diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Input);
        AssertDiagnosticFailure(
            nullDecodeText.Success,
            nullDecodeText.Diagnostic,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertDiagnosticFailure(
            decodeMode.Success,
            decodeMode.Diagnostic,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertInvalidInput(nullNormalization.Diagnostic);
        AssertLimit(normalizationInput.Diagnostic, UriComponent.Input);
    }

    [Test]
    public void QueryTransformationsReportEachInvalidInputStage()
    {
        IUriQueryCodec codec = new UriQueryCodec();
        UriQueryOptions options = new UriQueryOptions();
        UriQueryParameter parameter = new UriQueryParameter(
            "name",
            true,
            "value");
        UriQueryParameter invalidParameter = new UriQueryParameter(
            "name",
            true,
            "value");
        invalidParameter.Name = null!;
        UriQueryOptions resultLimit = new UriQueryOptions();
        resultLimit.Limits.MaxQueryItems = 1;

        UriQueryResult addSource = codec.AddQueryParameter(
            null!,
            parameter,
            options);
        UriQueryResult addParameter = codec.AddQueryParameter(
            new UriQueryParameter[0],
            invalidParameter,
            options);
        UriQueryResult replaceSource = codec.ReplaceQueryParameters(
            null!,
            "name",
            new UriQueryParameter[0],
            options);
        UriQueryResult replaceName = codec.ReplaceQueryParameters(
            new UriQueryParameter[0],
            null!,
            new UriQueryParameter[0],
            options);
        UriQueryResult replaceCount = codec.ReplaceQueryParameters(
            new UriQueryParameter[] { parameter },
            "other",
            new UriQueryParameter[] { parameter },
            resultLimit);
        UriQueryResult removeSource = codec.RemoveQueryParameters(
            null!,
            "name",
            options);
        UriQueryResult removeName = codec.RemoveQueryParameters(
            new UriQueryParameter[0],
            null!,
            options);
        UriQueryResult deduplicateSource = codec.DeduplicateQueryParameters(
            null!,
            options);

        AssertQueryFailure(addSource, UriDiagnosticCode.InvalidInput);
        AssertQueryFailure(addParameter, UriDiagnosticCode.InvalidInput);
        AssertQueryFailure(replaceSource, UriDiagnosticCode.InvalidInput);
        AssertQueryFailure(replaceName, UriDiagnosticCode.InvalidInput);
        AssertQueryFailure(replaceCount, UriDiagnosticCode.LimitExceeded);
        AssertQueryFailure(removeSource, UriDiagnosticCode.InvalidInput);
        AssertQueryFailure(removeName, UriDiagnosticCode.InvalidInput);
        AssertQueryFailure(
            deduplicateSource,
            UriDiagnosticCode.InvalidInput);
    }

    [Test]
    public void WebLinkParserRejectsMissingAndUndefinedModesOnEveryShape()
    {
        IWebLinkParser parser = new WebLinkParser();
        WebLinkParseOptions invalidMode = new WebLinkParseOptions();
        invalidMode.Mode = (WebLinkParseMode)99;

        WebLinkParseResult missingFieldsOptions =
            parser.TryParseLinkFieldValues(
                new string[] { "<target>" },
                null!);
        WebLinkParseResult invalidFieldsMode =
            parser.TryParseLinkFieldValues(
                new string[] { "<target>" },
                invalidMode);
        WebLinkParseResult missingSpanOptions =
            parser.TryParseLinkFieldValueSpan(
                "<target>".AsSpan(),
                null!);
        WebLinkParseResult invalidSpanMode =
            parser.TryParseLinkFieldValueSpan(
                "<target>".AsSpan(),
                invalidMode);

        AssertWebLinkFailure(missingFieldsOptions);
        AssertWebLinkFailure(invalidFieldsMode);
        AssertWebLinkFailure(missingSpanOptions);
        AssertWebLinkFailure(invalidSpanMode);
    }

    [Test]
    public void OperationOptionsProviderContractReturnsEveryOptionType()
    {
        IOperationOptionsProvider provider = new TestOptionsProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetUriParseOptions(), Is.Not.Null);
            Assert.That(provider.GetUriValidationOptions(), Is.Not.Null);
            Assert.That(provider.GetUriNormalizationOptions(), Is.Not.Null);
            Assert.That(provider.GetUriResolutionOptions(), Is.Not.Null);
            Assert.That(provider.GetUriRelativizationOptions(), Is.Not.Null);
            Assert.That(provider.GetUriComparisonOptions(), Is.Not.Null);
            Assert.That(provider.GetPercentCodecOptions(), Is.Not.Null);
            Assert.That(provider.GetUriQueryOptions(), Is.Not.Null);
            Assert.That(provider.GetIriParseOptions(), Is.Not.Null);
            Assert.That(provider.GetIriConversionOptions(), Is.Not.Null);
            Assert.That(provider.GetUriTemplateParseOptions(), Is.Not.Null);
            Assert.That(provider.GetUriTemplateExpansionOptions(), Is.Not.Null);
            Assert.That(provider.GetUrnParseOptions(), Is.Not.Null);
            Assert.That(provider.GetUrnLocatorMergeOptions(), Is.Not.Null);
            Assert.That(provider.GetWebLinkParseOptions(), Is.Not.Null);
        });
    }

    [Test]
    public void DiagnosticSinkContractReceivesCompleteDiagnosticContext()
    {
        TestDiagnosticSink sink = new TestDiagnosticSink();
        IUriDiagnosticSink contract = sink;
        UriDiagnostic diagnostic = new UriDiagnostic(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidPath,
            DiagnosticSeverity.Error,
            UriComponent.Path,
            new TextRange(3, 2),
            "Invalid path.");

        contract.ReportDiagnostic(diagnostic);

        Assert.That(sink.Diagnostic, Is.SameAs(diagnostic));
    }

    private static void AssertDiagnosticFailure(
        bool success,
        UriDiagnostic? diagnostic,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(diagnostic!.Code, Is.EqualTo(code));
            Assert.That(diagnostic.Component, Is.EqualTo(component));
        });
    }

    private static void AssertInvalidInput(UriDiagnostic? diagnostic)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.PercentEncoding));
            Assert.That(diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(diagnostic.Component, Is.EqualTo(UriComponent.Input));
        });
    }

    private static void AssertLimit(
        UriDiagnostic? diagnostic,
        UriComponent component)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(diagnostic.Component, Is.EqualTo(component));
        });
    }

    private static void AssertQueryFailure(
        UriQueryResult result,
        UriDiagnosticCode code)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Parameters, Is.Empty);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        });
    }

    private static void AssertWebLinkFailure(WebLinkParseResult result)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.LinkSet.Links, Is.Empty);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidInput));
        });
    }

    private class TestOptionsProvider : IOperationOptionsProvider
    {
        public UriParseOptions GetUriParseOptions()
        {
            return new UriParseOptions();
        }

        public UriValidationOptions GetUriValidationOptions()
        {
            return new UriValidationOptions();
        }

        public UriNormalizationOptions GetUriNormalizationOptions()
        {
            return new UriNormalizationOptions();
        }

        public UriResolutionOptions GetUriResolutionOptions()
        {
            return new UriResolutionOptions();
        }

        public UriRelativizationOptions GetUriRelativizationOptions()
        {
            return new UriRelativizationOptions();
        }

        public UriComparisonOptions GetUriComparisonOptions()
        {
            return new UriComparisonOptions();
        }

        public PercentCodecOptions GetPercentCodecOptions()
        {
            return new PercentCodecOptions();
        }

        public UriQueryOptions GetUriQueryOptions()
        {
            return new UriQueryOptions();
        }

        public IriParseOptions GetIriParseOptions()
        {
            return new IriParseOptions();
        }

        public IriConversionOptions GetIriConversionOptions()
        {
            return new IriConversionOptions();
        }

        public UriTemplateParseOptions GetUriTemplateParseOptions()
        {
            return new UriTemplateParseOptions();
        }

        public UriTemplateExpansionOptions GetUriTemplateExpansionOptions()
        {
            return new UriTemplateExpansionOptions();
        }

        public UrnParseOptions GetUrnParseOptions()
        {
            return new UrnParseOptions();
        }

        public UrnLocatorMergeOptions GetUrnLocatorMergeOptions()
        {
            return new UrnLocatorMergeOptions();
        }

        public WebLinkParseOptions GetWebLinkParseOptions()
        {
            return new WebLinkParseOptions();
        }
    }

    private class TestDiagnosticSink : IUriDiagnosticSink
    {
        internal UriDiagnostic? Diagnostic;

        public void ReportDiagnostic(UriDiagnostic diagnostic)
        {
            Diagnostic = diagnostic;
        }
    }
}
