using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class OrchestraDictionaryLoaderTests
{
    [Test]
    public void Version10MapsStructuralDefinitionsEnvelopeNestedGroupsAndRawConditions()
    {
        string xml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2020/orchestra/repository" name="Venue" version="FIX.5.0SP2_EP200">
              <fixr:metadata/>
              <fixr:datatypes>
                <fixr:datatype name="int"/>
                <fixr:datatype name="Length" baseType="int"/>
                <fixr:datatype name="NumInGroup" baseType="int"/>
                <fixr:datatype name="char"/>
                <fixr:datatype name="String"/>
                <fixr:datatype name="data"/>
              </fixr:datatypes>
              <fixr:codeSets>
                <fixr:codeSet id="54" name="SideCodeSet" type="char">
                  <fixr:code id="2" name="Sell" value="2" sort="2"/>
                  <fixr:code id="1" name="Buy" value="1" sort="1">
                    <fixr:annotation><fixr:documentation purpose="SYNOPSIS">Buy side</fixr:documentation></fixr:annotation>
                  </fixr:code>
                </fixr:codeSet>
              </fixr:codeSets>
              <fixr:fields>
                <fixr:field id="8" name="BeginString" type="String"/>
                <fixr:field id="9" name="BodyLength" type="Length"/>
                <fixr:field id="10" name="CheckSum" type="String"/>
                <fixr:field id="11" name="ClOrdID" abbrName="ClOrd" type="String">
                  <fixr:annotation><fixr:documentation purpose="SYNOPSIS">Client order identifier</fixr:documentation></fixr:annotation>
                </fixr:field>
                <fixr:field id="35" name="MsgType" type="String"/>
                <fixr:field id="54" name="Side" type="SideCodeSet"/>
                <fixr:field id="95" name="RawDataLength" type="Length"/>
                <fixr:field id="96" name="RawData" type="data" lengthId="95"/>
                <fixr:field id="448" name="PartyID" type="String"/>
                <fixr:field id="453" name="NoPartyIDs" type="NumInGroup"/>
                <fixr:field id="523" name="PartySubID" type="String"/>
                <fixr:field id="802" name="NoPartySubIDs" type="NumInGroup"/>
                <fixr:field id="9001" name="VenueID" type="String" discriminatorId="54"/>
              </fixr:fields>
              <fixr:components>
                <fixr:component id="1024" name="StandardHeader">
                  <fixr:fieldRef id="8" presence="required"/>
                  <fixr:fieldRef id="9" presence="required"/>
                  <fixr:fieldRef id="35" presence="required"/>
                </fixr:component>
                <fixr:component id="1025" name="StandardTrailer">
                  <fixr:fieldRef id="10" presence="required"/>
                </fixr:component>
                <fixr:component id="1100" name="OrderCore" abbrName="OrdCore">
                  <fixr:fieldRef id="11" presence="required"/>
                  <fixr:fieldRef id="9001"/>
                </fixr:component>
              </fixr:components>
              <fixr:groups>
                <fixr:group id="201" name="PartySubIDs">
                  <fixr:numInGroup id="802"/>
                  <fixr:fieldRef id="523" presence="required"/>
                </fixr:group>
                <fixr:group id="200" name="Parties">
                  <fixr:numInGroup id="453"/>
                  <fixr:fieldRef id="448" presence="required"/>
                  <fixr:groupRef id="201"/>
                </fixr:group>
              </fixr:groups>
              <fixr:messages>
                <fixr:message id="14" name="NewOrderSingle" msgType="D" category="Orders">
                  <fixr:structure>
                    <fixr:componentRef id="1024" presence="required"/>
                    <fixr:componentRef id="1100" presence="required"/>
                    <fixr:fieldRef id="54">
                      <fixr:rule presence="required"><fixr:when>exists(11)</fixr:when></fixr:rule>
                    </fixr:fieldRef>
                    <fixr:fieldRef id="95"/>
                    <fixr:fieldRef id="96"/>
                    <fixr:groupRef id="200"/>
                    <fixr:componentRef id="1025" presence="required"/>
                  </fixr:structure>
                  <fixr:annotation><fixr:documentation purpose="SYNOPSIS">Submit a new order</fixr:documentation></fixr:annotation>
                </fixr:message>
              </fixr:messages>
            </fixr:repository>
            """;

        DictionaryLoadResult result = Load(xml, new FixDictionaryOptions());

        Assert.That(result.Dictionary, Is.Not.Null);
        FixDictionary dictionary = result.Dictionary!;
        FixFieldDefinition rawData = dictionary.Fields.Single(field => field.Tag == 96);
        FixFieldDefinition rawLength = dictionary.Fields.Single(field => field.Tag == 95);
        FixFieldDefinition side = dictionary.Fields.Single(field => field.Tag == 54);
        FixMessageDefinition message = dictionary.Messages.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[] { "orchestra-behavior-deferred" }));
            Assert.That(dictionary.Identity.Name, Is.EqualTo("Venue"));
            Assert.That(dictionary.Identity.ApplicationVersion, Is.EqualTo("FIX.5.0SP2"));
            Assert.That(dictionary.Identity.BeginString, Is.Empty);
            Assert.That(dictionary.Identity.HasExtensionPack, Is.True);
            Assert.That(dictionary.Identity.ExtensionPack, Is.EqualTo(200));
            Assert.That(dictionary.Datatypes.Single(datatype => datatype.Name == "Length").FieldType, Is.EqualTo(FixFieldType.Length));
            Assert.That(side.CodeSetName, Is.EqualTo("SideCodeSet"));
            Assert.That(side.DatatypeName, Is.EqualTo("char"));
            Assert.That(side.FieldType, Is.EqualTo(FixFieldType.Char));
            Assert.That(dictionary.Fields.Single(field => field.Tag == 9001).HasDiscriminatorTag, Is.True);
            Assert.That(dictionary.Fields.Single(field => field.Tag == 9001).DiscriminatorTag, Is.EqualTo(54));
            Assert.That(dictionary.CodeSets.Single().Codes.ConvertAll(code => code.Value), Is.EqualTo(new[] { "2", "1" }));
            Assert.That(dictionary.CodeSets.Single().Codes[1].Description, Is.EqualTo("Buy side"));
            Assert.That(rawData.HasLengthTag, Is.True);
            Assert.That(rawData.LengthTag, Is.EqualTo(95));
            Assert.That(rawLength.HasDataTag, Is.True);
            Assert.That(rawLength.DataTag, Is.EqualTo(96));
            Assert.That(dictionary.Header.ConvertAll(member => member.ReferenceId), Is.EqualTo(new[] { 8, 9, 35 }));
            Assert.That(dictionary.Trailer.Single().ReferenceId, Is.EqualTo(10));
            Assert.That(dictionary.Components.ConvertAll(component => component.Name), Is.EqualTo(new[] { "OrderCore" }));
            Assert.That(dictionary.Groups.Single(group => group.Id == 200).DelimiterTag, Is.EqualTo(448));
            Assert.That(dictionary.Groups.Single(group => group.Id == 201).DelimiterTag, Is.EqualTo(523));
            Assert.That(message.Name, Is.EqualTo("NewOrderSingle"));
            Assert.That(message.Scenario, Is.EqualTo("base"));
            Assert.That(message.Description, Is.EqualTo("Submit a new order"));
            Assert.That(message.Members.ConvertAll(member => member.Kind), Is.EqualTo(new[]
            {
                FixMemberKind.Component,
                FixMemberKind.Field,
                FixMemberKind.Field,
                FixMemberKind.Field,
                FixMemberKind.Group
            }));
            Assert.That(message.Members[1].Presence, Is.EqualTo(FixPresence.Conditional));
            Assert.That(message.Members[1].PresenceRule, Is.EqualTo("exists(11)"));
            Assert.That(message.Members[1].RulePresence, Is.EqualTo(FixPresence.Required));
        });
    }

    [Test]
    public void Version11MapsExplicitCodeSetAndScenarioInheritance()
    {
        string xml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2026/orchestra/repository" name="Venue44" version="FIX.4.4">
              <fixr:metadata/>
              <fixr:scenarios>
                <fixr:scenario name="base"/>
                <fixr:scenario name="auction"/>
              </fixr:scenarios>
              <fixr:datatypes>
                <fixr:datatype name="char"/>
                <fixr:datatype name="String"/>
              </fixr:datatypes>
              <fixr:codeSets>
                <fixr:codeSet id="54" name="SideCodeSet" type="char">
                  <fixr:code id="1" name="Buy" value="1"/>
                </fixr:codeSet>
              </fixr:codeSets>
              <fixr:fields>
                <fixr:field id="8" name="BeginString" type="String"/>
                <fixr:field id="10" name="CheckSum" type="String"/>
                <fixr:field id="54" name="Side" codeSet="SideCodeSet"/>
                <fixr:field id="54" name="AuctionSide" codeSet="SideCodeSet" scenario="auction"/>
              </fixr:fields>
              <fixr:components>
                <fixr:component id="1024" name="StandardHeader"><fixr:fieldRef id="8" presence="required"/></fixr:component>
                <fixr:component id="1025" name="StandardTrailer"><fixr:fieldRef id="10" presence="required"/></fixr:component>
              </fixr:components>
              <fixr:messages>
                <fixr:message id="2" name="BaseOrder" msgType="U1" scenario="base" category="Orders">
                  <fixr:structure><fixr:fieldRef id="54" presence="required"/></fixr:structure>
                </fixr:message>
                <fixr:message id="2" name="AuctionOrder" msgType="U1" scenario="auction" scenarioRef="base" category="Orders">
                  <fixr:structure>
                    <fixr:componentRef id="1024" presence="required"/>
                    <fixr:fieldRef id="54" presence="required" scenario="auction"/>
                    <fixr:componentRef id="1025" presence="required"/>
                  </fixr:structure>
                </fixr:message>
              </fixr:messages>
            </fixr:repository>
            """;

        DictionaryLoadResult result = Load(xml, new FixDictionaryOptions());

        Assert.That(result.Dictionary, Is.Not.Null);
        FixDictionary dictionary = result.Dictionary!;
        FixMessageDefinition auction = dictionary.Messages.Single(message => message.Scenario == "auction");
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[] { "orchestra-scenario-definitions-deferred", "duplicate-message-id" }));
            Assert.That(result.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(diagnostic => diagnostic.Severity == ValidationSeverity.Warning));
            Assert.That(dictionary.Identity.BeginString, Is.EqualTo("FIX.4.4"));
            Assert.That(dictionary.Fields.Single(field => field.Tag == 54).CodeSetName, Is.EqualTo("SideCodeSet"));
            Assert.That(dictionary.Fields.Single(field => field.Tag == 54).FieldType, Is.EqualTo(FixFieldType.Char));
            Assert.That(dictionary.Fields.Exists(field => field.Name == "AuctionSide"), Is.False);
            Assert.That(dictionary.Messages.ConvertAll(message => message.Scenario), Is.EqualTo(new[] { "base", "auction" }));
            Assert.That(auction.Members.Single().Scenario, Is.EqualTo("auction"));
            Assert.That(dictionary.Scenarios.Single(scenario => scenario.Name == "auction").ParentName, Is.EqualTo("base"));
            Assert.That(dictionary.Header.Single().ReferenceId, Is.EqualTo(8));
            Assert.That(dictionary.Trailer.Single().ReferenceId, Is.EqualTo(10));
        });
    }

    [Test]
    public void UnsupportedNamespaceAndReferencePolicyProduceStructuredDiagnostics()
    {
        DictionaryLoadResult unsupported = Load("<repository xmlns='urn:example' name='x' version='x'/>", new FixDictionaryOptions());
        string unresolvedXml = """
            <repository xmlns="http://fixprotocol.io/2026/orchestra/repository" name="partial" version="FIX.4.4">
              <metadata/>
              <datatypes><datatype name="String"/></datatypes>
              <fields/>
              <messages>
                <message id="1" name="Partial" msgType="U1">
                  <structure><fieldRef id="9001" presence="required"/></structure>
                </message>
              </messages>
            </repository>
            """;
        DictionaryLoadResult strict = Load(unresolvedXml, new FixDictionaryOptions());
        DictionaryLoadResult permissive = Load(unresolvedXml, new FixDictionaryOptions { RequireResolvedReferences = false });
        string duplicateXml = """
            <repository xmlns="http://fixprotocol.io/2020/orchestra/repository" name="duplicate" version="FIX.4.4">
              <metadata/>
              <datatypes><datatype name="String"/></datatypes>
              <fields>
                <field id="54" name="Side" type="String"/>
                <field id="55" name="Side" type="String"/>
              </fields>
            </repository>
            """;
        DictionaryLoadResult duplicate = Load(duplicateXml, new FixDictionaryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupported.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[] { "unsupported-orchestra-namespace" }));
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostics.Exists(diagnostic => diagnostic.Code == "unresolved-member-reference" && diagnostic.Severity == ValidationSeverity.Error), Is.True);
            Assert.That(permissive.Success, Is.True, string.Join(",", permissive.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(permissive.Diagnostics.Exists(diagnostic => diagnostic.Code == "unresolved-member-reference" && diagnostic.Severity == ValidationSeverity.Warning), Is.True);
            Assert.That(duplicate.Success, Is.False);
            Assert.That(duplicate.Diagnostics.Exists(diagnostic => diagnostic.Code == "duplicate-field-name" && diagnostic.Severity == ValidationSeverity.Error), Is.True);
        });
    }

    [Test]
    public void OfficialLatestVersion10RepositoryLoadsRepresentativeDefinitions()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../specs/fix/FIXOrchestra_FIX.Latest_EP309/FIXOrchestra_FIX.Latest_EP309.xml"));
        using FileStream source = File.OpenRead(path);

        DictionaryLoadResult result = new OrchestraDictionaryLoader().LoadDictionary(source, new FixDictionaryOptions());

        Assert.That(result.Dictionary, Is.Not.Null);
        FixDictionary dictionary = result.Dictionary!;
        FixMessageDefinition? order = dictionary.Messages.Find(message => message.MsgType == "D");
        FixFieldDefinition? side = dictionary.Fields.Find(field => field.Tag == 54);
        FixGroupDefinition? parties = dictionary.Groups.Find(group => group.CountTag == 453);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code + ":" + diagnostic.DefinitionName).Distinct()));
            Assert.That(dictionary.Identity.Name, Is.EqualTo("FIX.Latest"));
            Assert.That(dictionary.Identity.ApplicationVersion, Is.EqualTo("FIX.Latest"));
            Assert.That(dictionary.Identity.ExtensionPack, Is.EqualTo(309));
            Assert.That(dictionary.Datatypes, Has.Count.GreaterThan(30));
            Assert.That(dictionary.CodeSets, Has.Count.GreaterThan(600));
            Assert.That(dictionary.Fields, Has.Count.GreaterThan(6000));
            Assert.That(dictionary.Components, Has.Count.GreaterThan(150));
            Assert.That(dictionary.Groups, Has.Count.GreaterThan(500));
            Assert.That(dictionary.Messages, Has.Count.GreaterThan(170));
            Assert.That(side, Is.Not.Null);
            Assert.That(side!.CodeSetName, Is.EqualTo("SideCodeSet"));
            Assert.That(order, Is.Not.Null);
            Assert.That(order!.Name, Is.EqualTo("NewOrderSingle"));
            Assert.That(parties, Is.Not.Null);
            Assert.That(parties!.DelimiterTag, Is.EqualTo(448));
            Assert.That(dictionary.Header[0].ReferenceId, Is.EqualTo(8));
            Assert.That(dictionary.Trailer[^1].ReferenceId, Is.EqualTo(10));
            Assert.That(result.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(diagnostic => diagnostic.Severity == ValidationSeverity.Warning));
        });
    }

    private static DictionaryLoadResult Load(string xml, FixDictionaryOptions options)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using MemoryStream source = new MemoryStream(bytes);
        return new OrchestraDictionaryLoader().LoadDictionary(source, options);
    }
}
