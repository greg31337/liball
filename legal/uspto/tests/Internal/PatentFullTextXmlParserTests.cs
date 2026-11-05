using System.Text;
using uspto;

namespace uspto.Tests;

public class PatentFullTextXmlParserTests
{
    [Test]
    public void GrantRootParsesTypedBibliographicTextAndResourceData()
    {
        string xml = """
            <us-patent-grant xmlns="urn:uspto:ice" lang="EN" dtd-version="v4.5" file="US123-B2.XML" id="grant" country="US" date-produced="20240102" date-publ="20240109">
              <us-bibliographic-data-grant>
                <publication-reference><document-id><country>US</country><doc-number>123</doc-number><kind>B2</kind><date>20240109</date></document-id></publication-reference>
                <application-reference appl-type="utility"><document-id><country>US</country><doc-number>18123456</doc-number><date>20230304</date></document-id></application-reference>
                <us-application-series-code>18</us-application-series-code>
                <classifications-ipcr><classification-ipcr><ipc-version-indicator><date>20230101</date></ipc-version-indicator><classification-level>A</classification-level><section>G</section><class>06</class><subclass>F</subclass><main-group>16</main-group><subgroup>245</subgroup><symbol-position>F</symbol-position><classification-value>I</classification-value></classification-ipcr></classifications-ipcr>
                <classifications-cpc><main-cpc><classification-cpc><cpc-version-indicator><date>20230501</date></cpc-version-indicator><section>G</section><class>06</class><subclass>F</subclass><main-group>16</main-group><subgroup>245</subgroup><classification-value>I</classification-value></classification-cpc></main-cpc><further-cpc><classification-cpc><section>H</section><class>04</class><subclass>L</subclass><main-group>9</main-group><subgroup>00</subgroup><classification-value>N</classification-value></classification-cpc></further-cpc></classifications-cpc>
                <classification-national><country>US</country><main-classification>707 001</main-classification></classification-national>
                <classification-wipo><field-code>6</field-code><field-name>Computer technology</field-name><sector>Electrical engineering</sector></classification-wipo>
                <invention-title id="title">Typed patent XML</invention-title>
                <us-references-cited>
                  <us-citation><patcit num="00001"><document-id><country>US</country><doc-number>999</doc-number><kind>A1</kind><name>Smith</name><date>20230000</date></document-id></patcit><category>cited by examiner</category><rel-claims>1-2</rel-claims><classification-cpc-text>G06F 16/245</classification-cpc-text></us-citation>
                  <us-citation><nplcit num="00002"><othercit>A useful paper.</othercit></nplcit><category>cited by applicant</category></us-citation>
                </us-references-cited>
                <number-of-claims>2</number-of-claims>
                <us-related-documents>
                  <continuation><parent-doc><document-id><country>US</country><doc-number>17000000</doc-number><date>20200101</date></document-id></parent-doc><child-doc><document-id><country>US</country><doc-number>18123456</doc-number></document-id></child-doc></continuation>
                  <related-publication><document-id><country>US</country><doc-number>20240123456</doc-number><kind>A1</kind></document-id></related-publication>
                </us-related-documents>
                <us-parties>
                  <us-applicants><us-applicant sequence="001" app-type="applicant" designation="us-only"><addressbook><orgname>Example Labs</orgname><address><city>Austin</city><state>TX</state><country>US</country></address></addressbook><residence><country>US</country></residence></us-applicant></us-applicants>
                  <inventors><inventor sequence="002"><addressbook><first-name>Ada</first-name><middle-name>M</middle-name><last-name>Lovelace</last-name><address><city>London</city><country>GB</country></address></addressbook></inventor></inventors>
                  <agents><agent rep-type="attorney"><addressbook><orgname>Patent Firm</orgname></addressbook></agent></agents>
                </us-parties>
                <assignees><assignee sequence="3"><addressbook><orgname>Example Owner</orgname><role>owner</role><address><city>Austin</city><country>US</country></address></addressbook></assignee></assignees>
                <examiners><primary-examiner><first-name>Grace</first-name><last-name>Hopper</last-name><department>2121</department></primary-examiner></examiners>
                <us-gov-interest><agency-name>Example Agency</agency-name><agency-code>EA</agency-code><contract-number>ABC-123</contract-number><rights-category>license</rights-category></us-gov-interest>
              </us-bibliographic-data-grant>
              <abstract id="abstract"><p id="a-1" num="0001">An <b>example</b> abstract.</p></abstract>
              <drawings><figure id="fig-1" num="0001"><img id="img-1" file="US123-D00001.TIF" img-format="tif" img-content="drawing" alt="Figure one" orientation="landscape" wi="100mm" he="80mm"/></figure></drawings>
              <description id="description"><heading id="h-1" level="1">BACKGROUND</heading><p id="p-1" num="0001">First paragraph.</p><description-of-drawings><heading id="h-2" level="2">DRAWINGS</heading><p id="p-2">Nested paragraph.</p></description-of-drawings></description>
              <claims id="claims"><claim id="CLM-00001" num="00001"><claim-text>1. A system.</claim-text></claim><claim id="CLM-00002" num="00002"><claim-text>2. The system of <claim-ref idref="CLM-00001">claim 1</claim-ref>.</claim-text></claim></claims>
            </us-patent-grant>
            """;

        PatentDataProvenance provenance = new PatentDataProvenance();
        provenance.Source = DataProvenance.Bulk;
        provenance.DatasetIdentifier = "PTGRXML";
        PatentFullText result = Parse(xml, provenance);

        Assert.Multiple(() =>
        {
            Assert.That(result.RootElementName, Is.EqualTo("us-patent-grant"));
            Assert.That(result.DocumentCategory, Is.EqualTo("grant"));
            Assert.That(result.SourceFileName, Is.EqualTo("US123-B2.XML"));
            Assert.That(result.DtdVersion, Is.EqualTo("v4.5"));
            Assert.That(result.HasProducedDate, Is.True);
            Assert.That(result.ProducedDate, Is.EqualTo(new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(result.HasPublishedDate, Is.True);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Bulk));
            Assert.That(result.Provenance, Is.Not.SameAs(provenance));
        });

        PatentBibliographicData data = result.BibliographicData;
        Assert.Multiple(() =>
        {
            Assert.That(data.Publication.DocumentNumber, Is.EqualTo("123"));
            Assert.That(data.Publication.KindCode, Is.EqualTo("B2"));
            Assert.That(data.Publication.HasDate, Is.True);
            Assert.That(data.Application.DocumentNumber, Is.EqualTo("18123456"));
            Assert.That(data.ApplicationType, Is.EqualTo("utility"));
            Assert.That(data.ApplicationSeriesCode, Is.EqualTo("18"));
            Assert.That(data.InventionTitle, Is.EqualTo("Typed patent XML"));
            Assert.That(data.ClaimCount, Is.EqualTo(2));
            Assert.That(data.HasClaimCount, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(data.Classifications.Ipc, Has.Length.EqualTo(1));
            Assert.That(data.Classifications.Ipc[0].Symbol, Is.EqualTo("G06F 16/245"));
            Assert.That(data.Classifications.Ipc[0].Version, Is.EqualTo("20230101"));
            Assert.That(data.Classifications.Cpc, Has.Length.EqualTo(2));
            Assert.That(data.Classifications.Cpc[0].IsPrimary, Is.True);
            Assert.That(data.Classifications.Cpc[1].Symbol, Is.EqualTo("H04L 9/00"));
            Assert.That(data.Classifications.Uspc[0].Symbol, Is.EqualTo("707 001"));
            Assert.That(data.Classifications.Wipo[0].FieldName, Is.EqualTo("Computer technology"));
            Assert.That(data.Classifications.GovernmentInterests[0].AgencyName, Is.EqualTo("Example Agency"));
            Assert.That(data.Classifications.GovernmentInterests[0].AwardNumber, Is.EqualTo("ABC-123"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(data.Parties.Applicants[0].Organization?.Name, Is.EqualTo("Example Labs"));
            Assert.That(data.Parties.Applicants[0].Addresses[0].GeographicRegionCode, Is.EqualTo("TX"));
            Assert.That(data.Parties.Inventors[0].Person.FullName, Is.EqualTo("Ada M Lovelace"));
            Assert.That(data.Parties.Assignees[0].Organization?.Name, Is.EqualTo("Example Owner"));
            Assert.That(data.Parties.Examiners[0].Person.FullName, Is.EqualTo("Grace Hopper"));
            Assert.That(data.Parties.Examiners[0].ArtUnit, Is.EqualTo("2121"));
            Assert.That(data.Parties.Attorneys[0].Organization?.Name, Is.EqualTo("Patent Firm"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(data.Citations, Has.Length.EqualTo(2));
            Assert.That(data.Citations[0].PatentDocument?.DocumentNumber, Is.EqualTo("999"));
            Assert.That(data.Citations[0].PatentDocument?.HasDate, Is.False);
            Assert.That(data.Citations[0].RelatedClaims, Is.EqualTo("1-2"));
            Assert.That(data.Citations[1].NonPatentText, Is.EqualTo("A useful paper."));
            Assert.That(data.RelatedApplications, Has.Length.EqualTo(2));
            Assert.That(data.RelatedApplications[0].ParentDocument?.DocumentNumber, Is.EqualTo("17000000"));
            Assert.That(data.RelatedApplications[1].Document?.DocumentNumber, Is.EqualTo("20240123456"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Abstract[0].Text, Is.EqualTo("An example abstract."));
            Assert.That(result.Description.Select(block => block.Category), Is.EqualTo(new[] { "heading", "p", "heading", "p" }));
            Assert.That(result.Description[2].Level, Is.EqualTo("2"));
            Assert.That(result.Claims, Has.Length.EqualTo(2));
            Assert.That(result.Claims[1].Text, Is.EqualTo("2. The system of claim 1."));
            Assert.That(result.Claims[1].References[0].Identifier, Is.EqualTo("CLM-00001"));
            Assert.That(result.ExternalResources[0].FigureIdentifier, Is.EqualTo("fig-1"));
            Assert.That(result.ExternalResources[0].FileName, Is.EqualTo("US123-D00001.TIF"));
            Assert.That(result.ExternalResources[0].Width, Is.EqualTo("100mm"));
        });
    }

    [Test]
    public void PublishedApplicationRootParsesApplicationBibliographicShape()
    {
        string xml = """
            <us-patent-application lang="EN" file="US20240000001A1.xml" id="application" country="US" date-produced="20231220" date-publ="20240104">
              <us-bibliographic-data-application>
                <publication-reference><document-id><country>US</country><doc-number>20240000001</doc-number><kind>A1</kind><date>20240104</date></document-id></publication-reference>
                <application-reference appl-type="utility"><document-id><country>US</country><doc-number>18000001</doc-number><date>20220601</date></document-id></application-reference>
                <invention-title>Published application</invention-title>
                <references-cited><citation><nplcit num="1"><othercit>Application reference.</othercit></nplcit></citation></references-cited>
                <related-documents><related-publication><document-id><country>WO</country><doc-number>2023000001</doc-number><kind>A1</kind></document-id></related-publication></related-documents>
              </us-bibliographic-data-application>
              <abstract>Short abstract.</abstract>
              <description><p>Application description.</p></description>
              <claims><claim id="c1" num="1"><claim-text>1. An application claim.</claim-text></claim></claims>
              <figure id="figure-a" num="1"><external-resource id="resource-a" href="drawing.svg"/></figure>
            </us-patent-application>
            """;

        PatentFullText result = Parse(xml, new PatentDataProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.DocumentCategory, Is.EqualTo("published application"));
            Assert.That(result.BibliographicData.Publication.KindCode, Is.EqualTo("A1"));
            Assert.That(result.BibliographicData.Application.DocumentNumber, Is.EqualTo("18000001"));
            Assert.That(result.BibliographicData.InventionTitle, Is.EqualTo("Published application"));
            Assert.That(result.BibliographicData.Citations[0].NonPatentText, Is.EqualTo("Application reference."));
            Assert.That(result.BibliographicData.RelatedApplications[0].Document?.CountryCode, Is.EqualTo("WO"));
            Assert.That(result.Abstract[0].Category, Is.EqualTo("abstract"));
            Assert.That(result.Abstract[0].Text, Is.EqualTo("Short abstract."));
            Assert.That(result.Claims[0].Number, Is.EqualTo("1"));
            Assert.That(result.ExternalResources[0].FileName, Is.EqualTo("drawing.svg"));
        });
    }

    [Test]
    public void CompleteGrantFixturePreservesNamespacedIceDataAndResourceOrder()
    {
        PatentFullText result = Parse(
            FixtureLoader.LoadText("patent-full-text/grant-complete.xml"),
            new PatentDataProvenance { Source = DataProvenance.Bulk, DatasetIdentifier = "PTGRXML" });

        Assert.Multiple(() =>
        {
            Assert.That(result.RootElementName, Is.EqualTo("us-patent-grant"));
            Assert.That(result.DocumentCategory, Is.EqualTo("grant"));
            Assert.That(result.DtdVersion, Is.EqualTo("v4.7"));
            Assert.That(result.SourceFileName, Is.EqualTo("US123-B2.XML"));
            Assert.That(result.HasProducedDate, Is.True);
            Assert.That(result.HasPublishedDate, Is.True);
            Assert.That(result.BibliographicData.Publication.DocumentNumber, Is.EqualTo("123"));
            Assert.That(result.BibliographicData.Application.DocumentNumber, Is.EqualTo("18123456"));
            Assert.That(result.BibliographicData.InventionTitle, Is.EqualTo("Namespaced fixture patent"));
            Assert.That(result.BibliographicData.ClaimCount, Is.EqualTo(2));
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.BibliographicData.Classifications.Ipc[0].Symbol, Is.EqualTo("G06F 16/245"));
            Assert.That(result.BibliographicData.Classifications.Cpc[0].Symbol, Is.EqualTo("G06F 16/245"));
            Assert.That(result.BibliographicData.Classifications.Uspc[0].Symbol, Is.EqualTo("707 001"));
            Assert.That(result.BibliographicData.Classifications.Wipo[0].FieldName, Is.EqualTo("Computer technology"));
            Assert.That(result.BibliographicData.Citations[0].PatentDocument?.DocumentNumber, Is.EqualTo("999"));
            Assert.That(result.BibliographicData.Citations[1].NonPatentText, Is.EqualTo("A fixture paper."));
            Assert.That(result.BibliographicData.RelatedApplications[0].ParentDocument?.DocumentNumber, Is.EqualTo("17000000"));
            Assert.That(result.BibliographicData.RelatedApplications[0].ChildDocument?.DocumentNumber, Is.EqualTo("18123456"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Abstract[0].Text, Is.EqualTo("An ordered abstract."));
            Assert.That(result.Description.Select(block => block.Text), Is.EqualTo(new[] { "DETAILS", "First mixed paragraph." }));
            Assert.That(result.Claims[1].Text, Is.EqualTo("2. The system of claim 1."));
            Assert.That(result.Claims[1].References[0].Identifier, Is.EqualTo("CLM-1"));
            Assert.That(result.ExternalResources.Select(resource => resource.FileName), Is.EqualTo(new[] { "drawing.svg", "drawing.tif" }));
            Assert.That(result.ExternalResources[0].FigureIdentifier, Is.EqualTo("fig-1"));
            Assert.That(result.ExternalResources[1].FigureNumber, Is.EqualTo("1"));
        });
    }

    [Test]
    public void CompleteApplicationFixtureMapsApplicationRootAndSparseSections()
    {
        PatentFullText result = Parse(FixtureLoader.LoadText("patent-full-text/application-complete.xml"), new PatentDataProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.RootElementName, Is.EqualTo("us-patent-application"));
            Assert.That(result.DocumentCategory, Is.EqualTo("published application"));
            Assert.That(result.DtdVersion, Is.EqualTo("v4.6"));
            Assert.That(result.BibliographicData.Publication.KindCode, Is.EqualTo("A1"));
            Assert.That(result.BibliographicData.Application.DocumentNumber, Is.EqualTo("18000001"));
            Assert.That(result.BibliographicData.Citations[0].NonPatentText, Is.EqualTo("Application fixture reference."));
            Assert.That(result.BibliographicData.RelatedApplications[0].Document?.CountryCode, Is.EqualTo("WO"));
            Assert.That(result.Abstract[0].Text, Is.EqualTo("Fixture application abstract."));
            Assert.That(result.Description[0].Text, Is.EqualTo("Fixture application description."));
            Assert.That(result.Claims[0].Text, Is.EqualTo("1. A fixture application claim."));
            Assert.That(result.ExternalResources, Is.Empty);
        });
    }

    [Test]
    public void MalformedFixtureIsRejectedByTheSecureXmlReader()
    {
        string xml = FixtureLoader.LoadText("patent-full-text/malformed.xml");

        Assert.Throws<System.Xml.XmlException>(() => Parse(xml, new PatentDataProvenance()));
    }

    [Test]
    public void ExternalEntityFixtureIsBlockedWithoutReadingTheReferencedResource()
    {
        string xml = FixtureLoader.LoadText("patent-full-text/external-entity.xml");

        Assert.Throws<System.Xml.XmlException>(() => Parse(xml, new PatentDataProvenance()));
    }

    [Test]
    public void UnsupportedRootReportsTypedXmlParseFailure()
    {
        ParseException exception = Assert.Throws<ParseException>(() => Parse("<patent/>", new PatentDataProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(exception.SourceName, Is.EqualTo("full-text.xml"));
            Assert.That(exception.DataPath, Is.EqualTo("/patent"));
        });
    }

    [Test]
    public void StandardIceDocumentTypeIsIgnoredButDeclaredEntitiesAreNotResolved()
    {
        string standard = "<!DOCTYPE us-patent-grant SYSTEM \"us-patent-grant-v45.dtd\"><us-patent-grant><us-bibliographic-data-grant><invention-title>Safe title</invention-title></us-bibliographic-data-grant></us-patent-grant>";
        PatentFullText parsed = Parse(standard, new PatentDataProvenance());
        string entity = "<!DOCTYPE us-patent-grant [<!ENTITY secret SYSTEM \"http://127.0.0.1:1/secret\">]><us-patent-grant><us-bibliographic-data-grant><invention-title>&secret;</invention-title></us-bibliographic-data-grant></us-patent-grant>";

        Assert.Multiple(() =>
        {
            Assert.That(parsed.BibliographicData.InventionTitle, Is.EqualTo("Safe title"));
            Assert.Throws<System.Xml.XmlException>(() => Parse(entity, new PatentDataProvenance()));
        });
    }

    [Test]
    public void MixedDescriptionContentFlattensInlineAndStructuralElementsInDocumentOrder()
    {
        string xml = """
            <us-patent-grant>
              <description>
                <description-of-drawings>
                  <heading id="h-10" level="2">DETAILED <i>DESCRIPTION</i></heading>
                  <p id="p-10" num="0010">Water H<sub>2</sub>O uses <b>mixed</b><![CDATA[ content]]><br/>after a break and<ul><li>first item</li><li>second <i>item</i></li></ul>to finish.</p>
                </description-of-drawings>
                <p id="p-11" num="0011"><table><row><entry>left</entry><entry>right</entry></row></table></p>
              </description>
            </us-patent-grant>
            """;

        PatentFullText result = Parse(xml, new PatentDataProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.Description.Select(block => block.Category), Is.EqualTo(new[] { "heading", "p", "p" }));
            Assert.That(result.Description[0].Identifier, Is.EqualTo("h-10"));
            Assert.That(result.Description[0].Level, Is.EqualTo("2"));
            Assert.That(result.Description[0].Text, Is.EqualTo("DETAILED DESCRIPTION"));
            Assert.That(result.Description[1].Identifier, Is.EqualTo("p-10"));
            Assert.That(result.Description[1].Number, Is.EqualTo("0010"));
            Assert.That(result.Description[1].Text, Is.EqualTo("Water H2O uses mixed content after a break and first item second item to finish."));
            Assert.That(result.Description[2].Text, Is.EqualTo("left right"));
        });
    }

