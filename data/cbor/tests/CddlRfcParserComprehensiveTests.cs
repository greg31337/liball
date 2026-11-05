using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlRfcParserComprehensiveTests
{
    [Test]
    public void ParsesRfc8610FiguresOneThroughSix()
    {
        string[] examples =
        {
            "person = {age: int, name: tstr, employer: tstr}",
            "pii = (age: int, name: tstr, employer: tstr)",
            "person = {pii}",
            "person = {(age: int, name: tstr, employer: tstr)}",
            "person = {age: int, name: tstr, employer: tstr}\n"
                + "dog = {age: int, name: tstr, leash-length: float}",
            "person = {identity, employer: tstr}\n"
                + "dog = {identity, leash-length: float}\n"
                + "identity = (age: int, name: tstr)"
        };

        AssertAllParse(examples);
    }

    [Test]
    public void ParsesRfc8610CoreTypeArrayAndMapExamples()
    {
        string[] examples =
        {
            "attire = \"bow tie\" / \"necktie\" / \"Internet attire\"\n"
                + "protocol = 6 / 17\n"
                + "attire /= \"swimwear\"",
            "address = {delivery}\n"
                + "delivery = (street: tstr, ? number: uint, city // "
                + "po-box: uint, city // per-pickup: true)\n"
                + "delivery //= (lat: float, long: float, drone-type: tstr)\n"
                + "city = (name: tstr, zip-code: uint)",
            "device-address = byte\n"
                + "max-byte = 255\n"
                + "byte = 0..max-byte\n"
                + "first-non-byte = 256\n"
                + "byte1 = 0...first-non-byte\n"
                + "numeric-range = 0..10 / 0.0..10.0",
            "terminal-color = &basecolors\n"
                + "basecolors = (black: 0, red: 1, green: 2, yellow: 3, "
                + "blue: 4, magenta: 5, cyan: 6, white: 7)\n"
                + "extended-color = &(basecolors, orange: 8, pink: 9, "
                + "purple: 10, brown: 11)",
            "my_breakfast = #6.55799(breakfast)\n"
                + "breakfast = cereal / porridge\n"
                + "cereal = #6.998(tstr)\n"
                + "porridge = #6.999([liquid, solid])\n"
                + "liquid = milk / water\n"
                + "milk = 0\nwater = 1\nsolid = tstr",
            "; This is a comment\n"
                + "person = {g}\n"
                + "g = (\"name\": tstr, age: int)",
            "apartment = {kitchen: size, * bedroom: size}\n"
                + "size = float",
            "unlimited-people = [* person]\n"
                + "one-or-two-people = [1*2 person]\n"
                + "at-least-two-people = [2* person]\n"
                + "person = (name: tstr, age: uint)",
            "Geography = [city: tstr, gpsCoordinates: GpsCoordinates]\n"
                + "GpsCoordinates = {longitude: uint, latitude: uint}",
            "located-samples = {sample-point: int, samples: [+ float], "
                + "* equipment-type => equipment-tolerances}\n"
                + "equipment-type = [name: tstr, manufacturer: tstr]\n"
                + "equipment-tolerances = [+ [float, float]]",
            "PersonalData = {? displayName: tstr, NameComponents, "
                + "? age: uint, * tstr => any}\n"
                + "NameComponents = (? firstName: tstr, ? familyName: tstr)",
            "square-roots = {* x => y}\nx = int\ny = float\n"
                + "tostring = {* mynumber => tstr}\n"
                + "mynumber = int / float",
            "labeled-values = {? fritz: number, * label => value}\n"
                + "label = text\nvalue = number\n"
                + "do-not-do-this = {int => int, int => 6}",
            "extensible-map-example = {? \"optional-key\" ^ => int, "
                + "* tstr => any}",
            "biguint = #6.2(bstr)\n"
                + "buuid = #6.37(bstr)\n"
                + "my_uri = #6.32(tstr) / tstr",
            "basic-header = [field1: int, field2: text]\n"
                + "advanced-header = [~basic-header, field3: bytes, "
                + "field4: ~time]"
        };

        AssertAllParse(examples);
    }

    [Test]
    public void ParsesRfc8610ControlSocketGenericAndPrecedenceExamples()
    {
        string[] examples =
        {
            "full-address = [[+ label], ip4, ip6]\n"
                + "ip4 = bstr .size 4\nip6 = bstr .size 16\n"
                + "label = bstr .size (1..63)\n"
                + "audio_sample = uint .size 3",
            "tcpflagbytes = bstr .bits flags\n"
                + "flags = &(fin: 8, syn: 9, rst: 10, psh: 11, ack: 12, "
                + "urg: 13, ece: 14, cwr: 15, ns: 0) / (4..7)\n"
                + "rwxbits = uint .bits rwx\n"
                + "rwx = &(r: 2, w: 1, x: 0)",
            "nai = tstr .regexp "
                + "\"[A-Za-z0-9]+@[A-Za-z0-9]+(\\\\.[A-Za-z0-9]+)+\"",
            "embedded = bytes .cbor type1\n"
                + "sequence = bytes .cborseq type2\n"
                + "intersection = type1 .and type2\n"
                + "subset = type1 .within type2",
            "message = $message .within message-structure\n"
                + "message-structure = [message_type, * message_option]\n"
                + "message_type = 0..255\nmessage_option = any\n"
                + "$message /= [3, dough: text, topping: [* text]]\n"
                + "$message /= [4, noodles: text, sauce: text, parmesan: bool]",
            "speed = number .ge 0\n"
                + "timer = {time: uint, "
                + "? displayed-step: (number .gt 0) .default 1}",
            "comparisons = number .lt 10 / number .le 10 "
                + "/ number .gt 0 / number .ge 0 / number .eq 5 "
                + "/ number .ne 6",
            "tcp-header = {seq: uint, ack: uint, * $$tcp-option}\n"
                + "$$tcp-option //= (sack: [+(left: uint, right: uint)])\n"
                + "$$tcp-option //= (sack-permitted: true)",
            "PersonalData = {? displayName: tstr, NameComponents, "
                + "? age: uint, * $$personaldata-extensions}\n"
                + "NameComponents = (? firstName: tstr, ? familyName: tstr)\n"
                + "$$personaldata-extensions //= (favorite-salsa: tstr)\n"
                + "$$personaldata-extensions //= (shoesize: uint)",
            "messages = message<\"reboot\", \"now\"> "
                + "/ message<\"sleep\", 1..100>\n"
                + "message<t, v> = {type: t, value: v}",
            "t = [group1]\n"
                + "group1 = (a / b // c / d)\n"
                + "a = 1 b = 2 c = 3 d = 4",
            "t = {group2}\n"
                + "group2 = (? ab: a / b // cd: c / d)\n"
                + "a = 1 b = 2 c = 3 d = 4",
            "t = [group3]\n"
                + "group3 = (+ a / b / c)\n"
                + "a = 1 b = 2 c = 3",
            "t = [group4]\n"
                + "group4 = (+ a // b / c)\n"
                + "a = 1 b = 2 c = 3",
            "t = [group4a]\n"
                + "group4a = ((+ a) // (b / c))\n"
                + "a = 1 b = 2 c = 3"
        };

        AssertAllParse(examples);
    }

    [Test]
    public void BuildsTheRfc8610OperatorPrecedenceExamplesExactly()
    {
        CddlDocument document = ParseDone(
            "group1 = (a / b // c / d)\n"
            + "group3 = (+ a / b / c)\n"
            + "group4 = (+ a // b / c)");
        CddlNode group1 = EntryValue(document.Rules[0].Value!);
        CddlNode group1Choice = group1.Children[0];
        CddlNode group3 = EntryValue(document.Rules[1].Value!);
        CddlNode group3Sequence = group3.Children[0];
        CddlNode group4 = EntryValue(document.Rules[2].Value!);
        CddlNode group4Choice = group4.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(group1.Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedGroup));
            Assert.That(group1Choice.Kind, Is.EqualTo(CddlNodeKind.GroupChoice));
            Assert.That(EntryValue(group1Choice.Children[0].Children[0]).Kind,
                Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(EntryValue(group1Choice.Children[1].Children[0]).Kind,
                Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(group3Sequence.Children[0].Occurrence,
                Is.EqualTo(CddlOccurrenceKind.OneOrMore));
            Assert.That(EntryValue(group3Sequence.Children[0]).Kind,
                Is.EqualTo(CddlNodeKind.TypeChoice));
            Assert.That(group4Choice.Kind, Is.EqualTo(CddlNodeKind.GroupChoice));
            Assert.That(group4Choice.Children[0].Children[0].Occurrence,
                Is.EqualTo(CddlOccurrenceKind.OneOrMore));
            Assert.That(EntryValue(group4Choice.Children[0].Children[0]).Name,
                Is.EqualTo("a"));
            Assert.That(EntryValue(group4Choice.Children[1].Children[0]).Kind,
                Is.EqualTo(CddlNodeKind.TypeChoice));
        });
    }

    [Test]
    public void ParsesRfc8610PreludeAndJsonNumericExamples()
    {
        string standardPrelude =
            "any = #\nuint = #0\nnint = #1\nint = uint / nint\n"
            + "bstr = #2\nbytes = bstr\ntstr = #3\ntext = tstr\n"
            + "tdate = #6.0(tstr)\ntime = #6.1(number)\n"
            + "number = int / float\nbiguint = #6.2(bstr)\n"
            + "bignint = #6.3(bstr)\nbigint = biguint / bignint\n"
            + "integer = int / bigint\nunsigned = uint / biguint\n"
            + "decfrac = #6.4([e10: int, m: integer])\n"
            + "bigfloat = #6.5([e2: int, m: integer])\n"
            + "eb64url = #6.21(any)\neb64legacy = #6.22(any)\n"
            + "eb16 = #6.23(any)\nencoded-cbor = #6.24(bstr)\n"
            + "uri = #6.32(tstr)\nb64url = #6.33(tstr)\n"
            + "b64legacy = #6.34(tstr)\nregexp = #6.35(tstr)\n"
            + "mime-message = #6.36(tstr)\ncbor-any = #6.55799(any)\n"
            + "float16 = #7.25\nfloat32 = #7.26\nfloat64 = #7.27\n"
            + "float16-32 = float16 / float32\n"
            + "float32-64 = float32 / float64\n"
            + "float = float16-32 / float64\n"
            + "false = #7.20\ntrue = #7.21\nbool = false / true\n"
            + "nil = #7.22\nnull = nil\nundefined = #7.23";
        string jsonPrelude =
            "any = #\nuint = #0\nnint = #1\nint = uint / nint\n"
            + "tstr = #3\ntext = tstr\nnumber = int / float\n"
            + "float16 = #7.25\nfloat32 = #7.26\nfloat64 = #7.27\n"
            + "float16-32 = float16 / float32\n"
            + "float32-64 = float32 / float64\n"
            + "float = float16-32 / float64\n"
            + "false = #7.20\ntrue = #7.21\nbool = false / true\n"
            + "nil = #7.22\nnull = nil";
        string iJsonTypes =
            "ij-uint = 0..9007199254740991\n"
            + "ij-nint = -9007199254740991..-1\n"
            + "ij-int = -9007199254740991..9007199254740991";

        AssertAllParse(new string[] { standardPrelude, jsonPrelude, iJsonTypes });
    }

    [Test]
    public void ParsesBothRfc8610AppendixHReputationSchemas()
    {
        string verbose =
            "reputation-object = {reputation-context, reputon-list}\n"
            + "reputation-context = (application: text)\n"
            + "reputon-list = (reputons: reputon-array)\n"
            + "reputon-array = [* reputon]\n"
            + "reputon = {rater-value, assertion-value, rated-value, "
            + "rating-value, ? conf-value, ? normal-value, ? sample-value, "
            + "? gen-value, ? expire-value, * ext-value}\n"
            + "rater-value = (rater: text)\n"
            + "assertion-value = (assertion: text)\n"
            + "rated-value = (rated: text)\n"
            + "rating-value = (rating: float16)\n"
            + "conf-value = (confidence: float16)\n"
            + "normal-value = (normal-rating: float16)\n"
            + "sample-value = (sample-size: uint)\n"
            + "gen-value = (generated: uint)\n"
            + "expire-value = (expires: uint)\n"
            + "ext-value = (text => any)";
        string compact =
            "reputation-object = {application: text, reputons: [* reputon]}\n"
            + "reputon = {rater: text, assertion: text, rated: text, "
            + "rating: float16, ? confidence: float16, "
            + "? normal-rating: float16, ? sample-size: uint, "
            + "? generated: uint, ? expires: uint, * text => any}";

        CddlDocument verboseDocument = ParseDone(verbose);
        CddlDocument compactDocument = ParseDone(compact);

        Assert.Multiple(() =>
        {
            Assert.That(verboseDocument.Rules, Has.Count.EqualTo(15));
            Assert.That(compactDocument.Rules, Has.Count.EqualTo(2));
            Assert.That(verboseDocument.RootRuleName,
                Is.EqualTo("reputation-object"));
            Assert.That(compactDocument.RootRuleName,
                Is.EqualTo("reputation-object"));
        });
    }

    [Test]
    public void DistinguishesAmbiguousTypeAndGroupContextsByPreemption()
    {
        CddlDocument document = ParseDone(
            "plain = (item)\n"
            + "comma-group = (item,)\n"
            + "key-group = (name: item)\n"
            + "choice-group = (item // other)\n"
            + "occurrence-group = (+ item)\n"
            + "container = [(item), (item,), (name: item)]\n"
            + "identifier = min..max\n"
            + "range = min .. max");
        CddlNode containerGroup = document.Rules[5].Value!.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(document.Rules[0].Kind, Is.EqualTo(CddlRuleKind.Type));
            Assert.That(document.Rules[0].Value!.Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedType));
            Assert.That(document.Rules.Skip(1).Take(4).Select(rule => rule.Kind),
                Is.All.EqualTo(CddlRuleKind.Group));
            Assert.That(EntryValue(containerGroup.Children[0]).Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedType));
            Assert.That(EntryValue(containerGroup.Children[1]).Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedGroup));
            Assert.That(EntryValue(containerGroup.Children[2]).Kind,
                Is.EqualTo(CddlNodeKind.ParenthesizedGroup));
            Assert.That(document.Rules[6].Value!.Kind,
                Is.EqualTo(CddlNodeKind.RuleReference));
            Assert.That(document.Rules[6].Value!.Name, Is.EqualTo("min..max"));
            Assert.That(document.Rules[7].Value!.Kind,
                Is.EqualTo(CddlNodeKind.Range));
        });
    }

    [Test]
    public void ParsesRfc9682EmptyModelsAndTypeValuedHeadArguments()
    {
        CddlParseResult empty = Parse(" ; no rules\n");
        CddlDocument updated = ParseDone(
            "tagged = #6.<tag-number>(content)\n"
            + "simple = #7.<simple-number>\n"
            + "tag-number = 1668546817..1668612095\n"
            + "simple-number = 0..255\n"
            + "content = any");

        Assert.Multiple(() =>
        {
            Assert.That(empty.IsSuccess, Is.True);
            Assert.That(empty.Document!.Rules, Is.Empty);
            Assert.That(empty.Document.RootRuleName, Is.Null);
            Assert.That(updated.Rules[0].Value!.Kind,
                Is.EqualTo(CddlNodeKind.Tag));
            Assert.That(updated.Rules[0].Value!.Children[0].Name,
                Is.EqualTo("tag-number"));
            Assert.That(updated.Rules[1].Value!.Kind,
                Is.EqualTo(CddlNodeKind.Representation));
            Assert.That(updated.Rules[1].Value!.Children[0].Name,
                Is.EqualTo("simple-number"));
        });
    }

    private static CddlNode EntryValue(CddlNode entry)
    {
        return entry.Children[entry.Children.Count - 1];
    }

    private static void AssertAllParse(string[] examples)
    {
        int index = 0;
        while (index < examples.Length)
        {
            CddlParseResult result = Parse(examples[index]);
            string message = $"RFC example index {index}:\n{examples[index]}";
            Assert.Multiple(() =>
            {
                Assert.That(result.Diagnostics, Is.Empty, message);
                Assert.That(result.IsSuccess, Is.True, message);
                Assert.That(result.Document, Is.Not.Null, message);
                Assert.That(result.Document!.Rules, Is.Not.Empty, message);
            });
            index = index + 1;
        }
    }

    private static CddlParseResult Parse(string source)
    {
        return new CddlParser().TryParseDocument(
            source,
            new CddlParseOptions { SourceName = "rfc-example.cddl" });
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
