using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class BulkCatalogRetrievalOperationTests
{
    [Test]
    public void ExactProductRetrievalParsesTheDocumentedBagAndRetainsRawData()
    {
        RecordingBulkCatalogRetrievalHandler handler = new RecordingBulkCatalogRetrievalHandler(new[]
        {
            RetrievalResponse.Json(HttpStatusCode.OK, DocumentedProduct("PTFWPRE", "weekly-data.zip"))
        });
        using HttpClient client = new HttpClient(handler);
        BulkCatalogRetrievalOperation operation = CreateOperation(client);
        RequestContext context = new RequestContext { RetainRawResponse = true };

        BulkProduct result = operation.GetProduct(" PTFWPRE ", context);

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
            Assert.That(handler.RequestUris[0].AbsolutePath, Is.EqualTo("/root/api/v1/datasets/products/PTFWPRE"));
            Assert.That(ReadQueryValue(handler.RequestUris[0], "includeFiles"), Is.EqualTo("true"));
            Assert.That(ReadQueryValue(handler.RequestUris[0], "offset"), Is.EqualTo("0"));
            Assert.That(ReadQueryValue(handler.RequestUris[0], "limit"), Is.EqualTo("100"));
            Assert.That(handler.ApiKeys, Is.EqualTo(new[] { "test-key" }));
            Assert.That(handler.Accepts, Is.EqualTo(new[] { UsptoMediaTypes.Json }));
            Assert.That(result.ProductIdentifier, Is.EqualTo("PTFWPRE"));
            Assert.That(result.Title, Is.EqualTo("Patent File Wrapper Weekly"));
            Assert.That(result.TotalFileSize, Is.EqualTo(5_000_000_000L));
            Assert.That(result.Files.Single().Size, Is.EqualTo(4_500_000_000L));
            Assert.That(result.Files.Single().CatalogUri?.Host, Is.EqualTo("api.uspto.gov"));
            Assert.That(result.RawResponse, Is.EqualTo(DocumentedProduct("PTFWPRE", "weekly-data.zip")));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("product-request"));
            Assert.That(RedirectPolicy.GetRedirectIdentity(result.Provenance.SourceUri!), Is.EqualTo(RedirectPolicy.GetRedirectIdentity(handler.RequestUris[0])));
        });
    }

    [Test]
    public void ExactProductRetrievalCollectsEveryFileMetadataPage()
    {
        string[] firstNames = new string[100];
        int index;
        for (index = 0; index < firstNames.Length; index += 1)
        {
            firstNames[index] = "file-" + index.ToString() + ".zip";
        }

        RecordingBulkCatalogRetrievalHandler handler = new RecordingBulkCatalogRetrievalHandler(new[]
        {
            RetrievalResponse.Json(HttpStatusCode.OK, ProductPage("PTGRNT", firstNames, 101)),
            RetrievalResponse.Json(HttpStatusCode.OK, ProductPage("PTGRNT", new[] { "file-100.zip" }, 101))
        });
        using HttpClient client = new HttpClient(handler);
        BulkCatalogRetrievalOperation operation = CreateOperation(client);

        BulkProduct result = operation.GetProduct("PTGRNT", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
            Assert.That(ReadQueryValue(handler.RequestUris[0], "offset"), Is.EqualTo("0"));
            Assert.That(ReadQueryValue(handler.RequestUris[1], "offset"), Is.EqualTo("100"));
            Assert.That(result.Files, Has.Length.EqualTo(101));
            Assert.That(result.Files[100].FileName, Is.EqualTo("file-100.zip"));
            Assert.That(result.ReturnedFileCount, Is.EqualTo(101L));
        });
    }

    [Test]
    public void ExactProductRetrievalAcceptsALiveObjectWrapperAndValidatesIdentity()
    {
        const string liveResponse = "{\"product\":{\"shortName\":\"PTFWPRE\",\"title\":\"Live product\",\"files\":[{\"filename\":\"weekly-data.zip\",\"size\":\"5000000000\"}]},\"requestIdentifier\":\"live-request\"}";
        RecordingBulkCatalogRetrievalHandler handler = new RecordingBulkCatalogRetrievalHandler(new[]
        {
            RetrievalResponse.Json(HttpStatusCode.OK, liveResponse)
        });
        using HttpClient client = new HttpClient(handler);
        BulkCatalogRetrievalOperation operation = CreateOperation(client);

        BulkProduct result = operation.GetProduct("ptfwpre", new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.ProductIdentifier, Is.EqualTo("PTFWPRE"));
            Assert.That(result.Title, Is.EqualTo("Live product"));
            Assert.That(result.Files.Single().FileName, Is.EqualTo("weekly-data.zip"));
            Assert.That(result.Files.Single().Size, Is.EqualTo(5_000_000_000L));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("live-request"));
        });

        ParseException exception = Assert.Throws<ParseException>(() => OdpBulkCatalogParser.ParseExactProduct(liveResponse, "OTHER", ResponseMetadata(), null));
        Assert.That(exception.DataPath, Is.EqualTo("$.product.productIdentifier"));
    }

    [Test]
    public void ExactFileRetrievalReturnsTheValidatedRedirectWithoutFollowingIt()
    {
        const string fileName = "weekly grant #1.zip";
        RecordingBulkCatalogRetrievalHandler handler = new RecordingBulkCatalogRetrievalHandler(new[]
        {
            RetrievalResponse.Json(HttpStatusCode.OK, DocumentedProduct("PTGRNT", fileName)),
            RetrievalResponse.Redirect(HttpStatusCode.Found, "https://bulkdata.uspto.gov/grants/weekly%20grant%20%231.zip", "redirect-request")
        });
        using HttpClient client = new HttpClient(handler);
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new[] { IPAddress.Parse("93.184.216.34") });
        BulkCatalogRetrievalOperation operation = CreateOperation(client, resolver);

        BulkProductFile result = operation.GetFile("PTGRNT", fileName, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
            Assert.That(handler.RequestUris[1].AbsolutePath, Is.EqualTo("/root/api/v1/datasets/products/files/PTGRNT/weekly%20grant%20%231.zip"));
            Assert.That(result.ProductIdentifier, Is.EqualTo("PTGRNT"));
            Assert.That(result.FileName, Is.EqualTo(fileName));
            Assert.That(result.Size, Is.EqualTo(4_500_000_000L));
            Assert.That(RedirectPolicy.GetRedirectIdentity(result.CatalogUri!), Is.EqualTo(RedirectPolicy.GetRedirectIdentity(handler.RequestUris[1])));
            Assert.That(result.DownloadUri?.AbsoluteUri, Is.EqualTo("https://bulkdata.uspto.gov/grants/weekly%20grant%20%231.zip"));
            Assert.That(result.ResponseStatusCode, Is.EqualTo(302));
            Assert.That(result.HasResponseStatusCode, Is.True);
            Assert.That(result.RawResponse, Is.Null);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.DatasetIdentifier, Is.EqualTo("PTGRNT"));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("redirect-request"));
            Assert.That(RedirectPolicy.GetRedirectIdentity(result.Provenance.SourceUri!), Is.EqualTo(RedirectPolicy.GetRedirectIdentity(handler.RequestUris[1])));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(resolver.CallCount, Is.EqualTo(1));
            Assert.That(resolver.LastHost, Is.EqualTo("bulkdata.uspto.gov"));
        });
    }

    [Test]
    public void ExactFileMatchingIsOrdinalAndDoesNotCallTheRedirectRouteWhenAbsent()
    {
        RecordingBulkCatalogRetrievalHandler handler = new RecordingBulkCatalogRetrievalHandler(new[]
        {
            RetrievalResponse.Json(HttpStatusCode.OK, DocumentedProduct("PTGRNT", "Weekly.ZIP"))
        });
        using HttpClient client = new HttpClient(handler);
        BulkCatalogRetrievalOperation operation = CreateOperation(client);

        NotFoundException exception = Assert.Throws<NotFoundException>(() => operation.GetFile("PTGRNT", "weekly.zip", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ResourceIdentifier, Is.EqualTo("PTGRNT/weekly.zip"));
            Assert.That(exception.HasStatusCode, Is.False);
            Assert.That(exception.RequestIdentifier, Is.EqualTo("product-request"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ExactFileRetrievalAcceptsAJsonMetadataResponseAndValidatesItsTarget()
    {
        const string fileResponse = "{\"file\":{\"filename\":\"weekly-data.zip\",\"size\":\"6000000000\",\"downloadUri\":\"https://bulkdata.uspto.gov/live/weekly-data.zip\"},\"requestIdentifier\":\"file-request\"}";
        RecordingBulkCatalogRetrievalHandler handler = new RecordingBulkCatalogRetrievalHandler(new[]
        {
            RetrievalResponse.Json(HttpStatusCode.OK, DocumentedProduct("PTFWPRE", "weekly-data.zip")),
            RetrievalResponse.Json(HttpStatusCode.OK, fileResponse)
        });
        using HttpClient client = new HttpClient(handler);
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new[] { IPAddress.Parse("93.184.216.34") });
        BulkCatalogRetrievalOperation operation = CreateOperation(client, resolver);
        RequestContext context = new RequestContext { RetainRawResponse = true };

        BulkProductFile result = operation.GetFile("PTFWPRE", "weekly-data.zip", context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Size, Is.EqualTo(6_000_000_000L));
            Assert.That(RedirectPolicy.GetRedirectIdentity(result.CatalogUri!), Is.EqualTo(RedirectPolicy.GetRedirectIdentity(handler.RequestUris[1])));
            Assert.That(result.DownloadUri?.AbsoluteUri, Is.EqualTo("https://bulkdata.uspto.gov/live/weekly-data.zip"));
            Assert.That(result.ResponseStatusCode, Is.EqualTo(200));
            Assert.That(result.HasResponseStatusCode, Is.True);
            Assert.That(result.RawResponse, Is.EqualTo(fileResponse));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("file-request"));
            Assert.That(resolver.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void UnsafeOrIncompleteRedirectsAreRejected()
    {
        RecordingBulkCatalogRetrievalHandler missingLocationHandler = new RecordingBulkCatalogRetrievalHandler(new[]
        {
            RetrievalResponse.Json(HttpStatusCode.OK, DocumentedProduct("PTFWPRE", "weekly-data.zip")),
            RetrievalResponse.Json(HttpStatusCode.Found, string.Empty)
        });
        using HttpClient missingLocationClient = new HttpClient(missingLocationHandler);
        BulkCatalogRetrievalOperation missingLocationOperation = CreateOperation(missingLocationClient);

        ApiException missingLocation = Assert.Throws<ApiException>(() => missingLocationOperation.GetFile("PTFWPRE", "weekly-data.zip", new RequestContext()));

        RecordingBulkCatalogRetrievalHandler unsafeHostHandler = new RecordingBulkCatalogRetrievalHandler(new[]
        {
            RetrievalResponse.Json(HttpStatusCode.OK, DocumentedProduct("PTFWPRE", "weekly-data.zip")),
            RetrievalResponse.Redirect(HttpStatusCode.Found, "https://unapproved.example/weekly-data.zip", null)
        });
        using HttpClient unsafeHostClient = new HttpClient(unsafeHostHandler);
        BulkCatalogRetrievalOperation unsafeHostOperation = CreateOperation(unsafeHostClient);

        ValidationException unsafeHost = Assert.Throws<ValidationException>(() => unsafeHostOperation.GetFile("PTFWPRE", "weekly-data.zip", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(missingLocation.ErrorCode, Is.EqualTo("REDIRECT_LOCATION_MISSING"));
            Assert.That(unsafeHost.ErrorCode, Is.EqualTo("DOWNLOAD_HOST_NOT_ALLOWED"));
        });
    }

    [Test]
    public void AutomaticRedirectFollowingIsRejectedBeforeTheReturnedBodyIsParsed()
    {
        RecordingBulkCatalogRetrievalHandler handler = new RecordingBulkCatalogRetrievalHandler(new[]
        {
            RetrievalResponse.Json(HttpStatusCode.OK, DocumentedProduct("PTFWPRE", "weekly-data.zip")),
            RetrievalResponse.AutoFollowed("https://bulkdata.uspto.gov/live/weekly-data.zip")
        });
        using HttpClient client = new HttpClient(handler);
        BulkCatalogRetrievalOperation operation = CreateOperation(client);

        ApiException exception = Assert.Throws<ApiException>(() => operation.GetFile("PTFWPRE", "weekly-data.zip", new RequestContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("AUTOMATIC_REDIRECT_REJECTED"));
            Assert.That(handler.RequestUris, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void CancellationStopsBeforeAnyCatalogRequest()
    {
        RecordingBulkCatalogRetrievalHandler handler = new RecordingBulkCatalogRetrievalHandler(new RetrievalResponse[0]);
        using HttpClient client = new HttpClient(handler);
        BulkCatalogRetrievalOperation operation = CreateOperation(client);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RequestContext context = new RequestContext { CancellationToken = cancellation.Token };

        Assert.Throws<OperationCanceledException>(() => operation.GetProduct("PTFWPRE", context));
        Assert.Throws<OperationCanceledException>(() => operation.GetFile("PTFWPRE", "weekly-data.zip", context));

        Assert.That(handler.RequestUris, Is.Empty);
    }

    private static BulkCatalogRetrievalOperation CreateOperation(HttpClient client, FakeDownloadHostResolver? resolver = null)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            OdpBaseUri = new Uri("https://example.test/root/"),
            OdpApiKey = "test-key",
            HttpClient = client,
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            OverallTimeoutMilliseconds = 5000
        };
        DownloadHostPolicy hostPolicy = new DownloadHostPolicy(resolver ?? new FakeDownloadHostResolver(new[] { IPAddress.Parse("93.184.216.34") }));
        return new BulkCatalogRetrievalOperation(client, options, hostPolicy);
    }

    private static string DocumentedProduct(string productIdentifier, string fileName)
    {
        string encodedFileName = Uri.EscapeDataString(fileName);
        return "{\"count\":1,\"bulkDataProductBag\":[{\"productIdentifier\":\"" + productIdentifier + "\",\"productTitleText\":\"Patent File Wrapper Weekly\",\"productTotalFileSize\":5000000000,\"productFileTotalQuantity\":1,\"productFileBag\":{\"count\":1,\"fileDataBag\":[{\"fileName\":\"" + fileName + "\",\"fileSize\":4500000000,\"mimeTypeIdentifier\":\"ZIP\",\"fileDownloadURI\":\"https://api.uspto.gov/api/v1/datasets/products/files/" + productIdentifier + "/" + encodedFileName + "\"}]} }],\"requestIdentifier\":\"product-request\"}";
    }

    private static string ProductPage(string productIdentifier, string[] fileNames, long total)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{\"count\":1,\"bulkDataProductBag\":[{\"productIdentifier\":\"");
        builder.Append(productIdentifier);
        builder.Append("\",\"productFileTotalQuantity\":");
        builder.Append(total);
        builder.Append(",\"productFileBag\":{\"count\":");
        builder.Append(fileNames.Length);
        builder.Append(",\"fileDataBag\":[");
        int index;
        for (index = 0; index < fileNames.Length; index += 1)
        {
            if (index != 0)
            {
                builder.Append(',');
            }

            builder.Append("{\"fileName\":\"");
            builder.Append(fileNames[index]);
            builder.Append("\",\"fileSize\":1}");
        }

        builder.Append("]}}],\"requestIdentifier\":\"page-request\"}");
        return builder.ToString();
    }

    private static HttpResponseMetadata ResponseMetadata()
    {
        return new HttpResponseMetadata
        {
            StatusCode = 200,
            RequestUri = new Uri("https://api.uspto.gov/api/v1/datasets/products/PTFWPRE"),
            MediaType = UsptoMediaTypes.Json,
            Format = DocumentFormat.Json
        };
    }

    private static string? ReadQueryValue(Uri uri, string name)
    {
        foreach (string pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            if (Uri.UnescapeDataString(parts[0]) == name)
            {
                return parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : null;
            }
        }

        return null;
    }
}

internal class RetrievalResponse
{
    internal HttpStatusCode StatusCode = HttpStatusCode.OK;
    internal string Body = string.Empty;
    internal string? Location = null;
    internal string? RequestIdentifier = null;
    internal Uri? ResponseRequestUri = null;

    internal static RetrievalResponse Json(HttpStatusCode statusCode, string body)
    {
        return new RetrievalResponse { StatusCode = statusCode, Body = body };
    }

    internal static RetrievalResponse Redirect(HttpStatusCode statusCode, string location, string? requestIdentifier)
    {
        return new RetrievalResponse { StatusCode = statusCode, Location = location, RequestIdentifier = requestIdentifier };
    }

    internal static RetrievalResponse AutoFollowed(string responseRequestUri)
    {
        return new RetrievalResponse
        {
            StatusCode = HttpStatusCode.OK,
            Body = "not-json",
            ResponseRequestUri = new Uri(responseRequestUri)
        };
    }
}

internal class RecordingBulkCatalogRetrievalHandler : HttpMessageHandler
{
    internal Queue<RetrievalResponse> Responses = new Queue<RetrievalResponse>();
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();

    internal RecordingBulkCatalogRetrievalHandler(RetrievalResponse[] responses)
    {
        foreach (RetrievalResponse response in responses)
        {
            Responses.Enqueue(response);
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri!);
        ApiKeys.Add(request.Headers.TryGetValues(UsptoHeaders.OdpApiKey, out IEnumerable<string>? keys) ? keys.Single() : null);
        Accepts.Add(request.Headers.Accept.SingleOrDefault()?.MediaType);
        RetrievalResponse next = Responses.Dequeue();
        HttpResponseMessage response = new HttpResponseMessage(next.StatusCode)
        {
            Content = new StringContent(next.Body, Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = next.ResponseRequestUri == null ? request : new HttpRequestMessage(HttpMethod.Get, next.ResponseRequestUri)
        };
        if (next.Location != null)
        {
            response.Headers.Location = new Uri(next.Location);
        }

        if (next.RequestIdentifier != null)
        {
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, next.RequestIdentifier);
        }

        return Task.FromResult(response);
    }
}
