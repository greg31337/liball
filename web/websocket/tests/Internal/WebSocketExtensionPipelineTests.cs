using websocket;

namespace websocket.Tests;

public class WebSocketExtensionPipelineTests
{
    [Test]
    public void AppliesOutgoingInOrderAndIncomingInReverse()
    {
        var calls = new List<string>();
        var first = new PipelineTestExtension("first", 1, calls);
        first.SetRsv1 = true;
        var second = new PipelineTestExtension("second", 2, calls);
        second.SetRsv2 = true;
        var pipeline = new WebSocketExtensionPipeline(
            [first, second],
            [Negotiation(true, false, []), Negotiation(false, true, [])]);
        var context = Context(true, true, true, true);
        context.Outbound = false;

        var outgoing = pipeline.TransformOutgoing(context, new byte[] { 9 });
        var outgoingCalls = calls.ToArray();
        calls.Clear();
        var incoming = pipeline.TransformIncoming(context, outgoing.Output);

        Assert.Multiple(() =>
        {
            Assert.That(outgoing.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(outgoing.ConsumedCount, Is.EqualTo(1));
            Assert.That(outgoing.Output, Is.EqualTo(new byte[] { 9, 1, 2 }));
            Assert.That(outgoing.SetRsv1, Is.True);
            Assert.That(outgoing.SetRsv2, Is.True);
            Assert.That(outgoingCalls, Is.EqualTo(new[]
            {
                "out-first",
                "out-second",
                "complete-first",
                "complete-second"
            }));
            Assert.That(incoming.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(incoming.ConsumedCount, Is.EqualTo(3));
            Assert.That(incoming.Output, Is.EqualTo(new byte[] { 9 }));
            Assert.That(calls, Is.EqualTo(new[]
            {
                "in-second",
                "in-first",
                "complete-second",
                "complete-first"
            }));
            Assert.That(first.ObservedDirections, Is.EqualTo(new[] { true, false }));
            Assert.That(second.ObservedDirections, Is.EqualTo(new[] { true, false }));
            Assert.That(context.Outbound, Is.False);
        });
    }

    [Test]
    public void RejectsRsvBitsSetByAnExtensionThatDoesNotOwnThem()
    {
        var extension = new PipelineTestExtension("only", 1, []);
        extension.SetRsv3 = true;
        var pipeline = new WebSocketExtensionPipeline(
            [extension],
            [Negotiation(false, false, [])]);

        var result = pipeline.TransformOutgoing(
            Context(true, true, true, true),
            new byte[] { 9 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Compression));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extension-transform-rsv-unclaimed"));
        });
    }

