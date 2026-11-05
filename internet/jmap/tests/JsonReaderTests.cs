using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class JsonReaderTests
{
    [Test]
    public void SpanReadingReturnsDetachedJsonAndPreservesJsonValueKinds()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes("{\"missingIsDifferent\":null,\"enabled\":false,\"count\":0,\"items\":[]}");
        JmapJsonReader reader = new JmapJsonReader(utf8.Length, 8);

        JsonElement result = reader.ReadUtf8Span(utf8);
        Array.Fill(utf8, (byte)32);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.GetProperty("missingIsDifferent").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(result.GetProperty("enabled").GetBoolean(), Is.False);
            Assert.That(result.GetProperty("count").GetInt32(), Is.Zero);
            Assert.That(result.GetProperty("items").GetArrayLength(), Is.Zero);
            Assert.That(result.TryGetProperty("missing", out JsonElement _), Is.False);
        }
    }

    [Test]
    public void MemoryReadingAcceptsUnicodeAndProducesDetachedJson()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes("{\"greeting\":\"Grüße 👋\"}");
        JmapJsonReader reader = new JmapJsonReader(1024, 8);

        JsonElement result = reader.ReadUtf8Memory(utf8.AsMemory());
        Array.Fill(utf8, (byte)32);

        Assert.That(result.GetProperty("greeting").GetString(), Is.EqualTo("Grüße 👋"));
    }

    [Test]
    public void StreamReadingHandlesFragmentedInputWithoutClosingTheStream()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes("{\"using\":[\"urn:ietf:params:jmap:core\"],\"methodCalls\":[]}");
        FragmentedReadStream stream = new FragmentedReadStream(utf8, 1);
        JmapJsonReader reader = new JmapJsonReader(1024, 8);

        JsonElement result = reader.ReadUtf8Stream(stream, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.GetProperty("using")[0].GetString(), Is.EqualTo(JmapCapabilities.Core));
            Assert.That(result.GetProperty("methodCalls").GetArrayLength(), Is.Zero);
            Assert.That(stream.CanRead, Is.True);
            Assert.That(stream.ReadByte(), Is.EqualTo(-1));
        }
    }

    [Test]
    public void EveryInputFormEnforcesTheConfiguredByteLimit()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes("{\"tooLarge\":true}");
        JmapJsonReader reader = new JmapJsonReader(4, 8);
        using MemoryStream stream = new MemoryStream(utf8);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => reader.ReadUtf8Span(utf8), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => reader.ReadUtf8Memory(utf8.AsMemory()), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => reader.ReadUtf8Stream(stream, CancellationToken.None), Throws.TypeOf<JmapProtocolException>());
            Assert.That(stream.Position, Is.EqualTo(5));
        }
    }

    [TestCase("{\"value\":1,\"value\":2}", "value")]
    [TestCase("{\"outer\":{\"same\":1,\"same\":2}}", "same")]
    [TestCase("{\"name\":1,\"\\u006eame\":2}", "name")]
    public void DuplicateObjectMembersAreRejectedAfterEscapeDecoding(string json, string duplicateName)
    {
        JmapJsonReader reader = new JmapJsonReader(1024, 8);

        JmapProtocolException exception = Assert.Throws<JmapProtocolException>(
            () => reader.ReadUtf8Memory(Encoding.UTF8.GetBytes(json)))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.Message, Does.Contain("duplicate"));
            Assert.That(exception.Path, Is.EqualTo(duplicateName));
        }
    }

    [TestCase("9007199254740991")]
    [TestCase("-9007199254740991")]
    [TestCase("90071992547409910e-1")]
    [TestCase("9.007199254740991e15")]
    public void SafeIntegerBoundaryIsAcceptedInEveryJsonNumberForm(string number)
    {
        JmapJsonReader reader = new JmapJsonReader(1024, 8);

        JsonElement result = reader.ReadUtf8Memory(Encoding.UTF8.GetBytes("{\"number\":" + number + "}"));

        Assert.That(result.GetProperty("number").GetRawText(), Is.EqualTo(number));
    }

    [TestCase("9007199254740992")]
    [TestCase("-9007199254740992")]
    [TestCase("90071992547409920e-1")]
    [TestCase("9.007199254740992e15")]
    [TestCase("1e400")]
    public void UnsafeIntegersAreRejectedInEveryJsonNumberForm(string number)
    {
        JmapJsonReader reader = new JmapJsonReader(1024, 8);
        byte[] utf8 = Encoding.UTF8.GetBytes("{\"number\":" + number + "}");

        Assert.That(() => reader.ReadUtf8Memory(utf8), Throws.TypeOf<JmapProtocolException>());
    }

    [Test]
    public void NestingAtTheConfiguredDepthIsAcceptedAndDeeperJsonIsRejected()
    {
        JmapJsonReader reader = new JmapJsonReader(1024, 2);

        JsonElement accepted = reader.ReadUtf8Memory(Encoding.UTF8.GetBytes("{\"nested\":{\"value\":1}}"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(accepted.GetProperty("nested").GetProperty("value").GetInt32(), Is.EqualTo(1));
            Assert.That(
                () => reader.ReadUtf8Memory(Encoding.UTF8.GetBytes("{\"nested\":{\"deeper\":{\"value\":1}}}")),
                Throws.InstanceOf<JsonException>());
        }
    }

    [Test]
    public void InvalidUtf8AndIJsonNoncharactersAreRejected()
    {
        JmapJsonReader reader = new JmapJsonReader(1024, 8);
        byte[] invalidUtf8 = AdversarialFixtures.InvalidUtf8Json();
        byte[] escapedNoncharacter = Encoding.UTF8.GetBytes("\"\\uFDD0\"");
        byte[] directNoncharacter = Encoding.UTF8.GetBytes("\"﷐\"");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => reader.ReadUtf8Memory(invalidUtf8), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => reader.ReadUtf8Memory(escapedNoncharacter), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => reader.ReadUtf8Memory(directNoncharacter), Throws.TypeOf<JmapProtocolException>());
        }
    }

    [Test]
    public void EscapedUnicodeRequiresPairedSurrogates()
    {
        JmapJsonReader reader = new JmapJsonReader(1024, 8);
        byte[] highOnly = Encoding.UTF8.GetBytes("\"\\uD800\"");
        byte[] lowOnly = Encoding.UTF8.GetBytes("\"\\uDEAD\"");
        byte[] validPair = Encoding.UTF8.GetBytes("\"\\uD800\\uDEAD\"");

        JsonElement accepted = reader.ReadUtf8Memory(validPair);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => reader.ReadUtf8Memory(highOnly), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => reader.ReadUtf8Memory(lowOnly), Throws.TypeOf<JmapProtocolException>());
            Assert.That(accepted.GetString(), Is.EqualTo("𐊭"));
        }
    }

    [Test]
    public void StreamReadingHonorsCancellationBeforeConsumingInput()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes("{}");
        using MemoryStream stream = new MemoryStream(utf8);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        JmapJsonReader reader = new JmapJsonReader(1024, 8);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => reader.ReadUtf8Stream(stream, cancellation.Token), Throws.TypeOf<OperationCanceledException>());
            Assert.That(stream.Position, Is.Zero);
        }
    }

    [Test]
    public void UnknownValueCloneSurvivesItsSourceDocument()
    {
        JmapJsonReader reader = new JmapJsonReader(1024, 8);
        JsonElement clone;

        using (JsonDocument document = JsonDocument.Parse("{\"vendor\":{\"code\":17}}"))
        {
            clone = reader.CloneUnknownValue(document.RootElement.GetProperty("vendor"));
        }

        Assert.That(clone.GetProperty("code").GetInt32(), Is.EqualTo(17));
    }
}

public class FragmentedReadStream : MemoryStream
{
    public int MaximumReadSize;

    public FragmentedReadStream(byte[] buffer, int maximumReadSize) : base(buffer)
    {
        MaximumReadSize = maximumReadSize;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return base.Read(buffer, offset, Math.Min(count, MaximumReadSize));
    }
}
