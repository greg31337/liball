using iso20022;

namespace iso20022.Tests;

public class RepositoryQueryApiTests
{
    [Test]
    public void QueryFindsExactAndDuplicateIdentifiersInSnapshotOrder()
    {
        RepositorySnapshot snapshot = CreateSnapshot();
        IRepositoryQuery query = new RepositoryQuery(snapshot);

        RepositoryQueryResult exact = query.QueryRepository(new RepositoryQueryRequest
        {
            Identifier = "_message02"
        });
        RepositoryQueryResult duplicate = query.QueryRepository(new RepositoryQueryRequest
        {
            Identifier = "_duplicate"
        });
        RepositoryQueryResult missing = query.QueryRepository(new RepositoryQueryRequest
        {
            Identifier = "_missing"
        });

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Items.Single().Value,
                Is.SameAs(snapshot.MessageDefinitions[1]));
            Assert.That(exact.Items.Single().MessageIdentifier,
                Is.EqualTo("pacs.008.001.02"));
            Assert.That(duplicate.Items.Select(item => item.Kind), Is.EqualTo(new[]
            {
                RepositoryEntityKind.BusinessComponent,
                RepositoryEntityKind.DataType
            }));
            Assert.That(duplicate.Items.Select(item => item.Value), Is.EqualTo(new object[]
            {
                snapshot.BusinessComponents[0],
                snapshot.DataTypes[0]
            }));
            Assert.That(missing.Success, Is.True);
            Assert.That(missing.TotalMatchCount, Is.EqualTo(0));
            Assert.That(missing.Items, Is.Empty);
        });
    }

    [Test]
    public void QueryFindsNamesKindsAndEveryRegistrationStatusExactly()
    {
        IRepositoryQuery query = new RepositoryQuery(CreateSnapshot());
        RepositoryQueryResult name = query.QueryRepository(new RepositoryQueryRequest
        {
            Name = "Shared"
        });
        RepositoryQueryResult kind = query.QueryRepository(new RepositoryQueryRequest
        {
            HasKind = true,
            Kind = RepositoryEntityKind.MessageDefinition
        });
        RepositoryQueryResult registered = query.QueryRepository(new RepositoryQueryRequest
        {
            HasRegistrationStatus = true,
            RegistrationStatus = RegistrationStatus.Registered
        });
        RepositoryQueryResult unknown = query.QueryRepository(new RepositoryQueryRequest
        {
            HasRegistrationStatus = true,
            RegistrationStatus = RegistrationStatus.Unknown
        });

        Assert.Multiple(() =>
        {
            Assert.That(name.Items.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "_areaPacs",
                "_duplicate",
                "_message02",
                "_duplicate"
            }));
            Assert.That(kind.Items.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "_message01",
                "_message02",
                "_message10",
                "_message009",
                "_camt08"
            }));
            Assert.That(registered.Items.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "_areaPacs",
                "_areaCamt",
                "_setA",
                "_message02",
                "_message10",
                "_camt08",
                "_codes",
                "_external"
            }));
            Assert.That(unknown.Items.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "_duplicate",
                "_duplicate",
                "_codeOne",
                "_codeTwo"
            }));
            Assert.That(name.Items.Select(item => item.Name),
                Is.All.EqualTo("Shared"));
            Assert.That(query.QueryRepository(new RepositoryQueryRequest
            {
                Name = "shared"
            }).Items, Is.Empty);
        });
    }

    [Test]
    public void QueryFindsBusinessAreasAndMessageSetsWithStableMembershipOrdering()
    {
        IRepositoryQuery query = new RepositoryQuery(CreateSnapshot());
        RepositoryQueryResult area = query.QueryRepository(new RepositoryQueryRequest
        {
            BusinessAreaCode = "pacs"
        });
        RepositoryQueryResult set = query.QueryRepository(new RepositoryQueryRequest
        {
            MessageSetIdentifier = "_setA"
        });
        RepositoryQueryResult messagesInSet = query.QueryRepository(new RepositoryQueryRequest
        {
            MessageSetIdentifier = "_setA",
            HasKind = true,
            Kind = RepositoryEntityKind.MessageDefinition
        });

        Assert.Multiple(() =>
        {
            Assert.That(area.Items.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "_areaPacs",
                "_message01",
                "_message02",
                "_message10",
                "_message009"
            }));
            Assert.That(set.Items.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "_setA",
                "_message01",
                "_message02",
                "_message10"
            }));
            Assert.That(messagesInSet.Items.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "_message01",
                "_message02",
                "_message10"
            }));
            Assert.That(query.QueryRepository(new RepositoryQueryRequest
            {
                BusinessAreaCode = "PACS"
            }).Items, Is.Empty);
        });
    }

    [Test]
    public void QueryFindsMessageIdentifiersNamespacesFunctionalityAndVariants()
    {
        IRepositoryQuery query = new RepositoryQuery(CreateSnapshot());
        RepositoryQueryResult identifier = query.QueryRepository(new RepositoryQueryRequest
        {
            MessageIdentifier = "pacs.008.001.10"
        });
        RepositoryQueryResult messageNamespace = query.QueryRepository(new RepositoryQueryRequest
        {
            NamespaceUri = Iso20022Namespaces.SchemaUrnPrefix + "pacs.008.001.10"
        });
        RepositoryQueryResult externalNamespace = query.QueryRepository(
            new RepositoryQueryRequest
            {
                NamespaceUri = "urn:example:schema:two"
            });
        RepositoryQueryResult functionality = query.QueryRepository(
            new RepositoryQueryRequest
            {
                MessageFunctionality = "008"
            });
        RepositoryQueryResult variant = query.QueryRepository(new RepositoryQueryRequest
        {
            Variant = "001",
            BusinessAreaCode = "pacs"
        });

        Assert.Multiple(() =>
        {
            Assert.That(identifier.Items.Single().Identifier, Is.EqualTo("_message10"));
            Assert.That(messageNamespace.Items.Single().Identifier,
                Is.EqualTo("_message10"));
            Assert.That(externalNamespace.Items.Single().Identifier,
                Is.EqualTo("_external"));
            Assert.That(externalNamespace.Items.Single().NamespaceUri,
                Is.EqualTo("urn:example:schema:two"));
            Assert.That(functionality.Items.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "_message01",
                "_message02",
                "_message10"
            }));
            Assert.That(variant.Items.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "_message01",
                "_message02",
                "_message10",
                "_message009"
            }));
        });
    }

    [Test]
    public void QueryAppliesEveryVersionPolicyWithNumericVersionOrdering()
    {
        IRepositoryQuery query = new RepositoryQuery(CreateSnapshot());
        RepositoryQueryResult exact = query.QueryRepository(new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.Exact,
                BaselineIdentifier = Identifier("pacs", "008", "001", "02")
            }
        });
        RepositoryQueryResult current = query.QueryRepository(new RepositoryQueryRequest
        {
            HasKind = true,
            Kind = RepositoryEntityKind.MessageDefinition,
            VersionSelector = new VersionSelector { Kind = VersionSelectionKind.Current }
        });
        RepositoryQueryResult latest = query.QueryRepository(new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.LatestCompatible,
                BaselineIdentifier = Identifier("pacs", "008", "001", "02")
            }
        });
        RepositoryQueryResult range = query.QueryRepository(new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.Range,
                MinimumVersion = "02",
                MaximumVersion = "08"
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(exact.Items.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_message02" }));
            Assert.That(current.Items.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "_message10",
                "_message009",
                "_camt08"
            }));
            Assert.That(latest.Items.Select(item => item.Identifier),
                Is.EqualTo(new[] { "_message10" }));
            Assert.That(range.Items.Select(item => item.Identifier), Is.EqualTo(new[]
            {
                "_message02",
                "_camt08"
            }));
            Assert.That(latest.Items.Single().Version, Is.EqualTo("10"));
        });
    }

    [Test]
    public void QueryCombinesCriteriaAndArtifactEditionConjunctively()
    {
        IRepositoryQuery query = new RepositoryQuery(CreateSnapshot());
        RepositoryQueryResult match = query.QueryRepository(new RepositoryQueryRequest
        {
            Name = "CreditTransferV10",
            HasKind = true,
            Kind = RepositoryEntityKind.MessageDefinition,
            HasRegistrationStatus = true,
            RegistrationStatus = RegistrationStatus.Registered,
            BusinessAreaCode = "pacs",
            MessageSetIdentifier = "_setB",
            MessageFunctionality = "008",
            Variant = "001",
            ArtifactEdition = "2026-06"
        });
        RepositoryQueryResult conflict = query.QueryRepository(new RepositoryQueryRequest
        {
            Identifier = "_message10",
            BusinessAreaCode = "camt"
        });
        RepositoryQueryResult wrongEdition = query.QueryRepository(
            new RepositoryQueryRequest
            {
                ArtifactEdition = "2025-12"
            });

        Assert.Multiple(() =>
        {
            Assert.That(match.Items.Single().Identifier, Is.EqualTo("_message10"));
            Assert.That(conflict.Success, Is.True);
            Assert.That(conflict.Items, Is.Empty);
            Assert.That(wrongEdition.Success, Is.True);
            Assert.That(wrongEdition.TotalMatchCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void QueryResultsAreRepeatableLimitedAndIndependentOfReturnedLists()
    {
        IRepositoryQuery query = new RepositoryQuery(CreateSnapshot());
        RepositoryQueryRequest request = new RepositoryQueryRequest
        {
            HasKind = true,
            Kind = RepositoryEntityKind.MessageDefinition,
            MaximumResultCount = 2
        };

        RepositoryQueryResult first = query.QueryRepository(request);
        string[] originalIdentifiers = first.Items.Select(item => item.Identifier).ToArray();
        first.Items.Clear();
        RepositoryQueryResult second = query.QueryRepository(request);
        RepositoryQueryResult third = query.QueryRepository(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.TotalMatchCount, Is.EqualTo(5));
            Assert.That(first.Truncated, Is.True);
            Assert.That(originalIdentifiers, Is.EqualTo(new[]
            {
                "_message01",
                "_message02"
            }));
            Assert.That(second.Items.Select(item => item.Identifier),
                Is.EqualTo(originalIdentifiers));
            Assert.That(second.Items.Select(ItemSignature),
                Is.EqualTo(third.Items.Select(ItemSignature)));
            Assert.That(second.Items, Is.Not.SameAs(third.Items));
        });
    }

    [Test]
    public void QueryIndexRetainsKeysWhenTheMutableSnapshotChanges()
    {
        RepositorySnapshot snapshot = CreateSnapshot();
        IRepositoryQuery query = new RepositoryQuery(snapshot);
        snapshot.MessageDefinitions[1].Name = "ChangedName";
        snapshot.MessageDefinitions[1].MessageIdentifier.Value = "pacs.999.001.02";
        snapshot.MessageDefinitions[1].MessageSets.Clear();

        RepositoryQueryResult original = query.QueryRepository(new RepositoryQueryRequest
        {
            Name = "Shared",
            MessageIdentifier = "pacs.008.001.02",
            MessageSetIdentifier = "_setA"
        });

        Assert.Multiple(() =>
        {
            Assert.That(original.Items.Single().Identifier, Is.EqualTo("_message02"));
            Assert.That(((MessageDefinitionMetadata)original.Items.Single().Value!).Name,
                Is.EqualTo("ChangedName"));
            Assert.That(query.QueryRepository(new RepositoryQueryRequest
            {
                Name = "ChangedName"
            }).Items, Is.Empty);
            Assert.That(query.QueryRepository(new RepositoryQueryRequest
            {
                MessageIdentifier = "pacs.999.001.02"
            }).Items, Is.Empty);
        });
    }

    [Test]
    public void QueryReturnsStableFailuresForInvalidRequestsAndSelectors()
    {
        IRepositoryQuery query = new RepositoryQuery(CreateSnapshot());
        RepositoryQueryResult missing = query.QueryRepository(null);
        RepositoryQueryResult limit = query.QueryRepository(new RepositoryQueryRequest
        {
            MaximumResultCount = 0
        });
        RepositoryQueryResult exact = query.QueryRepository(new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector { Kind = VersionSelectionKind.Exact }
        });
        RepositoryQueryResult latest = query.QueryRepository(new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.LatestCompatible,
                BaselineIdentifier = Identifier("PACS", "008", "001", "02")
            }
        });
        RepositoryQueryResult range = query.QueryRepository(new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.Range,
                MinimumVersion = "10",
                MaximumVersion = "02"
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCode, Is.EqualTo("repository.query.request_missing"));
            Assert.That(limit.ErrorCode,
                Is.EqualTo("repository.query.result_limit_invalid"));
            Assert.That(exact.ErrorCode,
                Is.EqualTo("repository.query.version_baseline_invalid"));
            Assert.That(latest.ErrorCode,
                Is.EqualTo("repository.query.version_baseline_invalid"));
            Assert.That(range.ErrorCode,
                Is.EqualTo("repository.query.version_range_invalid"));
            Assert.That(missing.Success, Is.False);
            Assert.That(limit.Items, Is.Empty);
            Assert.Throws<ArgumentNullException>(() => new RepositoryQuery(null!));
        });
    }

    [Test]
    public void QueryExercisesVariantSeedAndEachConjunctiveMismatch()
    {
        IRepositoryQuery query = new RepositoryQuery(CreateSnapshot());
        RepositoryQueryResult variants = query.QueryRepository(new RepositoryQueryRequest
        {
            Variant = "001"
        });
        RepositoryIndexEntry entry = new RepositoryIndexEntry
        {
            Identifier = "id",
            Name = "name",
            Kind = RepositoryEntityKind.MessageDefinition,
            RegistrationStatus = RegistrationStatus.Registered,
            BusinessAreaCode = "pacs",
            MessageIdentifier = "pacs.008.001.01",
            NamespaceUri = "urn:primary",
            MessageFunctionality = "008",
            Variant = "001",
            Version = "01"
        };
        entry.MessageSetIdentifiers.Add("set");
        entry.NamespaceUris.Add("urn:secondary");

        Assert.Multiple(() =>
        {
            Assert.That(variants.TotalMatchCount, Is.EqualTo(5));
            Assert.That(RepositoryQuery.MatchesQueryRequest(entry,
                new RepositoryQueryRequest { Identifier = "other" }), Is.False);
            Assert.That(RepositoryQuery.MatchesQueryRequest(entry,
                new RepositoryQueryRequest { Name = "other" }), Is.False);
            Assert.That(RepositoryQuery.MatchesQueryRequest(entry,
                new RepositoryQueryRequest
                {
                    HasRegistrationStatus = true,
                    RegistrationStatus = RegistrationStatus.Obsolete
                }), Is.False);
            Assert.That(RepositoryQuery.MatchesQueryRequest(entry,
                new RepositoryQueryRequest { MessageSetIdentifier = "other" }), Is.False);
            Assert.That(RepositoryQuery.MatchesQueryRequest(entry,
                new RepositoryQueryRequest { MessageIdentifier = "other" }), Is.False);
            Assert.That(RepositoryQuery.MatchesQueryRequest(entry,
                new RepositoryQueryRequest { NamespaceUri = "other" }), Is.False);
            Assert.That(RepositoryQuery.MatchesQueryRequest(entry,
                new RepositoryQueryRequest { NamespaceUri = "urn:primary" }), Is.True);
            Assert.That(RepositoryQuery.MatchesQueryRequest(entry,
                new RepositoryQueryRequest { NamespaceUri = "urn:secondary" }), Is.True);
            Assert.That(RepositoryQuery.MatchesQueryRequest(entry,
                new RepositoryQueryRequest { MessageFunctionality = "other" }), Is.False);
        });
    }

    [Test]
    public void QueryVersionValidationAndHelpersCoverBoundaryValues()
    {
        RepositoryQuery query = new RepositoryQuery(CreateSnapshot());
        RepositoryQueryRequest emptyMinimum = new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.Range,
                MinimumVersion = string.Empty,
                MaximumVersion = "02"
            }
        };
        RepositoryQueryRequest emptyMaximum = new RepositoryQueryRequest
        {
            VersionSelector = new VersionSelector
            {
                Kind = VersionSelectionKind.Range,
                MinimumVersion = "01",
                MaximumVersion = string.Empty
            }
        };
        RepositoryIndexEntry entry = new RepositoryIndexEntry
        {
            Kind = RepositoryEntityKind.MessageDefinition,
            MessageIdentifier = "pacs.008.001.01"
        };

        Assert.Multiple(() =>
        {
            Assert.That(query.QueryRepository(emptyMinimum).ErrorCode,
                Is.EqualTo("repository.query.version_range_invalid"));
            Assert.That(query.QueryRepository(emptyMaximum).ErrorCode,
                Is.EqualTo("repository.query.version_range_invalid"));
            Assert.That(RepositoryQuery.MatchesVersionFilter(entry,
                new VersionSelector { Kind = VersionSelectionKind.Exact }), Is.False);
            Assert.That(RepositoryQuery.MatchesVersionFilter(entry,
                new VersionSelector
                {
                    Kind = VersionSelectionKind.Exact,
                    BaselineIdentifier = new MessageIdentifier()
                }), Is.False);
            Assert.That(RepositoryQuery.ContainsExactText(new List<string>(), "x"), Is.False);
            Assert.That(RepositoryQuery.ContainsExactText(
                new List<string> { "a" }, "x"), Is.False);
            Assert.That(RepositoryQuery.IsNumericVersion(string.Empty), Is.False);
            Assert.That(RepositoryQuery.IsNumericVersion("/"), Is.False);
            Assert.That(RepositoryQuery.IsNumericVersion(":"), Is.False);
            Assert.That(RepositoryQuery.IsNumericVersion("01"), Is.True);
        });
    }

    private static string ItemSignature(RepositoryQueryItem item)
    {
        return item.Identifier + "|" + item.Name + "|" + item.Kind + "|" +
            item.RegistrationStatus + "|" + item.MessageIdentifier;
    }

    private static RepositorySnapshot CreateSnapshot()
    {
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Identifier = "_repository",
            Edition = "2026-06",
            DataDictionary = Reference("_repository", "dataDictionary", "_dictionary"),
            BusinessProcessCatalogue = Reference(
                "_repository",
                "businessProcessCatalogue",
                "_catalogue")
        };
        snapshot.BusinessAreas.Add(new BusinessAreaDefinition
        {
            Identifier = "_areaPacs",
            Name = "Shared",
            Code = "pacs",
            Registration = Registration(RegistrationStatus.Registered)
        });
        snapshot.BusinessAreas.Add(new BusinessAreaDefinition
        {
            Identifier = "_areaCamt",
            Name = "CashManagement",
            Code = "camt",
            Registration = Registration(RegistrationStatus.Registered)
        });
        snapshot.BusinessComponents.Add(new BusinessComponentDefinition
        {
            Identifier = "_duplicate",
            Name = "Shared"
        });
        snapshot.MessageSets.Add(new MessageSetDefinition
        {
            Identifier = "_setA",
            Name = "PaymentsSet",
            Registration = Registration(RegistrationStatus.Registered)
        });
        snapshot.MessageSets.Add(new MessageSetDefinition
        {
            Identifier = "_setB",
            Name = "ReportingSet",
            Registration = Registration(RegistrationStatus.ProvisionallyRegistered)
        });
        snapshot.MessageDefinitions.Add(Message(
            "_message01", "CreditTransferV1", "pacs", "008", "001", "01",
            RegistrationStatus.Obsolete, new[] { "_setA" }));
        snapshot.MessageDefinitions.Add(Message(
            "_message02", "Shared", "pacs", "008", "001", "02",
            RegistrationStatus.Registered, new[] { "_setA" }));
        snapshot.MessageDefinitions.Add(Message(
            "_message10", "CreditTransferV10", "pacs", "008", "001", "10",
            RegistrationStatus.Registered, new[] { "_setA", "_setB", "_setA" }));
        snapshot.MessageDefinitions.Add(Message(
            "_message009", "PaymentStatus", "pacs", "009", "001", "01",
            RegistrationStatus.ProvisionallyRegistered, new[] { "_setB" }));
        snapshot.MessageDefinitions.Add(Message(
            "_camt08", "AccountStatement", "camt", "053", "001", "08",
            RegistrationStatus.Registered, new string[0]));
        snapshot.DataTypes.Add(new DataTypeDefinition
        {
            Identifier = "_duplicate",
            Name = "Shared"
        });
        CodeSetDefinition codes = new CodeSetDefinition
        {
            Identifier = "_codes",
            Name = "StatusCode",
            Registration = Registration(RegistrationStatus.Registered)
        };
        codes.Codes.Add(new CodeValueDefinition
        {
            Identifier = "_codeOne",
            Name = "Accepted",
            Value = "ACCP"
        });
        codes.Codes.Add(new CodeValueDefinition
        {
            Identifier = "_codeTwo",
            Name = "Rejected",
            Value = "RJCT"
        });
        snapshot.CodeSets.Add(codes);
        ExternalSchemaDefinition external = new ExternalSchemaDefinition
        {
            Identifier = "_external",
            Name = "SupplementarySchema",
            Registration = Registration(RegistrationStatus.Registered)
        };
        external.NamespaceUris.Add("urn:example:schema:one");
        external.NamespaceUris.Add("urn:example:schema:two");
        external.NamespaceUris.Add("urn:example:schema:two");
        snapshot.ExternalSchemas.Add(external);
        return snapshot;
    }

    private static MessageDefinitionMetadata Message(
        string identifier,
        string name,
        string area,
        string functionality,
        string variant,
        string version,
        RegistrationStatus status,
        string[] sets)
    {
        MessageDefinitionMetadata message = new MessageDefinitionMetadata
        {
            Identifier = identifier,
            Name = name,
            MessageIdentifier = Identifier(area, functionality, variant, version),
            NamespaceUri = Iso20022Namespaces.SchemaUrnPrefix + area + "." +
                functionality + "." + variant + "." + version,
            Registration = Registration(status),
            BusinessArea = Reference(identifier, "businessArea", "_area" +
                char.ToUpperInvariant(area[0]) + area.Substring(1))
        };
        int setIndex = 0;
        while (setIndex < sets.Length)
        {
            message.MessageSets.Add(Reference(identifier, "messageSet", sets[setIndex]));
            setIndex++;
        }

        return message;
    }

    private static MessageIdentifier Identifier(
        string area,
        string functionality,
        string variant,
        string version)
    {
        return new MessageIdentifier
        {
            BusinessArea = area,
            MessageFunctionality = functionality,
            Variant = variant,
            Version = version,
            Value = area + "." + functionality + "." + variant + "." + version
        };
    }

    private static RegistrationMetadata Registration(RegistrationStatus status)
    {
        return new RegistrationMetadata
        {
            Status = status,
            StatusValue = status.ToString()
        };
    }

    private static RepositoryReference Reference(
        string source,
        string property,
        string target)
    {
        return new RepositoryReference
        {
            SourceIdentifier = source,
            PropertyName = property,
            TargetIdentifier = target,
            IsResolved = true
        };
    }
}
