namespace avif.Tests;

public class DiagnosticContextTests
{
    [Test]
    public void DiagnosticCapturesEveryActiveNestedContext()
    {
        DiagnosticContext context = new DiagnosticContext();
        context.PushBox("meta");
        context.PushBox("iloc");
        context.PushItem(17);
        context.PushTrack(4);
        context.PushFrame(8);
        context.PushTile(2);
        context.PushAv1Syntax("tile_group_obu");
        context.PushAv1Syntax("tile_size_minus_1");
        context.SetLocation(4096, 3);
        context.SetStandardReference("AV1 6.10.1");

        AvifDiagnostic diagnostic = context.CreateDiagnostic(
            AvifResultCode.InvalidAv1Bitstream,
            AvifDiagnosticSeverity.Fatal,
            AvifDiagnosticCategory.Av1,
            "invalid tile size");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.BoxPath, Is.EqualTo("/meta/iloc"));
            Assert.That(diagnostic.ItemId, Is.EqualTo(17));
            Assert.That(diagnostic.TrackId, Is.EqualTo(4));
            Assert.That(diagnostic.FrameIndex, Is.EqualTo(8));
            Assert.That(diagnostic.TileIndex, Is.EqualTo(2));
            Assert.That(diagnostic.Av1SyntaxPath, Is.EqualTo("tile_group_obu/tile_size_minus_1"));
            Assert.That(diagnostic.ByteOffset, Is.EqualTo(4096));
            Assert.That(diagnostic.BitOffset, Is.EqualTo(3));
            Assert.That(diagnostic.StandardReference, Is.EqualTo("AV1 6.10.1"));
        });
    }

    [Test]
    public void PopRestoresOuterEntityAndSyntaxContexts()
    {
        DiagnosticContext context = new DiagnosticContext();
        context.PushItem(10);
        context.PushItem(11);
        context.PushTrack(20);
        context.PushTrack(21);
        context.PushFrame(1);
        context.PushFrame(2);
        context.PushTile(3);
        context.PushTile(4);
        context.PushAv1Syntax("frame_header_obu");
        context.PushAv1Syntax("tile_info");

        context.PopItem();
        context.PopTrack();
        context.PopFrame();
        context.PopTile();
        context.PopAv1Syntax();
        AvifDiagnostic diagnostic = context.CreateDiagnostic(
            AvifResultCode.InvalidData,
            AvifDiagnosticSeverity.Error,
            AvifDiagnosticCategory.Av1,
            "outer context");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.ItemId, Is.EqualTo(10));
            Assert.That(diagnostic.TrackId, Is.EqualTo(20));
            Assert.That(diagnostic.FrameIndex, Is.EqualTo(1));
            Assert.That(diagnostic.TileIndex, Is.EqualTo(3));
            Assert.That(diagnostic.Av1SyntaxPath, Is.EqualTo("frame_header_obu"));
        });
    }

    [Test]
    public void InvalidPushPopAndLocationOperationsDoNotCorruptContext()
    {
        DiagnosticContext context = new DiagnosticContext();
        context.PushBox("meta");
        context.SetLocation(100, 5);

        Assert.Multiple(() =>
        {
            Assert.That(context.PushBox("bad/path"), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(context.PushItem(AvifDiagnostic.UnknownEntityId), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(context.PushFrame(-1), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(context.PushTile(-1), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(context.PushAv1Syntax(string.Empty), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(context.PopTrack(), Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(context.SetLocation(100, 8), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(context.SetLocation(AvifDiagnostic.UnknownByteOffset, 0), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(context.GetBoxPath(), Is.EqualTo("/meta"));
            Assert.That(context.ByteOffset, Is.EqualTo(100));
            Assert.That(context.BitOffset, Is.EqualTo(5));
        });
    }

    [Test]
    public void DiagnosticLogSnapshotsContextTracksFirstErrorAndClearsDeterministically()
    {
        DiagnosticLog log = new DiagnosticLog();
        log.Context.PushBox("meta");
        AvifDiagnostic warning = log.Add(
            AvifResultCode.NonConformingData,
            AvifDiagnosticSeverity.Warning,
            AvifDiagnosticCategory.Validation,
            "compatible deviation");
        log.Context.PushBox("iloc");
        AvifDiagnostic error = log.Add(
            AvifResultCode.InvalidContainer,
            AvifDiagnosticSeverity.Error,
            AvifDiagnosticCategory.Container,
            "invalid extent");
        log.Context.PopBox();
        log.Add(
            AvifResultCode.LimitExceeded,
            AvifDiagnosticSeverity.Fatal,
            AvifDiagnosticCategory.Resource,
            "limit");

        Assert.Multiple(() =>
        {
            Assert.That(warning.BoxPath, Is.EqualTo("/meta"));
            Assert.That(error.BoxPath, Is.EqualTo("/meta/iloc"));
            Assert.That(log.Diagnostics.Count, Is.EqualTo(3));
            Assert.That(log.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(log.Get(0), Is.SameAs(warning));
            Assert.That(log.Get(3), Is.Null);
        });

        log.Clear();
        Assert.Multiple(() =>
        {
            Assert.That(log.Diagnostics, Is.Empty);
            Assert.That(log.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(log.Context.GetBoxPath(), Is.Empty);
            Assert.That(log.Context.ByteOffset, Is.EqualTo(AvifDiagnostic.UnknownByteOffset));
        });
    }
}
