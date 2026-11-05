using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class JsonFieldReaderTests
{
    [Test]
    public void StringReadsDistinguishMissingNullAndInvalidValues()
    {
        using JsonDocument document = JsonDocument.Parse("{\"text\":\"value\",\"number\":12,\"flag\":true,\"none\":null,\"wrong\":{\"x\":1}}");
        JsonFieldReader reader = new JsonFieldReader(document.RootElement, "$");

        JsonFieldValue text = reader.ReadString("text");
        JsonFieldValue number = reader.ReadString("number");
        JsonFieldValue flag = reader.ReadString("flag");
        JsonFieldValue missing = reader.ReadString("missing");
        JsonFieldValue none = reader.ReadString("none");
        JsonFieldValue wrong = reader.ReadString("wrong");

        Assert.Multiple(() =>
        {
            Assert.That(text.IsPresent, Is.True);
            Assert.That(text.IsNull, Is.False);
            Assert.That(text.IsValid, Is.True);
            Assert.That(text.StringValue, Is.EqualTo("value"));
            Assert.That(text.Path, Is.EqualTo("$.text"));
            Assert.That(text.RawJson, Is.EqualTo("\"value\""));
            Assert.That(number.StringValue, Is.EqualTo("12"));
            Assert.That(flag.StringValue, Is.EqualTo("true"));
            Assert.That(missing.IsPresent, Is.False);
            Assert.That(missing.IsNull, Is.False);
            Assert.That(missing.IsValid, Is.True);
            Assert.That(missing.Path, Is.EqualTo("$.missing"));
            Assert.That(none.IsPresent, Is.True);
            Assert.That(none.IsNull, Is.True);
            Assert.That(none.IsValid, Is.True);
            Assert.That(wrong.IsPresent, Is.True);
            Assert.That(wrong.IsNull, Is.False);
            Assert.That(wrong.IsValid, Is.False);
        });
    }

    [Test]
    public void BooleanReadsAcceptBooleanStringsAndZeroOrOne()
    {
        using JsonDocument document = JsonDocument.Parse("{\"yes\":true,\"no\":false,\"textYes\":\" TRUE \",\"textNo\":\"false\",\"one\":1,\"zero\":0,\"two\":2,\"other\":\"yes\"}");
        JsonFieldReader reader = new JsonFieldReader(document.RootElement, "$");

        JsonFieldValue yes = reader.ReadBoolean("yes");
        JsonFieldValue no = reader.ReadBoolean("no");
        JsonFieldValue textYes = reader.ReadBoolean("textYes");
        JsonFieldValue textNo = reader.ReadBoolean("textNo");
        JsonFieldValue one = reader.ReadBoolean("one");
        JsonFieldValue zero = reader.ReadBoolean("zero");
        JsonFieldValue two = reader.ReadBoolean("two");
        JsonFieldValue other = reader.ReadBoolean("other");

        Assert.Multiple(() =>
        {
            Assert.That(yes.IsValid && yes.BooleanValue, Is.True);
            Assert.That(no.IsValid && !no.BooleanValue, Is.True);
            Assert.That(textYes.IsValid && textYes.BooleanValue, Is.True);
            Assert.That(textNo.IsValid && !textNo.BooleanValue, Is.True);
            Assert.That(one.IsValid && one.BooleanValue, Is.True);
            Assert.That(zero.IsValid && !zero.BooleanValue, Is.True);
            Assert.That(two.IsValid, Is.False);
            Assert.That(other.IsValid, Is.False);
        });
    }

    [Test]
    public void SignedIntegerReadsRejectFractionsAndOverflow()
    {
        using JsonDocument document = JsonDocument.Parse("{\"minimum\":\"-9223372036854775808\",\"maximum\":9223372036854775807,\"positive\":\"+17\",\"negative\":-42,\"intMaximum\":2147483647,\"tooLargeForInt\":2147483648,\"positiveOverflow\":9223372036854775808,\"negativeOverflow\":\"-9223372036854775809\",\"fraction\":1.5}");
        JsonFieldReader reader = new JsonFieldReader(document.RootElement, "$");

        JsonFieldValue minimum = reader.ReadInt64("minimum");
        JsonFieldValue maximum = reader.ReadInt64("maximum");
        JsonFieldValue positive = reader.ReadInt64("positive");
        JsonFieldValue negative = reader.ReadInt64("negative");
        JsonFieldValue intMaximum = reader.ReadInt32("intMaximum");
        JsonFieldValue tooLargeForInt = reader.ReadInt32("tooLargeForInt");
        JsonFieldValue positiveOverflow = reader.ReadInt64("positiveOverflow");
        JsonFieldValue negativeOverflow = reader.ReadInt64("negativeOverflow");
        JsonFieldValue fraction = reader.ReadInt64("fraction");

        Assert.Multiple(() =>
        {
            Assert.That(minimum.IsValid, Is.True);
            Assert.That(minimum.Int64Value, Is.EqualTo(long.MinValue));
            Assert.That(maximum.IsValid, Is.True);
            Assert.That(maximum.Int64Value, Is.EqualTo(long.MaxValue));
            Assert.That(positive.Int64Value, Is.EqualTo(17L));
            Assert.That(negative.Int64Value, Is.EqualTo(-42L));
            Assert.That(intMaximum.IsValid, Is.True);
            Assert.That(intMaximum.Int32Value, Is.EqualTo(int.MaxValue));
            Assert.That(tooLargeForInt.IsValid, Is.False);
            Assert.That(positiveOverflow.IsValid, Is.False);
            Assert.That(negativeOverflow.IsValid, Is.False);
            Assert.That(fraction.IsValid, Is.False);
        });
    }

    [Test]
    public void ArraysAndObjectsRemainAvailableAfterTheDocumentIsDisposed()
    {
        JsonFieldValue array;
        JsonFieldValue child;
        using (JsonDocument document = JsonDocument.Parse("{\"items\":[1,{\"name\":\"second\"}],\"child\":{\"enabled\":true}}"))
        {
            JsonFieldReader reader = new JsonFieldReader(document.RootElement, "$");
            array = reader.ReadArray("items");
            child = reader.ReadObject("child");
        }

        Assert.Multiple(() =>
        {
            Assert.That(array.IsValid, Is.True);
            Assert.That(array.ArrayItems, Has.Length.EqualTo(2));
            Assert.That(array.ArrayItems[0].GetInt32(), Is.EqualTo(1));
            Assert.That(array.ArrayItems[1].GetProperty("name").GetString(), Is.EqualTo("second"));
            Assert.That(array.ArrayItemPaths, Is.EqualTo(new string[] { "$.items[0]", "$.items[1]" }));
            Assert.That(child.IsValid, Is.True);
            Assert.That(child.HasElementValue, Is.True);
            Assert.That(child.ElementValue.GetProperty("enabled").GetBoolean(), Is.True);
        });
    }

    [Test]
    public void NestedReadersCreatePreciseEscapedPaths()
    {
        using JsonDocument document = JsonDocument.Parse("{\"a.b\":{\"quote'name\":7,\"1name\":8}}");
        JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
        JsonFieldValue parent = root.ReadObject("a.b");
        JsonFieldReader child = new JsonFieldReader(parent.ElementValue, parent.Path);

        JsonFieldValue quoted = child.ReadInt32("quote'name");
        JsonFieldValue numbered = child.ReadInt32("1name");

        Assert.Multiple(() =>
        {
            Assert.That(parent.Path, Is.EqualTo("$['a.b']"));
            Assert.That(quoted.Path, Is.EqualTo("$['a.b']['quote\\'name']"));
            Assert.That(quoted.Int32Value, Is.EqualTo(7));
            Assert.That(numbered.Path, Is.EqualTo("$['a.b']['1name']"));
            Assert.That(numbered.Int32Value, Is.EqualTo(8));
        });
    }

    [Test]
    public void FlexibleDatesAcceptCalendarTimestampAndEpochForms()
    {
        using JsonDocument document = JsonDocument.Parse("{\"iso\":\"2026-09-02\",\"compact\":\"20260902\",\"us\":\"09/02/2026\",\"timestamp\":\"2026-09-02T12:34:56.1234567Z\",\"offset\":\"2026-09-02 12:34:56-07:00\",\"epochSeconds\":0,\"epochText\":\"1767225600\",\"epochMilliseconds\":1767225600000}");
        JsonFieldReader reader = new JsonFieldReader(document.RootElement, "$");

        JsonFieldValue iso = reader.ReadDate("iso");
        JsonFieldValue compact = reader.ReadDate("compact");
        JsonFieldValue us = reader.ReadDate("us");
        JsonFieldValue timestamp = reader.ReadDate("timestamp");
        JsonFieldValue offset = reader.ReadDate("offset");
        JsonFieldValue epochSeconds = reader.ReadDate("epochSeconds");
        JsonFieldValue epochText = reader.ReadDate("epochText");
        JsonFieldValue epochMilliseconds = reader.ReadDate("epochMilliseconds");

        DateTimeOffset expectedDate = new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset expectedEpoch = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Multiple(() =>
        {
            Assert.That(iso.IsValid, Is.True);
            Assert.That(iso.DateValue, Is.EqualTo(expectedDate));
            Assert.That(compact.DateValue, Is.EqualTo(expectedDate));
            Assert.That(us.DateValue, Is.EqualTo(expectedDate));
            Assert.That(timestamp.IsValid, Is.True);
            Assert.That(timestamp.DateValue.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(1234567L));
            Assert.That(offset.IsValid, Is.True);
            Assert.That(offset.DateValue.Offset, Is.EqualTo(TimeSpan.FromHours(-7)));
            Assert.That(epochSeconds.DateValue, Is.EqualTo(DateTimeOffset.UnixEpoch));
            Assert.That(epochText.DateValue, Is.EqualTo(expectedEpoch));
            Assert.That(epochMilliseconds.DateValue, Is.EqualTo(expectedEpoch));
        });
    }

    [Test]
    public void InvalidDatesAreReportedWithoutThrowing()
    {
        using JsonDocument document = JsonDocument.Parse("{\"notLeap\":\"2025-02-29\",\"hour\":\"2026-09-02T24:00:00Z\",\"fraction\":\"2026-09-02T12:00:00.12345678Z\",\"offset\":\"2026-09-02T12:00:00+14:01\",\"nonsense\":\"later\",\"epoch\":999999999999999999}");
        JsonFieldReader reader = new JsonFieldReader(document.RootElement, "$");

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadDate("notLeap").IsValid, Is.False);
            Assert.That(reader.ReadDate("hour").IsValid, Is.False);
            Assert.That(reader.ReadDate("fraction").IsValid, Is.False);
            Assert.That(reader.ReadDate("offset").IsValid, Is.False);
            Assert.That(reader.ReadDate("nonsense").IsValid, Is.False);
            Assert.That(reader.ReadDate("epoch").IsValid, Is.False);
        });
    }

    [Test]
    public void UnknownFieldsPreserveRawValuesAndPathsAfterDisposal()
    {
        JsonUnknownField[] unknown;
        using (JsonDocument document = JsonDocument.Parse("{\"known\":1,\"extra.field\":{\"value\":2},\"hyphen-name\":[true]}"))
        {
            JsonFieldReader reader = new JsonFieldReader(document.RootElement, "$");
            unknown = reader.CaptureUnknownFields(new string[] { "known" });
        }

        Assert.Multiple(() =>
        {
            Assert.That(unknown, Has.Length.EqualTo(2));
            Assert.That(unknown[0].Name, Is.EqualTo("extra.field"));
            Assert.That(unknown[0].Path, Is.EqualTo("$['extra.field']"));
            Assert.That(unknown[0].RawJson, Is.EqualTo("{\"value\":2}"));
            Assert.That(unknown[0].Value.GetProperty("value").GetInt32(), Is.EqualTo(2));
            Assert.That(unknown[1].Name, Is.EqualTo("hyphen-name"));
            Assert.That(unknown[1].Path, Is.EqualTo("$['hyphen-name']"));
            Assert.That(unknown[1].Value[0].GetBoolean(), Is.True);
        });
    }

    [Test]
    public void RawReadsPreserveAnyKindAndUseTheLastDuplicateProperty()
    {
        JsonFieldValue raw;
        using (JsonDocument document = JsonDocument.Parse("{\"value\":1,\"value\":{\"nested\":true},\"none\":null}"))
        {
            JsonFieldReader reader = new JsonFieldReader(document.RootElement, "$");
            raw = reader.ReadRaw("value");
            Assert.That(reader.ReadRaw("missing").IsValid, Is.True);
            Assert.That(reader.ReadRaw("none").IsNull, Is.True);
        }

        Assert.Multiple(() =>
        {
            Assert.That(raw.IsPresent, Is.True);
            Assert.That(raw.IsValid, Is.True);
            Assert.That(raw.Path, Is.EqualTo("$.value"));
            Assert.That(raw.RawJson, Is.EqualTo("{\"nested\":true}"));
            Assert.That(raw.ElementValue.GetProperty("nested").GetBoolean(), Is.True);
        });
    }

    [Test]
    public void ReadsAgainstANonObjectContainerAreInvalid()
    {
        using JsonDocument document = JsonDocument.Parse("[1,2]");
        JsonFieldReader reader = new JsonFieldReader(document.RootElement, "$.items");

        JsonFieldValue value = reader.ReadString("name");
        JsonFieldValue raw = reader.ReadRaw("name");

        Assert.Multiple(() =>
        {
            Assert.That(value.IsPresent, Is.False);
            Assert.That(value.IsValid, Is.False);
            Assert.That(value.Path, Is.EqualTo("$.items.name"));
            Assert.That(raw.IsPresent, Is.False);
            Assert.That(raw.IsValid, Is.False);
        });
    }
}
