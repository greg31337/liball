using System.Diagnostics;
using System.Text;
using email;

namespace email.Tests;

public class EmailParserMutationFuzzTests
{
    const int MutationCount = 14;
    const int MaximumCaseBytes = 4096;
    const long MaximumAllocatedBytesPerCase = 32 * 1024 * 1024;
    const long MaximumElapsedMillisecondsPerCase = 2000;

    [Test]
    public void MutationCasesAreDeterministicReplayableDefensiveAndBounded()
    {
        EmailMutationFuzzOptions options = MutationOptions(23, 96);
        byte[] seed = Bytes("Subject: deterministic\r\n\r\nseed");
        List<EmailMutationCase> first = EmailMutationFuzzer.Generate(seed, options);
        List<EmailMutationCase> second = EmailMutationFuzzer.Generate(seed, options);

        Assert.That(first, Has.Count.EqualTo(23));
        Assert.That(first.Select(value => value.GetKind()).Distinct().Count(), Is.EqualTo(7));
        int index = 0;
        while (index < first.Count)
        {
            Assert.That(first[index].GetReplayIdentifier(), Is.EqualTo(second[index].GetReplayIdentifier()), "case " + index);
            Assert.That(first[index].GetBytes(), Is.EqualTo(second[index].GetBytes()), "case " + index);
            Assert.That(first[index].GetBytes(), Has.Length.LessThanOrEqualTo(96), "case " + index);
            byte[] exposed = first[index].GetBytes();
            if (exposed.Length > 0)
            {
                byte original = first[index].GetBytes()[0];
                exposed[0] ^= 0xff;
                Assert.That(first[index].GetBytes()[0], Is.EqualTo(original), "defensive copy, case " + index);
            }

            index++;
        }

        EmailMutationFuzzOptions capped = MutationOptions(7, seed.Length);
        Assert.That(EmailMutationFuzzer.Generate(seed, capped).All(value => value.GetBytes().Length <= seed.Length), Is.True);
        EmailLimitException exception = Assert.Throws<EmailLimitException>(() => EmailMutationFuzzer.Generate(new byte[seed.Length + 1], capped));
        Assert.That(exception.GetLimitName(), Is.EqualTo("MaxFuzzCaseBytes"));
    }

    [Test]
    public void EveryParserFamilySurvivesDeterministicMutationsWithinTimeAndAllocationBudgets()
    {
        List<ParserFuzzTarget> targets = ParserTargets();
        Assert.That(targets, Has.Count.EqualTo(33));

        foreach (ParserFuzzTarget target in targets)
        {
            RunExpected(target, target.Seed, "warmup");
            EmailMutationFuzzOptions options = MutationOptions(MutationCount, MaximumCaseBytes);
            options.SetDeterministicSeed(0x154000 + target.Name.Length);
            List<EmailMutationCase> cases = EmailMutationFuzzer.Generate(target.Seed, options);
            foreach (EmailMutationCase mutation in cases)
            {
                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                Stopwatch elapsed = Stopwatch.StartNew();
                RunExpected(target, mutation.GetBytes(), mutation.GetReplayIdentifier());
                elapsed.Stop();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

                Assert.That(elapsed.ElapsedMilliseconds, Is.LessThanOrEqualTo(MaximumElapsedMillisecondsPerCase), target.Name + " time budget, " + mutation.GetReplayIdentifier());
                Assert.That(allocated, Is.LessThanOrEqualTo(MaximumAllocatedBytesPerCase), target.Name + " allocation budget, " + mutation.GetReplayIdentifier());
            }
        }
    }

