using NUnit.Framework;

namespace fix.Tests;

public class DictionaryIndexCoverageTests
{
    [Test]
    public void EveryDuplicateKeyCategoryKeepsItsFirstIndexedDefinition()
    {
        FixDatatypeDefinition datatype = new FixDatatypeDefinition { Name = "Text", FieldType = FixFieldType.String };
        FixCodeDefinition buy = new FixCodeDefinition { Name = "Buy", Value = "1" };
        FixCodeSetDefinition codeSet = new FixCodeSetDefinition { Id = 1, Name = "Side", Codes = [buy] };
        FixFieldDefinition field = new FixFieldDefinition { Tag = 11, Name = "ClOrdID", Abbreviation = "ClOrd" };
        FixComponentDefinition component = new FixComponentDefinition { Id = 10, Name = "Core", Abbreviation = "Cr" };
        FixGroupDefinition group = new FixGroupDefinition { Id = 20, Name = "Parties", CountTag = 453 };
        FixMessageDefinition message = new FixMessageDefinition { Id = 30, Name = "Order", MsgType = "D", Scenario = "base" };
        FixScenarioDefinition scenario = new FixScenarioDefinition { Name = "base" };
        FixDictionary dictionary = new FixDictionary
        {
            Datatypes = [datatype, new FixDatatypeDefinition { Name = " text " }],
            CodeSets =
            [
                codeSet,
                new FixCodeSetDefinition { Id = 1, Name = "OtherSide" },
                new FixCodeSetDefinition { Id = 2, Name = " side ", Codes = [new FixCodeDefinition { Name = "Sell", Value = "2" }] }
            ],
            Fields =
            [
                field,
                new FixFieldDefinition { Tag = 11, Name = "VenueOrderID" },
                new FixFieldDefinition { Tag = 12, Name = "clordid" }
            ],
            Components =
            [
                component,
                new FixComponentDefinition { Id = 10, Name = "OtherCore" },
                new FixComponentDefinition { Id = 11, Name = "core" },
                new FixComponentDefinition { Id = 12, Name = "ThirdCore", Abbreviation = "cr" }
            ],
            Groups =
            [
                group,
                new FixGroupDefinition { Id = 20, Name = "OtherParties", CountTag = 454 },
                new FixGroupDefinition { Id = 21, Name = "parties", CountTag = 455 },
                new FixGroupDefinition { Id = 22, Name = "ThirdParties", CountTag = 453 }
            ],
            Messages =
            [
                message,
                new FixMessageDefinition { Id = 30, Name = "OtherOrder", MsgType = "E", Scenario = "base" },
                new FixMessageDefinition { Id = 31, Name = "order", MsgType = "F", Scenario = "base" },
                new FixMessageDefinition { Id = 32, Name = "ThirdOrder", MsgType = "D", Scenario = "BASE" }
            ],
            Scenarios = [scenario, new FixScenarioDefinition { Name = " BASE " }]
        };

        DictionaryIndex index = DictionaryNormalizer.NormalizeDictionary(dictionary, new FixDictionaryOptions { AllowDuplicateDefinitions = true, RequireResolvedReferences = false });

        Assert.Multiple(() =>
        {
            Assert.That(index.Success, Is.True);
            Assert.That(index.FindDatatypeByName("text"), Is.SameAs(datatype));
            Assert.That(index.FindCodeSetById(1), Is.SameAs(codeSet));
            Assert.That(index.FindCodeSetByName("side"), Is.SameAs(codeSet));
            Assert.That(index.FindCodeByValue("side", "1"), Is.SameAs(buy));
            Assert.That(index.FindCodeByValue("side", "2"), Is.Null, "codes from the ignored duplicate name must not leak into the first code set");
            Assert.That(index.FindFieldByTag(11), Is.SameAs(field));
            Assert.That(index.FindFieldByName("clordid"), Is.SameAs(field));
            Assert.That(index.FindComponentById(10), Is.SameAs(component));
            Assert.That(index.FindComponentByName("core"), Is.SameAs(component));
            Assert.That(index.FindGroupById(20), Is.SameAs(group));
            Assert.That(index.FindGroupByName("parties"), Is.SameAs(group));
            Assert.That(index.FindGroupByCountTag(453), Is.SameAs(group));
            Assert.That(index.FindMessageById(30), Is.SameAs(message));
            Assert.That(index.FindMessageByName("order", "base"), Is.SameAs(message));
            Assert.That(index.FindMessageByType("D", "base"), Is.SameAs(message));
            Assert.That(index.FindScenarioByName("base"), Is.SameAs(scenario));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-datatype-name"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-code-set-id"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-code-set-name"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-field-tag"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-field-name"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-component-id"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-component-name"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-group-id"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-group-name"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-group-count-tag"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-message-id"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-message-name"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-message-type"));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Does.Contain("duplicate-scenario-name"));
        });
    }

