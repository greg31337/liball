using fake;

namespace fake.Tests;

[TestFixture]
public class DeterminismAuditTests
{
    const long Seed = 15400;

    [Test]
    public void EveryProviderStreamReplaysForTheSameSeed()
    {
        FakeRandom[] first = AllProviderStreams(Create());
        FakeRandom[] second = AllProviderStreams(Create());

        Assert.That(second.Length, Is.EqualTo(first.Length));
        using (Assert.EnterMultipleScope())
        {
            int index = 0;
            while (index < first.Length)
            {
                Assert.That(second[index].Snapshot().InitialSeed,
                    Is.EqualTo(first[index].Snapshot().InitialSeed),
                    "provider stream " + index);
                Assert.That(Take(second[index], 12),
                    Is.EqualTo(Take(first[index], 12)),
                    "provider stream " + index);
                index++;
            }
        }
    }

    [Test]
    public void EveryProviderHasDeterministicPublicOutput()
    {
        string[] names = ProviderNames();
        Func<Fake, string>[] probes = ProviderProbes();

        Assert.That(probes.Length, Is.EqualTo(names.Length));
        using (Assert.EnterMultipleScope())
        {
            int index = 0;
            while (index < probes.Length)
            {
                string first = probes[index](Create());
                string second = probes[index](Create());
                Assert.That(second, Is.EqualTo(first), names[index]);
                index++;
            }
        }
    }

    [Test]
    public void EveryBundledLocaleReplaysLocalizedProviderOutput()
    {
        string[] locales = Create().Locale().Supported();

        Assert.That(locales, Is.Not.Empty);
        using (Assert.EnterMultipleScope())
        {
            int index = 0;
            while (index < locales.Length)
            {
                Fake first = Create(locales[index]);
                Fake second = Create(locales[index]);

                Assert.That(LocaleValues(second),
                    Is.EqualTo(LocaleValues(first)), locales[index]);
                index++;
            }
        }
    }

    [Test]
    public void EveryProfileReplaysAndExportsIdentically()
    {
        string[] names = new string[]
        {
            "person", "household", "company", "product", "order",
            "payment", "vehicle", "flight", "device", "student"
        };
        Func<Fake, object>[] profiles = new Func<Fake, object>[]
        {
            value => value.Profile().Person(),
            value => value.Profile().Household(),
            value => value.Profile().Company(),
            value => value.Profile().Product(),
            value => value.Profile().Order(),
            value => value.Profile().Payment(),
            value => value.Profile().Vehicle(),
            value => value.Profile().Flight(),
            value => value.Profile().Device(),
            value => value.Profile().Student()
        };

        using (Assert.EnterMultipleScope())
        {
            int index = 0;
            while (index < profiles.Length)
            {
                Fake first = Create();
                Fake second = Create();
                string expected = first.Export().Json(profiles[index](first));
                string actual = second.Export().Json(profiles[index](second));

                Assert.That(actual, Is.EqualTo(expected), names[index]);
                index++;
            }
        }
    }

    [Test]
    public void EveryExportPathReplaysIdentically()
    {
        string[] names = new string[]
        {
            "generated JSON", "generated CSV", "generated XML",
            "generated YAML", "generated SQL", "object JSON", "object CSV",
            "object XML", "vCard", "object SQL"
        };
        Func<Fake, string>[] exports = new Func<Fake, string>[]
        {
            value => value.Export().JsonFromNoArguments(),
            value => value.Export().CsvFromNoArguments(),
            value => value.Export().XmlFromNoArguments(),
            value => value.Export().Yaml(),
            value => value.Export().SqlInsert(),
            value => value.Export().Json(value.Profile().Person()),
            value => value.Export().Csv(value.Profile().Person()),
            value => value.Export().Xml(value.Profile().Person()),
            value => value.Export().VCard(value.Profile().Person()),
            value => value.Export().SqlInsertFromTableAndValue(
                "people", value.Profile().Person())
        };

        using (Assert.EnterMultipleScope())
        {
            int index = 0;
            while (index < exports.Length)
            {
                string expected = exports[index](Create());
                string actual = exports[index](Create());
                Assert.That(actual, Is.EqualTo(expected), names[index]);
                index++;
            }
        }
    }

