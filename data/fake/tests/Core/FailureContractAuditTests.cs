using System.Text;
using fake;

namespace fake.Tests;

[TestFixture]
public class FailureContractAuditTests
{
    [Test]
    public void PublicEntryPointsReportTheFailingArgument()
    {
        Fake value = Fake.Create(15500);
        DateTimeOffset instant = new DateTimeOffset(
            2040, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(
                () => Fake.Create((FakeOptions)null)).ParamName,
                Is.EqualTo("options"));
            Assert.That(Assert.Throws<ArgumentNullException>(
                () => value.Fork(null)).ParamName,
                Is.EqualTo("name"));
            Assert.That(Assert.Throws<ArgumentNullException>(
                () => value.Random().Choice((string[])null)).ParamName,
                Is.EqualTo("values"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(
                () => value.Primitive().Digits(-1)).ParamName,
                Is.EqualTo("count"));
            Assert.That(Assert.Throws<ArgumentNullException>(
                () => value.Text().Truncate(null, 1)).ParamName,
                Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(
                () => value.Identifier().NanoIdFromLength(-1)).ParamName,
                Is.EqualTo("length"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(
                () => value.Date().Between(instant, instant)).ParamName,
                Is.EqualTo("maximum"));
            Assert.That(Assert.Throws<ArgumentNullException>(
                () => value.Locale().Resolve(null)).ParamName,
                Is.EqualTo("field"));
            Assert.That(Assert.Throws<ArgumentNullException>(
                () => value.Internet().ToAsciiDomain(null)).ParamName,
                Is.EqualTo("domainName"));
            Assert.That(Assert.Throws<ArgumentNullException>(
                () => value.Export().JsonString(null)).ParamName,
                Is.EqualTo("value"));
        });
    }

    [Test]
    public void CountLimitIsEnforcedAcrossCoreProvidersAndComposition()
    {
        FakeOptions options = FakeOptions.Create(15501);
        options.MaximumCount = 1;
        Fake value = Fake.Create(options);
        string[] names = new string[]
        {
            "random sample", "random choices", "text words", "date values",
            "color palette", "schema array", "CSV rows"
        };
        Action[] operations = new Action[]
        {
            () => value.Random().Sample(new string[] { "a", "b" }, 2),
            () => value.Random().Choices(new string[] { "a" }, 2),
            () => value.Text().Words(2),
            () => value.Date().Betweens(
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch.AddDays(1),
                2),
            () => value.Color().PaletteFromCount(2),
            () => value.Schema().EvaluateFromRoot(value.Schema().Array(
                value.Schema().Constant("a"), 2)),
            () => value.Export().CsvFromRows(new object[] { "a", "b" })
        };

        AssertLimitFailures(names, operations);
    }

    [Test]
    public void OutputLimitIsEnforcedAcrossCoreProvidersAndComposition()
    {
        FakeOptions options = FakeOptions.Create(15502);
        options.MaximumOutputSize = 1;
        Fake value = Fake.Create(options);
        string[] names = new string[]
        {
            "primitive", "text", "checksum", "identifier", "date",
            "person", "address", "internet", "phone", "device", "finance",
            "company", "commerce", "system", "database", "git", "color",
            "vehicle", "airline", "book", "media", "food", "animal", "science",
            "health", "education", "culture", "profile", "schema", "edge",
            "export"
        };
        Action[] operations = new Action[]
        {
            () => value.Primitive().Digits(2),
            () => value.Text().Text(2),
            () => value.Checksum().Luhn().Append("1"),
            () => value.Identifier().Uuid4(),
            () => value.Date().MonthName(),
            () => value.Person().FullName(),
            () => value.Address().FullAddress(),
            () => value.Internet().Email(),
            () => value.Phone().Number(),
            () => value.Device().Manufacturer(),
            () => value.Finance().CurrencyName(),
            () => value.Company().Name(),
            () => value.Commerce().Product(),
            () => value.System().FileName(),
            () => value.Database().DatabaseName(),
            () => value.Git().Branch(),
            () => value.Color().Name(),
            () => value.Vehicle().Make(),
            () => value.Airline().AirlineName(),
            () => value.Book().Title(),
            () => value.Media().MovieTitle(),
            () => value.Food().Dish(),
            () => value.Animal().Animal(),
            () => value.Science().Field(),
            () => value.Health().BloodType(),
            () => value.Education().Institution(),
            () => value.Culture().Art(),
            () => value.Profile().Person(),
            () => value.Schema().EvaluateFromRoot(value.Schema().Constant("ab")),
            () => value.Edge().LongRepeatedTextFromLength(2),
            () => value.Export().JsonString("ab")
        };

        AssertLimitFailures(names, operations);
    }

