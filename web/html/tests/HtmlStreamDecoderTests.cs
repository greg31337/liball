using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlStreamDecoderTests
{
    [Test]
    public void MetaDeclarationRestartsTentativeDecodingBeforeTextIsExposed()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<meta charset=utf-8><p>€</p>");
        TrackingChunkedStream stream = new TrackingChunkedStream(bytes, 1);
        HtmlBudget budget = CreateBudget();
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.Create(
            stream, true, 2, "", "windows-1252", budget, diagnostics);

        HtmlDecodedInput result = decoder.Load();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetString(), Is.EqualTo("<meta charset=utf-8><p>€</p>"));
            Assert.That(decoder.GetInitialEncodingName(), Is.EqualTo("windows-1252"));
            Assert.That(decoder.GetDetection().GetEncodingName(), Is.EqualTo("UTF-8"));
            Assert.That(decoder.GetDetection().GetSource(), Is.EqualTo(HtmlEncodingSource.Meta));
            Assert.That(decoder.WasEncodingRestarted(), Is.True);
            Assert.That(decoder.GetBufferSize(), Is.EqualTo(2));
            Assert.That(decoder.GetBytesRead(), Is.EqualTo(bytes.Length));
            Assert.That(decoder.IsLoaded(), Is.True);
            Assert.That(decoder.WasLimitExceeded(), Is.False);
            Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
            Assert.That(ReferenceEquals(decoder.Load(), result), Is.True);
        });

        decoder.Close();
        Assert.That(stream.GetWasDisposed(), Is.False);
    }

    [Test]
    public void TransportEncodingPreventsAMetaRestart()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<meta charset=shift_jis>€");
        HtmlBudget budget = CreateBudget();
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.Create(
            new MemoryStream(bytes), true, 4, "utf-8", "windows-1252", budget, diagnostics);

        HtmlDecodedInput result = decoder.Load();

        Assert.That(result.GetString(), Is.EqualTo("<meta charset=shift_jis>€"));
        Assert.That(decoder.GetInitialEncodingName(), Is.EqualTo("UTF-8"));
        Assert.That(decoder.GetDetection().GetSource(), Is.EqualTo(HtmlEncodingSource.Transport));
        Assert.That(decoder.WasEncodingRestarted(), Is.False);
        decoder.Close();
    }

    [Test]
    public void BomOverridesAnExplicitEncodingAndIsNotAParserRestart()
    {
        byte[] bytes = [0xFF, 0xFE, 0x41, 0x00];
        HtmlBudget budget = CreateBudget();
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.CreateWithEncoding(
            new MemoryStream(bytes), Encoding.UTF8, true, 1, budget, diagnostics);

        HtmlDecodedInput result = decoder.Load();

        Assert.That(result.GetString(), Is.EqualTo("A"));
        Assert.That(result.GetEncodingName(), Is.EqualTo("UTF-16LE"));
        Assert.That(decoder.GetDetection().GetSource(), Is.EqualTo(HtmlEncodingSource.Bom));
        Assert.That(decoder.WasEncodingRestarted(), Is.False);
        Assert.That(result.GetByteOffset(0), Is.EqualTo(2));
        decoder.Close();
    }

    [Test]
    public void ExplicitEncodingUsesItsOwnSemanticsInsteadOfWebAliasSemantics()
    {
        byte[] bytes = [0x80, 0xE9];
        HtmlBudget budget = CreateBudget();
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.CreateWithEncoding(
            new MemoryStream(bytes), Encoding.Latin1, true, budget, diagnostics);

        HtmlDecodedInput result = decoder.Load();

        Assert.That(result.GetString(), Is.EqualTo("\u0080é"));
        Assert.That(result.GetEncodingName(), Is.EqualTo("iso-8859-1"));
        Assert.That(decoder.GetDetection().GetSource(), Is.EqualTo(HtmlEncodingSource.Explicit));
        Assert.That(decoder.GetDetection().GetConfidence(), Is.EqualTo(HtmlEncodingConfidence.Certain));
        Assert.That(decoder.WasEncodingRestarted(), Is.False);
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
        decoder.Close();
    }

    [Test]
    public void ExplicitNonWebEncodingIsDecodedWithExactByteMappings()
    {
        byte[] bytes = Encoding.UTF32.GetBytes("A💩");
        HtmlBudget budget = CreateBudget();
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.CreateWithEncoding(
            new MemoryStream(bytes), Encoding.UTF32, true, 3, budget, diagnostics);

        HtmlDecodedInput result = decoder.Load();

        Assert.That(result.GetString(), Is.EqualTo("A💩"));
        Assert.That(result.GetEncodingName(), Is.EqualTo("utf-32"));
        Assert.That(result.GetByteOffset(0), Is.Zero);
        Assert.That(result.GetByteOffset(1), Is.EqualTo(4));
        Assert.That(result.GetByteOffset(2), Is.EqualTo(4));
        Assert.That(result.GetByteOffset(3), Is.EqualTo(8));
        Assert.That(result.GetCharacterOffset(7), Is.EqualTo(1));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
        decoder.Close();
    }

    [Test]
    public void ExplicitlyEncodedReplacementCharacterIsNotReportedAsMalformed()
    {
        byte[] bytes = Encoding.UTF32.GetBytes("�");
        HtmlBudget budget = CreateBudget();
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.CreateWithEncoding(
            new MemoryStream(bytes), Encoding.UTF32, true, budget, diagnostics);

        Assert.That(decoder.Load().GetString(), Is.EqualTo("�"));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
        decoder.Close();
    }

    [Test]
    public void MalformedSequencesProduceByteAwareDiagnostics()
    {
        byte[] bytes = [0x41, 0x0A, 0xE1, 0x80, 0x42];
        HtmlBudget budget = CreateBudget();
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.CreateWithEncoding(
            new MemoryStream(bytes), Encoding.UTF8, true, 2, budget, diagnostics);

        HtmlDecodedInput result = decoder.Load();

        Assert.That(result.GetString(), Is.EqualTo("A\n�B"));
        HtmlDiagnostic[] found = diagnostics.GetDiagnostics();
        Assert.That(found, Has.Length.EqualTo(1));
        Assert.That(found[0].GetCode(), Is.EqualTo(HtmlDiagnosticCode.EncodingInvalidSequence));
        Assert.That(found[0].GetSeverity(), Is.EqualTo(HtmlDiagnosticSeverity.Warning));
        Assert.That(found[0].GetSpan().GetStart().GetCharacterOffset(), Is.EqualTo(2));
        Assert.That(found[0].GetSpan().GetStart().GetLine(), Is.EqualTo(2));
        Assert.That(found[0].GetSpan().GetStart().GetColumn(), Is.EqualTo(1));
        Assert.That(found[0].GetSpan().GetStart().GetByteOffset(), Is.EqualTo(2));
        Assert.That(found[0].GetSpan().GetEnd().GetCharacterOffset(), Is.EqualTo(3));
        Assert.That(found[0].GetSpan().GetEnd().GetByteOffset(), Is.EqualTo(4));
        Assert.That(found[0].GetSpan().GetByteLength(), Is.EqualTo(2));
        decoder.Close();
    }

    [Test]
    public void ExplicitAsciiReplacementAlsoProducesADiagnostic()
    {
        HtmlBudget budget = CreateBudget();
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.CreateWithEncoding(
            new MemoryStream(new byte[] { 0x80 }), Encoding.ASCII, true, budget, diagnostics);

        Assert.That(decoder.Load().GetString(), Is.EqualTo("�"));
        Assert.That(diagnostics.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.EncodingInvalidSequence }));
        decoder.Close();
    }

    [Test]
    public void ByteLimitBoundsReadsAndReportsTheTruncatedEndpoint()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputBytes(3);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        TrackingChunkedStream stream = new TrackingChunkedStream("abcde"u8.ToArray(), 2);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.Create(stream, true, budget, diagnostics);

        HtmlDecodedInput result = decoder.Load();

        Assert.That(result.GetString(), Is.EqualTo("abc"));
        Assert.That(decoder.WasLimitExceeded(), Is.True);
        Assert.That(decoder.GetBytesRead(), Is.EqualTo(3));
        Assert.That(stream.GetPosition(), Is.EqualTo(4));
        Assert.That(budget.GetUsed(HtmlLimitKind.InputBytes), Is.EqualTo(3));
        Assert.That(budget.GetFirstExceededLimit(), Is.EqualTo(HtmlLimitKind.InputBytes));
        HtmlDiagnostic diagnostic = diagnostics.GetDiagnostics().Single();
        Assert.That(diagnostic.GetCode(), Is.EqualTo(HtmlDiagnosticCode.InputLimitExceeded));
        Assert.That(diagnostic.GetSpan().GetStart().GetByteOffset(), Is.EqualTo(3));
        decoder.Close();
    }

    [Test]
    public void CharacterLimitIsAccountedForAfterDecoding()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(1);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.CreateWithEncoding(
            new MemoryStream(Encoding.UTF32.GetBytes("AB")), Encoding.UTF32, true, budget, diagnostics);

        Assert.That(decoder.Load().GetString(), Is.EqualTo("AB"));
        Assert.That(decoder.WasLimitExceeded(), Is.True);
        Assert.That(budget.GetFirstExceededLimit(), Is.EqualTo(HtmlLimitKind.InputCharacters));
        Assert.That(diagnostics.GetDiagnostics().Single().GetCode(), Is.EqualTo(HtmlDiagnosticCode.InputLimitExceeded));
        decoder.Close();
    }

    [Test]
    public void CancellationBeforeLoadDoesNotReadTheStream()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), source.Token);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        TrackingChunkedStream stream = new TrackingChunkedStream("abc"u8.ToArray(), 1);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.Create(stream, true, budget, diagnostics);

        Assert.Throws<OperationCanceledException>(() => decoder.Load());
        Assert.That(stream.GetPosition(), Is.Zero);
        Assert.That(budget.GetUsed(HtmlLimitKind.InputBytes), Is.Zero);
        decoder.Close();
    }

    [Test]
    public void CancellationAfterAReadStopsBeforeBufferMutation()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), source.Token);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        TrackingChunkedStream stream = new TrackingChunkedStream("abc"u8.ToArray(), 2, source);
        HtmlStreamDecoder decoder = HtmlStreamDecoder.Create(stream, true, budget, diagnostics);

        Assert.Throws<OperationCanceledException>(() => decoder.Load());
        Assert.That(stream.GetPosition(), Is.EqualTo(2));
        Assert.That(budget.GetUsed(HtmlLimitKind.InputBytes), Is.Zero);
        Assert.That(decoder.IsLoaded(), Is.False);
        decoder.Close();
    }

    [Test]
    public void CloseHonorsOwnershipAndIsIdempotent()
    {
        MemoryStream retainedStream = new MemoryStream("r"u8.ToArray());
        HtmlBudget retainedBudget = CreateBudget();
        HtmlStreamDecoder retained = HtmlStreamDecoder.Create(
            retainedStream, true, retainedBudget, new HtmlDiagnosticCollector(retainedBudget));
        Assert.That(retained.GetLeaveOpen(), Is.True);
        Assert.That(retained.GetBufferSize(), Is.EqualTo(4096));
        retained.Close();
        Assert.DoesNotThrow(retained.Close);
        Assert.That(retainedStream.ReadByte(), Is.EqualTo('r'));
        Assert.Throws<ObjectDisposedException>(() => retained.Load());

        MemoryStream ownedStream = new MemoryStream("o"u8.ToArray());
        HtmlBudget ownedBudget = CreateBudget();
        HtmlStreamDecoder owned = HtmlStreamDecoder.Create(
            ownedStream, false, ownedBudget, new HtmlDiagnosticCollector(ownedBudget));
        owned.Close();
        Assert.Throws<ObjectDisposedException>(() => ownedStream.ReadByte());
    }

    [Test]
    public void FactoriesValidateDependenciesAndBufferBounds()
    {
        HtmlBudget budget = CreateBudget();
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        MemoryStream stream = new MemoryStream();

        Assert.Throws<ArgumentNullException>(() => HtmlStreamDecoder.Create(null, true, budget, diagnostics));
        Assert.Throws<ArgumentNullException>(() => HtmlStreamDecoder.Create(stream, true, null, diagnostics));
        Assert.Throws<ArgumentNullException>(() => HtmlStreamDecoder.Create(stream, true, budget, null));
        Assert.Throws<ArgumentNullException>(() => HtmlStreamDecoder.Create(
            stream, true, 1, null, "windows-1252", budget, diagnostics));
        Assert.Throws<ArgumentNullException>(() => HtmlStreamDecoder.CreateWithEncoding(
            stream, null, true, budget, diagnostics));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlStreamDecoder.Create(
            stream, true, 0, "", "windows-1252", budget, diagnostics));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlStreamDecoder.Create(
            stream, true, 65537, "", "windows-1252", budget, diagnostics));
    }

    static HtmlBudget CreateBudget()
    {
        return new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
    }
}

