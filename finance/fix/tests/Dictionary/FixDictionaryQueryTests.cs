using fix;

namespace fix.Tests;

[TestFixture]
public class FixDictionaryQueryTests
{
    [Test]
    public void QueryResolvesEveryContractCategoryDeterministically()
    {
        FixFieldDefinition field = new FixFieldDefinition { Tag = 11, Name = "ClOrdID" };
        FixMessageDefinition baseMessage = new FixMessageDefinition { Id = 1, Name = "Order", MsgType = "D", Scenario = "base" };
        FixMessageDefinition alternateMessage = new FixMessageDefinition { Id = 2, Name = "OrderAlt", MsgType = "D", Scenario = "alternate" };
        FixComponentDefinition component = new FixComponentDefinition { Id = 100, Name = "OrderCore" };
        FixGroupDefinition group = new FixGroupDefinition { Id = 200, Name = "Parties" };
        FixCodeSetDefinition codeSet = new FixCodeSetDefinition { Id = 54, Name = " SideCodeSet " };
        FixScenarioDefinition scenario = new FixScenarioDefinition { Name = " Base " };
        FixDictionary dictionary = new FixDictionary
        {
            Fields = [field],
            Messages = [baseMessage, alternateMessage],
            Components = [component],
            Groups = [group],
            CodeSets = [codeSet],
            Scenarios = [scenario]
        };
        IDictionaryQuery query = new FixDictionaryQuery();

        FixFieldDefinition? resolvedField = query.ResolveFieldDefinition(dictionary, 11);
        FixMessageDefinition? resolvedBase = query.ResolveMessageDefinition(dictionary, "D", " BASE ");
        FixMessageDefinition? resolvedAlternate = query.ResolveMessageDefinition(dictionary, "D", "alternate");
        FixComponentDefinition? resolvedComponent = query.ResolveComponentDefinition(dictionary, 100);
        FixGroupDefinition? resolvedGroup = query.ResolveGroupDefinition(dictionary, 200);
        FixCodeSetDefinition? resolvedCodeSet = query.ResolveCodeSetDefinition(dictionary, "sidecodeset");
        FixScenarioDefinition? resolvedScenario = query.ResolveScenarioDefinition(dictionary, "base");

        Assert.Multiple(() =>
        {
            Assert.That(resolvedField, Is.SameAs(field));
            Assert.That(resolvedBase, Is.SameAs(baseMessage));
            Assert.That(resolvedAlternate, Is.SameAs(alternateMessage));
            Assert.That(resolvedComponent, Is.SameAs(component));
            Assert.That(resolvedGroup, Is.SameAs(group));
            Assert.That(resolvedCodeSet, Is.SameAs(codeSet));
            Assert.That(resolvedScenario, Is.SameAs(scenario));
            Assert.That(dictionary.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void MissingLookupsAddOneStableDiagnosticPerRequest()
    {
        FixDictionary dictionary = new FixDictionary();
        FixDictionaryQuery query = new FixDictionaryQuery();

        Assert.That(query.ResolveFieldDefinition(dictionary, 999), Is.Null);
        Assert.That(query.ResolveMessageDefinition(dictionary, "Z", "base"), Is.Null);
        Assert.That(query.ResolveComponentDefinition(dictionary, 999), Is.Null);
        Assert.That(query.ResolveGroupDefinition(dictionary, 999), Is.Null);
        Assert.That(query.ResolveCodeSetDefinition(dictionary, "MissingCodes"), Is.Null);
        Assert.That(query.ResolveScenarioDefinition(dictionary, "missing"), Is.Null);
        Assert.That(query.ResolveFieldDefinition(dictionary, 999), Is.Null);
        Assert.That(query.ResolveMessageDefinition(dictionary, "Z", "base"), Is.Null);

        Assert.Multiple(() =>
        {
            Assert.That(dictionary.Diagnostics, Has.Count.EqualTo(6));
            Assert.That(dictionary.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[]
            {
                "missing-field-definition",
                "missing-message-definition",
                "missing-component-definition",
                "missing-group-definition",
                "missing-code-set-definition",
                "missing-scenario-definition"
            }));
            Assert.That(dictionary.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(diagnostic => diagnostic.Severity == ValidationSeverity.Error));
            Assert.That(dictionary.Diagnostics[0].HasTag, Is.True);
            Assert.That(dictionary.Diagnostics[0].Tag, Is.EqualTo(999));
        });
    }

    [Test]
    public void AmbiguousLookupsReturnTheFirstSourceDefinitionAndWarnOnce()
    {
        FixFieldDefinition firstField = new FixFieldDefinition { Tag = 11, Name = "FirstField" };
        FixMessageDefinition firstMessage = new FixMessageDefinition { Id = 1, Name = "FirstMessage", MsgType = "D", Scenario = "base" };
        FixComponentDefinition firstComponent = new FixComponentDefinition { Id = 100, Name = "FirstComponent" };
        FixGroupDefinition firstGroup = new FixGroupDefinition { Id = 200, Name = "FirstGroup" };
        FixCodeSetDefinition firstCodes = new FixCodeSetDefinition { Id = 1, Name = "Side" };
        FixScenarioDefinition firstScenario = new FixScenarioDefinition { Name = "base" };
        FixDictionary dictionary = new FixDictionary
        {
            Options = new FixDictionaryOptions { AllowDuplicateDefinitions = true },
            Fields = [firstField, new FixFieldDefinition { Tag = 11, Name = "SecondField" }],
            Messages = [firstMessage, new FixMessageDefinition { Id = 2, Name = "SecondMessage", MsgType = "D", Scenario = " BASE " }],
            Components = [firstComponent, new FixComponentDefinition { Id = 100, Name = "SecondComponent" }],
            Groups = [firstGroup, new FixGroupDefinition { Id = 200, Name = "SecondGroup" }],
            CodeSets = [firstCodes, new FixCodeSetDefinition { Id = 2, Name = " side " }],
            Scenarios = [firstScenario, new FixScenarioDefinition { Name = " BASE " }]
        };
        FixDictionaryQuery query = new FixDictionaryQuery();

        FixFieldDefinition? field = query.ResolveFieldDefinition(dictionary, 11);
        FixMessageDefinition? message = query.ResolveMessageDefinition(dictionary, "D", "base");
        FixComponentDefinition? component = query.ResolveComponentDefinition(dictionary, 100);
        FixGroupDefinition? group = query.ResolveGroupDefinition(dictionary, 200);
        FixCodeSetDefinition? codes = query.ResolveCodeSetDefinition(dictionary, "SIDE");
        FixScenarioDefinition? scenario = query.ResolveScenarioDefinition(dictionary, "base");
        query.ResolveFieldDefinition(dictionary, 11);
        query.ResolveCodeSetDefinition(dictionary, " side ");
        query.ResolveScenarioDefinition(dictionary, " BASE ");
        query.ResolveMessageDefinition(dictionary, "D", " BASE ");

        Assert.Multiple(() =>
        {
            Assert.That(field, Is.SameAs(firstField));
            Assert.That(message, Is.SameAs(firstMessage));
            Assert.That(component, Is.SameAs(firstComponent));
            Assert.That(group, Is.SameAs(firstGroup));
            Assert.That(codes, Is.SameAs(firstCodes));
            Assert.That(scenario, Is.SameAs(firstScenario));
            Assert.That(dictionary.Diagnostics, Has.Count.EqualTo(6));
            Assert.That(dictionary.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[]
            {
                "ambiguous-field-definition",
                "ambiguous-message-definition",
                "ambiguous-component-definition",
                "ambiguous-group-definition",
                "ambiguous-code-set-definition",
                "ambiguous-scenario-definition"
            }));
            Assert.That(dictionary.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(diagnostic => diagnostic.Severity == ValidationSeverity.Warning));
        });
    }

    [Test]
    public void DisabledNameNormalizationRequiresExactNamesAndScenarios()
    {
        FixCodeSetDefinition codes = new FixCodeSetDefinition { Name = "Side" };
        FixScenarioDefinition scenario = new FixScenarioDefinition { Name = "base" };
        FixMessageDefinition message = new FixMessageDefinition { Name = "Order", MsgType = "D", Scenario = "base" };
        FixDictionary dictionary = new FixDictionary
        {
            Options = new FixDictionaryOptions { NormalizeNames = false },
            CodeSets = [codes],
            Scenarios = [scenario],
            Messages = [message]
        };
        FixDictionaryQuery query = new FixDictionaryQuery();

        Assert.Multiple(() =>
        {
            Assert.That(query.ResolveCodeSetDefinition(dictionary, "Side"), Is.SameAs(codes));
            Assert.That(query.ResolveScenarioDefinition(dictionary, "base"), Is.SameAs(scenario));
            Assert.That(query.ResolveMessageDefinition(dictionary, "D", "base"), Is.SameAs(message));
            Assert.That(query.ResolveCodeSetDefinition(dictionary, "side"), Is.Null);
            Assert.That(query.ResolveScenarioDefinition(dictionary, "BASE"), Is.Null);
            Assert.That(query.ResolveMessageDefinition(dictionary, "D", "BASE"), Is.Null);
        });
        Assert.That(dictionary.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[]
        {
            "missing-code-set-definition",
            "missing-scenario-definition",
            "missing-message-definition"
        }));
    }
}
