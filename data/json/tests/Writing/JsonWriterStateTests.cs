namespace json.Tests;

public class JsonWriterStateTests
{
    [Test]
    public void EveryRootValueShapeCanCompleteExactlyOnce()
    {
        WriterFixture scalar = new WriterFixture();
        AssertSuccess(scalar.Writer.WriteNull());
        AssertSuccess(scalar.Writer.CompleteWriter());
        AssertSuccess(scalar.Writer.CompleteWriter());

        WriterFixture array = new WriterFixture();
        AssertSuccess(array.Writer.WriteStartArray());
        AssertSuccess(array.Writer.WriteEndArray());
        AssertSuccess(array.Writer.CompleteWriter());

        WriterFixture objectFixture = new WriterFixture();
        AssertSuccess(objectFixture.Writer.WriteStartObject());
        AssertSuccess(objectFixture.Writer.WriteEndObject());
        AssertSuccess(objectFixture.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(scalar.Text(), Is.EqualTo("null"));
            Assert.That(array.Text(), Is.EqualTo("[]"));
            Assert.That(objectFixture.Text(), Is.EqualTo("{}"));
        });
    }

    [Test]
    public void ArrayAcceptsScalarArrayAndObjectValuesBeforeClosing()
    {
        WriterFixture fixture = new WriterFixture();

        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteNull());
        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.WriteBoolean(true));
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(fixture.Text(), Is.EqualTo("[null,[],{},true]"));
    }

    [Test]
    public void ObjectAlternatesNamesWithEveryValueShapeBeforeClosing()
    {
        WriterFixture fixture = new WriterFixture();

        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WritePropertyName("scalar"));
        AssertSuccess(fixture.Writer.WriteNull());
        AssertSuccess(fixture.Writer.WritePropertyName("array"));
        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteBoolean(true));
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.WritePropertyName("object"));
        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WritePropertyName("nested"));
        AssertSuccess(fixture.Writer.WriteString("value"));
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo(
                "{\"scalar\":null,\"array\":[true]," +
                "\"object\":{\"nested\":\"value\"}}"));
    }

    [Test]
    public void MultipleRootModesAcceptScalarArrayAndObjectTransitions()
    {
        WriterFixture whitespace = new WriterFixture(
            JsonInputMode.WhitespaceSeparated);
        AssertSuccess(whitespace.Writer.WriteNull());
        AssertSuccess(whitespace.Writer.WriteStartArray());
        AssertSuccess(whitespace.Writer.WriteEndArray());
        AssertSuccess(whitespace.Writer.WriteStartObject());
        AssertSuccess(whitespace.Writer.WriteEndObject());
        AssertSuccess(whitespace.Writer.CompleteWriter());

        WriterFixture sequence = new WriterFixture(
            JsonInputMode.JsonTextSequence);
        AssertSuccess(sequence.Writer.WriteNull());
        AssertSuccess(sequence.Writer.WriteStartArray());
        AssertSuccess(sequence.Writer.WriteEndArray());
        AssertSuccess(sequence.Writer.WriteStartObject());
        AssertSuccess(sequence.Writer.WriteEndObject());
        AssertSuccess(sequence.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(whitespace.Text(), Is.EqualTo("null\n[]\n{}"));
            Assert.That(
                sequence.Bytes(),
                Is.EqualTo(new byte[]
                {
                    JsonSyntaxBytes.RecordSeparator,
                    0x6E,
                    0x75,
                    0x6C,
                    0x6C,
                    JsonSyntaxBytes.LineFeed,
                    JsonSyntaxBytes.RecordSeparator,
                    JsonSyntaxBytes.BeginArray,
                    JsonSyntaxBytes.EndArray,
                    JsonSyntaxBytes.LineFeed,
                    JsonSyntaxBytes.RecordSeparator,
                    JsonSyntaxBytes.BeginObject,
                    JsonSyntaxBytes.EndObject,
                    JsonSyntaxBytes.LineFeed
                }));
        });
    }

    [Test]
    public void PropertyNamesAreRejectedOutsideAnObjectNameState()
    {
        WriterFixture root = new WriterFixture();
        AssertInvalidAndUnchanged(
            root.Writer,
            root.Writer.WritePropertyName("root"),
            0,
            0);
        AssertSuccess(root.Writer.WriteNull());
        AssertSuccess(root.Writer.CompleteWriter());

        WriterFixture array = new WriterFixture();
        AssertSuccess(array.Writer.WriteStartArray());
        AssertInvalidAndUnchanged(
            array.Writer,
            array.Writer.WritePropertyName("array"),
            1,
            0);
        AssertSuccess(array.Writer.WriteEndArray());
        AssertSuccess(array.Writer.CompleteWriter());

        WriterFixture awaitingValue = new WriterFixture();
        AssertSuccess(awaitingValue.Writer.WriteStartObject());
        AssertSuccess(awaitingValue.Writer.WritePropertyName("a"));
        AssertInvalidAndUnchanged(
            awaitingValue.Writer,
            awaitingValue.Writer.WritePropertyName("b"),
            5,
            0);
        AssertSuccess(awaitingValue.Writer.WriteNull());
        AssertSuccess(awaitingValue.Writer.WriteEndObject());
        AssertSuccess(awaitingValue.Writer.CompleteWriter());

        WriterFixture rootDone = new WriterFixture();
        AssertSuccess(rootDone.Writer.WriteNull());
        AssertInvalidAndUnchanged(
            rootDone.Writer,
            rootDone.Writer.WritePropertyName("after-root"),
            4,
            0);
        AssertSuccess(rootDone.Writer.CompleteWriter());

        WriterFixture completed = new WriterFixture();
        AssertSuccess(completed.Writer.WriteNull());
        AssertSuccess(completed.Writer.CompleteWriter());
        AssertInvalidAndUnchanged(
            completed.Writer,
            completed.Writer.WritePropertyName("completed"),
            0,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(root.Text(), Is.EqualTo("null"));
            Assert.That(array.Text(), Is.EqualTo("[]"));
            Assert.That(awaitingValue.Text(), Is.EqualTo("{\"a\":null}"));
            Assert.That(rootDone.Text(), Is.EqualTo("null"));
            Assert.That(completed.Text(), Is.EqualTo("null"));
        });
    }

    [Test]
    public void ValuesAreRejectedWhileAnObjectRequiresAName()
    {
        WriterFixture fixture = new WriterFixture();
        AssertSuccess(fixture.Writer.WriteStartObject());

        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteNull(),
            1,
            0);
        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteStartArray(),
            1,
            0);
        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteStartObject(),
            1,
            0);

        AssertSuccess(fixture.Writer.WritePropertyName("a"));
        AssertSuccess(fixture.Writer.WriteNull());

        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteBoolean(true),
            9,
            0);
        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteStartArray(),
            9,
            0);
        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteStartObject(),
            9,
            0);

        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.CompleteWriter());
        Assert.That(fixture.Text(), Is.EqualTo("{\"a\":null}"));
    }

    [Test]
    public void SingleValueRootRejectsEveryAdditionalValueShape()
    {
        WriterFixture scalar = new WriterFixture();
        AssertSuccess(scalar.Writer.WriteNull());
        AssertInvalidAndUnchanged(
            scalar.Writer,
            scalar.Writer.WriteBoolean(true),
            4,
            0);
        AssertInvalidAndUnchanged(
            scalar.Writer,
            scalar.Writer.WriteStartArray(),
            4,
            0);
        AssertInvalidAndUnchanged(
            scalar.Writer,
            scalar.Writer.WriteStartObject(),
            4,
            0);
        AssertSuccess(scalar.Writer.CompleteWriter());

        WriterFixture array = new WriterFixture();
        AssertSuccess(array.Writer.WriteStartArray());
        AssertSuccess(array.Writer.WriteEndArray());
        AssertInvalidAndUnchanged(
            array.Writer,
            array.Writer.WriteNull(),
            2,
            0);
        AssertSuccess(array.Writer.CompleteWriter());

        WriterFixture objectFixture = new WriterFixture();
        AssertSuccess(objectFixture.Writer.WriteStartObject());
        AssertSuccess(objectFixture.Writer.WriteEndObject());
        AssertInvalidAndUnchanged(
            objectFixture.Writer,
            objectFixture.Writer.WriteNull(),
            2,
            0);
        AssertSuccess(objectFixture.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(scalar.Text(), Is.EqualTo("null"));
            Assert.That(array.Text(), Is.EqualTo("[]"));
            Assert.That(objectFixture.Text(), Is.EqualTo("{}"));
        });
    }

    [Test]
    public void CompletedWriterRejectsEveryWriteShapeUntilReset()
    {
        WriterFixture fixture = new WriterFixture();
        AssertSuccess(fixture.Writer.WriteNull());
        AssertSuccess(fixture.Writer.CompleteWriter());

        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteNull(),
            0,
            4);
        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteStartArray(),
            0,
            4);
        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteStartObject(),
            0,
            4);
        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteEndArray(),
            0,
            4);
        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteEndObject(),
            0,
            4);

        AssertSuccess(fixture.Writer.FlushWriter());
        AssertSuccess(fixture.Writer.CompleteWriter());
        Assert.That(fixture.Text(), Is.EqualTo("null"));
    }

    [Test]
    public void EndsAreRejectedAtRootForMismatchesAndForDanglingNames()
    {
        WriterFixture root = new WriterFixture();
        AssertInvalidAndUnchanged(
            root.Writer,
            root.Writer.WriteEndArray(),
            0,
            0);
        AssertInvalidAndUnchanged(
            root.Writer,
            root.Writer.WriteEndObject(),
            0,
            0);
        AssertSuccess(root.Writer.WriteNull());
        AssertInvalidAndUnchanged(
            root.Writer,
            root.Writer.WriteEndArray(),
            4,
            0);
        AssertInvalidAndUnchanged(
            root.Writer,
            root.Writer.WriteEndObject(),
            4,
            0);
        AssertSuccess(root.Writer.CompleteWriter());

        WriterFixture array = new WriterFixture();
        AssertSuccess(array.Writer.WriteStartArray());
        AssertInvalidAndUnchanged(
            array.Writer,
            array.Writer.WriteEndObject(),
            1,
            0);
        AssertSuccess(array.Writer.WriteEndArray());
        AssertSuccess(array.Writer.CompleteWriter());

        WriterFixture objectFixture = new WriterFixture();
        AssertSuccess(objectFixture.Writer.WriteStartObject());
        AssertInvalidAndUnchanged(
            objectFixture.Writer,
            objectFixture.Writer.WriteEndArray(),
            1,
            0);
        AssertSuccess(objectFixture.Writer.WritePropertyName("a"));
        AssertInvalidAndUnchanged(
            objectFixture.Writer,
            objectFixture.Writer.WriteEndArray(),
            5,
            0);
        AssertInvalidAndUnchanged(
            objectFixture.Writer,
            objectFixture.Writer.WriteEndObject(),
            5,
            0);
        AssertSuccess(objectFixture.Writer.WriteStartArray());
        AssertSuccess(objectFixture.Writer.WriteEndArray());
        AssertSuccess(objectFixture.Writer.WriteEndObject());
        AssertSuccess(objectFixture.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(root.Text(), Is.EqualTo("null"));
            Assert.That(array.Text(), Is.EqualTo("[]"));
            Assert.That(objectFixture.Text(), Is.EqualTo("{\"a\":[]}"));
        });
    }

    [Test]
    public void CompletionRejectsEveryIncompleteStructuralStateAndCanRecover()
    {
        WriterFixture empty = new WriterFixture();
        AssertInvalidAndUnchanged(
            empty.Writer,
            empty.Writer.CompleteWriter(),
            0,
            0);
        AssertSuccess(empty.Writer.WriteNull());
        AssertSuccess(empty.Writer.CompleteWriter());

        WriterFixture array = new WriterFixture();
        AssertSuccess(array.Writer.WriteStartArray());
        AssertInvalidAndUnchanged(
            array.Writer,
            array.Writer.CompleteWriter(),
            1,
            0);
        AssertSuccess(array.Writer.WriteEndArray());
        AssertSuccess(array.Writer.CompleteWriter());

        WriterFixture objectFixture = new WriterFixture();
        AssertSuccess(objectFixture.Writer.WriteStartObject());
        AssertInvalidAndUnchanged(
            objectFixture.Writer,
            objectFixture.Writer.CompleteWriter(),
            1,
            0);
        AssertSuccess(objectFixture.Writer.WritePropertyName("a"));
        AssertInvalidAndUnchanged(
            objectFixture.Writer,
            objectFixture.Writer.CompleteWriter(),
            5,
            0);
        AssertSuccess(objectFixture.Writer.WriteNull());
        AssertSuccess(objectFixture.Writer.WriteEndObject());
        AssertSuccess(objectFixture.Writer.CompleteWriter());

        WriterFixture nested = new WriterFixture();
        AssertSuccess(nested.Writer.WriteStartArray());
        AssertSuccess(nested.Writer.WriteStartObject());
        AssertInvalidAndUnchanged(
            nested.Writer,
            nested.Writer.CompleteWriter(),
            2,
            0);
        AssertSuccess(nested.Writer.WriteEndObject());
        AssertSuccess(nested.Writer.WriteEndArray());
        AssertSuccess(nested.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(empty.Text(), Is.EqualTo("null"));
            Assert.That(array.Text(), Is.EqualTo("[]"));
            Assert.That(objectFixture.Text(), Is.EqualTo("{\"a\":null}"));
            Assert.That(nested.Text(), Is.EqualTo("[{}]"));
        });
    }

    [Test]
    public void FlushIsLegalInEveryStateAndDoesNotAdvanceTheGrammar()
    {
        WriterFixture fixture = new WriterFixture();
        AssertSuccess(fixture.Writer.FlushWriter());
        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.FlushWriter());
        AssertSuccess(fixture.Writer.WritePropertyName("a"));
        AssertSuccess(fixture.Writer.FlushWriter());
        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.FlushWriter());
        AssertSuccess(fixture.Writer.WriteNull());
        AssertSuccess(fixture.Writer.FlushWriter());
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.WriteEndObject());
        AssertSuccess(fixture.Writer.FlushWriter());
        AssertSuccess(fixture.Writer.CompleteWriter());
        AssertSuccess(fixture.Writer.FlushWriter());

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Text(), Is.EqualTo("{\"a\":[null]}"));
            Assert.That(fixture.Writer.GetBytesPending(), Is.Zero);
            Assert.That(
                fixture.Writer.GetBytesCommitted(),
                Is.EqualTo(fixture.Destination.WrittenCount));
        });
    }

    [Test]
    public void ResetIsLegalFromEveryStateAndDiscardsOnlyPendingOutput()
    {
        WriterFixture fixture = new WriterFixture();

        fixture.Writer.ResetWriter();
        AssertSuccess(fixture.Writer.WriteStartArray());
        fixture.Writer.ResetWriter();
        AssertSuccess(fixture.Writer.WriteStartObject());
        fixture.Writer.ResetWriter();
        AssertSuccess(fixture.Writer.WriteStartObject());
        AssertSuccess(fixture.Writer.WritePropertyName("dangling"));
        fixture.Writer.ResetWriter();
        AssertInvalidAndUnchanged(
            fixture.Writer,
            fixture.Writer.WriteEndArray(),
            0,
            0);
        fixture.Writer.ResetWriter();
        AssertSuccess(fixture.Writer.WriteNull());
        fixture.Writer.ResetWriter();

        AssertSuccess(fixture.Writer.WriteBoolean(true));
        AssertSuccess(fixture.Writer.CompleteWriter());
        Assert.That(fixture.Text(), Is.EqualTo("true"));
        fixture.Writer.ResetWriter();

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Writer.GetBytesPending(), Is.Zero);
            Assert.That(fixture.Writer.GetBytesCommitted(), Is.Zero);
            Assert.That(fixture.Text(), Is.EqualTo("true"));
        });

        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Text(), Is.EqualTo("true[]"));
            Assert.That(fixture.Writer.GetBytesPending(), Is.Zero);
            Assert.That(fixture.Writer.GetBytesCommitted(), Is.EqualTo(2));
        });
    }

    static void AssertInvalidAndUnchanged(
        IJsonTokenWriter writer,
        JsonOperationResult result,
        long pending,
        long committed)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(
                result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(writer.GetBytesPending(), Is.EqualTo(pending));
            Assert.That(writer.GetBytesCommitted(), Is.EqualTo(committed));
        });
    }

    static void AssertSuccess(JsonOperationResult result)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
    }

    sealed class WriterFixture
    {
        internal System.Buffers.ArrayBufferWriter<byte> Destination;
        internal IJsonTokenWriter Writer;

        internal WriterFixture()
            : this(JsonInputMode.SingleValue)
        {
        }

        internal WriterFixture(JsonInputMode outputMode)
        {
            Destination = new System.Buffers.ArrayBufferWriter<byte>();
            JsonWriterOptions options = new JsonWriterOptions();
            options.OutputMode = outputMode;
            JsonWriterTarget target = new JsonWriterTarget();
            target.BufferWriter = Destination;
            Writer = new JsonUtf8Writer(options, target);
        }

        internal byte[] Bytes()
        {
            return Destination.WrittenSpan.ToArray();
        }

        internal string Text()
        {
            return System.Text.Encoding.UTF8.GetString(
                Destination.WrittenSpan);
        }
    }
}
