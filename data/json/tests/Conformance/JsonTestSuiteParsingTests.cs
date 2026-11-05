namespace json.Tests;

public class JsonTestSuiteParsingTests
{
    static readonly object[][] ImplementationDefinedExpectations = new object[][]
    {
        new object[] { "i_number_double_huge_neg_exp.json", true, true, true, true },
        new object[] { "i_number_huge_exp.json", true, true, true, true },
        new object[] { "i_number_neg_int_huge_exp.json", true, true, true, true },
        new object[] { "i_number_pos_double_huge_exp.json", true, true, true, true },
        new object[] { "i_number_real_neg_overflow.json", true, true, true, true },
        new object[] { "i_number_real_pos_overflow.json", true, true, true, true },
        new object[] { "i_number_real_underflow.json", true, true, true, true },
        new object[] { "i_number_too_big_neg_int.json", true, true, true, true },
        new object[] { "i_number_too_big_pos_int.json", true, true, true, true },
        new object[] { "i_number_very_big_negative_int.json", true, true, true, true },
        new object[] { "i_object_key_lone_2nd_surrogate.json", true, false, false, false },
        new object[] { "i_string_1st_surrogate_but_2nd_missing.json", true, false, false, false },
        new object[] { "i_string_1st_valid_surrogate_2nd_invalid.json", true, false, false, false },
        new object[] { "i_string_UTF-16LE_with_BOM.json", false, false, false, false },
        new object[] { "i_string_UTF-8_invalid_sequence.json", false, false, false, false },
        new object[] { "i_string_UTF8_surrogate_U+D800.json", false, false, false, false },
        new object[] { "i_string_incomplete_surrogate_and_escape_valid.json", true, false, false, false },
        new object[] { "i_string_incomplete_surrogate_pair.json", true, false, false, false },
        new object[] { "i_string_incomplete_surrogates_escape_valid.json", true, false, false, false },
        new object[] { "i_string_invalid_lonely_surrogate.json", true, false, false, false },
        new object[] { "i_string_invalid_surrogate.json", true, false, false, false },
        new object[] { "i_string_invalid_utf-8.json", false, false, false, false },
        new object[] { "i_string_inverted_surrogates_U+1D11E.json", true, false, false, false },
        new object[] { "i_string_iso_latin_1.json", false, false, false, false },
        new object[] { "i_string_lone_second_surrogate.json", true, false, false, false },
        new object[] { "i_string_lone_utf8_continuation_byte.json", false, false, false, false },
        new object[] { "i_string_not_in_unicode_range.json", false, false, false, false },
        new object[] { "i_string_overlong_sequence_2_bytes.json", false, false, false, false },
        new object[] { "i_string_overlong_sequence_6_bytes.json", false, false, false, false },
        new object[] { "i_string_overlong_sequence_6_bytes_null.json", false, false, false, false },
        new object[] { "i_string_truncated-utf-8.json", false, false, false, false },
        new object[] { "i_string_utf16BE_no_BOM.json", false, false, false, false },
        new object[] { "i_string_utf16LE_no_BOM.json", false, false, false, false },
        new object[] { "i_structure_500_nested_arrays.json", false, false, false, false },
        new object[] { "i_structure_UTF-8_BOM_empty_object.json", false, false, false, false }
    };

    public static IEnumerable<TestCaseData> RequiredAcceptanceCases()
    {
        foreach (string fileName in EnumerateFixtureNames("y_*.json"))
        {
            yield return new TestCaseData(fileName).SetName("JSONTestSuite_accepts_" + fileName);
        }
    }

    public static IEnumerable<TestCaseData> RequiredRejectionCases()
    {
        foreach (string fileName in EnumerateFixtureNames("n_*.json"))
        {
            yield return new TestCaseData(fileName).SetName("JSONTestSuite_rejects_" + fileName);
        }
    }

    public static IEnumerable<TestCaseData> ImplementationDefinedCases()
    {
        foreach (object[] expectation in ImplementationDefinedExpectations)
        {
            string fileName = (string)expectation[0];
            for (int profileValue = 0; profileValue <= 3; profileValue += 1)
            {
                JsonConformanceProfile profile = (JsonConformanceProfile)profileValue;
                bool expected = (bool)expectation[profileValue + 1];
                yield return new TestCaseData(fileName, profile, expected)
                    .SetName("JSONTestSuite_implementation_" + profile + "_" + fileName);
            }
        }
    }