    [Test]
    public void AliasChainsResolveWhileCyclesAndMissingBasesRemainStructuredWarnings()
    {
        FixDatatypeDefinition root = new FixDatatypeDefinition { Name = "Root", FieldType = FixFieldType.String };
        FixDatatypeDefinition middle = new FixDatatypeDefinition { Name = "Middle", BaseTypeName = "Root" };
        FixDatatypeDefinition leaf = new FixDatatypeDefinition { Name = "Leaf", BaseTypeName = "Middle" };
        FixDatatypeDefinition cycleA = new FixDatatypeDefinition { Name = "CycleA", BaseTypeName = "CycleB" };
        FixDatatypeDefinition cycleB = new FixDatatypeDefinition { Name = "CycleB", BaseTypeName = "CycleA" };
        FixDatatypeDefinition missing = new FixDatatypeDefinition { Name = "Missing", BaseTypeName = "Absent" };
        FixFieldDefinition resolved = new FixFieldDefinition { Tag = 1, Name = "Resolved", DatatypeName = "leaf" };
        FixFieldDefinition unresolved = new FixFieldDefinition { Tag = 2, Name = "Unresolved", DatatypeName = "CycleA" };
        FixDictionary dictionary = new FixDictionary
        {
            Datatypes = [root, middle, leaf, cycleA, cycleB, missing],
            Fields = [resolved, unresolved]
        };

        DictionaryIndex index = DictionaryNormalizer.NormalizeDictionary(dictionary, new FixDictionaryOptions { AllowUnknownDatatypes = true });

        Assert.Multiple(() =>
        {
            Assert.That(index.Success, Is.True);
            Assert.That(middle.FieldType, Is.EqualTo(FixFieldType.String));
            Assert.That(leaf.FieldType, Is.EqualTo(FixFieldType.String));
            Assert.That(resolved.FieldType, Is.EqualTo(FixFieldType.String));
            Assert.That(cycleA.FieldType, Is.EqualTo(FixFieldType.Unknown));
            Assert.That(cycleB.FieldType, Is.EqualTo(FixFieldType.Unknown));
            Assert.That(unresolved.FieldType, Is.EqualTo(FixFieldType.Unknown));
            Assert.That(index.Diagnostics.ConvertAll(diagnostic => diagnostic.Code), Is.EqualTo(new[]
            {
                "unresolved-datatype-alias",
                "unresolved-datatype-alias",
                "unresolved-datatype-alias",
                "unresolved-field-datatype"
            }));
            Assert.That(index.Diagnostics, Is.All.Matches<DictionaryDiagnostic>(diagnostic => diagnostic.Severity == ValidationSeverity.Warning));
        });
    }

    [Test]
    public void RecursiveComponentsReportEachCycleWithoutFlaggingSharedAcyclicPaths()
    {
        FixComponentDefinition first = Component(1, "First", 2);
        FixComponentDefinition second = Component(2, "Second", 3);
        FixComponentDefinition third = Component(3, "Third", 2);
        FixComponentDefinition self = Component(4, "Self", 4);
        FixComponentDefinition shared = Component(5, "Shared", 2);
        FixComponentDefinition leaf = new FixComponentDefinition { Id = 6, Name = "Leaf" };
        FixDictionary dictionary = new FixDictionary { Components = [first, second, third, self, shared, leaf] };

        DictionaryIndex index = DictionaryNormalizer.NormalizeDictionary(dictionary, new FixDictionaryOptions { RequireResolvedReferences = false });

        Assert.Multiple(() =>
        {
            Assert.That(index.Success, Is.True);
            Assert.That(index.Diagnostics.Count(diagnostic => diagnostic.Code == "recursive-component-reference"), Is.EqualTo(2));
            Assert.That(index.Diagnostics.Where(diagnostic => diagnostic.Code == "recursive-component-reference").Select(diagnostic => diagnostic.DefinitionName), Is.EqualTo(new[] { "Second", "Self" }));
            Assert.That(index.Diagnostics.Exists(diagnostic => diagnostic.DefinitionName == "Shared"), Is.False);
            Assert.That(index.Diagnostics.Exists(diagnostic => diagnostic.DefinitionName == "Leaf"), Is.False);
        });
    }