    [Test]
    public void ForkResetAndSnapshotPathsRemainDeterministicTogether()
    {
        Fake value = Create();
        Fake expectedReset = Create();
        Take(value.Random(), 7);
        FakeState state = value.Snapshot();
        ulong[] expectedAfterSnapshot = Take(value.Random(), 16);
        ulong[] firstChild = Take(value.Fork("audit/child").Random(), 16);
        Take(value.Random(), 9);

        value.Restore(state);
        ulong[] actualAfterSnapshot = Take(value.Random(), 16);
        ulong[] secondChild = Take(value.Fork("audit/child").Random(), 16);
        value.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(actualAfterSnapshot,
                Is.EqualTo(expectedAfterSnapshot));
            Assert.That(secondChild, Is.EqualTo(firstChild));
            Assert.That(Take(value.Random(), 16),
                Is.EqualTo(Take(expectedReset.Random(), 16)));
        });
    }

    [Test]
    public void ReferenceClockIsCapturedAndReplaysEveryClockDependentPath()
    {
        FakeOptions supplied = Options();
        Fake actual = Fake.Create(supplied);
        Fake expected = Create();
        DateTimeOffset reference = supplied.ReferenceTime;

        supplied.Seed = Seed + 1;
        supplied.ReferenceTime = reference.AddYears(20);

        string[] expectedValues = ReferenceClockValues(expected);
        string[] actualValues = ReferenceClockValues(actual);
        byte[] uuid = actual.Identifier().Uuid7Bytes();

        Assert.Multiple(() =>
        {
            Assert.That(actualValues, Is.EqualTo(expectedValues));
            Assert.That(actual.Date().Rfc3339(),
                Is.EqualTo("2042-03-04T05:06:07Z"));
            Assert.That(actual.Identifier().Uuid7UnixMilliseconds(uuid),
                Is.EqualTo(reference.ToUnixTimeMilliseconds()));
        });
    }

    static Fake Create()
    {
        return Fake.Create(Options());
    }

    static Fake Create(string locale)
    {
        FakeOptions options = Options();
        options.Locale = locale;
        return Fake.Create(options);
    }

    static FakeOptions Options()
    {
        FakeOptions options = FakeOptions.Create(Seed);
        options.ReferenceTime = new DateTimeOffset(
            2042, 3, 4, 5, 6, 7, TimeSpan.Zero);
        return options;
    }

    static string[] LocaleValues(Fake value)
    {
        return new string[]
        {
            value.Date().MonthName(),
            value.Date().WeekdayName(),
            value.Person().FullName(),
            value.Address().FullAddress(),
            value.Phone().Number(),
            value.Education().Institution()
        };
    }

    static string[] ReferenceClockValues(Fake value)
    {
        return new string[]
        {
            value.Date().Rfc3339(),
            value.Date().Past().ToString("O"),
            value.Identifier().Uuid7(),
            value.Identifier().Ulid(),
            value.Identifier().ObjectId(),
            value.Identifier().SortableTimestampId(),
            value.System().LogEntry(),
            value.Git().CommitDate().ToString("O"),
            value.Export().Json(value.Person().Record()),
            value.Export().Json(value.Finance().Card()),
            value.Export().Json(value.Commerce().OrderDetails()),
            value.Export().Json(value.Vehicle().Record()),
            value.Export().Json(value.Airline().Record()),
            value.Export().Json(value.Book().Record()),
            value.Export().Json(value.Internet().JwtRecord())
        };
    }

    static string SchemaProbe(Fake value)
    {
        SchemaNode node = value.Schema().Variant(new SchemaVariant[]
        {
            value.Schema().Choice(value.Schema().Constant("first"), 1d),
            value.Schema().Choice(value.Schema().Constant("second"), 1d)
        });
        return value.Export().Json(value.Schema().EvaluateFromRoot(node));
    }

    static string Json(Fake value, object result)
    {
        return value.Export().Json(result);
    }

    static string[] ProviderNames()
    {
        return new string[]
        {
            "primitive", "text", "checksum", "identifier", "date",
            "locale", "person", "address", "internet", "phone", "device",
            "finance", "company", "commerce", "system", "database", "git",
            "color", "vehicle", "airline", "book", "food", "animal",
            "science", "health", "education", "culture", "profile", "schema",
            "edge", "export"
        };
    }

    static Func<Fake, string>[] ProviderProbes()
    {
        return new Func<Fake, string>[]
        {
            value => value.Primitive().Digits(16),
            value => value.Text().Clamp("", 16, 16),
            value => value.Checksum().Luhn().Append("7992739871"),
            value => value.Identifier().Uuid4(),
            value => value.Date().Anytime().ToString("O"),
            value => Json(value, value.Locale().Metadata(
                value.OptionsSnapshot().Locale)),
            value => value.Person().FullName(),
            value => value.Address().FullAddress(),
            value => value.Internet().Email(),
            value => value.Phone().Number(),
            value => Json(value, value.Device().Record()),
            value => Json(value, value.Finance().Money()),
            value => value.Company().Name(),
            value => Json(value, value.Commerce().Profile()),
            value => value.System().FilePath(),
            value => value.Database().ConnectionString(),
            value => value.Git().CommitEntry(),
            value => Json(value, value.Color().Record()),
            value => Json(value, value.Vehicle().Record()),
            value => Json(value, value.Airline().Record()),
            value => Json(value, value.Book().Record()),
            value => Json(value, value.Food().Record()),
            value => Json(value, value.Animal().Record()),
            value => Json(value, value.Science().Record()),
            value => Json(value, value.Health().Record()),
            value => Json(value, value.Education().Record()),
            value => Json(value, value.Culture().Record()),
            value => Json(value, value.Profile().Person()),
            value => SchemaProbe(value),
            value => Json(value, value.Edge().ValidUnusual()),
            value => value.Export().JsonFromNoArguments()
        };
    }

    static FakeRandom[] AllProviderStreams(Fake value)
    {
        return new FakeRandom[]
        {
            value.Primitive().RandomValue,
            value.Text().RandomValue,
            value.Checksum().RandomValue,
            value.Identifier().RandomValue,
            value.Date().RandomValue,
            value.Locale().RandomValue,
            value.Person().RandomValue,
            value.Address().RandomValue,
            value.Internet().RandomValue,
            value.Phone().RandomValue,
            value.Device().RandomValue,
            value.Finance().RandomValue,
            value.Company().RandomValue,
            value.Commerce().RandomValue,
            value.System().RandomValue,
            value.Database().RandomValue,
            value.Git().RandomValue,
            value.Color().RandomValue,
            value.Vehicle().RandomValue,
            value.Airline().RandomValue,
            value.Book().RandomValue,
            value.Food().RandomValue,
            value.Animal().RandomValue,
            value.Science().RandomValue,
            value.Health().RandomValue,
            value.Education().RandomValue,
            value.Culture().RandomValue,
            value.Profile().RandomValue,
            value.Schema().RandomValue,
            value.Edge().RandomValue,
            value.Export().RandomValue
        };
    }

    static ulong[] Take(FakeRandom random, int count)
    {
        ulong[] values = new ulong[count];
        int index = 0;
        while (index < count)
        {
            values[index] = random.ULong();
            index++;
        }
        return values;
    }
}
