using System.Numerics;
using System.Text;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlRfc9165ConformanceTests
{
    [Test]
    public void Figure1BuildsNonConflictingIntervalMapLabels()
    {
        string source =
            "interval<BASE> = (\n"
            + "  BASE => int\n"
            + "  (BASE .plus 1) => int\n"
            + "  ? (BASE .plus 2) => int\n"
            + ")\n"
            + "X = 0\n"
            + "Y = 3\n"
            + "rect = { interval<X>, interval<Y> }";
        CborValue required = Map(
            Entry(Integer(0), Integer(10)),
            Entry(Integer(1), Integer(20)),
            Entry(Integer(3), Integer(30)),
            Entry(Integer(4), Integer(40)));
        CborValue tolerance = Map(
            Entry(Integer(0), Integer(10)),
            Entry(Integer(1), Integer(20)),
            Entry(Integer(2), Integer(2)),
            Entry(Integer(3), Integer(30)),
            Entry(Integer(4), Integer(40)),
            Entry(Integer(5), Integer(5)));

        Assert.Multiple(() =>
        {
            AssertValid(source, required, "rect");
            AssertValid(source, tolerance, "rect");
            AssertInvalid(
                source,
                Map(
                    Entry(Integer(0), Integer(10)),
                    Entry(Integer(3), Integer(30)),
                    Entry(Integer(4), Integer(40))),
                "rect");
        });
    }

    [Test]
    public void Figure2ConcatenatesTextAndMultilineByteLiterals()
    {
        string source =
            "c = \"foo\" .cat '\n"
            + "  bar\n"
            + "  baz\n"
            + "'\n"
            + "b = \"foo\\n  bar\\n  baz\\n\"";

        Assert.Multiple(() =>
        {
            AssertValid(source, Text("foo\n  bar\n  baz\n"), "c");
            AssertValid(source, Text("foo\n  bar\n  baz\n"), "b");
            AssertInvalid(source, Bytes("foo\n  bar\n  baz\n"), "c");
        });
    }

    [Test]
    public void DedentingRemovesAllSpacesFromBlankLines()
    {
        string source =
            "lf = \" x\\n        \\n  y\" .det \"\"\n"
            + "crlf = ' x\\r\\n        \\r\\n  y' .det ''";

        Assert.Multiple(() =>
        {
            AssertValid(source, Text("x\n\n y"), "lf");
            AssertValid(source, Bytes("x\r\n\r\n y"), "crlf");
            AssertInvalid(source, Text("x\n       \n y"), "lf");
        });
    }

    [Test]
    public void Figures3And4MatchOidAndRelativeOidByteSequences()
    {
        string source =
            "oid = bstr .abnfb (\"oid\" .det cbor-tags-oid)\n"
            + "roid = bstr .abnfb (\"roid\" .det cbor-tags-oid)\n"
            + "cbor-tags-oid = '\n"
            + "  oid = 1*arc\n"
            + "  roid = *arc\n"
            + "  arc = [nlsb] %x00-7f\n"
            + "  nlsb = %x81-ff *%x80-ff\n"
            + "'";

        Assert.Multiple(() =>
        {
            AssertValid(source, Bytes(0x00), "oid");
            AssertValid(source, Bytes(0x01, 0x7f, 0x81, 0x00), "oid");
            AssertInvalid(source, Bytes(), "oid");
            AssertInvalid(source, Bytes(0x80), "oid");
            AssertValid(source, Bytes(), "roid");
            AssertValid(source, Bytes(0xff, 0x80, 0x01), "roid");
        });
    }

    [Test]
    public void Figure5ComposesRfc3339AbnfForTaggedStrings()
    {
        string source =
            "Tag1004 = #6.1004(tstr .abnf full-date)\n"
            + "Tag0 = #6.0(tstr .abnf date-time)\n"
            + "full-date = \"full-date\" .cat rfc3339\n"
            + "date-time = \"date-time\" .cat rfc3339\n"
            + "rfc3339 = '\n"
            + "  date-fullyear = 4DIGIT\n"
            + "  date-month = 2DIGIT\n"
            + "  date-mday = 2DIGIT\n"
            + "  time-hour = 2DIGIT\n"
            + "  time-minute = 2DIGIT\n"
            + "  time-second = 2DIGIT\n"
            + "  time-secfrac = \".\" 1*DIGIT\n"
            + "  time-numoffset = (\"+\" / \"-\") time-hour \":\" time-minute\n"
            + "  time-offset = \"Z\" / time-numoffset\n"
            + "  partial-time = time-hour \":\" time-minute \":\" time-second [time-secfrac]\n"
            + "  full-date = date-fullyear \"-\" date-month \"-\" date-mday\n"
            + "  full-time = partial-time time-offset\n"
            + "  date-time = full-date \"T\" full-time\n"
            + "' .det rfc5234-core\n"
            + "rfc5234-core = '\n"
            + "  DIGIT = %x30-39\n"
            + "'";

        Assert.Multiple(() =>
        {
            AssertValid(
                source,
                Tag(1004, Text("2026-08-28")),
                "Tag1004");
            AssertInvalid(
                source,
                Tag(1004, Text("2026-8-28")),
                "Tag1004");
            AssertValid(
                source,
                Tag(0, Text("2026-08-28T12:34:56.25Z")),
                "Tag0");
            AssertValid(
                source,
                Tag(0, Text("2026-08-28t12:34:56z")),
                "Tag0");
            AssertInvalid(
                source,
                Tag(0, Text("2026-08-28 12:34:56Z")),
                "Tag0");
        });
    }

    [Test]
    public void Figures6Through8ReportExplicitAndImplicitExtensions()
    {
        string source =
            "foo = { kind: bar / baz .feature ([\"foo-extensions\", \"bazify\"]) }\n"
            + "bar = 0\n"
            + "baz = 1 / 2\n"
            + "person = {\n"
            + "  ? name: tstr,\n"
            + "  ? organization: tstr,\n"
            + "  $$person-extensions,\n"
            + "  * (tstr .feature \"further-person-extension\") => any\n"
            + "}\n"
            + "$$person-extensions //= (? bloodgroup: tstr)\n"
            + "allowed-types = number / tstr / bool / null\n"
            + "  / [* number] / [* tstr] / [* bool]\n"
            + "  / (any .feature \"allowed-type-extension\")";
        CddlValidationResult explicitDetail = Validate(
            source,
            Map(Entry(Text("kind"), Integer(2))),
            "foo",
            Features("foo-extensions"));
        CddlValidationResult person = Validate(
            source,
            Map(
                Entry(Text("name"), Text("Ada")),
                Entry(Text("organisation"), Text("Example"))),
            "person",
            Features("further-person-extension"));
        CddlValidationResult knownPerson = Validate(
            source,
            Map(Entry(Text("bloodgroup"), Text("O+"))),
            "person",
            Features());
        CddlValidationResult extendedType = Validate(
            source,
            Bytes(0x01),
            "allowed-types",
            Features("allowed-type-extension"));
        CddlValidationResult disabledType = Validate(
            source,
            Bytes(0x01),
            "allowed-types",
            Features());

        Assert.Multiple(() =>
        {
            Assert.That(explicitDetail.IsValid, Is.True);
            Assert.That(explicitDetail.ControlResults.Single()
                .FeatureDetail!.TextStringValue, Is.EqualTo("bazify"));
            Assert.That(person.IsValid, Is.True);
            Assert.That(person.ControlResults.Single()
                .FeatureDetail!.TextStringValue,
                Is.EqualTo("organisation"));
            Assert.That(knownPerson.IsValid, Is.True);
            Assert.That(knownPerson.ControlResults, Is.Empty);
            Assert.That(extendedType.IsValid, Is.True);
            Assert.That(extendedType.ControlResults.Single().FeatureName,
                Is.EqualTo("allowed-type-extension"));
            Assert.That(disabledType.IsValid, Is.False);
            Assert.That(disabledType.ControlResults, Is.Empty);
        });
    }

    [Test]
    public void Figure9SelectsJsonAndCborMapKeyVariants()
    {
        string source =
            "SenML-Record = { ? v => number }\n"
            + "v = JC<\"v\", 2>\n"
            + "JC<J,C> = J .feature \"json\" / C .feature \"cbor\"";
        CddlValidationResult json = Validate(
            source,
            Map(Entry(Text("v"), Integer(1))),
            "SenML-Record",
            Features("json"));
        CddlValidationResult cbor = Validate(
            source,
            Map(Entry(Integer(2), Integer(1))),
            "SenML-Record",
            Features("cbor"));
        CddlValidationResult crossed = Validate(
            source,
            Map(Entry(Text("v"), Integer(1))),
            "SenML-Record",
            Features("cbor"));

        Assert.Multiple(() =>
        {
            Assert.That(json.IsValid, Is.True);
            Assert.That(json.ControlResults.Single().FeatureName,
                Is.EqualTo("json"));
            Assert.That(cbor.IsValid, Is.True);
            Assert.That(cbor.ControlResults.Single().FeatureName,
                Is.EqualTo("cbor"));
            Assert.That(crossed.IsValid, Is.False);
            Assert.That(crossed.ControlResults, Is.Empty);
        });
    }

    [TestCase("root = (tstr / bstr) .abnf \"%xD800\"")]
    [TestCase("root = (tstr / bstr) .abnf \"%x110000\"")]
    [TestCase("root = (tstr / bstr) .abnfb \"%x100\"")]
    [TestCase("root = (tstr / bstr) .abnf \"%s\\\"é\\\"\"")]
    public void RejectsAbnfTerminalsOutsideTheirMode(string source)
    {
        CddlValidationResult result = Validate(
            source,
            Text("anything"),
            "root",
            Features());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues[0].Code,
                Is.EqualTo(CddlValidationCode.InvalidSchema));
        });
    }

    [Test]
    public void Rfc9165ControllerEvaluationHonorsSharedBudgets()
    {
        string computed =
            "root = tstr .abnf (((\"%s\\\"a\\\"\" .cat \"\")"
            + " .cat \"\") .cat \"\")";
        CddlValidationResult alternatives = Validate(
            computed,
            Text("a"),
            "root",
            new CddlValidationOptions { MaxAlternatives = 2 });
        string grammar =
            "root = tstr .abnf \"value\\n"
            + "value = 1*32(%s\\\"a\\\" / %s\\\"b\\\")\\n\"";
        CddlValidationResult states = Validate(
            grammar,
            Text("aaaaaaaa"),
            "root",
            new CddlValidationOptions { MaxPatternStates = 8 });

        Assert.Multiple(() =>
        {
            AssertResourceLimit(alternatives);
            AssertResourceLimit(states);
        });
    }

    private static void AssertResourceLimit(CddlValidationResult result)
    {
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues.Select(item => item.Code),
            Does.Contain(CddlValidationCode.ResourceLimit));
    }

    private static void AssertValid(
        string source,
        CborValue value,
        string rule)
    {
        CddlValidationResult result = Validate(
            source,
            value,
            rule,
            Features());
        Assert.That(
            result.IsValid,
            Is.True,
            result.Issues.Length == 0 ? "" : result.Issues[0].Message);
    }

    private static void AssertInvalid(
        string source,
        CborValue value,
        string rule)
    {
        CddlValidationResult result = Validate(
            source,
            value,
            rule,
            Features());
        Assert.That(result.IsValid, Is.False);
    }

    private static CddlValidationResult Validate(
        string source,
        CborValue value,
        string rule,
        CddlValidationOptions options)
    {
        options.RootRuleName = rule;
        return new CddlValidator().ValidateCbor(
            Compile(source, rule),
            value,
            options);
    }

    private static CddlSchema Compile(string source, string rule)
    {
        CddlDocument document = new CddlParser().ParseDocument(
            source,
            new CddlParseOptions { SourceName = "rfc9165.cddl" });
        return new CddlCompiler().CompileSchema(
            document,
            new CddlCompileOptions { RootRuleName = rule });
    }

    private static CddlValidationOptions Features(params string[] names)
    {
        return new CddlValidationOptions { EnabledFeatures = names };
    }

    private static CborValue Integer(int value)
    {
        return new CborValue
        {
            Kind = value < 0
                ? CborValueKind.NegativeInteger
                : CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue Bytes(string value)
    {
        return Bytes(Encoding.UTF8.GetBytes(value));
    }

    private static CborValue Bytes(params byte[] value)
    {
        return new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = value
        };
    }

    private static CborValue Tag(ulong number, CborValue value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = number,
            TaggedValue = value
        };
    }

    private static CborMapEntry Entry(CborValue key, CborValue value)
    {
        return new CborMapEntry { Key = key, Value = value };
    }

    private static CborValue Map(params CborMapEntry[] entries)
    {
        return new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = new List<CborMapEntry>(entries)
        };
    }
}
