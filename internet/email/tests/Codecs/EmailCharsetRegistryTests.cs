using System.Text;
using email;

namespace email.Tests;

public class EmailCharsetRegistryTests
{
    [Test]
    public void BuiltInAliasesAreCaseInsensitiveAndExposeIndependentEncodings()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();

        EmailCharsetLookupResult ascii = registry.Lookup("ASCII");
        EmailCharsetLookupResult utf8 = registry.Lookup("Utf8");
        EmailCharsetLookupResult latin1 = registry.Lookup("LATIN-1");
        Encoding first = utf8.GetEncoding()!;
        first!.DecoderFallback = DecoderFallback.ExceptionFallback;

        Assert.That(ascii.IsFound(), Is.True);
        Assert.That(ascii.GetRequestedName(), Is.EqualTo("ASCII"));
        Assert.That(ascii.GetCanonicalName(), Is.EqualTo("us-ascii"));
        Assert.That(ascii.GetCodePage(), Is.EqualTo(20127));
        Assert.That(utf8.GetCanonicalName(), Is.EqualTo("utf-8"));
        Assert.That(utf8.GetEncoding()!.DecoderFallback, Is.Not.SameAs(DecoderFallback.ExceptionFallback));
        Assert.That(latin1.GetCodePage(), Is.EqualTo(28591));
        EmailCharsetLookupResult missing = registry.Lookup("not-a-real-charset");
        Assert.That(missing.IsFound(), Is.False);
        Assert.That(missing.GetRequestedName(), Is.EqualTo("not-a-real-charset"));
        Assert.That(registry.Contains("utf8"), Is.True);
        Assert.That(registry.Contains("not-a-real-charset"), Is.False);
    }

    [Test]
    public void BclCodePageProviderMakesLegacyEncodingsAndNumericAliasesAvailable()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetDecodeOptions options = new EmailCharsetDecodeOptions();

        EmailCharsetDecodeResult byName = registry.Decode(new byte[] { 0x80 }, "windows-1252", options);
        EmailCharsetDecodeResult byNumber = registry.Decode(new byte[] { 0x80 }, "cp1252", options);

        Assert.That(byName.GetText(), Is.EqualTo("€"));
        Assert.That(byNumber.GetText(), Is.EqualTo("€"));
        Assert.That(byName.IsSuccess(), Is.True);
        Assert.That(byName.GetUndecodableBytes(), Is.Empty);
    }

    [Test]
    public void CallerRegistrationAndAliasesOwnEncodingSnapshotsAndHonorCollisionPolicy()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        Encoding supplied = (Encoding)Encoding.Latin1.Clone();

        registry.Register("x-example", supplied, false);
        registry.RegisterAlias("x-example-alias", "X-EXAMPLE", false);
        supplied.DecoderFallback = DecoderFallback.ExceptionFallback;

        EmailCharsetDecodeResult decoded = registry.Decode(new byte[] { 0xE9 }, "X-EXAMPLE-ALIAS", new EmailCharsetDecodeOptions());
        Assert.That(decoded.GetText(), Is.EqualTo("é"));
        Assert.That(decoded.GetResolvedCharset(), Is.EqualTo("x-example"));
        Assert.That(() => registry.Register("x-example", Encoding.ASCII, false), Throws.InvalidOperationException);
        Assert.That(() => registry.RegisterAlias("x-example-alias", "x-example", false), Throws.InvalidOperationException);

        registry.Register("x-example", Encoding.ASCII, true);
        EmailCharsetDecodeResult replaced = registry.Decode(new byte[] { 0x41 }, "x-example", new EmailCharsetDecodeOptions());
        Assert.That(replaced.GetText(), Is.EqualTo("A"));
    }

    [TestCase("utf-8", new byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, "utf-8", 3)]
    [TestCase("utf-16", new byte[] { 0xFF, 0xFE, 0x41, 0x00 }, "utf-16", 2)]
    [TestCase("utf-16be", new byte[] { 0xFE, 0xFF, 0x00, 0x41 }, "utf-16be", 2)]
    [TestCase("utf-32", new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00 }, "utf-32", 4)]
    [TestCase("utf-32be", new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x41 }, "utf-32be", 4)]
    public void BomDetectionSelectsEncodingAndRemovesPreamble(string declared, byte[] input, string detected, int byteCount)
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();

        EmailCharsetDecodeResult result = registry.Decode(input, declared, new EmailCharsetDecodeOptions());

        Assert.That(result.GetText(), Is.EqualTo("A"));
        Assert.That(result.GetResolvedCharset(), Is.EqualTo(detected));
        Assert.That(result.HasBom(), Is.True);
        Assert.That(result.GetBomCharset(), Is.EqualTo(detected));
        Assert.That(result.GetBomByteCount(), Is.EqualTo(byteCount));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void BomCanSupplyMissingCharsetAndRecoverAnUnsupportedDeclaration()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        byte[] input = new byte[] { 0xEF, 0xBB, 0xBF, 0x41 };

        EmailCharsetDecodeResult missing = registry.Decode(input, null, new EmailCharsetDecodeOptions());
        EmailCharsetDecodeResult unsupported = registry.Decode(input, "x-unknown", new EmailCharsetDecodeOptions());

        Assert.That(missing.HasRequestedCharset(), Is.False);
        Assert.That(missing.GetRequestedCharset(), Is.Null);
        Assert.That(missing.GetText(), Is.EqualTo("A"));
        Assert.That(missing.GetResolvedCharset(), Is.EqualTo("utf-8"));
        Assert.That(missing.IsSuccess(), Is.True);
        Assert.That(unsupported.GetText(), Is.EqualTo("A"));
        Assert.That(unsupported.HasRequestedCharset(), Is.True);
        Assert.That(unsupported.GetRequestedCharset(), Is.EqualTo("x-unknown"));
        Assert.That(unsupported.GetIssues().ContainsCode(EmailIssueCode.CharsetUnsupported), Is.True);
        Assert.That(unsupported.IsSuccess(), Is.True);
    }

    [Test]
    public void BomMismatchUsesBomAndSeverityFollowsPolicy()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        byte[] input = new byte[] { 0xEF, 0xBB, 0xBF, 0xC3, 0xA9 };
        EmailCharsetDecodeOptions detect = new EmailCharsetDecodeOptions();
        EmailCharsetDecodeOptions require = new EmailCharsetDecodeOptions();
        require.SetBomPolicy(EmailCharsetBomPolicy.RequireMatch);

        EmailCharsetDecodeResult recovered = registry.Decode(input, "us-ascii", detect);
        EmailCharsetDecodeResult rejected = registry.Decode(input, "us-ascii", require);

        Assert.That(recovered.GetText(), Is.EqualTo("é"));
        Assert.That(recovered.GetResolvedCharset(), Is.EqualTo("utf-8"));
        Assert.That(recovered.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(recovered.IsSuccess(), Is.True);
        Assert.That(rejected.GetText(), Is.EqualTo("é"));
        Assert.That(rejected.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(rejected.IsSuccess(), Is.False);
    }

    [Test]
    public void IgnoreBomPolicyTreatsPreambleAsDeclaredCharsetPayload()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetDecodeOptions options = new EmailCharsetDecodeOptions();
        options.SetBomPolicy(EmailCharsetBomPolicy.Ignore);

        EmailCharsetDecodeResult result = registry.Decode(new byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, "iso-8859-1", options);

        Assert.That(result.GetText(), Is.EqualTo("ï»¿A"));
        Assert.That(result.HasBom(), Is.False);
        Assert.That(result.GetResolvedCharset(), Is.EqualTo("iso-8859-1"));
    }

    [Test]
    public void ReplacementFallbackInsertsConfiguredTextAndRetainsExactInvalidUtf8Bytes()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetDecodeOptions options = new EmailCharsetDecodeOptions();
        options.SetReplacement("[bad]");
        byte[] input = new byte[] { 0x41, 0xF0, 0x28, 0x8C, 0x28, 0x42 };

        EmailCharsetDecodeResult result = registry.Decode(input, "utf-8", options);

        Assert.That(result.GetText(), Is.EqualTo("A[bad]([bad](B"));
        Assert.That(result.GetUndecodableBytes(), Is.EqualTo(new byte[] { 0xF0, 0x8C }));
        Assert.That(result.UsedReplacement(), Is.True);
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(2));
        Assert.That(result.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void StrictFallbackOmitsInvalidSequencesAndReturnsErrorsWithExactPositions()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetDecodeOptions options = new EmailCharsetDecodeOptions();
        options.SetFallbackPolicy(EmailCharsetFallbackPolicy.Strict);
        byte[] input = new byte[] { 0x61, 0x0A, 0xFF, 0x62 };

        EmailCharsetDecodeResult result = registry.Decode(input, "ascii", options);

        Assert.That(result.GetText(), Is.EqualTo("a\nb"));
        Assert.That(result.GetUndecodableBytes(), Is.EqualTo(new byte[] { 0xFF }));
        Assert.That(result.UsedReplacement(), Is.False);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
        Assert.That(result.GetIssues().GetAt(0).GetSourcePosition()!.GetByteOffset(), Is.EqualTo(2));
        Assert.That(result.GetIssues().GetAt(0).GetSourcePosition()!.GetLineNumber(), Is.EqualTo(2));
        Assert.That(result.GetIssues().GetAt(0).GetSourcePosition()!.GetColumnNumber(), Is.EqualTo(1));
    }

    [TestCase(new byte[] { 0xC0, 0x80 })]
    [TestCase(new byte[] { 0xED, 0xA0, 0x80 })]
    [TestCase(new byte[] { 0xF4, 0x90, 0x80, 0x80 })]
    [TestCase(new byte[] { 0xE2, 0x82 })]
    public void Utf8RejectsOverlongSurrogateOutOfRangeAndTruncatedForms(byte[] input)
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetDecodeResult result = registry.Decode(input, "utf-8", new EmailCharsetDecodeOptions());

        Assert.That(result.GetUndecodableBytes(), Is.EqualTo(input));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CharsetInvalidByteSequence), Is.True);
        Assert.That(result.UsedReplacement(), Is.True);
    }

    [Test]
    public void Utf16AndUtf32ReportMalformedUnitsAndRetainEveryRejectedByte()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetDecodeOptions options = new EmailCharsetDecodeOptions();
        byte[] utf16 = new byte[] { 0x00, 0xD8, 0x41, 0x00, 0xFF };
        byte[] utf32 = new byte[] { 0x00, 0xD8, 0x00, 0x00, 0x41 };

        EmailCharsetDecodeResult sixteen = registry.Decode(utf16, "utf-16", options);
        EmailCharsetDecodeResult thirtyTwo = registry.Decode(utf32, "utf-32", options);

        Assert.That(sixteen.GetText(), Is.EqualTo("�A�"));
        Assert.That(sixteen.GetUndecodableBytes(), Is.EqualTo(new byte[] { 0x00, 0xD8, 0xFF }));
        Assert.That(thirtyTwo.GetText(), Is.EqualTo("��"));
        Assert.That(thirtyTwo.GetUndecodableBytes(), Is.EqualTo(utf32));
    }

    [Test]
    public void LegacyMultibyteDecoderReportsAnIncompleteSequence()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();

        EmailCharsetDecodeResult result = registry.Decode(new byte[] { 0x82 }, "shift_jis", new EmailCharsetDecodeOptions());

        Assert.That(result.GetText(), Is.EqualTo("�"));
        Assert.That(result.GetUndecodableBytes(), Is.EqualTo(new byte[] { 0x82 }));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CharsetInvalidByteSequence), Is.True);
    }

    [Test]
    public void UnsupportedCharsetProducesNoTextAndRetainsInput()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        byte[] input = new byte[] { 1, 2, 3 };

        EmailCharsetDecodeResult result = registry.Decode(input, "x-unsupported", new EmailCharsetDecodeOptions());

        Assert.That(result.GetText(), Is.Empty);
        Assert.That(result.HasResolvedCharset(), Is.False);
        Assert.That(result.GetUndecodableBytes(), Is.EqualTo(input));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CharsetUnsupported), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
    }

    [Test]
    public void InvalidUntrustedCharsetLabelReturnsADiagnosticInsteadOfThrowing()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        byte[] input = new byte[] { 0x41 };

        EmailCharsetDecodeResult result = registry.Decode(input, "utf 8", new EmailCharsetDecodeOptions());

        Assert.That(result.GetText(), Is.Empty);
        Assert.That(result.GetUndecodableBytes(), Is.EqualTo(input));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CharsetUnsupported), Is.True);
    }

    [Test]
    public void CharacterAndCharsetNameLimitsReturnBoundedResultsAndFatalIssues()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetDecodeOptions characterLimit = new EmailCharsetDecodeOptions();
        characterLimit.SetMaximumCharacters(2);
        EmailCharsetDecodeOptions nameLimit = new EmailCharsetDecodeOptions();
        nameLimit.SetMaximumCharsetNameLength(4);

        EmailCharsetDecodeResult characters = registry.Decode(Encoding.UTF8.GetBytes("A😀B"), "utf-8", characterLimit);
        EmailCharsetDecodeResult name = registry.Decode(new byte[] { 0x41 }, "utf-8", nameLimit);

        Assert.That(characters.GetText(), Is.EqualTo("A"));
        Assert.That(characters.HasFatalError(), Is.True);
        Assert.That(characters.GetIssues().ContainsCode(EmailIssueCode.CharsetCharacterLimitExceeded), Is.True);
        Assert.That(name.GetText(), Is.Empty);
        Assert.That(name.GetUndecodableBytes(), Is.EqualTo(new byte[] { 0x41 }));
        Assert.That(name.HasFatalError(), Is.True);
        Assert.That(name.GetIssues().ContainsCode(EmailIssueCode.CharsetNameLimitExceeded), Is.True);
    }

    [Test]
    public void ResultCollectionsAndRegisteredNameListsAreDefensiveSnapshots()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetDecodeResult result = registry.Decode(new byte[] { 0xFF }, "ascii", new EmailCharsetDecodeOptions());

        byte[] bytes = result.GetUndecodableBytes();
        bytes[0] = 0;
        EmailIssueList issues = result.GetIssues();
        issues.Clear();
        List<string> names = registry.GetRegisteredNames();
        int originalCount = registry.GetRegisteredNameCount();
        names.Clear();

        Assert.That(result.GetUndecodableBytes(), Is.EqualTo(new byte[] { 0xFF }));
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));
        Assert.That(registry.GetRegisteredNameCount(), Is.EqualTo(originalCount));
        Assert.That(registry.GetRegisteredNames(), Is.Ordered.Using((IComparer<string>)StringComparer.OrdinalIgnoreCase));
    }

    [Test]
    public void PublicInputsRejectInvalidConfigurationAndNames()
    {
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetDecodeOptions options = new EmailCharsetDecodeOptions();

        Assert.That(() => registry.Lookup("bad name"), Throws.ArgumentException);
        Assert.That(() => registry.RegisterAlias("alias", "missing", false), Throws.ArgumentException);
        Assert.That(() => options.SetFallbackPolicy((EmailCharsetFallbackPolicy)99), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.SetBomPolicy((EmailCharsetBomPolicy)99), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.SetReplacement(null!), Throws.ArgumentNullException);
        Assert.That(() => options.SetReplacement(""), Throws.ArgumentException);
        Assert.That(() => options.SetMaximumCharacters(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => options.SetMaximumCharsetNameLength(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => registry.Decode(new byte[0], "utf-8", null!), Throws.ArgumentNullException);
    }
}
