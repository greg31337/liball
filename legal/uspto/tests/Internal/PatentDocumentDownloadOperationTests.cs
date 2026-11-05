using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentDocumentDownloadOperationTests
{
    [Test]
    public void StreamDownloadValidatesMetadataAndReportsIncrementalProgress()
    {
        byte[] body = Encoding.ASCII.GetBytes("0123456789");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) =>
        {
            HttpResponseMessage response = BinaryResponse(HttpStatusCode.OK, body, UsptoMediaTypes.Pdf, "document.pdf");
            response.Headers.ETag = new EntityTagHeaderValue("\"document-v1\"");
            response.Headers.AcceptRanges.Add("bytes");
            response.Content.Headers.LastModified = new DateTimeOffset(2025, 4, 3, 2, 1, 0, TimeSpan.Zero);
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 3);
        using MemoryStream output = new MemoryStream();
        RecordingDownloadProgress progress = new RecordingDownloadProgress();
        DownloadRequest request = new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/document.pdf?token=signed-value"),
            Output = output,
            ExpectedSize = body.Length,
            HasExpectedSize = true,
            Progress = progress
        };

        DownloadResult result = operation.Download("DOCUMENT-1", request);

        Assert.Multiple(() =>
        {
            Assert.That(output.ToArray(), Is.EqualTo(body));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.ApiKeys[0], Is.EqualTo("test-key"));
            Assert.That(handler.Accepts[0], Is.EqualTo("*/*"));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Progress.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Progress.BytesTransferred, Is.EqualTo(body.Length));
            Assert.That(result.Progress.TotalBytes, Is.EqualTo(body.Length));
            Assert.That(result.Progress.HasTotalBytes, Is.True);
            Assert.That(result.Content.FileName, Is.EqualTo("document.pdf"));
            Assert.That(result.Content.MediaType, Is.EqualTo(UsptoMediaTypes.Pdf));
            Assert.That(result.Content.Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(result.Content.ETag, Is.EqualTo("\"document-v1\""));
            Assert.That(result.Content.HasLastModified, Is.True);
            Assert.That(result.Content.AcceptsByteRanges, Is.True);
            Assert.That(result.Content.FinalUri?.AbsoluteUri, Does.Not.Contain("signed-value"));
            Assert.That(result.FailureCode, Is.Null);
            Assert.That(progress.Reports.First().State, Is.EqualTo(DownloadState.Downloading));
            Assert.That(progress.Reports.First().BytesTransferred, Is.Zero);
            Assert.That(progress.Reports.Last().State, Is.EqualTo(DownloadState.Completed));
            Assert.That(progress.Reports.Select(item => item.BytesTransferred), Is.Ordered);
            Assert.That(progress.Reports, Has.Count.GreaterThanOrEqualTo(4));
        });
    }

    [Test]
    public void ConfiguredDownloadHostDoesNotReceiveTheOdpCredential()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(HttpStatusCode.OK, new byte[] { 1 }, UsptoMediaTypes.Pdf, "one.pdf"));
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[] { "download.example.test" }, 8);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download("DOCUMENT-2", new DownloadRequest
        {
            SourceUri = new Uri("https://download.example.test/one.pdf"),
            Output = output
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string?[] { null }));
        });
    }

    [Test]
    public void RedirectTargetIsValidatedAndCrossOriginCredentialsAreRemoved()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) =>
        {
            if (call == 0)
            {
                HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.Redirect);
                redirect.Headers.Location = new Uri("https://download.example.test/final.pdf");
                redirect.Content = new ByteArrayContent(Array.Empty<byte>());
                return redirect;
            }

            return BinaryResponse(HttpStatusCode.OK, Encoding.ASCII.GetBytes("pdf"), UsptoMediaTypes.Pdf, "final.pdf");
        });
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[] { "download.example.test" }, 8);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download("DOCUMENT-3", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/start.pdf"),
            Output = output
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(handler.RequestUris.Select(item => item.AbsoluteUri), Is.EqualTo(new string[]
            {
                "https://api.uspto.gov/start.pdf",
                "https://download.example.test/final.pdf"
            }));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string?[] { "test-key", null }));
            Assert.That(result.Content.FinalUri?.AbsoluteUri, Is.EqualTo("https://download.example.test/final.pdf"));
        });
    }

    [Test]
    public void UnapprovedRedirectIsRejectedBeforeTheTargetIsRequested()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) =>
        {
            HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.Redirect);
            redirect.Headers.Location = new Uri("https://unapproved.example.test/document.pdf");
            redirect.Content = new ByteArrayContent(Array.Empty<byte>());
            return redirect;
        });
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 8);
        using MemoryStream output = new MemoryStream();

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.Download("DOCUMENT-4", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/start.pdf"),
            Output = output
        }));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DOWNLOAD_HOST_NOT_ALLOWED"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase("https://user:password@download.example.test/document.pdf", "DOWNLOAD_USERINFO_REJECTED")]
    [TestCase("https://download.example.test/document.pdf#section", "DOWNLOAD_FRAGMENT_REJECTED")]
    public void UnsafeRedirectTargetIsRejectedBeforeTheTargetIsRequested(string target, string errorCode)
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) =>
        {
            HttpResponseMessage redirect = new HttpResponseMessage(HttpStatusCode.Redirect);
            redirect.Headers.Location = new Uri(target);
            redirect.Content = new ByteArrayContent(Array.Empty<byte>());
            return redirect;
        });
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[] { "download.example.test" }, 8);
        using MemoryStream output = new MemoryStream();

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.Download("DOCUMENT-5", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/start.pdf"),
            Output = output
        }));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [Test]
    public void ExistingDestinationFailsWithoutARequestWhenOverwriteIsDisabled()
    {
        string directory = CreateTemporaryDirectory();
        string destination = Path.Combine(directory, "document.pdf");
        File.WriteAllText(destination, "original");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(HttpStatusCode.OK, Encoding.ASCII.GetBytes("replacement"), UsptoMediaTypes.Pdf, "document.pdf"));
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 8);

        try
        {
            DownloadResult result = operation.Download("DOCUMENT-5", new DownloadRequest
            {
                SourceUri = new Uri("https://api.uspto.gov/document.pdf"),
                DestinationPath = destination
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(result.FailureCode, Is.EqualTo("DESTINATION_EXISTS"));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("original"));
                Assert.That(handler.RequestUris, Is.Empty);
                Assert.That(Directory.GetFiles(directory, "*.partial"), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SuccessfulFileDownloadAtomicallyReplacesAnExistingDestination()
    {
        byte[] body = Encoding.ASCII.GetBytes("replacement-content");
        string directory = CreateTemporaryDirectory();
        string destination = Path.Combine(directory, "document.pdf");
        File.WriteAllText(destination, "original");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(HttpStatusCode.OK, body, UsptoMediaTypes.Pdf, "document.pdf"));
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 4);

        try
        {
            DownloadResult result = operation.Download("DOCUMENT-6", new DownloadRequest
            {
                SourceUri = new Uri("https://api.uspto.gov/document.pdf"),
                DestinationPath = destination,
                OverwriteExisting = true
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(result.FinalPath, Is.EqualTo(Path.GetFullPath(destination)));
                Assert.That(File.ReadAllBytes(destination), Is.EqualTo(body));
                Assert.That(Directory.GetFiles(directory, "*.partial"), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FailedTransferPreservesExistingDestinationAndRemovesTemporaryFile()
    {
        byte[] body = Encoding.ASCII.GetBytes("short");
        string directory = CreateTemporaryDirectory();
        string destination = Path.Combine(directory, "document.pdf");
        File.WriteAllText(destination, "original");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) =>
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamingTestContent(new MemoryStream(body), UsptoMediaTypes.Pdf);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "document.pdf" };
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 2);

        try
        {
            DownloadResult result = operation.Download("DOCUMENT-7", new DownloadRequest
            {
                SourceUri = new Uri("https://api.uspto.gov/document.pdf"),
                DestinationPath = destination,
                OverwriteExisting = true,
                ExpectedSize = body.Length + 1,
                HasExpectedSize = true
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(result.FailureCode, Is.EqualTo("RESPONSE_LENGTH_MISMATCH"));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("original"));
                Assert.That(Directory.GetFiles(directory, "*.partial"), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void UnknownLengthTransferStopsBeforeWritingTheChunkThatCrossesTheLimit()
    {
        CountingReadStream source = new CountingReadStream(Encoding.ASCII.GetBytes("0123456789"), 3);
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) =>
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamingTestContent(source, UsptoMediaTypes.Pdf);
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 3);
        operation.Options.Download.MaximumBytes = 5;
        operation.Options.Download.HasMaximumBytes = true;
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download("DOCUMENT-8", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/document.pdf"),
            Output = output
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo("PAYLOAD_TOO_LARGE"));
            Assert.That(result.Progress.BytesTransferred, Is.EqualTo(3));
            Assert.That(output.Length, Is.EqualTo(3));
            Assert.That(source.BytesRead, Is.EqualTo(6));
        });
    }

    [TestCase(HttpStatusCode.NotFound, UsptoMediaTypes.Json, "HTTP_STATUS_404")]
    [TestCase(HttpStatusCode.OK, UsptoMediaTypes.Json, "UNEXPECTED_MEDIA_TYPE")]
    public void InvalidResponsesReturnTypedFailuresBeforeWriting(HttpStatusCode status, string mediaType, string failureCode)
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(status, Encoding.UTF8.GetBytes("{}"), mediaType, "document.pdf"));
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 8);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download("DOCUMENT-9", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/document.pdf"),
            Output = output
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo(failureCode));
            Assert.That(result.FailureMessage, Is.Not.Empty);
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase("../document.pdf", "INVALID_DOCUMENT_FILENAME")]
    [TestCase("document.zip", "DOCUMENT_FILENAME_FORMAT_MISMATCH")]
    public void UnsafeOrMismatchedResponseFilenameReturnsTypedFailure(string fileName, string failureCode)
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(HttpStatusCode.OK, new byte[] { 1 }, UsptoMediaTypes.Pdf, fileName));
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 8);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download("DOCUMENT-10", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/document.pdf"),
            Output = output
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo(failureCode));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [Test]
    public void DeclaredSizeMismatchReturnsTypedFailureBeforeReadingTheBody()
    {
        CountingReadStream source = new CountingReadStream(Encoding.ASCII.GetBytes("12345"), 2);
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) =>
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(source);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(UsptoMediaTypes.Pdf);
            response.Content.Headers.ContentLength = 5;
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 2);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download("DOCUMENT-11", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/document.pdf"),
            Output = output,
            ExpectedSize = 6,
            HasExpectedSize = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.FailureCode, Is.EqualTo("RESPONSE_LENGTH_MISMATCH"));
            Assert.That(source.ReadCount, Is.Zero);
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase("https://api.uspto.gov/patent.pdf", UsptoMediaTypes.Pdf, null, "patent.pdf", DocumentFormat.Pdf)]
    [TestCase("https://api.uspto.gov/patent.xml", UsptoMediaTypes.TextXml, null, "patent.xml", DocumentFormat.Xml)]
    [TestCase("https://api.uspto.gov/archive.zip", UsptoMediaTypes.Binary, null, "archive.zip", DocumentFormat.Zip)]
    [TestCase("https://api.uspto.gov/download", UsptoMediaTypes.Pdf, "response.pdf", "response.pdf", DocumentFormat.Pdf)]
    [TestCase("https://api.uspto.gov/", UsptoMediaTypes.Pdf, null, "DOCUMENT-12", DocumentFormat.Pdf)]
    public void MimeAndFilenameAlternativesProduceConsistentMetadata(string source, string mediaType, string? responseFileName, string expectedFileName, DocumentFormat expectedFormat)
    {
        byte[] body = Encoding.ASCII.GetBytes("document");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(HttpStatusCode.OK, body, mediaType, responseFileName));
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 8);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download("DOCUMENT-12", new DownloadRequest
        {
            SourceUri = new Uri(source),
            Output = output
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.MediaType, Is.EqualTo(mediaType));
            Assert.That(result.Content.FileName, Is.EqualTo(expectedFileName));
            Assert.That(result.Content.Format, Is.EqualTo(expectedFormat));
            Assert.That(output.ToArray(), Is.EqualTo(body));
        });
    }

    [Test]
    public void ExtendedDispositionFilenameTakesPrecedenceOverTheLegacyFilename()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) =>
        {
            HttpResponseMessage response = BinaryResponse(HttpStatusCode.OK, new byte[] { 1 }, UsptoMediaTypes.Pdf, null);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "legacy.pdf",
                FileNameStar = "preferred.pdf"
            };
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 8);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download("DOCUMENT-13", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/original.pdf"),
            Output = output
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.FileName, Is.EqualTo("preferred.pdf"));
        });
    }

    [Test]
    public void CancellationDuringFileTransferDoesNotCommitOrLeaveAPartialFile()
    {
        string directory = CreateTemporaryDirectory();
        string destination = Path.Combine(directory, "document.pdf");
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        CancelAfterReadStream source = new CancelAfterReadStream(Encoding.ASCII.GetBytes("cancelled-content"), cancellation);
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) =>
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamingTestContent(source, UsptoMediaTypes.Pdf);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "document.pdf" };
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 3);

        try
        {
            Assert.Throws<OperationCanceledException>(() => operation.Download("DOCUMENT-14", new DownloadRequest
            {
                SourceUri = new Uri("https://api.uspto.gov/document.pdf"),
                DestinationPath = destination,
                Context = new RequestContext { CancellationToken = cancellation.Token }
            }));

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(destination), Is.False);
                Assert.That(Directory.GetFiles(directory, "*.partial"), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PreCancelledRequestReturnsCancelledResultWithoutNetworkAccess()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(HttpStatusCode.OK, new byte[] { 1 }, UsptoMediaTypes.Pdf, "document.pdf"));
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 8);
        using MemoryStream output = new MemoryStream();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        DownloadResult result = operation.Download("DOCUMENT-12", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/document.pdf"),
            Output = output,
            Context = new RequestContext { CancellationToken = cancellation.Token }
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Cancelled));
            Assert.That(result.FailureCode, Is.EqualTo("CANCELLED"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    [Test]
    public void RequestRequiresSourceAndExactlyOneWritableDestination()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(HttpStatusCode.OK, new byte[] { 1 }, UsptoMediaTypes.Pdf, "document.pdf"));
        using HttpClient client = new HttpClient(handler);
        PatentDocumentDownloadOperation operation = CreateOperation(client, new string[0], 8);
        using MemoryStream output = new MemoryStream();
        using MemoryStream readOnly = new MemoryStream(new byte[1], false);

        ValidationException noSource = Assert.Throws<ValidationException>(() => operation.Download("DOCUMENT-13", new DownloadRequest { Output = output }));
        ValidationException noDestination = Assert.Throws<ValidationException>(() => operation.Download("DOCUMENT-13", new DownloadRequest { SourceUri = new Uri("https://api.uspto.gov/document.pdf") }));
        ValidationException twoDestinations = Assert.Throws<ValidationException>(() => operation.Download("DOCUMENT-13", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/document.pdf"),
            Output = output,
            DestinationPath = Path.Combine(Path.GetTempPath(), "document.pdf")
        }));
        ValidationException notWritable = Assert.Throws<ValidationException>(() => operation.Download("DOCUMENT-13", new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/document.pdf"),
            Output = readOnly
        }));

        Assert.Multiple(() =>
        {
            Assert.That(noSource.ErrorCode, Is.EqualTo("REQUIRED"));
            Assert.That(noDestination.ErrorCode, Is.EqualTo("DESTINATION_REQUIRED"));
            Assert.That(twoDestinations.ErrorCode, Is.EqualTo("DESTINATION_REQUIRED"));
            Assert.That(notWritable.ErrorCode, Is.EqualTo("STREAM_NOT_WRITABLE"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    private static PatentDocumentDownloadOperation CreateOperation(HttpClient client, string[] permittedHosts, int bufferSize)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            HttpClient = client,
            OdpApiKey = "test-key",
            PermittedDownloadHosts = permittedHosts,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            Download = new DownloadPolicy
            {
                TimeoutMilliseconds = 5000,
                BufferSize = bufferSize,
                RedirectHopCount = 3
            }
        };
        PatentDocumentDownloadOperation operation = new PatentDocumentDownloadOperation(client, options);
        operation.HostPolicy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
        return operation;
    }

    private static HttpResponseMessage BinaryResponse(HttpStatusCode status, byte[] body, string mediaType, string? fileName)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Content = new ByteArrayContent(body);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        if (fileName != null)
        {
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
        }

        return response;
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "uspto-document-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}

internal class RecordingPatentDocumentDownloadHandler : HttpMessageHandler
{
    internal Func<int, HttpRequestMessage, HttpResponseMessage> ResponseFactory;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();

    internal RecordingPatentDocumentDownloadHandler(Func<int, HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        ResponseFactory = responseFactory;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int call = RequestUris.Count;
        RequestUris.Add(request.RequestUri!);
        string? apiKey = null;
        if (request.Headers.TryGetValues(UsptoHeaders.OdpApiKey, out IEnumerable<string>? values))
        {
            apiKey = values.Single();
        }

        ApiKeys.Add(apiKey);
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        HttpResponseMessage response = ResponseFactory(call, request);
        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}
