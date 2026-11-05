using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraVersion11ParserTests
{
    [Test]
    public void ParsesLogicalDatatypeMappingsEncodingAppinfoAndSchemaReferences()
    {
        string xml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2026/orchestra/repository"
                             xmlns:dcterms="http://purl.org/dc/terms/"
                             xmlns:venue="urn:venue:encoding"
                             xmlns:xs="http://www.w3.org/2001/XMLSchema"
                             name="Venue" version="FIX.Latest"
                             namespace="urn:venue:repository" specUrl="https://example.test/repository">
              <fixr:metadata><dcterms:title>Venue repository</dcterms:title></fixr:metadata>
              <fixr:encodingStandards>
                <fixr:encodingStandard name="SBE" displayName="Simple Binary Encoding">
                  <fixr:encoding>
                    <fixr:annotation><fixr:appinfo purpose="CODEGEN"><venue:codec version="2"/><venue:schemaRef namespace="urn:venue:appinfo" href="appinfo-schema.xsd"/></fixr:appinfo></fixr:annotation>
                    <venue:schema namespace="urn:venue:sbe" location="venue-sbe.xml"/>
                  </fixr:encoding>
                  <fixr:annotation><fixr:documentation>Binary encoding</fixr:documentation></fixr:annotation>
                </fixr:encodingStandard>
                <fixr:encodingStandard name="FIXML"/>
              </fixr:encodingStandards>
              <fixr:datatypes>
                <fixr:datatype name="Price" baseType="Decimal" added="FIX.4.2">
                  <fixr:mappedDatatype standard="SBE" builtin="true" base="int64" pattern="-?[0-9]+" element="mantissa" size="8" parameter="scale=4" minInclusive="-100" maxInclusive="100">
                    <fixr:encoding>
                      <fixr:annotation><fixr:appinfo><venue:primitiveType>int64</venue:primitiveType></fixr:appinfo></fixr:annotation>
                      <venue:externalDatatype>
                        <xs:import namespace="urn:venue:types" schemaLocation="venue-types.xsd"/>
                      </venue:externalDatatype>
                    </fixr:encoding>
                    <fixr:annotation><fixr:documentation>Scaled integer price</fixr:documentation></fixr:annotation>
                  </fixr:mappedDatatype>
                  <fixr:mappedDatatype standard="FIXML" builtin="0" base="xs:decimal"/>
                </fixr:datatype>
                <fixr:datatype name="Decimal"/>
              </fixr:datatypes>
              <fixr:codeSets/>
              <fixr:fields/>
              <fixr:scenarios>
                <fixr:scenario name="base" added="FIX.4.0"><fixr:annotation><fixr:documentation>Default use case</fixr:documentation></fixr:annotation></fixr:scenario>
              </fixr:scenarios>
            </fixr:repository>
            """;

        OrchestraRepository repository = Parse(xml, "logical-v11.xml");

        Assert.Multiple(() =>
        {
            Assert.That(repository.Metadata.RepositoryNamespace, Is.EqualTo("urn:venue:repository"));
            Assert.That(repository.Metadata.SpecificationUri, Is.EqualTo("https://example.test/repository"));
            Assert.That(repository.EncodingStandards, Has.Count.EqualTo(2));
            Assert.That(repository.EncodingStandards[0].Name, Is.EqualTo("SBE"));
            Assert.That(repository.EncodingStandards[0].DisplayName, Is.EqualTo("Simple Binary Encoding"));
            Assert.That(repository.EncodingStandards[0].Encoding, Is.Not.Null);
            Assert.That(repository.EncodingStandards[0].Encoding!.Standard, Is.Empty);
            Assert.That(repository.EncodingStandards[0].Encoding!.Annotations.Single().Kind, Is.EqualTo("appinfo"));
            Assert.That(repository.EncodingStandards[0].Encoding!.Annotations.Single().Extensions.Select(item => item.Name), Is.EqualTo(new[] { "codec", "schemaRef" }));
            Assert.That(repository.EncodingStandards[0].Encoding!.Extensions.Single().Xml, Does.Contain("venue-sbe.xml"));
            Assert.That(repository.EncodingStandards[0].Encoding!.SchemaReferences.Select(item => item.NamespaceUri), Is.EqualTo(new[] { "urn:venue:appinfo", "urn:venue:sbe" }));
            Assert.That(repository.EncodingStandards[0].Encoding!.SchemaReferences.Select(item => item.Location), Is.EqualTo(new[] { "appinfo-schema.xsd", "venue-sbe.xml" }));

            Assert.That(repository.Datatypes, Has.Count.EqualTo(2));
            Assert.That(repository.Datatypes[0].BaseType, Is.EqualTo("Decimal"));
            Assert.That(repository.Datatypes[0].Mappings, Has.Count.EqualTo(2));
            Assert.That(repository.Datatypes[0].Mappings[0].Standard, Is.EqualTo("SBE"));
            Assert.That(repository.Datatypes[0].Mappings[0].HasBuiltin, Is.True);
            Assert.That(repository.Datatypes[0].Mappings[0].Builtin, Is.True);
            Assert.That(repository.Datatypes[0].Mappings[0].Base, Is.EqualTo("int64"));
            Assert.That(repository.Datatypes[0].Mappings[0].Pattern, Is.EqualTo("-?[0-9]+"));
            Assert.That(repository.Datatypes[0].Mappings[0].Element, Is.EqualTo("mantissa"));
            Assert.That(repository.Datatypes[0].Mappings[0].Size, Is.EqualTo("8"));
            Assert.That(repository.Datatypes[0].Mappings[0].Parameter, Is.EqualTo("scale=4"));
            Assert.That(repository.Datatypes[0].Mappings[0].MinimumInclusive, Is.EqualTo("-100"));
            Assert.That(repository.Datatypes[0].Mappings[0].MaximumInclusive, Is.EqualTo("100"));
            Assert.That(repository.Datatypes[0].Mappings[0].Encoding!.Annotations.Single().Extensions.Single().Name, Is.EqualTo("primitiveType"));
            Assert.That(repository.Datatypes[0].Mappings[0].Encoding!.SchemaReferences.Single().Name, Is.EqualTo("import"));
            Assert.That(repository.Datatypes[0].Mappings[0].Encoding!.SchemaReferences.Single().NamespaceUri, Is.EqualTo("urn:venue:types"));
            Assert.That(repository.Datatypes[0].Mappings[0].Encoding!.SchemaReferences.Single().Location, Is.EqualTo("venue-types.xsd"));
            Assert.That(repository.Datatypes[0].Mappings[1].BuiltinText, Is.EqualTo("0"));
            Assert.That(repository.Datatypes[0].Mappings[1].Builtin, Is.False);
            Assert.That(repository.Scenarios.Single().Name, Is.EqualTo("base"));
            Assert.That(repository.Scenarios.Single().Pedigree.AddedVersion, Is.EqualTo("FIX.4.0"));
            Assert.That(repository.Scenarios.Single().Annotations.Single().Content, Is.EqualTo("Default use case"));
        });
    }

    [Test]
    public void AttachesMultipleEncodingsAtDefinitionMemberAndContainerScopes()
    {
        string xml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2026/orchestra/repository"
                             xmlns:dcterms="http://purl.org/dc/terms/"
                             xmlns:venue="urn:venue:encoding"
                             name="Venue" version="FIX.Latest">
              <fixr:metadata><dcterms:title>Venue repository</dcterms:title></fixr:metadata>
              <fixr:categories>
                <fixr:category name="Orders" section="Trade">
                  <fixr:encodings><fixr:encoding standard="FIXML"><venue:name>Order</venue:name></fixr:encoding><fixr:encoding standard="SBE"><venue:id>1</venue:id></fixr:encoding></fixr:encodings>
                </fixr:category>
              </fixr:categories>
              <fixr:sections><fixr:section name="Trade"><fixr:encodings><fixr:encoding standard="FIXML"><venue:name>Trade</venue:name></fixr:encoding></fixr:encodings></fixr:section></fixr:sections>
              <fixr:datatypes><fixr:datatype name="String"/></fixr:datatypes>
              <fixr:codeSets>
                <fixr:codeSet id="10" name="SideCodeSet" type="String" unionDataType="StringOrInt">
                  <fixr:code id="100" name="Buy" value="1"/>
                  <fixr:encodings><fixr:encoding standard="FIXML"><venue:name>Side</venue:name></fixr:encoding></fixr:encodings>
                </fixr:codeSet>
                <fixr:encodings><fixr:encoding standard="SBE"><venue:type>enum</venue:type></fixr:encoding></fixr:encodings>
              </fixr:codeSets>
              <fixr:fields>
                <fixr:field id="54" name="Side" codeSet="SideCodeSet" unionDataType="StringOrInt">
                  <fixr:encodings><fixr:encoding standard="FIXML"><venue:name>Side</venue:name></fixr:encoding><fixr:encoding standard="SBE"><venue:id>54</venue:id></fixr:encoding></fixr:encodings>
                </fixr:field>
              </fixr:fields>
              <fixr:components>
                <fixr:component id="1000" name="OrderCore">
                  <fixr:fieldRef id="54"><fixr:encodings><fixr:encoding standard="SBE"><venue:offset>0</venue:offset></fixr:encoding></fixr:encodings></fixr:fieldRef>
                  <fixr:encodings><fixr:encoding standard="FIXML"><venue:name>Core</venue:name></fixr:encoding></fixr:encodings>
                </fixr:component>
                <fixr:encodings><fixr:encoding standard="SBE"><venue:type>composite</venue:type></fixr:encoding></fixr:encodings>
              </fixr:components>
              <fixr:groups>
                <fixr:group id="2000" name="Parties">
                  <fixr:numInGroup id="54"><fixr:encodings><fixr:encoding standard="SBE"><venue:id>453</venue:id></fixr:encoding></fixr:encodings></fixr:numInGroup>
                  <fixr:fieldRef id="54"><fixr:encodings><fixr:encoding standard="FIXML"><venue:name>Side</venue:name></fixr:encoding></fixr:encodings></fixr:fieldRef>
                  <fixr:encodings><fixr:encoding standard="SBE"><venue:type>group</venue:type></fixr:encoding></fixr:encodings>
                </fixr:group>
                <fixr:encodings><fixr:encoding standard="FIXML"><venue:type>repeating</venue:type></fixr:encoding></fixr:encodings>
              </fixr:groups>
              <fixr:messages>
                <fixr:message id="3000" name="NewOrderSingle" msgType="D">
                  <fixr:structure><fixr:fieldRef id="54"><fixr:encodings><fixr:encoding standard="SBE"><venue:offset>8</venue:offset></fixr:encoding></fixr:encodings></fixr:fieldRef></fixr:structure>
                  <fixr:encodings><fixr:encoding standard="FIXML"><venue:name>Order</venue:name></fixr:encoding><fixr:encoding standard="SBE"><venue:templateId>99</venue:templateId></fixr:encoding></fixr:encodings>
                </fixr:message>
                <fixr:encodings><fixr:encoding standard="SBE"><venue:type>message</venue:type></fixr:encoding></fixr:encodings>
              </fixr:messages>
            </fixr:repository>
            """;

        OrchestraRepository repository = Parse(xml, "encodings-v11.xml");

        Assert.Multiple(() =>
        {
            Assert.That(repository.Categories.Single().Encodings.Select(item => item.Standard), Is.EqualTo(new[] { "FIXML", "SBE" }));
            Assert.That(repository.Sections.Single().Encodings.Single().Standard, Is.EqualTo("FIXML"));
            Assert.That(repository.CodeSets.Single().UnionDatatype, Is.EqualTo("StringOrInt"));
            Assert.That(repository.CodeSets.Single().Encodings.Single().Standard, Is.EqualTo("FIXML"));
            Assert.That(repository.CodeSetEncodings.Single().Standard, Is.EqualTo("SBE"));
            Assert.That(repository.Fields.Single().Type, Is.Empty);
            Assert.That(repository.Fields.Single().CodeSet, Is.EqualTo("SideCodeSet"));
            Assert.That(repository.Fields.Single().UnionDatatype, Is.EqualTo("StringOrInt"));
            Assert.That(repository.Fields.Single().Encodings.Select(item => item.Standard), Is.EqualTo(new[] { "FIXML", "SBE" }));
            Assert.That(repository.Components.Single().Members.Single().Encodings.Single().Standard, Is.EqualTo("SBE"));
            Assert.That(repository.Components.Single().Encodings.Single().Standard, Is.EqualTo("FIXML"));
            Assert.That(repository.ComponentEncodings.Single().Standard, Is.EqualTo("SBE"));
            Assert.That(repository.Groups.Single().CountMember!.Encodings.Single().Standard, Is.EqualTo("SBE"));
            Assert.That(repository.Groups.Single().Members.Single().Encodings.Single().Standard, Is.EqualTo("FIXML"));
            Assert.That(repository.Groups.Single().Encodings.Single().Standard, Is.EqualTo("SBE"));
            Assert.That(repository.GroupEncodings.Single().Standard, Is.EqualTo("FIXML"));
            Assert.That(repository.Messages.Single().Members.Single().Encodings.Single().Standard, Is.EqualTo("SBE"));
            Assert.That(repository.Messages.Single().Encodings.Select(item => item.Standard), Is.EqualTo(new[] { "FIXML", "SBE" }));
            Assert.That(repository.MessageEncodings.Single().Standard, Is.EqualTo("SBE"));
            Assert.That(repository.Messages.Single().Encodings[1].Extensions.Single().Xml, Does.Contain("templateId"));
        });
    }

    private static OrchestraRepository Parse(string xml, string sourceName)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        OrchestraXmlLoadResult loaded = OrchestraXmlLoader.LoadDocument(source, new OrchestraReadOptions { BaseUri = sourceName });
        Assert.That(loaded.Success, Is.True, Codes(loaded));
        return OrchestraVersion11Parser.ParseDocument(loaded);
    }

    private static string Codes(OrchestraXmlLoadResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }
}
