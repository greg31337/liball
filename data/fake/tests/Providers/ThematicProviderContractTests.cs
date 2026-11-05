using fake;

namespace fake.Tests;

[TestFixture]
public class ThematicProviderContractTests
{
    [Test]
    public void EveryProviderReturnsDeterministicNonemptyCategoryMembers()
    {
        const long seed = 13200;
        Fake educationFake = Fake.Create(seed);
        EducationLocaleData education = EducationLocaleDataParser.Resolve(
            educationFake.Context);

        AssertCategory(seed, value => value.Color().Name(), ColorData.Names);
        AssertCategory(seed, value => value.Vehicle().Make(),
            VehicleData.Makes);
        AssertCategory(seed, value => value.Airline().AirlineName(),
            AirlineData.AirlineNames);
        AssertCategory(seed, value => value.Book().Genre(), BookData.Genres);
        AssertCategory(seed, value => value.Food().Cuisine(),
            FoodData.Cuisines);
        AssertCategory(seed, value => value.Animal().Animal(),
            AnimalData.Animals);
        AssertCategory(seed, value => value.Science().Field(),
            ScienceData.Fields);
        AssertCategory(seed, value => value.Health().BloodType(),
            HealthData.BloodTypes);
        AssertCategory(seed, value => value.Education().Institution(),
            Values(education.Institutions));
        AssertCategory(seed, value => value.Culture().Art(),
            CultureData.Art);
    }

