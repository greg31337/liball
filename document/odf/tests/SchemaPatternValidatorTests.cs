using System.Collections.Generic;

namespace odf.Tests;

public class SchemaPatternValidatorTests
{
    [Test]
    public void ChoiceGroupAndRepetitionValidateElementContent()
    {
        RuleGraphBuilder graph = new RuleGraphBuilder();
        int fast = graph.Add(
            SchemaPatternKind.Value,
            [],
            value: "fast",
            name: "token");
        int safe = graph.Add(
            SchemaPatternKind.Value,
            [],
            value: "safe",
            name: "token");
        int modeChoice = graph.Add(SchemaPatternKind.Choice, fast, safe);
        int mode = graph.Add(
            SchemaPatternKind.Attribute,
            modeChoice,
            name: "mode");
        int integer = graph.Add(SchemaPatternKind.Data, [], name: "integer");
        int item = graph.Add(SchemaPatternKind.Element, integer, name: "item");
        int items = graph.Add(SchemaPatternKind.OneOrMore, item);
        int group = graph.Add(SchemaPatternKind.Group, mode, items);
        int root = graph.Add(SchemaPatternKind.Element, group, name: "root");
        SchemaRuleSet rules = graph.Build(root);

        OdfNode valid = Element(
            "",
            "root",
            new[] { Attribute("", "mode", "fast") },
            Element("", "item", [], Text("12")),
            Element("", "item", [], Text("-3")));
        OdfNode badChoice = Element(
            "",
            "root",
            new[] { Attribute("", "mode", "slow") },
            Element("", "item", [], Text("12")));
        OdfNode badDatatype = Element(
            "",
            "root",
            new[] { Attribute("", "mode", "safe") },
            Element("", "item", [], Text("twelve")));

        SchemaValidationResult validResult = Validate(valid, rules);
        SchemaValidationResult choiceResult = Validate(badChoice, rules);
        SchemaValidationResult datatypeResult = Validate(badDatatype, rules);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.IsValid, Is.True);
            Assert.That(validResult.InvalidDatatype, Is.False);
            Assert.That(choiceResult.IsValid, Is.False);
            Assert.That(datatypeResult.IsValid, Is.False);
            Assert.That(datatypeResult.InvalidDatatype, Is.True);
        });
    }

    [Test]
    public void InterleaveAcceptsUnorderedAttributesAndShuffledContent()
    {
        RuleGraphBuilder graph = new RuleGraphBuilder();
        int text = graph.Add(SchemaPatternKind.Text);
        int attributeA = graph.Add(SchemaPatternKind.Attribute, text, name: "a");
        int optionalA = graph.Add(SchemaPatternKind.Optional, attributeA);
        int attributeB = graph.Add(SchemaPatternKind.Attribute, text, name: "b");
        int empty = graph.Add(SchemaPatternKind.Empty);
        int first = graph.Add(SchemaPatternKind.Element, empty, name: "first");
        int second = graph.Add(SchemaPatternKind.Element, empty, name: "second");
        int third = graph.Add(SchemaPatternKind.Element, empty, name: "third");
        int fourth = graph.Add(SchemaPatternKind.Element, empty, name: "fourth");
        int firstSequence = graph.Add(SchemaPatternKind.Group, first, second);
        int secondSequence = graph.Add(SchemaPatternKind.Group, third, fourth);
        int interleave = graph.Add(
            SchemaPatternKind.Interleave,
            optionalA,
            attributeB,
            firstSequence,
            secondSequence);
        int root = graph.Add(SchemaPatternKind.Element, interleave, name: "root");
        SchemaRuleSet rules = graph.Build(root);

        OdfNode node = Element(
            "",
            "root",
            new[]
            {
                Attribute("", "b", "2"),
                Attribute("", "a", "1")
            },
            Element("", "first", []),
            Element("", "third", []),
            Element("", "second", []),
            Element("", "fourth", []));

        Assert.That(Validate(node, rules).IsValid, Is.True);
    }

    [Test]
    public void MixedContentAllowsTextAroundRepeatedElements()
    {
        RuleGraphBuilder graph = new RuleGraphBuilder();
        int text = graph.Add(SchemaPatternKind.Text);
        int emphasis = graph.Add(SchemaPatternKind.Element, text, name: "em");
        int emphasisRuns = graph.Add(SchemaPatternKind.ZeroOrMore, emphasis);
        int mixed = graph.Add(SchemaPatternKind.Mixed, emphasisRuns);
        int root = graph.Add(SchemaPatternKind.Element, mixed, name: "p");
        SchemaRuleSet rules = graph.Build(root);
        OdfNode node = Element(
            "",
            "p",
            [],
            Text("before "),
            Element("", "em", [], Text("inside")),
            Text(" after"));

        Assert.That(Validate(node, rules).IsValid, Is.True);
    }

    [Test]
    public void AnyNameWithExceptSupportsForeignContentAllowances()
    {
        RuleGraphBuilder graph = new RuleGraphBuilder();
        int blockedName = graph.Add(
            SchemaPatternKind.Name,
            [],
            name: "blocked",
            namespaceName: "urn:foreign");
        int except = graph.Add(SchemaPatternKind.Except, blockedName);
        int anyElementName = graph.Add(SchemaPatternKind.AnyName, except);
        int anyAttributeName = graph.Add(SchemaPatternKind.AnyName);
        int text = graph.Add(SchemaPatternKind.Text);
        int anyAttribute = graph.Add(
            SchemaPatternKind.Attribute,
            anyAttributeName,
            text);
        int foreignAttributes = graph.Add(SchemaPatternKind.ZeroOrMore, anyAttribute);
        int foreignElement = graph.Add(
            SchemaPatternKind.Element,
            anyElementName,
            foreignAttributes);
        int foreignElements = graph.Add(SchemaPatternKind.ZeroOrMore, foreignElement);
        int root = graph.Add(SchemaPatternKind.Element, foreignElements, name: "root");
        SchemaRuleSet rules = graph.Build(root);

        OdfNode allowed = Element(
            "",
            "root",
            [],
            Element(
                "urn:foreign",
                "widget",
                new[] { Attribute("urn:vendor", "option", "on") }));
        OdfNode blocked = Element(
            "",
            "root",
            [],
            Element("urn:foreign", "blocked", []));

        Assert.Multiple(() =>
        {
            Assert.That(Validate(allowed, rules).IsValid, Is.True);
            Assert.That(Validate(blocked, rules).IsValid, Is.False);
        });
    }

    [Test]
    public void ListAndDatatypeFacetsValidateLexicalValues()
    {
        RuleGraphBuilder graph = new RuleGraphBuilder();
        int ncName = graph.Add(SchemaPatternKind.Data, [], name: "NCName");
        int names = graph.Add(SchemaPatternKind.OneOrMore, ncName);
        int list = graph.Add(SchemaPatternKind.List, names);
        int references = graph.Add(
            SchemaPatternKind.Attribute,
            list,
            name: "refs");
        int minimum = graph.Add(
            SchemaPatternKind.Param,
            [],
            value: "0.0",
            name: "minInclusive");
        int maximum = graph.Add(
            SchemaPatternKind.Param,
            [],
            value: "1.0",
            name: "maxInclusive");
        int decimalValue = graph.Add(
            SchemaPatternKind.Data,
            new[] { minimum, maximum },
            name: "decimal");
        int ratio = graph.Add(
            SchemaPatternKind.Attribute,
            decimalValue,
            name: "ratio");
        int colorPattern = graph.Add(
            SchemaPatternKind.Param,
            [],
            value: "#[0-9a-fA-F]{6}",
            name: "pattern");
        int colorValue = graph.Add(
            SchemaPatternKind.Data,
            colorPattern,
            name: "string");
        int color = graph.Add(
            SchemaPatternKind.Attribute,
            colorValue,
            name: "color");
        int curiePattern = graph.Add(
            SchemaPatternKind.Param,
            [],
            value: "(([\\i-[:]][\\c-[:]]*)?:)?.+",
            name: "pattern");
        int curieValue = graph.Add(
            SchemaPatternKind.Data,
            curiePattern,
            name: "string");
        int curie = graph.Add(
            SchemaPatternKind.Attribute,
            curieValue,
            name: "curie");
        int attributes = graph.Add(
            SchemaPatternKind.Interleave,
            references,
            ratio,
            color,
            curie);
        int root = graph.Add(SchemaPatternKind.Element, attributes, name: "root");
        SchemaRuleSet rules = graph.Build(root);

        OdfNode valid = Element(
            "",
            "root",
            new[]
            {
                Attribute("", "refs", "first second"),
                Attribute("", "ratio", "0.5"),
                Attribute("", "color", "#12aBcF"),
                Attribute("", "curie", "prefix:value")
            });
        OdfNode invalid = Element(
            "",
            "root",
            new[]
            {
                Attribute("", "refs", "first bad:name"),
                Attribute("", "ratio", "1.5"),
                Attribute("", "color", "red"),
                Attribute("", "curie", "prefix:value")
            });

        Assert.Multiple(() =>
        {
            Assert.That(Validate(valid, rules).IsValid, Is.True);
            Assert.That(Validate(invalid, rules).IsValid, Is.False);
            Assert.That(Validate(invalid, rules).InvalidDatatype, Is.True);
        });
    }

    [Test]
    public void EmptyStringDatatypeMatchesAbsentTextWithoutLooping()
    {
        RuleGraphBuilder graph = new RuleGraphBuilder();
        int value = graph.Add(SchemaPatternKind.Data, [], name: "string");
        int repeated = graph.Add(SchemaPatternKind.ZeroOrMore, value);
        int root = graph.Add(SchemaPatternKind.Element, repeated, name: "root");

        SchemaValidationResult result = Validate(
            Element("", "root", []),
            graph.Build(root));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.LimitExceeded, Is.False);
        });
    }

    [Test]
    public void RecursiveRulesStopAtConfiguredWorkBounds()
    {
        RuleGraphBuilder graph = new RuleGraphBuilder();
        int reference = graph.Add(SchemaPatternKind.Ref, [], name: "loop");
        graph.AddDefinition("loop", reference);
        SchemaRuleSet rules = graph.Build(reference);
        SchemaValidationLimits limits = new SchemaValidationLimits();
        limits.MaximumPatternDepth = 12;
        limits.MaximumOperations = 100;
        SchemaPatternValidator validator = new SchemaPatternValidator(limits);

        SchemaValidationResult result = validator.ValidateNode(
            Element("", "root", []),
            rules);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.LimitExceeded, Is.True);
            Assert.That(result.MaximumDepth, Is.GreaterThan(12));
            Assert.That(result.Operations, Is.LessThanOrEqualTo(100));
        });
    }

    [Test]
    public void BundledOdf14GrammarAcceptsMinimalDocumentContent()
    {
        SchemaRuleSet? rules = OdfSchemaRuleCatalog.GetRuleSet(
            OdfVersion.Version14,
            "document");
        Assert.That(rules, Is.Not.Null);
        OdfNode document = Element(
            OdfNamespaces.Office,
            "document-content",
            new[]
            {
                Attribute(OdfNamespaces.Office, "version", "1.4")
            },
            Element(
                OdfNamespaces.Office,
                "body",
                [],
                Element(OdfNamespaces.Office, "text", [])));

        SchemaValidationResult result = Validate(document, rules!);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.LimitExceeded, Is.False);
            Assert.That(result.Operations, Is.LessThan(1000000));
        });
    }

    static SchemaValidationResult Validate(OdfNode node, SchemaRuleSet rules)
    {
        return new SchemaPatternValidator(new SchemaValidationLimits())
            .ValidateNode(node, rules);
    }

    static OdfNode Element(
        string namespaceName,
        string localName,
        OdfAttribute[] attributes,
        params OdfNode[] children)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = new OdfQualifiedName();
        result.Name.NamespaceName = namespaceName;
        result.Name.LocalName = localName;
        result.Attributes = attributes;
        result.Children = children;
        foreach (OdfNode child in children)
        {
            child.Parent = result;
        }
        return result;
    }

    static OdfAttribute Attribute(
        string namespaceName,
        string localName,
        string value)
    {
        OdfAttribute result = new OdfAttribute();
        result.Name.NamespaceName = namespaceName;
        result.Name.LocalName = localName;
        result.Value = value;
        return result;
    }

    static OdfNode Text(string value)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Text;
        result.Value = value;
        return result;
    }

    sealed class RuleGraphBuilder
    {
        readonly List<SchemaPattern> patterns = [];
        readonly List<int> children = [];
        readonly List<SchemaRule> definitions = [];

        internal int Add(
            SchemaPatternKind kind,
            params int[] childIndexes)
        {
            return Add(kind, childIndexes, "", "", "");
        }

        internal int Add(
            SchemaPatternKind kind,
            int childIndex,
            string name)
        {
            return Add(kind, new[] { childIndex }, name, "", "");
        }

        internal int Add(
            SchemaPatternKind kind,
            int[] childIndexes,
            string name = "",
            string value = "",
            string namespaceName = "")
        {
            SchemaPattern pattern = new SchemaPattern();
            pattern.Kind = kind;
            pattern.Name = name;
            pattern.Value = value;
            pattern.NamespaceName = namespaceName;
            pattern.FirstChildIndex = children.Count;
            pattern.ChildCount = childIndexes.Length;
            children.AddRange(childIndexes);
            patterns.Add(pattern);
            return patterns.Count - 1;
        }

        internal void AddDefinition(string name, int patternIndex)
        {
            SchemaRule rule = new SchemaRule();
            rule.Name = name;
            rule.PatternIndex = patternIndex;
            definitions.Add(rule);
        }

        internal SchemaRuleSet Build(int startPattern)
        {
            SchemaRule start = new SchemaRule();
            start.IsStart = true;
            start.PatternIndex = startPattern;
            List<SchemaRule> rules = new List<SchemaRule>();
            rules.Add(start);
            rules.AddRange(definitions);
            SchemaRuleSet result = new SchemaRuleSet();
            result.Version = OdfVersion.Version14;
            result.Profile = "test";
            result.Rules = rules.ToArray();
            result.Patterns = patterns.ToArray();
            result.PatternChildIndexes = children.ToArray();
            return result;
        }
    }
}