    [Test]
    public void EveryDatePresentationStringHonorsTheOutputLimit()
    {
        FakeOptions options = FakeOptions.Create(15506);
        options.MaximumOutputSize = 1;
        DateProvider date = Fake.Create(options).Date();
        DateTimeOffset instant = DateTimeOffset.UnixEpoch;

        Assert.Multiple(() =>
        {
            Assert.That(() => date.MonthName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => date.MonthNameFromMonth(1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => date.WeekdayName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => date.WeekdayNameFromWeekday(DayOfWeek.Monday),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => date.Rfc3339(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => date.Rfc3339FromPrecision(TemporalPrecision.Ticks),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => date.Rfc3339FromValue(instant),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => date.Rfc3339FromValueAndPrecision(
                instant, TemporalPrecision.Milliseconds),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void EveryUniquenessEntryPointUsesItsExactRetryBudget()
    {
        FakeOptions options = FakeOptions.Create(15503);
        options.UniquenessRetryLimit = 2;
        Fake value = Fake.Create(options);
        UniqueScope random = value.Random().Unique("random-audit");
        random.Next(() => "same");
        value.Profile().UniqueFromNameAndGenerate("profile-audit", () => "same");
        SchemaNode schemaNode = value.Schema().UniqueFromNameAndSourceAndRetryLimit(
            "schema-audit", value.Schema().Constant("same"), 2);
        value.Schema().EvaluateFromRoot(schemaNode);
        int randomCalls = 0;
        int profileCalls = 0;

        FakeException randomException = Assert.Throws<FakeException>(() =>
            random.Next(() =>
            {
                randomCalls++;
                return "same";
            }));
        FakeException profileException = Assert.Throws<FakeException>(() =>
            value.Profile().UniqueFromNameAndGenerate("profile-audit", () =>
            {
                profileCalls++;
                return "same";
            }));
        FakeException schemaException = Assert.Throws<FakeException>(() =>
            value.Schema().EvaluateFromRoot(schemaNode));

        Assert.Multiple(() =>
        {
            Assert.That(randomCalls, Is.EqualTo(2));
            Assert.That(profileCalls, Is.EqualTo(2));
            Assert.That(randomException.Message,
                Does.Contain("random-audit").And.Contain("2 attempts"));
            Assert.That(profileException.Message,
                Does.Contain("profile-audit").And.Contain("2 attempts"));
            Assert.That(schemaException.Message,
                Does.Contain("schema-audit").And.Contain("2 attempts"));
        });
    }

    [Test]
    public void UnsupportedLocalesAndFieldsHaveStableFallbackDiagnostics()
    {
        FakeOptions fallbackOptions = FakeOptions.Create(
            "sr-Cyrl-RS", "en", 15504);
        Fake fallback = Fake.Create(fallbackOptions);
        FakeOptions strictOptions = fallbackOptions.Copy();
        strictOptions.StrictnessMode = StrictnessMode.Strict;
        Fake strict = Fake.Create(strictOptions);
        LocaleFieldValue[] noValues = Array.Empty<LocaleFieldValue>();

        FakeException metadata = Assert.Throws<FakeException>(
            () => fallback.Locale().Metadata("sr-Cyrl-RS"));
        FakeException missing = Assert.Throws<FakeException>(
            () => fallback.Locale().ResolveFromFieldAndValues("missing.audit", noValues));
        FakeException strictMissing = Assert.Throws<FakeException>(
            () => strict.Locale().ResolveFromFieldAndValues("missing.audit", noValues));

        Assert.Multiple(() =>
        {
            Assert.That(fallback.Locale().IsSupported("sr-Cyrl-RS"), Is.False);
            Assert.That(metadata.Message,
                Is.EqualTo(
                    "Locale 'sr-Cyrl-RS' has no native fields in the locale catalog."));
            Assert.That(missing.Message,
                Is.EqualTo(
                    "Locale field 'missing.audit' is unavailable for locale " +
                    "'sr-Cyrl-RS' after trying sr-Cyrl-RS -> sr-Cyrl -> sr -> " +
                    "en -> invariant."));
            Assert.That(strictMissing.Message,
                Is.EqualTo(
                    "Locale field 'missing.audit' is unavailable for exact " +
                    "locale 'sr-Cyrl-RS' because strict fallback is enabled."));
        });
    }

    [TestCase("{")]
    [TestCase("[1,]")]
    [TestCase("{\"$pack\":\"\\uD800\"}")]
    public void MalformedLocaleResourcesNameTheCallerResourceAndRemainAtomic(
        string json)
    {
        Fake value = Fake.Create(15505);
        value.Locale().RegisterFromLocaleAndFields(
            "en",
            new Dictionary<string, object> { ["audit.existing"] = "value" });
        const string resourceName = "malformed-audit-pack.json";

        FakeException exception = Assert.Throws<FakeException>(() =>
            value.Locale().RegisterFromStreamAndResourceName(Stream(json), resourceName));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message,
                Does.StartWith(
                    "Malformed locale pack resource '" + resourceName +
                    "' at character"));
            Assert.That(value.Locale().Resolve("audit.existing"),
                Is.EqualTo("value"));
            Assert.That(value.Context.LocaleRegistrations.RegistrationCount(),
                Is.EqualTo(1));
        });
    }

    static MemoryStream Stream(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value));
    }

    static void AssertLimitFailures(string[] names, Action[] operations)
    {
        Assert.That(operations.Length, Is.EqualTo(names.Length));
        using (Assert.EnterMultipleScope())
        {
            int index = 0;
            while (index < operations.Length)
            {
                Assert.That(operations[index],
                    Throws.TypeOf<ArgumentOutOfRangeException>(), names[index]);
                index++;
            }
        }
    }
}
