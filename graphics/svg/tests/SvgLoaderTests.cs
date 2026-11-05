namespace svg.Tests;

public class SvgLoaderTests
{
    [Test]
    public void LoadOptionsInstallIndependentSecureDefaults()
    {
        SvgLoadOptions first = new SvgLoadOptions();
        SvgLoadOptions second = new SvgLoadOptions();
        first.Limits.Input.MaxInputBytes = 7;
        first.ResourcePolicy.AllowCallerResolvedRootUri = false;

        Assert.Multiple(() =>
        {
            Assert.That(first.Mode, Is.EqualTo(SvgProcessingMode.SecureStatic));
            Assert.That(first.DiagnosticBehavior, Is.EqualTo(SvgDiagnosticBehavior.Strict));
            Assert.That(first.PreserveSource, Is.True);
            Assert.That(first.PreserveUnknown, Is.True);
            Assert.That(first.CopyInputData, Is.True);
            Assert.That(first.DtdPolicy, Is.EqualTo(SvgDtdPolicy.Prohibit));
            Assert.That(first.BaseUri, Is.Empty);
            Assert.That(first.ContentType, Is.Empty);
            Assert.That(first.ResourceProvider, Is.Null);
            Assert.That(second.Limits.Input.MaxInputBytes, Is.EqualTo(33554432));
            Assert.That(second.ResourcePolicy.AllowCallerResolvedRootUri, Is.True);
        });
    }

    [Test]
    public void CharacterStringAndSpanInputsProduceBoundedSnapshots()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = "memory:root";
        options.ContentType = "image/svg+xml";
        string text = "<svg/>";

        SvgLoadResult fromString = new SvgLoader().LoadWithStringAndSvgLoadOptions(text, options);
        SvgLoadResult fromSpan = new SvgLoader().LoadWithReadOnlySpanOfCharAndSvgLoadOptions(text.AsSpan(1, 4), options);