    [Test]
    public void MessageHarnessEnforcesFileRecursionAndDecodedExpansionBudgetsWithoutLeaks()
    {
        string directory = Path.Combine(Path.GetTempPath(), "email-fuzz-budgets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            EmailParseOptions fileOptions = BoundedMessageOptions(directory);
            EmailStorageOptions storage = fileOptions.GetStorageOptions();
            storage.GetLimits().SetMemoryThresholdBytes(1);
            storage.GetLimits().SetMaxTemporaryBytes(4096);
            storage.GetLimits().SetMaxTemporaryFileCount(1);
            fileOptions.SetStorageOptions(storage);
            byte[] multipart = Bytes("Content-Type: multipart/mixed; boundary=x\r\n\r\n--x\r\nContent-Transfer-Encoding: base64\r\n\r\nQUJDRA==\r\n--x\r\nContent-Transfer-Encoding: base64\r\n\r\nRUZHSA==\r\n--x--\r\n");

            EmailLimitException fileLimit = Assert.Throws<EmailLimitException>(() => new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(multipart, fileOptions));
            Assert.That(fileLimit.GetLimitName(), Is.EqualTo("MaxTemporaryFileCount"));
            Assert.That(Directory.GetFiles(directory), Is.Empty, "temporary files after the file budget abort");

            EmailParseOptions recursionOptions = BoundedMessageOptions(directory);
            EmailLimits recursionLimits = recursionOptions.GetLimits();
            recursionLimits.GetParsingLimits().SetMaxNestingDepth(2);
            recursionOptions.SetLimits(recursionLimits);
            EmailParseResult recursion = new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(NestedMessage(5), recursionOptions);
            Assert.That(recursion.IsComplete(), Is.False);
            Assert.That(recursion.GetIssues().ContainsCode(EmailIssueCode.LimitExceeded), Is.True);

            EmailParseOptions expansionOptions = BoundedMessageOptions(directory);
            EmailLimits expansionLimits = expansionOptions.GetLimits();
            expansionLimits.GetDecodingLimits().SetMaxDecodedBytes(4);
            expansionLimits.GetDecodingLimits().SetMaxExpansionRatio(1);
            expansionOptions.SetLimits(expansionLimits);
            EmailParseResult expansion = new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes("Content-Transfer-Encoding: base64\r\n\r\nQUJDREVGR0g="), expansionOptions);
            Assert.That(expansion.IsSuccess(), Is.False);
            Assert.That(expansion.GetIssues().ContainsCode(EmailIssueCode.DecodedByteLimitExceeded), Is.True);
            Assert.That(expansion.GetMessage()!.GetBody()!.GetDecodedContent()!.GetLength(), Is.LessThanOrEqualTo(4));
            Assert.That(Directory.GetFiles(directory), Is.Empty, "temporary files after all budget cases");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static List<ParserFuzzTarget> ParserTargets()
    {
        byte[] header = Bytes("Alice <alice@example.test>; tag=value (comment)");
        byte[] message = MessageSeed();
        byte[] calendar = Bytes("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\nUID:fuzz@example.test\r\nEND:VEVENT\r\nEND:VCALENDAR\r\n");
        byte[] cms = new byte[] { 0x30, 0x0f, 0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x07, 0x01, 0xa0, 0x02, 0x04, 0x00 };
        byte[] openPgp = new byte[] { 0xcb, 0x07, 0x62, 0x00, 0x00, 0x00, 0x00, 0x00, 0x78 };
        byte[] tnef = new byte[] { 0x78, 0x9f, 0x3e, 0x22, 0x01, 0x00 };
        byte[] reportFields = Bytes("Reporting-MTA: dns; mx.example.test\r\n\r\nFinal-Recipient: rfc822; user@example.test\r\nAction: failed\r\nStatus: 5.1.1\r\n");

        return new List<ParserFuzzTarget>
        {
            new ParserFuzzTarget("EmailParser", message, ParseMessage),
            new ParserFuzzTarget("EmailMimeParser", message, ParseMime),
            new ParserFuzzTarget("EmailFeedParser", message, ParseFeed),
            new ParserFuzzTarget("EmailTokenReader", message, ParseTokens),
            new ParserFuzzTarget("EmailHeaderBlockParser", message, ParseHeaderBlock),
            new ParserFuzzTarget("EmailAddressParser", header, ParseAddress),
            new ParserFuzzTarget("EmailDateParser", header, ParseDate),
            new ParserFuzzTarget("EmailMessageIdParser", header, ParseMessageId),
            new ParserFuzzTarget("EmailParameterParser", header, ParseParameters),
            new ParserFuzzTarget("EmailMediaTypeParser", header, ParseMediaType),
            new ParserFuzzTarget("EmailDispositionParser", header, ParseDisposition),
            new ParserFuzzTarget("EmailContentLanguageParser", header, ParseContentLanguage),
            new ParserFuzzTarget("EmailReceivedParser", header, ParseReceived),
            new ParserFuzzTarget("EmailAuthenticationResultsParser", header, ParseAuthenticationResults),
            new ParserFuzzTarget("EmailReceivedSpfParser", header, ParseReceivedSpf),
            new ParserFuzzTarget("EmailDkimSignatureParser", header, ParseDkimSignature),
            new ParserFuzzTarget("EmailArcMessageSignatureParser", header, ParseArcMessageSignature),
            new ParserFuzzTarget("EmailArcSealParser", header, ParseArcSeal),
            new ParserFuzzTarget("EmailArcAuthenticationResultsParser", header, ParseArcAuthenticationResults),
            new ParserFuzzTarget("EmailDkimKeyParser", header, ParseDkimKey),
            new ParserFuzzTarget("EmailMetadataParser", header, ParseMetadata),
            new ParserFuzzTarget("EmailValueParser", header, ParseValueFacade),
            new ParserFuzzTarget("EmailEncodedWord", header, ParseEncodedWords),
            new ParserFuzzTarget("EmailCalendarParser", calendar, ParseCalendar),
            new ParserFuzzTarget("EmailCmsReader", cms, ParseCms),
            new ParserFuzzTarget("EmailOpenPgpReader", openPgp, ParseOpenPgp),
            new ParserFuzzTarget("EmailTnefReader", tnef, ParseTnef),
            new ParserFuzzTarget("EmailReportFieldBlockParser", reportFields, ParseReportFields),
            new ParserFuzzTarget("EmailReportParser", message, value => ParseReportEntity(value, 0)),
            new ParserFuzzTarget("EmailDeliveryStatusParser", message, value => ParseReportEntity(value, 1)),
            new ParserFuzzTarget("EmailDispositionNotificationParser", message, value => ParseReportEntity(value, 2)),
            new ParserFuzzTarget("EmailFeedbackReportParser", message, value => ParseReportEntity(value, 3)),
            new ParserFuzzTarget("EmailSecurityMultipartParser", message, value => ParseReportEntity(value, 4))
        };
    }

    static void RunExpected(ParserFuzzTarget target, byte[] value, string replay)
    {
        try
        {
            target.Parse(value);
        }
        catch (EmailLimitException)
        {
        }
        catch (Exception exception)
        {
            Assert.Fail(target.Name + " crashed for " + replay + ": " + exception);
        }
    }

    static void ParseMessage(byte[] value)
    {
        new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(value, BoundedMessageOptions(null!));
    }

    static void ParseMime(byte[] value)
    {
        new EmailMimeParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(value, BoundedMessageOptions(null!));
    }

    static void ParseFeed(byte[] value)
    {
        EmailFeedParser parser = new EmailFeedParserOperations().CreateWithOptions(BoundedMessageOptions(null!));
        int split = value.Length / 2;
        parser.Feed(new ReadOnlySpan<byte>(value, 0, split));
        parser.Feed(new ReadOnlySpan<byte>(value, split, value.Length - split));
        parser.Complete();
        parser.Close();
    }

    static void ParseTokens(byte[] value)
    {
        EmailTokenReader reader = new EmailTokenReaderOperations().CreateWithInputAndOptionsUsingValueArrayAndEmailParseOptions(value, BoundedMessageOptions(null!));
        while (reader.Read())
        {
            if (reader.GetTokenKind() == EmailTokenKind.BodyChunk)
            {
                reader.OpenBodyChunk().Close();
            }
        }

        reader.Close();
    }

    static void ParseHeaderBlock(byte[] value)
    {
        EmailHeaderBlockParserOptions options = new EmailHeaderBlockParserOptions();
        EmailParsingLimits limits = options.GetLimits();
        ApplyParsingBudgets(limits);
        options.SetLimits(limits);
        new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(value, options, new EmailSourcePosition(0, 1, 1), true);
    }

    static void ParseAddress(byte[] value)
    {
        EmailAddressParserOptions options = new EmailAddressParserOptions();
        options.SetMaximumAddresses(32);
        options.SetMaximumInputBytes(MaximumCaseBytes);
        new EmailAddressParser().ParseListWithInputAndOptions(value, options);
    }

    static void ParseDate(byte[] value)
    {
        EmailDateParserOptions options = new EmailDateParserOptions();
        options.SetMaximumInputBytes(MaximumCaseBytes);
        new EmailDateParser().ParseWithInputAndOptions(value, options);
    }

    static void ParseMessageId(byte[] value)
    {
        EmailMessageIdParserOptions options = new EmailMessageIdParserOptions();
        options.SetMaximumIdentifiers(32);
        options.SetMaximumInputBytes(MaximumCaseBytes);
        new EmailMessageIdParser().ParseReferencesWithInputAndOptions(value, options);
    }

    static EmailStructuredValueParserOptions StructuredOptions()
    {
        EmailStructuredValueParserOptions options = new EmailStructuredValueParserOptions();
        options.SetMaximumInputBytes(MaximumCaseBytes);
        options.SetMaximumParameters(32);
        options.SetMaximumDecodedParameterBytes(MaximumCaseBytes);
        return options;
    }

    static void ParseParameters(byte[] value) { new EmailParameterParser().ParseWithInputAndOptions(value, StructuredOptions()); }
    static void ParseMediaType(byte[] value) { new EmailMediaTypeParser().ParseWithInputAndOptions(value, StructuredOptions()); }
    static void ParseDisposition(byte[] value) { new EmailDispositionParser().ParseWithInputAndOptions(value, StructuredOptions()); }
    static void ParseContentLanguage(byte[] value) { new EmailContentLanguageParser().ParseWithInputAndOptions(value, StructuredOptions()); }

    static void ParseReceived(byte[] value)
    {
        EmailReceivedParserOptions options = new EmailReceivedParserOptions();
        options.SetMaximumClauses(32);
        options.SetMaximumInputBytes(MaximumCaseBytes);
        new EmailReceivedParser().ParseWithInputAndOptions(value, options);
    }

    static EmailAuthenticationParserOptions AuthenticationOptions()
    {
        EmailAuthenticationParserOptions options = new EmailAuthenticationParserOptions();
        options.SetMaximumInputBytes(MaximumCaseBytes);
        options.SetMaximumResults(32);
        options.SetMaximumProperties(64);
        return options;
    }

    static void ParseAuthenticationResults(byte[] value) { new EmailAuthenticationResultsParser().ParseWithInputAndOptions(value, AuthenticationOptions()); }
    static void ParseReceivedSpf(byte[] value) { new EmailReceivedSpfParser().ParseWithInputAndOptions(value, AuthenticationOptions()); }

    static EmailSignatureParserOptions SignatureOptions()
    {
        EmailSignatureParserOptions options = new EmailSignatureParserOptions();
        options.SetMaximumInputBytes(MaximumCaseBytes);
        options.SetMaximumTags(32);
        return options;
    }

    static void ParseDkimSignature(byte[] value) { new EmailDkimSignatureParser().ParseWithInputAndOptions(value, SignatureOptions()); }
    static void ParseArcMessageSignature(byte[] value) { new EmailArcMessageSignatureParser().ParseWithInputAndOptions(value, SignatureOptions()); }
    static void ParseArcSeal(byte[] value) { new EmailArcSealParser().ParseWithInputAndOptions(value, SignatureOptions()); }
    static void ParseArcAuthenticationResults(byte[] value) { new EmailArcAuthenticationResultsParser().ParseWithInputAndOptions(value, SignatureOptions()); }

    static void ParseDkimKey(byte[] value)
    {
        EmailDkimKeyParserOptions options = new EmailDkimKeyParserOptions();
        options.SetMaximumInputBytes(MaximumCaseBytes);
        options.SetMaximumTags(32);
        options.SetMaximumDecodedKeyBytes(MaximumCaseBytes);
        new EmailDkimKeyParser().ParseWithInputAndOptions(value, options);
    }

    static void ParseMetadata(byte[] value)
    {
        EmailMetadataParserOptions options = new EmailMetadataParserOptions();
        options.SetMaximumInputBytes(MaximumCaseBytes);
        options.SetMaximumItems(32);
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        new EmailMetadataParser().ParseKeywordsWithInputAndOptionsAndInitialPosition(value, options, position);
        new EmailMetadataParser().ParsePriorityWithInputAndOptionsAndInitialPosition(value, options, position);
        new EmailMetadataParser().ParseAutoSubmittedWithInputAndOptionsAndInitialPosition(value, options, position);
        new EmailMetadataParser().ParseListUnsubscribePostWithInputAndOptionsAndInitialPosition(value, options, position);
    }

    static void ParseValueFacade(byte[] value)
    {
        new EmailValueParser().ParseAddressListWithInputAndMode(value, EmailAddressParseMode.Tolerant);
        new EmailValueParser().ParseDateWithInputAndMode(value, EmailDateParseMode.Tolerant);
        new EmailValueParser().ParseMessageIdWithInputAndMode(value, EmailMessageIdParseMode.Tolerant);
        new EmailValueParser().ParseMediaTypeWithInputAndMode(value, EmailStructuredValueParseMode.Tolerant);
    }

    static void ParseEncodedWords(byte[] value)
    {
        new EmailEncodedWord().ParseWithInputAndModeAndContextAndMaximumEncodedWordsAndMaximumDecodedBytes(value, EmailEncodedWordParseMode.Tolerant, EmailEncodedWordContext.UnstructuredText, 32, MaximumCaseBytes);
    }

    static void ParseCalendar(byte[] value)
    {
        EmailCalendarParseOptions options = new EmailCalendarParseOptions();
        options.SetMaximumCalendarBytes(MaximumCaseBytes);
        options.SetMaximumPhysicalLineBytes(1024);
        options.SetMaximumUnfoldedLineBytes(2048);
        options.SetMaximumContentLines(128);
        options.SetMaximumProperties(128);
        options.SetMaximumComponents(32);
        options.SetMaximumComponentDepth(4);
        options.SetMaximumParametersPerProperty(32);
        options.SetMaximumParameterValues(64);
        new EmailCalendarParser().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCalendarParseOptions(value, options);
    }

    static EmailCryptographicLimits CryptographicBudgets()
    {
        EmailCryptographicLimits limits = new EmailCryptographicLimits();
        limits.SetMaxInputBytes(MaximumCaseBytes);
        limits.SetMaxAsn1Depth(8);
        limits.SetMaxAsn1ElementCount(128);
        limits.SetMaxOpenPgpPacketCount(128);
        limits.SetMaxDecompressedBytes(MaximumCaseBytes);
        limits.SetMaxOperationCount(64);
        return limits;
    }

    static void ParseCms(byte[] value)
    {
        EmailCmsReaderOptions options = new EmailCmsReaderOptions();
        options.SetLimits(CryptographicBudgets());
        new EmailCmsReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailCmsReaderOptions(value, options);
    }

    static void ParseOpenPgp(byte[] value)
    {
        EmailOpenPgpReaderOptions options = new EmailOpenPgpReaderOptions();
        options.SetLimits(CryptographicBudgets());
        options.SetMaximumArmorLineBytes(1024);
        options.SetMaximumArmorHeaderCount(16);
        new EmailOpenPgpReader().ParseWithInputAndOptions(value, options);
    }

    static void ParseTnef(byte[] value)
    {
        EmailTnefReaderOptions options = new EmailTnefReaderOptions();
        EmailArchiveLimits limits = options.GetLimits();
        limits.SetMaxEntryBytes(MaximumCaseBytes);
        limits.SetMaxTnefAttributeCount(128);
        options.SetLimits(limits);
        options.SetMaximumEmbeddedDepth(4);
        options.SetMaximumPropertyCount(128);
        options.SetMaximumPropertyValueCount(128);
        options.SetMaximumRecipientCount(128);
        options.SetMaximumDecodedRtfBytes(MaximumCaseBytes);
        new EmailTnefReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(value, options);
    }

    static EmailReportParseOptions ReportOptions()
    {
        EmailReportParseOptions options = new EmailReportParseOptions();
        options.SetMaximumReportBytes(MaximumCaseBytes);
        options.SetMaximumPartCount(16);
        options.SetMaximumFieldBlockCount(32);
        options.SetMaximumFieldCount(128);
        options.SetMaximumFieldsPerBlock(32);
        options.SetMaximumFieldBlockBytes(2048);
        options.SetMaximumLineBytes(1024);
        return options;
    }

    static void ParseReportFields(byte[] value)
    {
        new EmailReportFieldBlockParser().ParseWithInputAndOptions(value, ReportOptions());
    }

    static void ParseReportEntity(byte[] value, int operation)
    {
        EmailParseResult parsed = new EmailParser().ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(value, BoundedMessageOptions(null!));
        if (!parsed.HasMessage()) return;
        EmailMessage message = parsed.GetMessage()!;
        if (operation == 0) new EmailReportParser().ParseWithMessageAndOptions(message!, ReportOptions());
        else if (operation == 1) new EmailDeliveryStatusParser().ParseWithMessageAndOptions(message!, ReportOptions());
        else if (operation == 2) new EmailDispositionNotificationParser().ParseWithMessageAndOptions(message!, ReportOptions());
        else if (operation == 3) new EmailFeedbackReportParser().ParseWithMessageAndOptions(message!, ReportOptions());
        else new EmailSecurityMultipartParser().ParseWithMessage(message!);
    }

    static EmailParseOptions BoundedMessageOptions(string directory)
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreateConformingTolerant();
        EmailLimits limits = options.GetLimits();
        ApplyParsingBudgets(limits.GetParsingLimits());
        limits.GetDecodingLimits().SetMaxDecodedBytes(MaximumCaseBytes);
        limits.GetDecodingLimits().SetMaxDecompressedBytes(MaximumCaseBytes);
        limits.GetDecodingLimits().SetMaxExpansionRatio(2);
        options.SetLimits(limits);
        EmailBodyIngestOptions ingest = options.GetBodyIngestOptions();
        ingest.SetStorageMode(EmailBodyStorageMode.Store);
        ingest.SetDecodeOnIngest(true);
        options.SetBodyIngestOptions(ingest);
        EmailStorageOptions storage = options.GetStorageOptions();
        storage.GetLimits().SetMemoryThresholdBytes(MaximumCaseBytes);
        storage.GetLimits().SetMaxTemporaryBytes(MaximumCaseBytes);
        storage.GetLimits().SetMaxTemporaryFileCount(2);
        storage.GetLimits().SetMaxOpenStreamCount(2);
        storage.GetLimits().SetMaxMaterializedBytes(MaximumCaseBytes);
        if (directory != null) storage.SetTemporaryDirectory(directory);
        options.SetStorageOptions(storage);
        return options;
    }