    [Test]
    public void ConstructorRejectsConflictingAndNonReservedOpcodeClaims()
    {
        var first = new PipelineTestExtension("first", 1, []);
        var second = new PipelineTestExtension("second", 2, []);
        var rsvFirst = Negotiation(true, false, []);
        var rsvSecond = Negotiation(true, false, []);
        var opcodeFirst = Negotiation(
            false,
            false,
            [WebSocketOpcode.ReservedControl11]);
        var opcodeSecond = Negotiation(
            false,
            false,
            [WebSocketOpcode.ReservedControl11]);
        var coreOpcode = Negotiation(false, false, [WebSocketOpcode.Binary]);

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketExtensionPipeline(
                    [first, second],
                    [rsvFirst, rsvSecond]));
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketExtensionPipeline(
                    [first, second],
                    [opcodeFirst, opcodeSecond]));
            Assert.Throws<WebSocketStateException>(() =>
                new WebSocketExtensionPipeline([first], [coreOpcode]));
        });
    }

    [Test]
    public void TracksChunksAndFragmentsAndCompletesEachDirectionOnce()
    {
        var calls = new List<string>();
        var extension = new PipelineTestExtension("only", 7, calls);
        var pipeline = new WebSocketExtensionPipeline(
            [extension],
            [Negotiation(false, false, [])]);

        var first = pipeline.TransformOutgoing(
            Context(true, false, true, false),
            new byte[] { 1 });
        var middle = pipeline.TransformOutgoing(
            Context(false, false, false, false),
            new byte[] { 2 });
        var final = pipeline.TransformOutgoing(
            Context(false, true, false, true),
            new byte[] { 3 });
        var nextMessage = pipeline.TransformOutgoing(
            Context(true, true, true, true),
            new byte[] { 4 });

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(middle.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(final.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(nextMessage.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(extension.CompleteCount, Is.EqualTo(2));
            Assert.That(extension.CompletionDirections, Is.EqualTo(new[] { true, true }));
            Assert.That(pipeline.OutgoingMessageActive, Is.False);
        });
    }

    [Test]
    public void IncompleteChunkRetainsLifecycleUntilACompleteRetry()
    {
        var extension = new PipelineTestExtension("only", 7, []);
        extension.OutboundStatus = WebSocketParseStatus.NeedInput;
        var pipeline = new WebSocketExtensionPipeline(
            [extension],
            [Negotiation(false, false, [])]);
        var context = Context(true, true, true, true);

        var incomplete = pipeline.TransformOutgoing(context, new byte[] { 1 });
        extension.OutboundStatus = WebSocketParseStatus.Complete;
        var completed = pipeline.TransformOutgoing(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(completed.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(extension.CompleteCount, Is.EqualTo(1));
            Assert.That(pipeline.OutgoingMessageActive, Is.False);
        });
    }

    [Test]
    public void DownstreamBackpressureDoesNotRepeatCompletedEarlierStages()
    {
        var calls = new List<string>();
        var first = new PipelineTestExtension("first", 1, calls);
        var second = new PipelineTestExtension("second", 2, calls);
        second.OutboundStatus = WebSocketParseStatus.NeedInput;
        var pipeline = new WebSocketExtensionPipeline(
            [first, second],
            [Negotiation(false, false, []), Negotiation(false, false, [])]);
        var context = Context(true, true, true, true);

        var incomplete = pipeline.TransformOutgoing(context, new byte[] { 9 });
        var firstCalls = calls.ToArray();
        calls.Clear();
        second.OutboundStatus = WebSocketParseStatus.Complete;
        var completed = pipeline.TransformOutgoing(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(incomplete.Output, Is.EqualTo(new byte[] { 9, 1, 2 }));
            Assert.That(firstCalls, Is.EqualTo(new[] { "out-first", "out-second" }));
            Assert.That(completed.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(calls, Is.EqualTo(new[]
            {
                "out-second",
                "complete-first",
                "complete-second"
            }));
            Assert.That(first.ObservedDirections, Is.EqualTo(new[] { true }));
        });
    }

    [Test]
    public void IllegalMessageStateLatchesFailureUntilReset()
    {
        var calls = new List<string>();
        var extension = new PipelineTestExtension("only", 7, calls);
        var pipeline = new WebSocketExtensionPipeline(
            [extension],
            [Negotiation(false, false, [])]);
        var control = Context(true, true, true, true);
        control.MessageType = (WebSocketMessageType)99;

        var failed = pipeline.TransformIncoming(control, new byte[] { 1, 7 });
        var repeated = pipeline.TransformIncoming(
            Context(true, true, true, true),
            new byte[] { 1, 7 });
        pipeline.ResetPipeline();
        var recovered = pipeline.TransformIncoming(
            Context(true, true, true, true),
            new byte[] { 1, 7 });

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("extension-message-state-invalid"));
            Assert.That(repeated.Error.Identifier,
                Is.EqualTo("extension-message-state-invalid"));
            Assert.That(recovered.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(extension.ResetCount, Is.EqualTo(1));
            Assert.That(calls, Is.EqualTo(new[]
            {
                "reset-only",
                "in-only",
                "complete-only"
            }));
        });
    }

    [Test]
    public void ExtensionFailureStopsLaterStagesAndRemainsTheConnectionFailure()
    {
        var calls = new List<string>();
        var first = new PipelineTestExtension("first", 1, calls);
        first.OutboundStatus = WebSocketParseStatus.ProtocolFailure;
        var second = new PipelineTestExtension("second", 2, calls);
        var pipeline = new WebSocketExtensionPipeline(
            [first, second],
            [Negotiation(false, false, []), Negotiation(false, false, [])]);
        var context = Context(true, true, true, true);

        var failed = pipeline.TransformOutgoing(context, new byte[] { 9 });
        var repeated = pipeline.TransformIncoming(
            context,
            new byte[] { 9, 2, 1 });

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("test-extension-failure"));
            Assert.That(repeated.Error.Identifier,
                Is.EqualTo("test-extension-failure"));
            Assert.That(calls, Is.EqualTo(new[] { "out-first" }));
            Assert.That(pipeline.Failed, Is.True);
        });
    }

    [Test]
    public void MapsCustomFailuresToDeterministicCloseSemantics()
    {
        var compression = new PipelineTestExtension("compression", 1, []);
        compression.OutboundStatus = WebSocketParseStatus.ProtocolFailure;
        compression.FailureKind = WebSocketErrorKind.Compression;
        var utf8 = new PipelineTestExtension("utf8", 2, []);
        utf8.OutboundStatus = WebSocketParseStatus.ProtocolFailure;
        utf8.FailureKind = WebSocketErrorKind.Utf8;
        var limit = new PipelineTestExtension("limit", 3, []);
        limit.OutboundStatus = WebSocketParseStatus.LimitRejected;
        limit.FailureKind = WebSocketErrorKind.Protocol;

        WebSocketTransformResult compressionResult =
            new WebSocketExtensionPipeline(
                [compression],
                [Negotiation(false, false, [])]).TransformOutgoing(
                    Context(true, true, true, true),
                    [9]);
        WebSocketTransformResult utf8Result =
            new WebSocketExtensionPipeline(
                [utf8],
                [Negotiation(false, false, [])]).TransformOutgoing(
                    Context(true, true, true, true),
                    [9]);
        WebSocketTransformResult limitResult =
            new WebSocketExtensionPipeline(
                [limit],
                [Negotiation(false, false, [])]).TransformOutgoing(
                    Context(true, true, true, true),
                    [9]);

        Assert.Multiple(() =>
        {
            Assert.That(compressionResult.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.ProtocolError));
            Assert.That(compressionResult.ProducedCount, Is.Zero);
            Assert.That(compressionResult.Output, Is.Empty);
            Assert.That(utf8Result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Utf8));
            Assert.That(utf8Result.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.InvalidFramePayloadData));
            Assert.That(limitResult.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(limitResult.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(limitResult.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
        });
    }

    [Test]
    public void ReplacesUnsafeOrNonProtocolCustomFailureMetadata()
    {
        var extension = new PipelineTestExtension("only", 1, []);
        extension.OutboundStatus = WebSocketParseStatus.ProtocolFailure;
        extension.FailureKind = WebSocketErrorKind.Transport;
        extension.FailureIdentifier = "bad\nidentifier";
        extension.FailureDiagnostic = "unsafe\r\ndiagnostic";
        var pipeline = new WebSocketExtensionPipeline(
            [extension],
            [Negotiation(false, false, [])]);

        WebSocketTransformResult result = pipeline.TransformOutgoing(
            Context(true, true, true, true),
            [9]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Compression));
            Assert.That(result.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.InternalError));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extension-transform-failed"));
            Assert.That(result.Error.Diagnostic,
                Is.EqualTo("A negotiated extension rejected the message transform."));
        });
    }

    [Test]
    public void GivesClaimedBitsAndOpcodesOnlyToTheirOwner()
    {
        var calls = new List<string>();
        var owner = new PipelineTestExtension("owner", 1, calls);
        var other = new PipelineTestExtension("other", 2, calls);
        other.MutateObservedContext = true;
        var pipeline = new WebSocketExtensionPipeline(
            [owner, other],
            [
                Negotiation(
                    true,
                    false,
                    [WebSocketOpcode.ReservedData3]),
                Negotiation(false, false, [])
            ]);
        WebSocketExtensionContext context = Context(true, true, true, true);
        context.Rsv1 = true;
        context.HasOpcode = true;
        context.Opcode = WebSocketOpcode.ReservedData3;

        WebSocketTransformResult result = pipeline.TransformIncoming(
            context,
            [9, 1, 2]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(other.ObservedRsv1, Is.EqualTo(new[] { false }));
            Assert.That(other.ObservedHasOpcode, Is.EqualTo(new[] { false }));
            Assert.That(owner.ObservedRsv1, Is.EqualTo(new[] { true }));
            Assert.That(owner.ObservedHasOpcode, Is.EqualTo(new[] { true }));
            Assert.That(owner.ObservedOpcodes,
                Is.EqualTo(new[] { WebSocketOpcode.ReservedData3 }));
            Assert.That(context.Rsv1, Is.True);
            Assert.That(context.HasOpcode, Is.True);
        });
    }

    [Test]
    public void RejectsUnownedInboundClaimsBeforeRunningExtensions()
    {
        var calls = new List<string>();
        var extension = new PipelineTestExtension("only", 1, calls);
        var pipeline = new WebSocketExtensionPipeline(
            [extension],
            [Negotiation(false, false, [])]);
        WebSocketExtensionContext context = Context(true, true, true, true);
        context.Rsv3 = true;
        context.HasOpcode = true;
        context.Opcode = WebSocketOpcode.ReservedControl11;

        WebSocketTransformResult result = pipeline.TransformIncoming(
            context,
            [9, 1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extension-transform-claim-unowned"));
            Assert.That(result.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.ProtocolError));
            Assert.That(calls, Is.Empty);
        });
    }

    [Test]
    public void ReleaseResetsOnceDropsReferencesAndCannotBeReused()
    {
        var first = new PipelineTestExtension("first", 1, []);
        var second = new PipelineTestExtension("second", 2, []);
        var pipeline = new WebSocketExtensionPipeline(
            [first, second],
            [Negotiation(false, false, []), Negotiation(false, false, [])]);

        first.OutboundStatus = WebSocketParseStatus.NeedInput;
        pipeline.TransformOutgoing(
            Context(true, true, true, true),
            [9]);
        pipeline.ReleasePipeline();
        pipeline.ReleasePipeline();
        pipeline.ResetPipeline();
        WebSocketTransformResult result = pipeline.TransformOutgoing(
            Context(true, true, true, true),
            [9]);

        Assert.Multiple(() =>
        {
            Assert.That(first.ResetCount, Is.EqualTo(1));
            Assert.That(second.ResetCount, Is.EqualTo(1));
            Assert.That(pipeline.Released, Is.True);
            Assert.That(pipeline.Extensions, Is.Empty);
            Assert.That(pipeline.Negotiations, Is.Empty);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.State));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extension-pipeline-released"));
        });
    }

    [Test]
    public void ConstructorRejectsTheSameStatefulHandlerTwice()
    {
        var extension = new PipelineTestExtension("same", 1, []);

        Assert.Throws<WebSocketStateException>(() =>
            new WebSocketExtensionPipeline(
                [extension, extension],
                [
                    Negotiation(true, false, []),
                    Negotiation(false, true, [])
                ]));
    }

    [Test]
    public void OversizedStageOutputFailsBeforeTheNextExtensionRuns()
    {
        var calls = new List<string>();
        var first = new PipelineTestExtension("first", 1, calls);
        var second = new PipelineTestExtension("second", 2, calls);
        var pipeline = new WebSocketExtensionPipeline(
            [first, second],
            [Negotiation(false, false, []), Negotiation(false, false, [])]);
        var context = Context(true, true, true, true);
        context.MaximumBufferedOutputBytes = 2;

        var result = pipeline.TransformOutgoing(context, new byte[] { 8, 9 });
        var repeated = pipeline.TransformOutgoing(context, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extension-transform-output-too-large"));
            Assert.That(repeated.Status,
                Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(calls, Is.EqualTo(new[] { "out-first" }));
        });
    }

    [Test]
    public void FragmentFlagsCannotReturnToAnEarlierFragment()
    {
        var extension = new PipelineTestExtension("only", 7, []);
        var pipeline = new WebSocketExtensionPipeline(
            [extension],
            [Negotiation(false, false, [])]);
        pipeline.TransformOutgoing(
            Context(true, false, true, false),
            new byte[] { 1 });
        pipeline.TransformOutgoing(
            Context(false, false, false, false),
            new byte[] { 2 });

        var result = pipeline.TransformOutgoing(
            Context(false, true, true, true),
            new byte[] { 3 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Identifier,
                Is.EqualTo("extension-message-state-invalid"));
            Assert.That(extension.CompleteCount, Is.Zero);
        });
    }

    static WebSocketExtensionContext Context(
        bool firstChunk,
        bool finalChunk,
        bool firstFragment,
        bool finalFragment)
    {
        return new WebSocketExtensionContext
        {
            MessageType = WebSocketMessageType.Binary,
            FirstChunk = firstChunk,
            FinalChunk = finalChunk,
            FirstFragment = firstFragment,
            FinalFragment = finalFragment
        };
    }

    static WebSocketExtensionNegotiation Negotiation(
        bool rsv1,
        bool rsv2,
        WebSocketOpcode[] opcodes)
    {
        return new WebSocketExtensionNegotiation
        {
            Status = WebSocketExtensionStatus.Negotiated,
            ClaimsRsv1 = rsv1,
            ClaimsRsv2 = rsv2,
            ClaimedOpcodes = opcodes
        };
    }
}