    [Test]
    public void CorpusInventoryAndImplementationDefinedManifestAreComplete()
    {
        string[] all = EnumerateFixtureNames("*.json");
        string[] accepted = EnumerateFixtureNames("y_*.json");
        string[] rejected = EnumerateFixtureNames("n_*.json");
        string[] implementationDefined = EnumerateFixtureNames("i_*.json");
        string[] recorded = ImplementationDefinedExpectations
            .Select(expectation => (string)expectation[0])
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(all, Has.Length.EqualTo(318));
            Assert.That(accepted, Has.Length.EqualTo(95));
            Assert.That(rejected, Has.Length.EqualTo(188));
            Assert.That(implementationDefined, Has.Length.EqualTo(35));
            Assert.That(recorded, Is.Unique);
            Assert.That(recorded, Is.EquivalentTo(implementationDefined));
        });
    }

    [TestCaseSource(nameof(RequiredAcceptanceCases))]
    public void RequiredAcceptanceFixtureIsAccepted(string fileName)
    {
        JsonDocumentResult result = ParseFixture(
            fileName,
            CreateProfileOptions(JsonConformanceProfile.SyntaxCompatible));

        AssertSuccess(result, fileName, JsonConformanceProfile.SyntaxCompatible);
    }

    [TestCaseSource(nameof(RequiredRejectionCases))]
    public void RequiredRejectionFixtureIsRejected(string fileName)
    {
        JsonDocumentResult result = ParseFixture(
            fileName,
            CreateProfileOptions(JsonConformanceProfile.SyntaxCompatible));

        AssertFailure(result, fileName, JsonConformanceProfile.SyntaxCompatible);
    }

    [TestCaseSource(nameof(ImplementationDefinedCases))]
    public void ImplementationDefinedFixtureHasRecordedProfileOutcome(
        string fileName,
        JsonConformanceProfile profile,
        bool expectedSuccess)
    {
        JsonDocumentResult result = ParseFixture(fileName, CreateProfileOptions(profile));

        if (expectedSuccess)
        {
            AssertSuccess(result, fileName, profile);
        }
        else
        {
            AssertFailure(result, fileName, profile);
        }
    }

    static JsonDocumentResult ParseFixture(string fileName, JsonReaderOptions options)
    {
        string path = Path.Combine(GetParsingDirectory(), fileName);
        byte[] input = File.ReadAllBytes(path);
        IJsonDocumentParser parser = new JsonDocumentParser();
        return parser.TryParseUtf8(input, options);
    }

    static JsonReaderOptions CreateProfileOptions(JsonConformanceProfile profile)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.ConformanceProfile = profile;
        if (profile != JsonConformanceProfile.SyntaxCompatible)
        {
            options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
            options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
            options.BomPolicy = JsonBomPolicy.Reject;
        }

        return options;
    }

    static void AssertSuccess(
        JsonDocumentResult result,
        string fileName,
        JsonConformanceProfile profile)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Describe(result, fileName, profile));
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.StreamComplete),
                Describe(result, fileName, profile));
            Assert.That(result.Document, Is.Not.Null, Describe(result, fileName, profile));
            Assert.That(result.Diagnostic, Is.Null, Describe(result, fileName, profile));
        });
    }

    static void AssertFailure(
        JsonDocumentResult result,
        string fileName,
        JsonConformanceProfile profile)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, Describe(result, fileName, profile));
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.Error),
                Describe(result, fileName, profile));
            Assert.That(result.Document, Is.Null, Describe(result, fileName, profile));
            Assert.That(result.Diagnostic, Is.Not.Null, Describe(result, fileName, profile));
        });
    }

    static string Describe(
        JsonDocumentResult result,
        string fileName,
        JsonConformanceProfile profile)
    {
        return fileName + " under " + profile + ": " +
            result.Diagnostic?.Code + " " + result.Diagnostic?.Message;
    }

    static string[] EnumerateFixtureNames(string pattern)
    {
        return Directory.GetFiles(GetParsingDirectory(), pattern, SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(fileName => fileName != null)
            .Cast<string>()
            .OrderBy(fileName => fileName, StringComparer.Ordinal)
            .ToArray();
    }

    static string GetParsingDirectory()
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/JSONTestSuite/test_parsing"));
    }
}