class TrackingChunkedStream : Stream
{
    byte[] bytes;
    int maximumChunk;
    int position;
    bool disposed;
    CancellationTokenSource cancelAfterRead;

    public TrackingChunkedStream(byte[] bytes, int maximumChunk)
    {
        this.bytes = bytes;
        this.maximumChunk = maximumChunk;
        this.position = 0;
        this.disposed = false;
        this.cancelAfterRead = null;
    }

    public TrackingChunkedStream(byte[] bytes, int maximumChunk, CancellationTokenSource cancelAfterRead)
    {
        this.bytes = bytes;
        this.maximumChunk = maximumChunk;
        this.position = 0;
        this.disposed = false;
        this.cancelAfterRead = cancelAfterRead;
    }

    public override bool CanRead => !this.disposed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => this.bytes.Length;

    public override long Position
    {
        get => this.position;
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        this.ThrowIfDisposed();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        this.ThrowIfDisposed();
        int remaining = this.bytes.Length - this.position;
        int read = Math.Min(Math.Min(count, this.maximumChunk), remaining);
        Array.Copy(this.bytes, this.position, buffer, offset, read);
        this.position += read;
        if (this.cancelAfterRead != null)
        {
            this.cancelAfterRead.Cancel();
        }

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public int GetPosition()
    {
        return this.position;
    }

    public bool GetWasDisposed()
    {
        return this.disposed;
    }

    protected override void Dispose(bool disposing)
    {
        this.disposed = true;
        base.Dispose(disposing);
    }

    void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException("TrackingChunkedStream");
        }
    }
}
