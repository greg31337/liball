using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlGenericSocketParserTests
{
    [Test]
    public void ParsesTypeAndGroupSocketReferencesAndOrderedPlugs()
    {
        CddlDocument document = ParseDone(
            "root = $message .within [uint, * $$options]\n"
            + "$message /= [0]\n"
            + "$message /= [1]\n"
            + "$$options //= (name: tstr,)\n"
            + "$$options //= (? count: uint,)");
        CddlNode root = document.Rules[0].Value!;
        CddlNode array = root.Children[1];
        CddlNode arrayGroup = array.Children[0];
        CddlNode groupSocketReference = EntryValue(arrayGroup.Children[1]);
        CddlNode firstGroupPlug = document.Rules[3].Value!;

        Assert.Multiple(() =>
        {
            Assert.That(document.RootRuleName, Is.EqualTo("root"));
            Assert.That(root.Kind, Is.EqualTo(CddlNodeKind.Control));
            Assert.That(root.Children[0].Name, Is.EqualTo("$message"));
            Assert.That(groupSocketReference.Name, Is.EqualTo("$$options"));
            Assert.That(arrayGroup.Children[1].Occurrence,
                Is.EqualTo(CddlOccurrenceKind.ZeroOrMore));
            Assert.That(document.Rules[1].Kind, Is.EqualTo(CddlRuleKind.TypeSocket));
            Assert.That(document.Rules[1].Name, Is.EqualTo("message"));
            Assert.That(document.Rules[2].Kind, Is.EqualTo(CddlRuleKind.TypeSocket));
            Assert.That(document.Rules[3].Kind, Is.EqualTo(CddlRuleKind.GroupSocket));
            Assert.That(document.Rules[3].Name, Is.EqualTo("options"));
            Assert.That(document.Rules[4].Kind, Is.EqualTo(CddlRuleKind.GroupSocket));
            Assert.That(firstGroupPlug.Kind, Is.EqualTo(CddlNodeKind.GroupEntry));
            Assert.That(EntryValue(firstGroupPlug).Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedGroup));
            Assert.That(document.Rules.Select(rule => rule.SourceOrder),
                Is.EqualTo(new int[] { 0, 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void AllowsUndefinedSocketsAsOrdinaryReferences()
    {
        CddlDocument document = ParseDone(
            "root = [$optional-type, * $$optional-group]");
        CddlNode group = document.Rules[0].Value!.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(EntryValue(group.Children[0]).Name,
                Is.EqualTo("$optional-type"));
            Assert.That(EntryValue(group.Children[1]).Name,
                Is.EqualTo("$$optional-group"));
            Assert.That(document.Rules, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ParsesGenericParametersAndLiteralRangeOrNestedArguments()
    {
        CddlDocument document = ParseDone(
            "messages = message<\"reboot\", now<1>> "
            + "/ message<\"sleep\", 1..100>\n"
            + "message<t, v> = {type: t, value: v}\n"
            + "now<x> = x");
        CddlNode messages = document.Rules[0].Value!;
        CddlNode first = messages.Children[0];
        CddlNode second = messages.Children[1];
        CddlNode nestedNow = first.Children[1];
        CddlRule genericRule = document.Rules[1];
        CddlNode genericMap = genericRule.Value!;
        CddlNode genericMapGroup = genericMap.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(messages.Kind, Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(first.Name, Is.EqualTo("message"));
            Assert.That(first.Children, Has.Count.EqualTo(2));
            Assert.That(first.Children[0].TextValue, Is.EqualTo("reboot"));
            Assert.That(nestedNow.Name, Is.EqualTo("now"));
            Assert.That(nestedNow.Children, Has.Count.EqualTo(1));
            Assert.That(second.Children[0].TextValue, Is.EqualTo("sleep"));
            Assert.That(second.Children[1].Kind, Is.EqualTo(CddlNodeKind.Range));
            Assert.That(genericRule.GenericParameters,
                Is.EqualTo(new string[] { "t", "v" }));
            Assert.That(genericMapGroup.Children[0].Name, Is.EqualTo("type"));
            Assert.That(EntryValue(genericMapGroup.Children[0]).Name,
                Is.EqualTo("t"));
            Assert.That(EntryValue(genericMapGroup.Children[1]).Name,
                Is.EqualTo("v"));
            Assert.That(document.Rules[2].GenericParameters,
                Is.EqualTo(new string[] { "x" }));
        });
    }

    [Test]
    public void ParsesDeeplyNestedGenericApplicationsWithoutFlattening()
    {
        CddlNode box = ParseDone(
            "root<a> = box<list<a>, pair<a, box<uint>>>")
            .Rules[0].Value!;
        CddlNode list = box.Children[0];
        CddlNode pair = box.Children[1];
        CddlNode innerBox = pair.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(box.Name, Is.EqualTo("box"));
            Assert.That(box.Children, Has.Count.EqualTo(2));
            Assert.That(list.Name, Is.EqualTo("list"));
            Assert.That(list.Children[0].Name, Is.EqualTo("a"));
            Assert.That(pair.Name, Is.EqualTo("pair"));
            Assert.That(pair.Children[0].Name, Is.EqualTo("a"));
            Assert.That(innerBox.Name, Is.EqualTo("box"));
            Assert.That(innerBox.Children[0].Name, Is.EqualTo("uint"));
            Assert.That(box.Span.Length,
                Is.EqualTo("box<list<a>, pair<a, box<uint>>>".Length));
        });
    }

    [Test]
    public void AppliesGenericArgumentsToUnwrapEnumerationAndMemberKeys()
    {
        CddlNode array = ParseDone(
            "root = [~container<uint>, &fields<tstr>, "
            + "key<uint> => value<tstr>]")
            .Rules[0].Value!;
        CddlNode group = array.Children[0];
        CddlNode unwrap = EntryValue(group.Children[0]);
        CddlNode enumeration = EntryValue(group.Children[1]);
        CddlNode keyed = group.Children[2];

        Assert.Multiple(() =>
        {
            Assert.That(unwrap.Kind, Is.EqualTo(CddlNodeKind.Unwrap));
            Assert.That(unwrap.Name, Is.EqualTo("container"));
            Assert.That(unwrap.Children[0].Name, Is.EqualTo("uint"));
            Assert.That(enumeration.Kind,
                Is.EqualTo(CddlNodeKind.GroupEnumeration));
            Assert.That(enumeration.Name, Is.EqualTo("fields"));
            Assert.That(enumeration.Children[0].Name, Is.EqualTo("tstr"));
            Assert.That(keyed.Children[0].Name, Is.EqualTo("key"));
            Assert.That(keyed.Children[0].Children[0].Name, Is.EqualTo("uint"));
            Assert.That(EntryValue(keyed).Name, Is.EqualTo("value"));
            Assert.That(EntryValue(keyed).Children[0].Name, Is.EqualTo("tstr"));
        });
    }

    [Test]
    public void DistinguishesGenericTypeAndGroupRulesByParenthesizedContext()
    {
        CddlDocument document = ParseDone(
            "type<t> = (box<t> / t)\n"
            + "group<t> = (box<t>,)\n"
            + "root = [type<uint>, group<tstr>, (uint), (uint,)]");
        CddlRule typeRule = document.Rules[0];
        CddlRule groupRule = document.Rules[1];
        CddlNode rootGroup = document.Rules[2].Value!.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(typeRule.Kind, Is.EqualTo(CddlRuleKind.Type));
            Assert.That(typeRule.Value!.Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedType));
            Assert.That(groupRule.Kind, Is.EqualTo(CddlRuleKind.Group));
            Assert.That(EntryValue(groupRule.Value!).Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedGroup));
            Assert.That(EntryValue(rootGroup.Children[0]).Name, Is.EqualTo("type"));
            Assert.That(EntryValue(rootGroup.Children[1]).Name, Is.EqualTo("group"));
            Assert.That(EntryValue(rootGroup.Children[2]).Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedType));
            Assert.That(EntryValue(rootGroup.Children[3]).Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedGroup));
        });
    }

    [TestCase("root<> = uint", "CDDL_PARSE_GENERIC_PARAMETER_EXPECTED", 5)]
    [TestCase("root<t,> = uint", "CDDL_PARSE_GENERIC_PARAMETER_EXPECTED", 7)]
    [TestCase("root<t = uint", "CDDL_PARSE_GENERIC_PARAMETER_CLOSE_EXPECTED", 7)]
    [TestCase("root = box<>", "CDDL_PARSE_GENERIC_ARGUMENT_EXPECTED", 11)]
    [TestCase("root = box<uint", "CDDL_PARSE_GENERIC_ARGUMENT_CLOSE_EXPECTED", 15)]
    [TestCase("root = box<uint,>", "CDDL_PARSE_GENERIC_ARGUMENT_EXPECTED", 16)]
    [TestCase("root = box<uint / tstr>", "CDDL_PARSE_GENERIC_ARGUMENT_CLOSE_EXPECTED", 16)]
    [TestCase("$socket = uint", "CDDL_PARSE_TYPE_SOCKET_ASSIGNMENT_INVALID", 8)]
    [TestCase("$$socket /= uint", "CDDL_PARSE_GROUP_SOCKET_ASSIGNMENT_INVALID", 9)]
    [TestCase("$ /= uint", "CDDL_PARSE_SOCKET_NAME_EXPECTED", 0)]
    [TestCase("root = ~box<>", "CDDL_PARSE_GENERIC_ARGUMENT_EXPECTED", 12)]
    [TestCase("root = &fields<>", "CDDL_PARSE_GENERIC_ARGUMENT_EXPECTED", 15)]
    [TestCase("root = box <uint>", "CDDL_PARSE_RULE_END_EXPECTED", 11)]
    [TestCase("root = {key<uint>: value}", "CDDL_PARSE_MEMBER_KEY_INVALID", 8)]
    public void ReportsMalformedSocketAndGenericSyntaxAtExactOffsets(
        string source,
        string code,
        int offset)
    {
        CddlParseResult result = Parse(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics[0].Span.Start, Is.EqualTo(offset));
        });
    }

    [Test]
    public void GenericApplicationsHonorTheNestingDepthLimit()
    {
        CddlParseResult result = new CddlParser().TryParseDocument(
            "root = outer<middle<inner<uint>>>",
            new CddlParseOptions { MaxNestingDepth = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo("CDDL_PARSE_DEPTH_LIMIT"));
            Assert.That(result.Diagnostics[0].Span.Start, Is.EqualTo(25));
        });
    }

    private static CddlNode EntryValue(CddlNode entry)
    {
        return entry.Children[entry.Children.Count - 1];
    }

    private static CddlParseResult Parse(string source)
    {
        return new CddlParser().TryParseDocument(source, new CddlParseOptions());
    }

    private static CddlDocument ParseDone(string source)
    {
        CddlParseResult result = Parse(source);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Document, Is.Not.Null);
        return result.Document!;
    }
}
