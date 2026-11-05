using uspto;

namespace uspto.Tests;

public class PatentsViewLocalPredicateEvaluatorTests
{
    [Test]
    public void EqualityAndSetMembershipCoerceNumericAndStringValues()
    {
        PatentsViewDerivedTableRecord record = CreateRecord(
            new[] { "patent_id", "score", "category" },
            new[] { "AbC-123", "0007.5000", "Utility" });

        bool identifierMatch = PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("Patent_Id", SearchOperator.Equal, "abc-123"));
        bool numericMatch = PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("score", SearchOperator.Equal, "7.5"));
        bool setMatch = PatentsViewLocalPredicateEvaluator.Matches(record, new SearchFilter
        {
            Field = "score",
            Operator = SearchOperator.In,
            Values = new[] { "3", "7.50", "11" }
        });
        bool setMiss = PatentsViewLocalPredicateEvaluator.Matches(record, new SearchFilter
        {
            Field = "category",
            Operator = SearchOperator.In,
            Values = new[] { "design", "plant" }
        });

        Assert.Multiple(() =>
        {
            Assert.That(identifierMatch, Is.True);
            Assert.That(numericMatch, Is.True);
            Assert.That(setMatch, Is.True);
            Assert.That(setMiss, Is.False);
        });
    }

    [Test]
    public void ComparisonsUseNumbersDatesAndDeterministicTextFallback()
    {
        PatentsViewDerivedTableRecord record = CreateRecord(
            new[] { "score", "patent_date", "label", "negative" },
            new[] { "1.20e2", "2024-02-29", "Beta", "-12.5" });

        Assert.Multiple(() =>
        {
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("score", SearchOperator.GreaterThan, "119.99")), Is.True);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("score", SearchOperator.LessThanOrEqual, "120")), Is.True);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("negative", SearchOperator.LessThan, "-2")), Is.True);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("patent_date", SearchOperator.LessThan, "20240301")), Is.True);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("patent_date", SearchOperator.Equal, "2024/02/29")), Is.True);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("label", SearchOperator.GreaterThan, "alpha")), Is.True);
        });
    }

    [Test]
    public void ContainsAndBooleanCompositionShortCircuitToExpectedResult()
    {
        PatentsViewDerivedTableRecord record = CreateRecord(
            new[] { "patent_title", "patent_type", "withdrawn" },
            new[] { "Adaptive Battery Controller", "utility", "false" });
        SearchFilter predicate = new SearchFilter
        {
            Operator = SearchOperator.And,
            Children = new[]
            {
                Leaf("patent_title", SearchOperator.Contains, "BATTERY"),
                new SearchFilter
                {
                    Operator = SearchOperator.Or,
                    Children = new[]
                    {
                        Leaf("patent_type", SearchOperator.Equal, "design"),
                        new SearchFilter
                        {
                            Operator = SearchOperator.Not,
                            Children = new[] { Leaf("withdrawn", SearchOperator.Equal, "true") }
                        }
                    }
                }
            }
        };

        Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, predicate), Is.True);
    }

    [Test]
    public void BooleanCompositionReturnsFalseForFailedAndOrAndNotBranches()
    {
        PatentsViewDerivedTableRecord record = CreateRecord(
            new[] { "patent_type", "withdrawn" },
            new[] { "utility", "true" });
        SearchFilter andPredicate = new SearchFilter
        {
            Operator = SearchOperator.And,
            Children = new[]
            {
                Leaf("patent_type", SearchOperator.Equal, "utility"),
                Leaf("withdrawn", SearchOperator.Equal, "false")
            }
        };
        SearchFilter orPredicate = new SearchFilter
        {
            Operator = SearchOperator.Or,
            Children = new[]
            {
                Leaf("patent_type", SearchOperator.Equal, "design"),
                Leaf("patent_type", SearchOperator.Equal, "plant")
            }
        };
        SearchFilter notPredicate = new SearchFilter
        {
            Operator = SearchOperator.Not,
            Children = new[] { Leaf("withdrawn", SearchOperator.Equal, "true") }
        };

        Assert.Multiple(() =>
        {
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, andPredicate), Is.False);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, orPredicate), Is.False);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, notPredicate), Is.False);
        });
    }

    [Test]
    public void MissingAndBlankFieldsHaveStablePredicateSemantics()
    {
        PatentsViewDerivedTableRecord record = CreateRecord(
            new[] { "patent_id", "optional_value" },
            new[] { "123", "   " });

        Assert.Multiple(() =>
        {
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("unknown", SearchOperator.Equal, "x")), Is.False);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("unknown", SearchOperator.NotEqual, "x")), Is.True);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("optional_value", SearchOperator.Contains, "x")), Is.False);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, Leaf("optional_value", SearchOperator.GreaterThan, "0")), Is.False);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, new SearchFilter
            {
                Field = "optional_value",
                Operator = SearchOperator.In,
                Values = new[] { "x", "y" }
            }), Is.False);
            Assert.That(PatentsViewLocalPredicateEvaluator.Matches(record, new SearchFilter
            {
                Operator = SearchOperator.Not,
                Children = new[] { Leaf("unknown", SearchOperator.Equal, "x") }
            }), Is.True);
        });
    }

    [Test]
    public void RangeEvaluationUsesTheSameCoercionAndBoundaryRules()
    {
        PatentsViewDerivedTableRecord record = CreateRecord(
            new[] { "patent_date", "score" },
            new[] { "2025-01-15", "10.00" });

        bool dateMatch = PatentsViewLocalPredicateEvaluator.MatchesRange(record, new SearchRange
        {
            Field = "patent_date",
            LowerValue = "20250115",
            UpperValue = "2025-02-01",
            IncludeLower = true,
            IncludeUpper = false
        });
        bool excludedBoundary = PatentsViewLocalPredicateEvaluator.MatchesRange(record, new SearchRange
        {
            Field = "score",
            LowerValue = "10",
            IncludeLower = false
        });
        bool missing = PatentsViewLocalPredicateEvaluator.MatchesRange(record, new SearchRange
        {
            Field = "unknown",
            LowerValue = "1"
        });

        Assert.Multiple(() =>
        {
            Assert.That(dateMatch, Is.True);
            Assert.That(excludedBoundary, Is.False);
            Assert.That(missing, Is.False);
        });
    }

    [Test]
    public void InvalidPredicateShapesAreRejectedBeforeEvaluation()
    {
        PatentsViewDerivedTableRecord record = CreateRecord(new[] { "patent_id" }, new[] { "123" });
        SearchFilter missingValue = new SearchFilter { Field = "patent_id", Operator = SearchOperator.Equal };
        SearchFilter shortAnd = new SearchFilter
        {
            Operator = SearchOperator.And,
            Children = new[] { Leaf("patent_id", SearchOperator.Equal, "123") }
        };

        ValidationException valueFailure = Assert.Throws<ValidationException>(() => PatentsViewLocalPredicateEvaluator.Matches(record, missingValue))!;
        ValidationException childFailure = Assert.Throws<ValidationException>(() => PatentsViewLocalPredicateEvaluator.Matches(record, shortAnd))!;

        Assert.Multiple(() =>
        {
            Assert.That(valueFailure.ErrorCode, Is.EqualTo("INVALID_PREDICATE_SHAPE"));
            Assert.That(childFailure.ErrorCode, Is.EqualTo("INVALID_PREDICATE_CHILDREN"));
        });
    }

    private static SearchFilter Leaf(string field, SearchOperator searchOperator, string value)
    {
        return new SearchFilter { Field = field, Operator = searchOperator, Value = value };
    }

    private static PatentsViewDerivedTableRecord CreateRecord(string[] headers, string[] values)
    {
        DelimitedRow row = new DelimitedRow
        {
            Headers = headers,
            Values = values,
            ExpectedColumnCount = headers.Length,
            RowNumber = 1,
            StartLineNumber = 2,
            EndLineNumber = 2
        };
        PatentDataProvenance provenance = new PatentDataProvenance
        {
            Source = DataProvenance.Bulk,
            IsAuthoritative = true
        };
        return PatentsViewDerivedTableParser.ParseRow(row, "local_table.tsv", DatasetFamily.PatentsViewAnnualized, provenance);
    }
}
