using iso20022;

namespace iso20022.Tests;

public class RepositoryComparerTests
{
    [Test]
    public void EquivalentGraphsRemainEquivalentAcrossChangedXmiIdentities()
    {
        RepositorySnapshot source = CreateSnapshot("source", false);
        RepositorySnapshot target = CreateSnapshot("target", false);
        MessageComponentDefinition sourceComponent = MessageComponent(
            "_sourceComponent",
            "Payment",
            source);
        MessageComponentDefinition targetComponent = MessageComponent(
            "_targetComponent",
            "Payment",
            target);
        source.MessageComponents.Add(sourceComponent);
        target.MessageComponents.Add(targetComponent);
        MessageElementDefinition sourceElement = MessageElement(
            "_sourceElement",
            "Amount",
            sourceComponent.Identifier);
        MessageElementDefinition targetElement = MessageElement(
            "_targetElement",
            "Amount",
            targetComponent.Identifier);
        source.MessageElements.Add(sourceElement);
        target.MessageElements.Add(targetElement);
        source.References.Add(Reference(
            sourceComponent.Identifier,
            RepositoryEntityKind.MessageComponent,
            "messageElements",
            sourceElement.Identifier,
            RepositoryEntityKind.MessageElement,
            0));
        target.References.Add(Reference(
            targetComponent.Identifier,
            RepositoryEntityKind.MessageComponent,
            "messageElements",
            targetElement.Identifier,
            RepositoryEntityKind.MessageElement,
            0));

        RepositoryDiff difference = new RepositoryComparer().CompareRepositories(
            Request(source, target));

        Assert.Multiple(() =>
        {
            Assert.That(difference.Success, Is.True);
            Assert.That(difference.Equivalent, Is.True);
            Assert.That(difference.Differences, Is.Empty);
            Assert.That(difference.SourceEdition, Is.EqualTo("source"));
            Assert.That(difference.TargetEdition, Is.EqualTo("target"));
        });
    }

