using uspto;

namespace uspto.Tests;

public class SearchModelsTests
{
    [Test]
    public void SearchRequestUsesBoundedOffsetDefaults()
    {
        SearchRequest request = new SearchRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.Query, Is.Null);
            Assert.That(request.Filters, Is.Empty);
            Assert.That(request.Ranges, Is.Empty);
            Assert.That(request.Sorts, Is.Empty);
            Assert.That(request.FieldSelection.Fields, Is.Empty);
            Assert.That(request.Facets, Is.Empty);
            Assert.That(request.UseCursorPage, Is.False);
            Assert.That(request.OffsetPage.Offset, Is.Zero);
            Assert.That(request.OffsetPage.Limit, Is.EqualTo(UsptoDefaults.PageSize));
            Assert.That(request.CursorPage.Size, Is.EqualTo(UsptoDefaults.PageSize));
            Assert.That(request.MaximumPages, Is.EqualTo(UsptoDefaults.MaximumPages));
            Assert.That(request.MaximumResults, Is.EqualTo(UsptoDefaults.MaximumResults));
        });
    }

    [Test]
    public void MutableSearchDefaultsAreIndependent()
    {
        SearchRequest first = new SearchRequest();
        SearchRequest second = new SearchRequest();

        first.FieldSelection.Fields = new string[] { "applicationNumberText" };
        first.OffsetPage.Offset = 100L;
        first.CursorPage.AfterValues = new string[] { "cursor-a" };
        first.Context.CorrelationIdentifier = "request-a";

        Assert.Multiple(() =>
        {
            Assert.That(first.FieldSelection, Is.Not.SameAs(second.FieldSelection));
            Assert.That(first.OffsetPage, Is.Not.SameAs(second.OffsetPage));
            Assert.That(first.CursorPage, Is.Not.SameAs(second.CursorPage));
            Assert.That(first.Context, Is.Not.SameAs(second.Context));
            Assert.That(second.FieldSelection.Fields, Is.Empty);
            Assert.That(second.OffsetPage.Offset, Is.Zero);
            Assert.That(second.CursorPage.AfterValues, Is.Empty);
            Assert.That(second.Context.CorrelationIdentifier, Is.Null);
        });
    }

    [Test]
    public void FilterTreeRetainsLeafAndLogicalPredicates()
    {
        SearchFilter status = new SearchFilter
        {
            Field = "applicationMetaData.applicationStatusCode",
            Operator = SearchOperator.In,
            Values = new string[] { "150", "161" }
        };
        SearchFilter title = new SearchFilter
        {
            Field = "applicationMetaData.inventionTitle",
            Operator = SearchOperator.Contains,
            Value = "battery"
        };
        SearchFilter root = new SearchFilter
        {
            Operator = SearchOperator.And,
            Children = new SearchFilter[] { status, title }
        };

        Assert.Multiple(() =>
        {
            Assert.That(root.Field, Is.Null);
            Assert.That(root.Operator, Is.EqualTo(SearchOperator.And));
            Assert.That(root.Children, Has.Length.EqualTo(2));
            Assert.That(root.Children[0], Is.SameAs(status));
            Assert.That(root.Children[0].Values, Is.EqualTo(new string[] { "150", "161" }));
            Assert.That(root.Children[1], Is.SameAs(title));
            Assert.That(root.Children[1].Value, Is.EqualTo("battery"));
        });
    }

    [Test]
    public void SearchRequestComposesRangeSortFieldAndFacetData()
    {
        SearchRequest request = new SearchRequest
        {
            Query = "applicationTypeLabelName:Utility",
            Ranges = new SearchRange[]
            {
                new SearchRange
                {
                    Field = "filingDate",
                    LowerValue = "2025-01-01",
                    UpperValue = "2025-12-31",
                    IncludeLower = true,
                    IncludeUpper = false
                }
            },
            Sorts = new SearchSort[]
            {
                new SearchSort
                {
                    Field = "applicationNumberText",
                    Direction = SortDirection.Ascending
                }
            },
            FieldSelection = new SearchFieldSelection
            {
                Fields = new string[] { "applicationNumberText", "filingDate" }
            },
            Facets = new SearchFacetRequest[]
            {
                new SearchFacetRequest
                {
                    Field = "applicationTypeLabelName",
                    Limit = 10,
                    HasLimit = true,
                    Prefix = "U",
                    IncludeMissing = false
                }
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(request.Query, Is.EqualTo("applicationTypeLabelName:Utility"));
            Assert.That(request.Ranges[0].LowerValue, Is.EqualTo("2025-01-01"));
            Assert.That(request.Ranges[0].IncludeUpper, Is.False);
            Assert.That(request.Sorts[0].Direction, Is.EqualTo(SortDirection.Ascending));
            Assert.That(request.FieldSelection.Fields, Has.Length.EqualTo(2));
            Assert.That(request.Facets[0].Limit, Is.EqualTo(10));
            Assert.That(request.Facets[0].HasLimit, Is.True);
            Assert.That(request.Facets[0].Prefix, Is.EqualTo("U"));
        });
    }

    [Test]
    public void CursorPagePreservesStableAfterValues()
    {
        SearchRequest request = new SearchRequest
        {
            UseCursorPage = true,
            CursorPage = new CursorPage
            {
                AfterValues = new string[] { "2025-03-14", "US12345678" },
                Size = 1000
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(request.UseCursorPage, Is.True);
            Assert.That(request.CursorPage.AfterValues, Is.EqualTo(new string[] { "2025-03-14", "US12345678" }));
            Assert.That(request.CursorPage.Size, Is.EqualTo(UsptoDefaults.PatentsViewMaximumPageSize));
        });
    }

    [Test]
    public void ResponseMetadataDistinguishesAbsentTotalsAndCursors()
    {
        SearchResponseMetadata metadata = new SearchResponseMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Total, Is.Zero);
            Assert.That(metadata.HasTotal, Is.False);
            Assert.That(metadata.HasOffsetPage, Is.False);
            Assert.That(metadata.HasNextCursorPage, Is.False);
            Assert.That(metadata.Facets, Is.Empty);
            Assert.That(metadata.Provenance, Is.EqualTo(DataProvenance.Unknown));
            Assert.That(metadata.FromCache, Is.False);
        });

        metadata.Total = 25L;
        metadata.HasTotal = true;
        metadata.ReturnedCount = 10;
        metadata.HasNextCursorPage = true;
        metadata.NextCursorPage.AfterValues = new string[] { "next" };
        metadata.Facets = new SearchFacetResult[]
        {
            new SearchFacetResult
            {
                Field = "status",
                Values = new SearchFacetValue[]
                {
                    new SearchFacetValue
                    {
                        Value = "Patented Case",
                        Count = 7L
                    }
                }
            }
        };
        metadata.Provenance = DataProvenance.Odp;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Total, Is.EqualTo(25L));
            Assert.That(metadata.HasTotal, Is.True);
            Assert.That(metadata.ReturnedCount, Is.EqualTo(10));
            Assert.That(metadata.NextCursorPage.AfterValues, Is.EqualTo(new string[] { "next" }));
            Assert.That(metadata.Facets[0].Field, Is.EqualTo("status"));
            Assert.That(metadata.Facets[0].Values[0].Count, Is.EqualTo(7L));
            Assert.That(metadata.Provenance, Is.EqualTo(DataProvenance.Odp));
        });
    }
}
