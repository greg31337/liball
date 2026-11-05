using iso20022;

namespace iso20022.Tests;

public class ArtifactCatalogueExternalCodeDataTests
{
    [Test]
    public void ArtifactStoreContractCarriesExplicitRootCoordinateManifestAndContent()
    {
        ArtifactManifest manifest = CreateManifest("2026-Q1", "external-code-sets");
        ArtifactStoreRequest request = new ArtifactStoreRequest
        {
            RootPath = "/caller/cache",
            Coordinate = manifest.Coordinate,
            Artifact = manifest,
            AllowIdenticalArtifactReuse = true,
            VerifyHash = true,
            MaxArtifactBytes = 4096
        };
        IArtifactStore store = new CapturingArtifactStore();

        ArtifactStoreResult stored = store.StoreArtifactStream(
            new MemoryStream(new byte[] { 0x49, 0x53, 0x4f }), request);
        ArtifactStoreResult opened = store.OpenArtifact(request);

        Assert.Multiple(() =>
        {
            Assert.That(stored.Success, Is.True);
            Assert.That(stored.ReusedExistingArtifact, Is.True);
            Assert.That(stored.Artifact, Is.SameAs(manifest));
            Assert.That(stored.StoragePath, Does.StartWith("/caller/cache/"));
            Assert.That(stored.ByteCount, Is.EqualTo(3));
            Assert.That(opened.Found, Is.True);
            Assert.That(opened.Content, Is.Not.Null);
            Assert.That(opened.Artifact.Coordinate.Edition, Is.EqualTo("2026-Q1"));
        });
    }

