using fake;

namespace fake.Tests;

[TestFixture]
public class ScienceProviderTests
{
    static Dictionary<string, string> ExperimentSubjects =
        new Dictionary<string, string>
        {
            ["astronomy"] = "orbital sample",
            ["biology"] = "growth sample",
            ["chemistry"] = "reaction sample",
            ["geology"] = "mineral sample",
            ["meteorology"] = "weather sample",
            ["physics"] = "motion sample"
        };

    static Dictionary<string, string> ElementSymbols =
        new Dictionary<string, string>
        {
            ["hydrogen"] = "H",
            ["carbon"] = "C",
            ["nitrogen"] = "N",
            ["oxygen"] = "O",
            ["silicon"] = "Si",
            ["iron"] = "Fe"
        };

    static Dictionary<string, int> AtomicNumbers =
        new Dictionary<string, int>
        {
            ["hydrogen"] = 1,
            ["carbon"] = 6,
            ["nitrogen"] = 7,
            ["oxygen"] = 8,
            ["silicon"] = 14,
            ["iron"] = 26
        };

    static Dictionary<string, string> Quantities =
        new Dictionary<string, string>
        {
            ["metre"] = "length",
            ["gram"] = "mass",
            ["second"] = "time",
            ["litre"] = "volume",
            ["watt"] = "power",
            ["volt"] = "voltage"
        };

    static Dictionary<string, string> UnitSymbols =
        new Dictionary<string, string>
        {
            ["metre"] = "m",
            ["gram"] = "g",
            ["second"] = "s",
            ["litre"] = "L",
            ["watt"] = "W",
            ["volt"] = "V"
        };

    static Dictionary<string, string> PrefixSymbols =
        new Dictionary<string, string>
        {
            ["nano"] = "n",
            ["micro"] = "μ",
            ["milli"] = "m",
            ["centi"] = "c",
            ["kilo"] = "k",
            ["mega"] = "M",
            ["giga"] = "G"
        };

    static Dictionary<string, int> PrefixExponents =
        new Dictionary<string, int>
        {
            ["nano"] = -9,
            ["micro"] = -6,
            ["milli"] = -3,
            ["centi"] = -2,
            ["kilo"] = 3,
            ["mega"] = 6,
            ["giga"] = 9
        };

    static string[] Planets = new string[]
    {
        "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn",
        "Uranus", "Neptune"
    };

    static string[] SpaceTerms = new string[]
    {
        "asteroid", "comet", "galaxy", "moon", "nebula", "orbit", "star"
    };

    [Test]
    public void FieldsAndExperimentLabelsArePairedAndDeterministic()
    {
        int seed = 12800;
        while (seed < 12900)
        {
            string field = Fake.Create(seed).Science().Field();
            string experiment = Fake.Create(seed).Science().Experiment();

            Assert.Multiple(() =>
            {
                Assert.That(ExperimentSubjects.ContainsKey(field), Is.True);
                Assert.That(experiment,
                    Does.Match("^TEST-[0-9]{4}: (Compare|Measure|Observe|Record|Simulate|Test) .+ sample$"));
                Assert.That(experiment,
                    Does.EndWith(ExperimentSubjects[field]));
                Assert.That(Fake.Create(seed).Science().Field(),
                    Is.EqualTo(field));
                Assert.That(Fake.Create(seed).Science().Experiment(),
                    Is.EqualTo(experiment));
            });
            seed++;
        }
    }

