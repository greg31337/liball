using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class QuickFixDictionaryLoaderTests
{
    [Test]
    public void LoaderMapsFieldsEnumsComponentsNestedGroupsMessagesAndEnvelopeSections()
    {
        string xml = """
            <fix type='FIX' major='4' minor='4' servicepack='0'>
              <header>
                <field name='BeginString' required='Y'/>
                <field name='BodyLength' required='Y'/>
                <field name='MsgType' required='Y'/>
              </header>
              <messages>
                <message name='VenueOrder' msgtype='U1' msgcat='app'>
                  <component name='OrderCore' required='Y'/>
                  <field name='Side' required='Y'/>
                  <field name='RawDataLength' required='N'/>
                  <field name='RawData' required='N'/>
                  <group name='NoPartyIDs' required='N'>
                    <field name='PartyID' required='Y'/>
                    <group name='NoPartySubIDs' required='N'>
                      <field name='PartySubID' required='Y'/>
                    </group>
                  </group>
                </message>
              </messages>
              <trailer>
                <field name='CheckSum' required='Y'/>
              </trailer>
              <components>
                <component name='OrderCore'>
                  <field name='ClOrdID' required='Y'/>
                  <field name='VenueID' required='N'/>
                </component>
              </components>
              <fields>
                <field number='8' name='BeginString' type='STRING'/>
                <field number='9' name='BodyLength' type='LENGTH'/>
                <field number='10' name='CheckSum' type='STRING'/>
                <field number='11' name='ClOrdID' type='STRING'/>
                <field number='35' name='MsgType' type='STRING'/>
                <field number='54' name='Side' type='CHAR'>
                  <value enum='1' description='BUY'/>
                  <value enum='2' description='SELL'/>
                </field>
                <field number='95' name='RawDataLength' type='LENGTH'/>
                <field number='96' name='RawData' type='DATA'/>
                <field number='448' name='PartyID' type='STRING'/>
                <field number='453' name='NoPartyIDs' type='NUMINGROUP'/>
                <field number='523' name='PartySubID' type='STRING'/>
                <field number='802' name='NoPartySubIDs' type='NUMINGROUP'/>
                <field number='9001' name='VenueID' type='STRING'/>
              </fields>
            </fix>
            """;

        DictionaryLoadResult result = Load(xml, new FixDictionaryOptions());

        Assert.That(result.Success, Is.True, string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
        Assert.That(result.Dictionary, Is.Not.Null);
        FixDictionary dictionary = result.Dictionary!;
        FixFieldDefinition side = dictionary.Fields.Find(field => field.Tag == 54)!;
        FixFieldDefinition length = dictionary.Fields.Find(field => field.Tag == 95)!;
        FixFieldDefinition data = dictionary.Fields.Find(field => field.Tag == 96)!;
        FixMessageDefinition message = dictionary.Messages[0];
        FixGroupDefinition parties = dictionary.Groups.Find(group => group.CountTag == 453)!;
        FixGroupDefinition subParties = dictionary.Groups.Find(group => group.CountTag == 802)!;

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.Identity.Name, Is.EqualTo("FIX44"));
            Assert.That(dictionary.Identity.BeginString, Is.EqualTo("FIX.4.4"));
            Assert.That(dictionary.Identity.ApplicationVersion, Is.EqualTo("FIX.4.4"));
            Assert.That(dictionary.Header.ConvertAll(member => member.ReferenceId), Is.EqualTo(new[] { 8, 9, 35 }));
            Assert.That(dictionary.Header.ConvertAll(member => member.Presence), Is.All.EqualTo(FixPresence.Required));
            Assert.That(dictionary.Trailer[0].ReferenceId, Is.EqualTo(10));
            Assert.That(dictionary.Components, Has.Count.EqualTo(1));
            Assert.That(dictionary.Components[0].Name, Is.EqualTo("OrderCore"));
            Assert.That(dictionary.Components[0].Members.ConvertAll(member => member.ReferenceId), Is.EqualTo(new[] { 11, 9001 }));
            Assert.That(message.Name, Is.EqualTo("VenueOrder"));
            Assert.That(message.MsgType, Is.EqualTo("U1"));
            Assert.That(message.Category, Is.EqualTo("app"));
            Assert.That(message.Members.ConvertAll(member => member.Kind), Is.EqualTo(new[]
            {
                FixMemberKind.Component,
                FixMemberKind.Field,
                FixMemberKind.Field,
                FixMemberKind.Field,
                FixMemberKind.Group
            }));
            Assert.That(parties.DelimiterTag, Is.EqualTo(448));
            Assert.That(parties.Members.ConvertAll(member => member.Kind), Is.EqualTo(new[] { FixMemberKind.Field, FixMemberKind.Group }));
            Assert.That(subParties.DelimiterTag, Is.EqualTo(523));
            Assert.That(side.CodeSetName, Is.EqualTo("SideCodeSet"));
            Assert.That(dictionary.CodeSets[0].Codes.ConvertAll(code => code.Value), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(dictionary.CodeSets[0].Codes.ConvertAll(code => code.Name), Is.EqualTo(new[] { "BUY", "SELL" }));
            Assert.That(length.HasDataTag, Is.True);
            Assert.That(length.DataTag, Is.EqualTo(96));
            Assert.That(data.HasLengthTag, Is.True);
            Assert.That(data.LengthTag, Is.EqualTo(95));
            Assert.That(dictionary.Fields.Exists(field => field.Tag == 9001), Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void OfficialFix42DictionaryLoadsWithRepresentativeDefinitionsResolved()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../specs/fix/quickfix-dictionaries/FIX42.xml"));
        using FileStream source = File.OpenRead(path);

        DictionaryLoadResult result = new QuickFixDictionaryLoader().LoadDictionary(source, new FixDictionaryOptions());

        Assert.That(result.Success, Is.True, string.Join(",", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Code).Distinct()));
        Assert.That(result.Dictionary, Is.Not.Null);
        FixDictionary dictionary = result.Dictionary!;
        FixMessageDefinition? newOrder = dictionary.Messages.Find(message => message.MsgType == "D");
        FixFieldDefinition? side = dictionary.Fields.Find(field => field.Tag == 54);
        FixFieldDefinition? encodedText = dictionary.Fields.Find(field => field.Tag == 355);

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.Identity.Name, Is.EqualTo("FIX42"));
            Assert.That(dictionary.Fields.Count, Is.GreaterThan(400));
            Assert.That(dictionary.Messages.Count, Is.GreaterThan(40));
            Assert.That(dictionary.Groups.Count, Is.GreaterThan(10));
            Assert.That(newOrder, Is.Not.Null);
            Assert.That(newOrder!.Name, Is.EqualTo("NewOrderSingle"));
            Assert.That(side, Is.Not.Null);
            Assert.That(side!.CodeSetName, Is.EqualTo("SideCodeSet"));
            Assert.That(encodedText, Is.Not.Null);
            Assert.That(encodedText!.HasLengthTag, Is.True);
            Assert.That(encodedText.LengthTag, Is.EqualTo(354));
            Assert.That(dictionary.Header[0].ReferenceId, Is.EqualTo(8));
            Assert.That(dictionary.Trailer[^1].ReferenceId, Is.EqualTo(10));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void UnresolvedReferencesFollowStrictAndPermissivePolicies()
    {
        string xml = """
            <fix type='FIX' major='4' minor='4' servicepack='0'>
              <header><field name='BeginString' required='Y'/></header>
              <messages>
                <message name='Broken' msgtype='U2' msgcat='app'>
                  <field name='MissingField' required='Y'/>
                  <component name='MissingComponent' required='N'/>
                </message>
              </messages>
              <trailer><field name='CheckSum' required='Y'/></trailer>
              <components/>
              <fields>
                <field number='8' name='BeginString' type='STRING'/>
                <field number='10' name='CheckSum' type='STRING'/>
              </fields>
            </fix>
            """;

        DictionaryLoadResult strict = Load(xml, new FixDictionaryOptions());
        DictionaryLoadResult permissive = Load(xml, new FixDictionaryOptions { RequireResolvedReferences = false });

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostics.Exists(diagnostic => diagnostic.Code == "unknown-field-reference" && diagnostic.Severity == ValidationSeverity.Error), Is.True);
            Assert.That(strict.Diagnostics.Exists(diagnostic => diagnostic.Code == "unknown-component-reference" && diagnostic.Severity == ValidationSeverity.Error), Is.True);
            Assert.That(permissive.Success, Is.True, string.Join(",", permissive.Diagnostics.ConvertAll(diagnostic => diagnostic.Code)));
            Assert.That(permissive.Diagnostics.Exists(diagnostic => diagnostic.Code == "unknown-field-reference" && diagnostic.Severity == ValidationSeverity.Warning), Is.True);
            Assert.That(permissive.Diagnostics.Exists(diagnostic => diagnostic.Code == "unknown-component-reference" && diagnostic.Severity == ValidationSeverity.Warning), Is.True);
        });
    }

    [Test]
    public void InvalidRootAndDefinitionLimitReturnStructuredFailures()
    {
        DictionaryLoadResult root = Load("<dictionary/>", new FixDictionaryOptions());
        string xml = """
            <fix type='FIX' major='4' minor='4' servicepack='0'>
              <header><field name='BeginString' required='Y'/></header>
              <messages/>
              <trailer><field name='CheckSum' required='Y'/></trailer>
              <components/>
              <fields>
                <field number='8' name='BeginString' type='STRING'/>
                <field number='10' name='CheckSum' type='STRING'/>
              </fields>
            </fix>
            """;
        DictionaryLoadResult limited = Load(xml, new FixDictionaryOptions { MaxDefinitions = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(root.Success, Is.False);
            Assert.That(root.Dictionary, Is.Not.Null);
            Assert.That(root.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[] { "invalid-quickfix-root" }));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Diagnostics.Exists(diagnostic => diagnostic.Code == "definition-limit-exceeded" && diagnostic.Severity == ValidationSeverity.Fatal), Is.True);
        });
    }

    private static DictionaryLoadResult Load(string xml, FixDictionaryOptions options)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        using MemoryStream source = new MemoryStream(bytes);
        return new QuickFixDictionaryLoader().LoadDictionary(source, options);
    }
}
