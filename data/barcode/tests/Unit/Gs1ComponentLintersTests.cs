namespace barcode.Tests;

public class Gs1ComponentLintersTests
{
    [Test]
    public void DispatcherCoversEveryLinterReferencedByTheAiTable()
    {
        global::System.Collections.Generic.HashSet<string> referenced =
            new global::System.Collections.Generic.HashSet<string>();
        int definitionIndex = 0;
        while (definitionIndex < Gs1AiTable.Definitions.Length)
        {
            Gs1AiComponentDefinition[] components =
                Gs1AiTable.Definitions[definitionIndex].Components;
            int componentIndex = 0;
            while (componentIndex < components.Length)
            {
                string[] linters = components[componentIndex].Linters;
                int linterIndex = 0;
                while (linterIndex < linters.Length)
                {
                    referenced.Add(linters[linterIndex]);
                    linterIndex++;
                }

                componentIndex++;
            }

            definitionIndex++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(Gs1ComponentLinters.Names,
                Is.Ordered.And.Unique.And.EqualTo(referenced.OrderBy(name => name)));
            Assert.That(Gs1ComponentLinters.Names,
                Has.All.Matches<string>(Gs1ComponentLinters.SupportsLinter));
            Assert.That(Gs1ComponentLinters.SupportsLinter("unknown"), Is.False);
            Assert.That(Gs1ComponentLinters.Lint("unknown", "data").Code,
                Is.EqualTo("GS1_LINTER_UNKNOWN"));
        });
    }

    [TestCase("couponcode", "012345612345611110123")]
    [TestCase("couponposoffer", "001234561234560123456")]
    [TestCase("csum", "02345673")]
    [TestCase("csumalpha", "1987654Ad4X4bL5ttr2310c2K")]
    [TestCase("gcppos1", "1234")]
    [TestCase("gcppos2", "01234")]
    [TestCase("hasnondigit", "/")]
    [TestCase("hh", "23")]
    [TestCase("hhmi", "2359")]
    [TestCase("hyphen", "---")]
    [TestCase("iban", "FR7630006000011234567890189")]
    [TestCase("importeridx", "_")]
    [TestCase("iso3166", "840")]
    [TestCase("iso3166999", "999")]
    [TestCase("iso3166alpha2", "US")]
    [TestCase("iso4217", "840")]
    [TestCase("iso5218", "9")]
    [TestCase("latitude", "1800000000")]
    [TestCase("longitude", "3600000000")]
    [TestCase("mediatype", "99")]
    [TestCase("mi", "59")]
    [TestCase("nonzero", "0001")]
    [TestCase("nozeroprefix", "123")]
    [TestCase("packagetype", "X20")]
    [TestCase("pcenc", "A%2fB")]
    [TestCase("pieceoftotal", "9899")]
    [TestCase("posinseqslash", "99/100")]
    [TestCase("ss", "59")]
    [TestCase("winding", "9")]
    [TestCase("yesno", "1")]
    [TestCase("yymmd0", "200200")]
    [TestCase("yymmdd", "200229")]
    [TestCase("yyyymmdd", "20000229")]
    [TestCase("zero", "000")]
    public void ReferenceValidValuesPass(string name, string data)
    {
        Gs1LinterResult result = Gs1ComponentLinters.Lint(name, data);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Code);
            Assert.That(result.Code, Is.EqualTo("GS1_LINTER_OK"));
            Assert.That(result.ErrorOffset, Is.Zero);
            Assert.That(result.ErrorLength, Is.Zero);
        });
    }

    [TestCase("csum", "02345674", "GS1_LINTER_INCORRECT_CHECK_DIGIT", 7, 1)]
    [TestCase("csumalpha", "33", "GS1_LINTER_INCORRECT_CHECK_PAIR", 0, 2)]
    [TestCase("gcppos1", "12A4", "GS1_LINTER_INVALID_GCP_PREFIX", 2, 1)]
    [TestCase("gcppos2", "012A4", "GS1_LINTER_INVALID_GCP_PREFIX", 3, 1)]
    [TestCase("hasnondigit", "123", "GS1_LINTER_REQUIRES_NON_DIGIT_CHARACTER", 0, 3)]
    [TestCase("hh", "24", "GS1_LINTER_ILLEGAL_HOUR", 0, 2)]
    [TestCase("hhmi", "1260", "GS1_LINTER_ILLEGAL_MINUTE", 2, 2)]
    [TestCase("hyphen", "-x", "GS1_LINTER_NOT_HYPHEN", 0, 2)]
    [TestCase("iban", "BE71096123456760", "GS1_LINTER_INCORRECT_IBAN_CHECKSUM", 2, 2)]
    [TestCase("importeridx", "@", "GS1_LINTER_INVALID_IMPORT_IDX_CHARACTER", 0, 1)]
    [TestCase("iso3166", "000", "GS1_LINTER_NOT_ISO3166", 0, 3)]
    [TestCase("iso3166999", "000", "GS1_LINTER_NOT_ISO3166_OR_999", 0, 3)]
    [TestCase("iso3166alpha2", "AA", "GS1_LINTER_NOT_ISO3166_ALPHA2", 0, 2)]
    [TestCase("iso4217", "000", "GS1_LINTER_NOT_ISO4217", 0, 3)]
    [TestCase("iso5218", "3", "GS1_LINTER_INVALID_BIOLOGICAL_SEX_CODE", 0, 1)]
    [TestCase("latitude", "1800000001", "GS1_LINTER_INVALID_LATITUDE", 0, 10)]
    [TestCase("longitude", "3600000001", "GS1_LINTER_INVALID_LONGITUDE", 0, 10)]
    [TestCase("mediatype", "11", "GS1_LINTER_INVALID_MEDIA_TYPE", 0, 2)]
    [TestCase("mi", "60", "GS1_LINTER_ILLEGAL_MINUTE", 0, 2)]
    [TestCase("nonzero", "000", "GS1_LINTER_ILLEGAL_ZERO_VALUE", 0, 3)]
    [TestCase("nozeroprefix", "01", "GS1_LINTER_ILLEGAL_ZERO_PREFIX", 0, 1)]
    [TestCase("packagetype", "0", "GS1_LINTER_INVALID_PACKAGE_TYPE", 0, 1)]
    [TestCase("pcenc", "A%2G", "GS1_LINTER_INVALID_PERCENT_SEQUENCE", 1, 3)]
    [TestCase("pieceoftotal", "21", "GS1_LINTER_PIECE_NUMBER_EXCEEDS_TOTAL", 0, 2)]
    [TestCase("posinseqslash", "3/2", "GS1_LINTER_POSITION_EXCEEDS_END", 0, 3)]
    [TestCase("ss", "60", "GS1_LINTER_ILLEGAL_SECOND", 0, 2)]
    [TestCase("winding", "2", "GS1_LINTER_INVALID_WINDING_DIRECTION", 0, 1)]
    [TestCase("yesno", "2", "GS1_LINTER_NOT_ZERO_OR_ONE", 0, 1)]
    [TestCase("yymmd0", "200230", "GS1_LINTER_ILLEGAL_DAY", 4, 2)]
    [TestCase("yymmdd", "200200", "GS1_LINTER_ILLEGAL_DAY", 4, 2)]
    [TestCase("yyyymmdd", "19000229", "GS1_LINTER_ILLEGAL_DAY", 6, 2)]
    [TestCase("zero", "001", "GS1_LINTER_NOT_ZERO", 0, 3)]
    public void ReferenceInvalidValuesPreserveCodeAndErrorSpan(
        string name,
        string data,
        string code,
        int offset,
        int length)
    {
        Gs1LinterResult result = Gs1ComponentLinters.Lint(name, data);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(code));
            Assert.That(result.ErrorOffset, Is.EqualTo(offset));
            Assert.That(result.ErrorLength, Is.EqualTo(length));
        });
    }

    [Test]
    public void CouponLintersHandleOptionalFieldsAndTruncation()
    {
        Gs1LinterResult optionals = Gs1ComponentLinters.Lint(
            "couponcode",
            "106141411234562891101201212085010048000214025610048000310123191000");
        Gs1LinterResult truncated = Gs1ComponentLinters.Lint(
            "couponcode",
            "01234561234561");
        Gs1LinterResult excess = Gs1ComponentLinters.Lint(
            "couponposoffer",
            "0012345612345601234567");

        Assert.Multiple(() =>
        {
            Assert.That(optionals.Success, Is.True, optionals.Code);
            AssertFailure(
                truncated,
                "GS1_LINTER_COUPON_TRUNCATED_SAVE_VALUE",
                0,
                14);
            AssertFailure(
                excess,
                "GS1_LINTER_COUPON_EXCESS_DATA",
                21,
                1);
        });
    }

    [Test]
    public void NullInputsReturnDeterministicFailure()
    {
        Gs1LinterResult nullName = Gs1ComponentLinters.Lint(null, "data");
        Gs1LinterResult nullData = Gs1ComponentLinters.Lint("csum", null);

        Assert.Multiple(() =>
        {
            AssertFailure(nullName, "GS1_LINTER_NULL_INPUT", 0, 0);
            AssertFailure(nullData, "GS1_LINTER_NULL_INPUT", 0, 0);
        });
    }

    static void AssertFailure(
        Gs1LinterResult result,
        string code,
        int offset,
        int length)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(code));
            Assert.That(result.ErrorOffset, Is.EqualTo(offset));
            Assert.That(result.ErrorLength, Is.EqualTo(length));
        });
    }
}
