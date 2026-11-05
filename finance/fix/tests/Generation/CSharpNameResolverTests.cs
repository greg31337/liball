using fix;

namespace fix.Tests;

[TestFixture]
public class CSharpNameResolverTests
{
    [Test]
    public void ResolverHandlesTagsMsgTypesCodesReservedWordsInvalidTextAndScenarios()
    {
        FixDictionary dictionary = CreateDictionary();
        ModelGenerationOptions options = new ModelGenerationOptions { Namespace = "venue.Generated" };

        CSharpNamePlan plan = CSharpNameResolver.ResolveNames(dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(plan.RootNamespace, Is.EqualTo("venue.Generated"));
            Assert.That(plan.VersionNamespace, Is.EqualTo("venue.Generated.FixLatestEp309Venue2"));
            Assert.That(plan.FieldNames[1], Is.EqualTo("ClassValue"));
            Assert.That(plan.FieldNames[2], Is.EqualTo("OrderId"));
            Assert.That(plan.FieldNames[3], Is.EqualTo("OrderIDTag3"));
            Assert.That(plan.FieldNames[4], Is.EqualTo("OrderIDTag4"));
            Assert.That(plan.FieldNames[5], Is.EqualTo("Field5123Price"));
            Assert.That(plan.MessageNames[CSharpNameResolver.CreateMessageKey("A/B", "base")], Is.EqualTo("ClassType"));
            Assert.That(plan.MessageNames[CSharpNameResolver.CreateMessageKey("D", "base")], Is.EqualTo("NewOrder"));
            Assert.That(plan.MessageNames[CSharpNameResolver.CreateMessageKey("D", "auction-day")], Is.EqualTo("NewOrderMsgTypeDScenarioAuctionDay"));
            Assert.That(plan.MessageNames[CSharpNameResolver.CreateMessageKey("F", "base")], Is.EqualTo("NewOrderMsgTypeF"));
            Assert.That(plan.CodeSetNames[CSharpNameResolver.NormalizeNameKey("side-code")], Is.EqualTo("SideCode"));
            Assert.That(plan.CodeNames[CSharpNameResolver.CreateCodeKey("side-code", "1")], Is.EqualTo("ClassValue"));
            Assert.That(plan.CodeNames[CSharpNameResolver.CreateCodeKey("side-code", "2")], Is.EqualTo("BuySide"));
            Assert.That(plan.CodeNames[CSharpNameResolver.CreateCodeKey("side-code", "3")], Is.EqualTo("BuySideValue3"));
            Assert.That(plan.ScenarioNames["BASE"], Is.EqualTo("BaseScenario"));
            Assert.That(plan.ScenarioNames["AUCTION-DAY"], Is.EqualTo("AuctionDay"));
            Assert.That(plan.ScenarioNamespaces["BASE"], Is.EqualTo(plan.VersionNamespace));
            Assert.That(plan.ScenarioNamespaces["AUCTION-DAY"], Is.EqualTo(plan.VersionNamespace + ".AuctionDay"));
            Assert.That(plan.Diagnostics.Select(item => item.Code), Does.Contain("reserved-identifier"));
            Assert.That(plan.Diagnostics.Select(item => item.Code), Does.Contain("identifier-normalized"));
            Assert.That(plan.Diagnostics.Select(item => item.Code), Does.Contain("identifier-collision"));
            Assert.That(dictionary.Fields.Select(item => item.Name), Is.EqualTo(new[] { "class", "order-id", "Order ID", "Order-ID", "123Price" }));
        });
    }

    [Test]
    public void ResolutionIsIndependentOfDefinitionInsertionOrder()
    {
        FixDictionary firstDictionary = CreateDictionary();
        FixDictionary secondDictionary = CreateDictionary();
        secondDictionary.Fields.Reverse();
        secondDictionary.Messages.Reverse();
        secondDictionary.CodeSets.Reverse();
        secondDictionary.CodeSets[0].Codes.Reverse();
        secondDictionary.Scenarios.Reverse();
        ModelGenerationOptions options = new ModelGenerationOptions { Namespace = "venue.Generated" };

        CSharpNamePlan first = CSharpNameResolver.ResolveNames(firstDictionary, options);
        CSharpNamePlan second = CSharpNameResolver.ResolveNames(secondDictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(PlanText(second), Is.EqualTo(PlanText(first)));
            Assert.That(second.Diagnostics.Select(DiagnosticText), Is.EqualTo(first.Diagnostics.Select(DiagnosticText)));
            Assert.That(secondDictionary.Fields[0].Tag, Is.EqualTo(5));
            Assert.That(secondDictionary.Messages[0].MsgType, Is.EqualTo("F"));
        });
    }

    [Test]
    public void VersionAndRootNamespacesArePortableAndStable()
    {
        ModelGenerationOptions normal = new ModelGenerationOptions { Namespace = "Fix.Generated" };
        ModelGenerationOptions unusual = new ModelGenerationOptions { Namespace = "123 venue.namespace" };
        CSharpNamePlan classic = ResolveIdentity(new FixDictionaryIdentity { BeginString = FixBeginStrings.Fix44 }, normal);
        CSharpNamePlan transport = ResolveIdentity(new FixDictionaryIdentity { BeginString = FixBeginStrings.Fixt11 }, normal);
        CSharpNamePlan servicePack = ResolveIdentity(new FixDictionaryIdentity { ApplicationVersion = FixApplicationVersions.Fix50Sp2 }, normal);
        CSharpNamePlan textualServicePack = ResolveIdentity(new FixDictionaryIdentity { ApplicationVersion = "FIX.5.0 SP2" }, normal);
        CSharpNamePlan latest = ResolveIdentity(new FixDictionaryIdentity { ApplicationVersion = "FIX.Latest", HasExtensionPack = true, ExtensionPack = 309 }, normal);
        CSharpNamePlan invalidRoot = ResolveIdentity(new FixDictionaryIdentity { ApplicationVersion = FixApplicationVersions.Fix44 }, unusual);

        Assert.Multiple(() =>
        {
            Assert.That(classic.VersionNamespace, Is.EqualTo("Fix.Generated.Fix44"));
            Assert.That(transport.VersionNamespace, Is.EqualTo("Fix.Generated.Fixt11"));
            Assert.That(servicePack.VersionNamespace, Is.EqualTo("Fix.Generated.Fix50Sp2"));
            Assert.That(textualServicePack.VersionNamespace, Is.EqualTo(servicePack.VersionNamespace));
            Assert.That(latest.VersionNamespace, Is.EqualTo("Fix.Generated.FixLatestEp309"));
            Assert.That(invalidRoot.RootNamespace, Is.EqualTo("Generated123Venue.namespaceNamespace"));
            Assert.That(invalidRoot.VersionNamespace, Is.EqualTo("Generated123Venue.namespaceNamespace.Fix44"));
            Assert.That(invalidRoot.Diagnostics.Select(item => item.Code), Does.Contain("namespace-normalized"));
        });
    }

    [Test]
    public void MissingInputsReturnOrderedErrorsInsteadOfThrowing()
    {
        CSharpNamePlan missingDictionary = CSharpNameResolver.ResolveNames(null!, new ModelGenerationOptions());
        CSharpNamePlan missingOptions = CSharpNameResolver.ResolveNames(new FixDictionary(), null!);

        Assert.Multiple(() =>
        {
            Assert.That(missingDictionary.Diagnostics.Single().Code, Is.EqualTo("missing-name-dictionary"));
            Assert.That(missingDictionary.Diagnostics.Single().Severity, Is.EqualTo(ValidationSeverity.Error));
            Assert.That(missingOptions.Diagnostics.Single().Code, Is.EqualTo("missing-name-options"));
            Assert.That(missingOptions.Diagnostics.Single().Severity, Is.EqualTo(ValidationSeverity.Error));
        });
    }

    [Test]
    public void LatestDictionaryProducesACompletePortableCollisionFreePlan()
    {
        DictionaryLoadResult loaded = new EmbeddedDictionaryProvider(null).ResolveDictionary(new FixDictionaryIdentity { Name = "FIX.Latest" });
        Assert.That(loaded.Success, Is.True, string.Join(",", loaded.Diagnostics.Select(item => item.Code)));
        FixDictionary dictionary = loaded.Dictionary!;

        CSharpNamePlan plan = CSharpNameResolver.ResolveNames(dictionary, new ModelGenerationOptions { Namespace = "Fix.Generated" });
        List<string> typeNames = [];
        typeNames.AddRange(plan.CodeSetNames.Values);
        typeNames.AddRange(plan.ComponentNames.Values);
        typeNames.AddRange(plan.GroupNames.Values);
        typeNames.AddRange(plan.MessageNames.Values);

        Assert.Multiple(() =>
        {
            Assert.That(plan.VersionNamespace, Is.EqualTo("Fix.Generated.FixLatestEp309"));
            Assert.That(plan.FieldNames, Has.Count.EqualTo(6203));
            Assert.That(plan.CodeSetNames, Has.Count.EqualTo(694));
            Assert.That(plan.CodeNames, Has.Count.EqualTo(5745));
            Assert.That(plan.ComponentNames, Has.Count.EqualTo(165));
            Assert.That(plan.GroupNames, Has.Count.EqualTo(580));
            Assert.That(plan.MessageNames, Has.Count.EqualTo(181));
            Assert.That(plan.FieldNames.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), Has.Length.EqualTo(plan.FieldNames.Count));
            Assert.That(typeNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), Has.Length.EqualTo(typeNames.Count));
            Assert.That(plan.FieldNames.Values, Is.All.Matches<string>(IsPortableIdentifier));
            Assert.That(typeNames, Is.All.Matches<string>(IsPortableIdentifier));
            Assert.That(plan.CodeNames.Values, Is.All.Matches<string>(IsPortableIdentifier));
            Assert.That(plan.Diagnostics.Exists(item => item.Severity == ValidationSeverity.Error || item.Severity == ValidationSeverity.Fatal), Is.False);
        });
    }

    private static FixDictionary CreateDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity
            {
                ApplicationVersion = FixApplicationVersions.FixLatest,
                HasExtensionPack = true,
                ExtensionPack = 309,
                CustomVersion = "venue-2"
            },
            Fields =
            [
                new FixFieldDefinition { Tag = 1, Name = "class" },
                new FixFieldDefinition { Tag = 2, Name = "order-id" },
                new FixFieldDefinition { Tag = 3, Name = "Order ID" },
                new FixFieldDefinition { Tag = 4, Name = "Order-ID" },
                new FixFieldDefinition { Tag = 5, Name = "123Price" }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 10,
                    Name = "side-code",
                    Codes =
                    [
                        new FixCodeDefinition { Name = "class", Value = "1" },
                        new FixCodeDefinition { Name = "buy-side", Value = "2" },
                        new FixCodeDefinition { Name = "buy side", Value = "3" }
                    ]
                }
            ],
            Messages =
            [
                new FixMessageDefinition { Id = 1, Name = "class", MsgType = "A/B", Scenario = "base" },
                new FixMessageDefinition { Id = 2, Name = "NewOrder", MsgType = "D", Scenario = "base" },
                new FixMessageDefinition { Id = 3, Name = "NewOrder", MsgType = "D", Scenario = "auction-day" },
                new FixMessageDefinition { Id = 4, Name = "new-order", MsgType = "F", Scenario = "base" }
            ],
            Scenarios =
            [
                new FixScenarioDefinition { Name = "base" },
                new FixScenarioDefinition { Name = "auction-day", ParentName = "base" }
            ]
        };
    }

    private static CSharpNamePlan ResolveIdentity(FixDictionaryIdentity identity, ModelGenerationOptions options)
    {
        return CSharpNameResolver.ResolveNames(new FixDictionary { Identity = identity }, options);
    }

    private static string PlanText(CSharpNamePlan plan)
    {
        List<string> values = [plan.RootNamespace, plan.VersionNamespace];
        values.AddRange(plan.FieldNames.OrderBy(item => item.Key).Select(item => "F:" + item.Key + ":" + item.Value));
        values.AddRange(plan.MessageNames.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => "M:" + item.Key + ":" + item.Value));
        values.AddRange(plan.CodeSetNames.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => "S:" + item.Key + ":" + item.Value));
        values.AddRange(plan.CodeNames.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => "C:" + item.Key + ":" + item.Value));
        values.AddRange(plan.ScenarioNames.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => "R:" + item.Key + ":" + item.Value));
        values.AddRange(plan.ScenarioNamespaces.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => "N:" + item.Key + ":" + item.Value));
        return string.Join(Environment.NewLine, values);
    }

    private static string DiagnosticText(GenerationDiagnostic diagnostic)
    {
        return diagnostic.Severity + ":" + diagnostic.Code + ":" + diagnostic.Source;
    }

    private static bool IsPortableIdentifier(string value)
    {
        if (value.Length == 0 || !(value[0] == '_' || value[0] >= 'A' && value[0] <= 'Z' || value[0] >= 'a' && value[0] <= 'z')) return false;
        for (int index = 1; index < value.Length; index++)
        {
            char character = value[index];
            if (!(character == '_' || character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9')) return false;
        }
        return !CSharpNameResolver.IsReservedIdentifier(value);
    }
}