    [Test]
    public void CatalogueDownloadContractRetainsConditionalPolicyRedirectsAndProvenance()
    {
        CatalogueDownloadRequest request = new CatalogueDownloadRequest
        {
            Uri = "https://example.test/catalogue.zip",
            Coordinate = new ArtifactCoordinate
            {
                Kind = ArtifactKind.ExternalCodeSet,
                Edition = "2026-Q1",
                Identifier = "external-code-sets",
                Version = "1"
            },
            IfNoneMatch = "\"edition-2026-q1\"",
            IfModifiedSince = new IsoDateTimeValue { LexicalValue = "2026-05-27T13:28:00Z" },
            ExpectedHash = new IsoBinaryValue { Value = new byte[] { 0x20, 0x26 } },
            ExpectedHashAlgorithm = "SHA-256",
            MaxRedirectCount = 3,
            MaxArtifactBytes = 8192,
            IsApproved = true
        };
        request.AcceptedMediaTypes.Add(Iso20022MediaTypes.Zip);
        ICatalogueClient client = new CapturingCatalogueClient();

        CatalogueDownloadResult result = client.DownloadCatalogue(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.FinalUri, Is.EqualTo("https://cdn.example.test/catalogue.zip"));
            Assert.That(result.RedirectUris, Is.EqualTo(new[] { "https://cdn.example.test/catalogue.zip" }));
            Assert.That(result.EntityTag, Is.EqualTo("\"edition-2026-q1\""));
            Assert.That(result.Artifact.Coordinate, Is.SameAs(request.Coordinate));
            Assert.That(result.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.Zip));
            Assert.That(result.Artifact.IsApproved, Is.True);
            Assert.That(result.Content, Is.Not.Null);
        });
    }

    [Test]
    public void ExternalCatalogRetainsOfficialLifecycleMappingAndFormatMetadata()
    {
        ExternalCodeValue value = new ExternalCodeValue
        {
            Value = "ADEA",
            Name = "AccountServicerDeadlineMissed",
            Definition = "Received after the servicer's deadline.",
            Requester = "Omgeo/ISITC/FPL",
            RegistrationStatus = "Registered",
            LastUpdatedDate = new IsoDateValue { Year = 2016, Month = 5, Day = 11 },
            CreationDate = new IsoDateValue { Year = 2016, Month = 5, Day = 1 },
            ActivationDate = new IsoDateValue { Year = 2016, Month = 5, Day = 1 },
            DeactivationDate = new IsoDateValue { Year = 2030, Month = 1, Day = 1 },
            ReplacedBy = "NEXT",
            AdditionalInformation = "Published by ISO 20022."
        };
        value.MappingStandards.Add("ExampleStandard");
        value.MappingValues.Add("A1");
        ExternalCodeSet set = new ExternalCodeSet
        {
            Identifier = "ExternalAcceptedReason1Code",
            Name = "ExternalAcceptedReason1Code",
            Definition = "Specifies the reason for an accepted status.",
            RegistrationStatus = "Registered",
            HasMinimumLength = true,
            MinimumLength = 1,
            HasMaximumLength = true,
            MaximumLength = 4
        };
        set.Values.Add(value);
        ExternalCodeCatalog catalog = new ExternalCodeCatalog
        {
            Artifact = CreateManifest("2026-Q1", "external-code-sets"),
            Edition = "2026-Q1",
            SchemaIdentifier = "urn:iso:std:iso:20022:tech:xsd:externalcodeset",
            SchemaVersion = "1",
            SourceFormat = "XSD"
        };
        catalog.CodeSets.Add(set);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.CodeSets.Single().Identifier, Is.EqualTo("ExternalAcceptedReason1Code"));
            Assert.That(catalog.CodeSets.Single().MaximumLength, Is.EqualTo(4));
            Assert.That(catalog.CodeSets.Single().Values.Single().Value, Is.EqualTo("ADEA"));
            Assert.That(catalog.CodeSets.Single().Values.Single().LastUpdatedDate?.Day, Is.EqualTo(11));
            Assert.That(catalog.CodeSets.Single().Values.Single().ActivationDate?.Year, Is.EqualTo(2016));
            Assert.That(catalog.CodeSets.Single().Values.Single().DeactivationDate?.Year, Is.EqualTo(2030));
            Assert.That(catalog.CodeSets.Single().Values.Single().MappingStandards,
                Is.EqualTo(new[] { "ExampleStandard" }));
            Assert.That(catalog.CodeSets.Single().Values.Single().MappingValues,
                Is.EqualTo(new[] { "A1" }));
            Assert.That(catalog.Artifact.HashAlgorithm, Is.EqualTo("SHA-256"));
        });
    }

    [Test]
    public void ImportAndLookupContractsCarryNormalizedCatalogAndSelectedProvenance()
    {
        ArtifactManifest manifest = CreateManifest("2026-Q1", "external-code-sets");
        ExternalCodeImportRequest importRequest = new ExternalCodeImportRequest
        {
            SourceName = "1Q2026_externalcodesets_v1.xsd",
            Artifact = manifest,
            Edition = "2026-Q1",
            MaxArtifactBytes = 1000000
        };
        IExternalCodeSetImporter importer = new CapturingExternalCodeSetImporter();

        ExternalCodeImportResult imported = importer.ImportXsdStream(new MemoryStream(new byte[] { 1 }), importRequest);
        ICodeSetProvider provider = new CapturingCodeSetProvider(imported.Catalog, manifest);
        CodeLookupRequest lookup = new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalAcceptedReason1Code",
            Value = "ADEA",
            Edition = "2026-Q1",
            SelectLatestEdition = false,
            ActiveOn = new IsoDateValue { Year = 2026, Month = 5, Day = 27 }
        };

        CodeLookupResult result = provider.LookupCode(lookup);

        Assert.Multiple(() =>
        {
            Assert.That(imported.Success, Is.True);
            Assert.That(imported.SourceFormat, Is.EqualTo("XSD"));
            Assert.That(imported.Catalog?.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Found, Is.True);
            Assert.That(result.ExternalCodeValue?.Value, Is.EqualTo("ADEA"));
            Assert.That(result.InternalCodeValue, Is.Null);
            Assert.That(result.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(result.Artifact, Is.SameAs(manifest));
        });
    }

    [Test]
    public void MutableCollectionDefaultsAreIndependentBetweenInstances()
    {
        CatalogueDownloadRequest firstDownload = new CatalogueDownloadRequest();
        CatalogueDownloadRequest secondDownload = new CatalogueDownloadRequest();
        ExternalCodeCatalog firstCatalog = new ExternalCodeCatalog();
        ExternalCodeCatalog secondCatalog = new ExternalCodeCatalog();
        ExternalCodeValue firstValue = new ExternalCodeValue();
        ExternalCodeValue secondValue = new ExternalCodeValue();

        firstDownload.AcceptedMediaTypes.Add(Iso20022MediaTypes.Json);
        firstCatalog.CodeSets.Add(new ExternalCodeSet());
        firstValue.MappingValues.Add("A1");

        Assert.Multiple(() =>
        {
            Assert.That(secondDownload.AcceptedMediaTypes, Is.Empty);
            Assert.That(secondCatalog.CodeSets, Is.Empty);
            Assert.That(secondValue.MappingValues, Is.Empty);
        });
    }

    private static ArtifactManifest CreateManifest(string edition, string identifier)
    {
        return new ArtifactManifest
        {
            Coordinate = new ArtifactCoordinate
            {
                Kind = ArtifactKind.ExternalCodeSet,
                Edition = edition,
                Identifier = identifier,
                Version = "1"
            },
            OriginUri = "https://example.test/" + identifier,
            HashAlgorithm = "SHA-256",
            Hash = new IsoBinaryValue { Value = new byte[] { 0x20, 0x26 } },
            MediaType = Iso20022MediaTypes.Zip,
            IsApproved = true
        };
    }

    private sealed class CapturingArtifactStore : IArtifactStore
    {
        public ArtifactStoreResult StoreArtifactStream(Stream? input, ArtifactStoreRequest? request)
        {
            if (input == null || request == null)
            {
                return new ArtifactStoreResult { ErrorCode = "artifact.invalid" };
            }

            return new ArtifactStoreResult
            {
                Success = true,
                Found = true,
                ReusedExistingArtifact = request.AllowIdenticalArtifactReuse,
                Artifact = request.Artifact,
                StoragePath = request.RootPath + "/external-code-sets.bin",
                ByteCount = input.Length
            };
        }

        public ArtifactStoreResult OpenArtifact(ArtifactStoreRequest? request)
        {
            if (request == null)
            {
                return new ArtifactStoreResult { ErrorCode = "artifact.invalid" };
            }

            return new ArtifactStoreResult
            {
                Success = true,
                Found = true,
                Content = new MemoryStream(new byte[] { 0x49, 0x53, 0x4f }),
                Artifact = request.Artifact,
                ByteCount = 3
            };
        }
    }

    private sealed class CapturingCatalogueClient : ICatalogueClient
    {
        public CatalogueDownloadResult DownloadCatalogue(CatalogueDownloadRequest? request)
        {
            if (request == null)
            {
                return new CatalogueDownloadResult { ErrorCode = "catalogue.invalid" };
            }

            ArtifactManifest manifest = new ArtifactManifest
            {
                Coordinate = request.Coordinate,
                OriginUri = request.Uri,
                Hash = request.ExpectedHash ?? new IsoBinaryValue(),
                HashAlgorithm = request.ExpectedHashAlgorithm,
                MediaType = request.AcceptedMediaTypes.Single(),
                PublicationDate = request.PublicationDate,
                LicenseNotice = request.LicenseNotice,
                IsApproved = request.IsApproved
            };
            CatalogueDownloadResult result = new CatalogueDownloadResult
            {
                Success = true,
                Content = new MemoryStream(new byte[] { 1, 2, 3 }),
                Artifact = manifest,
                FinalUri = "https://cdn.example.test/catalogue.zip",
                StatusCode = 200,
                EntityTag = request.IfNoneMatch,
                ByteCount = 3
            };
            result.RedirectUris.Add(result.FinalUri);
            return result;
        }
    }

    private sealed class CapturingExternalCodeSetImporter : IExternalCodeSetImporter
    {
        public ExternalCodeImportResult ImportJsonStream(Stream? input, ExternalCodeImportRequest? request)
        {
            return CreateResult(input, request, "JSON");
        }

        public ExternalCodeImportResult ImportXsdStream(Stream? input, ExternalCodeImportRequest? request)
        {
            return CreateResult(input, request, "XSD");
        }

        public ExternalCodeImportResult ImportXlsxStream(Stream? input, ExternalCodeImportRequest? request)
        {
            return CreateResult(input, request, "XLSX");
        }

        public ExternalCodeCatalogComparisonResult CompareCatalogs(
            ExternalCodeCatalogComparisonRequest? request)
        {
            if (request == null)
            {
                return new ExternalCodeCatalogComparisonResult
                {
                    ErrorCode = "external-code.compare.invalid"
                };
            }

            return new ExternalCodeCatalogComparisonResult
            {
                Success = true,
                Equivalent = true
            };
        }

        private static ExternalCodeImportResult CreateResult(
            Stream? input,
            ExternalCodeImportRequest? request,
            string format)
        {
            if (input == null || request == null)
            {
                return new ExternalCodeImportResult { ErrorCode = "external-code.invalid" };
            }

            ExternalCodeValue value = new ExternalCodeValue { Value = "ADEA" };
            ExternalCodeSet set = new ExternalCodeSet { Identifier = "ExternalAcceptedReason1Code" };
            set.Values.Add(value);
            ExternalCodeCatalog catalog = new ExternalCodeCatalog
            {
                Artifact = request.Artifact,
                Edition = request.Edition,
                SourceFormat = format
            };
            catalog.CodeSets.Add(set);
            return new ExternalCodeImportResult
            {
                Success = true,
                Catalog = catalog,
                Artifact = request.Artifact,
                SourceEntryName = request.SourceName,
                SourceFormat = format
            };
        }
    }

    private sealed class CapturingCodeSetProvider : ICodeSetProvider
    {
        private readonly ExternalCodeCatalog? catalog;
        private readonly ArtifactManifest artifact;

        public CapturingCodeSetProvider(ExternalCodeCatalog? catalog, ArtifactManifest artifact)
        {
            this.catalog = catalog;
            this.artifact = artifact;
        }

        public CodeLookupResult LookupCode(CodeLookupRequest? request)
        {
            if (request == null || catalog == null)
            {
                return new CodeLookupResult { ErrorCode = "code.invalid" };
            }

            ExternalCodeSet set = catalog.CodeSets.Single(item => item.Identifier == request.CodeSetIdentifier);
            ExternalCodeValue value = set.Values.Single(item => item.Value == request.Value);
            return new CodeLookupResult
            {
                Success = true,
                Found = true,
                ExternalCodeSet = set,
                ExternalCodeValue = value,
                Artifact = artifact,
                Edition = catalog.Edition
            };
        }
    }
}
