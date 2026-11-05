using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PatentApplicationExportOperationTests
{
    [Test]
    public void CsvExportStreamsTheRequestedSearchAndReturnsValidatedMetadata()
    {
        string csv = FixtureLoader.LoadText("patent-applications/export.csv");
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, csv, UsptoMediaTypes.Csv, "patent-results.csv");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationExportOperation operation = new PatentApplicationExportOperation(client, CreateOptions(client));
        SearchRequest search = CreateSearch();
        using MemoryStream output = new MemoryStream();
        RecordingDownloadProgress progress = new RecordingDownloadProgress();
        DownloadRequest download = new DownloadRequest
        {
            ExportFormat = ExportFormat.Csv,
            Output = output,
            Progress = progress
        };

        DownloadResult result = operation.DownloadSearch(search, download);

        using JsonDocument body = JsonDocument.Parse(handler.RequestBody!);
        Assert.Multiple(() =>
        {
            Assert.That(handler.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.RequestUri?.AbsoluteUri, Is.EqualTo("https://example.test/root/api/v1/patent/applications/search/download"));
            Assert.That(handler.RequestContentType, Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(handler.Accept, Is.EqualTo(UsptoMediaTypes.Csv));
            Assert.That(handler.ApiKey, Is.EqualTo("test-key"));
            Assert.That(body.RootElement.GetProperty("q").GetString(), Is.EqualTo("applicationMetaData.inventionTitle:battery"));
            Assert.That(body.RootElement.GetProperty("pagination").GetProperty("offset").GetInt64(), Is.EqualTo(25L));
            Assert.That(body.RootElement.GetProperty("pagination").GetProperty("limit").GetInt32(), Is.EqualTo(10));
            Assert.That(body.RootElement.GetProperty("format").GetString(), Is.EqualTo("csv"));
            Assert.That(Encoding.UTF8.GetString(output.ToArray()), Is.EqualTo(csv));
            Assert.That(output.CanWrite, Is.True);
            Assert.That(result.Request, Is.SameAs(download));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Progress.BytesTransferred, Is.EqualTo(Encoding.UTF8.GetByteCount(csv)));
            Assert.That(result.Content.FileName, Is.EqualTo("patent-results.csv"));
            Assert.That(result.Content.MediaType, Is.EqualTo(UsptoMediaTypes.Csv));
            Assert.That(result.Content.Format, Is.EqualTo(DocumentFormat.Csv));
            Assert.That(result.Content.FinalUri?.AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/search/download"));
            Assert.That(result.Content.HasContentLength, Is.True);
            Assert.That(progress.Reports, Has.Count.EqualTo(1));
            Assert.That(progress.Reports[0], Is.SameAs(result.Progress));
        });
    }

    [Test]
    public void JsonExportStreamsDirectlyToAValidatedDestinationFile()
    {
        string json = FixtureLoader.LoadText("patent-applications/export.json");
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, json, UsptoMediaTypes.Json, "applications.json");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationExportOperation operation = new PatentApplicationExportOperation(client, CreateOptions(client));
        string destination = Path.Combine(Path.GetTempPath(), "uspto-export-" + Guid.NewGuid().ToString("N") + ".json");
        DownloadRequest download = new DownloadRequest { DestinationPath = destination };

        try
        {
            DownloadResult result = operation.DownloadSearch(CreateSearch(), download);

            Assert.Multiple(() =>
            {
                Assert.That(handler.Accept, Is.EqualTo(UsptoMediaTypes.Json));
                Assert.That(handler.RequestBody, Does.Contain("\"format\":\"json\""));
                Assert.That(File.ReadAllText(destination), Is.EqualTo(json));
                Assert.That(result.FinalPath, Is.EqualTo(destination));
                Assert.That(result.Content.FileName, Is.EqualTo("applications.json"));
                Assert.That(result.Content.Format, Is.EqualTo(DocumentFormat.Json));
            });
        }
        finally
        {
            File.Delete(destination);
        }
    }

    [Test]
    public void UnexpectedMediaTypeIsRejectedBeforeWritingOutput()
    {
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, "{}", UsptoMediaTypes.Json, "applications.csv");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationExportOperation operation = new PatentApplicationExportOperation(client, CreateOptions(client));
        using MemoryStream output = new MemoryStream();
        DownloadRequest download = new DownloadRequest { ExportFormat = ExportFormat.Csv, Output = output };

        ApiException exception = Assert.Throws<ApiException>(() => operation.DownloadSearch(CreateSearch(), download));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("UNEXPECTED_MEDIA_TYPE"));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [TestCase("../applications.csv", "INVALID_EXPORT_FILENAME")]
    [TestCase("applications.json", "EXPORT_FILENAME_FORMAT_MISMATCH")]
    public void UnsafeOrMismatchedResponseFileNamesAreRejectedBeforeWritingOutput(string fileName, string errorCode)
    {
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, "a,b\r\n", UsptoMediaTypes.Csv, fileName);
        using HttpClient client = new HttpClient(handler);
        PatentApplicationExportOperation operation = new PatentApplicationExportOperation(client, CreateOptions(client));
        using MemoryStream output = new MemoryStream();
        DownloadRequest download = new DownloadRequest { ExportFormat = ExportFormat.Csv, Output = output };

        ApiException exception = Assert.Throws<ApiException>(() => operation.DownloadSearch(CreateSearch(), download));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [Test]
    public void PayloadTooLargeResponseUsesTheSharedErrorMapping()
    {
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(
            HttpStatusCode.RequestEntityTooLarge,
            "{\"code\":413,\"message\":\"Payload Too Large\",\"detailedMessage\":\"reduce the result set\",\"requestIdentifier\":\"request-413\"}",
            UsptoMediaTypes.Json,
            null);
        using HttpClient client = new HttpClient(handler);
        PatentApplicationExportOperation operation = new PatentApplicationExportOperation(client, CreateOptions(client));
        using MemoryStream output = new MemoryStream();
        DownloadRequest download = new DownloadRequest { Output = output };

        ApiException exception = Assert.Throws<ApiException>(() => operation.DownloadSearch(CreateSearch(), download));

        Assert.Multiple(() =>
        {
            Assert.That(exception.StatusCode, Is.EqualTo(413));
            Assert.That(exception.ErrorCode, Is.EqualTo("413"));
            Assert.That(exception.ErrorDetails, Is.EqualTo("reduce the result set"));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("request-413"));
            Assert.That(output.Length, Is.Zero);
        });
    }

    [Test]
    public void ExportRequiresExactlyOneValidDestinationAndASupportedFormat()
    {
        RecordingPatentExportHandler handler = new RecordingPatentExportHandler(HttpStatusCode.OK, "{}", UsptoMediaTypes.Json, "applications.json");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationExportOperation operation = new PatentApplicationExportOperation(client, CreateOptions(client));
        DownloadRequest missing = new DownloadRequest();
        DownloadRequest invalidFormat = new DownloadRequest { Output = new MemoryStream(), ExportFormat = (ExportFormat)99 };
        DownloadRequest wrongExtension = new DownloadRequest { DestinationPath = Path.Combine(Path.GetTempPath(), "applications.txt") };

        ValidationException destination = Assert.Throws<ValidationException>(() => operation.DownloadSearch(CreateSearch(), missing));
        ValidationException format = Assert.Throws<ValidationException>(() => operation.DownloadSearch(CreateSearch(), invalidFormat));
        ValidationException extension = Assert.Throws<ValidationException>(() => operation.DownloadSearch(CreateSearch(), wrongExtension));
        invalidFormat.Output?.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(destination.ErrorCode, Is.EqualTo("DESTINATION_REQUIRED"));
            Assert.That(format.ErrorCode, Is.EqualTo("UNSUPPORTED_FORMAT"));
            Assert.That(extension.ErrorCode, Is.EqualTo("EXPORT_FILENAME_FORMAT_MISMATCH"));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    private static SearchRequest CreateSearch()
    {
        return new SearchRequest
        {
            Query = "applicationMetaData.inventionTitle:battery",
            OffsetPage = new OffsetPage { Offset = 25L, Limit = 10 },
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "applicationNumberText", Direction = SortDirection.Ascending }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "applicationNumberText", "applicationMetaData.inventionTitle" }
            }
        };
    }

    private static UsptoClientOptions CreateOptions(HttpClient client)
    {
        return new UsptoClientOptions
        {
            OdpBaseUri = new Uri("https://example.test/root/"),
            OdpApiKey = "test-key",
            HttpClient = client,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            OverallTimeoutMilliseconds = 5000,
            Download = new DownloadPolicy
            {
                TimeoutMilliseconds = 5000,
                BufferSize = 8
            }
        };
    }
}

internal class RecordingPatentExportHandler : HttpMessageHandler
{
    internal HttpStatusCode StatusCode;
    internal string ResponseBody;
    internal string MediaType;
    internal string? FileName;
    internal HttpMethod? Method;
    internal Uri? RequestUri;
    internal string? RequestBody;
    internal string? RequestContentType;
    internal string? Accept;
    internal string? ApiKey;
    internal int CallCount = 0;

    internal RecordingPatentExportHandler(HttpStatusCode statusCode, string responseBody, string mediaType, string? fileName)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        MediaType = mediaType;
        FileName = fileName;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount += 1;
        Method = request.Method;
        RequestUri = request.RequestUri;
        RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
        RequestContentType = request.Content.Headers.ContentType?.MediaType;
        Accept = request.Headers.Accept.Single().MediaType;
        ApiKey = request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single();
        HttpResponseMessage response = new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(ResponseBody, Encoding.UTF8, MediaType),
            RequestMessage = request
        };
        if (FileName != null)
        {
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = FileName
            };
        }

        return response;
    }
}
