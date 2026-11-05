using fake;

namespace fake.Tests;

[TestFixture]
public class VehicleProviderTests
{
    static HashSet<string> FixtureSignatures = new HashSet<string>(new string[]
    {
        "Example Aster Motors|Meridian Example|sedan|gasoline",
        "Example Aster Motors|Horizon Fixture|sport utility vehicle|hybrid",
        "Fixture Beacon Automotive|Cityline Sample|hatchback|battery electric",
        "Fixture Beacon Automotive|Trailmark Test|pickup|diesel",
        "Sample Calder Mobility|Willow Demo|wagon|hybrid",
        "Sample Calder Mobility|Vector Mock|coupe|gasoline",
        "Test Northstar Vehicles|Atlas Example|van|battery electric",
        "Test Northstar Vehicles|Summit Fixture|sport utility vehicle|hydrogen"
    });

    static string[] Bicycles = new string[]
    {
        "cargo bicycle", "folding bicycle", "hybrid bicycle",
        "mountain bicycle", "road bicycle", "touring bicycle"
    };

    static string[] Routes = new string[]
    {
        "Cedar Loop", "Harbor Connector", "Market Crosstown",
        "North Ridge", "River Express", "South Campus"
    };

    static string[] TransitModes = new string[]
    {
        "bus", "commuter rail", "ferry", "light rail", "metro", "tram"
    };

    static string[] Colors = new string[]
    {
        "black", "blue", "gray", "green", "red", "silver", "white"
    };

