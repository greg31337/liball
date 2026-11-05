using uspto;

namespace uspto.Tests;

public class BulkCatalogModelsTests
{
    [Test]
    public void DefaultsAreSafeAndIndependentAcrossCatalogModels()
    {
        BulkProduct firstProduct = new BulkProduct();
        BulkProduct secondProduct = new BulkProduct();
        BulkProductFile firstFile = new BulkProductFile();
        BulkProductFile secondFile = new BulkProductFile();
        BulkProductResponse firstResponse = new BulkProductResponse();
        BulkProductResponse secondResponse = new BulkProductResponse();
        BulkDatasetPreset firstPreset = new BulkDatasetPreset();
        BulkDatasetPreset secondPreset = new BulkDatasetPreset();

        firstProduct.DataDateRange.FromDateText = "2001-01-01";
        firstProduct.Files = new[] { firstFile };
        firstProduct.UnknownFields["futureProduct"] = "true";
        firstProduct.Provenance.DatasetIdentifier = "PTFWPRE";
        firstFile.DataDateRange.ToDateText = "2010-12-31";
        firstFile.Checksums = new[] { new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha256 } };
        firstFile.UnknownFields["futureFile"] = "true";
        firstResponse.Request.Query = "PATENT";
        firstResponse.Page.Offset = 25L;
        firstResponse.Products = new[] { firstProduct };
        firstResponse.Facets = new[] { new BulkCatalogFacet { Field = "productLabelArrayText" } };
        firstResponse.UnknownFields["futureResponse"] = "true";
        firstPreset.ProductIdentifiers = new[] { "PTFWPRE" };
        firstPreset.Products = new[] { firstProduct };
        firstPreset.Provenance.DatasetIdentifier = "preset";