internal class PipelineTestExtension : IWebSocketExtension
{
    internal string Name;
    internal byte Marker;
    internal List<string> Calls;
    internal List<bool> ObservedDirections = [];
    internal List<bool> ObservedRsv1 = [];
    internal List<bool> ObservedHasOpcode = [];
    internal List<WebSocketOpcode> ObservedOpcodes = [];
    internal bool SetRsv1;
    internal bool SetRsv2;
    internal bool SetRsv3;
    internal WebSocketParseStatus OutboundStatus = WebSocketParseStatus.Complete;
    internal WebSocketErrorKind FailureKind = WebSocketErrorKind.Compression;
    internal string FailureIdentifier = "test-extension-failure";
    internal string FailureDiagnostic = "";
    internal bool MutateObservedContext;
    internal int CompleteCount;
    internal int ResetCount;
    internal List<bool> CompletionDirections = [];

    internal PipelineTestExtension(string name, byte marker, List<string> calls)
    {
        Name = name;
        Marker = marker;
        Calls = calls;
    }

    public WebSocketExtensionOffer CreateClientExtensionOffer()
    {
        return new WebSocketExtensionOffer { Name = Name };
    }

    public WebSocketExtensionNegotiation AcceptClientExtensionOffer(
        WebSocketExtensionOffer offer)
    {
        return new WebSocketExtensionNegotiation();
    }

