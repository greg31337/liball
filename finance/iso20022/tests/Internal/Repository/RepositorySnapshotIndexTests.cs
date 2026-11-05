using iso20022;

namespace iso20022.Tests;

public class RepositorySnapshotIndexTests
{
    [Test]
    public void IndexerBuildsEveryQueryKeyInDeterministicSnapshotOrder()
    {
        RepositorySnapshot snapshot = CreateValidSnapshot();
        RepositorySnapshotIndex index = RepositorySnapshotIndexer.BuildIndex(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(index.Entries, Has.Count.EqualTo(16));
            Assert.That(index.FindIdentifier("_message10").Single().Value,
                Is.SameAs(snapshot.MessageDefinitions[0]));
            Assert.That(index.FindName("SharedName").Select(entry => entry.Identifier),
                Is.EqualTo(new[] { "_areaPacs", "_component" }));
            Assert.That(index.FindKind(RepositoryEntityKind.MessageDefinition)
                    .Select(entry => entry.Identifier),
                Is.EqualTo(new[] { "_message10", "_message11", "_statement" }));
            Assert.That(index.FindKind(RepositoryEntityKind.CodeValue)
                    .Select(entry => entry.Identifier),
                Is.EqualTo(new[] { "_codeOne", "_codeTwo" }));
            Assert.That(index.FindStatus(RegistrationStatus.Registered), Has.Count.EqualTo(8));
            Assert.That(index.FindBusinessArea("pacs").Select(entry => entry.Identifier),
                Is.EqualTo(new[] { "_areaPacs", "_message10", "_message11" }));
            Assert.That(index.FindMessageSet("_setA").Select(entry => entry.Identifier),
                Is.EqualTo(new[] { "_setA", "_message10", "_message11" }));
            Assert.That(index.FindMessageSet("_setB").Select(entry => entry.Identifier),
                Is.EqualTo(new[] { "_setB", "_message11", "_statement" }));
            Assert.That(index.FindMessageIdentifier("pacs.008.001.10").Single().Identifier,
                Is.EqualTo("_message10"));
            Assert.That(index.FindMessageFunctionality("008")
                    .Select(entry => entry.Identifier),
                Is.EqualTo(new[] { "_message10", "_message11" }));
            Assert.That(index.FindVariant("001").Select(entry => entry.Identifier),
                Is.EqualTo(new[] { "_message10", "_message11", "_statement" }));
            Assert.That(index.FindNamespace(
                    "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.11").Single().Identifier,
                Is.EqualTo("_message11"));
            Assert.That(index.FindNamespace("urn:example:schema:two").Single().Identifier,
                Is.EqualTo("_external"));
            Assert.That(index.FindVersion("10").Select(entry => entry.Identifier),
                Is.EqualTo(new[] { "_message10", "_statement" }));
            Assert.That(index.Entries.Select(entry => entry.Order),
                Is.EqualTo(Enumerable.Range(0, index.Entries.Count)));
        });
    }

    [Test]
    public void BuiltIndexRetainsKeysWhenMutableSnapshotValuesChange()
    {
        RepositorySnapshot snapshot = CreateValidSnapshot();
        RepositorySnapshotIndex first = RepositorySnapshotIndexer.BuildIndex(snapshot);
        snapshot.BusinessComponents[0].Name = "ChangedName";
        snapshot.MessageDefinitions[0].MessageIdentifier.Value = "pacs.999.001.10";
        RepositorySnapshotIndex second = RepositorySnapshotIndexer.BuildIndex(snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(first.FindName("SharedName").Select(entry => entry.Identifier),
                Does.Contain("_component"));
            Assert.That(first.FindName("ChangedName"), Is.Empty);
            Assert.That(first.FindMessageIdentifier("pacs.008.001.10"), Has.Count.EqualTo(1));
            Assert.That(first.FindMessageIdentifier("pacs.999.001.10"), Is.Empty);
            Assert.That(second.FindName("ChangedName").Single().Identifier,
                Is.EqualTo("_component"));
            Assert.That(second.FindMessageIdentifier("pacs.999.001.10").Single().Identifier,
                Is.EqualTo("_message10"));
        });
    }

    [Test]
    public void IntegrityCheckerAcceptsConsistentIdentityReferencesAndMultiplicities()
    {
        RepositorySnapshot snapshot = CreateValidSnapshot();
        RepositorySnapshotIndex index = RepositorySnapshotIndexer.BuildIndex(snapshot);
        RepositoryIntegrityResult result = RepositoryIntegrityChecker.ValidateSnapshot(
            snapshot,
            index,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.InspectedEntityCount, Is.EqualTo(index.Entries.Count));
            Assert.That(result.InspectedReferenceCount, Is.EqualTo(snapshot.References.Count));
        });
    }

    [Test]
    public void IntegrityCheckerReportsCorruptionInStableBoundedOrder()
    {
        RepositorySnapshot snapshot = CreateValidSnapshot();
        snapshot.BusinessRoles.Add(new BusinessRoleDefinition { Name = "MissingIdentifier" });
        snapshot.BusinessComponents[0].Identifier = "_areaPacs";
        MessageDefinitionMetadata duplicateMessage = snapshot.MessageDefinitions[1];
        duplicateMessage.MessageIdentifier.BusinessArea = "pacs";
        duplicateMessage.MessageIdentifier.MessageFunctionality = "008";
        duplicateMessage.MessageIdentifier.Variant = "001";
        duplicateMessage.MessageIdentifier.Version = "10";
        duplicateMessage.MessageIdentifier.Value = "pacs.008.001.10";
        duplicateMessage.NamespaceUri = "urn:wrong";
        duplicateMessage.BusinessArea = Reference(
            duplicateMessage.Identifier,
            RepositoryEntityKind.MessageDefinition,
            "businessArea",
            "_areaCamt",
            RepositoryEntityKind.BusinessArea,
            0);
        snapshot.MessageElements[0].Multiplicity.MinimumOccurs = 2;
        snapshot.MessageElements[0].Multiplicity.MaximumOccurs = 1;
        snapshot.References.Add(new RepositoryReference
        {
            SourceIdentifier = "_component",
            SourceKind = RepositoryEntityKind.BusinessComponent,
            PropertyName = "superType",
            Position = -1,
            TargetIdentifier = "_missing",
            IsResolved = false,
            ErrorCode = "repository.xmi.reference_missing",
            ErrorMessage = "The target is missing."
        });
        snapshot.References.Add(Reference(
            "_message10",
            RepositoryEntityKind.MessageDefinition,
            "businessArea",
            "_areaCamt",
            RepositoryEntityKind.CodeSet,
            0));
        RepositorySnapshotIndex index = RepositorySnapshotIndexer.BuildIndex(snapshot);
        RepositoryIntegrityResult first = RepositoryIntegrityChecker.ValidateSnapshot(
            snapshot,
            index,
            100);
        RepositoryIntegrityResult second = RepositoryIntegrityChecker.ValidateSnapshot(
            snapshot,
            index,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsValid, Is.False);
            Assert.That(first.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new[]
            {
                "repository.integrity.identifier_missing",
                "repository.integrity.identifier_duplicate",
                "repository.integrity.message_identifier_duplicate",
                "repository.integrity.message_namespace_mismatch",
                "repository.integrity.message_business_area_mismatch",
                "repository.integrity.multiplicity_range_invalid",
                "repository.integrity.reference_position_negative",
                "repository.xmi.reference_missing",
                "repository.integrity.reference_kind_mismatch"
            }));
            Assert.That(first.Diagnostics.Select(diagnostic => diagnostic.Order),
                Is.EqualTo(Enumerable.Range(0, first.Diagnostics.Count)));
            Assert.That(first.Diagnostics.Select(DiagnosticKey),
                Is.EqualTo(second.Diagnostics.Select(DiagnosticKey)));
            Assert.Throws<InvalidOperationException>(() =>
                RepositoryIntegrityChecker.ValidateSnapshot(snapshot, index, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RepositoryIntegrityChecker.ValidateSnapshot(snapshot, index, 0));
        });
    }

    private static string DiagnosticKey(RepositoryIntegrityDiagnostic diagnostic)
    {
        return diagnostic.Order + "|" + diagnostic.Code + "|" + diagnostic.Identifier + "|" +
            diagnostic.RelatedIdentifier;
    }

    private static RepositorySnapshot CreateValidSnapshot()
    {
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Identifier = "_repository",
            Edition = "test-edition",
            DataDictionary = Reference(
                "_repository",
                RepositoryEntityKind.Repository,
                "dataDictionary",
                "_dictionary",
                RepositoryEntityKind.DataDictionary,
                0),
            BusinessProcessCatalogue = Reference(
                "_repository",
                RepositoryEntityKind.Repository,
                "businessProcessCatalogue",
                "_catalogue",
                RepositoryEntityKind.BusinessProcessCatalogue,
                0)
        };
        snapshot.References.Add(snapshot.DataDictionary);
        snapshot.References.Add(snapshot.BusinessProcessCatalogue);
        BusinessAreaDefinition pacs = new BusinessAreaDefinition
        {
            Identifier = "_areaPacs",
            Name = "SharedName",
            Code = "pacs",
            Registration = Registration(RegistrationStatus.Registered)
        };
        BusinessAreaDefinition camt = new BusinessAreaDefinition
        {
            Identifier = "_areaCamt",
            Name = "CashManagement",
            Code = "camt",
            Registration = Registration(RegistrationStatus.Registered)
        };
        snapshot.BusinessAreas.Add(pacs);
        snapshot.BusinessAreas.Add(camt);
        snapshot.BusinessComponents.Add(new BusinessComponentDefinition
        {
            Identifier = "_component",
            Name = "SharedName",
            Registration = Registration(RegistrationStatus.Registered)
        });
        MessageSetDefinition setA = new MessageSetDefinition
        {
            Identifier = "_setA",
            Name = "PaymentsSet",
            Registration = Registration(RegistrationStatus.Registered)
        };
        MessageSetDefinition setB = new MessageSetDefinition
        {
            Identifier = "_setB",
            Name = "ReportingSet",
            Registration = Registration(RegistrationStatus.ProvisionallyRegistered)
        };
        snapshot.MessageSets.Add(setA);
        snapshot.MessageSets.Add(setB);
        snapshot.MessageDefinitions.Add(Message(
            "_message10",
            "CreditTransferV10",
            "pacs.008.001.10",
            "pacs",
            "008",
            "001",
            "10",
            "_areaPacs",
            RegistrationStatus.Registered,
            new[] { "_setA" }));
        snapshot.MessageDefinitions.Add(Message(
            "_message11",
            "CreditTransferV11",
            "pacs.008.001.11",
            "pacs",
            "008",
            "001",
            "11",
            "_areaPacs",
            RegistrationStatus.ProvisionallyRegistered,
            new[] { "_setA", "_setB" }));
        snapshot.MessageDefinitions.Add(Message(
            "_statement",
            "BankToCustomerStatementV10",
            "camt.053.001.10",
            "camt",
            "053",
            "001",
            "10",
            "_areaCamt",
            RegistrationStatus.Registered,
            new[] { "_setB" }));
        snapshot.MessageElements.Add(new MessageElementDefinition
        {
            Identifier = "_element",
            Kind = MessageElementKind.Attribute,
            Name = "Amount",
            Registration = Registration(RegistrationStatus.ProvisionallyRegistered),
            Multiplicity = new MultiplicityDefinition
            {
                MinimumOccurs = 0,
                HasMaximumOccurs = true,
                MaximumOccurs = 1
            }
        });
        CodeSetDefinition codeSet = new CodeSetDefinition
        {
            Identifier = "_codeSet",
            Name = "StatusCode",
            Registration = Registration(RegistrationStatus.Registered)
        };
        codeSet.Codes.Add(new CodeValueDefinition
        {
            Identifier = "_codeOne",
            Name = "Accepted",
            Value = "ACCP"
        });
        codeSet.Codes.Add(new CodeValueDefinition
        {
            Identifier = "_codeTwo",
            Name = "Rejected",
            Value = "RJCT"
        });
        snapshot.CodeSets.Add(codeSet);
        ExternalSchemaDefinition external = new ExternalSchemaDefinition
        {
            Identifier = "_external",
            Name = "SupplementarySchema",
            Registration = Registration(RegistrationStatus.Registered)
        };
        external.NamespaceUris.Add("urn:example:schema:one");
        external.NamespaceUris.Add("urn:example:schema:two");
        snapshot.ExternalSchemas.Add(external);
        return snapshot;
    }

    private static MessageDefinitionMetadata Message(
        string identifier,
        string name,
        string value,
        string businessArea,
        string functionality,
        string variant,
        string version,
        string areaIdentifier,
        RegistrationStatus status,
        string[] messageSets)
    {
        MessageDefinitionMetadata message = new MessageDefinitionMetadata
        {
            Identifier = identifier,
            Name = name,
            MessageIdentifier = new MessageIdentifier
            {
                BusinessArea = businessArea,
                MessageFunctionality = functionality,
                Variant = variant,
                Version = version,
                Value = value
            },
            NamespaceUri = Iso20022Namespaces.SchemaUrnPrefix + value,
            BusinessArea = Reference(
                identifier,
                RepositoryEntityKind.MessageDefinition,
                "businessArea",
                areaIdentifier,
                RepositoryEntityKind.BusinessArea,
                0),
            Registration = Registration(status)
        };
        int setIndex = 0;
        while (setIndex < messageSets.Length)
        {
            message.MessageSets.Add(Reference(
                identifier,
                RepositoryEntityKind.MessageDefinition,
                "messageSet",
                messageSets[setIndex],
                RepositoryEntityKind.MessageSet,
                setIndex));
            setIndex++;
        }

        return message;
    }

    private static RegistrationMetadata Registration(RegistrationStatus status)
    {
        RegistrationMetadata registration = new RegistrationMetadata();
        registration.Status = status;
        if (status == RegistrationStatus.Registered)
        {
            registration.StatusValue = "Registered";
        }

        if (status == RegistrationStatus.ProvisionallyRegistered)
        {
            registration.StatusValue = "Provisionally Registered";
        }

        return registration;
    }

    private static RepositoryReference Reference(
        string sourceIdentifier,
        RepositoryEntityKind sourceKind,
        string propertyName,
        string targetIdentifier,
        RepositoryEntityKind targetKind,
        int position)
    {
        return new RepositoryReference
        {
            SourceIdentifier = sourceIdentifier,
            SourceKind = sourceKind,
            PropertyName = propertyName,
            TargetIdentifier = targetIdentifier,
            TargetKind = targetKind,
            Position = position,
            IsResolved = true
        };
    }
}
