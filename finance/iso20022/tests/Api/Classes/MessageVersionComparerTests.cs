using iso20022;

namespace iso20022.Tests;

public class MessageVersionComparerTests
{
    [Test]
    public void EquivalentSelectedMessagesIgnoreUnrelatedRepositoryChanges()
    {
        RepositorySnapshot source = CreateMessageSnapshot("10", false);
        RepositorySnapshot target = CreateMessageSnapshot("10", false);
        AddSimpleMessageGraph(source, false);
        AddSimpleMessageGraph(target, false);
        source.BusinessComponents.Add(new BusinessComponentDefinition
        {
            Identifier = "_unrelated",
            Name = "Unrelated",
            Definition = "source"
        });
        target.BusinessComponents.Add(new BusinessComponentDefinition
        {
            Identifier = "_unrelated",
            Name = "Unrelated",
            Definition = "target"
        });

        RepositoryDiff difference = new MessageVersionComparer().CompareMessageVersions(
            Request(source, target, "10", "10"));

        Assert.Multiple(() =>
        {
            Assert.That(difference.Success, Is.True);
            Assert.That(difference.Equivalent, Is.True);
            Assert.That(difference.Differences, Is.Empty);
            Assert.That(difference.Differences.Any(item =>
                item.SourceIdentifier == "_unrelated"), Is.False);
        });
    }