    static void ApplyParsingBudgets(EmailParsingLimits limits)
    {
        limits.SetMaxMessageBytes(MaximumCaseBytes);
        limits.SetMaxHeaderBytes(2048);
        limits.SetMaxHeaderFieldCount(64);
        limits.SetMaxLineBytes(1024);
        limits.SetMaxNestingDepth(4);
        limits.SetMaxPartCount(32);
        limits.SetMaxParametersPerHeader(32);
        limits.SetMaxAddressesPerHeader(32);
        limits.SetMaxEncodedWordsPerHeader(32);
    }

    static EmailMutationFuzzOptions MutationOptions(int count, int maximumBytes)
    {
        EmailMutationFuzzOptions options = new EmailMutationFuzzOptions();
        options.SetMutationCount(count);
        options.SetMaximumCaseBytes(maximumBytes);
        options.SetMaximumMutationBytes(16);
        return options;
    }

    static byte[] MessageSeed()
    {
        return Bytes(
            "Subject: mutation seed\r\nContent-Type: multipart/report; report-type=delivery-status; boundary=x\r\n\r\n" +
            "--x\r\nContent-Type: text/plain\r\n\r\nDelivery failed.\r\n" +
            "--x\r\nContent-Type: message/delivery-status\r\n\r\nReporting-MTA: dns; mx.example.test\r\n\r\nFinal-Recipient: rfc822; user@example.test\r\nAction: failed\r\nStatus: 5.1.1\r\n" +
            "--x--\r\n");
    }

    static byte[] NestedMessage(int depth)
    {
        StringBuilder source = new StringBuilder();
        int index = 0;
        while (index < depth)
        {
            source.Append("Content-Type: message/rfc822\r\n\r\n");
            index++;
        }

        source.Append("Subject: leaf\r\n\r\nbody");
        return Bytes(source.ToString());
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }
}

class ParserFuzzTarget
{
    public ParserFuzzTarget(string name, byte[] seed, Action<byte[]> parse)
    {
        Name = name;
        Seed = seed;
        Parse = parse;
    }

    public string Name { get; }
    public byte[] Seed { get; }
    public Action<byte[]> Parse { get; }
}
