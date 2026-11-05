namespace svg.Tests;

public class SvgHostileInputTests
{
    [TestCase("<!DOCTYPE svg [<!ENTITY a 'x'><!ENTITY b '&a;&a;&a;&a;'>]><svg>&b;</svg>")]
    [TestCase("<!DOCTYPE svg [<!ENTITY external SYSTEM 'file:///definitely-not-read'>]><svg>&external;</svg>")]
    [TestCase("<!DOCTYPE svg SYSTEM 'https://example.invalid/not-read.dtd'><svg/>")]
    public void DefaultPolicyRejectsEntityExpansionInputsAtTheDoctype(string input)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = "memory:entity";

        SvgLoadResult first = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);
        SvgLoadResult second = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.Document, Is.Null);
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(first.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(first.Diagnostics[0].Source.StartOffset, Is.Zero);
            Assert.That(first.Diagnostics[0].Source.Source.Uri, Is.EqualTo("memory:entity"));
            Assert.That(first.Diagnostics[0].Message, Does.Contain("prohibited"));
            AssertEquivalentDiagnostic(first.Diagnostics[0], second.Diagnostics[0]);
        });
    }

    [Test]
    public void PreserveOnlyRetainsDoctypeButNeverExpandsNestedOrExternalEntities()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.DtdPolicy = SvgDtdPolicy.PreserveOnly;
        string nestedInput = "<!DOCTYPE svg [<!ENTITY a 'expanded'><!ENTITY b '&a;&a;'>]><svg>&b;</svg>";
        string externalInput = "<!DOCTYPE svg [<!ENTITY x SYSTEM 'file:///definitely-not-read'>]><svg>&x;</svg>";

        SvgLoadResult nested = new SvgLoader().LoadWithStringAndSvgLoadOptions(nestedInput, options);
        SvgLoadResult external = new SvgLoader().LoadWithStringAndSvgLoadOptions(externalInput, options);

        Assert.Multiple(() =>
        {
            Assert.That(nested.Success, Is.False);
            Assert.That(nested.Document, Is.Null);
            Assert.That(nested.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(nested.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(nested.Diagnostics[0].Source.StartOffset, Is.EqualTo(nestedInput.IndexOf("&b;", StringComparison.Ordinal) + 1));
            Assert.That(nested.Diagnostics[0].Message, Does.Contain("not expanded"));
            Assert.That(nested.Diagnostics[0].Message, Does.Not.Contain("expandedexpanded"));
            Assert.That(external.Success, Is.False);
            Assert.That(external.Document, Is.Null);
            Assert.That(external.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(external.Diagnostics[0].Source.StartOffset, Is.EqualTo(externalInput.IndexOf("&x;", StringComparison.Ordinal) + 1));
            Assert.That(external.Diagnostics[0].Message, Does.Contain("not expanded"));
        });
    }

    [Test]
    public void RawByteStreamReaderAndCallbackInputsStopAtConfiguredSizeLimits()
    {
        byte[] oversizedBytes = System.Text.Encoding.UTF8.GetBytes("<svg>oversized</svg>");
        string oversizedText = "<svg>oversized</svg>";
        SvgLoadOptions byteOptions = new SvgLoadOptions();
        byteOptions.Limits.Input.MaxInputBytes = oversizedBytes.Length - 1;
        SvgLoadOptions textOptions = new SvgLoadOptions();
        textOptions.Limits.Input.MaxInputCharacters = oversizedText.Length - 1;
        using MemoryStream stream = new MemoryStream(oversizedBytes);
        using StringReader reader = new StringReader(oversizedText);

        SvgLoadResult bytes = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(oversizedBytes, byteOptions);
        SvgLoadResult streamed = new SvgLoader().LoadWithStreamAndOptions(stream, byteOptions);
        SvgLoadResult characters = new SvgLoader().LoadWithStringAndSvgLoadOptions(oversizedText, textOptions);
        SvgLoadResult read = new SvgLoader().LoadWithTextReaderAndSvgLoadOptions(reader, textOptions);
        SvgLoadOptions callbackOptions = new SvgLoadOptions();
        callbackOptions.Limits.Input.MaxInputBytes = oversizedBytes.Length - 1;
        callbackOptions.ResourceProvider = new SvgResourceProvider(request =>
        {
            SvgResourceResponse response = new SvgResourceResponse();
            response.Bytes = oversizedBytes;
            return response;
        });
        SvgLoadResult callback = new SvgLoader().LoadUri("memory:oversized", callbackOptions);

        Assert.Multiple(() =>
        {
            AssertLimitFailure(bytes);
            AssertLimitFailure(streamed);
            AssertLimitFailure(characters);
            AssertLimitFailure(read);
            AssertLimitFailure(callback);
            Assert.That(bytes.Source, Is.Null);
            Assert.That(streamed.Source, Is.Null);
            Assert.That(characters.Source, Is.Null);
            Assert.That(read.Source, Is.Null);
            Assert.That(callback.Source, Is.Null);
            Assert.That(callback.Resources.RequestCount, Is.EqualTo(1));
            Assert.That(callback.Resources.LoadedCount, Is.Zero);
        });
    }

    [Test]
    public void GzipOutputUsesAnExactInclusiveBoundaryAndRejectsOneByteLess()
    {
        string xml = "<svg>" + new string('x', 2048) + "</svg>";
        byte[] plain = System.Text.Encoding.UTF8.GetBytes(xml);
        byte[] compressed = Compress(plain);
        SvgLoadOptions exactOptions = new SvgLoadOptions();
        exactOptions.Limits.Input.MaxInputBytes = compressed.Length;
        exactOptions.Limits.Input.MaxGzipOutputBytes = plain.Length;
        SvgLoadOptions shortOptions = new SvgLoadOptions();
        shortOptions.Limits.Input.MaxInputBytes = compressed.Length;
        shortOptions.Limits.Input.MaxGzipOutputBytes = plain.Length - 1;

        SvgLoadResult exact = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(compressed, exactOptions);
        SvgLoadResult shortResult = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(compressed, shortOptions);
        SvgLoadResult repeated = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(compressed, shortOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.DetectedContainer, Is.EqualTo("gzip"));
            Assert.That(exact.DecodedCharacters, Is.EqualTo(xml));
            Assert.That(shortResult.Success, Is.False);
            Assert.That(shortResult.Document, Is.Null);
            Assert.That(shortResult.DetectedContainer, Is.EqualTo("gzip"));
            Assert.That(shortResult.DecodedCharacters, Is.Null);
            Assert.That(shortResult.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(shortResult.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(shortResult.Diagnostics[0].Message, Does.Contain("gzip output"));
            AssertEquivalentDiagnostic(shortResult.Diagnostics[0], repeated.Diagnostics[0]);
        });
    }

    [Test]
    public void DepthNodeAttributeAndTextBudgetsProduceRepeatableLimitDiagnostics()
    {
        List<(string Input, SvgLoadOptions Options)> cases = new List<(string, SvgLoadOptions)>();
        SvgLoadOptions depth = new SvgLoadOptions();
        depth.Limits.Tree.MaxDepth = 2;
        cases.Add(("<svg><g><rect/></g></svg>", depth));
        SvgLoadOptions nodes = new SvgLoadOptions();
        nodes.Limits.Tree.MaxNodes = 3;
        cases.Add(("<svg><g/><rect/><circle/></svg>", nodes));
        SvgLoadOptions perElement = new SvgLoadOptions();
        perElement.Limits.Tree.MaxAttributesPerElement = 2;
        cases.Add(("<svg a='1' b='2' c='3'/>", perElement));
        SvgLoadOptions totalAttributes = new SvgLoadOptions();
        totalAttributes.Limits.Tree.MaxTotalAttributes = 2;
        cases.Add(("<svg a='1'><g b='2'/><rect c='3'/></svg>", totalAttributes));
        SvgLoadOptions text = new SvgLoadOptions();
        text.Limits.Tree.MaxTextCharacters = 4;
        cases.Add(("<svg>ab<g>cd</g>ef</svg>", text));

        foreach ((string input, SvgLoadOptions options) in cases)
        {
            SvgLoadResult first = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);
            SvgLoadResult second = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);

            Assert.Multiple(() =>
            {
                AssertLimitFailure(first);
                Assert.That(first.Document, Is.Null);
                Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(first.Diagnostics[0].Source, Is.Not.Null);
                Assert.That(first.Diagnostics[0].Source.StartOffset, Is.GreaterThanOrEqualTo(0));
                AssertEquivalentDiagnostic(first.Diagnostics[0], second.Diagnostics[0]);
            });
        }
    }

    [TestCase(new byte[] { 60, 115, 118, 103, 62, 195, 60 })]
    [TestCase(new byte[] { 60, 115, 118, 103, 62, 192, 175, 60 })]
    [TestCase(new byte[] { 60, 115, 118, 103, 62, 226, 130 })]
    [TestCase(new byte[] { 255, 254, 60, 0, 115 })]
    public void MalformedUnicodeByteSequencesFailWithoutPublishingDecodedText(byte[] bytes)
    {
        SvgLoadResult first = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(bytes, new SvgLoadOptions());
        SvgLoadResult second = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(bytes, new SvgLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.Document, Is.Null);
            Assert.That(first.DecodedCharacters, Is.Null);
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(first.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.UnsupportedEncoding));
            AssertEquivalentDiagnostic(first.Diagnostics[0], second.Diagnostics[0]);
        });
    }

    [TestCase("<svg a='unterminated></svg>")]
    [TestCase("<svg>&unknown;</svg>")]
    [TestCase("<svg><!--bad--comment--></svg>")]
    [TestCase("<svg><![CDATA[unterminated</svg>")]
    [TestCase("<svg><g></svg>")]
    public void MalformedXmlFormsReturnOneDeterministicLocatedDiagnostic(string input)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = "memory:malformed";

        SvgLoadResult first = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);
        SvgLoadResult second = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.Document, Is.Null);
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(first.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(first.Diagnostics[0].Source, Is.Not.Null);
            Assert.That(first.Diagnostics[0].Source.Source.Uri, Is.EqualTo("memory:malformed"));
            Assert.That(first.Diagnostics[0].Source.StartOffset, Is.GreaterThanOrEqualTo(0));
            AssertEquivalentDiagnostic(first.Diagnostics[0], second.Diagnostics[0]);
        });
    }

    [Test]
    public void UnpairedUtf16SurrogatesReturnDeterministicMalformedXmlDiagnostics()
    {
        string highInput = string.Concat("<svg>", '\ud800', "</svg>");
        string lowInput = string.Concat("<svg>", '\udc00', "</svg>");

        SvgLoadResult high = new SvgLoader().LoadWithStringAndSvgLoadOptions(highInput, new SvgLoadOptions());
        SvgLoadResult highRepeated = new SvgLoader().LoadWithStringAndSvgLoadOptions(highInput, new SvgLoadOptions());
        SvgLoadResult low = new SvgLoader().LoadWithStringAndSvgLoadOptions(lowInput, new SvgLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(high.Success, Is.False);
            Assert.That(high.Document, Is.Null);
            Assert.That(high.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(high.Diagnostics.Single().Source.StartOffset, Is.EqualTo(5));
            AssertEquivalentDiagnostic(high.Diagnostics.Single(), highRepeated.Diagnostics.Single());
            Assert.That(low.Success, Is.False);
            Assert.That(low.Document, Is.Null);
            Assert.That(low.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(low.Diagnostics.Single().Source.StartOffset, Is.EqualTo(5));
        });
    }

    static void AssertLimitFailure(SvgLoadResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
    }

    static void AssertEquivalentDiagnostic(SvgDiagnostic first, SvgDiagnostic second)
    {
        Assert.That(second.Code, Is.EqualTo(first.Code));
        Assert.That(second.Severity, Is.EqualTo(first.Severity));
        Assert.That(second.Message, Is.EqualTo(first.Message));
        if (first.Source == null || second.Source == null)
        {
            Assert.That(second.Source, Is.SameAs(first.Source));
            return;
        }
        Assert.That(second.Source.StartOffset, Is.EqualTo(first.Source.StartOffset));
        Assert.That(second.Source.EndOffset, Is.EqualTo(first.Source.EndOffset));
        Assert.That(second.Source.StartLine, Is.EqualTo(first.Source.StartLine));
        Assert.That(second.Source.StartColumn, Is.EqualTo(first.Source.StartColumn));
        Assert.That(second.Source.Source.Uri, Is.EqualTo(first.Source.Source.Uri));
    }

    static byte[] Compress(byte[] value)
    {
        using MemoryStream output = new MemoryStream();
        using (System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.SmallestSize, true))
        {
            gzip.Write(value, 0, value.Length);
        }
        return output.ToArray();
    }
}
