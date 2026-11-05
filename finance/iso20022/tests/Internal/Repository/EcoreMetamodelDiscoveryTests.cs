using System.IO.Compression;
using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class EcoreMetamodelDiscoveryTests
{
    [Test]
    public void DiscoverySelects2013AdapterAndPreservesSeekableInput()
    {
        string xml = "<?xml version='1.0'?>\n" +
            "<iso:Repository xmlns:iso='urn:iso:std:iso:20022:2013:ecore' " +
            "xmlns:xmi='http://www.omg.org/XMI' xmi:version='2.0' xmi:id='_repository'/>";
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        EcoreMetamodelDiscoveryResult result = EcoreMetamodelDiscovery.DiscoverMetamodel(
            source,
            "fixture.iso20022",
            4096);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.RootName.NamespaceUri, Is.EqualTo(Iso20022Namespaces.Ecore2013));
            Assert.That(result.RootName.LocalName, Is.EqualTo("Repository"));
            Assert.That(result.RootName.Prefix, Is.EqualTo("iso"));
            Assert.That(result.RootIdentifier, Is.EqualTo("_repository"));
            Assert.That(result.XmiVersion, Is.EqualTo("2.0"));
            Assert.That(result.MetamodelVersion, Is.EqualTo("2013"));
            Assert.That(result.AdapterIdentifier, Is.EqualTo("iso20022.ecore.2013"));
            Assert.That(result.Adapter, Is.Not.Null);
            Assert.That(result.Location.SourceName, Is.EqualTo("fixture.iso20022"));
            Assert.That(result.Location.HasLineInformation, Is.True);
            Assert.That(result.Location.LineNumber, Is.EqualTo(2));
            Assert.That(source.Position, Is.EqualTo(0));
            Assert.That(source.CanRead, Is.True);
        });
    }

    [Test]
    public void DiscoveryReportsUnsupportedRootsAndMetamodelNamespaces()
    {
        EcoreMetamodelDiscoveryResult wrongRoot = DiscoverText(
            "<iso:Package xmlns:iso='urn:iso:std:iso:20022:2013:ecore'/>");
        EcoreMetamodelDiscoveryResult wrongNamespace = DiscoverText(
            "<iso:Repository xmlns:iso='urn:iso:std:iso:20022:future:ecore'/>");

        Assert.Multiple(() =>
        {
            Assert.That(wrongRoot.Success, Is.False);
            Assert.That(wrongRoot.ErrorCode, Is.EqualTo("repository.root.unsupported"));
            Assert.That(wrongRoot.RootName.LocalName, Is.EqualTo("Package"));
            Assert.That(wrongNamespace.Success, Is.False);
            Assert.That(wrongNamespace.ErrorCode, Is.EqualTo("repository.metamodel.unsupported"));
            Assert.That(wrongNamespace.MetamodelNamespace,
                Is.EqualTo("urn:iso:std:iso:20022:future:ecore"));
            Assert.That(wrongNamespace.Adapter, Is.Null);
        });
    }

    [Test]
    public void DiscoveryProhibitsDocumentTypesAndEnforcesCharacterLimit()
    {
        string documentType = "<!DOCTYPE Repository [<!ENTITY value 'unsafe'>]>" +
            "<Repository xmlns='urn:iso:std:iso:20022:2013:ecore'>&value;</Repository>";
        byte[] oversized = Encoding.UTF8.GetBytes(
            "<Repository xmlns='urn:iso:std:iso:20022:2013:ecore'>content</Repository>");

        Assert.Multiple(() =>
        {
            Assert.Throws<XmlException>(() => DiscoverText(documentType));
            Assert.Throws<XmlException>(() => EcoreMetamodelDiscovery.DiscoverMetamodel(
                new MemoryStream(oversized),
                "oversized.iso20022",
                oversized.Length - 1));
        });
    }

    [Test]
    public void AdapterNormalizesEcoreTypeReferencesAndMapsEveryConcreteClassifierFamily()
    {
        Iso20022Ecore2013Adapter adapter = new Iso20022Ecore2013Adapter();
        (string TypeName, RepositoryEntityKind Kind)[] mappings =
        {
            ("Repository", RepositoryEntityKind.Repository),
            ("DataDictionary", RepositoryEntityKind.DataDictionary),
            ("BusinessProcessCatalogue", RepositoryEntityKind.BusinessProcessCatalogue),
            ("BusinessArea", RepositoryEntityKind.BusinessArea),
            ("BusinessProcess", RepositoryEntityKind.BusinessProcess),
            ("BusinessRole", RepositoryEntityKind.BusinessRole),
            ("Participant", RepositoryEntityKind.Participant),
            ("BusinessTransaction", RepositoryEntityKind.BusinessTransaction),
            ("Conversation", RepositoryEntityKind.Conversation),
            ("MessageChoreography", RepositoryEntityKind.MessageChoreography),
            ("MessageTransmission", RepositoryEntityKind.MessageTransmission),
            ("Send", RepositoryEntityKind.MessageTransmission),
            ("Receive", RepositoryEntityKind.MessageTransmission),
            ("MessagingEndpoint", RepositoryEntityKind.MessagingEndpoint),
            ("EndPointCategory", RepositoryEntityKind.MessagingEndpoint),
            ("BroadcastList", RepositoryEntityKind.BroadcastList),
            ("MessageTransportSystem", RepositoryEntityKind.MessageTransportSystem),
            ("MessageTransportMode", RepositoryEntityKind.TransportMode),
            ("Address", RepositoryEntityKind.Address),
            ("TransportMessage", RepositoryEntityKind.TransportMessage),
            ("MessageInstance", RepositoryEntityKind.TransportMessage),
            ("BusinessComponent", RepositoryEntityKind.BusinessComponent),
            ("BusinessAttribute", RepositoryEntityKind.BusinessAttribute),
            ("BusinessAssociationEnd", RepositoryEntityKind.BusinessAssociationEnd),
            ("BusinessElement", RepositoryEntityKind.BusinessElement),
            ("MessageSet", RepositoryEntityKind.MessageSet),
            ("IndustryMessageSet", RepositoryEntityKind.MessageSet),
            ("ISO15022MessageSet", RepositoryEntityKind.MessageSet),
            ("MessageDefinition", RepositoryEntityKind.MessageDefinition),
            ("MessageDefinitionIdentifier", RepositoryEntityKind.MessageDefinition),
            ("MessageBuildingBlock", RepositoryEntityKind.MessageBuildingBlock),
            ("MessageComponent", RepositoryEntityKind.MessageComponent),
            ("MessageAttribute", RepositoryEntityKind.MessageAttribute),
            ("MessageAssociationEnd", RepositoryEntityKind.MessageAssociationEnd),
            ("MessageElement", RepositoryEntityKind.MessageElement),
            ("ChoiceComponent", RepositoryEntityKind.Choice),
            ("Xor", RepositoryEntityKind.Choice),
            ("Text", RepositoryEntityKind.DataType),
            ("Amount", RepositoryEntityKind.DataType),
            ("DateTime", RepositoryEntityKind.DataType),
            ("SchemaType", RepositoryEntityKind.DataType),
            ("CodeSet", RepositoryEntityKind.CodeSet),
            ("Code", RepositoryEntityKind.CodeValue),
            ("Syntax", RepositoryEntityKind.Syntax),
            ("Encoding", RepositoryEntityKind.Encoding),
            ("ExternalSchema", RepositoryEntityKind.ExternalSchema),
            ("UserDefined", RepositoryEntityKind.ExternalSchema),
            ("SyntaxMessageScheme", RepositoryEntityKind.SyntaxMessageScheme),
            ("Constraint", RepositoryEntityKind.Constraint),
            ("SemanticMarkup", RepositoryEntityKind.SemanticMarkup),
            ("SemanticMarkupElement", RepositoryEntityKind.SemanticMarkup),
            ("Doclet", RepositoryEntityKind.Documentation),
            ("ConvergenceDocumentation", RepositoryEntityKind.Convergence)
        };

        Assert.Multiple(() =>
        {
            Assert.That(adapter.GetMetamodelNamespace(), Is.EqualTo(Iso20022Namespaces.Ecore2013));
            Assert.That(adapter.NormalizeTypeName("iso20022:BusinessComponent"),
                Is.EqualTo("BusinessComponent"));
            Assert.That(adapter.NormalizeTypeName("#//MessageDefinition"),
                Is.EqualTo("MessageDefinition"));
            Assert.That(adapter.NormalizeTypeName("platform:/resource/model.ecore#//CodeSet"),
                Is.EqualTo("CodeSet"));
            foreach ((string typeName, RepositoryEntityKind kind) in mappings)
            {
                Assert.That(adapter.IsKnownType(typeName), Is.True, typeName);
                Assert.That(adapter.ResolveEntityKind(typeName), Is.EqualTo(kind), typeName);
            }

            Assert.That(adapter.IsKnownType("RepositoryConcept"), Is.True);
            Assert.That(adapter.IsKnownType("UnknownFutureClassifier"), Is.False);
            Assert.That(adapter.ResolveEntityKind("UnknownFutureClassifier"),
                Is.EqualTo(RepositoryEntityKind.Unknown));
        });
    }

    [Test]
    public void AdapterResolvesContainmentFeaturesAndDeclaredXsiTypes()
    {
        Iso20022Ecore2013Adapter adapter = new Iso20022Ecore2013Adapter();

        Assert.Multiple(() =>
        {
            Assert.That(adapter.ResolveContainedType("Repository", "dataDictionary", string.Empty),
                Is.EqualTo("DataDictionary"));
            Assert.That(adapter.ResolveContainedType(
                "DataDictionary",
                "topLevelDictionaryEntry",
                "iso20022:BusinessComponent"),
                Is.EqualTo("BusinessComponent"));
            Assert.That(adapter.ResolveContainedType("MessageDefinition", "messageBuildingBlock", string.Empty),
                Is.EqualTo("MessageBuildingBlock"));
            Assert.That(adapter.ResolveContainedType("MessageTransmission", "send", string.Empty),
                Is.EqualTo("Send"));
            Assert.That(adapter.ResolveContainedType("CodeSet", "code", string.Empty),
                Is.EqualTo("Code"));
            Assert.That(adapter.IsContainmentFeature("Repository", "businessProcessCatalogue"), Is.True);
            Assert.That(adapter.IsContainmentFeature("DataDictionary", "topLevelDictionaryEntry"), Is.True);
            Assert.That(adapter.IsContainmentFeature("BusinessComponent", "element"), Is.True);
            Assert.That(adapter.IsContainmentFeature("BusinessComponent", "superType"), Is.False);
            Assert.That(adapter.IsTopLevelPartitionType("iso20022:DataDictionary"), Is.True);
            Assert.That(adapter.IsTopLevelPartitionType("BusinessComponent"), Is.False);
        });
    }

    [Test]
    public void BundledJune2026RepositorySelectsThe2013Adapter()
    {
        string archivePath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022/eRepository_ISO20022_20260626.zip"));
        using FileStream archiveStream = File.OpenRead(archivePath);
        using ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);
        ZipArchiveEntry entry = archive.Entries[0];
        using Stream repositoryStream = entry.Open();

        EcoreMetamodelDiscoveryResult result = EcoreMetamodelDiscovery.DiscoverMetamodel(
            repositoryStream,
            entry.FullName,
            Iso20022Limits.MaxArtifactBytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.RootIdentifier, Is.EqualTo("_9DmuQENJEeGHJ_bHJRPaIQ"));
            Assert.That(result.XmiVersion, Is.EqualTo("2.0"));
            Assert.That(result.MetamodelNamespace, Is.EqualTo(Iso20022Namespaces.Ecore2013));
            Assert.That(result.AdapterIdentifier, Is.EqualTo("iso20022.ecore.2013"));
        });
    }

    private static EcoreMetamodelDiscoveryResult DiscoverText(string xml)
    {
        MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return EcoreMetamodelDiscovery.DiscoverMetamodel(source, "fixture.iso20022", 4096);
    }
}
