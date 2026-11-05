using System.Text;
using iso20022;

namespace iso20022.Tests;

public class RepositoryUnknownContentImporterTests
{
    [Test]
    public void PassTwoRetainsRawFutureTypeNamespaceQualifiedAttributesAndText()
    {
        XmiPassTwoResult graph = BuildGraph();
        XmiPassTwoEntity extension = graph.Entities.Single(
            entity => entity.Identifier == "_extension");
        XmiPassTwoEntity detail = graph.Entities.Single(
            entity => entity.ElementName.LocalName == "detail");
        XmiPassTwoAttribute flag = extension.Attributes.Single(
            attribute => attribute.LocalName == "flag");

        Assert.Multiple(() =>
        {
            Assert.That(extension.TypeName, Is.EqualTo("FutureClassifier"));
            Assert.That(extension.SourceTypeName, Is.EqualTo("future:FutureClassifier"));
            Assert.That(extension.SourceTypeNamespaceUri, Is.EqualTo("urn:example:future"));
            Assert.That(extension.Kind, Is.EqualTo(RepositoryEntityKind.Unknown));
            Assert.That(extension.ElementName.NamespaceUri, Is.EqualTo("urn:example:future"));
            Assert.That(extension.TextValue, Is.EqualTo("PrefixSuffix"));
            Assert.That(flag.NamespaceUri, Is.EqualTo("urn:example:future"));
            Assert.That(flag.Prefix, Is.EqualTo("future"));
            Assert.That(flag.Value, Is.EqualTo("yes"));
            Assert.That(detail.TypeName, Is.Empty);
            Assert.That(detail.TextValue, Is.EqualTo("Payload"));
        });
    }