        Assert.Multiple(() =>
        {
            Assert.That(fromString.Success, Is.True);
            Assert.That(fromString.Source.Kind, Is.EqualTo(SvgLoadSourceKind.Characters));
            Assert.That(fromString.Source.Characters, Is.SameAs(text));
            Assert.That(fromString.Source.SourceUri, Is.EqualTo("memory:root"));
            Assert.That(fromString.Source.CallerRetainsOwnership, Is.True);
            Assert.That(fromString.Source.IsIndependentSnapshot, Is.True);
            Assert.That(fromString.Source.Length, Is.EqualTo(6));
            Assert.That(fromString.DetectedMediaType, Is.EqualTo("image/svg+xml"));
            Assert.That(fromString.Document, Is.Not.Null);
            Assert.That(fromSpan.Source.Characters, Is.EqualTo("svg/"));
            Assert.That(fromSpan.Source.IsIndependentSnapshot, Is.True);
        });
    }

    [Test]
    public void ByteArrayCopyPolicyAndByteSpansHaveExplicitOwnership()
    {
        byte[] original = System.Text.Encoding.UTF8.GetBytes("<svg/>");
        SvgLoadOptions copying = new SvgLoadOptions();
        SvgLoadResult copied = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(original, copying);
        original[0] = (byte)'!';

        SvgLoadOptions retaining = new SvgLoadOptions();
        retaining.CopyInputData = false;
        SvgLoadResult retained = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(original, retaining);
        SvgLoadResult span = new SvgLoader().LoadWithReadOnlySpanOfByteAndSvgLoadOptions(new System.ReadOnlySpan<byte>(original, 1, 4), retaining);
        original[1] = (byte)'?';

        Assert.Multiple(() =>
        {
            Assert.That(copied.Source.Bytes, Is.Not.SameAs(original));
            Assert.That(copied.Source.Bytes[0], Is.EqualTo((byte)'<'));
            Assert.That(copied.Source.CallerRetainsOwnership, Is.True);
            Assert.That(copied.Source.IsIndependentSnapshot, Is.True);
            Assert.That(retained.Source.Bytes, Is.SameAs(original));
            Assert.That(retained.Source.Bytes[1], Is.EqualTo((byte)'?'));
            Assert.That(retained.Source.IsIndependentSnapshot, Is.False);
            Assert.That(span.Source.Bytes, Is.EqualTo(new byte[] { (byte)'s', (byte)'v', (byte)'g', (byte)'/' }));
            Assert.That(span.Source.IsIndependentSnapshot, Is.True);
        });
    }

    [Test]
    public void StreamsAndTextReadersRemainOpenAndAreReadFromCurrentPositions()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("xx<svg/>");
        System.IO.MemoryStream stream = new System.IO.MemoryStream(bytes);
        stream.Position = 2;
        System.IO.StringReader reader = new System.IO.StringReader("yy<svg/>");
        reader.Read();
        reader.Read();

        SvgLoadResult streamResult = new SvgLoader().LoadWithStreamAndOptions(stream, new SvgLoadOptions());
        SvgLoadResult readerResult = new SvgLoader().LoadWithTextReaderAndSvgLoadOptions(reader, new SvgLoadOptions());
        stream.Position = 0;
        int readerEnd = reader.Peek();

        Assert.Multiple(() =>
        {
            Assert.That(streamResult.Success, Is.True);
            Assert.That(System.Text.Encoding.UTF8.GetString(streamResult.Source.Bytes), Is.EqualTo("<svg/>"));
            Assert.That(streamResult.Source.CallerRetainsOwnership, Is.True);
            Assert.That(streamResult.Source.IsIndependentSnapshot, Is.True);
            Assert.That(stream.CanRead, Is.True);
            Assert.That(readerResult.Source.Characters, Is.EqualTo("<svg/>"));
            Assert.That(readerResult.Source.CallerRetainsOwnership, Is.True);
            Assert.That(readerResult.Source.IsIndependentSnapshot, Is.True);
            Assert.That(readerEnd, Is.EqualTo(-1));
        });

        stream.Dispose();
        reader.Dispose();
    }

    [Test]
    public void XmlReadersAreConsumedWithoutBeingClosedOrOwned()
    {
        System.Xml.XmlReader reader = System.Xml.XmlReader.Create(new System.IO.StringReader("<svg/>"));
        System.Xml.ReadState before = reader.ReadState;

        SvgLoadResult result = new SvgLoader().LoadWithXmlReaderAndSvgLoadOptions(reader, new SvgLoadOptions());
        System.Xml.ReadState after = reader.ReadState;
        bool canRead = reader.Read();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Source.Kind, Is.EqualTo(SvgLoadSourceKind.XmlReader));
            Assert.That(result.Source.XmlReader, Is.SameAs(reader));
            Assert.That(result.Source.CallerRetainsOwnership, Is.True);
            Assert.That(result.Source.IsIndependentSnapshot, Is.False);
            Assert.That(before, Is.EqualTo(System.Xml.ReadState.Initial));
            Assert.That(after, Is.EqualTo(System.Xml.ReadState.EndOfFile));
            Assert.That(canRead, Is.False);
            Assert.That(result.Document, Is.Not.Null);
            Assert.That(reader.Settings.CloseInput, Is.False);
        });

        reader.Dispose();
    }

    [Test]
    public void FilesystemPathsAreTheOnlyDirectIoEntryPoint()
    {
        string path = System.IO.Path.GetTempFileName();
        try
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes("<svg id='file'/>");
            System.IO.File.WriteAllBytes(path, bytes);

            SvgLoadResult result = new SvgLoader().LoadPath(path, new SvgLoadOptions());
            SvgLoadResult missing = new SvgLoader().LoadPath(path + ".missing", new SvgLoadOptions());

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Source.Bytes, Is.EqualTo(bytes));
                Assert.That(result.Source.SourceUri, Is.EqualTo(System.IO.Path.GetFullPath(path)));
                Assert.That(result.Source.CallerRetainsOwnership, Is.False);
                Assert.That(result.Source.IsIndependentSnapshot, Is.True);
                Assert.That(missing.Success, Is.False);
                Assert.That(missing.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.ResourceFailed));
            });
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Test]
    public void UriLoadingUsesOnlyTheCallbackAndCopiesResponseMetadata()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        SvgResourceRequest observed = null;
        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes("<svg/>");
        options.ResourceProvider = new SvgResourceProvider(request =>
        {
            observed = request;
            SvgResourceResponse response = new SvgResourceResponse();
            response.Bytes = responseBytes;
            response.DeclaredMediaType = "application/xml";
            response.ActualMediaType = "image/svg+xml";
            response.FinalUri = "https://cdn.example/root.svg";
            response.Cacheable = true;
            response.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Info, SvgDiagnosticCode.None, "callback metadata", null));
            return response;
        });

        SvgLoadResult result = new SvgLoader().LoadUri("https://example.test/root.svg", options);
        responseBytes[0] = (byte)'!';

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(observed, Is.Not.Null);
            Assert.That(observed.SourceUri, Is.EqualTo("https://example.test/root.svg"));
            Assert.That(observed.NormalizedUri, Is.EqualTo(observed.SourceUri));
            Assert.That(observed.MediaKind, Is.EqualTo("image/svg+xml"));
            Assert.That(observed.ReferenceDepth, Is.Zero);
            Assert.That(result.Source.SourceUri, Is.EqualTo("https://cdn.example/root.svg"));
            Assert.That(result.Source.Bytes[0], Is.EqualTo((byte)'<'));
            Assert.That(result.DetectedMediaType, Is.EqualTo("image/svg+xml"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Resources.RequestCount, Is.EqualTo(1));
            Assert.That(result.Resources.LoadedCount, Is.EqualTo(1));
            Assert.That(result.Resources.EncodedBytes, Is.EqualTo(6));
        });
    }

    [Test]
    public void CallbackStreamsRemainOpenAndInvalidUriResponsesFailStably()
    {
        System.IO.MemoryStream responseStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("<svg/>"));
        SvgLoadOptions streamOptions = new SvgLoadOptions();
        streamOptions.ResourceProvider = new SvgResourceProvider(request =>
        {
            SvgResourceResponse response = new SvgResourceResponse();
            response.Stream = responseStream;
            return response;
        });
        SvgLoadResult streamed = new SvgLoader().LoadUri("memory:svg", streamOptions);
        responseStream.Position = 0;

        SvgLoadOptions missingOptions = new SvgLoadOptions();
        SvgLoadResult missing = new SvgLoader().LoadUri("memory:svg", missingOptions);
        SvgLoadOptions deniedOptions = new SvgLoadOptions();
        deniedOptions.ResourcePolicy.AllowCallerResolvedRootUri = false;
        deniedOptions.ResourceProvider = new SvgResourceProvider(request => new SvgResourceResponse());
        SvgLoadResult denied = new SvgLoader().LoadUri("memory:svg", deniedOptions);
        SvgLoadOptions invalidOptions = new SvgLoadOptions();
        invalidOptions.ResourceProvider = new SvgResourceProvider(request => new SvgResourceResponse());
        SvgLoadResult invalid = new SvgLoader().LoadUri("memory:svg", invalidOptions);

        Assert.Multiple(() =>
        {
            Assert.That(streamed.Success, Is.True);
            Assert.That(responseStream.CanRead, Is.True);
            Assert.That(missing.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.CallbackRequired));
            Assert.That(denied.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.ResourceDenied));
            Assert.That(invalid.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.ResourceFailed));
            Assert.That(invalid.Resources.RequestCount, Is.EqualTo(1));
        });

        responseStream.Dispose();
    }

    [Test]
    public void InvalidArgumentsLimitsAndCancellationReturnDiagnostics()
    {
        SvgLoadOptions characterLimit = new SvgLoadOptions();
        characterLimit.Limits.Input.MaxInputCharacters = 3;
        SvgLoadOptions byteLimit = new SvgLoadOptions();
        byteLimit.Limits.Input.MaxInputBytes = 2;
        SvgLoadOptions invalidLimits = new SvgLoadOptions();
        invalidLimits.Limits.Input.MaxInputBytes = 0;
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        cancellation.Cancel();
        SvgLoadOptions cancelled = new SvgLoadOptions();
        cancelled.CancellationToken = cancellation.Token;

        SvgLoadResult nullText = new SvgLoader().LoadWithStringAndSvgLoadOptions((string)null, new SvgLoadOptions());
        SvgLoadResult tooManyCharacters = new SvgLoader().LoadWithStringAndSvgLoadOptions("four", characterLimit);
        SvgLoadResult tooManyBytes = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(new byte[] { 1, 2, 3 }, byteLimit);
        SvgLoadResult badOptions = new SvgLoader().LoadWithStringAndSvgLoadOptions("ok", invalidLimits);
        SvgLoadResult cancelledResult = new SvgLoader().LoadWithStringAndSvgLoadOptions("ok", cancelled);
        SvgLoadResult nullOptions = new SvgLoader().LoadWithStringAndSvgLoadOptions("ok", null);

        Assert.Multiple(() =>
        {
            Assert.That(nullText.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
            Assert.That(tooManyCharacters.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(tooManyBytes.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(badOptions.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
            Assert.That(cancelledResult.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
            Assert.That(nullOptions.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
        });

        cancellation.Dispose();
    }
}
