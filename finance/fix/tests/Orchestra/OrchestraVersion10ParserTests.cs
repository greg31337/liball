using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraVersion10ParserTests
{
    [Test]
    public void ParsesEveryVersion10DefinitionKindWithoutCollapsingScenarios()
    {
        string xml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2020/orchestra/repository"
                             xmlns:dcterms="http://purl.org/dc/terms/"
                             xmlns:ext="urn:venue:extension"
                             name="Venue" version="FIX.5.0SP2_EP300">
              <fixr:metadata><dcterms:title>Venue</dcterms:title></fixr:metadata>
              <fixr:datatypes>
                <fixr:datatype name="String" added="FIX.4.2"><fixr:annotation><fixr:documentation purpose="SYNOPSIS">Text</fixr:documentation></fixr:annotation></fixr:datatype>
                <fixr:datatype name="AccountCode" baseType="String" ext:logical="account"><ext:facet length="20"/></fixr:datatype>
              </fixr:datatypes>
              <fixr:codeSets>
                <fixr:codeSet id="10" name="SideCodeSet" scenario="special" type="String" default="1" specUrl="https://example.test/side" added="FIX.4.2">
                  <fixr:code id="1001" name="Buy" value="1" sort="2" group="orders"/>
                  <fixr:code id="1002" name="Sell" scenario="alt" value="2"><fixr:annotation><fixr:documentation>Sell side</fixr:documentation></fixr:annotation></fixr:code>
                  <fixr:annotation><fixr:documentation purpose="SYNOPSIS">Order side</fixr:documentation></fixr:annotation>
                </fixr:codeSet>
              </fixr:codeSets>
              <fixr:fields>
                <fixr:field id="1" name="Account" abbrName="Acct" type="AccountCode" presence="required" value="DEFAULT" lengthId="2" discriminatorId="3" minInclusive="A" ext:owner="venue">
                  <fixr:annotation><fixr:documentation>Account identifier</fixr:documentation></fixr:annotation>
                  <ext:fieldFacet sensitive="true"/>
                </fixr:field>
                <fixr:field id="1" name="Account" scenario="alt" type="AccountCode" presence="constant" value="ALT" deprecated="FIX.Latest"/>
              </fixr:fields>
              <fixr:components>
                <fixr:component id="1000" name="OrderCore" abbrName="OrdCore" category="Orders" rendering="inline" which="anyOf" added="FIX.4.4">
                  <fixr:fieldRef id="1"/>
                  <fixr:componentRef id="1001" scenario="alt" presence="required" instanceName="Details"/>
                  <fixr:groupRef id="2000" presence="forbidden" implMinOccurs="0" implMaxOccurs="5"/>
                  <fixr:annotation><fixr:documentation>Core order fields</fixr:documentation></fixr:annotation>
                </fixr:component>
                <fixr:component id="1000" name="OrderCore" scenario="buy" category="Orders"><fixr:fieldRef id="1" presence="required"/></fixr:component>
              </fixr:components>
              <fixr:groups>
                <fixr:group id="2000" name="Parties" abbrName="Ptys" category="Orders" rendering="block" which="oneOf" implMinOccurs="0" implMaxOccurs="unbounded">
                  <fixr:numInGroup id="3" presence="required"/>
                  <fixr:fieldRef id="1" presence="constant" value="PARTY" lengthId="2" instanceName="PartyAccount"/>
                  <fixr:componentRef id="1001" presence="ignored"/>
                  <fixr:annotation><fixr:documentation>Party entries</fixr:documentation></fixr:annotation>
                </fixr:group>
              </fixr:groups>
              <fixr:messages>
                <fixr:message id="3000" name="NewOrderSingle" abbrName="NewOrd" msgType="D" category="Orders" flow="OrderFlow" added="FIX.4.2">
                  <fixr:structure which="anyOf">
                    <fixr:fieldRef id="1"/>
                    <fixr:fieldRef id="2" presence="required"/>
                    <fixr:fieldRef id="3" presence="forbidden"/>
                    <fixr:fieldRef id="4" presence="ignored"/>
                    <fixr:fieldRef id="5" presence="constant" value="X"/>
                    <fixr:componentRef id="1000" instanceName="Core"/>
                    <fixr:groupRef id="2000" implMinOccurs="1" implMaxOccurs="unbounded"/>
                  </fixr:structure>
                  <fixr:annotation><fixr:documentation purpose="SYNOPSIS">Submit order</fixr:documentation></fixr:annotation>
                </fixr:message>
                <fixr:message id="3000" name="NewOrderSingle" scenario="buy" msgType="D" category="Orders"><fixr:structure><fixr:componentRef id="1000" scenario="buy" presence="required"/></fixr:structure></fixr:message>
              </fixr:messages>
            </fixr:repository>
            """;

        OrchestraRepository repository = Parse(xml, "definitions-v10.xml");

        Assert.Multiple(() =>
        {
            Assert.That(repository.Datatypes, Has.Count.EqualTo(2));
            Assert.That(repository.Datatypes[0].Name, Is.EqualTo("String"));
            Assert.That(repository.Datatypes[0].Pedigree.AddedVersion, Is.EqualTo("FIX.4.2"));
            Assert.That(repository.Datatypes[0].Annotations[0].Content, Is.EqualTo("Text"));
            Assert.That(repository.Datatypes[1].BaseType, Is.EqualTo("String"));
            Assert.That(repository.Datatypes[1].Attributes["ext:logical"], Is.EqualTo("account"));
            Assert.That(repository.Datatypes[1].Extensions.Single(item => item.Name == "facet").Xml, Does.Contain("length=\"20\""));

            Assert.That(repository.CodeSets, Has.Count.EqualTo(1));
            Assert.That(repository.CodeSets[0].Id, Is.EqualTo("10"));
            Assert.That(repository.CodeSets[0].Scenario, Is.EqualTo("special"));
            Assert.That(repository.CodeSets[0].Type, Is.EqualTo("String"));
            Assert.That(repository.CodeSets[0].DefaultValue, Is.EqualTo("1"));
            Assert.That(repository.CodeSets[0].SpecificationUri, Is.EqualTo("https://example.test/side"));
            Assert.That(repository.CodeSets[0].Codes.Select(item => item.Value), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(repository.CodeSets[0].Codes[0].Sort, Is.EqualTo("2"));
            Assert.That(repository.CodeSets[0].Codes[0].Group, Is.EqualTo("orders"));
            Assert.That(repository.CodeSets[0].Codes[1].Scenario, Is.EqualTo("alt"));
            Assert.That(repository.CodeSets[0].Codes[1].Annotations[0].Content, Is.EqualTo("Sell side"));

            Assert.That(repository.Fields, Has.Count.EqualTo(2));
            Assert.That(repository.Fields[0].Id, Is.EqualTo("1"));
            Assert.That(repository.Fields[0].Abbreviation, Is.EqualTo("Acct"));
            Assert.That(repository.Fields[0].Scenario, Is.EqualTo("base"));
            Assert.That(repository.Fields[0].Presence, Is.EqualTo(FixPresence.Required));
            Assert.That(repository.Fields[0].Value, Is.EqualTo("DEFAULT"));
            Assert.That(repository.Fields[0].LengthId, Is.EqualTo("2"));
            Assert.That(repository.Fields[0].DiscriminatorId, Is.EqualTo("3"));
            Assert.That(repository.Fields[0].Attributes["minInclusive"], Is.EqualTo("A"));
            Assert.That(repository.Fields[0].Attributes["ext:owner"], Is.EqualTo("venue"));
            Assert.That(repository.Fields[0].Extensions.Single(item => item.Name == "fieldFacet").Xml, Does.Contain("sensitive=\"true\""));
            Assert.That(repository.Fields[1].Scenario, Is.EqualTo("alt"));
            Assert.That(repository.Fields[1].Presence, Is.EqualTo(FixPresence.Constant));
            Assert.That(repository.Fields[1].Pedigree.DeprecatedVersion, Is.EqualTo("FIX.Latest"));

            Assert.That(repository.Components, Has.Count.EqualTo(2));
            Assert.That(repository.Components[0].Name, Is.EqualTo("OrderCore"));
            Assert.That(repository.Components[0].Selection, Is.EqualTo("anyOf"));
            Assert.That(repository.Components[0].Members.Select(item => item.Kind), Is.EqualTo(new[] { "fieldRef", "componentRef", "groupRef" }));
            Assert.That(repository.Components[0].Members[0].Presence, Is.EqualTo(FixPresence.Optional));
            Assert.That(repository.Components[0].Members[0].PresenceText, Is.Empty);
            Assert.That(repository.Components[0].Members[1].Scenario, Is.EqualTo("alt"));
            Assert.That(repository.Components[0].Members[1].InstanceName, Is.EqualTo("Details"));
            Assert.That(repository.Components[0].Members[2].ImplementationMaximum, Is.EqualTo("5"));
            Assert.That(repository.Components[1].Scenario, Is.EqualTo("buy"));

            Assert.That(repository.Groups, Has.Count.EqualTo(1));
            Assert.That(repository.Groups[0].CountMember, Is.Not.Null);
            Assert.That(repository.Groups[0].CountMember!.Kind, Is.EqualTo("fieldRef"));
            Assert.That(repository.Groups[0].CountMember!.ReferenceId, Is.EqualTo("3"));
            Assert.That(repository.Groups[0].CountMember!.Presence, Is.EqualTo(FixPresence.Required));
            Assert.That(repository.Groups[0].Members, Has.Count.EqualTo(2));
            Assert.That(repository.Groups[0].Members[0].Value, Is.EqualTo("PARTY"));
            Assert.That(repository.Groups[0].Members[0].LengthId, Is.EqualTo("2"));
            Assert.That(repository.Groups[0].Members[1].Presence, Is.EqualTo(FixPresence.Ignored));
            Assert.That(repository.Groups[0].Attributes["implMaxOccurs"], Is.EqualTo("unbounded"));

            Assert.That(repository.Messages, Has.Count.EqualTo(2));
            Assert.That(repository.Messages[0].MessageType, Is.EqualTo("D"));
            Assert.That(repository.Messages[0].Flow, Is.EqualTo("OrderFlow"));
            Assert.That(repository.Messages[0].Selection, Is.EqualTo("anyOf"));
            Assert.That(repository.Messages[0].Members.Select(item => item.Position), Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
            Assert.That(repository.Messages[0].Members.Take(5).Select(item => item.Presence), Is.EqualTo(new[] { FixPresence.Optional, FixPresence.Required, FixPresence.Forbidden, FixPresence.Ignored, FixPresence.Constant }));
            Assert.That(repository.Messages[0].Members[4].Value, Is.EqualTo("X"));
            Assert.That(repository.Messages[0].Members[6].ImplementationMinimum, Is.EqualTo("1"));
            Assert.That(repository.Messages[0].Annotations[0].Content, Is.EqualTo("Submit order"));
            Assert.That(repository.Messages[1].Scenario, Is.EqualTo("buy"));
            Assert.That(repository.Messages[1].Members[0].Scenario, Is.EqualTo("buy"));

            Assert.That(repository.Scenarios.Select(item => item.Name), Is.EqualTo(new[] { "special", "base", "alt", "buy" }));
        });
    }

    [Test]
    public void OfficialLatestRepositoryRetainsAllVersion10DefinitionsAndSpecializations()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../specs/fix/FIXOrchestra_FIX.Latest_EP309/FIXOrchestra_FIX.Latest_EP309.xml"));
        OrchestraRepository repository;
        using (FileStream source = File.OpenRead(path))
        {
            OrchestraXmlLoadResult loaded = OrchestraXmlLoader.LoadDocument(source, new OrchestraReadOptions { BaseUri = path });
            Assert.That(loaded.Success, Is.True, Codes(loaded));
            repository = OrchestraVersion10Parser.ParseDocument(loaded);
        }

        Assert.Multiple(() =>
        {
            Assert.That(repository.Datatypes, Has.Count.EqualTo(38));
            Assert.That(repository.CodeSets, Has.Count.GreaterThan(100));
            Assert.That(repository.Fields, Has.Count.GreaterThan(1000));
            Assert.That(repository.Components, Has.Count.GreaterThan(100));
            Assert.That(repository.Groups, Has.Count.GreaterThan(100));
            Assert.That(repository.Messages, Has.Count.GreaterThan(100));
            Assert.That(repository.Scenarios.Select(item => item.Name), Does.Contain("base"));
            Assert.That(repository.Fields.Single(item => item.Id == "11" && item.Scenario == "base").Name, Is.EqualTo("ClOrdID"));
            Assert.That(repository.Messages.Single(item => item.MessageType == "D" && item.Scenario == "base").Name, Is.EqualTo("NewOrderSingle"));
        });
    }

    [TestCase("", FixPresence.Optional)]
    [TestCase("optional", FixPresence.Optional)]
    [TestCase("required", FixPresence.Required)]
    [TestCase("forbidden", FixPresence.Forbidden)]
    [TestCase("ignored", FixPresence.Ignored)]
    [TestCase("constant", FixPresence.Constant)]
    public void PresenceLexicalValuesMapToCanonicalPresence(string lexical, FixPresence expected)
    {
        Assert.That(OrchestraVersion10Parser.ParsePresence(lexical), Is.EqualTo(expected));
    }

    private static OrchestraRepository Parse(string xml, string sourceName)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        OrchestraXmlLoadResult loaded = OrchestraXmlLoader.LoadDocument(source, new OrchestraReadOptions { BaseUri = sourceName });
        Assert.That(loaded.Success, Is.True, Codes(loaded));
        return OrchestraVersion10Parser.ParseDocument(loaded);
    }

    private static string Codes(OrchestraXmlLoadResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }
}
