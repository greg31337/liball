using fake;

namespace fake.Tests;

[TestFixture]
public class PublicApiSpanCompletionTests
{
    [Test]
    public void UnsignedNumericConvenienceMethodsHonorExclusiveBounds()
    {
        PrimitiveProvider primitive = Fake.Create(15110).Primitive();

        Assert.Multiple(() =>
        {
            Assert.That(
                primitive.NumberStringFromMinimumAndMaximum(10UL, 11UL),
                Is.EqualTo("10"));
            Assert.That(
                primitive.BinaryFromMinimumAndMaximum(2UL, 3UL),
                Is.EqualTo("10"));
            Assert.That(
                primitive.OctalFromMinimumAndMaximum(8UL, 9UL),
                Is.EqualTo("10"));
            Assert.That(
                primitive.HexFromMinimumAndMaximum(15UL, 16UL),
                Is.EqualTo("f"));
            Assert.That(
                primitive.Base36FromMinimumAndMaximum(35UL, 36UL),
                Is.EqualTo("z"));
            Assert.That(
                () => primitive.NumberStringFromMinimumAndMaximum(1UL, 1UL),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void UnsignedNumericWritersHonorFormattingAndDestinationBounds()
    {
        PrimitiveProvider primitive = Fake.Create(15111).Primitive();
        char[] decimalValue = Enumerable.Repeat('#', 6).ToArray();
        char[] binaryValue = Enumerable.Repeat('#', 7).ToArray();
        char[] octalValue = Enumerable.Repeat('#', 7).ToArray();
        char[] base36Value = Enumerable.Repeat('#', 6).ToArray();

        int decimalWritten = primitive
            .WriteNumberStringFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(
                decimalValue,
                7UL,
                7UL,
                true,
                true,
                3);
        int binaryWritten = primitive
            .WriteBinaryFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(
                binaryValue,
                2UL,
                2UL,
                true,
                true,
                4);
        int octalWritten = primitive
            .WriteOctalFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(
                octalValue,
                8UL,
                8UL,
                true,
                true,
                4);
        int base36Written = primitive
            .WriteBase36FromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(
                base36Value,
                35UL,
                35UL,
                true,
                true,
                3);

        Assert.Multiple(() =>
        {
            Assert.That(decimalWritten, Is.EqualTo(4));
            Assert.That(new string(decimalValue, 0, decimalWritten),
                Is.EqualTo("+007"));
            Assert.That(decimalValue.Skip(decimalWritten),
                Has.All.EqualTo('#'));
            Assert.That(binaryWritten, Is.EqualTo(5));
            Assert.That(new string(binaryValue, 0, binaryWritten),
                Is.EqualTo("+0010"));
            Assert.That(binaryValue.Skip(binaryWritten),
                Has.All.EqualTo('#'));
            Assert.That(octalWritten, Is.EqualTo(5));
            Assert.That(new string(octalValue, 0, octalWritten),
                Is.EqualTo("+0010"));
            Assert.That(octalValue.Skip(octalWritten),
                Has.All.EqualTo('#'));
            Assert.That(base36Written, Is.EqualTo(4));
            Assert.That(new string(base36Value, 0, base36Written),
                Is.EqualTo("+00z"));
            Assert.That(base36Value.Skip(base36Written),
                Has.All.EqualTo('#'));
            Assert.That(
                () => primitive
                    .WriteNumberStringFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(
                        new char[3],
                        7UL,
                        7UL,
                        true,
                        true,
                        3),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => primitive
                    .WriteBinaryFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(
                        new char[4],
                        2UL,
                        2UL,
                        true,
                        true,
                        4),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => primitive
                    .WriteOctalFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(
                        new char[4],
                        8UL,
                        8UL,
                        true,
                        true,
                        4),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => primitive
                    .WriteBase36FromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(
                        new char[3],
                        35UL,
                        35UL,
                        true,
                        true,
                        3),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void GenericUnsignedRadixWriterUsesTheRequestedRadix()
    {
        PrimitiveProvider primitive = Fake.Create(15114).Primitive();
        char[] destination = Enumerable.Repeat('#', 7).ToArray();

        int written = primitive
            .WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingSpanOfCharAndInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(
                destination,
                16,
                255UL,
                255UL,
                true,
                true,
                4);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(5));
            Assert.That(new string(destination, 0, written),
                Is.EqualTo("+00ff"));
            Assert.That(destination.Skip(written), Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void ObjectIdDestinationOperationsRoundTripWithoutTouchingSuffixes()
    {
        IdentifierProvider identifier = Fake.Create(15112).Identifier();
        byte[] source = new byte[]
        {
            0x00,
            0x01,
            0x02,
            0x03,
            0x04,
            0x05,
            0x06,
            0x07,
            0x08,
            0x09,
            0x0a,
            0x0b
        };
        char[] formatted = Enumerable.Repeat('#', 26).ToArray();
        byte[] parsed = Enumerable.Repeat((byte)0xff, 14).ToArray();

        int characters = identifier.WriteObjectIdFromValueAndDestination(
            source,
            formatted);
        int bytes = identifier.ParseObjectIdFromValueAndDestination(
            formatted.AsSpan(0, characters),
            parsed);

        Assert.Multiple(() =>
        {
            Assert.That(characters, Is.EqualTo(24));
            Assert.That(new string(formatted, 0, characters),
                Is.EqualTo("000102030405060708090a0b"));
            Assert.That(formatted.Skip(characters), Has.All.EqualTo('#'));
            Assert.That(bytes, Is.EqualTo(12));
            Assert.That(parsed.Take(bytes), Is.EqualTo(source));
            Assert.That(parsed.Skip(bytes), Has.All.EqualTo((byte)0xff));
            Assert.That(
                () => identifier.WriteObjectIdFromValueAndDestination(
                    source,
                    new char[23]),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => identifier.ParseObjectIdFromValueAndDestination(
                    "000102030405060708090a0b".AsSpan(),
                    new byte[11]),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void IdentifierWritersHonorExplicitValuesAndExactLengths()
    {
        IdentifierProvider identifier = Fake.Create(15113).Identifier();
        char[] isbn = Enumerable.Repeat('#', 22).ToArray();
        char[] issn = Enumerable.Repeat('#', 18).ToArray();
        char[] imeiSv = Enumerable.Repeat('#', 18).ToArray();
        char[] vin = Enumerable.Repeat('#', 19).ToArray();

        int isbnWritten = identifier
            .WriteIsbn10FromValueAndDestinationAndStyle(
                "0306406152".AsSpan(),
                isbn,
                BookIdentifierStyle.Labeled);
        int issnWritten = identifier
            .WriteIssnFromValueAndDestinationAndStyle(
                "03178471".AsSpan(),
                issn,
                BookIdentifierStyle.Labeled);
        int imeiSvWritten = identifier
            .WriteImeiSvFromDestinationAndSoftwareVersion(imeiSv, 42);
        int vinWritten = identifier.WriteVin(vin);

        Assert.Multiple(() =>
        {
            Assert.That(new string(isbn, 0, isbnWritten),
                Is.EqualTo("ISBN 0-306-40615-2"));
            Assert.That(isbn.Skip(isbnWritten), Has.All.EqualTo('#'));
            Assert.That(new string(issn, 0, issnWritten),
                Is.EqualTo("ISSN 0317-8471"));
            Assert.That(issn.Skip(issnWritten), Has.All.EqualTo('#'));
            Assert.That(imeiSvWritten, Is.EqualTo(16));
            Assert.That(new string(imeiSv, 0, imeiSvWritten),
                Does.EndWith("42"));
            Assert.That(identifier.ValidateImeiSv(
                new string(imeiSv, 0, imeiSvWritten)), Is.True);
            Assert.That(imeiSv.Skip(imeiSvWritten), Has.All.EqualTo('#'));
            Assert.That(vinWritten, Is.EqualTo(17));
            Assert.That(identifier.ValidateVin(
                new string(vin, 0, vinWritten)), Is.True);
            Assert.That(vin.Skip(vinWritten), Has.All.EqualTo('#'));
            Assert.That(
                () => identifier
                    .WriteIsbn10FromValueAndDestinationAndStyle(
                        "0306406152".AsSpan(),
                        new char[17],
                        BookIdentifierStyle.Labeled),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => identifier
                    .WriteIssnFromValueAndDestinationAndStyle(
                        "03178471".AsSpan(),
                        new char[13],
                        BookIdentifierStyle.Labeled),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => identifier
                    .WriteImeiSvFromDestinationAndSoftwareVersion(
                        new char[15],
                        42),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => identifier.WriteVin(new char[16]),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void TimestampIdentifierSpanAliasesPreserveRequestedInstants()
    {
        IdentifierProvider identifier = Fake.Create(15115).Identifier();
        DateTimeOffset timestamp = new DateTimeOffset(
            2026,
            8,
            25,
            12,
            34,
            56,
            TimeSpan.Zero).AddMilliseconds(789);
        long expectedMilliseconds = timestamp.ToUnixTimeMilliseconds();
        char[] ulidText = new char[26];
        byte[] ulidBytes = new byte[16];
        char[] monotonicText = new char[26];
        byte[] monotonicBytes = new byte[16];
        char[] sortableText = new char[32];
        byte[] sortableBytes = new byte[16];

        int ulidCharacters = identifier
            .WriteUlidFromDestinationAndTimestamp(ulidText, timestamp);
        int ulidByteCount = identifier
            .WriteUlidBytesFromDestinationAndTimestamp(ulidBytes, timestamp);
        int monotonicCharacters = identifier
            .WriteMonotonicUlidFromDestinationAndTimestamp(
                monotonicText,
                timestamp);
        int monotonicByteCount = identifier
            .WriteMonotonicUlidBytesFromDestinationAndTimestamp(
                monotonicBytes,
                timestamp);
        int sortableCharacters = identifier
            .WriteSortableTimestampIdFromDestinationAndTimestamp(
                sortableText,
                timestamp);
        int sortableByteCount = identifier
            .WriteSortableTimestampIdBytesFromDestinationAndTimestamp(
                sortableBytes,
                timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(ulidCharacters, Is.EqualTo(26));
            Assert.That(identifier.ValidateUlidFromValue(ulidText.AsSpan()),
                Is.True);
            Assert.That(identifier.UlidUnixMillisecondsFromValue(
                ulidText.AsSpan()), Is.EqualTo(expectedMilliseconds));
            Assert.That(ulidByteCount, Is.EqualTo(16));
            Assert.That(identifier
                .UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(
                    ulidBytes), Is.EqualTo(expectedMilliseconds));
            Assert.That(monotonicCharacters, Is.EqualTo(26));
            Assert.That(identifier.ValidateUlidFromValue(
                monotonicText.AsSpan()), Is.True);
            Assert.That(monotonicByteCount, Is.EqualTo(16));
            Assert.That(identifier
                .UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(
                    monotonicBytes), Is.EqualTo(expectedMilliseconds));
            Assert.That(sortableCharacters, Is.EqualTo(32));
            Assert.That(identifier.ValidateSortableTimestampIdFromValue(
                sortableText.AsSpan()), Is.True);
            Assert.That(identifier
                .SortableTimestampIdUnixMillisecondsFromValue(
                    sortableText.AsSpan()), Is.EqualTo(expectedMilliseconds));
            Assert.That(sortableByteCount, Is.EqualTo(16));
            Assert.That(identifier
                .SortableTimestampIdUnixMillisecondsFromValueUsingReadOnlySpanOfByte(
                    sortableBytes), Is.EqualTo(expectedMilliseconds));
        });
    }

    [Test]
    public void IdentifierSpanAliasesRoundTripEverySupportedRepresentation()
    {
        IdentifierProvider identifier = Fake.Create(15116).Identifier();
        string uuid4 = identifier.Uuid4();
        byte[] uuid4Bytes = identifier.ParseUuidFromValue(uuid4.AsSpan());
        string uuid7 = identifier.Uuid7();
        string ulid = identifier.Ulid();
        byte[] ulidBytes = identifier.ParseUlidFromValue(ulid.AsSpan());
        string objectId = identifier.ObjectId();
        byte[] objectIdBytes = identifier.ParseObjectIdFromValue(
            objectId.AsSpan());
        string sortable = identifier.SortableTimestampId();
        byte[] sortableBytes = identifier.ParseSortableTimestampIdFromValue(
            sortable.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(uuid4Bytes, Has.Length.EqualTo(16));
            Assert.That(identifier.ValidateUuidFromValue(uuid4.AsSpan()),
                Is.True);
            Assert.That(identifier.ValidateUuid4FromValue(uuid4.AsSpan()),
                Is.True);
            Assert.That(identifier.ValidateUuid7FromValue(uuid7.AsSpan()),
                Is.True);
            Assert.That(identifier.FormatUlidFromValue(ulidBytes),
                Is.EqualTo(ulid));
            Assert.That(identifier.ValidateUlidFromValue(ulid.AsSpan()),
                Is.True);
            Assert.That(identifier.UlidUnixMillisecondsFromValue(
                ulid.AsSpan()),
                Is.EqualTo(identifier
                    .UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(
                        ulidBytes)));
            Assert.That(identifier.FormatObjectIdFromValue(objectIdBytes),
                Is.EqualTo(objectId));
            Assert.That(identifier.ValidateObjectIdFromValue(
                objectId.AsSpan()), Is.True);
            Assert.That(identifier.ObjectIdUnixSecondsFromValue(
                objectId.AsSpan()),
                Is.EqualTo(identifier
                    .ObjectIdUnixSecondsFromValueUsingReadOnlySpanOfByte(
                        objectIdBytes)));
            Assert.That(identifier.FormatSortableTimestampIdFromValue(
                sortableBytes), Is.EqualTo(sortable));
            Assert.That(identifier.ValidateSortableTimestampIdFromValue(
                sortable.AsSpan()), Is.True);
            Assert.That(identifier
                .SortableTimestampIdUnixMillisecondsFromValue(
                    sortable.AsSpan()),
                Is.EqualTo(identifier
                    .SortableTimestampIdUnixMillisecondsFromValueUsingReadOnlySpanOfByte(
                        sortableBytes)));
        });
    }

    [Test]
    public void StandardsSpanAliasesValidateGeneratedValues()
    {
        IdentifierProvider identifier = Fake.Create(15117).Identifier();
        char[] doiDestination = new char[20];
        char[] gtinDestination = new char[13];
        int doiWritten = identifier
            .WriteDoiFromDestinationAndRegistrantDigitsAndSuffixLength(
                doiDestination,
                5,
                7);
        int gtinWritten = identifier.WriteGtinFromDestinationAndLength(
            gtinDestination,
            13);
        string doi = new string(doiDestination, 0, doiWritten);
        string gtin = new string(gtinDestination, 0, gtinWritten);
        string ean = identifier.Ean8();
        string upc = identifier.UpcA();
        string imei = identifier.Imei();
        string imeiSv = identifier.ImeiSv();
        string vin = identifier.Vin();

        Assert.Multiple(() =>
        {
            Assert.That(identifier.ValidateDoiFromValue(doi.AsSpan()),
                Is.True);
            Assert.That(gtinWritten, Is.EqualTo(13));
            Assert.That(identifier.ValidateGtinFromValue(gtin.AsSpan()),
                Is.True);
            Assert.That(identifier.ValidateEan8FromValue(ean.AsSpan()),
                Is.True);
            Assert.That(identifier.ValidateUpcAFromValue(upc.AsSpan()),
                Is.True);
            Assert.That(identifier.ValidateImeiFromValue(imei.AsSpan()),
                Is.True);
            Assert.That(identifier.ValidateImeiSvFromValue(imeiSv.AsSpan()),
                Is.True);
            Assert.That(identifier.ValidateVinFromValue(vin.AsSpan()),
                Is.True);
        });
    }
}
