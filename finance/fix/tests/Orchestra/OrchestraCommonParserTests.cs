using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraCommonParserTests
{
    [Test]
    public void Version10ParsesMetadataCategoriesSectionsPedigreeConceptsAndAppinfo()
    {
        string xml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2020/orchestra/repository"
                             xmlns:dcterms="http://purl.org/dc/terms/"
                             xmlns:ext="urn:venue:extension"
                             name="Venue" version="FIX.5.0SP2_EP300" ext:profile="cash">
              <fixr:metadata>
                <dcterms:title xml:lang="en">Venue Orchestra</dcterms:title>
                <dcterms:description>First</dcterms:description>
                <dcterms:description>Second</dcterms:description>
                <dcterms:created>2026-08-30</dcterms:created>
                <ext:build channel="stable">42</ext:build>
              </fixr:metadata>
              <fixr:categories>
                <fixr:category name="Orders" section="Trade" FIXMLFileName="orders" componentType="Message" includeFile="components"
                               added="FIX.4.2" addedEP="7" changeType="Definitional"
                               deprecated="FIX.5.0" deprecatedEP="100" issue="FIX-9" lastModified="FIX.Latest"
                               replaced="FIX.Latest" replacedEP="301" replacedByField="11" supported="unsupported"
                               updated="FIX.5.0SP2" updatedEP="200" ext:owner="venue">
                  <fixr:annotation added="FIX.4.4">
                    <fixr:documentation purpose="SYNOPSIS" langId="en" contentType="text/markdown" deprecated="FIX.Latest">Order <ext:em>entry</ext:em></fixr:documentation>
                    <fixr:appinfo purpose="GENERATOR" specUrl="https://example.test/spec" ext:mode="strict">prefix<ext:settings enabled="true"/>suffix</fixr:appinfo>
                  </fixr:annotation>
                  <ext:categoryData code="A"/>
                </fixr:category>
              </fixr:categories>
              <fixr:sections>
                <fixr:section name="Trade" displayOrder="-2" FIXMLFileName="trade" updated="FIX.Latest" updatedEP="271">
                  <fixr:annotation><fixr:documentation purpose="SYNOPSIS">Trading messages</fixr:documentation></fixr:annotation>
                </fixr:section>
              </fixr:sections>
              <fixr:concepts>
                <fixr:concept name="OrderIdentity">
                  <fixr:fieldRef id="11" scenario="base" added="FIX.4.2"/>
                  <fixr:componentRef id="1001" scenario="buy"/>
                  <fixr:groupRef id="2001"/>
                  <fixr:messageRef id="3001" name="NewOrderSingle" msgType="D" scenario="base"/>
                  <fixr:annotation><fixr:documentation purpose="SYNOPSIS">Order identifiers</fixr:documentation></fixr:annotation>
                </fixr:concept>
              </fixr:concepts>
              <fixr:annotation><ext:custom-note importance="high">Retain me</ext:custom-note></fixr:annotation>
              <ext:repositoryData version="2"/>
            </fixr:repository>
            """;

        OrchestraRepository repository = Parse(xml, "common-v10.xml");

        OrchestraCategory category = repository.Categories[0];
        OrchestraPedigree pedigree = category.Pedigree;
        OrchestraAnnotation documentation = category.Annotations[0];
        OrchestraAnnotation appinfo = category.Annotations[1];
        Assert.Multiple(() =>
        {
            Assert.That(repository.Version, Is.EqualTo(OrchestraVersion.Version10));
            Assert.That(repository.Metadata.Name, Is.EqualTo("Venue"));
            Assert.That(repository.Metadata.Release, Is.EqualTo("FIX.5.0SP2_EP300"));
            Assert.That(repository.Metadata.FixVersion, Is.EqualTo("FIX.5.0SP2"));
            Assert.That(repository.Metadata.HasExtensionPack, Is.True);
            Assert.That(repository.Metadata.ExtensionPack, Is.EqualTo(300));
            Assert.That(repository.Metadata.PublicationDate, Is.EqualTo("2026-08-30"));
            Assert.That(repository.Metadata.Terms.Select(item => item.Name), Is.EqualTo(new[] { "title", "description", "description", "created", "build" }));
            Assert.That(repository.Metadata.Terms[0].Language, Is.EqualTo("en"));
            Assert.That(repository.Metadata.Terms[4].Xml, Does.Contain("channel=\"stable\""));

            Assert.That(category.Name, Is.EqualTo("Orders"));
            Assert.That(category.Section, Is.EqualTo("Trade"));
            Assert.That(category.FixmlFileName, Is.EqualTo("orders"));
            Assert.That(category.ComponentType, Is.EqualTo("Message"));
            Assert.That(category.IncludeFile, Is.EqualTo("components"));
            Assert.That(pedigree.AddedVersion, Is.EqualTo("FIX.4.2"));
            Assert.That(pedigree.AddedExtensionPack, Is.EqualTo("7"));
            Assert.That(pedigree.ChangeType, Is.EqualTo("Definitional"));
            Assert.That(pedigree.DeprecatedVersion, Is.EqualTo("FIX.5.0"));
            Assert.That(pedigree.DeprecatedExtensionPack, Is.EqualTo("100"));
            Assert.That(pedigree.Issue, Is.EqualTo("FIX-9"));
            Assert.That(pedigree.LastModifiedVersion, Is.EqualTo("FIX.Latest"));
            Assert.That(pedigree.ReplacedVersion, Is.EqualTo("FIX.Latest"));
            Assert.That(pedigree.ReplacedExtensionPack, Is.EqualTo("301"));
            Assert.That(pedigree.ReplacedByFieldId, Is.EqualTo("11"));
            Assert.That(pedigree.Supported, Is.EqualTo("unsupported"));
            Assert.That(pedigree.UpdatedVersion, Is.EqualTo("FIX.5.0SP2"));
            Assert.That(pedigree.UpdatedExtensionPack, Is.EqualTo("200"));

            Assert.That(documentation.Kind, Is.EqualTo("documentation"));
            Assert.That(documentation.Purpose, Is.EqualTo("SYNOPSIS"));
            Assert.That(documentation.Language, Is.EqualTo("en"));
            Assert.That(documentation.ContentType, Is.EqualTo("text/markdown"));
            Assert.That(documentation.Content, Is.EqualTo("Order entry"));
            Assert.That(documentation.XmlContent, Does.Contain("ext:em"));
            Assert.That(documentation.ContainerPedigree.AddedVersion, Is.EqualTo("FIX.4.4"));
            Assert.That(documentation.Pedigree.DeprecatedVersion, Is.EqualTo("FIX.Latest"));
            Assert.That(documentation.Extensions.Single().Name, Is.EqualTo("em"));
            Assert.That(appinfo.Kind, Is.EqualTo("appinfo"));
            Assert.That(appinfo.SpecificationUri, Is.EqualTo("https://example.test/spec"));
            Assert.That(appinfo.XmlContent, Does.Contain("prefix"));
            Assert.That(appinfo.Extensions.Select(item => item.Name), Is.EquivalentTo(new[] { "settings", "mode" }));
            Assert.That(category.Extensions.Select(item => item.Name), Is.EquivalentTo(new[] { "categoryData", "owner" }));

            Assert.That(repository.Sections[0].Name, Is.EqualTo("Trade"));
            Assert.That(repository.Sections[0].HasDisplayOrder, Is.True);
            Assert.That(repository.Sections[0].DisplayOrder, Is.EqualTo(-2));
            Assert.That(repository.Sections[0].DisplayOrderText, Is.EqualTo("-2"));
            Assert.That(repository.Sections[0].Pedigree.UpdatedExtensionPack, Is.EqualTo("271"));
            Assert.That(repository.Sections[0].Annotations[0].Content, Is.EqualTo("Trading messages"));

            Assert.That(repository.Concepts[0].Name, Is.EqualTo("OrderIdentity"));
            Assert.That(repository.Concepts[0].Members.Select(item => item.Kind), Is.EqualTo(new[] { "fieldRef", "componentRef", "groupRef", "messageRef" }));
            Assert.That(repository.Concepts[0].Members[0].Id, Is.EqualTo("11"));
            Assert.That(repository.Concepts[0].Members[0].Pedigree.AddedVersion, Is.EqualTo("FIX.4.2"));
            Assert.That(repository.Concepts[0].Members[3].Name, Is.EqualTo("NewOrderSingle"));
            Assert.That(repository.Concepts[0].Members[3].MessageType, Is.EqualTo("D"));
            Assert.That(repository.Concepts[0].Annotations[0].Content, Is.EqualTo("Order identifiers"));

            Assert.That(repository.Annotations.Single(item => item.Kind == "extension").Extensions[0].Name, Is.EqualTo("custom-note"));
            Assert.That(repository.Extensions.Select(item => item.Name), Is.EquivalentTo(new[] { "repositoryData", "profile" }));
        });
    }

    [Test]
    public void Version11ParsesRequiredCategoryAndSectionShapesAndMetadataFallbacks()
    {
        string xml = """
            <repository xmlns="http://fixprotocol.io/2026/orchestra/repository" xmlns:dcterms="http://purl.org/dc/terms/">
              <metadata>
                <dcterms:title xml:lang="de">Fallback Name</dcterms:title>
                <dcterms:issued>2026-09-01</dcterms:issued>
              </metadata>
              <categories><category name="Orders" section="Trade"/></categories>
              <sections><section name="Trade"><annotation><documentation xml:lang="fr">Négociation</documentation></annotation></section></sections>
              <concepts><concept name="Empty"><fieldRef id="1"/></concept></concepts>
            </repository>
            """;

        OrchestraRepository repository = Parse(xml, "common-v11.xml");

        Assert.Multiple(() =>
        {
            Assert.That(repository.Version, Is.EqualTo(OrchestraVersion.Version11));
            Assert.That(repository.Metadata.Name, Is.EqualTo("Fallback Name"));
            Assert.That(repository.Metadata.PublicationDate, Is.EqualTo("2026-09-01"));
            Assert.That(repository.Metadata.HasExtensionPack, Is.False);
            Assert.That(repository.Categories[0].Name, Is.EqualTo("Orders"));
            Assert.That(repository.Categories[0].Section, Is.EqualTo("Trade"));
            Assert.That(repository.Categories[0].FixmlFileName, Is.Empty);
            Assert.That(repository.Sections[0].HasDisplayOrder, Is.False);
            Assert.That(repository.Sections[0].Annotations[0].Language, Is.EqualTo("fr"));
            Assert.That(repository.Concepts[0].Members[0].Kind, Is.EqualTo("fieldRef"));
        });
    }

    [Test]
    public void OfficialLatestRepositoryCommonContentIsParsedWithoutTraversingDeferredDefinitions()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../specs/fix/FIXOrchestra_FIX.Latest_EP309/FIXOrchestra_FIX.Latest_EP309.xml"));
        OrchestraRepository repository;
        using (FileStream source = File.OpenRead(path))
        {
            OrchestraXmlLoadResult loaded = OrchestraXmlLoader.LoadDocument(source, new OrchestraReadOptions { BaseUri = path });
            Assert.That(loaded.Success, Is.True, Codes(loaded));
            repository = OrchestraCommonParser.ParseDocument(loaded);
        }

        OrchestraCategory session = repository.Categories.Single(item => item.Name == "Session");
        OrchestraSection preTrade = repository.Sections.Single(item => item.Name == "PreTrade");
        Assert.Multiple(() =>
        {
            Assert.That(repository.Metadata.Name, Is.EqualTo("FIX.Latest"));
            Assert.That(repository.Metadata.FixVersion, Is.EqualTo("FIX.Latest"));
            Assert.That(repository.Metadata.ExtensionPack, Is.EqualTo(309));
            Assert.That(repository.Metadata.PublicationDate, Is.EqualTo("2026-07-31"));
            Assert.That(repository.Categories, Has.Count.GreaterThan(20));
            Assert.That(repository.Sections, Has.Count.EqualTo(5));
            Assert.That(session.Annotations.Single().Kind, Is.EqualTo("appinfo"));
            Assert.That(session.Annotations.Single().Extensions.Single().Name, Is.EqualTo("FIXMLencodingType"));
            Assert.That(preTrade.Pedigree.UpdatedExtensionPack, Is.EqualTo("271"));
        });
    }

    private static OrchestraRepository Parse(string xml, string sourceName)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        OrchestraXmlLoadResult loaded = OrchestraXmlLoader.LoadDocument(source, new OrchestraReadOptions { BaseUri = sourceName });
        Assert.That(loaded.Success, Is.True, Codes(loaded));
        return OrchestraCommonParser.ParseDocument(loaded);
    }

    private static string Codes(OrchestraXmlLoadResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }
}
