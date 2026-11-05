using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class XmiPassOneReaderTests
{
    [Test]
    public void PassOneIndexesKindsLocationsParentsAndTopLevelPartitions()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(CreateRepositoryFixture());
        using MemoryStream source = new MemoryStream(bytes);

        XmiPassOneResult result = XmiPassOneReader.BuildIndex(
            source,
            "pass-one.iso20022",
            new Iso20022Ecore2013Adapter(),
            new XmiPassOneOptions());
        XmiPassOneEntry? component = result.FindIdentifier("_component");
        XmiPassOneEntry? attribute = result.FindIdentifier("_duplicate");
        XmiPassOneEntry? markupElement = result.FindIdentifier("_markupElement");
        XmiPassOneEntry? unknown = result.FindIdentifier("_unknown");
        XmiPassOneEntry? message = result.FindIdentifier("_message");

        Assert.Multiple(() =>
        {
            Assert.That(result.ElementCount, Is.EqualTo(12));
            Assert.That(result.IdentifiedElementCount, Is.EqualTo(11));
            Assert.That(result.UnidentifiedElementCount, Is.EqualTo(1));
            Assert.That(result.Entries, Has.Count.EqualTo(11));
            Assert.That(result.EntriesByIdentifier, Has.Count.EqualTo(10));
            Assert.That(result.Repository?.Identifier, Is.EqualTo("_repository"));
            Assert.That(result.Repository?.Kind, Is.EqualTo(RepositoryEntityKind.Repository));
            Assert.That(result.DataDictionary?.Identifier, Is.EqualTo("_dictionary"));
            Assert.That(result.DataDictionary?.IsTopLevelPartition, Is.True);
            Assert.That(result.BusinessProcessCatalogue?.Identifier, Is.EqualTo("_catalogue"));
            Assert.That(result.BusinessProcessCatalogue?.IsTopLevelPartition, Is.True);
            Assert.That(component?.TypeName, Is.EqualTo("BusinessComponent"));
            Assert.That(component?.Kind, Is.EqualTo(RepositoryEntityKind.BusinessComponent));
            Assert.That(component?.ParentIdentifier, Is.EqualTo("_dictionary"));
            Assert.That(component?.TopLevelPartitionIdentifier, Is.EqualTo("_dictionary"));
            Assert.That(component?.TopLevelPartitionKind, Is.EqualTo(RepositoryEntityKind.DataDictionary));
            Assert.That(attribute?.Kind, Is.EqualTo(RepositoryEntityKind.BusinessAttribute));
            Assert.That(attribute?.ParentIdentifier, Is.EqualTo("_component"));
            Assert.That(attribute?.IsDuplicate, Is.False);
            Assert.That(markupElement?.TypeName, Is.EqualTo("SemanticMarkupElement"));
            Assert.That(markupElement?.ParentIdentifier, Is.EqualTo("_markup"));
            Assert.That(unknown?.TypeName, Is.EqualTo("UnknownFutureClassifier"));
            Assert.That(unknown?.Kind, Is.EqualTo(RepositoryEntityKind.Unknown));
            Assert.That(message?.TopLevelPartitionIdentifier, Is.EqualTo("_catalogue"));
            Assert.That(message?.TopLevelPartitionKind,
                Is.EqualTo(RepositoryEntityKind.BusinessProcessCatalogue));
            Assert.That(component?.Location.SourceName, Is.EqualTo("pass-one.iso20022"));
            Assert.That(component?.Location.HasLineInformation, Is.True);
            Assert.That(component?.Location.LineNumber, Is.EqualTo(4));
            Assert.That(source.Position, Is.EqualTo(0));
            Assert.That(source.CanRead, Is.True);
        });
    }

    [Test]
    public void DuplicateIdentifiersRetainFirstEntryAndBothLocations()
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(CreateRepositoryFixture()));

        XmiPassOneResult result = XmiPassOneReader.BuildIndex(
            source,
            "duplicates.iso20022",
            new Iso20022Ecore2013Adapter(),
            new XmiPassOneOptions());
        XmiDuplicateIdentifier duplicate = result.Duplicates[0];
        XmiPassOneEntry indexed = result.EntriesByIdentifier["_duplicate"];

        Assert.Multiple(() =>
        {
            Assert.That(result.Duplicates, Has.Count.EqualTo(1));
            Assert.That(duplicate.Identifier, Is.EqualTo("_duplicate"));
            Assert.That(duplicate.ErrorCode, Is.EqualTo("repository.xmi.id_duplicate"));
            Assert.That(duplicate.FirstEntry, Is.SameAs(indexed));
            Assert.That(duplicate.FirstEntry.Kind, Is.EqualTo(RepositoryEntityKind.BusinessAttribute));
            Assert.That(duplicate.DuplicateEntry.Kind,
                Is.EqualTo(RepositoryEntityKind.BusinessAssociationEnd));
            Assert.That(duplicate.DuplicateEntry.IsDuplicate, Is.True);
            Assert.That(duplicate.FirstEntry.Location.LineNumber, Is.EqualTo(5));
            Assert.That(duplicate.DuplicateEntry.Location.LineNumber, Is.EqualTo(6));
            Assert.That(duplicate.FirstEntry.DocumentOrder,
                Is.LessThan(duplicate.DuplicateEntry.DocumentOrder));
        });
    }

    [Test]
    public void PassOneSupportsForwardOnlyStreams()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(CreateRepositoryFixture());
        using ForwardOnlyReadStream source = new ForwardOnlyReadStream(bytes);

        XmiPassOneResult result = XmiPassOneReader.BuildIndex(
            source,
            "forward-only.iso20022",
            new Iso20022Ecore2013Adapter(),
            new XmiPassOneOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Repository?.Identifier, Is.EqualTo("_repository"));
            Assert.That(result.FindIdentifier("_message")?.Kind,
                Is.EqualTo(RepositoryEntityKind.MessageDefinition));
            Assert.That(source.TotalBytesRead, Is.EqualTo(bytes.Length));
            Assert.That(source.CanRead, Is.True);
        });
    }

    [Test]
    public void PassOneProhibitsDocumentTypesAndEnforcesConfiguredBounds()
    {
        string documentType = "<!DOCTYPE Repository [<!ENTITY item 'value'>]>" +
            "<Repository xmlns='urn:iso:std:iso:20022:2013:ecore' " +
            "xmlns:xmi='http://www.omg.org/XMI' xmi:id='_root'>&item;</Repository>";
        XmiPassOneOptions elementOptions = new XmiPassOneOptions
        {
            MaximumElementCount = 2
        };
        XmiPassOneOptions identifierOptions = new XmiPassOneOptions
        {
            MaximumIdentifierCount = 2
        };
        XmiPassOneOptions characterOptions = new XmiPassOneOptions
        {
            MaximumCharacters = 128
        };
        XmiPassOneOptions depthOptions = new XmiPassOneOptions
        {
            MaximumDepth = 2
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<XmlException>(() => BuildText(documentType, new XmiPassOneOptions()));
            Assert.Throws<XmlException>(() => BuildText(CreateRepositoryFixture(), elementOptions));
            Assert.Throws<XmlException>(() => BuildText(CreateRepositoryFixture(), identifierOptions));
            Assert.Throws<XmlException>(() => BuildText(CreateRepositoryFixture(), characterOptions));
            Assert.Throws<XmlException>(() => BuildText(CreateRepositoryFixture(), depthOptions));
        });
    }

    [Test]
    public void PassOneBoundsDuplicateDiagnosticsAndRequiresSelectedRootMetamodel()
    {
        string duplicates =
            "<iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore' " +
            "xmlns:xmi='http://www.omg.org/XMI' xmi:id='_root'>" +
            "<dataDictionary xmi:id='_same'><semanticMarkup xmi:id='_same'/>" +
            "<doclet xmi:id='_same'/></dataDictionary></iso:Repository>";
        XmiPassOneOptions duplicateOptions = new XmiPassOneOptions
        {
            MaximumDuplicateCount = 1
        };
        string unsupported =
            "<iso:Repository xmlns:iso='urn:iso:std:iso:20022:future:ecore' " +
            "xmlns:xmi='http://www.omg.org/XMI' xmi:id='_root'/>";

        Assert.Multiple(() =>
        {
            Assert.Throws<XmlException>(() => BuildText(duplicates, duplicateOptions));
            Assert.Throws<XmlException>(() => BuildText(unsupported, new XmiPassOneOptions()));
        });
    }

    private static XmiPassOneResult BuildText(string xml, XmiPassOneOptions options)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return XmiPassOneReader.BuildIndex(
            source,
            "fixture.iso20022",
            new Iso20022Ecore2013Adapter(),
            options);
    }

    private static string CreateRepositoryFixture()
    {
        return "<?xml version='1.0'?>\n" +
            "<iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore' " +
            "xmlns:xmi='http://www.omg.org/XMI' " +
            "xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmi:id='_repository'>\n" +
            "  <dataDictionary xmi:id='_dictionary'>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:BusinessComponent' xmi:id='_component'>\n" +
            "      <element xsi:type='iso:BusinessAttribute' xmi:id='_duplicate'/>\n" +
            "      <element xsi:type='iso:BusinessAssociationEnd' xmi:id='_duplicate'/>\n" +
            "      <semanticMarkup xmi:id='_markup'>\n" +
            "        <elements xmi:id='_markupElement'/>\n" +
            "      </semanticMarkup>\n" +
            "    </topLevelDictionaryEntry>\n" +
            "    <topLevelDictionaryEntry xsi:type='iso:UnknownFutureClassifier' xmi:id='_unknown'/>\n" +
            "  </dataDictionary>\n" +
            "  <businessProcessCatalogue xmi:id='_catalogue'>\n" +
            "    <topLevelCatalogueEntry xsi:type='iso:BusinessArea' xmi:id='_area'>\n" +
            "      <messageDefinition xmi:id='_message'>\n" +
            "        <messageDefinitionIdentifier businessArea='pain'/>\n" +
            "      </messageDefinition>\n" +
            "    </topLevelCatalogueEntry>\n" +
            "  </businessProcessCatalogue>\n" +
            "</iso:Repository>";
    }

    private sealed class ForwardOnlyReadStream : Stream
    {
        private readonly MemoryStream inner;

        internal ForwardOnlyReadStream(byte[] bytes)
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
