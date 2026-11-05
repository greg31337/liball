using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentDocumentListingOperationTests
{
    [Test]
    public void ListingNormalizesTheApplicationAndMapsDocumentsAndDownloads()
    {
        RecordingDocumentListHandler handler = new RecordingDocumentListHandler(HttpStatusCode.OK, CompleteResponse());
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentDocumentList result = operation.GetDocuments("16/123,123", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/16123123/documents"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new string[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new string[] { UsptoMediaTypes.Json }));
            Assert.That(result.ApplicationNumber, Is.EqualTo("16123123"));
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Documents, Has.Length.EqualTo(2));
            Assert.That(result.RequestIdentifier, Is.EqualTo("document-request"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.UnknownFields["futureResponse"], Is.EqualTo("{\"version\":2}"));
        });

        PatentDocument first = result.Documents[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.ApplicationNumber, Is.EqualTo("16123123"));
            Assert.That(first.DocumentIdentifier, Is.EqualTo("LDXBTPQ7XBLUEX3"));
            Assert.That(first.Code, Is.EqualTo("WFEE"));
            Assert.That(first.Description, Is.EqualTo("Fee Worksheet (SB06)"));
            Assert.That(first.DirectionCategory, Is.EqualTo("INTERNAL"));
            Assert.That(first.OfficialDate, Is.EqualTo(new DateTimeOffset(2020, 8, 31, 1, 20, 29, TimeSpan.FromHours(-4))));
            Assert.That(first.HasOfficialDate, Is.True);
            Assert.That(first.FiledAt, Is.EqualTo(new DateTimeOffset(2020, 8, 30, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(first.HasFiledAt, Is.True);
            Assert.That(first.CreatedAt, Is.EqualTo(new DateTimeOffset(2020, 8, 31, 6, 0, 0, TimeSpan.Zero)));
            Assert.That(first.HasCreatedAt, Is.True);
            Assert.That(first.LastModifiedAt, Is.EqualTo(new DateTimeOffset(2020, 9, 1, 7, 30, 0, TimeSpan.Zero)));
            Assert.That(first.HasLastModifiedAt, Is.True);
            Assert.That(first.DownloadAlternatives, Has.Length.EqualTo(2));
            Assert.That(first.UnknownFields["futureDocument"], Is.EqualTo("true"));
        });

        PatentDocumentDownloadAlternative pdf = first.DownloadAlternatives[0];
        Assert.Multiple(() =>
        {
            Assert.That(pdf.MimeTypeIdentifier, Is.EqualTo("PDF"));
            Assert.That(pdf.MediaType, Is.EqualTo(UsptoMediaTypes.Pdf));
            Assert.That(pdf.Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(pdf.DownloadUri, Is.EqualTo(new Uri("https://api.uspto.gov/api/v1/patent/application/documents/16123123/LDXBTPQ7XBLUEX3.pdf")));
            Assert.That(pdf.FileName, Is.EqualTo("fee-worksheet.pdf"));
            Assert.That(pdf.PageCount, Is.EqualTo(2));
            Assert.That(pdf.HasPageCount, Is.True);
            Assert.That(pdf.Size, Is.EqualTo(4294967296L));
            Assert.That(pdf.HasSize, Is.True);
            Assert.That(pdf.UnknownFields["futureDownload"], Is.EqualTo("\"retained\""));
        });

        PatentDocumentDownloadAlternative xml = first.DownloadAlternatives[1];
        Assert.Multiple(() =>
        {
            Assert.That(xml.MimeTypeIdentifier, Is.EqualTo("application/xml; charset=utf-8"));
            Assert.That(xml.MediaType, Is.EqualTo(UsptoMediaTypes.Xml));
            Assert.That(xml.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(xml.PageCount, Is.Zero);
            Assert.That(xml.HasPageCount, Is.True);
            Assert.That(xml.Size, Is.Zero);
            Assert.That(xml.HasSize, Is.True);
        });

        PatentDocument second = result.Documents[1];
        Assert.Multiple(() =>
        {
            Assert.That(second.ApplicationNumber, Is.EqualTo("16123123"));
            Assert.That(second.HasOfficialDate, Is.False);
            Assert.That(second.HasFiledAt, Is.False);
            Assert.That(second.HasCreatedAt, Is.False);
            Assert.That(second.HasLastModifiedAt, Is.False);
            Assert.That(second.DownloadAlternatives[0].MimeTypeIdentifier, Is.EqualTo("BINARY"));
            Assert.That(second.DownloadAlternatives[0].MediaType, Is.Null);
            Assert.That(second.DownloadAlternatives[0].Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(second.DownloadAlternatives[0].HasPageCount, Is.False);
            Assert.That(second.DownloadAlternatives[0].HasSize, Is.False);
        });
    }

    [Test]
    public void GrantIdentifierResolvesBeforeDocumentListing()
    {
        RecordingDocumentListHandler handler = new RecordingDocumentListHandler(
            HttpStatusCode.OK,
            "{\"documentBag\":[]}",
            "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"16/123,123\"}]}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        PatentDocumentList result = operation.GetDocuments("US 12,156,579 B2", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
            Assert.That(handler.RequestUris[0].AbsolutePath, Does.EndWith("/search"));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/patent/applications/16123123/documents"));
            Assert.That(result.ApplicationNumber, Is.EqualTo("16123123"));
            Assert.That(result.Documents, Is.Empty);
        });
    }

    [Test]
    public void EmptyListingUsesHeaderRequestIdentifierAndEmptyDefaults()
    {
        PatentDocumentList result = Parse("{\"documentBag\":[]}");
        PatentDocument document = new PatentDocument();
        PatentDocumentDownloadAlternative alternative = new PatentDocumentDownloadAlternative();

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("16123123"));
            Assert.That(result.Count, Is.Zero);
            Assert.That(result.Documents, Is.Empty);
            Assert.That(result.RequestIdentifier, Is.EqualTo("header-request"));
            Assert.That(document.DownloadAlternatives, Is.Empty);
            Assert.That(document.HasOfficialDate, Is.False);
            Assert.That(document.HasFiledAt, Is.False);
            Assert.That(document.HasCreatedAt, Is.False);
            Assert.That(document.HasLastModifiedAt, Is.False);
            Assert.That(alternative.Format, Is.EqualTo(DocumentFormat.Unknown));
            Assert.That(alternative.HasPageCount, Is.False);
            Assert.That(alternative.HasSize, Is.False);
        });
    }

    [TestCase("{}", "$.documentBag")]
    [TestCase("{\"documentBag\":null}", "$.documentBag")]
    [TestCase("{\"documentBag\":{}}", "$.documentBag")]
    [TestCase("{\"count\":2,\"documentBag\":[]}", "$.count")]
    [TestCase("{\"documentBag\":[false]}", "$.documentBag[0]")]
    [TestCase("{\"documentBag\":[{\"applicationNumberText\":\"17/123,123\"}]}", "$.documentBag[0].applicationNumberText")]
    public void MalformedListingContainersReportTheirExactPath(string response, string expectedPath)
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(exception.SourceName, Is.EqualTo("ODP patent document listing"));
            Assert.That(exception.DataPath, Is.EqualTo(expectedPath));
        });
    }

    [TestCase("\"officialDate\":\"not-a-date\"", "officialDate")]
    [TestCase("\"documentFilingDate\":{}", "documentFilingDate")]
    [TestCase("\"documentIdentifier\":{}", "documentIdentifier")]
    [TestCase("\"downloadOptionBag\":{}", "downloadOptionBag")]
    [TestCase("\"downloadOptionBag\":[false]", "downloadOptionBag[0]")]
    public void MalformedDocumentValuesReportTheirExactPath(string documentFragment, string relativePath)
    {
        string response = "{\"documentBag\":[{" + documentFragment + "}]}";

        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.That(exception.DataPath, Is.EqualTo("$.documentBag[0]." + relativePath));
    }

    [TestCase("\"mimeTypeIdentifier\":{}", "mimeTypeIdentifier")]
    [TestCase("\"downloadUrl\":\"relative/document.pdf\"", "downloadUrl")]
    [TestCase("\"pageTotalQuantity\":{}", "pageTotalQuantity")]
    [TestCase("\"pageTotalQuantity\":-1", "pageTotalQuantity")]
    [TestCase("\"documentSizeQuantity\":{}", "documentSizeQuantity")]
    [TestCase("\"documentSizeQuantity\":-1", "documentSizeQuantity")]
    public void MalformedDownloadValuesReportTheirExactPath(string downloadFragment, string fieldName)
    {
        string response = "{\"documentBag\":[{\"downloadOptionBag\":[{" + downloadFragment + "}]}]}";

        ParseException exception = Assert.Throws<ParseException>(() => Parse(response));

        Assert.That(exception.DataPath, Is.EqualTo("$.documentBag[0].downloadOptionBag[0]." + fieldName));
    }

    [Test]
    public void ListingMapsNotFoundUsingTheCanonicalApplicationNumber()
    {
        RecordingDocumentListHandler handler = new RecordingDocumentListHandler(
            HttpStatusCode.NotFound,
            "{\"message\":\"not found\",\"requestIdentifier\":\"missing-documents\"}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));

        NotFoundException exception = Assert.Throws<NotFoundException>(() => operation.GetNormalizedDocuments("16/123,123", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ResourceIdentifier, Is.EqualTo("16123123"));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("missing-documents"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void CancellationStopsBeforeDocumentListing()
    {
        RecordingDocumentListHandler handler = new RecordingDocumentListHandler(HttpStatusCode.OK, "{\"documentBag\":[]}");
        using HttpClient client = new HttpClient(handler);
        PatentApplicationRetrievalOperation operation = new PatentApplicationRetrievalOperation(client, CreateOptions(client));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => operation.GetDocuments("16/123,123", new RequestContext { CancellationToken = cancellation.Token }));

        Assert.That(handler.RequestUris, Is.Empty);
    }

    private static PatentDocumentList Parse(string response)
    {
        return OdpPatentDocumentParser.Parse(response, "16123123", new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/patent/applications/16123123/documents"),
            RequestIdentifier = "header-request",
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json
        });
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
            OverallTimeoutMilliseconds = 5000
        };
    }

    private static string CompleteResponse()
    {
        return "{\"count\":2,\"documentBag\":[{\"applicationNumberText\":\"16/123,123\",\"officialDate\":\"2020-08-31T01:20:29.000-0400\",\"documentFilingDate\":\"2020-08-30\",\"documentCreateDateTime\":\"2020-08-31T06:00:00Z\",\"documentLastModifiedDateTime\":\"2020-09-01T07:30:00Z\",\"documentIdentifier\":\"LDXBTPQ7XBLUEX3\",\"documentCode\":\"WFEE\",\"documentCodeDescriptionText\":\"Fee Worksheet (SB06)\",\"documentDirectionCategory\":\"INTERNAL\",\"downloadOptionBag\":[{\"mimeTypeIdentifier\":\"PDF\",\"downloadUrl\":\"https://api.uspto.gov/api/v1/patent/application/documents/16123123/LDXBTPQ7XBLUEX3.pdf\",\"fileName\":\"fee-worksheet.pdf\",\"pageTotalQuantity\":\"2\",\"documentSizeQuantity\":\"4294967296\",\"futureDownload\":\"retained\"},{\"mimeTypeIdentifier\":\"application/xml; charset=utf-8\",\"documentURI\":\"https://api.uspto.gov/document.xml\",\"pageCount\":0,\"fileSize\":0}],\"futureDocument\":true},{\"documentIdentifier\":\"ARCHIVE1\",\"officialDate\":null,\"downloadOptionBag\":[{\"mimeTypeIdentifier\":\"BINARY\",\"downloadURI\":\"https://api.uspto.gov/archive.zip\"}]}],\"requestIdentifier\":\"document-request\",\"futureResponse\":{\"version\":2}}";
    }
}

internal class RecordingDocumentListHandler : HttpMessageHandler
{
    internal HttpStatusCode DocumentStatusCode;
    internal string DocumentResponse;
    internal string LookupResponse;
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();

    internal RecordingDocumentListHandler(HttpStatusCode documentStatusCode, string documentResponse)
    {
        DocumentStatusCode = documentStatusCode;
        DocumentResponse = documentResponse;
        LookupResponse = "{\"count\":0,\"patentFileWrapperDataBag\":[]}";
    }

    internal RecordingDocumentListHandler(HttpStatusCode documentStatusCode, string documentResponse, string lookupResponse)
    {
        DocumentStatusCode = documentStatusCode;
        DocumentResponse = documentResponse;
        LookupResponse = lookupResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single());
        Accepts.Add(request.Headers.Accept.Single().MediaType);
        bool lookup = request.RequestUri!.AbsolutePath.EndsWith("/search", StringComparison.Ordinal);
        HttpResponseMessage response = new HttpResponseMessage(lookup ? HttpStatusCode.OK : DocumentStatusCode)
        {
            Content = new StringContent(lookup ? LookupResponse : DocumentResponse, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        return Task.FromResult(response);
    }
}
