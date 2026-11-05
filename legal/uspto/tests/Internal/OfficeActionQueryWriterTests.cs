using System.Text;
using uspto;

namespace uspto.Tests;

public class OfficeActionQueryWriterTests
{
    [TestCase("14/412,875", "14412875")]
    [TestCase("US 14 412 875", "14412875")]
    [TestCase("09123456", "09123456")]
    public void ApplicationNumbersAreNormalizedToTheDsapiIdentifier(string input, string expected)
    {
        string result = OfficeActionQueryWriter.NormalizeApplicationNumber(input);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ApplicationCriteriaUsesTheExactAppIdField()
    {
        string result = OfficeActionQueryWriter.WriteApplicationCriteria("14/412,875");

        Assert.That(result, Is.EqualTo("app_id:\"14412875\""));
    }

    [TestCase(null, "REQUIRED")]
    [TestCase("2023/0000001 A1", "UNSUPPORTED_APPLICATION_PREFIX")]
    [TestCase("PCT/US2024/012345", "UNSUPPORTED_APPLICATION_PREFIX")]
    [TestCase("14/000,000", "INVALID_APPLICATION_SERIAL")]
    public void InvalidApplicationNumbersRetainStructuredParserErrors(string? input, string errorCode)
    {
        ValidationException exception = Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.NormalizeApplicationNumber(input));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(exception.FieldName, Is.EqualTo("identifier"));
        });
    }

    [Test]
    public void LuceneLiteralEscapesReservedCharactersAndWhitespace()
    {
        string result = OfficeActionQueryWriter.EscapeLuceneLiteral("alpha +-&|!(){}[]^\"~*?:\\/ beta");

        Assert.That(result, Is.EqualTo("""alpha\ \+\-\&\|\!\(\)\{\}\[\]\^\"\~\*\?\:\\\/\ beta"""));
    }

    [Test]
    public void ExactAndContainsValuesCannotChangeTheQueryStructure()
    {
        string equality = OfficeActionQueryWriter.WriteEquality("paragraph_text", "battery \"pack\" \\ case");
        string inequality = OfficeActionQueryWriter.WritePredicate(new SearchFilter
        {
            Field = "paragraph_text",
            Operator = SearchOperator.NotEqual,
            Value = "battery \"pack\" \\ case"
        });
        string contains = OfficeActionQueryWriter.WritePredicate(new SearchFilter
        {
            Field = "paragraph_text",
            Operator = SearchOperator.Contains,
            Value = "battery* OR app_id:?"
        });

        Assert.Multiple(() =>
        {
            Assert.That(equality, Is.EqualTo("paragraph_text:\"battery \\\"pack\\\" \\\\ case\""));
            Assert.That(inequality, Is.EqualTo("(NOT paragraph_text:\"battery \\\"pack\\\" \\\\ case\")"));
            Assert.That(contains, Is.EqualTo("paragraph_text:*battery\\*\\ OR\\ app_id\\:\\?*"));
        });
    }

    [TestCase(SearchOperator.LessThan, "filing_date:{* TO 2024\\-01\\-01}")]
    [TestCase(SearchOperator.LessThanOrEqual, "filing_date:[* TO 2024\\-01\\-01]")]
    [TestCase(SearchOperator.GreaterThan, "filing_date:{2024\\-01\\-01 TO *}")]
    [TestCase(SearchOperator.GreaterThanOrEqual, "filing_date:[2024\\-01\\-01 TO *]")]
    public void ComparisonsUseLuceneRangeSyntax(SearchOperator searchOperator, string expected)
    {
        string result = OfficeActionQueryWriter.WriteComparison("filing_date", searchOperator, "2024-01-01");

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void RangesPreserveOpenAndExclusiveBounds()
    {
        SearchRange bounded = new SearchRange
        {
            Field = "mail_date",
            LowerValue = "2024-01-01",
            UpperValue = "2024-12-31",
            IncludeLower = true,
            IncludeUpper = false
        };
        SearchRange open = new SearchRange
        {
            Field = "mail_date",
            LowerValue = null,
            UpperValue = "2024-12-31",
            IncludeLower = false,
            IncludeUpper = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(OfficeActionQueryWriter.WriteRange(bounded), Is.EqualTo("mail_date:[2024\\-01\\-01 TO 2024\\-12\\-31}"));
            Assert.That(OfficeActionQueryWriter.WriteRange(open), Is.EqualTo("mail_date:{* TO 2024\\-12\\-31]"));
        });
    }

    [TestCase("2024-01-01", "2024-12-31", true, true, "mail_date:[2024\\-01\\-01 TO 2024\\-12\\-31]")]
    [TestCase("2024-01-01", "2024-12-31", true, false, "mail_date:[2024\\-01\\-01 TO 2024\\-12\\-31}")]
    [TestCase("2024-01-01", "2024-12-31", false, true, "mail_date:{2024\\-01\\-01 TO 2024\\-12\\-31]")]
    [TestCase("2024-01-01", "2024-12-31", false, false, "mail_date:{2024\\-01\\-01 TO 2024\\-12\\-31}")]
    [TestCase("2024-01-01", null, true, true, "mail_date:[2024\\-01\\-01 TO *]")]
    [TestCase("2024-01-01", null, false, true, "mail_date:{2024\\-01\\-01 TO *]")]
    [TestCase(null, "2024-12-31", true, true, "mail_date:[* TO 2024\\-12\\-31]")]
    [TestCase(null, "2024-12-31", true, false, "mail_date:[* TO 2024\\-12\\-31}")]
    public void EveryOfficeActionRangeFormEscapesItsBounds(string? lower, string? upper, bool includeLower, bool includeUpper, string expected)
    {
        SearchRange range = new SearchRange
        {
            Field = "mail_date",
            LowerValue = lower,
            UpperValue = upper,
            IncludeLower = includeLower,
            IncludeUpper = includeUpper
        };

        string result = OfficeActionQueryWriter.WriteRange(range);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void PredicateTreesSerializeWithExplicitGrouping()
    {
        SearchFilter predicate = new SearchFilter
        {
            Operator = SearchOperator.And,
            Children = new SearchFilter[]
            {
                new SearchFilter
                {
                    Field = "app_id",
                    Operator = SearchOperator.In,
                    Values = new string[] { "14412875", "09123456" }
                },
                new SearchFilter
                {
                    Operator = SearchOperator.Or,
                    Children = new SearchFilter[]
                    {
                        new SearchFilter { Field = "section_code", Operator = SearchOperator.Equal, Value = "102" },
                        new SearchFilter
                        {
                            Operator = SearchOperator.Not,
                            Children = new SearchFilter[]
                            {
                                new SearchFilter { Field = "paragraph_text", Operator = SearchOperator.Contains, Value = "withdrawn" }
                            }
                        }
                    }
                }
            }
        };

        string result = OfficeActionQueryWriter.WritePredicate(predicate);

        Assert.That(result, Is.EqualTo("((app_id:\"14412875\" OR app_id:\"09123456\") AND (section_code:\"102\" OR (NOT paragraph_text:*withdrawn*)))"));
    }

    [Test]
    public void CriteriaCombinesRawStructuredAndRangeClausesInCallerOrder()
    {
        SearchRequest request = new SearchRequest
        {
            Query = "document_cd:CTNF",
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "app_id", Operator = SearchOperator.Equal, Value = "14412875" }
            },
            Ranges = new SearchRange[]
            {
                new SearchRange { Field = "mail_date", LowerValue = "2024-01-01", UpperValue = null }
            }
        };

        string result = OfficeActionQueryWriter.WriteCriteria(request);

        Assert.That(result, Is.EqualTo("(document_cd:CTNF) AND app_id:\"14412875\" AND mail_date:[2024\\-01\\-01 TO *]"));
        Assert.That(OfficeActionQueryWriter.WriteCriteria(new SearchRequest()), Is.EqualTo("*:*"));
    }

    [Test]
    public void FormFieldsContainOnlyDocumentedDsapiValues()
    {
        SearchRequest request = new SearchRequest
        {
            Filters = new SearchFilter[]
            {
                new SearchFilter { Field = "app_id", Operator = SearchOperator.Equal, Value = "14412875" }
            },
            OffsetPage = new OffsetPage { Offset = 10L, Limit = 25 }
        };

        Dictionary<string, string> result = OfficeActionQueryWriter.WriteFormFields(request, new UsptoLimits());

        Assert.That(result, Is.EquivalentTo(new Dictionary<string, string>
        {
            ["criteria"] = "app_id:\"14412875\"",
            ["start"] = "10",
            ["rows"] = "25"
        }));
    }

    [Test]
    public async Task RequestWriterBuildsTheDatasetUriAndDeterministicEncodedBody()
    {
        SearchRequest search = new SearchRequest
        {
            Query = "paragraph_text:\"café & battery\"",
            OffsetPage = new OffsetPage { Offset = 4L, Limit = 20 }
        };
        using HttpRequestMessage request = OfficeActionQueryWriter.CreateFormRequest(
            new Uri("https://developer.uspto.gov/"),
            "oa_actions",
            "v1",
            search,
            new UsptoLimits());

        string body = await request.Content!.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.RequestUri?.AbsoluteUri, Is.EqualTo("https://developer.uspto.gov/api/v1/patent/oa/oa_actions/v1/records"));
            Assert.That(request.Content.Headers.ContentType?.MediaType, Is.EqualTo(UsptoMediaTypes.FormUrlEncoded));
            Assert.That(body, Is.EqualTo("criteria=paragraph_text%3A%22caf%C3%A9+%26+battery%22&rows=20&start=4"));
        });
    }

    [Test]
    public async Task EncodedBodySizeAcceptsTheExactBoundaryAndRejectsOneByteLess()
    {
        SearchRequest request = new SearchRequest { Query = "paragraph_text:café" };
        UsptoLimits generous = new UsptoLimits { MaximumRequestBytes = 1000L };
        using HttpRequestMessage message = OfficeActionQueryWriter.CreateFormRequest(
            new Uri("https://example.test/"), "oa_actions", "v1", request, generous);
        string body = await message.Content!.ReadAsStringAsync();
        long encodedBytes = Encoding.UTF8.GetByteCount(body);

        UsptoLimits exact = new UsptoLimits { MaximumRequestBytes = encodedBytes };
        Dictionary<string, string> fields = OfficeActionQueryWriter.WriteFormFields(request, exact);
        ValidationException exception = Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.WriteFormFields(
            request,
            new UsptoLimits { MaximumRequestBytes = encodedBytes - 1L }));

        Assert.Multiple(() =>
        {
            Assert.That(fields["criteria"], Is.EqualTo("paragraph_text:café"));
            Assert.That(exception.ErrorCode, Is.EqualTo("REQUEST_BODY_TOO_LARGE"));
        });
    }

    [Test]
    public void PaginationValuesAreValidatedBeforeWriting()
    {
        SearchRequest negative = new SearchRequest { OffsetPage = new OffsetPage { Offset = -1L, Limit = 10 } };
        SearchRequest overflow = new SearchRequest { OffsetPage = new OffsetPage { Offset = (long)int.MaxValue + 1L, Limit = 10 } };
        SearchRequest empty = new SearchRequest { OffsetPage = new OffsetPage { Offset = 0L, Limit = 0 } };
        SearchRequest cursor = new SearchRequest { UseCursorPage = true };

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.WriteFormFields(negative, new UsptoLimits()))!.ErrorCode, Is.EqualTo("INVALID_OFFSET"));
            Assert.That(Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.WriteFormFields(overflow, new UsptoLimits()))!.ErrorCode, Is.EqualTo("INVALID_OFFSET"));
            Assert.That(Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.WriteFormFields(empty, new UsptoLimits()))!.ErrorCode, Is.EqualTo("INVALID_LIMIT"));
            Assert.That(Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.WriteFormFields(cursor, new UsptoLimits()))!.ErrorCode, Is.EqualTo("UNSUPPORTED_PAGE_MODE"));
        });
    }

    [Test]
    public void UnsupportedDsapiSearchOptionsAreRejected()
    {
        SearchRequest sorted = new SearchRequest
        {
            Sorts = new SearchSort[] { new SearchSort { Field = "mail_date" } }
        };
        SearchRequest selected = new SearchRequest
        {
            FieldSelection = new SearchFieldSelection { Fields = new string[] { "app_id" } }
        };
        SearchRequest faceted = new SearchRequest
        {
            Facets = new SearchFacetRequest[] { new SearchFacetRequest { Field = "document_cd" } }
        };

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.WriteCriteria(sorted))!.ErrorCode, Is.EqualTo("UNSUPPORTED_SEARCH_OPTION"));
            Assert.That(Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.WriteCriteria(selected))!.ErrorCode, Is.EqualTo("UNSUPPORTED_SEARCH_OPTION"));
            Assert.That(Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.WriteCriteria(faceted))!.ErrorCode, Is.EqualTo("UNSUPPORTED_SEARCH_OPTION"));
        });
    }

    [TestCase("bad field")]
    [TestCase("bad:field")]
    [TestCase(".bad")]
    [TestCase("bad..field")]
    [TestCase("bad.")]
    public void InvalidFieldNamesAreRejected(string field)
    {
        ValidationException exception = Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.WriteEquality(field, "value"));

        Assert.That(exception.ErrorCode, Is.EqualTo("INVALID_FIELD_NAME"));
    }

    [Test]
    public void InvalidPredicateShapesAndControlCharactersAreRejected()
    {
        SearchFilter incompleteAnd = new SearchFilter
        {
            Operator = SearchOperator.And,
            Children = new SearchFilter[]
            {
                new SearchFilter { Field = "app_id", Operator = SearchOperator.Equal, Value = "14412875" }
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.WritePredicate(incompleteAnd))!.ErrorCode, Is.EqualTo("INVALID_PREDICATE_CHILDREN"));
            Assert.That(Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.EscapeLuceneLiteral("line\nbreak"))!.ErrorCode, Is.EqualTo("INVALID_QUERY_LITERAL"));
        });
    }

    [Test]
    public void PredicateDepthIsBounded()
    {
        SearchFilter predicate = new SearchFilter { Field = "app_id", Operator = SearchOperator.Equal, Value = "14412875" };
        for (int index = 0; index < OfficeActionQueryWriter.MaximumPredicateDepth; index += 1)
        {
            predicate = new SearchFilter { Operator = SearchOperator.Not, Children = new SearchFilter[] { predicate } };
        }

        ValidationException exception = Assert.Throws<ValidationException>(() => OfficeActionQueryWriter.WritePredicate(predicate));

        Assert.That(exception.ErrorCode, Is.EqualTo("PREDICATE_DEPTH_EXCEEDED"));
    }
}
