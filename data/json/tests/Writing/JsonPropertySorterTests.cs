namespace json.Tests;

public class JsonPropertySorterTests
{
    [Test]
    public void PreserveReturnsAnIndependentListInOriginalOrder()
    {
        JsonMember first = Member("z", 9);
        JsonMember second = Member("a", 1);
        List<JsonMember> source = new List<JsonMember> { first, second };
        JsonPropertySorter sorter = CreateSorter(
            JsonPropertyOrder.Preserve,
            null);

        List<JsonMember> result = sorter.SortProperties(source);
        source.Reverse();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(source));
            Assert.That(result, Is.EqualTo(new[] { first, second }));
            Assert.That(result.Select(member => member.Name),
                Is.EqualTo(new[] { "z", "a" }));
        });
    }

    [Test]
    public void Utf16AndScalarOrdersDifferForSupplementaryNames()
    {
        string supplementary = char.ConvertFromUtf32(0x1D11E);
        string privateUse = "\uE000";
        List<JsonMember> source = new List<JsonMember>
        {
            Member(supplementary, 0),
            Member("aa", 1),
            Member(privateUse, 2),
            Member("b", 3),
            Member("", 4),
            Member("a", 5)
        };

        List<JsonMember> utf16 = CreateSorter(
            JsonPropertyOrder.OrdinalUtf16,
            null).SortProperties(source);
        List<JsonMember> jcs = CreateSorter(
            JsonPropertyOrder.Jcs,
            null).SortProperties(source);
        List<JsonMember> scalar = CreateSorter(
            JsonPropertyOrder.OrdinalUnicodeScalar,
            null).SortProperties(source);

        Assert.Multiple(() =>
        {
            Assert.That(
                utf16.Select(member => member.Name),
                Is.EqualTo(new[]
                {
                    "",
                    "a",
                    "aa",
                    "b",
                    supplementary,
                    privateUse
                }));
            Assert.That(jcs, Is.EqualTo(utf16));
            Assert.That(
                scalar.Select(member => member.Name),
                Is.EqualTo(new[]
                {
                    "",
                    "a",
                    "aa",
                    "b",
                    privateUse,
                    supplementary
                }));
        });
    }

    [Test]
    public void EqualNamesRetainInputOrderRegardlessOfSourceOrdinal()
    {
        JsonMember bFirst = Member("b", 100);
        JsonMember aFirst = Member("a", 50);
        JsonMember bSecond = Member("b", 0);
        JsonMember aSecond = Member("a", -1);
        List<JsonMember> source = new List<JsonMember>
        {
            bFirst,
            aFirst,
            bSecond,
            aSecond
        };

        List<JsonMember> result = CreateSorter(
            JsonPropertyOrder.OrdinalUtf16,
            null).SortProperties(source);

        Assert.That(result,
            Is.EqualTo(new[] { aFirst, aSecond, bFirst, bSecond }));
    }

    [Test]
    public void ScalarAndJcsOrdersRejectUnpairedSurrogates()
    {
        string high = new string(new char[] { '\uD800' });
        string low = new string(new char[] { '\uDC00' });
        List<JsonMember> names = new List<JsonMember>
        {
            Member(high, 0),
            Member(low, 1)
        };

        List<JsonMember> utf16 = CreateSorter(
            JsonPropertyOrder.OrdinalUtf16,
            null).SortProperties(names);
        JsonWriteException scalar = Assert.Throws<JsonWriteException>(() =>
            CreateSorter(
                JsonPropertyOrder.OrdinalUnicodeScalar,
                null).SortProperties(names))
            ?? throw new InvalidOperationException(
                "Expected JsonWriteException.");
        JsonWriteException jcs = Assert.Throws<JsonWriteException>(() =>
            CreateSorter(
                JsonPropertyOrder.Jcs,
                null).SortProperties(names))
            ?? throw new InvalidOperationException(
                "Expected JsonWriteException.");

        Assert.Multiple(() =>
        {
            Assert.That(utf16, Is.EqualTo(names));
            Assert.That(scalar.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
            Assert.That(jcs.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
        });
    }

    [Test]
    public void MemberLimitAndMissingNamesFailBeforeReturningAList()
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumObjectMembers = 1;
        JsonPropertySorter limited = CreateSorter(
            JsonPropertyOrder.OrdinalUtf16,
            limits);
        JsonLimitException countFailure = Assert.Throws<JsonLimitException>(() =>
            limited.SortProperties(new List<JsonMember>
            {
                Member("a", 0),
                Member("b", 1)
            }))
            ?? throw new InvalidOperationException(
                "Expected JsonLimitException.");

        JsonMember unnamed = new JsonMember();
        JsonWriteException nameFailure = Assert.Throws<JsonWriteException>(() =>
            CreateSorter(
                JsonPropertyOrder.Preserve,
                null).SortProperties(new List<JsonMember> { unnamed }))
            ?? throw new InvalidOperationException(
                "Expected JsonWriteException.");

        Assert.Multiple(() =>
        {
            Assert.That(countFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.ObjectMemberLimitExceeded));
            Assert.That(nameFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
        });
    }

    static JsonPropertySorter CreateSorter(
        JsonPropertyOrder propertyOrder,
        JsonLimits? limits)
    {
        JsonWriterOptions options = new JsonWriterOptions();
        options.PropertyOrder = propertyOrder;
        options.Limits = limits;
        return new JsonPropertySorter(options);
    }

    static JsonMember Member(string name, int sourceOrdinal)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.SourceOrdinal = sourceOrdinal;
        return member;
    }
}
