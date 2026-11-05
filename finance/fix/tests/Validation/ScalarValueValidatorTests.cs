using System.Collections.Generic;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class ScalarValueValidatorTests
{
    [Test]
    public void EverySupportedScalarDefinitionAcceptsAValidWireValue()
    {
        FixFieldType[] types =
        [
            FixFieldType.Int,
            FixFieldType.SeqNum,
            FixFieldType.NumInGroup,
            FixFieldType.Length,
            FixFieldType.TagNum,
            FixFieldType.Float,
            FixFieldType.Qty,
            FixFieldType.Price,
            FixFieldType.PriceOffset,
            FixFieldType.Amt,
            FixFieldType.Percentage,
            FixFieldType.Char,
            FixFieldType.Boolean,
            FixFieldType.String,
            FixFieldType.MultipleCharValue,
            FixFieldType.MultipleStringValue,
            FixFieldType.Currency,
            FixFieldType.Country,
            FixFieldType.Exchange,
            FixFieldType.MonthYear,
            FixFieldType.DayOfMonth,
            FixFieldType.LocalMktDate,
            FixFieldType.UtcDateOnly,
            FixFieldType.UtcTimeOnly,
            FixFieldType.UtcTimestamp,
            FixFieldType.TzTimeOnly,
            FixFieldType.TzTimestamp,
            FixFieldType.Data,
            FixFieldType.XmlData
        ];
        byte[][] values =
        [
            Bytes("-2147483648"),
            Bytes("1"),
            Bytes("0"),
            Bytes("1"),
            Bytes("35"),
            Bytes("-12.50"),
            Bytes("12.50"),
            Bytes("12.50"),
            Bytes("-0.50"),
            Bytes("12.50"),
            Bytes("99.99"),
            Bytes("A"),
            Bytes("Y"),
            Encoding.Latin1.GetBytes("café"),
            Bytes("A 1"),
            Bytes("ALPHA beta"),
            Bytes("USD"),
            Bytes("US"),
            Bytes("XNAS"),
            Bytes("202608W5"),
            Bytes("30"),
            Bytes("20260830"),
            Bytes("20260830"),
            Bytes("12:34:56.123456789012"),
            Bytes("20260830-12:34:56.123456789012"),
            Bytes("12:34:56-05:30"),
            Bytes("20260830-12:34:56.123456-05:30"),
            [0, 1, 255],
            Bytes("<x/>")
        ];
        FixDictionary dictionary = new FixDictionary();
        FixMessage message = new FixMessage { MsgType = "D" };
        for (int index = 0; index < types.Length; index++)
        {
            int tag = 1000 + index;
            dictionary.Fields.Add(Definition(tag, types[index]));
            message.BodyFields.Add(Field(tag, values[index], index));
        }

        ValidationResult result = Validate(message, dictionary, ValidationLayer.Datatype, new ValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(types.Length, Is.EqualTo(29));
            Assert.That(result.IsValid, Is.True, string.Join(",", result.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag)));
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void LexicalRangeTimestampAndUnknownDatatypeFailuresAreDistinguished()
    {
        FixDictionary dictionary = new FixDictionary
        {
            Fields =
            [
                Definition(2000, FixFieldType.Int),
                Definition(2001, FixFieldType.Boolean),
                Definition(2002, FixFieldType.UtcDateOnly),
                Definition(2003, FixFieldType.Unknown)
            ]
        };
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            BodyFields =
            [
                Field(2000, Bytes("2147483648"), 0, 10),
                Field(2001, Bytes("x"), 1, 30),
                Field(2002, Bytes("20230229"), 2, 40),
                Field(2003, Bytes("value"), 3, 60)
            ]
        };

        ValidationResult result = Validate(message, dictionary, ValidationLayer.Datatype, new ValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[]
            {
                "field-value-out-of-range:2000",
                "invalid-datatype-value:2001",
                "invalid-timestamp-value:2002",
                "unsupported-field-datatype:2003"
            }));
            Assert.That(result.Issues[1].HasSourceOffset, Is.True);
            Assert.That(result.Issues[1].SourceOffset, Is.EqualTo(35));
            Assert.That(result.Issues[2].Message, Does.Contain("invalid-calendar-date"));
            Assert.That(result.Issues[0].SessionRejectReason, Is.EqualTo(5));
            Assert.That(result.Issues[1].SessionRejectReason, Is.EqualTo(6));
            Assert.That(result.Issues[2].SessionRejectReason, Is.EqualTo(6));
            Assert.That(result.Issues[3].HasSessionRejectReason, Is.False);
        });
    }

    [Test]
    public void TemporalFormatsEnforceCalendarClockPrecisionAndTimezoneRules()
    {
        FixDictionary dictionary = new FixDictionary
        {
            Fields =
            [
                Definition(2100, FixFieldType.MonthYear),
                Definition(2101, FixFieldType.DayOfMonth),
                Definition(2102, FixFieldType.UtcTimeOnly),
                Definition(2103, FixFieldType.UtcTimestamp),
                Definition(2104, FixFieldType.TzTimeOnly),
                Definition(2105, FixFieldType.TzTimestamp)
            ]
        };
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            BodyFields =
            [
                Field(2100, Bytes("202613"), 0),
                Field(2101, Bytes("0"), 1),
                Field(2102, Bytes("24:00:00"), 2),
                Field(2103, Bytes("20260630-12:34:60"), 3),
                Field(2104, Bytes("12:34+00"), 4),
                Field(2105, Bytes("20260830-12:34:56.12Z"), 5)
            ]
        };

        ValidationResult result = Validate(message, dictionary, ValidationLayer.Datatype, new ValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "invalid-timestamp-value",
                "invalid-timestamp-value",
                "invalid-timestamp-value",
                "invalid-timestamp-value",
                "invalid-timestamp-value",
                "invalid-timestamp-value"
            }));
            Assert.That(result.Issues.TrueForAll(issue => issue.SessionRejectReason == 6), Is.True);
            Assert.That(result.Issues.ConvertAll(issue => issue.Tag), Is.EqualTo(new[] { 2100, 2101, 2102, 2103, 2104, 2105 }));
        });
    }

    [Test]
    public void EmptyValuePolicyAppliesToTextTemporalAndBinaryDefinitions()
    {
        FixDictionary dictionary = new FixDictionary
        {
            Fields =
            [
                Definition(3000, FixFieldType.String),
                Definition(3001, FixFieldType.UtcTimestamp),
                Definition(3002, FixFieldType.Data)
            ]
        };
        FixMessage message = new FixMessage
        {
            BodyFields =
            [
                Field(3000, [], 0),
                Field(3001, [], 1),
                Field(3002, [], 2)
            ]
        };

        ValidationResult strict = Validate(message, dictionary, ValidationLayer.Datatype, new ValidationOptions());
        ValidationResult relaxed = Validate(message, dictionary, ValidationLayer.Datatype, new ValidationOptions { RejectEmptyValues = false });

        Assert.Multiple(() =>
        {
            Assert.That(strict.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "empty-field-value",
                "empty-field-value",
                "empty-field-value"
            }));
            Assert.That(relaxed.IsValid, Is.True);
            Assert.That(relaxed.Issues, Is.Empty);
            Assert.That(strict.Issues.TrueForAll(issue => issue.SessionRejectReason == 4), Is.True);
        });
    }

    [Test]
    public void CodeSetsValidateExactValuesAndEveryMultipleValueToken()
    {
        FixCodeSetDefinition sideCodes = new FixCodeSetDefinition
        {
            Name = "SideCodes",
            Codes =
            [
                new FixCodeDefinition { Value = "1" },
                new FixCodeDefinition { Value = "2" },
                new FixCodeDefinition { Value = "3", IsRemoved = true }
            ]
        };
        FixDictionary dictionary = new FixDictionary
        {
            CodeSets = [sideCodes],
            Fields =
            [
                CodeDefinition(4000, FixFieldType.Char, "sidecodes"),
                CodeDefinition(4001, FixFieldType.MultipleCharValue, "SideCodes"),
                CodeDefinition(4002, FixFieldType.MultipleCharValue, "SideCodes"),
                CodeDefinition(4003, FixFieldType.Char, "MissingCodes"),
                CodeDefinition(4004, FixFieldType.Boolean, "SideCodes")
            ]
        };
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            BodyFields =
            [
                Field(4000, Bytes("1"), 0),
                Field(4001, Bytes("1 2"), 1),
                Field(4002, Bytes("1 3"), 2, 50),
                Field(4003, Bytes("1"), 3),
                Field(4004, Bytes("x"), 4)
            ]
        };

        ValidationResult result = Validate(message, dictionary, ValidationLayer.Datatype | ValidationLayer.CodeSet, new ValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[]
            {
                "invalid-datatype-value:4004",
                "invalid-code-set-value:4002",
                "unresolved-code-set:4003"
            }));
            Assert.That(result.Issues[1].SourceOffset, Is.EqualTo(57));
            Assert.That(result.Issues.Exists(issue => issue.Tag == 4000), Is.False);
            Assert.That(result.Issues.Exists(issue => issue.Tag == 4001), Is.False);
            Assert.That(result.Issues[0].SessionRejectReason, Is.EqualTo(6));
            Assert.That(result.Issues[1].SessionRejectReason, Is.EqualTo(5));
            Assert.That(result.Issues[2].HasSessionRejectReason, Is.False);
        });
    }

    [Test]
    public void LengthDataPairsRequireAdjacencyAndExactByteCounts()
    {
        FixDictionary dictionary = new FixDictionary
        {
            Fields =
            [
                LengthDefinition(95, 96),
                DataDefinition(96, 95),
                LengthDefinition(212, 213),
                DataDefinition(213, 212),
                LengthDefinition(300, 302),
                DataDefinition(301, 299)
            ]
        };
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            BodyFields =
            [
                Field(95, Bytes("3"), 0),
                Field(96, [65, 1, 66], 1),
                Field(212, Bytes("2"), 2),
                Field(213, [65, 1, 66], 3),
                Field(300, Bytes("1"), 4),
                Field(301, [65], 5)
            ]
        };

        ValidationResult result = Validate(message, dictionary, ValidationLayer.Datatype, new ValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[]
            {
                "data-length-mismatch:213",
                "length-data-not-adjacent:300",
                "length-data-not-adjacent:301"
            }));
            Assert.That(result.Issues.Exists(issue => issue.Tag == 95 || issue.Tag == 96), Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 5, 14, 14 }));
        });
    }

    private static ValidationResult Validate(FixMessage message, FixDictionary dictionary, ValidationLayer layers, ValidationOptions options)
    {
        options.Layers = layers;
        return new FixMessageValidator().ValidateMessage(message, dictionary, options, new ValidationContext());
    }

    private static FixFieldDefinition Definition(int tag, FixFieldType type)
    {
        return new FixFieldDefinition { Tag = tag, Name = "Field" + tag, FieldType = type };
    }

    private static FixFieldDefinition CodeDefinition(int tag, FixFieldType type, string codeSetName)
    {
        FixFieldDefinition definition = Definition(tag, type);
        definition.CodeSetName = codeSetName;
        return definition;
    }

    private static FixFieldDefinition LengthDefinition(int tag, int dataTag)
    {
        FixFieldDefinition definition = Definition(tag, FixFieldType.Length);
        definition.HasDataTag = true;
        definition.DataTag = dataTag;
        return definition;
    }

    private static FixFieldDefinition DataDefinition(int tag, int lengthTag)
    {
        FixFieldDefinition definition = Definition(tag, FixFieldType.Data);
        definition.HasLengthTag = true;
        definition.LengthTag = lengthTag;
        return definition;
    }

    private static FixField Field(int tag, byte[] value, int wireIndex)
    {
        return Field(tag, value, wireIndex, -1);
    }

    private static FixField Field(int tag, byte[] value, int wireIndex, int sourceOffset)
    {
        return new FixField { Tag = tag, Value = value, WireIndex = wireIndex, SourceOffset = sourceOffset };
    }

    private static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}