    [Test]
    public void NestedClaimTextFlattensWithBoundariesAndRetainsOrderedReferences()
    {
        string xml = """
            <us-patent-application>
              <claims>
                <claim id="CLM-00007" num="00007"><claim-text>7. The device of <claim-ref idref="CLM-00001">claim <b>1</b></claim-ref>, comprising:<claim-text>a <i>first</i> stage;</claim-text><claim-text>a second stage referring to <claim-ref idref="CLM-00003">claim 3</claim-ref>.</claim-text></claim-text></claim>
                <claim id="CLM-00008"><claim-number>8</claim-number><claim-text>8. A compact claim.</claim-text></claim>
              </claims>
            </us-patent-application>
            """;

        PatentFullText result = Parse(xml, new PatentDataProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.Claims, Has.Length.EqualTo(2));
            Assert.That(result.Claims[0].Identifier, Is.EqualTo("CLM-00007"));
            Assert.That(result.Claims[0].Number, Is.EqualTo("00007"));
            Assert.That(result.Claims[0].Text, Is.EqualTo("7. The device of claim 1, comprising: a first stage; a second stage referring to claim 3."));
            Assert.That(result.Claims[0].References.Select(reference => reference.Identifier), Is.EqualTo(new[] { "CLM-00001", "CLM-00003" }));
            Assert.That(result.Claims[0].References.Select(reference => reference.Text), Is.EqualTo(new[] { "claim 1", "claim 3" }));
            Assert.That(result.Claims[1].Number, Is.EqualTo("8"));
            Assert.That(result.Claims[1].Text, Is.EqualTo("8. A compact claim."));
        });
    }

    private static PatentFullText Parse(string xml, PatentDataProvenance provenance)
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        UsptoLimits limits = new UsptoLimits();
        limits.MaximumXmlDepth = 64;
        limits.MaximumStringCharacters = 100000;
        return PatentFullTextXmlParser.Parse(input, "full-text.xml", limits, provenance);
    }
}
