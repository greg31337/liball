using System.Text;
using uspto;

namespace uspto.Tests;

public class OdpSearchRequestWriterTests
{
    [Test]
    public void GetQueryWritesEverySupportedParameterInCanonicalForm()
    {
        SearchRequest request = CreateCompleteRequest();

        Dictionary<string, string?> result = OdpSearchRequestWriter.WriteGetQuery(request, new UsptoLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(8));
            Assert.That(result["q"], Is.EqualTo("applicationMetaData.inventionTitle:battery"));
            Assert.That(result["filters"], Is.EqualTo("applicationMetaData.applicationTypeCode UTL,DES"));
            Assert.That(result["rangeFilters"], Is.EqualTo("applicationMetaData.grantDate 2024-01-01:2024-12-31"));
            Assert.That(result["sort"], Is.EqualTo("applicationMetaData.filingDate desc,applicationNumberText asc"));
            Assert.That(result["fields"], Is.EqualTo("applicationNumberText,applicationMetaData.patentNumber"));
            Assert.That(result["facets"], Is.EqualTo("applicationMetaData.applicationStatusCode"));
            Assert.That(result["offset"], Is.EqualTo("25"));
            Assert.That(result["limit"], Is.EqualTo("10"));
        });
    }

    [Test]
    public void PostBodyHasDeterministicPropertyOrderAndOdpShapes()
    {
        SearchRequest request = CreateCompleteRequest();

        string first = OdpSearchRequestWriter.WritePostBody(request, new UsptoLimits());
        string second = OdpSearchRequestWriter.WritePostBody(request, new UsptoLimits());

        const string expected = "{\"q\":\"applicationMetaData.inventionTitle:battery\",\"filters\":[{\"name\":\"applicationMetaData.applicationTypeCode\",\"value\":[\"UTL\",\"DES\"]}],\"rangeFilters\":[{\"field\":\"applicationMetaData.grantDate\",\"valueFrom\":\"2024-01-01\",\"valueTo\":\"2024-12-31\"}],\"sort\":[{\"field\":\"applicationMetaData.filingDate\",\"order\":\"desc\"},{\"field\":\"applicationNumberText\",\"order\":\"asc\"}],\"fields\":[\"applicationNumberText\",\"applicationMetaData.patentNumber\"],\"pagination\":{\"offset\":25,\"limit\":10},\"facets\":[\"applicationMetaData.applicationStatusCode\"]}";
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(expected));
            Assert.That(second, Is.EqualTo(expected));
        });
    }

    [Test]
    public void StructuredPredicatesAndUnrepresentableRangesAreCombinedWithRawQuery()
    {
        SearchRequest request = new SearchRequest
        {
            Query = "applicationMetaData.applicationTypeCode:UTL",
            Filters = new SearchFilter[]
            {
                new SearchFilter
                {
                    Field = "applicationMetaData.applicationStatusCode",
                    Operator = SearchOperator.GreaterThanOrEqual,
                    Value = "150"
                }
            },
            Ranges = new SearchRange[]
            {
                new SearchRange
                {
                    Field = "applicationMetaData.filingDate",
                    LowerValue = "2025-01-01",
                    UpperValue = null,
                    IncludeLower = false
                }
            }
        };

        Dictionary<string, string?> get = OdpSearchRequestWriter.WriteGetQuery(request, new UsptoLimits());
        string post = OdpSearchRequestWriter.WritePostBody(request, new UsptoLimits());

        const string query = "(applicationMetaData.applicationTypeCode:UTL) AND applicationMetaData.applicationStatusCode:>=150 AND applicationMetaData.filingDate:{2025\\-01\\-01 TO *]";
        Assert.Multiple(() =>
        {
            Assert.That(get["q"], Is.EqualTo(query));
            Assert.That(get.ContainsKey("filters"), Is.False);
            Assert.That(get.ContainsKey("rangeFilters"), Is.False);
            Assert.That(post, Does.StartWith("{\"q\":\""));
        });

        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(post);
        Assert.That(document.RootElement.GetProperty("q").GetString(), Is.EqualTo(query));
    }

    [Test]
    public void MultipleGetFiltersAndRangesFallBackToEquivalentQueryPredicates()
    {
        SearchRequest request = new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "first", Operator = SearchOperator.Equal, Value = "one" },
                new SearchFilter { Field = "second", Operator = SearchOperator.In, Values = new string[] { "two", "three" } }
            },
            Ranges = new SearchRange[]
            {
                new SearchRange { Field = "filed", LowerValue = "2024-01-01", UpperValue = "2024-12-31" },
                new SearchRange { Field = "granted", LowerValue = "2025-01-01", UpperValue = "2025-12-31" }
            }
        };

        Dictionary<string, string?> result = OdpSearchRequestWriter.WriteGetQuery(request, new UsptoLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.ContainsKey("filters"), Is.False);
            Assert.That(result.ContainsKey("rangeFilters"), Is.False);
            Assert.That(result["q"], Is.EqualTo("first:\"one\" AND (second:\"two\" OR second:\"three\") AND filed:[2024\\-01\\-01 TO 2024\\-12\\-31] AND granted:[2025\\-01\\-01 TO 2025\\-12\\-31]"));
        });
    }

    [Test]
    public void PostBodyKeepsMultipleFiltersAndRangesAsDedicatedArrays()
    {
        SearchRequest request = new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "first", Operator = SearchOperator.Equal, Value = "one,two" },
                new SearchFilter { Field = "second", Operator = SearchOperator.In, Values = new string[] { "three", "four" } }
            },
            Ranges = new SearchRange[]
            {
                new SearchRange { Field = "filed", LowerValue = "2024-02-29", UpperValue = "2024-12-31" },
                new SearchRange { Field = "granted", LowerValue = "2025-01-01", UpperValue = "2025-12-31" }
            }
        };

        string result = OdpSearchRequestWriter.WritePostBody(request, new UsptoLimits());

        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(result);
        System.Text.Json.JsonElement root = document.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.TryGetProperty("q", out _), Is.False);
            Assert.That(root.GetProperty("filters").GetArrayLength(), Is.EqualTo(2));
            Assert.That(root.GetProperty("filters")[0].GetProperty("value")[0].GetString(), Is.EqualTo("one,two"));
            Assert.That(root.GetProperty("rangeFilters").GetArrayLength(), Is.EqualTo(2));
            Assert.That(root.GetProperty("rangeFilters")[0].GetProperty("valueFrom").GetString(), Is.EqualTo("2024-02-29"));
        });
    }

    [Test]
    public void GetFallbackPreservesCommaFilterValuesWithoutDelimiterAmbiguity()
    {
        SearchRequest request = new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "title", Operator = SearchOperator.Equal, Value = "Alpha, Beta" }
            }
        };

        Dictionary<string, string?> result = OdpSearchRequestWriter.WriteGetQuery(request, new UsptoLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.ContainsKey("filters"), Is.False);
            Assert.That(result["q"], Is.EqualTo("title:\"Alpha, Beta\""));
        });
    }

    [Test]
    public void GetFallbackPreservesUnrepresentableMembershipAndRangeValues()
    {
        SearchRequest membership = new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "kind", Operator = SearchOperator.In, Values = new string[] { "A,B", "C" } }
            }
        };
        SearchRequest colonRange = new SearchRequest
        {
            Ranges = new SearchRange[]
            {
                new SearchRange { Field = "time", LowerValue = "10:00", UpperValue = "20:00" }
            }
        };
        SearchRequest whitespaceRange = new SearchRequest
        {
            Ranges = new SearchRange[]
            {
                new SearchRange { Field = "code", LowerValue = " A", UpperValue = "Z " }
            }
        };

        Dictionary<string, string?> membershipResult = OdpSearchRequestWriter.WriteGetQuery(membership, new UsptoLimits());
        Dictionary<string, string?> colonResult = OdpSearchRequestWriter.WriteGetQuery(colonRange, new UsptoLimits());
        Dictionary<string, string?> whitespaceResult = OdpSearchRequestWriter.WriteGetQuery(whitespaceRange, new UsptoLimits());

        Assert.Multiple(() =>
        {
            Assert.That(membershipResult.ContainsKey("filters"), Is.False);
            Assert.That(membershipResult["q"], Is.EqualTo("(kind:\"A,B\" OR kind:\"C\")"));
            Assert.That(colonResult.ContainsKey("rangeFilters"), Is.False);
            Assert.That(colonResult["q"], Is.EqualTo("time:[10\\:00 TO 20\\:00]"));
            Assert.That(whitespaceResult.ContainsKey("rangeFilters"), Is.False);
            Assert.That(whitespaceResult["q"], Is.EqualTo("code:[\\ A TO Z\\ ]"));
        });
    }

    [Test]
    public void GetAndPostPreserveDateBoundsVerbatim()
    {
        SearchRequest request = new SearchRequest
        {
            Ranges = new SearchRange[]
            {
                new SearchRange
                {
                    Field = "applicationMetaData.filingDate",
                    LowerValue = "2024-02-29",
                    UpperValue = "2026-09-02"
                }
            }
        };

        Dictionary<string, string?> get = OdpSearchRequestWriter.WriteGetQuery(request, new UsptoLimits());
        string post = OdpSearchRequestWriter.WritePostBody(request, new UsptoLimits());

        Assert.Multiple(() =>
        {
            Assert.That(get["rangeFilters"], Is.EqualTo("applicationMetaData.filingDate 2024-02-29:2026-09-02"));
            Assert.That(post, Does.Contain("\"valueFrom\":\"2024-02-29\""));
            Assert.That(post, Does.Contain("\"valueTo\":\"2026-09-02\""));
        });
    }

    [TestCase(-1L, 25, "INVALID_OFFSET")]
    [TestCase(2147483648L, 25, "INVALID_OFFSET")]
    [TestCase(0L, 0, "INVALID_LIMIT")]
    [TestCase(0L, -1, "INVALID_LIMIT")]
    public void InvalidPaginationNumbersAreRejected(long offset, int limit, string errorCode)
    {
        SearchRequest request = new SearchRequest
        {
            OffsetPage = new OffsetPage { Offset = offset, Limit = limit }
        };

        ValidationException? getException = Assert.Throws<ValidationException>(() => OdpSearchRequestWriter.WriteGetQuery(request, new UsptoLimits()));
        ValidationException? postException = Assert.Throws<ValidationException>(() => OdpSearchRequestWriter.WritePostBody(request, new UsptoLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(getException!.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(postException!.ErrorCode, Is.EqualTo(errorCode));
        });
    }

    [Test]
    public void CursorPaginationIsRejectedForOdpRequests()
    {
        SearchRequest request = new SearchRequest { UseCursorPage = true };

        ValidationException? exception = Assert.Throws<ValidationException>(() => OdpSearchRequestWriter.WritePostBody(request, new UsptoLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.FieldName, Is.EqualTo("UseCursorPage"));
            Assert.That(exception.ErrorCode, Is.EqualTo("UNSUPPORTED_PAGE_MODE"));
        });
    }

    [Test]
    public void EncodedGetQueryHonorsTheExactConfiguredBoundary()
    {
        SearchRequest request = new SearchRequest { Query = "é" };
        UsptoLimits exact = new UsptoLimits { MaximumUriCharacters = 28 };
        UsptoLimits tooSmall = new UsptoLimits { MaximumUriCharacters = 27 };

        Dictionary<string, string?> result = OdpSearchRequestWriter.WriteGetQuery(request, exact);
        ValidationException? exception = Assert.Throws<ValidationException>(() => OdpSearchRequestWriter.WriteGetQuery(request, tooSmall));

        Assert.Multiple(() =>
        {
            Assert.That(result["q"], Is.EqualTo("é"));
            Assert.That(exception!.ErrorCode, Is.EqualTo("REQUEST_URI_TOO_LARGE"));
            Assert.That(exception.FieldName, Is.EqualTo("MaximumUriCharacters"));
        });
    }

    [Test]
    public void CompleteEncodedGetUriHonorsTheConfiguredBoundary()
    {
        SearchRequest request = new SearchRequest { Query = "title:café" };
        Dictionary<string, string?> query = OdpSearchRequestWriter.WriteGetQuery(request, new UsptoLimits());
        Uri uri = RequestBuilder.BuildUri(
            new Uri("https://example.test/root/"),
            new string[] { "api", "v1", "patent", "applications", "search" },
            query);
        UsptoLimits exact = new UsptoLimits { MaximumUriCharacters = uri.AbsoluteUri.Length };
        UsptoLimits tooSmall = new UsptoLimits { MaximumUriCharacters = uri.AbsoluteUri.Length - 1 };

        OdpSearchRequestWriter.ValidateGetRequestUri(uri, exact);
        ValidationException? exception = Assert.Throws<ValidationException>(() => OdpSearchRequestWriter.ValidateGetRequestUri(uri, tooSmall));

        Assert.Multiple(() =>
        {
            Assert.That(uri.AbsoluteUri, Does.Contain("title%3Acaf%C3%A9"));
            Assert.That(exception!.ErrorCode, Is.EqualTo("REQUEST_URI_TOO_LARGE"));
            Assert.That(exception.FieldName, Is.EqualTo("MaximumUriCharacters"));
        });
    }

    [Test]
    public void Utf8PostBodyHonorsTheExactConfiguredBoundary()
    {
        SearchRequest request = new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "title", Operator = SearchOperator.Equal, Value = "café" }
            }
        };
        string body = OdpSearchRequestWriter.WritePostBody(request, new UsptoLimits());
        int bytes = Encoding.UTF8.GetByteCount(body);
        UsptoLimits exact = new UsptoLimits { MaximumRequestBytes = bytes };
        UsptoLimits tooSmall = new UsptoLimits { MaximumRequestBytes = bytes - 1 };

        string exactResult = OdpSearchRequestWriter.WritePostBody(request, exact);
        ValidationException? exception = Assert.Throws<ValidationException>(() => OdpSearchRequestWriter.WritePostBody(request, tooSmall));

        Assert.Multiple(() =>
        {
            Assert.That(exactResult, Is.EqualTo(body));
            Assert.That(exception!.ErrorCode, Is.EqualTo("REQUEST_BODY_TOO_LARGE"));
            Assert.That(exception.FieldName, Is.EqualTo("MaximumRequestBytes"));
        });
    }

    [Test]
    public void GetAndPostValidateOnlyTheirApplicableSizeLimit()
    {
        SearchRequest request = new SearchRequest();
        UsptoLimits getLimits = new UsptoLimits { MaximumUriCharacters = 100, MaximumRequestBytes = 0L };
        UsptoLimits postLimits = new UsptoLimits { MaximumUriCharacters = 0, MaximumRequestBytes = 100L };

        Dictionary<string, string?> get = OdpSearchRequestWriter.WriteGetQuery(request, getLimits);
        string post = OdpSearchRequestWriter.WritePostBody(request, postLimits);

        Assert.Multiple(() =>
        {
            Assert.That(get["offset"], Is.EqualTo("0"));
            Assert.That(post, Is.EqualTo("{\"pagination\":{\"offset\":0,\"limit\":100}}"));
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("line\nbreak")]
    public void InvalidRawQueriesAreRejected(string query)
    {
        SearchRequest request = new SearchRequest { Query = query };

        ValidationException? exception = Assert.Throws<ValidationException>(() => OdpSearchRequestWriter.WriteGetQuery(request, new UsptoLimits()));

        Assert.That(exception!.ErrorCode, Is.EqualTo("INVALID_QUERY_LITERAL"));
    }

    [Test]
    public void MissingRequestCollectionsHaveStructuredValidationErrors()
    {
        SearchRequest request = new SearchRequest { Filters = null! };

        ValidationException? exception = Assert.Throws<ValidationException>(() => OdpSearchRequestWriter.WritePostBody(request, new UsptoLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.FieldName, Is.EqualTo("Filters"));
            Assert.That(exception.ErrorCode, Is.EqualTo("REQUIRED"));
        });
    }

    private static SearchRequest CreateCompleteRequest()
    {
        return new SearchRequest
        {
            Query = "applicationMetaData.inventionTitle:battery",
            Filters = new SearchFilter[]
            {
                new SearchFilter
                {
                    Field = "applicationMetaData.applicationTypeCode",
                    Operator = SearchOperator.In,
                    Values = new string[] { "UTL", "DES" }
                }
            },
            Ranges = new SearchRange[]
            {
                new SearchRange
                {
                    Field = "applicationMetaData.grantDate",
                    LowerValue = "2024-01-01",
                    UpperValue = "2024-12-31"
                }
            },
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "applicationMetaData.filingDate", Direction = SortDirection.Descending },
                new SearchSort { Field = "applicationNumberText", Direction = SortDirection.Ascending }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "applicationNumberText", "applicationMetaData.patentNumber" }
            },
            Facets = new SearchFacetRequest[]
            {
                new SearchFacetRequest { Field = "applicationMetaData.applicationStatusCode" }
            },
            OffsetPage = new OffsetPage { Offset = 25L, Limit = 10 }
        };
    }
}
