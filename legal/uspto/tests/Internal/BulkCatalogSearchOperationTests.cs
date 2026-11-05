using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class BulkCatalogSearchOperationTests
{
    [Test]
    public void SearchWritesEveryCatalogDimensionAndParsesTypedProducts()
    {
        RecordingBulkCatalogHandler handler = new RecordingBulkCatalogHandler(new[] { CompleteResponse() });
        handler.HeaderRequestIdentifier = "header-request";
        using HttpClient client = new HttpClient(handler);
        BulkCatalogSearchOperation operation = new BulkCatalogSearchOperation(client, CreateOptions(client));
        SearchRequest request = CompleteRequest();
        request.Context.RetainRawResponse = true;

        BulkProductResponse result = operation.Search(request);

        BulkProduct product = result.Products.Single();
        BulkProductFile file = product.Files.Single();
        Assert.Multiple(() =>
        {
            Assert.That(handler.Methods.Single(), Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.RequestUris.Single().AbsolutePath, Is.EqualTo("/root/api/v1/datasets/products/search"));
            Assert.That(handler.Queries[0]["q"], Does.Contain("patent"));
            Assert.That(handler.Queries[0]["q"], Does.Contain("productLabelArrayText"));
            Assert.That(handler.Queries[0]["q"], Does.Contain("productDataSetCategoryArrayText"));
            Assert.That(handler.Queries[0]["q"], Does.Contain("productDataSetArrayText"));
            Assert.That(handler.Queries[0]["q"], Does.Contain("productTitleText"));
            Assert.That(handler.Queries[0]["q"], Does.Contain("mimeTypeIdentifierArrayText"));
            Assert.That(handler.Queries[0]["q"], Does.Contain("productIdentifier"));
            Assert.That(handler.Queries[0]["rangeFilters"], Is.EqualTo("productFromDate 2001-01-01:2026-12-31"));
            Assert.That(handler.Queries[0]["sort"], Is.EqualTo("productTitleText desc,productIdentifier asc"));
            Assert.That(handler.Queries[0]["fields"], Is.EqualTo("productIdentifier,productTitleText"));
            Assert.That(handler.Queries[0]["facets"], Is.EqualTo("productLabelArrayText,mimeTypeIdentifierArrayText"));
            Assert.That(handler.Queries[0]["offset"], Is.EqualTo("25"));
            Assert.That(handler.Queries[0]["limit"], Is.EqualTo("10"));
            Assert.That(handler.ApiKeys.Single(), Is.EqualTo("test-key"));
            Assert.That(handler.Accepts.Single(), Is.EqualTo(UsptoMediaTypes.Json));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Count, Is.EqualTo(1L));
            Assert.That(result.HasCount, Is.True);
            Assert.That(result.ReturnedCount, Is.EqualTo(1));
            Assert.That(result.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(result.RawResponse, Is.EqualTo(CompleteResponse()));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.UnknownFields["futureResponse"], Is.EqualTo("true"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(product.ProductIdentifier, Is.EqualTo("PTFWPRE"));
            Assert.That(product.Title, Is.EqualTo("Patent File Wrapper Weekly"));
            Assert.That(product.Description, Is.EqualTo("Patent file wrapper data"));
            Assert.That(product.Frequency, Is.EqualTo("WEEKLY"));
            Assert.That(product.DaysOfWeek, Is.EqualTo(new[] { "SUNDAY", "WEDNESDAY" }));
            Assert.That(product.Labels, Is.EqualTo(new[] { "RESEARCH", "PATENT" }));
            Assert.That(product.DatasetTypes, Is.EqualTo(new[] { "Research" }));
            Assert.That(product.Categories, Is.EqualTo(new[] { "Patent file wrapper" }));
            Assert.That(product.DataDateRange.FromDate, Is.EqualTo(new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(product.DataDateRange.HasFromDate, Is.True);
            Assert.That(product.DataDateRange.ToDateText, Is.EqualTo("current release"));
            Assert.That(product.DataDateRange.HasToDate, Is.False);
            Assert.That(product.TotalFileSize, Is.EqualTo(32_511_973_080L));
            Assert.That(product.HasTotalFileSize, Is.True);
            Assert.That(product.FileCount, Is.EqualTo(3_000_000_000L));
            Assert.That(product.HasFileCount, Is.True);
            Assert.That(product.ReturnedFileCount, Is.EqualTo(1L));
            Assert.That(product.HasReturnedFileCount, Is.True);
            Assert.That(product.HasLastModified, Is.True);
            Assert.That(product.MimeTypeIdentifiers, Is.EqualTo(new[] { "JSON", "XML" }));
            Assert.That(product.Provenance.DatasetIdentifier, Is.EqualTo("PTFWPRE"));
            Assert.That(product.UnknownFields["futureProduct"], Is.EqualTo("{\"enabled\":true}"));
            Assert.That(product.UnknownFields["productFileBag.futureFileBag"], Is.EqualTo("9"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(file.ProductIdentifier, Is.EqualTo("PTFWPRE"));
            Assert.That(file.FileName, Is.EqualTo("2001-2010-patent-filewrapper-full-json.zip"));
            Assert.That(file.Size, Is.EqualTo(4_500_000_000L));
            Assert.That(file.HasSize, Is.True);
            Assert.That(file.DataDateRange.HasFromDate, Is.True);
            Assert.That(file.DataDateRange.HasToDate, Is.True);
            Assert.That(file.FileType, Is.EqualTo("Data"));
            Assert.That(file.DownloadUri?.AbsolutePath, Does.EndWith("2001-2010-patent-filewrapper-full-json.zip"));
            Assert.That(file.Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(file.Compression, Is.EqualTo(CompressionFormat.Zip));
            Assert.That(file.MediaType, Is.EqualTo(UsptoMediaTypes.Zip));
            Assert.That(file.HasReleaseDate, Is.True);
            Assert.That(file.HasFileDate, Is.True);
            Assert.That(file.HasLastModified, Is.True);
            Assert.That(file.UnknownFields["futureFile"], Is.EqualTo("\"retained\""));
            Assert.That(result.Facets.Select(item => item.Field), Is.EqualTo(new[] { "productLabelArrayText", "mimeTypeIdentifierArrayText" }));
            Assert.That(result.Facets[0].Values[0].Count, Is.EqualTo(3_500_000_000L));
        });
    }

    [Test]
    public void ParserAcceptsAliasesDirectFileArraysAndArrayFacets()
    {
        const string json = """
        {
          "count": "1",
          "products": [
            {
              "shortName": "ALTXML",
              "title": "Alternate XML product",
              "labels": ["PATENT"],
              "datasetTypes": ["Patent"],
              "categories": ["Grants"],
              "fromDate": "not-a-date",
              "files": [
                {
                  "filename": "grant-data.xml",
                  "size": "5000000000",
                  "downloadUri": "https://bulkdata.uspto.gov/grants/grant-data.xml"
                }
              ]
            }
          ],
          "facets": [
            {
              "mimeTypeIdentifierArrayText": [
                { "value": "XML", "count": "4000000000" }
              ]
            }
          ]
        }
        """;

        BulkProductResponse result = OdpBulkCatalogParser.ParseSearch(json, new SearchRequest(), Metadata("header-request"), null);

        BulkProduct product = result.Products.Single();
        BulkProductFile file = product.Files.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.RequestIdentifier, Is.EqualTo("header-request"));
            Assert.That(product.ProductIdentifier, Is.EqualTo("ALTXML"));
            Assert.That(product.Title, Is.EqualTo("Alternate XML product"));
            Assert.That(product.DataDateRange.FromDateText, Is.EqualTo("not-a-date"));
            Assert.That(product.DataDateRange.HasFromDate, Is.False);
            Assert.That(product.ReturnedFileCount, Is.EqualTo(1L));
            Assert.That(file.FileName, Is.EqualTo("grant-data.xml"));
            Assert.That(file.Size, Is.EqualTo(5_000_000_000L));
            Assert.That(file.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(file.Compression, Is.EqualTo(CompressionFormat.None));
            Assert.That(file.MediaType, Is.EqualTo(UsptoMediaTypes.Xml));
            Assert.That(result.Facets.Single().Field, Is.EqualTo("mimeTypeIdentifierArrayText"));
            Assert.That(result.Facets.Single().Values.Single().Count, Is.EqualTo(4_000_000_000L));
        });
    }

    [Test]
    public void ParserAcceptsSecondaryAliasesAndSignedInt64MaximumMetadata()
    {
        const string json = """
        {
          "count": 1,
          "productBag": [
            {
              "productId": "ALIASCSV",
              "productTitle": "Alias CSV product",
              "productDescription": "Alternate product fields",
              "productFrequency": "DAILY",
              "productDaysOfWeekText": " MONDAY, , FRIDAY ",
              "productLabels": ["PATENT"],
              "productDatasetArrayText": ["Research"],
              "productDatasetCategoryArrayText": ["Grants"],
              "fromDate": "2024-01-01",
              "toDate": "2024-12-31",
              "totalFileSize": "9223372036854775807",
              "fileCount": "9223372036854775807",
              "productLastModifiedDateTime": "2025-01-02T03:04:05Z",
              "mimeTypeIdentifiers": ["CSV"],
              "productFileBag": {
                "fileCount": 1,
                "files": [
                  {
                    "filename": "grants.csv",
                    "size": "9223372036854775807",
                    "dataFromDate": "2024-01-01",
                    "dataToDate": "2024-12-31",
                    "fileType": "Data",
                    "mimeType": "CSV",
                    "downloadURI": "https://bulkdata.uspto.gov/grants/grants.csv",
                    "releaseDate": "2025-01-02T03:04:05Z",
                    "fileDate": "2024-12-31",
                    "lastModifiedDateTime": "2025-01-03T04:05:06Z"
                  }
                ]
              }
            }
          ],
          "facets": {
            "productLabels": [
              {"value": "PATENT", "count": "9223372036854775807"}
            ]
          }
        }
        """;

        BulkProductResponse result = OdpBulkCatalogParser.ParseSearch(json, new SearchRequest(), Metadata(null), null);

        BulkProduct product = result.Products.Single();
        BulkProductFile file = product.Files.Single();
        Assert.Multiple(() =>
        {
            Assert.That(product.ProductIdentifier, Is.EqualTo("ALIASCSV"));
            Assert.That(product.Title, Is.EqualTo("Alias CSV product"));
            Assert.That(product.Description, Is.EqualTo("Alternate product fields"));
            Assert.That(product.Frequency, Is.EqualTo("DAILY"));
            Assert.That(product.DaysOfWeekText, Is.EqualTo(" MONDAY, , FRIDAY "));
            Assert.That(product.DaysOfWeek, Is.EqualTo(new[] { "MONDAY", "FRIDAY" }));
            Assert.That(product.Labels, Is.EqualTo(new[] { "PATENT" }));
            Assert.That(product.DatasetTypes, Is.EqualTo(new[] { "Research" }));
            Assert.That(product.Categories, Is.EqualTo(new[] { "Grants" }));
            Assert.That(product.DataDateRange.FromDate, Is.EqualTo(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(product.DataDateRange.ToDate, Is.EqualTo(new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(product.TotalFileSize, Is.EqualTo(long.MaxValue));
            Assert.That(product.HasTotalFileSize, Is.True);
            Assert.That(product.FileCount, Is.EqualTo(long.MaxValue));
            Assert.That(product.HasFileCount, Is.True);
            Assert.That(product.ReturnedFileCount, Is.EqualTo(1L));
            Assert.That(product.HasReturnedFileCount, Is.True);
            Assert.That(product.LastModified, Is.EqualTo(new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero)));
            Assert.That(product.MimeTypeIdentifiers, Is.EqualTo(new[] { "CSV" }));
        });

        Assert.Multiple(() =>
        {
            Assert.That(file.FileName, Is.EqualTo("grants.csv"));
            Assert.That(file.Size, Is.EqualTo(long.MaxValue));
            Assert.That(file.HasSize, Is.True);
            Assert.That(file.DataDateRange.HasFromDate, Is.True);
            Assert.That(file.DataDateRange.HasToDate, Is.True);
            Assert.That(file.FileType, Is.EqualTo("Data"));
            Assert.That(file.MimeTypeIdentifier, Is.EqualTo("CSV"));
            Assert.That(file.DownloadUri?.AbsoluteUri, Is.EqualTo("https://bulkdata.uspto.gov/grants/grants.csv"));
            Assert.That(file.Format, Is.EqualTo(DocumentFormat.Csv));
            Assert.That(file.Compression, Is.EqualTo(CompressionFormat.None));
            Assert.That(file.MediaType, Is.EqualTo(UsptoMediaTypes.Csv));
            Assert.That(file.HasReleaseDate, Is.True);
            Assert.That(file.HasFileDate, Is.True);
            Assert.That(file.LastModified, Is.EqualTo(new DateTimeOffset(2025, 1, 3, 4, 5, 6, TimeSpan.Zero)));
            Assert.That(result.Facets.Single().Field, Is.EqualTo("productLabels"));
            Assert.That(result.Facets.Single().Values.Single().Count, Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void AllPageSearchAggregatesUntilEmptyAndPreservesFirstPageMetadata()
    {
        string first = Page(new[] { "APPXML", "PTGRXML" }, "page-one", "\"facets\":{\"productLabelArrayText\":[{\"value\":\"PATENT\",\"count\":3}]},\"futureResponse\":true,");
        RecordingBulkCatalogHandler handler = new RecordingBulkCatalogHandler(new[]
        {
            first,
            Page(new[] { "PTFWPRE" }, "page-two", string.Empty),
            Page(Array.Empty<string>(), "page-three", string.Empty)
        });
        using HttpClient client = new HttpClient(handler);
        BulkCatalogSearchOperation operation = new BulkCatalogSearchOperation(client, CreateOptions(client));
        SearchRequest request = PaginationRequest();
        request.FieldSelection.Fields = new[] { "productTitleText" };
        request.Context.RetainRawResponse = true;

        BulkProductResponse result = operation.SearchAll(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Products.Select(item => item.ProductIdentifier), Is.EqualTo(new[] { "APPXML", "PTGRXML", "PTFWPRE" }));
            Assert.That(result.Count, Is.EqualTo(3L));
            Assert.That(result.HasCount, Is.True);
            Assert.That(result.ReturnedCount, Is.EqualTo(3));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.RequestIdentifier, Is.EqualTo("page-one"));
            Assert.That(result.RawResponse, Is.EqualTo(first));
            Assert.That(result.Facets.Single().Values.Single().Count, Is.EqualTo(3L));
            Assert.That(result.UnknownFields["futureResponse"], Is.EqualTo("true"));
            Assert.That(handler.Offsets, Is.EqualTo(new long[] { 0L, 2L, 3L }));
            Assert.That(handler.Limits, Is.EqualTo(new[] { 2, 2, 2 }));
            Assert.That(handler.SelectedFields, Has.All.EqualTo("productTitleText,productIdentifier"));
            Assert.That(request.OffsetPage.Offset, Is.Zero);
            Assert.That(request.FieldSelection.Fields, Is.EqualTo(new[] { "productTitleText" }));
        });
    }

    [Test]
    public void AllPageSearchHonorsConfiguredResultLimit()
    {
        RecordingBulkCatalogHandler handler = new RecordingBulkCatalogHandler(new[]
        {
            Page(new[] { "A", "B" }, "page-one", string.Empty),
            Page(new[] { "C", "D" }, "page-two", string.Empty)
        });
        using HttpClient client = new HttpClient(handler);
        UsptoClientOptions options = CreateOptions(client);
        options.Limits.MaximumResults = 3;
        BulkCatalogSearchOperation operation = new BulkCatalogSearchOperation(client, options);
        SearchRequest request = PaginationRequest();
        request.MaximumResults = 10;

        BulkProductResponse result = operation.SearchAll(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Products.Select(item => item.ProductIdentifier), Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(result.Count, Is.EqualTo(3L));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(handler.Offsets, Is.EqualTo(new long[] { 0L, 2L }));
        });
    }

    [Test]
    public void AllPageSearchRequiresProductIdentifierAsFinalStableSort()
    {
        RecordingBulkCatalogHandler handler = new RecordingBulkCatalogHandler(Array.Empty<string>());
        using HttpClient client = new HttpClient(handler);
        BulkCatalogSearchOperation operation = new BulkCatalogSearchOperation(client, CreateOptions(client));
        SearchRequest request = PaginationRequest();
        request.Sorts = new[] { new SearchSort { Field = "productTitleText", Direction = SortDirection.Ascending } };

        ValidationException exception = Assert.Throws<ValidationException>(() => operation.SearchAll(request));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DETERMINISTIC_SORT_REQUIRED"));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    [Test]
    public void SearchMapsRemoteErrorsAndHonorsCallerCancellation()
    {
        RecordingBulkCatalogHandler handler = new RecordingBulkCatalogHandler(new[] { "{\"error\":\"Too Many Requests\"}" });
        handler.StatusCode = HttpStatusCode.TooManyRequests;
        handler.RetryAfter = "3";
        using HttpClient client = new HttpClient(handler);
        BulkCatalogSearchOperation operation = new BulkCatalogSearchOperation(client, CreateOptions(client));

        RateLimitException rateLimit = Assert.Throws<RateLimitException>(() => operation.Search(new SearchRequest()));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => operation.Search(new SearchRequest { Context = new RequestContext { CancellationToken = cancellation.Token } }));

        Assert.Multiple(() =>
        {
            Assert.That(rateLimit.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(rateLimit.StatusCode, Is.EqualTo(429));
            Assert.That(rateLimit.RetryAfterMilliseconds, Is.EqualTo(3000L));
            Assert.That(handler.CallCount, Is.EqualTo(1));
        });
    }

    [TestCase("{\"bulkDataProductBag\":[]}", "$.count")]
    [TestCase("{\"count\":1,\"bulkDataProductBag\":[]}", "$.bulkDataProductBag")]
    [TestCase("{\"count\":1,\"bulkDataProductBag\":[{}]}", "$.bulkDataProductBag[0].productIdentifier")]
    [TestCase("{\"count\":1,\"bulkDataProductBag\":[{\"productIdentifier\":\"P\",\"productTotalFileSize\":-1}]}", "$.bulkDataProductBag[0].productTotalFileSize")]
    [TestCase("{\"count\":1,\"bulkDataProductBag\":[{\"productIdentifier\":\"P\",\"productTotalFileSize\":9223372036854775808}]}", "$.bulkDataProductBag[0].productTotalFileSize")]
    [TestCase("{\"count\":1,\"bulkDataProductBag\":[{\"productIdentifier\":\"P\",\"productFileTotalQuantity\":-1}]}", "$.bulkDataProductBag[0].productFileTotalQuantity")]
    [TestCase("{\"count\":1,\"bulkDataProductBag\":[{\"productIdentifier\":\"P\",\"productFileBag\":{\"count\":1,\"fileDataBag\":[]}}]}", "$.bulkDataProductBag[0].productFileBag.fileDataBag")]
    [TestCase("{\"count\":1,\"bulkDataProductBag\":[{\"productIdentifier\":\"P\",\"files\":[{\"fileName\":\"data.zip\",\"fileSize\":9223372036854775808}]}]}", "$.bulkDataProductBag[0].files[0].fileSize")]
    [TestCase("{\"count\":1,\"bulkDataProductBag\":[{\"productIdentifier\":\"P\",\"files\":[{\"fileName\":\"data.zip\",\"fileDownloadURI\":\"not a URI\"}]}]}", "$.bulkDataProductBag[0].files[0].fileDownloadURI")]
    [TestCase("{\"count\":0,\"bulkDataProductBag\":[],\"facets\":true}", "$.facets")]
    [TestCase("{\"count\":0,\"bulkDataProductBag\":[],\"facets\":{\"label\":[{\"value\":\"P\",\"count\":-1}]}}", "$.facets.label[0].count")]
    public void MalformedCatalogResponsesHavePrecisePaths(string json, string path)
    {
        ParseException exception = Assert.Throws<ParseException>(() => OdpBulkCatalogParser.ParseSearch(json, new SearchRequest(), Metadata(null), null));

        Assert.That(exception.DataPath, Is.EqualTo(path));
    }

    private static SearchRequest CompleteRequest()
    {
        return new SearchRequest
        {
            Query = "patent",
            Filters = new[]
            {
                Equal("productLabelArrayText", "PATENT"),
                Equal("productDataSetCategoryArrayText", "Patent file wrapper"),
                Equal("productDataSetArrayText", "Research"),
                Equal("productTitleText", "Patent File Wrapper Weekly"),
                Equal("mimeTypeIdentifierArrayText", "JSON"),
                Equal("productIdentifier", "PTFWPRE")
            },
            Ranges = new[]
            {
                new SearchRange
                {
                    Field = "productFromDate",
                    LowerValue = "2001-01-01",
                    UpperValue = "2026-12-31"
                }
            },
            Sorts = new[]
            {
                new SearchSort { Field = "productTitleText", Direction = SortDirection.Descending },
                new SearchSort { Field = "productIdentifier", Direction = SortDirection.Ascending }
            },
            FieldSelection = new SearchFieldSelection { Fields = new[] { "productIdentifier", "productTitleText" } },
            Facets = new[]
            {
                new SearchFacetRequest { Field = "productLabelArrayText" },
                new SearchFacetRequest { Field = "mimeTypeIdentifierArrayText" }
            },
            OffsetPage = new OffsetPage { Offset = 25L, Limit = 10 }
        };
    }

    private static SearchFilter Equal(string field, string value)
    {
        return new SearchFilter { Field = field, Operator = SearchOperator.Equal, Value = value };
    }

    private static SearchRequest PaginationRequest()
    {
        return new SearchRequest
        {
            OffsetPage = new OffsetPage { Offset = 0L, Limit = 2 },
            MaximumPages = 10,
            MaximumResults = 10,
            Sorts = new[] { new SearchSort { Field = "productIdentifier", Direction = SortDirection.Ascending } }
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
            OverallTimeoutMilliseconds = 5000
        };
    }

    private static HttpResponseMetadata Metadata(string? requestIdentifier)
    {
        return new HttpResponseMetadata
        {
            RequestUri = new Uri("https://api.uspto.gov/api/v1/datasets/products/search"),
            RequestIdentifier = requestIdentifier
        };
    }

    private static string Page(string[] identifiers, string requestIdentifier, string leadingFields)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        builder.Append(leadingFields);
        builder.Append("\"count\":");
        builder.Append(identifiers.Length);
        builder.Append(",\"bulkDataProductBag\":[");
        for (int index = 0; index < identifiers.Length; index += 1)
        {
            if (index != 0)
            {
                builder.Append(',');
            }

            builder.Append("{\"productIdentifier\":\"");
            builder.Append(identifiers[index]);
            builder.Append("\",\"productTitleText\":\"Title ");
            builder.Append(index);
            builder.Append("\"}");
        }

        builder.Append("],\"requestIdentifier\":\"");
        builder.Append(requestIdentifier);
        builder.Append("\"}");
        return builder.ToString();
    }

    private static string CompleteResponse()
    {
        return """
        {
          "count": 1,
          "bulkDataProductBag": [
            {
              "productIdentifier": "PTFWPRE",
              "productDescriptionText": "Patent file wrapper data",
              "productTitleText": "Patent File Wrapper Weekly",
              "productFrequencyText": "WEEKLY",
              "daysOfWeekText": "SUNDAY, WEDNESDAY",
              "productLabelArrayText": ["RESEARCH", "PATENT"],
              "productDataSetArrayText": ["Research"],
              "productDataSetCategoryArrayText": ["Patent file wrapper"],
              "productFromDate": "2001-01-01",
              "productToDate": "current release",
              "productTotalFileSize": 32511973080,
              "productFileTotalQuantity": 3000000000,
              "lastModifiedDateTime": "2025-01-13T08:01:00Z",
              "mimeTypeIdentifierArrayText": ["JSON", "XML"],
              "productFileBag": {
                "count": 1,
                "fileDataBag": [
                  {
                    "fileName": "2001-2010-patent-filewrapper-full-json.zip",
                    "fileSize": 4500000000,
                    "fileDataFromDate": "2001-01-01",
                    "fileDataToDate": "2010-12-31",
                    "fileTypeText": "Data",
                    "fileDownloadURI": "https://api.uspto.gov/api/v1/datasets/products/files/PTFWPRE/2001-2010-patent-filewrapper-full-json.zip",
                    "fileReleaseDate": "2025-01-13 08:01:00",
                    "fileDate": "2001-01-01T00:00:00.000Z",
                    "fileLastModifiedDateTime": "2025-01-13 08:01:00",
                    "futureFile": "retained"
                  }
                ],
                "futureFileBag": 9
              },
              "futureProduct": {"enabled":true}
            }
          ],
          "facets": {
            "productLabelArrayText": [
              {"value":"PATENT","count":3500000000}
            ],
            "mimeTypeIdentifierArrayText": [
              {"value":"JSON","count":1}
            ]
          },
          "requestIdentifier": "body-request",
          "futureResponse": true
        }
        """;
    }
}

internal class RecordingBulkCatalogHandler : HttpMessageHandler
{
    internal Queue<string> Responses = new Queue<string>();
    internal List<HttpMethod> Methods = new List<HttpMethod>();
    internal List<Uri> RequestUris = new List<Uri>();
    internal List<Dictionary<string, string?>> Queries = new List<Dictionary<string, string?>>();
    internal List<long> Offsets = new List<long>();
    internal List<int> Limits = new List<int>();
    internal List<string?> SelectedFields = new List<string?>();
    internal List<string?> ApiKeys = new List<string?>();
    internal List<string?> Accepts = new List<string?>();
    internal HttpStatusCode StatusCode = HttpStatusCode.OK;
    internal string? HeaderRequestIdentifier;
    internal string? RetryAfter;
    internal int CallCount;

    internal RecordingBulkCatalogHandler(string[] responses)
    {
        foreach (string response in responses)
        {
            Responses.Enqueue(response);
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount += 1;
        Methods.Add(request.Method);
        RequestUris.Add(request.RequestUri!);
        Dictionary<string, string?> query = ReadQuery(request.RequestUri);
        Queries.Add(query);
        if (query.ContainsKey("offset"))
        {
            Offsets.Add(long.Parse(query["offset"]!));
            Limits.Add(int.Parse(query["limit"]!));
            SelectedFields.Add(query.GetValueOrDefault("fields"));
        }

        ApiKeys.Add(request.Headers.TryGetValues(UsptoHeaders.OdpApiKey, out IEnumerable<string>? keys) ? keys.Single() : null);
        Accepts.Add(request.Headers.Accept.SingleOrDefault()?.MediaType);
        HttpResponseMessage response = new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(Responses.Dequeue(), Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
        if (HeaderRequestIdentifier != null)
        {
            response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, HeaderRequestIdentifier);
        }

        if (RetryAfter != null)
        {
            response.Headers.TryAddWithoutValidation("Retry-After", RetryAfter);
        }

        return Task.FromResult(response);
    }

    private static Dictionary<string, string?> ReadQuery(Uri? uri)
    {
        Dictionary<string, string?> values = new Dictionary<string, string?>(StringComparer.Ordinal);
        string query = uri?.Query.TrimStart('?') ?? string.Empty;
        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            string key = Uri.UnescapeDataString(parts[0]);
            string? value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : null;
            values[key] = value;
        }

        return values;
    }
}
