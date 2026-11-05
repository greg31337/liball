namespace json.Tests;

public class JsonWriterCoreTests
{
    [Test]
    public void WriteFrameTracksRootArrayAndObjectTransitions()
    {
        JsonWriteFrame root = new JsonWriteFrame(JsonContainerKind.Root);
        JsonWriteFrame array = new JsonWriteFrame(JsonContainerKind.Array);
        JsonWriteFrame objectFrame = new JsonWriteFrame(JsonContainerKind.Object);

        objectFrame.AcceptPropertyName();
        objectFrame.AcceptCompletedValue();
        array.AcceptCompletedValue();
        root.AcceptCompletedValue();

        Assert.Multiple(() =>
        {
            Assert.That(root.CanAcceptValue(), Is.False);
            Assert.That(root.GetValueCount(), Is.EqualTo(1));
            Assert.That(array.CanAcceptValue(), Is.True);
            Assert.That(array.CanCloseContainer(), Is.True);
            Assert.That(array.GetValueCount(), Is.EqualTo(1));
            Assert.That(objectFrame.CanAcceptPropertyName(), Is.True);
            Assert.That(objectFrame.CanCloseContainer(), Is.True);
            Assert.That(objectFrame.GetMemberCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void CoreWritesNestedObjectAndArrayAndCompletesOneRoot()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterCore core = CreateCore(destination, new JsonLimits());

        Assert.That(core.WriteObjectStart().Success, Is.True);
        Assert.That(core.WriteEncodedPropertyName(Bytes("\"items\"")).Success, Is.True);
        Assert.That(core.WriteArrayStart().Success, Is.True);
        Assert.That(core.WriteEncodedScalarValue(Bytes("true")).Success, Is.True);
        Assert.That(core.WriteObjectStart().Success, Is.True);
        Assert.That(core.WriteEncodedPropertyName(Bytes("\"n\"")).Success, Is.True);
        Assert.That(core.WriteEncodedScalarValue(Bytes("null")).Success, Is.True);
        Assert.That(core.WriteObjectEnd().Success, Is.True);
        Assert.That(core.WriteArrayEnd().Success, Is.True);
        Assert.That(core.WriteObjectEnd().Success, Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(core.GetContainerDepth(), Is.Zero);
            Assert.That(core.GetPendingByteCount(), Is.EqualTo(27));
            Assert.That(core.GetCommittedByteCount(), Is.Zero);
            Assert.That(core.IsOutputCompleted(), Is.False);
        });

        JsonOperationResult completed = core.CompleteOutput();

        Assert.Multiple(() =>
        {
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.BytesWritten, Is.EqualTo(27));
            Assert.That(core.IsOutputCompleted(), Is.True);
            Assert.That(core.GetPendingByteCount(), Is.Zero);
            Assert.That(core.GetCommittedByteCount(), Is.EqualTo(27));
            Assert.That(Text(destination), Is.EqualTo("{\"items\":[true,{\"n\":null}]}"));
            Assert.That(core.CompleteOutput().Success, Is.True);
        });
    }

    [Test]
    public void InvalidTransitionsLeaveStateAndOutputUnchanged()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterCore core = CreateCore(destination, new JsonLimits());

        JsonOperationResult rootName =
            core.WriteEncodedPropertyName(Bytes("\"name\""));
        Assert.That(core.WriteObjectStart().Success, Is.True);
        JsonOperationResult unnamedValue =
            core.WriteEncodedScalarValue(Bytes("false"));
        Assert.That(core.WriteEncodedPropertyName(Bytes("\"name\"")).Success, Is.True);
        JsonOperationResult danglingEnd = core.WriteObjectEnd();
        JsonOperationResult wrongEnd = core.WriteArrayEnd();

        Assert.Multiple(() =>
        {
            Assert.That(rootName.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
            Assert.That(unnamedValue.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
            Assert.That(danglingEnd.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
            Assert.That(wrongEnd.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
            Assert.That(core.GetContainerDepth(), Is.EqualTo(1));
            Assert.That(core.GetPendingByteCount(), Is.EqualTo(8));
        });

        Assert.That(core.WriteEncodedScalarValue(Bytes("0")).Success, Is.True);
        Assert.That(core.WriteObjectEnd().Success, Is.True);
        Assert.That(core.CompleteOutput().Success, Is.True);
        JsonOperationResult afterComplete =
            core.WriteEncodedScalarValue(Bytes("1"));

        Assert.Multiple(() =>
        {
            Assert.That(afterComplete.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
            Assert.That(Text(destination), Is.EqualTo("{\"name\":0}"));
        });
    }

    [Test]
    public void DepthAndCollectionLimitsFailBeforeChangingWriterState()
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumNestingDepth = 1;
        limits.MaximumArrayItems = 1;
        limits.MaximumObjectMembers = 1;
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterCore core = CreateCore(destination, limits);

        Assert.That(core.WriteArrayStart().Success, Is.True);
        JsonOperationResult depth = core.WriteObjectStart();
        Assert.That(core.WriteEncodedScalarValue(Bytes("0")).Success, Is.True);
        JsonOperationResult arrayCount =
            core.WriteEncodedScalarValue(Bytes("1"));
        Assert.That(core.WriteArrayEnd().Success, Is.True);
        Assert.That(core.CompleteOutput().Success, Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(depth.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.NestingDepthLimitExceeded));
            Assert.That(arrayCount.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.ArrayItemLimitExceeded));
            Assert.That(Text(destination), Is.EqualTo("[0]"));
        });

        core.ResetCore();
        Assert.That(core.WriteObjectStart().Success, Is.True);
        Assert.That(core.WriteEncodedPropertyName(Bytes("\"a\"")).Success, Is.True);
        Assert.That(core.WriteEncodedScalarValue(Bytes("0")).Success, Is.True);
        JsonOperationResult objectCount =
            core.WriteEncodedPropertyName(Bytes("\"b\""));

        Assert.Multiple(() =>
        {
            Assert.That(objectCount.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.ObjectMemberLimitExceeded));
            Assert.That(core.GetPendingByteCount(), Is.EqualTo(6));
        });
    }