    public WebSocketExtensionNegotiation ValidateServerExtensionResponse(
        WebSocketExtensionOffer response)
    {
        return new WebSocketExtensionNegotiation();
    }

    public WebSocketTransformResult TransformOutboundChunk(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        Calls.Add("out-" + Name);
        ObserveContext(context);
        byte[] output = new byte[source.Length + 1];
        source.CopyTo(output);
        output[source.Length] = Marker;
        var result = new WebSocketTransformResult
        {
            Status = OutboundStatus,
            ConsumedCount = source.Length,
            ProducedCount = output.Length,
            Output = output,
            SetRsv1 = SetRsv1,
            SetRsv2 = SetRsv2,
            SetRsv3 = SetRsv3
        };
        if (OutboundStatus == WebSocketParseStatus.ProtocolFailure ||
            OutboundStatus == WebSocketParseStatus.LimitRejected)
        {
            result.HasError = true;
            result.Error.Kind = FailureKind;
            result.Error.Identifier = FailureIdentifier;
            result.Error.Diagnostic = FailureDiagnostic;
        }

        return result;
    }

    public WebSocketTransformResult TransformInboundChunk(
        WebSocketExtensionContext context,
        ReadOnlySpan<byte> source)
    {
        Calls.Add("in-" + Name);
        ObserveContext(context);
        if (source.Length == 0 || source[source.Length - 1] != Marker)
        {
            var failure = new WebSocketTransformResult();
            failure.Status = WebSocketParseStatus.ProtocolFailure;
            failure.HasError = true;
            failure.Error.Kind = WebSocketErrorKind.Compression;
            failure.Error.Identifier = "test-marker-missing";
            return failure;
        }

        byte[] output = source.Slice(0, source.Length - 1).ToArray();
        return new WebSocketTransformResult
        {
            Status = WebSocketParseStatus.Complete,
            ConsumedCount = source.Length,
            ProducedCount = output.Length,
            Output = output
        };
    }

    public void CompleteExtensionMessage(WebSocketExtensionContext context)
    {
        Calls.Add("complete-" + Name);
        CompleteCount++;
        CompletionDirections.Add(context.Outbound);
    }

    public void ResetExtensionState()
    {
        Calls.Add("reset-" + Name);
        ResetCount++;
    }

    void ObserveContext(WebSocketExtensionContext context)
    {
        ObservedDirections.Add(context.Outbound);
        ObservedRsv1.Add(context.Rsv1);
        ObservedHasOpcode.Add(context.HasOpcode);
        if (context.HasOpcode)
        {
            ObservedOpcodes.Add(context.Opcode);
        }

        if (MutateObservedContext)
        {
            context.Rsv1 = false;
            context.Rsv2 = false;
            context.Rsv3 = false;
            context.HasOpcode = false;
            context.Opcode = WebSocketOpcode.Continuation;
        }
    }
}