    [Test]
    public void ElementsUnitsAndPrefixesUseCorrelatedReviewedMetadata()
    {
        const long seed = 12801;
        ScienceProvider first = Fake.Create(seed).Science();
        ScienceProvider second = Fake.Create(seed).Science();
        HashSet<string> elements = new HashSet<string>();
        HashSet<string> units = new HashSet<string>();
        HashSet<string> prefixes = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            string element = first.Element();
            string unit = first.Unit();
            string prefix = first.SiPrefix();

            Assert.Multiple(() =>
            {
                Assert.That(element, Is.EqualTo(second.Element()));
                Assert.That(unit, Is.EqualTo(second.Unit()));
                Assert.That(prefix, Is.EqualTo(second.SiPrefix()));
                Assert.That(ElementSymbols.ContainsKey(element), Is.True);
                Assert.That(Quantities.ContainsKey(unit), Is.True);
                Assert.That(PrefixSymbols.ContainsKey(prefix), Is.True);
            });
            elements.Add(element);
            units.Add(unit);
            prefixes.Add(prefix);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(elements, Is.EquivalentTo(ElementSymbols.Keys));
            Assert.That(units, Is.EquivalentTo(Quantities.Keys));
            Assert.That(prefixes, Is.EquivalentTo(PrefixSymbols.Keys));
        });
    }

    [Test]
    public void MeasurementsCorrelateUnitsPrefixesAndInvariantFormatting()
    {
        const long seed = 12802;
        ScienceProvider first = Fake.Create(seed).Science();
        ScienceProvider second = Fake.Create(seed).Science();

        int index = 0;
        while (index < 512)
        {
            MeasurementRecord value = first.Measurement();
            MeasurementRecord same = second.Measurement();

            Assert.Multiple(() =>
            {
                AssertMeasurementEqual(value, same);
                Assert.That(value.Quantity,
                    Is.EqualTo(Quantities[value.Unit]));
                Assert.That(value.UnitSymbol,
                    Is.EqualTo(UnitSymbols[value.Unit]));
                Assert.That(value.PrefixSymbol,
                    Is.EqualTo(PrefixSymbols[value.SiPrefix]));
                Assert.That(value.PrefixExponent,
                    Is.EqualTo(PrefixExponents[value.SiPrefix]));
                Assert.That(value.Value, Is.InRange(0.01m, 100m));
                Assert.That(value.Formatted, Is.EqualTo(
                    value.Value.ToString("0.00",
                        System.Globalization.CultureInfo.InvariantCulture) +
                    " " + value.PrefixSymbol + value.UnitSymbol));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.IsMeasured, Is.False);
            });
            index++;
        }

        Assert.That(Fake.Create(seed).Science().Measurement().Unit,
            Is.EqualTo(Fake.Create(seed).Science().Unit()));
    }

    [Test]
    public void PlanetAndSpaceTermsAreDeterministicReviewedVocabulary()
    {
        const long seed = 12803;
        ScienceProvider first = Fake.Create(seed).Science();
        ScienceProvider second = Fake.Create(seed).Science();
        HashSet<string> planets = new HashSet<string>();
        HashSet<string> space = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            string planet = first.Planet();
            string term = first.Space();
            Assert.Multiple(() =>
            {
                Assert.That(planet, Is.EqualTo(second.Planet()));
                Assert.That(term, Is.EqualTo(second.Space()));
                Assert.That(planet, Is.AnyOf(Planets));
                Assert.That(term, Is.AnyOf(SpaceTerms));
            });
            planets.Add(planet);
            space.Add(term);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(planets, Is.EquivalentTo(Planets));
            Assert.That(space, Is.EquivalentTo(SpaceTerms));
        });
    }

    [Test]
    public void ScienceRecordsPreserveMetadataAndExperimentCorrelation()
    {
        const long seed = 12804;
        ScienceProvider first = Fake.Create(seed).Science();
        ScienceProvider second = Fake.Create(seed).Science();

        int index = 0;
        while (index < 512)
        {
            ScienceRecord value = first.Record();
            ScienceRecord same = second.Record();

            Assert.Multiple(() =>
            {
                AssertScienceEqual(value, same);
                Assert.That(value.ElementSymbol,
                    Is.EqualTo(ElementSymbols[value.Element]));
                Assert.That(value.AtomicNumber,
                    Is.EqualTo(AtomicNumbers[value.Element]));
                Assert.That(value.Experiment,
                    Does.EndWith(ExperimentSubjects[value.Field]));
                Assert.That(value.Planet, Is.AnyOf(Planets));
                Assert.That(value.Space, Is.AnyOf(SpaceTerms));
                Assert.That(value.Measurement.Quantity,
                    Is.EqualTo(Quantities[value.Measurement.Unit]));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.RepresentsExperiment, Is.False);
            });
            index++;
        }
    }

    [Test]
    public void LimitsFailBeforeScienceEntropy()
    {
        const long seed = 12805;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        ScienceProvider tinyInspected = Fake.Create(tinyOptions).Science();
        ScienceProvider tinyUntouched = Fake.Create(tinyOptions).Science();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 3;
        ScienceProvider countInspected = Fake.Create(countOptions).Science();
        ScienceProvider countUntouched = Fake.Create(countOptions).Science();

        Assert.Multiple(() =>
        {
            Assert.That(() => tinyInspected.Field(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Element(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Unit(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.SiPrefix(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Measurement(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Planet(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Space(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Experiment(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Field(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Element(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Unit(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.SiPrefix(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Measurement(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Planet(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Space(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Experiment(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(tinyInspected.RandomValue.Long(),
                Is.EqualTo(tinyUntouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void ScienceGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(12806);
        Fake untouched = Fake.Create(12806);

        int index = 0;
        while (index < 32)
        {
            inspected.Science().Field();
            inspected.Science().Element();
            inspected.Science().Unit();
            inspected.Science().SiPrefix();
            inspected.Science().Measurement();
            inspected.Science().Planet();
            inspected.Science().Space();
            inspected.Science().Experiment();
            inspected.Science().Record();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertMeasurementEqual(
        MeasurementRecord first,
        MeasurementRecord second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first.Quantity, Is.EqualTo(second.Quantity));
            Assert.That(first.Value, Is.EqualTo(second.Value));
            Assert.That(first.SiPrefix, Is.EqualTo(second.SiPrefix));
            Assert.That(first.PrefixSymbol,
                Is.EqualTo(second.PrefixSymbol));
            Assert.That(first.PrefixExponent,
                Is.EqualTo(second.PrefixExponent));
            Assert.That(first.Unit, Is.EqualTo(second.Unit));
            Assert.That(first.UnitSymbol, Is.EqualTo(second.UnitSymbol));
            Assert.That(first.Formatted, Is.EqualTo(second.Formatted));
            Assert.That(first.IsSynthetic, Is.EqualTo(second.IsSynthetic));
            Assert.That(first.IsMeasured, Is.EqualTo(second.IsMeasured));
        });
    }

    static void AssertScienceEqual(ScienceRecord first, ScienceRecord second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first.Field, Is.EqualTo(second.Field));
            Assert.That(first.Element, Is.EqualTo(second.Element));
            Assert.That(first.ElementSymbol,
                Is.EqualTo(second.ElementSymbol));
            Assert.That(first.AtomicNumber, Is.EqualTo(second.AtomicNumber));
            AssertMeasurementEqual(first.Measurement, second.Measurement);
            Assert.That(first.Planet, Is.EqualTo(second.Planet));
            Assert.That(first.Space, Is.EqualTo(second.Space));
            Assert.That(first.Experiment, Is.EqualTo(second.Experiment));
            Assert.That(first.IsSynthetic, Is.EqualTo(second.IsSynthetic));
            Assert.That(first.RepresentsExperiment,
                Is.EqualTo(second.RepresentsExperiment));
        });
    }
}
