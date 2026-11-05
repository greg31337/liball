using uspto;

namespace uspto.Tests;

public class OdpQueryWriterTests
{
    [Test]
    public void SimplifiedQueryLiteralEscapesReservedCharactersAndWhitespace()
    {
        string result = OdpQueryWriter.EscapeSimplifiedQueryLiteral("alpha +-=&&||><!(){}[]^\"~*?:\\/ beta");

        Assert.That(result, Is.EqualTo("""alpha\ \+\-\=\&\&\|\|\>\<\!\(\)\{\}\[\]\^\"\~\*\?\:\\\/\ beta"""));
    }

    [Test]
    public void EqualityUsesAnExactQuotedLiteralAndEscapesQuoteBoundaries()
    {
        string result = OdpQueryWriter.WriteEquality("applicationMetaData.inventionTitle", "battery \"pack\"");

        Assert.That(result, Is.EqualTo("applicationMetaData.inventionTitle:\"battery \\\"pack\\\"\""));
    }

    [TestCase(SearchOperator.LessThan, "<")]
    [TestCase(SearchOperator.LessThanOrEqual, "<=")]
    [TestCase(SearchOperator.GreaterThan, ">")]
    [TestCase(SearchOperator.GreaterThanOrEqual, ">=")]
    public void ComparisonsUseOdpComparisonTokens(SearchOperator searchOperator, string token)
    {
        string result = OdpQueryWriter.WriteComparison("applicationMetaData.applicationStatusCode", searchOperator, "600");

        Assert.That(result, Is.EqualTo("applicationMetaData.applicationStatusCode:" + token + "600"));
    }

