using System.Text;
using email;

namespace email.Tests;

public class EmailMessageIdParserTests
{
    [Test]
    public void ParsesCanonicalMessageIdAndPreservesComponents()
    {
        EmailMessageIdParseResult result = ParseMessageId(" <1234.abc@Local.Example> ", EmailMessageIdParseMode.Strict);

        Assert.That(result.HasSingleIdentifier(), Is.True);
        EmailMessageId identifier = result.GetIdentifier()!;
        Assert.That(identifier!.GetIdLeft(), Is.EqualTo("1234.abc"));
        Assert.That(identifier.GetIdRight(), Is.EqualTo("Local.Example"));
        Assert.That(identifier.IsRightDomainLiteral(), Is.False);
        Assert.That(identifier.UsesObsoleteSyntax(), Is.False);
        Assert.That(identifier.WasRecovered(), Is.False);
        Assert.That(Text(identifier.GetRawBytes()), Is.EqualTo("<1234.abc@Local.Example>"));
        Assert.That(result.GetIdentifiers().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(Bytes(" <1234.abc@Local.Example> ").Length));
        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void ParsesDomainLiteralRightSide()
    {
        EmailMessageId identifier = ParseMessageId("<part@[IPv6:2001:db8::1]>", EmailMessageIdParseMode.Strict).GetIdentifier()!;

        Assert.That(identifier!.GetIdLeft(), Is.EqualTo("part"));
        Assert.That(identifier.GetIdRight(), Is.EqualTo("IPv6:2001:db8::1"));
        Assert.That(identifier.IsRightDomainLiteral(), Is.True);
    }

    [Test]
    public void ContentIdUsesTheSameSingleIdentifierSyntax()
    {
        EmailMessageIdParseResult result = new EmailMessageIdParser().ParseContentIdWithInputAndMode(Bytes("<image.1@cid.example>"), EmailMessageIdParseMode.Strict);

        Assert.That(result.GetIdentifier()!.GetIdLeft(), Is.EqualTo("image.1"));
        Assert.That(result.GetIdentifier()!.GetIdRight(), Is.EqualTo("cid.example"));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void ParsesReferencesAndInReplyToListsInOrder()
    {
        const string input = "<first@example>\r\n <second@example><third@[127.0.0.1]>";

        EmailMessageIdParseResult references = new EmailMessageIdParser().ParseReferencesWithInputAndMode(Bytes(input), EmailMessageIdParseMode.Strict);
        EmailMessageIdParseResult replies = new EmailMessageIdParser().ParseInReplyToWithInputAndMode(Bytes(input), EmailMessageIdParseMode.Strict);

        Assert.That(references.GetIdentifiers().GetCount(), Is.EqualTo(3));
        Assert.That(references.GetIdentifiers().GetAt(0).GetIdLeft(), Is.EqualTo("first"));
        Assert.That(references.GetIdentifiers().GetAt(1).GetIdLeft(), Is.EqualTo("second"));
        Assert.That(references.GetIdentifiers().GetAt(2).IsRightDomainLiteral(), Is.True);
        Assert.That(replies.GetIdentifiers().GetCount(), Is.EqualTo(3));
        Assert.That(references.GetIssues().GetCount(), Is.Zero);
        Assert.That(replies.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void ParsesObsoleteInternalCfwsAndAddressLikeParts()
    {
        const string input = "<left (comment) . part @ example (comment) . com>";

        EmailMessageIdParseResult tolerant = ParseMessageId(input, EmailMessageIdParseMode.Tolerant);
        EmailMessageIdParseResult strict = ParseMessageId(input, EmailMessageIdParseMode.Strict);

        Assert.That(tolerant.GetIdentifier()!.GetIdLeft(), Is.EqualTo("left.part"));
        Assert.That(tolerant.GetIdentifier()!.GetIdRight(), Is.EqualTo("example.com"));
        Assert.That(tolerant.GetIdentifier()!.UsesObsoleteSyntax(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.MessageIdObsoleteSyntax), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.HasSingleIdentifier(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void ParsesObsoleteQuotedLeftSide()
    {
        EmailMessageIdParseResult result = ParseMessageId("<\"quoted value\"@example.com>", EmailMessageIdParseMode.Tolerant);

        Assert.That(result.GetIdentifier()!.GetIdLeft(), Is.EqualTo("quoted value"));
        Assert.That(result.GetIdentifier()!.UsesObsoleteSyntax(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MessageIdObsoleteSyntax), Is.True);
    }

    [Test]
    public void IgnoresObsoletePhraseTextInReferenceFields()
    {
        const string input = "Your message <first@example> from yesterday <second@example>";

        EmailMessageIdParseResult result = new EmailMessageIdParser().ParseReferencesWithInputAndMode(Bytes(input), EmailMessageIdParseMode.Tolerant);

        Assert.That(result.GetIdentifiers().GetCount(), Is.EqualTo(2));
        Assert.That(result.GetIdentifiers().GetAt(0).GetIdLeft(), Is.EqualTo("first"));
        Assert.That(result.GetIdentifiers().GetAt(1).GetIdLeft(), Is.EqualTo("second"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MessageIdObsoleteSyntax), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void RecoversBareAndMissingClosingAngleForms()
    {
        EmailMessageIdParseResult bare = ParseMessageId("bare.id@example.com", EmailMessageIdParseMode.Tolerant);
        EmailMessageIdParseResult unclosed = ParseMessageId("<open.id@example.com", EmailMessageIdParseMode.Tolerant);

        Assert.That(bare.GetIdentifier()!.GetIdLeft(), Is.EqualTo("bare.id"));
        Assert.That(bare.GetIdentifier()!.WasRecovered(), Is.True);
        Assert.That(Text(bare.GetIdentifier()!.GetRawBytes()), Is.EqualTo("bare.id@example.com"));
        Assert.That(bare.GetIssues().ContainsCode(EmailIssueCode.MessageIdRecoveryApplied), Is.True);
        Assert.That(unclosed.GetIdentifier()!.GetIdLeft(), Is.EqualTo("open.id"));
        Assert.That(unclosed.GetIdentifier()!.WasRecovered(), Is.True);
        Assert.That(Text(unclosed.GetIdentifier()!.GetRawBytes()), Is.EqualTo("<open.id@example.com"));
        Assert.That(unclosed.GetIssues().ContainsCode(EmailIssueCode.MessageIdRecoveryApplied), Is.True);
    }

    [Test]
    public void RecoversCommaSeparatedBrokenLists()
    {
        EmailMessageIdParseResult result = new EmailMessageIdParser().ParseReferencesWithInputAndMode(Bytes("<one@example>, <two@example>"), EmailMessageIdParseMode.Tolerant);

        Assert.That(result.GetIdentifiers().GetCount(), Is.EqualTo(2));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MessageIdRecoveryApplied), Is.True);
    }

    [Test]
    public void SkipsMalformedIdentifierAndRetainsLaterValidOnes()
    {
        EmailMessageIdParseResult result = new EmailMessageIdParser().ParseReferencesWithInputAndMode(Bytes("<missing-at> <good@example>"), EmailMessageIdParseMode.Tolerant);

        Assert.That(result.GetIdentifiers().GetCount(), Is.EqualTo(1));
        Assert.That(result.GetIdentifiers().GetAt(0).GetIdLeft(), Is.EqualTo("good"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MessageIdMalformed), Is.True);
    }

    [Test]
    public void SingleValueFieldsRejectMultipleIdentifiersWithoutDiscardingThem()
    {
        EmailMessageIdParseResult result = ParseMessageId("<one@example> <two@example>", EmailMessageIdParseMode.Tolerant);

        Assert.That(result.HasSingleIdentifier(), Is.False);
        Assert.That(result.GetIdentifiers().GetCount(), Is.EqualTo(2));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MessageIdMalformed), Is.True);
    }

    [Test]
    public void StrictModeRetainsBrokenRecoveryButReportsFailure()
    {
        EmailMessageIdParseResult result = ParseMessageId("bare@example", EmailMessageIdParseMode.Strict);

        Assert.That(result.HasSingleIdentifier(), Is.True);
        Assert.That(result.GetIdentifier()!.WasRecovered(), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Error));
    }

    [Test]
    public void AppliesInputAndIdentifierCountLimits()
    {
        EmailMessageIdParserOptions inputOptions = new EmailMessageIdParserOptions();
        inputOptions.SetMaximumInputBytes(5);
        EmailMessageIdParseResult inputLimited = new EmailMessageIdParser().ParseReferencesWithInputAndOptions(Bytes("<one@example>"), inputOptions);
        EmailMessageIdParserOptions countOptions = new EmailMessageIdParserOptions();
        countOptions.SetMaximumIdentifiers(1);
        EmailMessageIdParseResult countLimited = new EmailMessageIdParser().ParseReferencesWithInputAndOptions(Bytes("<one@example> <two@example>"), countOptions);

        Assert.That(inputLimited.HasFatalError(), Is.True);
        Assert.That(inputLimited.GetConsumedByteCount(), Is.Zero);
        Assert.That(inputLimited.GetIssues().ContainsCode(EmailIssueCode.MessageIdLimitExceeded), Is.True);
        Assert.That(countLimited.HasFatalError(), Is.True);
        Assert.That(countLimited.GetIdentifiers().GetCount(), Is.EqualTo(1));
        Assert.That(countLimited.GetIssues().ContainsCode(EmailIssueCode.MessageIdLimitExceeded), Is.True);
    }

    [Test]
    public void TracksInitialSourcePositionAndDefensivelyCopiesRawBytes()
    {
        byte[] input = Bytes("<id@example>");
        EmailMessageIdParserOptions options = new EmailMessageIdParserOptions();
        EmailMessageIdParseResult result = new EmailMessageIdParser().ParseMessageIdWithInputAndOptionsAndInitialPosition(input, options, new EmailSourcePosition(50, 4, 7));
        input[1] = (byte)'X';
        byte[] raw = result.GetIdentifier()!.GetRawBytes();
        raw[1] = (byte)'Y';

        Assert.That(result.GetIdentifier()!.GetSourcePosition().GetByteOffset(), Is.EqualTo(50));
        Assert.That(result.GetIdentifier()!.GetSourcePosition().GetLineNumber(), Is.EqualTo(4));
        Assert.That(result.GetIdentifier()!.GetSourcePosition().GetColumnNumber(), Is.EqualTo(7));
        Assert.That(Text(result.GetIdentifier()!.GetRawBytes()), Is.EqualTo("<id@example>"));
        Assert.That(Text(result.GetIdentifiers().GetRawBytes()), Is.EqualTo("<id@example>"));
    }

    [Test]
    public void ParserOptionsValidateAndCloneIndependently()
    {
        EmailMessageIdParserOptions options = new EmailMessageIdParserOptions();
        options.SetMode(EmailMessageIdParseMode.Strict);
        options.SetMaximumIdentifiers(5);
        options.SetMaximumInputBytes(100);
        EmailMessageIdParserOptions clone = options.Clone();
        clone.SetMaximumIdentifiers(7);

        Assert.That(options.GetMaximumIdentifiers(), Is.EqualTo(5));
        Assert.That(clone.GetMaximumIdentifiers(), Is.EqualTo(7));
        Assert.That(clone.GetMode(), Is.EqualTo(EmailMessageIdParseMode.Strict));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMode((EmailMessageIdParseMode)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumIdentifiers(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumInputBytes(0));
    }

    [Test]
    public void DirectOptionOverloadsCoverEverySingleIdentifierField()
    {
        EmailMessageIdParser parser = new EmailMessageIdParser();
        EmailMessageIdParserOptions options = new EmailMessageIdParserOptions();

        Assert.That(parser.ParseMessageIdWithInputAndOptions(Bytes("<message@example>"), options).IsSuccess(), Is.True);
        Assert.That(parser.ParseContentIdWithInputAndOptions(Bytes("<content@example>"), options).IsSuccess(), Is.True);
        Assert.That(parser.ParseInReplyToWithInputAndOptions(Bytes("<reply@example>"), options).IsSuccess(), Is.True);
    }

    static EmailMessageIdParseResult ParseMessageId(string value, EmailMessageIdParseMode mode)
    {
        return new EmailMessageIdParser().ParseMessageIdWithInputAndMode(Bytes(value), mode);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
