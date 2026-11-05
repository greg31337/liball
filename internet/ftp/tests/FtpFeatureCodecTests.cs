namespace ftp.Tests;

public class FtpFeatureCodecTests
{
    [Test]
    public void Parser_preserves_repeated_unknown_and_compound_features_in_wire_order()
    {
        FtpReply reply = ParseReply(
            "211-Extensions supported:\r\n" +
            " AUTH tls SSL\r\n" +
            " auth TLS-C\r\n" +
            " MLST Type*;Size;Modify*;UNIX.mode;\r\n" +
            " REST STREAM\r\n" +
            " X-vendor Alpha  Beta\r\n" +
            " UTF8\r\n" +
            " UTF8 ON\r\n" +
            "211 End\r\n");

        FtpFeatureSet featureSet = FtpFeatureCodec.ParseFeatureReply(reply);

        Assert.Multiple(() =>
        {
            Assert.That(featureSet.Negotiated, Is.True);
            Assert.That(featureSet.SourceReply, Is.SameAs(reply));
            Assert.That(featureSet.Features.Select(feature => feature.Name), Is.EqualTo(new[]
            {
                "AUTH", "AUTH", "MLST", "REST STREAM", "X-VENDOR", "UTF8", "UTF8"
            }));
            Assert.That(featureSet.Features[0].Parameters, Is.EqualTo("tls SSL"));
            Assert.That(featureSet.Features[3].Parameters, Is.Null);
            Assert.That(featureSet.Features[4].Parameters, Is.EqualTo("Alpha  Beta"));
            Assert.That(featureSet.Features[4].RawLine, Is.EqualTo(" X-vendor Alpha  Beta"));
            Assert.That(featureSet.Features[5].Parameters, Is.Null);
            Assert.That(featureSet.Features[6].Parameters, Is.EqualTo("ON"));
            Assert.That(featureSet.Features.All(feature =>
                feature.Support == FtpFeatureSupport.Supported), Is.True);
        });
    }

    [Test]
    public void Authentication_extraction_combines_repeated_AUTH_mechanism_lists()
    {
        FtpFeatureSet featureSet = FtpFeatureCodec.ParseFeatureReply(ParseReply(
            "211-Features:\r\n AUTH tls SSL\r\n AUTH TLS-C\r\n UTF8\r\n211 End\r\n"));

        string[] mechanisms = FtpFeatureCodec.ExtractAuthenticationMechanisms(featureSet);

        Assert.That(mechanisms, Is.EqualTo(new[] { "TLS", "SSL", "TLS-C" }));
    }

    [Test]
    public void Mlst_extraction_normalizes_facts_and_identifies_asterisk_defaults()
    {
        FtpFeatureSet featureSet = FtpFeatureCodec.ParseFeatureReply(ParseReply(
            "211-Features:\r\n" +
            " MLST Type*;Size;Modify*;UNIX.mode;\r\n" +
            " MLST Charset;\r\n" +
            "211 End\r\n"));

        FtpMlstFeatureInfo info = FtpFeatureCodec.ExtractMlstFacts(featureSet);

        Assert.Multiple(() =>
        {
            Assert.That(info.Advertised, Is.True);
            Assert.That(info.Facts, Is.EqualTo(new[]
            {
                "type", "size", "modify", "unix.mode", "charset"
            }));
            Assert.That(info.DefaultFacts, Is.EqualTo(new[] { "type", "modify" }));
        });
    }

    [Test]
    public void Generator_emits_canonical_multiline_AUTH_MLST_and_repeated_unknown_features()
    {
        FtpFeatureSet featureSet = new FtpFeatureSet();
        featureSet.Features = new[]
        {
            FtpFeatureCodec.CreateAuthenticationFeature(new[] { "tls", "TLS-C" }),
            FtpFeatureCodec.CreateMlstFeature(
                new[] { "Type", "Size", "Modify" }, new[] { "TYPE", "modify" }),
            CreateFeature("x-foo", "alpha"),
            CreateFeature("X-FOO", "beta")
        };

        FtpReply reply = FtpFeatureCodec.GenerateFeatureReply(featureSet, "Features supported:", "End");
        FtpReplyWriter writer = new FtpReplyWriter(
            FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines);
        string formatted = writer.FormatReply(reply);

        Assert.Multiple(() =>
        {
            Assert.That(formatted, Is.EqualTo(
                "211-Features supported:\r\n" +
                " AUTH TLS TLS-C\r\n" +
                " MLST type*;size;modify*;\r\n" +
                " X-FOO alpha\r\n" +
                " X-FOO beta\r\n" +
                "211 End\r\n"));
            Assert.That(reply.Multiline, Is.True);
            Assert.That(reply.Classification.Succeeded, Is.True);
            Assert.That(reply.Lines[1].RawText, Is.EqualTo(" AUTH TLS TLS-C"));
        });

        FtpFeatureSet reparsed = FtpFeatureCodec.ParseFeatureReply(ParseReply(formatted));
        Assert.That(reparsed.Features.Select(feature => feature.Name),
            Is.EqualTo(new[] { "AUTH", "MLST", "X-FOO", "X-FOO" }));
    }