    [Test]
    public void EveryProviderRecordPreservesItsCoreCorrelation()
    {
        FakeOptions options = FakeOptions.Create(13201);
        options.ReferenceTime = new DateTimeOffset(
            2042,
            3,
            4,
            5,
            6,
            7,
            TimeSpan.Zero);
        Fake value = Fake.Create(options);

        ColorRecord color = value.Color().Record();
        VehicleProfile vehicle = value.Vehicle().Record();
        FlightProfile flight = value.Airline().Record();
        BookRecord book = value.Book().Record();
        FoodRecord food = value.Food().Record();
        AnimalRecord animal = value.Animal().Record();
        ScienceRecord science = value.Science().Record();
        HealthRecord health = value.Health().Record();
        StudentProfile student = value.Education().Record();
        CultureRecord culture = value.Culture().Record();

        int colorIndex = Array.IndexOf(ColorData.Names, color.Name);
        int vehicleIndex = Array.IndexOf(VehicleData.Models, vehicle.Model);
        int airlineIndex = Array.IndexOf(
            AirlineData.AirlineNames,
            flight.AirlineName);
        int originIndex = Array.IndexOf(
            AirlineData.AirportNames,
            flight.OriginAirportName);
        int destinationIndex = Array.IndexOf(
            AirlineData.AirportNames,
            flight.DestinationAirportName);
        int dishIndex = Array.IndexOf(FoodData.Dishes, food.Dish);
        int animalIndex = Array.IndexOf(AnimalData.Animals, animal.Animal);
        int petIndex = Array.IndexOf(AnimalData.Pets, animal.Pet);
        int elementIndex = Array.IndexOf(ScienceData.Elements,
            science.Element);
        int bodyIndex = Array.IndexOf(HealthData.BodyParts,
            health.BodyPart);
        int providerIndex = Array.IndexOf(HealthData.ProviderNames,
            health.Provider);
        int artistIndex = Array.IndexOf(CultureData.MusicArtists,
            culture.MusicArtist);
        int sportIndex = Array.IndexOf(CultureData.Sports, culture.Sport);
        string[] titleParts = book.Title.Split(' ');
        string[] seriesParts = book.Series.Split(' ');
        EducationLocaleData education = EducationLocaleDataParser.Resolve(
            value.Context);
        EducationSubjectData subject = education.Subjects.Single(
            item => item.Value == student.Subject);

        Assert.Multiple(() =>
        {
            Assert.That(colorIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(color.Rgb.Red, Is.EqualTo(
                ColorData.Red[colorIndex]));
            Assert.That(color.Rgb.Green, Is.EqualTo(
                ColorData.Green[colorIndex]));
            Assert.That(color.Rgb.Blue, Is.EqualTo(
                ColorData.Blue[colorIndex]));
            Assert.That(color.Hex, Is.EqualTo(value.Color().HexFromValue(color.Rgb)));

            Assert.That(vehicleIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(vehicle.Make, Is.EqualTo(
                VehicleData.Makes[vehicleIndex]));
            Assert.That(vehicle.Type, Is.EqualTo(
                VehicleData.Types[vehicleIndex]));
            Assert.That(vehicle.Fuel, Is.EqualTo(
                VehicleData.Fuels[vehicleIndex]));

            Assert.That(airlineIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(flight.AirlineCode, Is.EqualTo(
                AirlineData.AirlineCodes[airlineIndex]));
            Assert.That(flight.OriginAirportCode, Is.EqualTo(
                AirlineData.AirportCodes[originIndex]));
            Assert.That(flight.DestinationAirportCode, Is.EqualTo(
                AirlineData.AirportCodes[destinationIndex]));
            Assert.That(flight.Route, Is.EqualTo(
                flight.OriginAirportCode + "-" +
                flight.DestinationAirportCode));
            Assert.That(flight.Arrival, Is.EqualTo(
                flight.Departure.AddMinutes(flight.DurationMinutes)));

            Assert.That(titleParts[2], Is.EqualTo(seriesParts[1]));
            Assert.That(food.Cuisine, Is.EqualTo(
                FoodData.Cuisines[dishIndex]));
            Assert.That(food.MainIngredient, Is.EqualTo(
                FoodData.MainIngredients[dishIndex]));
            Assert.That(food.Menu, Is.EqualTo(
                food.Dish + " with " + food.Beverage));

            Assert.That(animal.Species, Is.EqualTo(
                AnimalData.Species[animalIndex]));
            Assert.That(animal.Habitat, Is.EqualTo(
                AnimalData.Habitats[animalIndex]));
            Assert.That(animal.Breed, Is.EqualTo(
                AnimalData.Breeds[petIndex]));

            Assert.That(science.ElementSymbol, Is.EqualTo(
                ScienceData.ElementSymbols[elementIndex]));
            Assert.That(science.AtomicNumber, Is.EqualTo(
                ScienceData.AtomicNumbers[elementIndex]));
            Assert.That(science.Experiment, Does.EndWith(
                ScienceData.ExperimentSubjects[Array.IndexOf(
                    ScienceData.Fields,
                    science.Field)]));

            Assert.That(health.BodySystem, Is.EqualTo(
                HealthData.BodySystems[bodyIndex]));
            Assert.That(health.Facility, Is.EqualTo(
                HealthData.Facilities[providerIndex]));
            Assert.That(health.Dosage, Does.EndWith(
                health.MedicineForm + " units per fixture"));

            Assert.That(subject.Courses.Select(item => item.Value),
                Does.Contain(student.Course));
            Assert.That(subject.Degrees.Select(item => item.Value),
                Does.Contain(student.Degree));

            Assert.That(culture.MusicAlbum, Is.EqualTo(
                CultureData.MusicAlbums[artistIndex]));
            Assert.That(culture.MusicSong, Is.EqualTo(
                CultureData.MusicSongs[artistIndex]));
            Assert.That(culture.Team, Is.EqualTo(
                CultureData.Teams[sportIndex]));
            Assert.That(culture.Position, Is.EqualTo(
                CultureData.Positions[sportIndex]));
        });
    }

    [Test]
    public void EveryProviderHasStableInvariantOrDeclaredLocaleFallback()
    {
        const long seed = 13202;

        AssertLocaleStable(seed, value => value.Color().Name());
        AssertLocaleStable(seed, value => value.Vehicle().Make());
        AssertLocaleStable(seed, value => value.Airline().AirlineName());
        AssertLocaleStable(seed, value => value.Book().Genre());
        AssertLocaleStable(seed, value => value.Food().Cuisine());
        AssertLocaleStable(seed, value => value.Animal().Animal());
        AssertLocaleStable(seed, value => value.Science().Field());
        AssertLocaleStable(seed, value => value.Health().BloodType());
        AssertLocaleStable(seed, value => value.Education().Institution());
        AssertLocaleStable(seed, value => value.Culture().Art());

        Fake localized = Fake.Create("fr-CA", seed);
        localized.Education().Institution();
        FallbackTrace trace = localized.Locale().FallbackTrace(
            "education.catalog");

        Assert.Multiple(() =>
        {
            Assert.That(trace.RequestedLocale, Is.EqualTo("fr-CA"));
            Assert.That(trace.SourceLocale, Is.EqualTo("invariant"));
            Assert.That(trace.UsedFallback, Is.True);
            Assert.That(trace.AttemptedLocales,
                Is.EqualTo(new string[]
                {
                    "fr-CA", "fr", "en", "invariant"
                }));
        });
    }

    [Test]
    public void EveryProviderRecordKeepsSyntheticSafetySemantics()
    {
        Fake value = Fake.Create(13203);
        ColorRecord color = value.Color().Record();
        VehicleProfile vehicle = value.Vehicle().Record();
        FlightProfile flight = value.Airline().Record();
        BookRecord book = value.Book().Record();
        FoodRecord food = value.Food().Record();
        AnimalRecord animal = value.Animal().Record();
        ScienceRecord science = value.Science().Record();
        HealthRecord health = value.Health().Record();
        StudentProfile student = value.Education().Record();
        CultureRecord culture = value.Culture().Record();
        EducationLocaleData education = EducationLocaleDataParser.Resolve(
            value.Context);

        Assert.Multiple(() =>
        {
            Assert.That(color.IsSynthetic, Is.True);

            Assert.That(vehicle.IsSynthetic, Is.True);
            Assert.That(vehicle.VinIsAssigned, Is.False);
            Assert.That(vehicle.RegistrationIsAssigned, Is.False);
            Assert.That(vehicle.RegistrationPlate, Does.StartWith("TEST-"));

            Assert.That(flight.IsSynthetic, Is.True);
            Assert.That(flight.AirlineCodeIsAssigned, Is.False);
            Assert.That(flight.AirportCodesAreAssigned, Is.False);
            Assert.That(flight.RecordLocatorIsBooked, Is.False);

            Assert.That(book.IsSynthetic, Is.True);
            Assert.That(book.IsbnIsAssigned, Is.False);
            Assert.That(book.RepresentsPublishedWork, Is.False);

            Assert.That(food.IsSynthetic, Is.True);
            Assert.That(food.ClaimsSuitability, Is.False);
            Assert.That(food.Nutrition.IsSynthetic, Is.True);
            Assert.That(food.Nutrition.IsMeasured, Is.False);

            Assert.That(animal.IsSynthetic, Is.True);
            Assert.That(animal.RepresentsObservation, Is.False);
            Assert.That(animal.MountainIsReal, Is.False);

            Assert.That(science.IsSynthetic, Is.True);
            Assert.That(science.RepresentsExperiment, Is.False);
            Assert.That(science.Experiment, Does.StartWith("TEST-"));
            Assert.That(science.Measurement.IsSynthetic, Is.True);
            Assert.That(science.Measurement.IsMeasured, Is.False);

            Assert.That(health.IsSynthetic, Is.True);
            Assert.That(health.ForClinicalUse, Is.False);
            Assert.That(health.CodesAreAssigned, Is.False);
            Assert.That(health.RepresentsPatient, Is.False);
            Assert.That(health.RepresentsRealProviderOrFacility, Is.False);
            Assert.That(health.ProcedureCode, Does.StartWith("TEST-PX-"));
            Assert.That(health.DiagnosisCode, Does.StartWith("TEST-DX-"));

            Assert.That(education.StudentIdMasks.Any(
                mask => MatchesMask(student.StudentId, mask.Value)), Is.True);

            Assert.That(culture.IsSynthetic, Is.True);
            Assert.That(culture.RepresentsRealArtistOrTeam, Is.False);
            Assert.That(culture.IsOperationalGuidance, Is.False);
        });
    }

    static void AssertCategory(
        long seed,
        Func<Fake, string> select,
        string[] admitted)
    {
        string first = select(Fake.Create(seed));
        string second = select(Fake.Create(seed));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null.And.Not.Empty);
            Assert.That(first, Is.EqualTo(second));
            Assert.That(admitted, Does.Contain(first));
        });
    }