    [Test]
    public void ComponentVocabularyIsDeterministicReviewedData()
    {
        const long seed = 12300;
        VehicleProvider first = Fake.Create(seed).Vehicle();
        VehicleProvider second = Fake.Create(seed).Vehicle();
        HashSet<string> makes = new HashSet<string>();
        HashSet<string> models = new HashSet<string>();
        HashSet<string> types = new HashSet<string>();
        HashSet<string> fuels = new HashSet<string>();
        HashSet<string> bicycles = new HashSet<string>();
        HashSet<string> routes = new HashSet<string>();
        HashSet<string> transitModes = new HashSet<string>();

        int index = 0;
        while (index < 512)
        {
            string make = first.Make();
            string model = first.Model();
            string type = first.Type();
            string fuel = first.Fuel();
            string bicycle = first.Bicycle();
            string route = first.Route();
            string transitMode = first.TransitMode();

            Assert.Multiple(() =>
            {
                Assert.That(make, Is.EqualTo(second.Make()));
                Assert.That(model, Is.EqualTo(second.Model()));
                Assert.That(type, Is.EqualTo(second.Type()));
                Assert.That(fuel, Is.EqualTo(second.Fuel()));
                Assert.That(bicycle, Is.EqualTo(second.Bicycle()));
                Assert.That(route, Is.EqualTo(second.Route()));
                Assert.That(transitMode, Is.EqualTo(second.TransitMode()));
                Assert.That(bicycle, Is.AnyOf(Bicycles));
                Assert.That(route, Is.AnyOf(Routes));
                Assert.That(transitMode, Is.AnyOf(TransitModes));
            });
            makes.Add(make);
            models.Add(model);
            types.Add(type);
            fuels.Add(fuel);
            bicycles.Add(bicycle);
            routes.Add(route);
            transitModes.Add(transitMode);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(makes, Is.EquivalentTo(new string[]
            {
                "Example Aster Motors", "Fixture Beacon Automotive",
                "Sample Calder Mobility", "Test Northstar Vehicles"
            }));
            Assert.That(models, Is.EquivalentTo(new string[]
            {
                "Meridian Example", "Horizon Fixture", "Cityline Sample",
                "Trailmark Test", "Willow Demo", "Vector Mock",
                "Atlas Example", "Summit Fixture"
            }));
            Assert.That(types, Is.EquivalentTo(new string[]
            {
                "sedan", "sport utility vehicle", "hatchback", "pickup",
                "wagon", "coupe", "van"
            }));
            Assert.That(fuels, Is.EquivalentTo(new string[]
            {
                "gasoline", "hybrid", "battery electric", "diesel",
                "hydrogen"
            }));
            Assert.That(bicycles, Is.EquivalentTo(Bicycles));
            Assert.That(routes, Is.EquivalentTo(Routes));
            Assert.That(transitModes, Is.EquivalentTo(TransitModes));
        });
    }

    [Test]
    public void PlatesAndVinsAreDeterministicSyntheticShapes()
    {
        const long seed = 12301;
        VehicleProvider first = Fake.Create(seed).Vehicle();
        VehicleProvider second = Fake.Create(seed).Vehicle();
        IdentifierProvider identifiers = Fake.Create(seed).Identifier();
        HashSet<string> plates = new HashSet<string>();
        HashSet<string> vins = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            string plate = first.RegistrationPlate();
            string vin = first.VinFromModelYear(2028);

            Assert.Multiple(() =>
            {
                Assert.That(plate,
                    Is.EqualTo(second.RegistrationPlate()));
                Assert.That(vin, Is.EqualTo(second.VinFromModelYear(2028)));
                Assert.That(plate, Does.Match("^TEST-[0-9]{4}$"));
                Assert.That(vin, Does.Match("^[A-HJ-NPR-Z0-9]{17}$"));
                Assert.That(identifiers.ValidateVin(vin), Is.True);
                Assert.That(identifiers.VinModelYear(vin), Is.EqualTo(2028));
            });
            plates.Add(plate);
            vins.Add(vin);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(plates.Count, Is.GreaterThan(200));
            Assert.That(vins, Has.Count.EqualTo(256));
            Assert.That(Fake.Create(seed).Vehicle().Plate(),
                Is.EqualTo(Fake.Create(seed).Vehicle().RegistrationPlate()));
        });
    }

    [Test]
    public void RecordsKeepVehicleFixturesAndIdentifiersCorrelated()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2032,
            6,
            7,
            8,
            9,
            10,
            TimeSpan.Zero);
        FakeOptions firstOptions = FakeOptions.Create(12302);
        firstOptions.ReferenceTime = reference;
        FakeOptions secondOptions = FakeOptions.Create(12302);
        secondOptions.ReferenceTime = reference;
        VehicleProvider first = Fake.Create(firstOptions).Vehicle();
        VehicleProvider second = Fake.Create(secondOptions).Vehicle();
        IdentifierProvider identifiers = Fake.Create(12302).Identifier();
        HashSet<string> observedFixtures = new HashSet<string>();

        int index = 0;
        while (index < 512)
        {
            VehicleProfile record = first.Record();
            VehicleProfile same = second.Record();
            string fixture = record.Make + "|" + record.Model + "|" +
                record.Type + "|" + record.Fuel;

            Assert.Multiple(() =>
            {
                AssertRecordEqual(record, same);
                Assert.That(FixtureSignatures, Does.Contain(fixture));
                Assert.That(record.ModelYear, Is.InRange(2017, 2032));
                Assert.That(identifiers.ValidateVin(record.Vin), Is.True);
                Assert.That(identifiers.VinModelYear(record.Vin),
                    Is.EqualTo(record.ModelYear));
                Assert.That(record.RegistrationPlate,
                    Does.Match("^TEST-[0-9]{4}$"));
                Assert.That(record.Color, Is.AnyOf(Colors));
                Assert.That(record.Route, Is.AnyOf(Routes));
                Assert.That(record.TransitMode, Is.AnyOf(TransitModes));
                Assert.That(record.IsSynthetic, Is.True);
                Assert.That(record.VinIsAssigned, Is.False);
                Assert.That(record.RegistrationIsAssigned, Is.False);
            });
            observedFixtures.Add(fixture);
            index++;
        }

        Assert.That(observedFixtures, Is.EquivalentTo(FixtureSignatures));
    }

    [Test]
    public void IndividualVehicleFieldsUseTheSameFixtureAtTheSameSeed()
    {
        const long seed = 12303;
        VehicleProfile record = Fake.Create(seed).Vehicle().Record();

        Assert.Multiple(() =>
        {
            Assert.That(Fake.Create(seed).Vehicle().Make(),
                Is.EqualTo(record.Make));
            Assert.That(Fake.Create(seed).Vehicle().Model(),
                Is.EqualTo(record.Model));
            Assert.That(Fake.Create(seed).Vehicle().Type(),
                Is.EqualTo(record.Type));
            Assert.That(Fake.Create(seed).Vehicle().Fuel(),
                Is.EqualTo(record.Fuel));
        });
    }

    [Test]
    public void ReferenceYearsClampToTheSupportedVinWindow()
    {
        FakeOptions minimumOptions = FakeOptions.Create(12304);
        minimumOptions.ReferenceTime = DateTimeOffset.MinValue;
        Fake minimumFake = Fake.Create(minimumOptions);
        string minimumVin = minimumFake.Vehicle().Vin();
        VehicleProfile minimumRecord = Fake.Create(minimumOptions).Vehicle().Record();
        FakeOptions maximumOptions = FakeOptions.Create(12304);
        maximumOptions.ReferenceTime = DateTimeOffset.MaxValue;
        Fake maximumFake = Fake.Create(maximumOptions);
        string maximumVin = maximumFake.Vehicle().Vin();
        VehicleProfile maximumRecord = Fake.Create(maximumOptions).Vehicle().Record();

        Assert.Multiple(() =>
        {
            Assert.That(minimumFake.Identifier().VinModelYear(minimumVin),
                Is.EqualTo(1980));
            Assert.That(minimumRecord.ModelYear, Is.EqualTo(1980));
            Assert.That(minimumFake.Identifier().VinModelYear(
                minimumRecord.Vin), Is.EqualTo(1980));
            Assert.That(maximumFake.Identifier().VinModelYear(maximumVin),
                Is.EqualTo(2039));
            Assert.That(maximumRecord.ModelYear, Is.InRange(2024, 2039));
            Assert.That(maximumFake.Identifier().VinModelYear(
                maximumRecord.Vin), Is.EqualTo(maximumRecord.ModelYear));
        });
    }

    [Test]
    public void LimitsAndInvalidYearsFailBeforeVehicleEntropy()
    {
        const long seed = 12305;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        VehicleProvider tinyInspected = Fake.Create(tinyOptions).Vehicle();
        VehicleProvider tinyUntouched = Fake.Create(tinyOptions).Vehicle();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 5;
        VehicleProvider countInspected = Fake.Create(countOptions).Vehicle();
        VehicleProvider countUntouched = Fake.Create(countOptions).Vehicle();

        Assert.Multiple(() =>
        {
            Assert.That(() => tinyInspected.Make(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Model(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Type(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Fuel(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Bicycle(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.RegistrationPlate(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Vin(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Route(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.TransitMode(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Make(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Bicycle(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Route(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.TransitMode(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.VinFromModelYear(1979),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.VinFromModelYear(2040),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(tinyInspected.RandomValue.Long(),
                Is.EqualTo(tinyUntouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void VehicleGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(12306);
        Fake untouched = Fake.Create(12306);

        int index = 0;
        while (index < 32)
        {
            inspected.Vehicle().Make();
            inspected.Vehicle().Model();
            inspected.Vehicle().Type();
            inspected.Vehicle().Fuel();
            inspected.Vehicle().Bicycle();
            inspected.Vehicle().RegistrationPlate();
            inspected.Vehicle().Vin();
            inspected.Vehicle().Route();
            inspected.Vehicle().TransitMode();
            inspected.Vehicle().Record();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertRecordEqual(
        VehicleProfile first,
        VehicleProfile second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first.Make, Is.EqualTo(second.Make));
            Assert.That(first.Model, Is.EqualTo(second.Model));
            Assert.That(first.Type, Is.EqualTo(second.Type));
            Assert.That(first.Fuel, Is.EqualTo(second.Fuel));
            Assert.That(first.ModelYear, Is.EqualTo(second.ModelYear));
            Assert.That(first.Vin, Is.EqualTo(second.Vin));
            Assert.That(first.RegistrationPlate,
                Is.EqualTo(second.RegistrationPlate));
            Assert.That(first.Color, Is.EqualTo(second.Color));
            Assert.That(first.Route, Is.EqualTo(second.Route));
            Assert.That(first.TransitMode, Is.EqualTo(second.TransitMode));
            Assert.That(first.IsSynthetic, Is.EqualTo(second.IsSynthetic));
            Assert.That(first.VinIsAssigned,
                Is.EqualTo(second.VinIsAssigned));
            Assert.That(first.RegistrationIsAssigned,
                Is.EqualTo(second.RegistrationIsAssigned));
        });
    }
}
