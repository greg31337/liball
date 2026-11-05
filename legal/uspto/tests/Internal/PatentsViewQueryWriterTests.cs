using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class PatentsViewQueryWriterTests
{
    [TestCase(SearchOperator.Equal, "_eq")]
    [TestCase(SearchOperator.NotEqual, "_neq")]
    [TestCase(SearchOperator.LessThan, "_lt")]
    [TestCase(SearchOperator.LessThanOrEqual, "_lte")]
    [TestCase(SearchOperator.GreaterThan, "_gt")]
    [TestCase(SearchOperator.GreaterThanOrEqual, "_gte")]
    public void ScalarOperatorsUsePatentsViewComparisonObjects(SearchOperator searchOperator, string operatorName)
    {
        SearchFilter filter = new SearchFilter
        {
            Field = "patent_date",
            Operator = searchOperator,
            Value = "2024-01-02"
        };

        string result = PatentsViewQueryWriter.WriteQuery(filter);

        Assert.That(result, Is.EqualTo("{\"" + operatorName + "\":{\"patent_date\":\"2024-01-02\"}}"));
    }

    [Test]
    public void TextQueriesUseTheRecommendedFullTextOperatorAndEscapeJson()
    {
        string direct = PatentsViewQueryWriter.WriteText("patent_abstract", "café \"battery\"");
        string fromFilter = PatentsViewQueryWriter.WriteQuery(new SearchFilter
        {
            Field = "patent_abstract",
            Operator = SearchOperator.Contains,
            Value = "machine learning"
        });

        using JsonDocument directDocument = JsonDocument.Parse(direct);
        Assert.Multiple(() =>
        {
            Assert.That(directDocument.RootElement.GetProperty("_text_all").GetProperty("patent_abstract").GetString(), Is.EqualTo("café \"battery\""));
            Assert.That(fromFilter, Is.EqualTo("{\"_text_all\":{\"patent_abstract\":\"machine learning\"}}"));
        });
    }

    [Test]
    public void SetMembershipWritesAValueArrayInCallerOrder()
    {
        SearchFilter filter = new SearchFilter
        {
            Field = "inventors.inventor_name_last",
            Operator = SearchOperator.In,
            Values = new string[] { "Whitney", "Hopper", "O'Connor" }
        };

        string result = PatentsViewQueryWriter.WriteQuery(filter);

        using JsonDocument document = JsonDocument.Parse(result);
        JsonElement values = document.RootElement.GetProperty("inventors.inventor_name_last");
        Assert.That(
            new string[] { values[0].GetString() ?? string.Empty, values[1].GetString() ?? string.Empty, values[2].GetString() ?? string.Empty },
            Is.EqualTo(new string[] { "Whitney", "Hopper", "O'Connor" }));
    }

    [Test]
    public void LogicalTreesWriteAndOrAndNotWithoutLosingGrouping()
    {
        SearchFilter filter = new SearchFilter
        {
            Operator = SearchOperator.And,
            Children = new SearchFilter[]
            {
                new SearchFilter { Field = "patent_type", Operator = SearchOperator.Equal, Value = "utility" },
                new SearchFilter
                {
                    Operator = SearchOperator.Or,
                    Children = new SearchFilter[]
                    {
                        new SearchFilter { Field = "patent_date", Operator = SearchOperator.GreaterThanOrEqual, Value = "2024-01-01" },
                        new SearchFilter
                        {
                            Operator = SearchOperator.Not,
                            Children = new SearchFilter[]
                            {
                                new SearchFilter { Field = "withdrawn", Operator = SearchOperator.Equal, Value = "true" }
                            }
                        }
                    }
                }
            }
        };

        string result = PatentsViewQueryWriter.WriteQuery(filter);

        const string expected = "{\"_and\":[{\"_eq\":{\"patent_type\":\"utility\"}},{\"_or\":[{\"_gte\":{\"patent_date\":\"2024-01-01\"}},{\"_not\":{\"_eq\":{\"withdrawn\":\"true\"}}}]}]}";
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void BoundedAndOpenRangesUseInclusiveAndExclusiveComparisons()
    {
        SearchRequest bounded = CreateRequest();
        bounded.Filters = new SearchFilter[0];
        bounded.Ranges = new SearchRange[]
        {
            new SearchRange
            {
                Field = "patent_date",
                LowerValue = "2020-01-01",
                UpperValue = "2020-12-31",
                IncludeLower = true,
                IncludeUpper = false
            }
        };
        SearchRequest open = CreateRequest();
        open.Filters = new SearchFilter[0];
        open.Ranges = new SearchRange[]
        {
            new SearchRange
            {
                Field = "patent_date",
                LowerValue = null,
                UpperValue = "2020-12-31",
                IncludeUpper = true
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(PatentsViewQueryWriter.WriteSearchQuery(bounded), Is.EqualTo("{\"_and\":[{\"_gte\":{\"patent_date\":\"2020-01-01\"}},{\"_lt\":{\"patent_date\":\"2020-12-31\"}}]}"));
            Assert.That(PatentsViewQueryWriter.WriteSearchQuery(open), Is.EqualTo("{\"_lte\":{\"patent_date\":\"2020-12-31\"}}"));
        });
    }

    [TestCase("10", "20", true, true, "{\"_and\":[{\"_gte\":{\"score\":\"10\"}},{\"_lte\":{\"score\":\"20\"}}]}")]
    [TestCase("10", "20", true, false, "{\"_and\":[{\"_gte\":{\"score\":\"10\"}},{\"_lt\":{\"score\":\"20\"}}]}")]
    [TestCase("10", "20", false, true, "{\"_and\":[{\"_gt\":{\"score\":\"10\"}},{\"_lte\":{\"score\":\"20\"}}]}")]
    [TestCase("10", "20", false, false, "{\"_and\":[{\"_gt\":{\"score\":\"10\"}},{\"_lt\":{\"score\":\"20\"}}]}")]
    [TestCase("10", null, true, true, "{\"_gte\":{\"score\":\"10\"}}")]
    [TestCase("10", null, false, true, "{\"_gt\":{\"score\":\"10\"}}")]
    [TestCase(null, "20", true, true, "{\"_lte\":{\"score\":\"20\"}}")]
    [TestCase(null, "20", true, false, "{\"_lt\":{\"score\":\"20\"}}")]
    public void EveryPatentsViewRangeFormUsesTheMatchingComparisonOperators(string? lower, string? upper, bool includeLower, bool includeUpper, string expected)
    {
        SearchRequest request = CreateRequest();
        request.Ranges = new SearchRange[]
        {
            new SearchRange
            {
                Field = "score",
                LowerValue = lower,
                UpperValue = upper,
                IncludeLower = includeLower,
                IncludeUpper = includeUpper
            }
        };

        string result = PatentsViewQueryWriter.WriteSearchQuery(request);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void RequestPredicatesAndRangesAreJoinedInStableOrder()
    {
        SearchRequest request = CreateRequest();
        request.Filters = new SearchFilter[]
        {
            new SearchFilter { Field = "patent_type", Operator = SearchOperator.Equal, Value = "utility" },
            new SearchFilter { Field = "inventors.inventor_name_last", Operator = SearchOperator.In, Values = new string[] { "Whitney", "Hopper" } }
        };
        request.Ranges = new SearchRange[]
        {
            new SearchRange { Field = "patent_date", LowerValue = "2024-01-01", UpperValue = null }
        };

        string result = PatentsViewQueryWriter.WriteSearchQuery(request);

        Assert.That(result, Is.EqualTo("{\"_and\":[{\"_eq\":{\"patent_type\":\"utility\"}},{\"inventors.inventor_name_last\":[\"Whitney\",\"Hopper\"]},{\"_gte\":{\"patent_date\":\"2024-01-01\"}}]}"));
        Assert.That(PatentsViewQueryWriter.WriteSearchQuery(CreateRequest()), Is.EqualTo("{}"));
    }

    [Test]
    public void FieldsAndSortsHaveCanonicalJsonShapes()
    {
        SearchFieldSelection fields = new SearchFieldSelection
        {
            Fields = new string[] { "patent_id", "inventors.inventor_name_last", "patent_title" }
        };
        SearchSort[] sorts = new SearchSort[]
        {
            new SearchSort { Field = "patent_date", Direction = SortDirection.Descending },
            new SearchSort { Field = "patent_id", Direction = SortDirection.Ascending }
        };

        Assert.Multiple(() =>
        {
            Assert.That(PatentsViewQueryWriter.WriteFields(fields), Is.EqualTo("[\"patent_id\",\"inventors.inventor_name_last\",\"patent_title\"]"));
            Assert.That(PatentsViewQueryWriter.WriteSorts(sorts), Is.EqualTo("[{\"patent_date\":\"desc\"},{\"patent_id\":\"asc\"}]"));
        });
    }

    [Test]
    public void OptionsUseScalarAndArrayCursorsAsDocumented()
    {
        SearchSort[] oneSort = new SearchSort[]
        {
            new SearchSort { Field = "patent_id", Direction = SortDirection.Ascending }
        };
        SearchSort[] twoSorts = new SearchSort[]
        {
            new SearchSort { Field = "patent_date", Direction = SortDirection.Descending },
            new SearchSort { Field = "patent_id", Direction = SortDirection.Ascending }
        };

        string firstPage = PatentsViewQueryWriter.WriteOptions(new CursorPage { Size = 100 }, oneSort);
        string scalar = PatentsViewQueryWriter.WriteOptions(new CursorPage { Size = 100, AfterValues = new string[] { "11191202" } }, oneSort);
        string array = PatentsViewQueryWriter.WriteOptions(new CursorPage { Size = 25, AfterValues = new string[] { "2024-10-01", "US12345678" } }, twoSorts);

        Assert.Multiple(() =>
        {
            Assert.That(firstPage, Is.EqualTo("{\"size\":100}"));
            Assert.That(scalar, Is.EqualTo("{\"size\":100,\"after\":\"11191202\"}"));
            Assert.That(array, Is.EqualTo("{\"size\":25,\"after\":[\"2024-10-01\",\"US12345678\"]}"));
        });
    }

    [Test]
    public void GetQueryWritesEveryPatentsViewParameter()
    {
        SearchRequest request = CreateCompleteRequest();

        Dictionary<string, string?> result = PatentsViewQueryWriter.WriteGetQuery(request, new UsptoLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(4));
            Assert.That(result["q"], Is.EqualTo("{\"_eq\":{\"patent_type\":\"utility\"}}"));
            Assert.That(result["f"], Is.EqualTo("[\"patent_id\",\"patent_date\"]"));
            Assert.That(result["s"], Is.EqualTo("[{\"patent_date\":\"desc\"},{\"patent_id\":\"asc\"}]"));
            Assert.That(result["o"], Is.EqualTo("{\"size\":25,\"after\":[\"2024-10-01\",\"US12345678\"]}"));
        });
    }

    [Test]
    public void PostBodyHasDeterministicPatentsViewPropertyOrder()
    {
        SearchRequest request = CreateCompleteRequest();

        string first = PatentsViewQueryWriter.WritePostBody(request, new UsptoLimits());
        string second = PatentsViewQueryWriter.WritePostBody(request, new UsptoLimits());

        const string expected = "{\"q\":{\"_eq\":{\"patent_type\":\"utility\"}},\"f\":[\"patent_id\",\"patent_date\"],\"s\":[{\"patent_date\":\"desc\"},{\"patent_id\":\"asc\"}],\"o\":{\"size\":25,\"after\":[\"2024-10-01\",\"US12345678\"]}}";
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(expected));
            Assert.That(second, Is.EqualTo(expected));
        });
    }

    [Test]
    public void OptionalFieldsAndSortsAreOmittedButQueryAndOptionsRemain()
    {
        SearchRequest request = CreateRequest();

        Dictionary<string, string?> get = PatentsViewQueryWriter.WriteGetQuery(request, new UsptoLimits());
        string post = PatentsViewQueryWriter.WritePostBody(request, new UsptoLimits());

        Assert.Multiple(() =>
        {
            Assert.That(get, Has.Count.EqualTo(2));
            Assert.That(get["q"], Is.EqualTo("{}"));
            Assert.That(get["o"], Is.EqualTo("{\"size\":100}"));
            Assert.That(post, Is.EqualTo("{\"q\":{},\"o\":{\"size\":100}}"));
        });
    }

    [Test]
    public void PostBodyUsesUtf8ByteLimitsAtTheExactBoundary()
    {
        SearchRequest request = CreateRequest();
        request.Filters = new SearchFilter[]
        {
            new SearchFilter { Field = "patent_title", Operator = SearchOperator.Contains, Value = "café" }
        };
        string body = PatentsViewQueryWriter.WritePostBody(request, new UsptoLimits());
        int bytes = Encoding.UTF8.GetByteCount(body);

        string exact = PatentsViewQueryWriter.WritePostBody(request, new UsptoLimits { MaximumRequestBytes = bytes });
        ValidationException exception = Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WritePostBody(
            request,
            new UsptoLimits { MaximumRequestBytes = bytes - 1 }));

        Assert.Multiple(() =>
        {
            Assert.That(exact, Is.EqualTo(body));
            Assert.That(exception.ErrorCode, Is.EqualTo("REQUEST_BODY_TOO_LARGE"));
        });
    }

    [Test]
    public void GetQueryAppliesTheEncodedCharacterLimit()
    {
        SearchRequest request = CreateRequest();
        request.Filters = new SearchFilter[]
        {
            new SearchFilter { Field = "patent_title", Operator = SearchOperator.Equal, Value = "café" }
        };

        ValidationException exception = Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WriteGetQuery(
            request,
            new UsptoLimits { MaximumUriCharacters = 10 }));

        Assert.That(exception.ErrorCode, Is.EqualTo("REQUEST_URI_TOO_LARGE"));
    }

    [Test]
    public void CompleteEncodedUriAcceptsTheExactBoundaryAndRejectsOneCharacterLess()
    {
        Uri uri = RequestBuilder.BuildUri(
            new Uri("https://example.test/custom/base/"),
            new[] { "api", "v1", "publication", "US 2024/é#1" },
            new Dictionary<string, string?> { ["q"] = "{\"title\":\"café & battery\"}" });
        UsptoLimits exact = new UsptoLimits { MaximumUriCharacters = uri.OriginalString.Length };

        PatentsViewQueryWriter.ValidateGetRequestUri(uri, exact);
        ValidationException failure = Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.ValidateGetRequestUri(
            uri,
            new UsptoLimits { MaximumUriCharacters = uri.OriginalString.Length - 1 }))!;

        Assert.Multiple(() =>
        {
            Assert.That(uri.OriginalString, Is.EqualTo("https://example.test/custom/base/api/v1/publication/US%202024%2F%C3%A9%231?q=%7B%22title%22%3A%22caf%C3%A9%20%26%20battery%22%7D"));
            Assert.That(failure.ErrorCode, Is.EqualTo("REQUEST_URI_TOO_LARGE"));
            Assert.That(failure.FieldName, Is.EqualTo("MaximumUriCharacters"));
        });
    }

    [TestCase(0, "PAGE_SIZE_OUT_OF_RANGE")]
    [TestCase(1001, "PAGE_SIZE_OUT_OF_RANGE")]
    public void InvalidCursorSizesAreRejected(int size, string errorCode)
    {
        SearchRequest request = CreateRequest();
        request.CursorPage.Size = size;

        ValidationException exception = Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WritePostBody(request, new UsptoLimits()));

        Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
    }

    [Test]
    public void DocumentedMaximumPageSizePreservesSelectedFieldsAndCursorValues()
    {
        SearchRequest request = CreateCompleteRequest();
        request.CursorPage.Size = 1000;

        string body = PatentsViewQueryWriter.WritePostBody(request, new UsptoLimits());

        using JsonDocument document = JsonDocument.Parse(body);
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("f").EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[] { "patent_id", "patent_date" }));
            Assert.That(document.RootElement.GetProperty("o").GetProperty("size").GetInt32(), Is.EqualTo(1000));
            Assert.That(document.RootElement.GetProperty("o").GetProperty("after").EnumerateArray().Select(value => value.GetString()), Is.EqualTo(new[] { "2024-10-01", "US12345678" }));
        });
    }

    [Test]
    public void CursorValuesMustMatchSortsAndCannotBeBlank()
    {
        SearchRequest mismatch = CreateRequest();
        mismatch.Sorts = new SearchSort[] { new SearchSort { Field = "patent_id" } };
        mismatch.CursorPage.AfterValues = new string[] { "one", "two" };
        SearchRequest blank = CreateRequest();
        blank.Sorts = new SearchSort[] { new SearchSort { Field = "patent_id" } };
        blank.CursorPage.AfterValues = new string[] { "   " };

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WritePostBody(mismatch, new UsptoLimits()))!.ErrorCode, Is.EqualTo("CURSOR_SORT_MISMATCH"));
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WritePostBody(blank, new UsptoLimits()))!.ErrorCode, Is.EqualTo("INVALID_CURSOR"));
        });
    }

    [Test]
    public void OffsetModeRawQueriesAndFacetsAreRejected()
    {
        SearchRequest offset = new SearchRequest();
        SearchRequest raw = CreateRequest();
        raw.Query = "{\"patent_id\":\"123\"}";
        SearchRequest facet = CreateRequest();
        facet.Facets = new SearchFacetRequest[] { new SearchFacetRequest { Field = "patent_type" } };

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WritePostBody(offset, new UsptoLimits()))!.ErrorCode, Is.EqualTo("CURSOR_PAGE_REQUIRED"));
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WritePostBody(raw, new UsptoLimits()))!.ErrorCode, Is.EqualTo("UNSUPPORTED_SEARCH_OPTION"));
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WritePostBody(facet, new UsptoLimits()))!.ErrorCode, Is.EqualTo("UNSUPPORTED_SEARCH_OPTION"));
        });
    }

    [TestCase("bad field")]
    [TestCase("bad:field")]
    [TestCase(".bad")]
    [TestCase("bad..field")]
    [TestCase("bad.")]
    public void InvalidFieldNamesAreRejected(string field)
    {
        SearchFilter filter = new SearchFilter { Field = field, Operator = SearchOperator.Equal, Value = "value" };

        ValidationException exception = Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WriteQuery(filter));

        Assert.That(exception.ErrorCode, Is.EqualTo("INVALID_FIELD_NAME"));
    }

    [Test]
    public void DuplicateFieldsSortsAndInvalidDirectionsAreRejected()
    {
        SearchFieldSelection fields = new SearchFieldSelection { Fields = new string[] { "patent_id", "PATENT_ID" } };
        SearchSort[] duplicateSorts = new SearchSort[]
        {
            new SearchSort { Field = "patent_id" },
            new SearchSort { Field = "PATENT_ID" }
        };
        SearchSort[] invalidSort = new SearchSort[]
        {
            new SearchSort { Field = "patent_id", Direction = (SortDirection)99 }
        };

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WriteFields(fields))!.ErrorCode, Is.EqualTo("DUPLICATE_FIELD"));
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WriteSorts(duplicateSorts))!.ErrorCode, Is.EqualTo("DUPLICATE_SORT"));
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WriteSorts(invalidSort))!.ErrorCode, Is.EqualTo("INVALID_SORT"));
        });
    }

    [Test]
    public void InvalidPredicateShapesAndValuesAreRejected()
    {
        SearchFilter missingValue = new SearchFilter { Field = "patent_id", Operator = SearchOperator.Equal };
        SearchFilter shortAnd = new SearchFilter
        {
            Operator = SearchOperator.And,
            Children = new SearchFilter[]
            {
                new SearchFilter { Field = "patent_id", Operator = SearchOperator.Equal, Value = "123" }
            }
        };
        SearchFilter control = new SearchFilter { Field = "patent_title", Operator = SearchOperator.Contains, Value = "line\nbreak" };

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WriteQuery(missingValue))!.ErrorCode, Is.EqualTo("INVALID_PREDICATE_SHAPE"));
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WriteQuery(shortAnd))!.ErrorCode, Is.EqualTo("INVALID_PREDICATE_CHILDREN"));
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WriteQuery(control))!.ErrorCode, Is.EqualTo("INVALID_QUERY_VALUE"));
        });
    }

    [Test]
    public void EmptyRangesAndExcessivePredicateDepthAreRejected()
    {
        SearchRequest rangeRequest = CreateRequest();
        rangeRequest.Ranges = new SearchRange[] { new SearchRange { Field = "patent_date" } };
        SearchFilter predicate = new SearchFilter { Field = "patent_id", Operator = SearchOperator.Equal, Value = "123" };
        for (int index = 0; index < PatentsViewQueryWriter.MaximumPredicateDepth; index += 1)
        {
            predicate = new SearchFilter { Operator = SearchOperator.Not, Children = new SearchFilter[] { predicate } };
        }

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WriteSearchQuery(rangeRequest))!.ErrorCode, Is.EqualTo("RANGE_BOUND_REQUIRED"));
            Assert.That(Assert.Throws<ValidationException>(() => PatentsViewQueryWriter.WriteQuery(predicate))!.ErrorCode, Is.EqualTo("PREDICATE_DEPTH_EXCEEDED"));
        });
    }

    private static SearchRequest CreateRequest()
    {
        return new SearchRequest
        {
            UseCursorPage = true,
            CursorPage = new CursorPage { Size = 100 }
        };
    }

    private static SearchRequest CreateCompleteRequest()
    {
        return new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "patent_type", Operator = SearchOperator.Equal, Value = "utility" }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "patent_id", "patent_date" }
            },
            Sorts = new SearchSort[]
            {
                new SearchSort { Field = "patent_date", Direction = SortDirection.Descending },
                new SearchSort { Field = "patent_id", Direction = SortDirection.Ascending }
            },
            UseCursorPage = true,
            CursorPage = new CursorPage
            {
                Size = 25,
                AfterValues = new string[] { "2024-10-01", "US12345678" }
            }
        };
    }
}