    [Test]
    public void LengthDataPairsCompleteInEitherDirectionAndKeepFirstConflictingPair()
    {
        FixFieldDefinition forwardLength = new FixFieldDefinition { Tag = 95, Name = "RawDataLength", FieldType = FixFieldType.Length, HasDataTag = true, DataTag = 96 };
        FixFieldDefinition forwardData = new FixFieldDefinition { Tag = 96, Name = "RawData", FieldType = FixFieldType.Data };
        FixFieldDefinition reverseLength = new FixFieldDefinition { Tag = 97, Name = "XmlDataLength", FieldType = FixFieldType.Length };
        FixFieldDefinition reverseData = new FixFieldDefinition { Tag = 98, Name = "XmlData", FieldType = FixFieldType.XmlData, HasLengthTag = true, LengthTag = 97 };
        FixFieldDefinition conflict = new FixFieldDefinition { Tag = 99, Name = "OtherData", FieldType = FixFieldType.Data, HasLengthTag = true, LengthTag = 95 };
        FixDictionary dictionary = new FixDictionary { Fields = [forwardLength, forwardData, reverseLength, reverseData, conflict] };

        DictionaryIndex index = DictionaryNormalizer.NormalizeDictionary(dictionary, new FixDictionaryOptions());

        Assert.Multiple(() =>
        {
            Assert.That(index.Success, Is.False);
            Assert.That(forwardData.HasLengthTag, Is.True);
            Assert.That(forwardData.LengthTag, Is.EqualTo(95));
            Assert.That(reverseLength.HasDataTag, Is.True);
            Assert.That(reverseLength.DataTag, Is.EqualTo(98));
            Assert.That(index.FindLengthFieldByDataTag(96), Is.SameAs(forwardLength));
            Assert.That(index.FindDataFieldByLengthTag(95), Is.SameAs(forwardData));
            Assert.That(index.FindLengthFieldByDataTag(98), Is.SameAs(reverseLength));
            Assert.That(index.FindDataFieldByLengthTag(97), Is.SameAs(reverseData));
            Assert.That(index.FindLengthFieldByDataTag(99), Is.Null);
            Assert.That(index.Diagnostics.Count(diagnostic => diagnostic.Code == "invalid-length-data-pair" && diagnostic.Tag == 99), Is.EqualTo(1));
        });
    }

    [Test]
    public void ScenarioInheritanceCyclesAndStableQueriesAreDeterministic()
    {
        FixScenarioDefinition root = new FixScenarioDefinition { Name = "root" };
        FixScenarioDefinition child = new FixScenarioDefinition { Name = " child ", ParentName = " ROOT " };
        FixScenarioDefinition cycleA = new FixScenarioDefinition { Name = "cycle-a", ParentName = "cycle-b" };
        FixScenarioDefinition cycleB = new FixScenarioDefinition { Name = "cycle-b", ParentName = "cycle-a" };
        FixScenarioDefinition orphan = new FixScenarioDefinition { Name = "orphan", ParentName = "missing" };
        FixMessageDefinition first = new FixMessageDefinition { Id = 1, Name = "ChildOrder", MsgType = "D", Scenario = "child" };
        FixMessageDefinition duplicate = new FixMessageDefinition { Id = 2, Name = "ChildOrderDuplicate", MsgType = "D", Scenario = " CHILD " };
        FixDictionary dictionary = new FixDictionary
        {
            Options = new FixDictionaryOptions { AllowDuplicateDefinitions = true, RequireResolvedReferences = false },
            Scenarios = [root, child, cycleA, cycleB, orphan],
            Messages = [first, duplicate]
        };

        DictionaryIndex index = DictionaryNormalizer.NormalizeDictionary(dictionary, dictionary.Options);
        FixDictionaryQuery query = new FixDictionaryQuery();
        FixMessageDefinition? resolvedFirst = query.ResolveMessageDefinition(dictionary, "D", "child");
        FixMessageDefinition? resolvedSecond = query.ResolveMessageDefinition(dictionary, "D", " CHILD ");
        FixScenarioDefinition? resolvedChild = query.ResolveScenarioDefinition(dictionary, "CHILD");

        Assert.Multiple(() =>
        {
            Assert.That(index.Success, Is.True);
            Assert.That(child.Name, Is.EqualTo("child"));
            Assert.That(child.ParentName, Is.EqualTo("ROOT"));
            Assert.That(index.FindScenarioByName(child.ParentName), Is.SameAs(root));
            Assert.That(index.Diagnostics.Count(diagnostic => diagnostic.Code == "recursive-scenario-reference"), Is.EqualTo(1));
            Assert.That(index.Diagnostics.Exists(diagnostic => diagnostic.Code == "unresolved-parent-scenario" && diagnostic.DefinitionName == "orphan"), Is.True);
            Assert.That(resolvedFirst, Is.SameAs(first));
            Assert.That(resolvedSecond, Is.SameAs(first));
            Assert.That(resolvedChild, Is.SameAs(child));
            Assert.That(dictionary.Diagnostics.Count(diagnostic => diagnostic.Code == "ambiguous-message-definition"), Is.EqualTo(1));
        });
    }

    private static FixComponentDefinition Component(int id, string name, int nestedId)
    {
        return new FixComponentDefinition
        {
            Id = id,
            Name = name,
            Members = [new FixMemberDefinition { Kind = FixMemberKind.Component, ReferenceId = nestedId }]
        };
    }
}