        Assert.Multiple(() =>
        {
            Assert.That(secondProduct.ProductIdentifier, Is.Null);
            Assert.That(secondProduct.DaysOfWeek, Is.Empty);
            Assert.That(secondProduct.Labels, Is.Empty);
            Assert.That(secondProduct.DatasetTypes, Is.Empty);
            Assert.That(secondProduct.Categories, Is.Empty);
            Assert.That(secondProduct.Files, Is.Empty);
            Assert.That(secondProduct.HasTotalFileSize, Is.False);
            Assert.That(secondProduct.HasFileCount, Is.False);
            Assert.That(secondProduct.HasReturnedFileCount, Is.False);
            Assert.That(secondProduct.HasLastModified, Is.False);
            Assert.That(secondProduct.RawResponse, Is.Null);
            Assert.That(secondProduct.DataDateRange, Is.Not.SameAs(firstProduct.DataDateRange));
            Assert.That(secondProduct.Provenance, Is.Not.SameAs(firstProduct.Provenance));
            Assert.That(secondProduct.UnknownFields, Is.Not.SameAs(firstProduct.UnknownFields));
            Assert.That(secondProduct.UnknownFields, Is.Empty);
            Assert.That(secondFile.Checksums, Is.Empty);
            Assert.That(secondFile.Format, Is.EqualTo(DocumentFormat.Unknown));
            Assert.That(secondFile.Compression, Is.EqualTo(CompressionFormat.Unknown));
            Assert.That(secondFile.HasSize, Is.False);
            Assert.That(secondFile.HasReleaseDate, Is.False);
            Assert.That(secondFile.HasFileDate, Is.False);
            Assert.That(secondFile.HasLastModified, Is.False);
            Assert.That(secondFile.CatalogUri, Is.Null);
            Assert.That(secondFile.DownloadUri, Is.Null);
            Assert.That(secondFile.HasResponseStatusCode, Is.False);
            Assert.That(secondFile.RawResponse, Is.Null);
            Assert.That(secondFile.DataDateRange, Is.Not.SameAs(firstFile.DataDateRange));
            Assert.That(secondFile.Provenance, Is.Not.SameAs(firstFile.Provenance));
            Assert.That(secondFile.UnknownFields, Is.Not.SameAs(firstFile.UnknownFields));
            Assert.That(secondResponse.Products, Is.Empty);
            Assert.That(secondResponse.Facets, Is.Empty);
            Assert.That(secondResponse.HasCount, Is.False);
            Assert.That(secondResponse.ReturnedCount, Is.Zero);
            Assert.That(secondResponse.IsComplete, Is.False);
            Assert.That(secondResponse.Request, Is.Not.SameAs(firstResponse.Request));
            Assert.That(secondResponse.Page, Is.Not.SameAs(firstResponse.Page));
            Assert.That(secondResponse.Provenance, Is.Not.SameAs(firstResponse.Provenance));
            Assert.That(secondResponse.UnknownFields, Is.Not.SameAs(firstResponse.UnknownFields));
            Assert.That(secondPreset.Family, Is.EqualTo(DatasetFamily.Unknown));
            Assert.That(secondPreset.ProductIdentifiers, Is.Empty);
            Assert.That(secondPreset.Products, Is.Empty);
            Assert.That(secondPreset.IsAvailable, Is.False);
            Assert.That(secondPreset.Provenance, Is.Not.SameAs(firstPreset.Provenance));
        });
    }

    [Test]
    public void ProductAndFileRetainLargeCountsSizesDatesAndTransferMetadata()
    {
        DateTimeOffset productFrom = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset productTo = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset productModified = new DateTimeOffset(2025, 1, 13, 8, 1, 0, TimeSpan.Zero);
        DateTimeOffset fileFrom = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset fileTo = new DateTimeOffset(2010, 12, 31, 0, 0, 0, TimeSpan.Zero);
        BulkProductFile file = new BulkProductFile
        {
            ProductIdentifier = "PTFWPRE",
            FileName = "2001-2010-patent-filewrapper-full-json.zip",
            Size = 4_500_000_000L,
            HasSize = true,
            DataDateRange = new BulkDateRange
            {
                FromDate = fileFrom,
                HasFromDate = true,
                FromDateText = "2001-01-01",
                ToDate = fileTo,
                HasToDate = true,
                ToDateText = "2010-12-31"
            },
            FileType = "Data",
            MimeTypeIdentifier = "JSON",
            MediaType = "application/zip",
            Format = DocumentFormat.Zip,
            Compression = CompressionFormat.Zip,
            DownloadUri = new Uri("https://api.uspto.gov/api/v1/datasets/products/files/PTFWPRE/2001-2010-patent-filewrapper-full-json.zip"),
            ReleaseDate = productModified,
            HasReleaseDate = true,
            ReleaseDateText = "2025-01-13 08:01:00",
            FileDate = fileFrom,
            HasFileDate = true,
            FileDateText = "2001-01-01T00:00:00.000Z",
            LastModified = productModified,
            HasLastModified = true,
            LastModifiedText = "2025-01-13 08:01:00",
            Checksums = new[]
            {
                new ChecksumExpectation
                {
                    Algorithm = ChecksumAlgorithm.Sha256,
                    Value = "0123456789abcdef"
                }
            },
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.Odp,
                DatasetIdentifier = "PTFWPRE",
                IsAuthoritative = true
            }
        };
        BulkProduct product = new BulkProduct
        {
            ProductIdentifier = "PTFWPRE",
            Title = "Patent File Wrapper (Bulk Datasets) - Weekly",
            Description = "Bibliographic and assignment data.",
            Frequency = "WEEKLY",
            DaysOfWeekText = "SUNDAY",
            DaysOfWeek = new[] { "SUNDAY" },
            Labels = new[] { "RESEARCH", "PATENT" },
            DatasetTypes = new[] { "Research" },
            Categories = new[] { "Patent file wrapper" },
            DataDateRange = new BulkDateRange
            {
                FromDate = productFrom,
                HasFromDate = true,
                FromDateText = "2001-01-01",
                ToDate = productTo,
                HasToDate = true,
                ToDateText = "2025-12-31"
            },
            TotalFileSize = 32_511_973_080L,
            HasTotalFileSize = true,
            FileCount = 3_000_000_000L,
            HasFileCount = true,
            ReturnedFileCount = 1L,
            HasReturnedFileCount = true,
            LastModified = productModified,
            HasLastModified = true,
            LastModifiedText = "2025-01-13T08:01:00Z",
            MimeTypeIdentifiers = new[] { "JSON" },
            Files = new[] { file },
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.Odp,
                DatasetIdentifier = "PTFWPRE",
                RequestIdentifier = "catalog-request",
                IsAuthoritative = true
            }
        };
        product.UnknownFields["futureProductField"] = "\"value\"";
        file.UnknownFields["futureFileField"] = "42";

        Assert.Multiple(() =>
        {
            Assert.That(product.ProductIdentifier, Is.EqualTo("PTFWPRE"));
            Assert.That(product.Title, Does.Contain("Patent File Wrapper"));
            Assert.That(product.DaysOfWeek, Is.EqualTo(new[] { "SUNDAY" }));
            Assert.That(product.Labels, Is.EqualTo(new[] { "RESEARCH", "PATENT" }));
            Assert.That(product.DataDateRange.FromDate, Is.EqualTo(productFrom));
            Assert.That(product.DataDateRange.HasFromDate, Is.True);
            Assert.That(product.DataDateRange.ToDateText, Is.EqualTo("2025-12-31"));
            Assert.That(product.TotalFileSize, Is.EqualTo(32_511_973_080L));
            Assert.That(product.TotalFileSize, Is.GreaterThan(int.MaxValue));
            Assert.That(product.FileCount, Is.EqualTo(3_000_000_000L));
            Assert.That(product.FileCount, Is.GreaterThan(int.MaxValue));
            Assert.That(product.ReturnedFileCount, Is.EqualTo(1L));
            Assert.That(product.LastModified, Is.EqualTo(productModified));
            Assert.That(product.Files.Single(), Is.SameAs(file));
            Assert.That(product.Provenance.RequestIdentifier, Is.EqualTo("catalog-request"));
            Assert.That(product.UnknownFields["futureProductField"], Is.EqualTo("\"value\""));
            Assert.That(file.Size, Is.EqualTo(4_500_000_000L));
            Assert.That(file.Size, Is.GreaterThan(int.MaxValue));
            Assert.That(file.DataDateRange.ToDate, Is.EqualTo(fileTo));
            Assert.That(file.Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(file.Compression, Is.EqualTo(CompressionFormat.Zip));
            Assert.That(file.DownloadUri?.Host, Is.EqualTo("api.uspto.gov"));
            Assert.That(file.Checksums.Single().Algorithm, Is.EqualTo(ChecksumAlgorithm.Sha256));
            Assert.That(file.UnknownFields["futureFileField"], Is.EqualTo("42"));
        });
    }

    [Test]
    public void ResponseComposesProductsFacetsPagingRawDataAndLargeCounts()
    {
        SearchRequest request = new SearchRequest
        {
            Query = "productLabelArrayText:PATENT",
            OffsetPage = new OffsetPage { Offset = 25L, Limit = 25 }
        };
        BulkProduct product = new BulkProduct { ProductIdentifier = "PTFWPRE" };
        BulkProductResponse response = new BulkProductResponse
        {
            Request = request,
            Products = new[] { product },
            Facets = new[]
            {
                new BulkCatalogFacet
                {
                    Field = "productLabelArrayText",
                    Values = new[]
                    {
                        new BulkCatalogFacetValue
                        {
                            Value = "PATENT",
                            Count = 3_500_000_000L
                        }
                    }
                }
            },
            Count = 5_000_000_000L,
            HasCount = true,
            ReturnedCount = 1,
            Page = new OffsetPage { Offset = 25L, Limit = 25 },
            RequestIdentifier = "bulk-search-request",
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.Odp,
                RequestIdentifier = "bulk-search-request",
                IsAuthoritative = true
            },
            RawResponse = "{\"count\":5000000000}",
            IsComplete = true
        };
        response.UnknownFields["futureResponseField"] = "[]";

        Assert.Multiple(() =>
        {
            Assert.That(response.Request, Is.SameAs(request));
            Assert.That(response.Products.Single(), Is.SameAs(product));
            Assert.That(response.Facets.Single().Field, Is.EqualTo("productLabelArrayText"));
            Assert.That(response.Facets.Single().Values.Single().Value, Is.EqualTo("PATENT"));
            Assert.That(response.Facets.Single().Values.Single().Count, Is.EqualTo(3_500_000_000L));
            Assert.That(response.Count, Is.EqualTo(5_000_000_000L));
            Assert.That(response.HasCount, Is.True);
            Assert.That(response.ReturnedCount, Is.EqualTo(1));
            Assert.That(response.Page.Offset, Is.EqualTo(25L));
            Assert.That(response.RequestIdentifier, Is.EqualTo("bulk-search-request"));
            Assert.That(response.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(response.RawResponse, Does.Contain("5000000000"));
            Assert.That(response.IsComplete, Is.True);
            Assert.That(response.UnknownFields["futureResponseField"], Is.EqualTo("[]"));
        });
    }

    [Test]
    public void PresetGroupsCurrentProductsByLogicalDatasetFamily()
    {
        BulkProduct complete = new BulkProduct { ProductIdentifier = "PTFWPRE" };
        BulkDatasetPreset preset = new BulkDatasetPreset
        {
            Family = DatasetFamily.PatentFileWrapperComplete,
            ProductIdentifiers = new[] { "PTFWPRE" },
            Products = new[] { complete },
            IsAvailable = true,
            Provenance = new PatentDataProvenance
            {
                Source = DataProvenance.ClientDerived,
                DatasetIdentifier = "bulk dataset preset",
                IsAuthoritative = false
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(preset.Family, Is.EqualTo(DatasetFamily.PatentFileWrapperComplete));
            Assert.That(preset.ProductIdentifiers, Is.EqualTo(new[] { "PTFWPRE" }));
            Assert.That(preset.Products.Single(), Is.SameAs(complete));
            Assert.That(preset.IsAvailable, Is.True);
            Assert.That(preset.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(preset.Provenance.IsAuthoritative, Is.False);
        });
    }
}
