using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class JsonWriterTests
{
    [Test]
    public void ManualWritingEscapesNamesAndValuesWithoutChangingTheirText()
    {
        byte[] utf8 = Write(writer =>
        {
            writer.WriteObjectStart();
            writer.WriteMemberName("quoted\"name");
            writer.WriteStringValue("line one\nline two \\ café 👋");
            writer.WriteObjectEnd();
        });
        string json = Encoding.UTF8.GetString(utf8);

        using JsonDocument document = JsonDocument.Parse(utf8);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(document.RootElement.GetProperty("quoted\"name").GetString(), Is.EqualTo("line one\nline two \\ café 👋"));
            Assert.That(json, Does.Contain("\\n"));
            Assert.That(json, Does.Contain("\\\\"));
            Assert.That(json, Does.Not.Contain("line one\nline two"));
        }
    }

    [Test]
    public void ManualStructureMethodsPreserveArrayOrder()
    {
        byte[] utf8 = Write(writer =>
        {
            writer.WriteObjectStart();
            writer.WriteMemberName("values");
            writer.WriteArrayStart();
            writer.WriteStringValue("first");
            writer.WriteIntegerValue(2);
            writer.WriteBooleanValue(true);
            writer.WriteNullValue();
            writer.WriteArrayEnd();
            writer.WriteObjectEnd();
        });

        Assert.That(Encoding.UTF8.GetString(utf8), Is.EqualTo("{\"values\":[\"first\",2,true,null]}"));
    }

    [Test]
    public void MemberHelpersDistinguishOmittedDefaultsFromExplicitValuesAndNull()
    {
        JsonElement undefined = default;
        JsonElement explicitNull = Json("null");

        byte[] utf8 = Write(writer =>
        {
            writer.WriteObjectStart();
            writer.WriteStringMember("omittedString", null, false);
            writer.WriteStringMember("explicitNull", null, true);
            writer.WriteBooleanMember("omittedFalse", false, false);
            writer.WriteBooleanMember("falseValue", false, true);
            writer.WriteIntegerMember("omittedZero", 0, false);
            writer.WriteIntegerMember("zeroValue", 0, true);
            writer.WriteElementMember("omittedUndefined", undefined);
            writer.WriteElementMember("jsonNull", explicitNull);
            writer.WriteNullMember("directNull");
            writer.WriteObjectEnd();
        });

        Assert.That(
            Encoding.UTF8.GetString(utf8),
            Is.EqualTo("{\"explicitNull\":null,\"falseValue\":false,\"zeroValue\":0,\"jsonNull\":null,\"directNull\":null}"));
    }

    [Test]
    public void ArbitraryJsonElementsUseDeterministicOrdinalMemberOrderRecursively()
    {
        JsonElement element = Json("{\"z\":1,\"a\":{\"b\":\"two\",\"a\":null},\"m\":[{\"z\":0,\"a\":false},3,2,1]}");

        byte[] first = Write(writer => writer.WriteElementValue(element));
        byte[] second = Write(writer => writer.WriteElementValue(element));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(
                Encoding.UTF8.GetString(first),
                Is.EqualTo("{\"a\":{\"a\":null,\"b\":\"two\"},\"m\":[{\"a\":false,\"z\":0},3,2,1],\"z\":1}"));
        }
    }

    [Test]
    public void SafeIntegerBoundariesAreWrittenAsExactDecimalValues()
    {
        byte[] utf8 = Write(writer =>
        {
            writer.WriteArrayStart();
            writer.WriteIntegerValue(JmapProtocolLimits.MinimumInt);
            writer.WriteIntegerValue(JmapProtocolLimits.MaximumInt);
            writer.WriteArrayEnd();
        });

        Assert.That(Encoding.UTF8.GetString(utf8), Is.EqualTo("[-9007199254740991,9007199254740991]"));
    }

    [Test]
    public void UnsafeIntegersAreRejectedFromDirectAndArbitraryJsonPaths()
    {
        using MemoryStream directStream = new MemoryStream();
        JmapJsonWriter directWriter = new JmapJsonWriter(directStream);
        directWriter.WriteArrayStart();

        using MemoryStream memberStream = new MemoryStream();
        JmapJsonWriter memberWriter = new JmapJsonWriter(memberStream);
        memberWriter.WriteObjectStart();

        JsonElement unsafeElement = Json("9.007199254740992e15");
        using MemoryStream elementStream = new MemoryStream();
        JmapJsonWriter elementWriter = new JmapJsonWriter(elementStream);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => directWriter.WriteIntegerValue(JmapProtocolLimits.MaximumInt + 1L),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(
                () => memberWriter.WriteIntegerMember("count", JmapProtocolLimits.MinimumInt - 1L, true),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(
                () => elementWriter.WriteElementValue(unsafeElement),
                Throws.TypeOf<JmapProtocolException>());
        }

        directWriter.DisposeWriter();
        memberWriter.DisposeWriter();
        elementWriter.DisposeWriter();
    }

    [Test]
    public void UndefinedElementValuesAndInvalidUnicodeAreRejected()
    {
        string unpairedSurrogate = new string(new[] { '\uD800' });
        string noncharacter = new string(new[] { '\uFDD0' });

        using MemoryStream undefinedStream = new MemoryStream();
        JmapJsonWriter undefinedWriter = new JmapJsonWriter(undefinedStream);
        using MemoryStream surrogateStream = new MemoryStream();
        JmapJsonWriter surrogateWriter = new JmapJsonWriter(surrogateStream);
        using MemoryStream noncharacterStream = new MemoryStream();
        JmapJsonWriter noncharacterWriter = new JmapJsonWriter(noncharacterStream);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => undefinedWriter.WriteElementValue(default),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(
                () => surrogateWriter.WriteStringValue(unpairedSurrogate),
                Throws.TypeOf<JmapValidationException>());
            Assert.That(
                () => noncharacterWriter.WriteStringValue(noncharacter),
                Throws.TypeOf<JmapValidationException>());
        }

        undefinedWriter.DisposeWriter();
        surrogateWriter.DisposeWriter();
        noncharacterWriter.DisposeWriter();
    }

    [Test]
    public void JsonElementWithDuplicateMembersIsRejectedBeforeWriting()
    {
        JsonElement duplicate = Json("{\"same\":1,\"same\":2}");
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);

        Assert.That(() => writer.WriteElementValue(duplicate), Throws.TypeOf<JmapProtocolException>());

        writer.DisposeWriter();
        Assert.That(stream.Length, Is.Zero);
    }

    [Test]
    public void FlushAndDisposeLeaveTheCallerOwnedStreamOpen()
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        writer.WriteStringValue("done");

        writer.FlushWriter();
        long flushedLength = stream.Length;
        writer.DisposeWriter();
        stream.WriteByte(32);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(flushedLength, Is.GreaterThan(0));
            Assert.That(stream.CanWrite, Is.True);
            Assert.That(stream.Length, Is.EqualTo(flushedLength + 1));
        }
    }

    private static byte[] Write(Action<JmapJsonWriter> write)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        write(writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