    [Test]
    public void ExactMessageDiffClassifiesAllStructuralChangeKinds()
    {
        RepositorySnapshot source = CreateMessageSnapshot("10", true);
        RepositorySnapshot target = CreateMessageSnapshot("11", true);
        AddDetailedMessageGraph(source, false);
        AddDetailedMessageGraph(target, true);

        MessageVersionComparer comparer = new MessageVersionComparer();
        RepositoryDiff first = comparer.CompareMessageVersions(
            Request(source, target, "10", "11"));
        RepositoryDiff second = comparer.CompareMessageVersions(
            Request(source, target, "10", "11"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Equivalent, Is.False);
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Renamed && item.SourceIdentifier == "_renamed"));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Moved && item.SourceIdentifier == "_moved"));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Removed && item.SourceIdentifier == "_removed"));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Added && item.TargetIdentifier == "_added"));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.TypeChanged && item.SourceIdentifier == "_changed"));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.CardinalityChanged &&
                item.SourceIdentifier == "_changed"));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.FacetChanged && item.SourceIdentifier == "_typeA"));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.CodeChanged));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.CodeRemoved));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.CodeAdded));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.ConstraintChanged));
            Assert.That(DifferenceKeys(first), Is.EqualTo(DifferenceKeys(second)));
            Assert.That(first.Differences.All(item =>
                item.SourceIdentifier != "_unrelated" &&
                item.TargetIdentifier != "_unrelated"), Is.True);
        });
    }

    [Test]
    public void ScopedOutputLimitsExcludeUnrelatedDifferencesBeforeTruncation()
    {
        RepositorySnapshot source = CreateMessageSnapshot("10", true);
        RepositorySnapshot target = CreateMessageSnapshot("10", true);
        AddSimpleMessageGraph(source, false);
        AddSimpleMessageGraph(target, false);
        int unrelatedIndex = 0;
        while (unrelatedIndex < 10)
        {
            string identifier = "_unrelated" + unrelatedIndex;
            source.BusinessComponents.Add(new BusinessComponentDefinition
            {
                Identifier = identifier,
                Name = identifier,
                Definition = "source"
            });
            target.BusinessComponents.Add(new BusinessComponentDefinition
            {
                Identifier = identifier,
                Name = identifier,
                Definition = "target"
            });
            unrelatedIndex++;
        }

        source.MessageElements[0].Multiplicity.MinimumOccurs = 0;
        target.MessageElements[0].Multiplicity.MinimumOccurs = 1;
        RepositoryComparisonRequest request = Request(source, target, "10", "10");
        request.Limits.MaxCollectionCount = 20;

        RepositoryDiff difference = new MessageVersionComparer().CompareMessageVersions(request);

        Assert.Multiple(() =>
        {
            Assert.That(difference.Success, Is.True);
            Assert.That(difference.Truncated, Is.False);
            Assert.That(difference.Differences, Has.Count.EqualTo(1));
            Assert.That(difference.Differences[0].Kind,
                Is.EqualTo(DifferenceKind.CardinalityChanged));
        });
    }

    [Test]
    public void ExactSelectionReturnsStableValidationAndLookupFailures()
    {
        MessageVersionComparer comparer = new MessageVersionComparer();
        RepositorySnapshot source = CreateMessageSnapshot("10", false);
        RepositorySnapshot target = CreateMessageSnapshot("11", false);
        RepositoryDiff missingRequest = comparer.CompareMessageVersions(null);
        RepositoryDiff missingIdentifier = comparer.CompareMessageVersions(
            new RepositoryComparisonRequest { Source = source, Target = target });
        RepositoryComparisonRequest invalidRequest = Request(source, target, "10", "11");
        invalidRequest.SourceMessageIdentifier = new MessageIdentifier
        {
            BusinessArea = "BAD",
            MessageFunctionality = "8",
            Variant = "1",
            Version = "x"
        };
        RepositoryDiff invalid = comparer.CompareMessageVersions(invalidRequest);
        RepositoryComparisonRequest notFoundRequest = Request(source, target, "09", "11");
        RepositoryDiff notFound = comparer.CompareMessageVersions(notFoundRequest);
        source.MessageDefinitions.Add(MessageDefinition("_duplicate", "10"));
        RepositoryDiff ambiguous = comparer.CompareMessageVersions(
            Request(source, target, "10", "11"));

        Assert.Multiple(() =>
        {
            Assert.That(missingRequest.ErrorCode, Is.EqualTo("comparison.request_missing"));
            Assert.That(missingIdentifier.ErrorCode,
                Is.EqualTo("comparison.source_message_identifier_required"));
            Assert.That(invalid.ErrorCode,
                Is.EqualTo("comparison.source_message_identifier_invalid"));
            Assert.That(notFound.ErrorCode,
                Is.EqualTo("comparison.source_message_not_found"));
            Assert.That(ambiguous.ErrorCode,
                Is.EqualTo("comparison.source_message_ambiguous"));
        });
    }

    [Test]
    public void ExactSelectionReportsEveryRepositoryTargetAndMatchingBoundary()
    {
        MessageVersionComparer comparer = new MessageVersionComparer();
        RepositorySnapshot source = CreateMessageSnapshot("10", false);
        RepositorySnapshot target = CreateMessageSnapshot("11", false);

        RepositoryDiff bothRepositoriesMissing = comparer.CompareMessageVersions(
            new RepositoryComparisonRequest());
        RepositoryDiff targetRepositoryMissing = comparer.CompareMessageVersions(
            new RepositoryComparisonRequest { Source = source });
        RepositoryComparisonRequest missingTargetIdentifier = new RepositoryComparisonRequest
        {
            Source = source,
            Target = target,
            SourceMessageIdentifier = Identifier("10")
        };
        RepositoryDiff targetIdentifierMissing = comparer.CompareMessageVersions(
            missingTargetIdentifier);
        RepositoryComparisonRequest targetNotFoundRequest = Request(
            source,
            target,
            "10",
            "09");
        RepositoryDiff targetNotFound = comparer.CompareMessageVersions(
            targetNotFoundRequest);
        RepositoryComparisonRequest invalidConfidenceRequest = Request(
            source,
            target,
            "10",
            "11");
        invalidConfidenceRequest.MinimumMatchConfidence = 2m;
        RepositoryDiff invalidConfidence = comparer.CompareMessageVersions(
            invalidConfidenceRequest);

        Assert.Multiple(() =>
        {
            Assert.That(bothRepositoriesMissing.ErrorCode,
                Is.EqualTo("comparison.repository_required"));
            Assert.That(targetRepositoryMissing.SourceEdition, Is.EqualTo("edition-10"));
            Assert.That(targetRepositoryMissing.TargetEdition, Is.Empty);
            Assert.That(targetIdentifierMissing.ErrorCode,
                Is.EqualTo("comparison.target_message_identifier_required"));
            Assert.That(targetNotFound.ErrorCode,
                Is.EqualTo("comparison.target_message_not_found"));
            Assert.That(invalidConfidence.ErrorCode,
                Is.EqualTo("comparison.confidence_invalid"));
        });
    }

    private static void AddSimpleMessageGraph(RepositorySnapshot snapshot, bool target)
    {
        MessageDefinitionMetadata message = snapshot.MessageDefinitions[0];
        MessageBuildingBlockDefinition block = Block("_block", "_component", message.Identifier);
        MessageComponentDefinition component = Component("_component", "Component", snapshot);
        MessageElementDefinition element = Element("_element", "Element", component.Identifier);
        component.MessageElements.Add(Reference(
            component.Identifier,
            RepositoryEntityKind.MessageComponent,
            "messageElements",
            element.Identifier,
            RepositoryEntityKind.MessageElement,
            0));
        message.BuildingBlocks.Add(Reference(
            message.Identifier,
            RepositoryEntityKind.MessageDefinition,
            "buildingBlocks",
            block.Identifier,
            RepositoryEntityKind.MessageBuildingBlock,
            0));
        snapshot.MessageBuildingBlocks.Add(block);
        snapshot.MessageComponents.Add(component);
        snapshot.MessageElements.Add(element);
    }

    private static void AddDetailedMessageGraph(RepositorySnapshot snapshot, bool target)
    {
        MessageDefinitionMetadata message = snapshot.MessageDefinitions[0];
        MessageBuildingBlockDefinition blockA = Block("_blockA", "_componentA", message.Identifier);
        MessageBuildingBlockDefinition blockB = Block("_blockB", "_componentB", message.Identifier);
        message.BuildingBlocks.Add(Reference(
            message.Identifier,
            RepositoryEntityKind.MessageDefinition,
            "buildingBlocks",
            blockA.Identifier,
            RepositoryEntityKind.MessageBuildingBlock,
            0));
        message.BuildingBlocks.Add(Reference(
            message.Identifier,
            RepositoryEntityKind.MessageDefinition,
            "buildingBlocks",
            blockB.Identifier,
            RepositoryEntityKind.MessageBuildingBlock,
            1));
        snapshot.MessageBuildingBlocks.Add(blockA);
        snapshot.MessageBuildingBlocks.Add(blockB);

        MessageComponentDefinition componentA = Component("_componentA", "ComponentA", snapshot);
        MessageComponentDefinition componentB = Component("_componentB", "ComponentB", snapshot);
        MessageElementDefinition renamed = Element(
            "_renamed",
            target ? "NewName" : "OldName",
            componentA.Identifier);
        MessageElementDefinition moved = Element(
            "_moved",
            "Moved",
            target ? componentB.Identifier : componentA.Identifier);
        MessageElementDefinition changed = Element("_changed", "Changed", componentA.Identifier);
        changed.MemberType = Reference(
            changed.Identifier,
            RepositoryEntityKind.MessageElement,
            "memberType",
            target ? "_typeB" : "_typeA",
            RepositoryEntityKind.DataType,
            0);
        changed.Multiplicity.MinimumOccurs = target ? 1 : 0;
        MessageElementDefinition codeElement = Element("_codeElement", "Status", componentA.Identifier);
        codeElement.MemberType = Reference(
            codeElement.Identifier,
            RepositoryEntityKind.MessageElement,
            "memberType",
            "_codeSet",
            RepositoryEntityKind.CodeSet,
            0);
        AddElementReference(componentA, renamed, 0);
        if (!target)
        {
            AddElementReference(componentA, moved, 1);
        }

        AddElementReference(componentA, changed, 2);
        AddElementReference(componentA, codeElement, 3);
        if (target)
        {
            AddElementReference(componentB, moved, 0);
        }

        MessageElementDefinition exclusive = Element(
            target ? "_added" : "_removed",
            target ? "Added" : "Removed",
            componentA.Identifier);
        AddElementReference(componentA, exclusive, 4);
        snapshot.MessageComponents.Add(componentA);
        snapshot.MessageComponents.Add(componentB);
        snapshot.MessageElements.Add(renamed);
        snapshot.MessageElements.Add(moved);
        snapshot.MessageElements.Add(changed);
        snapshot.MessageElements.Add(codeElement);
        snapshot.MessageElements.Add(exclusive);

        DataTypeDefinition typeA = new DataTypeDefinition
        {
            Identifier = "_typeA",
            Name = "TypeA",
            Kind = DataTypeKind.Text,
            DataDictionary = snapshot.DataDictionary
        };
        typeA.Facets.Add(new DataTypeFacet
        {
            Name = "maxLength",
            Value = target ? "35" : "20"
        });
        snapshot.DataTypes.Add(typeA);
        snapshot.DataTypes.Add(new DataTypeDefinition
        {
            Identifier = "_typeB",
            Name = "TypeB",
            Kind = DataTypeKind.Decimal,
            DataDictionary = snapshot.DataDictionary
        });

        CodeSetDefinition codeSet = new CodeSetDefinition
        {
            Identifier = "_codeSet",
            Name = "Status",
            DataDictionary = snapshot.DataDictionary
        };
        codeSet.Codes.Add(Code("_changedCode", "Changed", target ? "B" : "A", codeSet.Identifier));
        codeSet.Codes.Add(Code(
            target ? "_addedCode" : "_removedCode",
            target ? "Added" : "Removed",
            target ? "NEW" : "OLD",
            codeSet.Identifier));
        snapshot.CodeSets.Add(codeSet);

        componentA.Registration.Constraints.Add(new ConstraintDefinition
        {
            Identifier = "_constraint",
            Name = "MessageRule",
            ExpressionLanguage = "OCL",
            Expression = target ? "new expression" : "old expression"
        });
        snapshot.BusinessComponents.Add(new BusinessComponentDefinition
        {
            Identifier = "_unrelated",
            Name = "Unrelated",
            Definition = target ? "target" : "source"
        });
    }

    private static void AddElementReference(
        MessageComponentDefinition component,
        MessageElementDefinition element,
        int position)
    {
        component.MessageElements.Add(Reference(
            component.Identifier,
            RepositoryEntityKind.MessageComponent,
            "messageElements",
            element.Identifier,
            RepositoryEntityKind.MessageElement,
            position));
    }

    private static RepositorySnapshot CreateMessageSnapshot(string version, bool sharedIdentifiers)
    {
        string suffix = sharedIdentifiers ? string.Empty : version;
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Identifier = "_repository" + suffix,
            Edition = "edition-" + version,
            DataDictionary = Reference(
                "_repository" + suffix,
                RepositoryEntityKind.Repository,
                "dataDictionary",
                "_dictionary" + suffix,
                RepositoryEntityKind.DataDictionary,
                0),
            BusinessProcessCatalogue = Reference(
                "_repository" + suffix,
                RepositoryEntityKind.Repository,
                "businessProcessCatalogue",
                "_catalogue" + suffix,
                RepositoryEntityKind.BusinessProcessCatalogue,
                0)
        };
        snapshot.MessageDefinitions.Add(MessageDefinition("_message" + suffix, version));
        return snapshot;
    }

    private static MessageDefinitionMetadata MessageDefinition(string identifier, string version)
    {
        return new MessageDefinitionMetadata
        {
            Identifier = identifier,
            Name = "CustomerCreditTransfer",
            XmlName = "CustomerCreditTransferInitiation",
            MessageIdentifier = Identifier(version)
        };
    }

    private static MessageIdentifier Identifier(string version)
    {
        return new MessageIdentifier
        {
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = version,
            Value = "pacs.008.001." + version
        };
    }

    private static RepositoryComparisonRequest Request(
        RepositorySnapshot source,
        RepositorySnapshot target,
        string sourceVersion,
        string targetVersion)
    {
        return new RepositoryComparisonRequest
        {
            Source = source,
            Target = target,
            SourceMessageIdentifier = Identifier(sourceVersion),
            TargetMessageIdentifier = Identifier(targetVersion)
        };
    }

    private static MessageBuildingBlockDefinition Block(
        string identifier,
        string componentIdentifier,
        string messageIdentifier)
    {
        return new MessageBuildingBlockDefinition
        {
            Identifier = identifier,
            Name = identifier,
            MessageDefinition = Reference(
                identifier,
                RepositoryEntityKind.MessageBuildingBlock,
                "messageDefinition",
                messageIdentifier,
                RepositoryEntityKind.MessageDefinition,
                0),
            ComplexType = Reference(
                identifier,
                RepositoryEntityKind.MessageBuildingBlock,
                "complexType",
                componentIdentifier,
                RepositoryEntityKind.MessageComponent,
                0)
        };
    }

    private static MessageComponentDefinition Component(
        string identifier,
        string name,
        RepositorySnapshot snapshot)
    {
        return new MessageComponentDefinition
        {
            Identifier = identifier,
            Name = name,
            DataDictionary = snapshot.DataDictionary
        };
    }

    private static MessageElementDefinition Element(
        string identifier,
        string name,
        string componentIdentifier)
    {
        return new MessageElementDefinition
        {
            Identifier = identifier,
            Name = name,
            ComponentContext = Reference(
                identifier,
                RepositoryEntityKind.MessageElement,
                "componentContext",
                componentIdentifier,
                RepositoryEntityKind.MessageComponent,
                0)
        };
    }

    private static CodeValueDefinition Code(
        string identifier,
        string name,
        string value,
        string codeSetIdentifier)
    {
        return new CodeValueDefinition
        {
            Identifier = identifier,
            Name = name,
            Value = value,
            Owner = Reference(
                identifier,
                RepositoryEntityKind.CodeValue,
                "owner",
                codeSetIdentifier,
                RepositoryEntityKind.CodeSet,
                0)
        };
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

    private static string[] DifferenceKeys(RepositoryDiff difference)
    {
        return difference.Differences.Select(item =>
            ((int)item.Kind) + "|" + item.SourceIdentifier + "|" +
            item.TargetIdentifier + "|" + item.PropertyName + "|" +
            item.SourceValue + "|" + item.TargetValue).ToArray();
    }
}