    [Test]
    public void Empty_success_and_negative_FEAT_replies_retain_negotiation_state()
    {
        FtpFeatureSet empty = FtpFeatureCodec.ParseFeatureReply(
            ParseReply("211 No features\r\n"));
        FtpReply unsupportedReply = ParseReply("502 FEAT not implemented\r\n");
        FtpFeatureSet unsupported = FtpFeatureCodec.ParseFeatureReply(unsupportedReply);
        FtpReply generated = FtpFeatureCodec.GenerateFeatureReply(
            new FtpFeatureSet(), "Features", "No features");
        FtpReplyWriter writer = new FtpReplyWriter(
            FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Negotiated, Is.True);
            Assert.That(empty.Features, Is.Empty);
            Assert.That(FtpFeatureCodec.FindFeatureSupport(empty, FtpFeatureNames.Utf8),
                Is.EqualTo(FtpFeatureSupport.NotSupported));
            Assert.That(unsupported.Negotiated, Is.False);
            Assert.That(unsupported.SourceReply, Is.SameAs(unsupportedReply));
            Assert.That(FtpFeatureCodec.FindFeatureSupport(unsupported, FtpFeatureNames.Utf8),
                Is.EqualTo(FtpFeatureSupport.Unknown));
            Assert.That(generated.Multiline, Is.False);
            Assert.That(writer.FormatReply(generated), Is.EqualTo("211 No features\r\n"));
        });
    }

    [Test]
    public void Feature_support_lookup_is_case_insensitive_after_negotiation()
    {
        FtpFeatureSet featureSet = FtpFeatureCodec.ParseFeatureReply(
            ParseReply("211-Features:\r\n utf8\r\n211 End\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(FtpFeatureCodec.FindFeatureSupport(featureSet, "Utf8"),
                Is.EqualTo(FtpFeatureSupport.Supported));
            Assert.That(FtpFeatureCodec.FindFeatureSupport(featureSet, "SIZE"),
                Is.EqualTo(FtpFeatureSupport.NotSupported));
        });
    }

    [Test]
    public void Extended_FEAT_terminator_text_is_accepted_only_in_lenient_mode()
    {
        FtpReply reply = ParseReply(
            "211-Features:\r\n UTF8\r\n211 End FEAT.\r\n");

        FtpFeatureSet compatible =
            FtpFeatureCodec.ParseLenientFeatureReply(reply);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpFeatureCodec.ParseFeatureReply(reply));
            Assert.That(compatible.Negotiated, Is.True);
            Assert.That(compatible.Features.Select(feature => feature.Name),
                Is.EqualTo(new[] { "UTF8" }));
            Assert.That(compatible.SourceReply, Is.SameAs(reply));
        });
    }

    [Test]
    public void Feature_parameters_preserve_rfc_TCHAR_horizontal_tabs()
    {
        FtpFeatureSet parsed = FtpFeatureCodec.ParseFeatureReply(ParseReply(
            "211-Features:\r\n X-VENDOR alpha\tbeta\r\n211 End\r\n"));
        FtpReply generated = FtpFeatureCodec.GenerateFeatureReply(parsed, "Features:", "End");
        FtpReplyWriter writer = new FtpReplyWriter(
            FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Features[0].Parameters, Is.EqualTo("alpha\tbeta"));
            Assert.That(parsed.Features[0].RawLine, Is.EqualTo(" X-VENDOR alpha\tbeta"));
            Assert.That(writer.FormatReply(generated), Is.EqualTo(
                "211-Features:\r\n X-VENDOR alpha\tbeta\r\n211 End\r\n"));
        });
    }

    [Test]
    public void Parser_rejects_wrong_success_code_and_invalid_feature_indentation()
    {
        FtpReply wrongCode = ParseReply("200 FEAT okay\r\n");
        FtpReply unindented = ParseReply("211-Features:\r\nUTF8\r\n211 End\r\n");
        FtpReply overIndented = ParseReply("211-Features:\r\n  UTF8\r\n211 End\r\n");
        FtpReply emptyMultiline = ParseReply("211-Features:\r\n211 End\r\n");
        FtpReply wrongTerminator = ParseReply("211-Features:\r\n UTF8\r\n211 Done\r\n");

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpFeatureCodec.ParseFeatureReply(wrongCode));
            Assert.Throws<FtpProtocolException>(() => FtpFeatureCodec.ParseFeatureReply(unindented));
            Assert.Throws<FtpProtocolException>(() => FtpFeatureCodec.ParseFeatureReply(overIndented));
            Assert.Throws<FtpProtocolException>(() => FtpFeatureCodec.ParseFeatureReply(emptyMultiline));
            Assert.Throws<FtpProtocolException>(() => FtpFeatureCodec.ParseFeatureReply(wrongTerminator));
            Assert.Throws<FtpProtocolException>(() => FtpFeatureCodec.ParseFeatureLine(" UTF8 "));
            Assert.Throws<FtpProtocolException>(() => FtpFeatureCodec.ParseFeatureLine(" X-CAFÉ value"));
        });
    }

    [Test]
    public void Typed_extraction_rejects_malformed_AUTH_and_MLST_parameter_grammars()
    {
        FtpFeatureSet missingMechanism = CreateFeatureSet(CreateFeature("AUTH", null));
        FtpFeatureSet emptyMechanism = CreateFeatureSet(CreateFeature("AUTH", "TLS  SSL"));
        FtpFeatureSet missingSemicolon = CreateFeatureSet(CreateFeature("MLST", "Type*;Size"));
        FtpFeatureSet invalidAsterisk = CreateFeatureSet(CreateFeature("MLST", "Ty*pe;"));

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpFeatureCodec.ExtractAuthenticationMechanisms(missingMechanism));
            Assert.Throws<FtpProtocolException>(() =>
                FtpFeatureCodec.ExtractAuthenticationMechanisms(emptyMechanism));
            Assert.Throws<FtpProtocolException>(() =>
                FtpFeatureCodec.ExtractMlstFacts(missingSemicolon));
            Assert.Throws<FtpProtocolException>(() =>
                FtpFeatureCodec.ExtractMlstFacts(invalidAsterisk));
        });
    }

    [Test]
    public void Feature_creators_validate_mechanisms_default_facts_and_collection_bounds()
    {
        FtpFeature[] tooManyFeatures = new FtpFeature[FtpDefaults.MaximumFeatureCount + 1];
        int index = 0;
        while (index < tooManyFeatures.Length)
        {
            tooManyFeatures[index] = CreateFeature("X-" + index, null);
            index++;
        }
        FtpFeatureSet oversized = new FtpFeatureSet();
        oversized.Features = tooManyFeatures;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpFeatureCodec.CreateAuthenticationFeature(new string[0]));
            Assert.Throws<FtpProtocolException>(() => FtpFeatureCodec.CreateMlstFeature(
                new[] { "type" }, new[] { "size" }));
            Assert.Throws<FtpProtocolException>(() =>
                FtpFeatureCodec.GenerateFeatureReply(oversized, "Features", "End"));
        });
    }

    private static FtpReply ParseReply(string text)
    {
        FtpReplyParser parser = new FtpReplyParser(
            FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines, true);
        return parser.ParseReplyText(text);
    }

    private static FtpFeature CreateFeature(string name, string? parameters)
    {
        FtpFeature feature = new FtpFeature();
        feature.Name = name;
        feature.Parameters = parameters;
        feature.Support = FtpFeatureSupport.Supported;
        return feature;
    }

    private static FtpFeatureSet CreateFeatureSet(FtpFeature feature)
    {
        FtpFeatureSet featureSet = new FtpFeatureSet();
        featureSet.Features = new[] { feature };
        featureSet.Negotiated = true;
        return featureSet;
    }
}