    [Test]
    public void OutputLimitFailureIsAtomicAndResetStartsIndependentState()
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumOutputBytes = 3;
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterCore core = CreateCore(destination, limits);

        Assert.That(core.WriteArrayStart().Success, Is.True);
        Assert.That(core.WriteEncodedScalarValue(Bytes("0")).Success, Is.True);
        JsonOperationResult limited =
            core.WriteEncodedScalarValue(Bytes("1"));

        Assert.Multiple(() =>
        {
            Assert.That(limited.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
            Assert.That(core.GetPendingByteCount(), Is.EqualTo(2));
            Assert.That(core.GetContainerDepth(), Is.EqualTo(1));
        });

        Assert.That(core.WriteArrayEnd().Success, Is.True);
        Assert.That(core.CompleteOutput().Success, Is.True);
        core.ResetCore();
        Assert.That(core.WriteEncodedScalarValue(Bytes("2")).Success, Is.True);
        Assert.That(core.CompleteOutput().Success, Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(core.GetCommittedByteCount(), Is.EqualTo(1));
            Assert.That(Text(destination), Is.EqualTo("[0]2"));
        });
    }

    [Test]
    public void CompletionRequiresAClosedRootValue()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterCore core = CreateCore(destination, new JsonLimits());

        JsonOperationResult empty = core.CompleteOutput();
        Assert.That(core.WriteObjectStart().Success, Is.True);
        JsonOperationResult open = core.CompleteOutput();

        Assert.Multiple(() =>
        {
            Assert.That(empty.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
            Assert.That(open.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
            Assert.That(destination.WrittenCount, Is.Zero);
            Assert.That(core.GetPendingByteCount(), Is.EqualTo(1));
        });
    }

    static JsonWriterCore CreateCore(
        System.Buffers.ArrayBufferWriter<byte> destination,
        JsonLimits limits)
    {
        JsonWriterOptions options = new JsonWriterOptions();
        options.Limits = limits;
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        return new JsonWriterCore(options, target);
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    static string Text(System.Buffers.ArrayBufferWriter<byte> destination)
    {
        return System.Text.Encoding.UTF8.GetString(destination.WrittenSpan);
    }
}