    [Test]
    public void ImporterBuildsOrderedUnknownTreesAndAttachesThemToKnownOwners()
    {
        XmiPassTwoResult graph = BuildGraph();
        RepositorySnapshot snapshot = DataDictionaryImporter.ImportDataDictionary(graph);
        RepositoryUnknownContentImporter.PopulateUnknownContent(graph, snapshot);
        BusinessComponentDefinition component = snapshot.BusinessComponents.Single(
            item => item.Identifier == "_known");
        RepositoryUnknownData extension = snapshot.UnknownContent.Single(
            item => item.Identifier == "_extension");
        RepositoryUnknownData knownSlot = snapshot.UnknownContent.Single(
            item => item.Name.LocalName == "knownSlot");
        RepositoryUnknownData detail = extension.Children.Single(
            item => item.Name.LocalName == "detail");

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.UnknownContent, Has.Count.EqualTo(4));
            Assert.That(component.UnknownContent, Has.Count.EqualTo(4));
            Assert.That(component.UnknownContent, Is.EqualTo(snapshot.UnknownContent));
            Assert.That(snapshot.UnknownContent.Select(item => item.Name.LocalName),
                Is.EqualTo(new[] { "superType", "futureFlag", "extension", "knownSlot" }));
            Assert.That(snapshot.UnknownContent[0].IsAttribute, Is.True);
            Assert.That(snapshot.UnknownContent[0].Name.NamespaceUri,
                Is.EqualTo("urn:example:future"));
            Assert.That(snapshot.UnknownContent[0].Value, Is.EqualTo("_notAReference"));
            Assert.That(snapshot.UnknownContent[1].Value, Is.EqualTo("future-value"));
            Assert.That(extension.OwnerIdentifier, Is.EqualTo("_known"));
            Assert.That(extension.OwnerKind, Is.EqualTo(RepositoryEntityKind.BusinessComponent));
            Assert.That(extension.TypeName, Is.EqualTo("future:FutureClassifier"));
            Assert.That(extension.Value, Is.EqualTo("PrefixSuffix"));
            Assert.That(extension.Children.Select(item => item.Name.LocalName),
                Is.EqualTo(new[] { "flag", "customValue", "superType", "detail" }));
            Assert.That(detail.OwnerIdentifier, Is.EqualTo("_extension"));
            Assert.That(detail.Value, Is.EqualTo("Payload"));
            Assert.That(detail.Children.Single().Name.LocalName, Is.EqualTo("level"));
            Assert.That(extension.RawContent?.RootName?.Prefix, Is.EqualTo("future"));
            Assert.That(extension.RawContent?.NamespaceDeclarations.Single(
                    declaration => declaration.Prefix == "future").NamespaceUri,
                Is.EqualTo("urn:example:future"));
            Assert.That(knownSlot.TypeName, Is.EqualTo("iso:Text"));
            Assert.That(knownSlot.RawContent?.NamespaceDeclarations.Single(
                    declaration => declaration.Prefix == "iso").NamespaceUri,
                Is.EqualTo(Iso20022Namespaces.Ecore2013));
        });
    }

    [Test]
    public void ImporterPreservesResolvedAndUnresolvedUnknownReferencesWithDiagnostics()
    {
        RepositorySnapshot snapshot =
            RepositoryUnknownContentImporter.ImportUnknownContent(BuildGraph());
        RepositoryUnknownData extension = snapshot.UnknownContent.Single(
            item => item.Identifier == "_extension");
        RepositoryReference resolved = extension.References[0];
        RepositoryReference missingInside = extension.References[1];
        RepositoryReference missing = snapshot.References.Single(
            reference => reference.TargetIdentifier == "_missing");
        RepositoryReference ambiguous = snapshot.References.Single(
            reference => reference.TargetIdentifier == "_ambiguous");

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.References, Has.Count.EqualTo(4));
            Assert.That(snapshot.References.Count(reference => !reference.IsResolved),
                Is.EqualTo(3));
            Assert.That(resolved.TargetIdentifier, Is.EqualTo("_target"));
            Assert.That(resolved.IsResolved, Is.True);
            Assert.That(missingInside.TargetIdentifier, Is.EqualTo("_insideMissing"));
            Assert.That(missingInside.ErrorCode,
                Is.EqualTo("repository.xmi.reference_missing"));
            Assert.That(missingInside.ErrorMessage,
                Is.EqualTo("The XMI reference target is not declared in the repository artifact."));
            Assert.That(missing.ErrorCode, Is.EqualTo("repository.xmi.reference_missing"));
            Assert.That(ambiguous.ErrorCode,
                Is.EqualTo("repository.xmi.reference_ambiguous"));
            Assert.That(ambiguous.TargetKind,
                Is.EqualTo(RepositoryEntityKind.BusinessComponent));
            Assert.That(snapshot.References.Any(reference =>
                reference.TargetIdentifier == "_notAReference"), Is.False);
        });
    }

    [Test]
    public void ImporterPreservesUnknownContentAndDiagnosticOrderingAcrossRuns()
    {
        RepositorySnapshot first =
            RepositoryUnknownContentImporter.ImportUnknownContent(BuildGraph());
        RepositorySnapshot second =
            RepositoryUnknownContentImporter.ImportUnknownContent(BuildGraph());

        Assert.Multiple(() =>
        {
            Assert.That(first.UnknownContent.Select(UnknownKey),
                Is.EqualTo(second.UnknownContent.Select(UnknownKey)));
            Assert.That(first.References.Select(ReferenceKey),
                Is.EqualTo(second.References.Select(ReferenceKey)));
        });
    }

    private static string UnknownKey(RepositoryUnknownData unknown)
    {
        return unknown.OwnerIdentifier + "|" + unknown.TypeName + "|" +
            unknown.Name.NamespaceUri + "|" + unknown.Name.LocalName + "|" + unknown.Value;
    }

    private static string ReferenceKey(RepositoryReference reference)
    {
        return reference.SourceIdentifier + "|" + reference.PropertyName + "|" +
            reference.Position + "|" + reference.TargetIdentifier + "|" + reference.ErrorCode;
    }

    private static XmiPassTwoResult BuildGraph()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(CreateFixture());
        using MemoryStream source = new MemoryStream(bytes);
        Iso20022Ecore2013Adapter adapter = new Iso20022Ecore2013Adapter();
        XmiPassOneResult passOne = XmiPassOneReader.BuildIndex(
            source,
            "unknown-content.iso20022",
            adapter,
            new XmiPassOneOptions());
        return XmiPassTwoReader.BuildGraph(
            source,
            "unknown-content.iso20022",
            adapter,
            passOne,
            new XmiPassTwoOptions());
    }

    private static string CreateFixture()
    {
        return """
            <?xml version='1.0'?>
            <iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore'
                xmlns:future='urn:example:future'
                xmlns:xmi='http://www.omg.org/XMI'
                xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmi:id='_repository'>
              <dataDictionary xmi:id='_dictionary'>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_known'
                    name='KnownComponent' superType='_missing _ambiguous'
                    future:superType='_notAReference' futureFlag='future-value'>
                  <future:extension xsi:type='future:FutureClassifier' xmi:id='_extension'
                      future:flag='yes' customValue='alpha'
                      superType='_target _insideMissing'>Prefix<future:detail
                        future:level='2'>Payload</future:detail>Suffix</future:extension>
                  <future:knownSlot xsi:type='iso:Text' xmi:id='_knownSlot'
                      name='KnownTypeInUnknownFeature' future:mode='extended'/>
                </topLevelDictionaryEntry>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_target'
                    name='Target'/>
                <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_ambiguous'
                    name='FirstAmbiguous'/>
                <topLevelDictionaryEntry xsi:type='iso:Text' xmi:id='_ambiguous'
                    name='SecondAmbiguous'/>
              </dataDictionary>
              <businessProcessCatalogue xmi:id='_catalogue'/>
            </iso:Repository>
            """;
    }
}