    [Test]
    public void GraphDiffClassifiesEveryRepositoryChangeCategory()
    {
        RepositorySnapshot source = CreateSnapshot("source", true);
        RepositorySnapshot target = CreateSnapshot("target", true);
        AddComprehensiveEntities(source, false);
        AddComprehensiveEntities(target, true);

        RepositoryComparer comparer = new RepositoryComparer();
        RepositoryDiff first = comparer.CompareRepositories(Request(source, target));
        RepositoryDiff second = comparer.CompareRepositories(Request(source, target));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Equivalent, Is.False);
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.RegistrationChanged &&
                item.EntityKind == RepositoryEntityKind.BusinessArea));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Modified &&
                item.EntityKind == RepositoryEntityKind.BusinessComponent));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.CardinalityChanged));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.TypeChanged));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.FacetChanged));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.CodeChanged));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.CodeRemoved));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.CodeAdded));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.ConstraintChanged));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.ConstraintAdded));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.ConstraintRemoved));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.SyntaxChanged));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.TransportChanged));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.RelationshipChanged));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.RelationshipAdded));
            Assert.That(first.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.RelationshipRemoved));
            Assert.That(DifferenceKeys(first), Is.EqualTo(DifferenceKeys(second)));
        });
    }

    [Test]
    public void DiffReportsRenameMoveAdditionRemovalAndAmbiguityWithoutDuplicateAdds()
    {
        RepositorySnapshot source = CreateSnapshot("source", true);
        RepositorySnapshot target = CreateSnapshot("target", true);
        source.BusinessComponents.Add(Component("_rename", "OldName", source));
        target.BusinessComponents.Add(Component("_rename", "NewName", target));
        source.BusinessComponents.Add(Component("_ownerA", "OwnerA", source));
        source.BusinessComponents.Add(Component("_ownerB", "OwnerB", source));
        target.BusinessComponents.Add(Component("_ownerA", "OwnerA", target));
        target.BusinessComponents.Add(Component("_ownerB", "OwnerB", target));
        source.BusinessElements.Add(BusinessElement("_moved", "Member", "_ownerA"));
        target.BusinessElements.Add(BusinessElement("_moved", "Member", "_ownerB"));
        source.BusinessComponents.Add(Component("_removed", "RemovedOnly", source));
        target.BusinessComponents.Add(Component("_added", "AddedOnly", target));
        source.BusinessComponents.Add(Component("_ambiguousSource", "Shared", source));
        target.BusinessComponents.Add(Component("_ambiguousFirst", "Shared", target));
        target.BusinessComponents.Add(Component("_ambiguousSecond", "Shared", target));

        RepositoryDiff difference = new RepositoryComparer().CompareRepositories(
            Request(source, target));
        DifferenceItem ambiguity = difference.Differences.Single(item =>
            item.Kind == DifferenceKind.AmbiguousMatch);

        Assert.Multiple(() =>
        {
            Assert.That(difference.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Renamed && item.SourceIdentifier == "_rename"));
            Assert.That(difference.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Moved && item.SourceIdentifier == "_moved"));
            Assert.That(difference.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Removed && item.SourceIdentifier == "_removed"));
            Assert.That(difference.Differences, Has.Some.Matches<DifferenceItem>(item =>
                item.Kind == DifferenceKind.Added && item.TargetIdentifier == "_added"));
            Assert.That(difference.HasAmbiguousMatches, Is.True);
            Assert.That(ambiguity.MatchConfidence, Is.EqualTo(0.95m));
            Assert.That(ambiguity.AlternativeTargetIdentifiers,
                Is.EqualTo(new[] { "_ambiguousFirst", "_ambiguousSecond" }));
            Assert.That(difference.Differences.Any(item =>
                item.Kind == DifferenceKind.Added &&
                ambiguity.AlternativeTargetIdentifiers.Contains(item.TargetIdentifier)),
                Is.False);
        });
    }

    [Test]
    public void IncludeUnchangedRetainsMatchesWithoutChangingEquivalence()
    {
        RepositorySnapshot source = CreateSnapshot("same", true);
        RepositorySnapshot target = CreateSnapshot("same", true);
        source.BusinessComponents.Add(Component("_component", "Account", source));
        target.BusinessComponents.Add(Component("_component", "Account", target));
        RepositoryComparisonRequest request = Request(source, target);
        request.IncludeUnchanged = true;

        RepositoryDiff difference = new RepositoryComparer().CompareRepositories(request);

        Assert.Multiple(() =>
        {
            Assert.That(difference.Success, Is.True);
            Assert.That(difference.Equivalent, Is.True);
            Assert.That(difference.Differences, Is.Not.Empty);
            Assert.That(difference.Differences.All(item => item.Kind == DifferenceKind.Unchanged),
                Is.True);
        });
    }

    [Test]
    public void DifferenceOutputIsBoundedAndReportsTruncation()
    {
        RepositorySnapshot source = CreateSnapshot("same", true);
        RepositorySnapshot target = CreateSnapshot("same", true);
        BusinessElementDefinition sourceElement = BusinessElement(
            "_element",
            "OldName",
            "_ownerOne");
        sourceElement.Definition = "old definition";
        sourceElement.Kind = MessageElementKind.Attribute;
        sourceElement.Multiplicity.MinimumOccurs = 0;
        sourceElement.Aggregation = "none";
        BusinessElementDefinition targetElement = BusinessElement(
            "_element",
            "NewName",
            "_ownerTwo");
        targetElement.Definition = "new definition";
        targetElement.Kind = MessageElementKind.Attribute;
        targetElement.Multiplicity.MinimumOccurs = 1;
        targetElement.Aggregation = "shared";
        sourceElement.MemberType = Reference(
            sourceElement.Identifier,
            RepositoryEntityKind.BusinessAttribute,
            "memberType",
            "_typeOne",
            RepositoryEntityKind.DataType,
            0);
        targetElement.MemberType = Reference(
            targetElement.Identifier,
            RepositoryEntityKind.BusinessAttribute,
            "memberType",
            "_typeTwo",
            RepositoryEntityKind.DataType,
            0);
        source.BusinessElements.Add(sourceElement);
        target.BusinessElements.Add(targetElement);
        RepositoryComparisonRequest request = Request(source, target);
        request.Limits.MaxCollectionCount = 4;

        RepositoryDiff difference = new RepositoryComparer().CompareRepositories(request);

        Assert.Multiple(() =>
        {
            Assert.That(difference.Success, Is.True);
            Assert.That(difference.Equivalent, Is.False);
            Assert.That(difference.Truncated, Is.True);
            Assert.That(difference.Differences, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void PublicComparerReturnsStableRequestAndMatchingFailures()
    {
        RepositoryComparer comparer = new RepositoryComparer();
        RepositoryDiff missingRequest = comparer.CompareRepositories(null);
        RepositoryDiff missingRepository = comparer.CompareRepositories(
            new RepositoryComparisonRequest());
        RepositoryComparisonRequest invalidConfidenceRequest = Request(
            CreateSnapshot("source", true),
            CreateSnapshot("target", true));
        invalidConfidenceRequest.MinimumMatchConfidence = 2m;
        RepositoryDiff invalidConfidence = comparer.CompareRepositories(invalidConfidenceRequest);

        Assert.Multiple(() =>
        {
            Assert.That(missingRequest.Success, Is.False);
            Assert.That(missingRequest.ErrorCode, Is.EqualTo("comparison.request_missing"));
            Assert.That(missingRepository.ErrorCode,
                Is.EqualTo("comparison.repository_required"));
            Assert.That(invalidConfidence.ErrorCode,
                Is.EqualTo("comparison.confidence_invalid"));
        });
    }

    private static void AddComprehensiveEntities(RepositorySnapshot snapshot, bool target)
    {
        BusinessAreaDefinition area = new BusinessAreaDefinition
        {
            Identifier = "_area",
            Name = "Payments",
            Code = "pacs",
            BusinessProcessCatalogue = snapshot.BusinessProcessCatalogue,
            Registration = new RegistrationMetadata
            {
                Status = target ? RegistrationStatus.Obsolete : RegistrationStatus.Registered
            }
        };
        snapshot.BusinessAreas.Add(area);

        BusinessComponentDefinition component = Component("_business", "Account", snapshot);
        component.Definition = target ? "target business definition" : "source business definition";
        if (target)
        {
            component.Registration.Constraints.Add(new ConstraintDefinition
            {
                Identifier = "_ownedConstraint",
                Name = "OwnedRule",
                Expression = "value"
            });
        }

        snapshot.BusinessComponents.Add(component);
        snapshot.BusinessComponents.Add(Component("_ownerA", "OwnerA", snapshot));
        snapshot.BusinessComponents.Add(Component("_ownerB", "OwnerB", snapshot));
        BusinessElementDefinition element = BusinessElement("_element", "Balance", "_ownerA");
        element.Kind = target ? MessageElementKind.AssociationEnd : MessageElementKind.Attribute;
        element.Multiplicity.MinimumOccurs = target ? 1 : 0;
        snapshot.BusinessElements.Add(element);

        DataTypeDefinition firstType = new DataTypeDefinition
        {
            Identifier = "_typeA",
            Name = "TypeA",
            DataDictionary = snapshot.DataDictionary,
            Kind = DataTypeKind.Text
        };
        firstType.Facets.Add(new DataTypeFacet
        {
            Name = "maxLength",
            Value = target ? "35" : "20"
        });
        snapshot.DataTypes.Add(firstType);
        snapshot.DataTypes.Add(new DataTypeDefinition
        {
            Identifier = "_typeB",
            Name = "TypeB",
            DataDictionary = snapshot.DataDictionary,
            Kind = DataTypeKind.Decimal
        });

        MessageComponentDefinition messageComponent = MessageComponent(
            "_messageComponent",
            "PaymentData",
            snapshot);
        messageComponent.Definition = target ? "target message definition" : "source message definition";
        snapshot.MessageComponents.Add(messageComponent);
        MessageElementDefinition messageElement = MessageElement(
            "_messageElement",
            "Amount",
            messageComponent.Identifier);
        messageElement.MemberType = Reference(
            messageElement.Identifier,
            RepositoryEntityKind.MessageElement,
            "memberType",
            target ? "_typeB" : "_typeA",
            RepositoryEntityKind.DataType,
            0);
        messageElement.Multiplicity.MinimumOccurs = target ? 1 : 0;
        snapshot.MessageElements.Add(messageElement);

        CodeSetDefinition codeSet = new CodeSetDefinition
        {
            Identifier = "_codeSet",
            Name = "Status",
            DataDictionary = snapshot.DataDictionary
        };
        codeSet.Codes.Add(Code("_changedCode", "Changed", target ? "B" : "A", codeSet.Identifier));
        if (target)
        {
            codeSet.Codes.Add(Code("_addedCode", "Added", "NEW", codeSet.Identifier));
        }
        else
        {
            codeSet.Codes.Add(Code("_removedCode", "Removed", "OLD", codeSet.Identifier));
        }

        snapshot.CodeSets.Add(codeSet);
        snapshot.Constraints.Add(new ConstraintDefinition
        {
            Identifier = "_constraint",
            Name = "RepositoryRule",
            ExpressionLanguage = "OCL",
            Expression = target ? "new expression" : "old expression",
            Owner = Reference(
                "_constraint",
                RepositoryEntityKind.Constraint,
                "owner",
                snapshot.Identifier,
                RepositoryEntityKind.Repository,
                0)
        });
        if (!target)
        {
            snapshot.Constraints.Add(new ConstraintDefinition
            {
                Identifier = "_removedConstraint",
                Name = "RemovedRepositoryRule",
                ExpressionLanguage = "OCL",
                Expression = "source only",
                Owner = Reference(
                    "_removedConstraint",
                    RepositoryEntityKind.Constraint,
                    "owner",
                    snapshot.Identifier,
                    RepositoryEntityKind.Repository,
                    0)
            });
        }
        snapshot.Syntaxes.Add(new SyntaxDefinition
        {
            Identifier = "_syntax",
            Name = "XML",
            Definition = target ? "new syntax" : "old syntax"
        });
        snapshot.Transports.Add(new TransportDefinition
        {
            Identifier = "_transport",
            Kind = RepositoryEntityKind.TransportMode,
            Name = "StoreAndForward",
            BusinessProcessCatalogue = snapshot.BusinessProcessCatalogue,
            DeliveryAssurance = target ? "AtLeastOnce" : "BestEffort"
        });

        snapshot.References.Add(Reference(
            messageComponent.Identifier,
            RepositoryEntityKind.MessageComponent,
            "messageElements",
            target ? "_typeB" : "_typeA",
            RepositoryEntityKind.DataType,
            0));
        if (target)
        {
            snapshot.References.Add(Reference(
                messageComponent.Identifier,
                RepositoryEntityKind.MessageComponent,
                "targetOnlyRelationship",
                "_typeB",
                RepositoryEntityKind.DataType,
                0));
        }
        else
        {
            snapshot.References.Add(Reference(
                messageComponent.Identifier,
                RepositoryEntityKind.MessageComponent,
                "sourceOnlyRelationship",
                "_typeA",
                RepositoryEntityKind.DataType,
                0));
        }
    }

    private static string[] DifferenceKeys(RepositoryDiff difference)
    {
        return difference.Differences.Select(item =>
            ((int)item.Kind) + "|" + ((int)item.EntityKind) + "|" +
            item.SourceIdentifier + "|" + item.TargetIdentifier + "|" +
            item.PropertyName + "|" + item.SourceValue + "|" + item.TargetValue).ToArray();
    }

    private static RepositoryComparisonRequest Request(
        RepositorySnapshot source,
        RepositorySnapshot target)
    {
        return new RepositoryComparisonRequest
        {
            Source = source,
            Target = target
        };
    }

    private static RepositorySnapshot CreateSnapshot(string edition, bool sharedIdentifiers)
    {
        string suffix = sharedIdentifiers ? string.Empty : edition;
        RepositorySnapshot snapshot = new RepositorySnapshot
        {
            Identifier = "_repository" + suffix,
            Edition = edition
        };
        snapshot.DataDictionary = Reference(
            snapshot.Identifier,
            RepositoryEntityKind.Repository,
            "dataDictionary",
            "_dictionary" + suffix,
            RepositoryEntityKind.DataDictionary,
            0);
        snapshot.BusinessProcessCatalogue = Reference(
            snapshot.Identifier,
            RepositoryEntityKind.Repository,
            "businessProcessCatalogue",
            "_catalogue" + suffix,
            RepositoryEntityKind.BusinessProcessCatalogue,
            0);
        snapshot.References.Add(snapshot.DataDictionary);
        snapshot.References.Add(snapshot.BusinessProcessCatalogue);
        return snapshot;
    }

    private static BusinessComponentDefinition Component(
        string identifier,
        string name,
        RepositorySnapshot snapshot)
    {
        return new BusinessComponentDefinition
        {
            Identifier = identifier,
            Name = name,
            DataDictionary = Reference(
                identifier,
                RepositoryEntityKind.BusinessComponent,
                "dataDictionary",
                snapshot.DataDictionary.TargetIdentifier,
                RepositoryEntityKind.DataDictionary,
                0)
        };
    }

    private static BusinessElementDefinition BusinessElement(
        string identifier,
        string name,
        string ownerIdentifier)
    {
        return new BusinessElementDefinition
        {
            Identifier = identifier,
            Name = name,
            ElementContext = Reference(
                identifier,
                RepositoryEntityKind.BusinessElement,
                "elementContext",
                ownerIdentifier,
                RepositoryEntityKind.BusinessComponent,
                0)
        };
    }

    private static MessageComponentDefinition MessageComponent(
        string identifier,
        string name,
        RepositorySnapshot snapshot)
    {
        return new MessageComponentDefinition
        {
            Identifier = identifier,
            Name = name,
            DataDictionary = Reference(
                identifier,
                RepositoryEntityKind.MessageComponent,
                "dataDictionary",
                snapshot.DataDictionary.TargetIdentifier,
                RepositoryEntityKind.DataDictionary,
                0)
        };
    }

    private static MessageElementDefinition MessageElement(
        string identifier,
        string name,
        string ownerIdentifier)
    {
        return new MessageElementDefinition
        {
            Identifier = identifier,
            Name = name,
            ComponentContext = Reference(
                identifier,
                RepositoryEntityKind.MessageElement,
                "componentContext",
                ownerIdentifier,
                RepositoryEntityKind.MessageComponent,
                0)
        };
    }

    private static CodeValueDefinition Code(
        string identifier,
        string name,
        string value,
        string ownerIdentifier)
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
                ownerIdentifier,
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
}