    [Test]
    public void TextAndContainsEscapeCallerWildcardsInsteadOfActivatingThem()
    {
        SearchFilter filter = new SearchFilter
        {
            Field = "applicationMetaData.inventionTitle",
            Operator = SearchOperator.Contains,
            Value = "battery* pack?"
        };

        string text = OdpQueryWriter.WriteText("utility OR design");
        string contains = OdpQueryWriter.WritePredicate(filter);

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("utility\\ OR\\ design"));
            Assert.That(contains, Is.EqualTo("applicationMetaData.inventionTitle:*battery\\*\\ pack\\?*"));
        });
    }

    [Test]
    public void PredicateTreesSerializeWithExplicitGrouping()
    {
        SearchFilter root = new SearchFilter
        {
            Operator = SearchOperator.And,
            Children = new SearchFilter[]
            {
                new SearchFilter
                {
                    Field = "applicationMetaData.applicationTypeLabelName",
                    Operator = SearchOperator.Equal,
                    Value = "Utility"
                },
                new SearchFilter
                {
                    Operator = SearchOperator.Or,
                    Children = new SearchFilter[]
                    {
                        new SearchFilter
                        {
                            Field = "applicationMetaData.applicationStatusCode",
                            Operator = SearchOperator.GreaterThan,
                            Value = "600"
                        },
                        new SearchFilter
                        {
                            Field = "applicationMetaData.inventionTitle",
                            Operator = SearchOperator.Contains,
                            Value = "battery"
                        }
                    }
                },
                new SearchFilter
                {
                    Operator = SearchOperator.Not,
                    Children = new SearchFilter[]
                    {
                        new SearchFilter
                        {
                            Field = "applicationMetaData.applicationTypeCode",
                            Operator = SearchOperator.Equal,
                            Value = "DES"
                        }
                    }
                }
            }
        };

        string result = OdpQueryWriter.WritePredicate(root);

        Assert.That(result, Is.EqualTo("(applicationMetaData.applicationTypeLabelName:\"Utility\" AND (applicationMetaData.applicationStatusCode:>600 OR applicationMetaData.inventionTitle:*battery*) AND (NOT applicationMetaData.applicationTypeCode:\"DES\"))"));
    }

    [Test]
    public void SetMembershipRepeatsTheExactFieldForEachValue()
    {
        SearchFilter filter = new SearchFilter
        {
            Field = "applicationMetaData.applicationTypeCode",
            Operator = SearchOperator.In,
            Values = new string[] { "UTL", "DES", "PLT" }
        };

        string result = OdpQueryWriter.WritePredicate(filter);

        Assert.That(result, Is.EqualTo("(applicationMetaData.applicationTypeCode:\"UTL\" OR applicationMetaData.applicationTypeCode:\"DES\" OR applicationMetaData.applicationTypeCode:\"PLT\")"));
    }

    [Test]
    public void PredicateRootsAreCombinedInCallerOrder()
    {
        SearchFilter[] filters = new SearchFilter[]
        {
            new SearchFilter { Field = "first", Operator = SearchOperator.Equal, Value = "one" },
            new SearchFilter { Field = "second", Operator = SearchOperator.NotEqual, Value = "two" }
        };

        string result = OdpQueryWriter.WritePredicates(filters);

        Assert.That(result, Is.EqualTo("first:\"one\" AND (NOT second:\"two\")"));
    }

    [Test]
    public void ExactFiltersSupportEqualityAndSetMembership()
    {
        SearchFilter equality = new SearchFilter
        {
            Field = "applicationMetaData.filingDate",
            Operator = SearchOperator.Equal,
            Value = "2023-10-18"
        };
        SearchFilter membership = new SearchFilter
        {
            Field = "applicationMetaData.applicationTypeCode",
            Operator = SearchOperator.In,
            Values = new string[] { "UTL", "DES" }
        };

        string equalityResult = OdpQueryWriter.WriteExactFilter(equality);
        string membershipResult = OdpQueryWriter.WriteExactFilter(membership);

        Assert.Multiple(() =>
        {
            Assert.That(equalityResult, Is.EqualTo("applicationMetaData.filingDate 2023-10-18"));
            Assert.That(membershipResult, Is.EqualTo("applicationMetaData.applicationTypeCode UTL,DES"));
        });
    }

    [Test]
    public void QueryRangesPreserveOpenAndExclusiveBounds()
    {
        SearchRange bounded = new SearchRange
        {
            Field = "applicationMetaData.filingDate",
            LowerValue = "2025-01-01",
            UpperValue = "2025-12-31",
            IncludeLower = true,
            IncludeUpper = false
        };
        SearchRange open = new SearchRange
        {
            Field = "applicationMetaData.applicationStatusCode",
            LowerValue = "150",
            UpperValue = null,
            IncludeLower = false,
            IncludeUpper = true
        };

        string boundedResult = OdpQueryWriter.WriteRangeQuery(bounded);
        string openResult = OdpQueryWriter.WriteRangeQuery(open);

        Assert.Multiple(() =>
        {
            Assert.That(boundedResult, Is.EqualTo("applicationMetaData.filingDate:[2025\\-01\\-01 TO 2025\\-12\\-31}"));
            Assert.That(openResult, Is.EqualTo("applicationMetaData.applicationStatusCode:{150 TO *]"));
        });
    }

    [TestCase("10", "20", true, true, "score:[10 TO 20]")]
    [TestCase("10", "20", true, false, "score:[10 TO 20}")]
    [TestCase("10", "20", false, true, "score:{10 TO 20]")]
    [TestCase("10", "20", false, false, "score:{10 TO 20}")]
    [TestCase("10", null, true, true, "score:[10 TO *]")]
    [TestCase("10", null, false, true, "score:{10 TO *]")]
    [TestCase(null, "20", true, true, "score:[* TO 20]")]
    [TestCase(null, "20", true, false, "score:[* TO 20}")]
    public void EveryQueryRangeBoundFormHasStableDelimiters(string? lower, string? upper, bool includeLower, bool includeUpper, string expected)
    {
        SearchRange range = new SearchRange
        {
            Field = "score",
            LowerValue = lower,
            UpperValue = upper,
            IncludeLower = includeLower,
            IncludeUpper = includeUpper
        };

        string result = OdpQueryWriter.WriteRangeQuery(range);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void RangeFiltersUseTheDocumentedFieldAndColonForm()
    {
        SearchRange range = new SearchRange
        {
            Field = "applicationMetaData.grantDate",
            LowerValue = "2010-01-01",
            UpperValue = "2011-01-01"
        };

        string result = OdpQueryWriter.WriteRangeFilter(range);

        Assert.That(result, Is.EqualTo("applicationMetaData.grantDate 2010-01-01:2011-01-01"));
    }

    [Test]
    public void SelectedFieldsFacetsAndSortsRetainCallerOrder()
    {
        SearchFieldSelection selection = new SearchFieldSelection
        {
            Fields = new string[] { "applicationNumberText", "applicationMetaData.patentNumber" }
        };
        SearchFacetRequest[] facets = new SearchFacetRequest[]
        {
            new SearchFacetRequest { Field = "applicationMetaData.applicationTypeCode" },
            new SearchFacetRequest { Field = "applicationMetaData.applicationStatusCode" }
        };
        SearchSort[] sorts = new SearchSort[]
        {
            new SearchSort { Field = "applicationMetaData.filingDate", Direction = SortDirection.Descending },
            new SearchSort { Field = "applicationNumberText", Direction = SortDirection.Ascending }
        };

        string fieldsResult = OdpQueryWriter.WriteSelectedFields(selection);
        string facetsResult = OdpQueryWriter.WriteFacets(facets);
        string sortsResult = OdpQueryWriter.WriteSorts(sorts);

        Assert.Multiple(() =>
        {
            Assert.That(fieldsResult, Is.EqualTo("applicationNumberText,applicationMetaData.patentNumber"));
            Assert.That(facetsResult, Is.EqualTo("applicationMetaData.applicationTypeCode,applicationMetaData.applicationStatusCode"));
            Assert.That(sortsResult, Is.EqualTo("applicationMetaData.filingDate desc,applicationNumberText asc"));
        });
    }

    [TestCase("bad field")]
    [TestCase("bad:field")]
    [TestCase(".bad")]
    [TestCase("bad..field")]
    [TestCase("bad.")]
    public void InvalidFieldNamesAreRejected(string field)
    {
        ValidationException? exception = Assert.Throws<ValidationException>(() => OdpQueryWriter.WriteEquality(field, "value"));

        Assert.That(exception!.ErrorCode, Is.EqualTo("INVALID_FIELD_NAME"));
    }

    [Test]
    public void InvalidPredicateShapesAreRejectedDeterministically()
    {
        SearchFilter missingValue = new SearchFilter { Field = "field", Operator = SearchOperator.Equal };
        SearchFilter shortAnd = new SearchFilter
        {
            Operator = SearchOperator.And,
            Children = new SearchFilter[] { new SearchFilter { Field = "field", Operator = SearchOperator.Equal, Value = "value" } }
        };
        SearchFilter unsupportedExactFilter = new SearchFilter { Field = "field", Operator = SearchOperator.Contains, Value = "value" };

        ValidationException? valueException = Assert.Throws<ValidationException>(() => OdpQueryWriter.WritePredicate(missingValue));
        ValidationException? childrenException = Assert.Throws<ValidationException>(() => OdpQueryWriter.WritePredicate(shortAnd));
        ValidationException? filterException = Assert.Throws<ValidationException>(() => OdpQueryWriter.WriteExactFilter(unsupportedExactFilter));

        Assert.Multiple(() =>
        {
            Assert.That(valueException!.ErrorCode, Is.EqualTo("INVALID_PREDICATE_SHAPE"));
            Assert.That(childrenException!.ErrorCode, Is.EqualTo("INVALID_PREDICATE_CHILDREN"));
            Assert.That(filterException!.ErrorCode, Is.EqualTo("UNSUPPORTED_FILTER_OPERATOR"));
        });
    }

    [Test]
    public void UnrepresentableFilterRangeFacetAndDuplicateFieldsAreRejected()
    {
        SearchFilter commaFilter = new SearchFilter { Field = "field", Operator = SearchOperator.Equal, Value = "one,two" };
        SearchRange exclusiveRange = new SearchRange { Field = "date", LowerValue = "2025-01-01", UpperValue = "2025-12-31", IncludeUpper = false };
        SearchFacetRequest[] limitedFacet = new SearchFacetRequest[] { new SearchFacetRequest { Field = "field", HasLimit = true, Limit = 5 } };
        SearchFacetRequest[] prefixedFacet = new SearchFacetRequest[] { new SearchFacetRequest { Field = "field", Prefix = "A" } };
        SearchFacetRequest[] missingFacet = new SearchFacetRequest[] { new SearchFacetRequest { Field = "field", IncludeMissing = true } };
        SearchFieldSelection duplicateFields = new SearchFieldSelection { Fields = new string[] { "field", "FIELD" } };

        ValidationException? filterException = Assert.Throws<ValidationException>(() => OdpQueryWriter.WriteExactFilter(commaFilter));
        ValidationException? rangeException = Assert.Throws<ValidationException>(() => OdpQueryWriter.WriteRangeFilter(exclusiveRange));
        ValidationException? limitException = Assert.Throws<ValidationException>(() => OdpQueryWriter.WriteFacets(limitedFacet));
        ValidationException? prefixException = Assert.Throws<ValidationException>(() => OdpQueryWriter.WriteFacets(prefixedFacet));
        ValidationException? missingException = Assert.Throws<ValidationException>(() => OdpQueryWriter.WriteFacets(missingFacet));
        ValidationException? fieldException = Assert.Throws<ValidationException>(() => OdpQueryWriter.WriteSelectedFields(duplicateFields));

        Assert.Multiple(() =>
        {
            Assert.That(filterException!.ErrorCode, Is.EqualTo("INVALID_FILTER_VALUE"));
            Assert.That(rangeException!.ErrorCode, Is.EqualTo("UNSUPPORTED_RANGE_INCLUSIVITY"));
            Assert.That(limitException!.ErrorCode, Is.EqualTo("UNSUPPORTED_FACET_OPTION"));
            Assert.That(prefixException!.ErrorCode, Is.EqualTo("UNSUPPORTED_FACET_OPTION"));
            Assert.That(missingException!.ErrorCode, Is.EqualTo("UNSUPPORTED_FACET_OPTION"));
            Assert.That(fieldException!.ErrorCode, Is.EqualTo("DUPLICATE_FIELD"));
        });
    }

    [Test]
    public void ControlCharactersAndExcessivePredicateDepthAreRejected()
    {
        ValidationException? controlException = Assert.Throws<ValidationException>(() => OdpQueryWriter.WriteText("line\nbreak"));
        SearchFilter nested = new SearchFilter { Field = "field", Operator = SearchOperator.Equal, Value = "value" };
        int index;
        for (index = 0; index < OdpQueryWriter.MaximumPredicateDepth; index += 1)
        {
            nested = new SearchFilter { Operator = SearchOperator.Not, Children = new SearchFilter[] { nested } };
        }

        ValidationException? depthException = Assert.Throws<ValidationException>(() => OdpQueryWriter.WritePredicate(nested));

        Assert.Multiple(() =>
        {
            Assert.That(controlException!.ErrorCode, Is.EqualTo("INVALID_QUERY_LITERAL"));
            Assert.That(depthException!.ErrorCode, Is.EqualTo("PREDICATE_DEPTH_EXCEEDED"));
        });
    }

    [Test]
    public void EmptyCollectionsSerializeAsEmptyParameterValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdpQueryWriter.WritePredicates(Array.Empty<SearchFilter>()), Is.Empty);
            Assert.That(OdpQueryWriter.WriteSelectedFields(new SearchFieldSelection()), Is.Empty);
            Assert.That(OdpQueryWriter.WriteFacets(Array.Empty<SearchFacetRequest>()), Is.Empty);
            Assert.That(OdpQueryWriter.WriteSorts(Array.Empty<SearchSort>()), Is.Empty);
        });
    }
}