    static void AssertLocaleStable(long seed, Func<Fake, string> select)
    {
        string frenchCanadian = select(Fake.Create("fr-CA", seed));
        string japanese = select(Fake.Create("ja-JP", seed));

        Assert.Multiple(() =>
        {
            Assert.That(frenchCanadian, Is.Not.Null.And.Not.Empty);
            Assert.That(japanese, Is.EqualTo(frenchCanadian));
        });
    }

    static string[] Values(EducationValueData[] values)
    {
        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index].Value;
            index++;
        }
        return result;
    }

    static bool MatchesMask(string value, string mask)
    {
        int valueIndex = 0;
        int maskIndex = 0;
        while (maskIndex < mask.Length && valueIndex < value.Length)
        {
            char expected = mask[maskIndex];
            if (expected == '\\')
            {
                maskIndex++;
                if (maskIndex >= mask.Length ||
                    value[valueIndex] != mask[maskIndex])
                    return false;
            }
            else if (expected == '#')
            {
                if (value[valueIndex] < '0' || value[valueIndex] > '9')
                    return false;
            }
            else if (expected == '?')
            {
                char actual = value[valueIndex];
                bool upper = actual >= 'A' && actual <= 'Z';
                bool lower = actual >= 'a' && actual <= 'z';
                if (!upper && !lower)
                    return false;
            }
            else if (expected == '*')
            {
                char actual = value[valueIndex];
                bool digit = actual >= '0' && actual <= '9';
                bool upper = actual >= 'A' && actual <= 'Z';
                bool lower = actual >= 'a' && actual <= 'z';
                if (!digit && !upper && !lower)
                    return false;
            }
            else if (value[valueIndex] != expected)
                return false;
            maskIndex++;
            valueIndex++;
        }
        return maskIndex == mask.Length && valueIndex == value.Length;
    }
}
