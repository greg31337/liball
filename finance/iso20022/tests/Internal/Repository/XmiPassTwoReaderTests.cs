using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class XmiPassTwoReaderTests
{
    [Test]
    public void PassTwoCapturesAttributesContainmentAndIdlessEntitiesInDocumentOrder()
    {
        (XmiPassOneResult passOne, XmiPassTwoResult passTwo, MemoryStream source) = BuildFixture();
        using (source)
        {
            XmiPassTwoEntity component = passTwo.Entities.Single(
                entity => entity.Identifier == "_component");
            XmiPassTwoEntity markup = passTwo.Entities.Single(
                entity => entity.Identifier == "_markup");
            XmiPassTwoEntity identifier = passTwo.Entities.Single(
                entity => entity.TypeName == "MessageDefinitionIdentifier");
            XmiPassTwoReference identifierContainment = passTwo.Relationships.Single(
                relationship => relationship.IsContainment &&
                    relationship.TargetDocumentOrder == identifier.DocumentOrder);

            Assert.Multiple(() =>
            {
                Assert.That(passTwo.Entities, Has.Count.EqualTo(passOne.ElementCount));
                Assert.That(passTwo.Entities.Select(entity => entity.DocumentOrder),
                    Is.EqualTo(Enumerable.Range(0, passTwo.Entities.Count)));
                Assert.That(component.Kind, Is.EqualTo(RepositoryEntityKind.BusinessComponent));
                Assert.That(component.ParentIdentifier, Is.EqualTo("_dictionary"));
                Assert.That(component.TopLevelPartitionIdentifier, Is.EqualTo("_dictionary"));
                Assert.That(component.Attributes.Select(attribute => attribute.LocalName),
                    Is.EqualTo(new[] { "name", "superType", "example", "example" }));
                Assert.That(component.Attributes[0].IsReference, Is.False);
                Assert.That(component.Attributes[1].IsReference, Is.True);
                Assert.That(component.Attributes.Where(attribute => attribute.LocalName == "example")
                    .Select(attribute => attribute.Value), Is.EqualTo(new[] { "First", "Second" }));
                Assert.That(markup.Attributes.Single(attribute => attribute.LocalName == "type")
                    .IsReference, Is.False);
                Assert.That(identifier.Identifier, Is.Empty);
                Assert.That(identifier.ParentIdentifier, Is.EqualTo("_message"));
                Assert.That(identifier.Attributes.Select(attribute => attribute.LocalName),
                    Is.EqualTo(new[] { "businessArea", "messageFunctionality" }));
                Assert.That(identifier.Attributes.Single(attribute =>
                    attribute.LocalName == "businessArea").IsReference, Is.False);
                Assert.That(identifierContainment.PropertyName,
                    Is.EqualTo("messageDefinitionIdentifier"));
                Assert.That(identifierContainment.TargetIdentifier, Is.Empty);
                Assert.That(identifierContainment.IsResolved, Is.True);
                Assert.That(identifierContainment.SourceIdentifier, Is.EqualTo("_message"));
                Assert.That(passTwo.ContainmentCount,
                    Is.EqualTo(passTwo.Relationships.Count(relationship => relationship.IsContainment)));
                Assert.That(source.Position, Is.EqualTo(0));
                Assert.That(source.CanRead, Is.True);
            });
        }
    }

    [Test]
    public void PassTwoResolvesSpaceDelimitedForwardBackwardMissingAndAmbiguousReferences()
    {
        (XmiPassOneResult _, XmiPassTwoResult result, MemoryStream source) = BuildFixture();
        using (source)
        {
            XmiPassTwoReference[] superTypes = result.Relationships.Where(
                relationship => !relationship.IsContainment &&
                    relationship.SourceIdentifier == "_component" &&
                    relationship.PropertyName == "superType").ToArray();
            XmiPassTwoReference associationType = result.Relationships.Single(
                relationship => relationship.SourceIdentifier == "_association" &&
                    relationship.PropertyName == "type");
            XmiPassTwoReference previousVersion = result.Relationships.Single(
                relationship => relationship.SourceIdentifier == "_later" &&
                    relationship.PropertyName == "previousVersion");

            Assert.Multiple(() =>
            {
                Assert.That(superTypes.Select(reference => reference.TargetIdentifier),
                    Is.EqualTo(new[] { "_later", "_missing", "_duplicate" }));
                Assert.That(superTypes.Select(reference => reference.Position),
                    Is.EqualTo(new[] { 0, 1, 2 }));
                Assert.That(superTypes[0].IsResolved, Is.True);
                Assert.That(superTypes[0].IsForwardReference, Is.True);
                Assert.That(superTypes[0].TargetKind,
                    Is.EqualTo(RepositoryEntityKind.BusinessComponent));
                Assert.That(superTypes[0].RawValue,
                    Is.EqualTo("_later   _missing _duplicate"));
                Assert.That(superTypes[1].IsResolved, Is.False);
                Assert.That(superTypes[1].ErrorCode,
                    Is.EqualTo("repository.xmi.reference_missing"));
                Assert.That(superTypes[2].IsResolved, Is.False);
                Assert.That(superTypes[2].ErrorCode,
                    Is.EqualTo("repository.xmi.reference_ambiguous"));
                Assert.That(superTypes[2].TargetKind,
                    Is.EqualTo(RepositoryEntityKind.BusinessComponent));
                Assert.That(associationType.IsResolved, Is.True);
                Assert.That(associationType.IsForwardReference, Is.True);
                Assert.That(associationType.RawToken, Is.EqualTo("#_later"));
                Assert.That(associationType.TargetIdentifier, Is.EqualTo("_later"));
                Assert.That(previousVersion.IsResolved, Is.True);
                Assert.That(previousVersion.IsBackwardReference, Is.True);
                Assert.That(result.ResolvedReferenceCount, Is.EqualTo(3));
                Assert.That(result.UnresolvedReferenceCount, Is.EqualTo(2));
                Assert.That(result.ForwardReferenceCount, Is.EqualTo(2));
                Assert.That(result.BackwardReferenceCount, Is.EqualTo(1));
                Assert.That(superTypes[0].SourceDocumentOrder,
                    Is.LessThan(superTypes[0].TargetDocumentOrder));
                Assert.That(previousVersion.SourceDocumentOrder,
                    Is.GreaterThan(previousVersion.TargetDocumentOrder));
            });
        }
    }

    [Test]
    public void PassTwoRelationshipOrderingIsDeterministicAcrossRuns()
    {
        (XmiPassOneResult _, XmiPassTwoResult first, MemoryStream firstSource) = BuildFixture();
        (XmiPassOneResult _, XmiPassTwoResult second, MemoryStream secondSource) = BuildFixture();
        using (firstSource)
        using (secondSource)
        {
            string[] firstOrder = first.Relationships.Select(CreateRelationshipSignature).ToArray();
            string[] secondOrder = second.Relationships.Select(CreateRelationshipSignature).ToArray();

            Assert.That(secondOrder, Is.EqualTo(firstOrder));
        }
    }

    [Test]
    public void PassTwoUsesEcoreOwnerTypeToDisambiguateReferenceFeatureNames()
    {
        Iso20022Ecore2013Adapter adapter = new Iso20022Ecore2013Adapter();

        Assert.Multiple(() =>
        {
            Assert.That(adapter.IsReferenceFeature("BusinessAssociationEnd", "type"), Is.True);
            Assert.That(adapter.IsReferenceFeature("SemanticMarkup", "type"), Is.False);
            Assert.That(adapter.IsReferenceFeature("MessageDefinition", "businessArea"), Is.True);
            Assert.That(adapter.IsReferenceFeature("MessageDefinitionIdentifier", "businessArea"),
                Is.False);
            Assert.That(adapter.IsReferenceFeature("CodeSet", "code"), Is.True);
            Assert.That(adapter.IsReferenceFeature("BusinessArea", "code"), Is.False);
            Assert.That(adapter.IsReferenceFeature("BusinessComponent", "derivationElement"), Is.True);
            Assert.That(adapter.IsReferenceFeature("BusinessComponent", "name"), Is.False);
            Assert.That(adapter.IsElementAttributeFeature("BusinessComponent", "example"), Is.True);
            Assert.That(adapter.IsElementAttributeFeature("ExternalSchema", "namespaceList"), Is.True);
            Assert.That(adapter.IsElementAttributeFeature("BusinessComponent", "namespaceList"), Is.False);
        });
    }

    [Test]
    public void PassTwoRejectsInputThatChangedAfterPassOne()
    {
        string original = CreateGraphFixture();
        using MemoryStream first = new MemoryStream(Encoding.UTF8.GetBytes(original));
        XmiPassOneResult passOne = XmiPassOneReader.BuildIndex(
            first,
            "first.iso20022",
            new Iso20022Ecore2013Adapter(),
            new XmiPassOneOptions());
        string changed = original.Replace("xmi:id='_later'", "xmi:id='_changed'", StringComparison.Ordinal);
        using MemoryStream second = new MemoryStream(Encoding.UTF8.GetBytes(changed));

        Assert.Throws<XmlException>(() => XmiPassTwoReader.BuildGraph(
            second,
            "second.iso20022",
            new Iso20022Ecore2013Adapter(),
            passOne,
            new XmiPassTwoOptions()));
    }

    [Test]
    public void PassTwoSupportsForwardOnlyStreamsAndEnforcesGraphBounds()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(CreateGraphFixture());
        using ForwardOnlyGraphStream first = new ForwardOnlyGraphStream(bytes);
        XmiPassOneResult passOne = XmiPassOneReader.BuildIndex(
            first,
            "first-forward.iso20022",
            new Iso20022Ecore2013Adapter(),
            new XmiPassOneOptions());
        using ForwardOnlyGraphStream second = new ForwardOnlyGraphStream(bytes);
        XmiPassTwoResult result = XmiPassTwoReader.BuildGraph(
            second,
            "second-forward.iso20022",
            new Iso20022Ecore2013Adapter(),
            passOne,
            new XmiPassTwoOptions());
        XmiPassTwoOptions attributeLimit = new XmiPassTwoOptions
        {
            MaximumAttributeCount = 2
        };
        XmiPassTwoOptions relationshipLimit = new XmiPassTwoOptions
        {
            MaximumRelationshipCount = 2
        };
        XmiPassTwoOptions diagnosticLimit = new XmiPassTwoOptions
        {
            MaximumDiagnosticCount = 1
        };
        XmiPassTwoOptions valueLimit = new XmiPassTwoOptions
        {
            MaximumAttributeValueLength = 4
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Entities, Has.Count.EqualTo(passOne.ElementCount));
            Assert.That(second.TotalBytesRead, Is.EqualTo(bytes.Length));
            Assert.Throws<XmlException>(() => BuildGraphWithOptions(bytes, passOne, attributeLimit));
            Assert.Throws<XmlException>(() => BuildGraphWithOptions(bytes, passOne, relationshipLimit));
            Assert.Throws<XmlException>(() => BuildGraphWithOptions(bytes, passOne, diagnosticLimit));
            Assert.Throws<XmlException>(() => BuildGraphWithOptions(bytes, passOne, valueLimit));
        });
    }

    private static (XmiPassOneResult PassOne, XmiPassTwoResult PassTwo, MemoryStream Source) BuildFixture()
    {
        MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(CreateGraphFixture()));
        Iso20022Ecore2013Adapter adapter = new Iso20022Ecore2013Adapter();
        XmiPassOneResult passOne = XmiPassOneReader.BuildIndex(
            source,
            "graph.iso20022",
            adapter,
            new XmiPassOneOptions());
        XmiPassTwoResult passTwo = XmiPassTwoReader.BuildGraph(
            source,
            "graph.iso20022",
            adapter,
            passOne,
            new XmiPassTwoOptions());
        return (passOne, passTwo, source);
    }

    private static XmiPassTwoResult BuildGraphWithOptions(
        byte[] bytes,
        XmiPassOneResult passOne,
        XmiPassTwoOptions options)
    {
        using MemoryStream source = new MemoryStream(bytes);
        return XmiPassTwoReader.BuildGraph(
            source,
            "bounded.iso20022",
            new Iso20022Ecore2013Adapter(),
            passOne,
            options);
    }

    private static string CreateRelationshipSignature(XmiPassTwoReference relationship)
    {
        return relationship.SourceDocumentOrder + "|" + relationship.AttributeOrder + "|" +
            relationship.PropertyName + "|" + relationship.Position + "|" +
            relationship.TargetDocumentOrder + "|" + relationship.TargetIdentifier + "|" +
            relationship.ErrorCode;
    }

    private static string CreateGraphFixture()
    {
        return "<?xml version='1.0'?>\n" +
            "<iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore' " +
            "xmlns:xmi='http://www.omg.org/XMI' " +
            "xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmi:id='_repository'>\n" +
            "  <dataDictionary xmi:id='_dictionary'>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_component' " +
            "name='Component' superType='_later   _missing _duplicate'>\n" +
            "      <element xsi:type='iso:BusinessAssociationEnd' xmi:id='_association' " +
            "name='Association' type='#_later'/>\n" +
            "      <semanticMarkup xmi:id='_markup' type='projectName'>\n" +
            "        <elements xmi:id='_markupElement' name='projectName' value='Test'/>\n" +
            "      </semanticMarkup>\n" +
            "      <example>First</example>\n" +
            "      <example><![CDATA[Second]]></example>\n" +
            "    </topLevelDictionaryEntry>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_later' " +
            "name='Later' previousVersion='_component'/>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_duplicate' " +
            "name='FirstDuplicate'/>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:Text' xmi:id='_duplicate' " +
            "name='SecondDuplicate'/>\n" +
            "  </dataDictionary>\n" +
            "  <businessProcessCatalogue xmi:id='_catalogue'>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:BusinessArea' xmi:id='_area' code='pain'>\n" +
            "      <messageDefinition xmi:id='_message'>\n" +
            "        <messageDefinitionIdentifier businessArea='pain' messageFunctionality='001'/>\n" +
            "      </messageDefinition>\n" +
            "    </topLevelCatalogueEntry>\n" +
            "  </businessProcessCatalogue>\n" +
            "</iso:Repository>";
    }

    private sealed class ForwardOnlyGraphStream : Stream
    {
        private readonly MemoryStream inner;

        internal ForwardOnlyGraphStream(byte[] bytes)
        {
            inner = new MemoryStream(bytes, false);
        }

        internal long TotalBytesRead;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = inner.Read(buffer, offset, count);
            TotalBytesRead += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
