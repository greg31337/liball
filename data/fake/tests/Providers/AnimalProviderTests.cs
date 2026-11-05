using fake;

namespace fake.Tests;

[TestFixture]
public class AnimalProviderTests
{
    static Dictionary<string, string> Species = new Dictionary<string, string>
    {
        ["fox"] = "red fox",
        ["otter"] = "river otter",
        ["owl"] = "barn owl",
        ["rabbit"] = "European rabbit",
        ["turtle"] = "painted turtle",
        ["whale"] = "humpback whale"
    };

    static Dictionary<string, string> Habitats = new Dictionary<string, string>
    {
        ["fox"] = "woodland",
        ["otter"] = "river",
        ["owl"] = "grassland",
        ["rabbit"] = "meadow",
        ["turtle"] = "wetland",
        ["whale"] = "ocean"
    };

    static Dictionary<string, string> Breeds = new Dictionary<string, string>
    {
        ["cat"] = "domestic shorthair",
        ["chicken"] = "Plymouth Rock",
        ["dog"] = "retriever",
        ["goat"] = "pygmy",
        ["horse"] = "quarter horse",
        ["rabbit"] = "lop"
    };

    static string[] Plants = new string[]
    {
        "basil", "fern", "ivy", "mint", "moss", "rosemary"
    };

    static string[] Trees = new string[]
    {
        "birch", "cedar", "maple", "oak", "pine", "willow"
    };

    static string[] Flowers = new string[]
    {
        "aster", "daisy", "iris", "marigold", "sunflower", "tulip"
    };

    static string[] MountainStems = new string[]
    {
        "Aster Example", "Beacon Sample", "Cedar Fixture", "Harbor Test",
        "Northstar Demo", "Willow Mock"
    };

    static string[] MountainForms = new string[]
    {
        "Mountain", "Peak", "Ridge", "Summit"
    };

    static string[] Weather = new string[]
    {
        "clear", "cloudy", "foggy", "rainy", "snowy", "stormy", "windy"
    };

    [Test]
    public void AnimalSpeciesAndHabitatsArePairedAndDeterministic()
    {
        int seed = 12700;
        while (seed < 12800)
        {
            string animal = Fake.Create(seed).Animal().Animal();
            string species = Fake.Create(seed).Animal().Species();
            string habitat = Fake.Create(seed).Animal().Habitat();

            Assert.Multiple(() =>
            {
                Assert.That(Species.ContainsKey(animal), Is.True);
                Assert.That(species, Is.EqualTo(Species[animal]));
                Assert.That(habitat, Is.EqualTo(Habitats[animal]));
                Assert.That(Fake.Create(seed).Animal().Animal(),
                    Is.EqualTo(animal));
                Assert.That(Fake.Create(seed).Animal().Species(),
                    Is.EqualTo(species));
                Assert.That(Fake.Create(seed).Animal().Habitat(),
                    Is.EqualTo(habitat));
            });
            seed++;
        }
    }

    [Test]
    public void PetTypesAndBreedsArePairedAndDeterministic()
    {
        int seed = 12701;
        while (seed < 12801)
        {
            string pet = Fake.Create(seed).Animal().Pet();
            string breed = Fake.Create(seed).Animal().Breed();

            Assert.Multiple(() =>
            {
                Assert.That(Breeds.ContainsKey(pet), Is.True);
                Assert.That(breed, Is.EqualTo(Breeds[pet]));
                Assert.That(Fake.Create(seed).Animal().Pet(), Is.EqualTo(pet));
                Assert.That(Fake.Create(seed).Animal().Breed(),
                    Is.EqualTo(breed));
            });
            seed++;
        }
    }

    [Test]
    public void PlantAndWeatherCategoriesUseReviewedVocabulary()
    {
        const long seed = 12702;
        AnimalProvider first = Fake.Create(seed).Animal();
        AnimalProvider second = Fake.Create(seed).Animal();
        HashSet<string> plants = new HashSet<string>();
        HashSet<string> trees = new HashSet<string>();
        HashSet<string> flowers = new HashSet<string>();
        HashSet<string> weather = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            string plant = first.Plant();
            string tree = first.Tree();
            string flower = first.Flower();
            string condition = first.Weather();

            Assert.Multiple(() =>
            {
                Assert.That(plant, Is.EqualTo(second.Plant()));
                Assert.That(tree, Is.EqualTo(second.Tree()));
                Assert.That(flower, Is.EqualTo(second.Flower()));
                Assert.That(condition, Is.EqualTo(second.Weather()));
                Assert.That(plant, Is.AnyOf(Plants));
                Assert.That(tree, Is.AnyOf(Trees));
                Assert.That(flower, Is.AnyOf(Flowers));
                Assert.That(condition, Is.AnyOf(Weather));
            });
            plants.Add(plant);
            trees.Add(tree);
            flowers.Add(flower);
            weather.Add(condition);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(plants, Is.EquivalentTo(Plants));
            Assert.That(trees, Is.EquivalentTo(Trees));
            Assert.That(flowers, Is.EquivalentTo(Flowers));
            Assert.That(weather, Is.EquivalentTo(Weather));
        });
    }

    [Test]
    public void MountainsAreDeterministicFictionalCompositions()
    {
        const long seed = 12703;
        AnimalProvider first = Fake.Create(seed).Animal();
        AnimalProvider second = Fake.Create(seed).Animal();
        HashSet<string> observed = new HashSet<string>();

        int index = 0;
        while (index < 256)
        {
            string mountain = first.Mountain();
            int separator = mountain.LastIndexOf(' ');

            Assert.Multiple(() =>
            {
                Assert.That(mountain, Is.EqualTo(second.Mountain()));
                Assert.That(separator, Is.GreaterThan(0));
                Assert.That(mountain.Substring(0, separator),
                    Is.AnyOf(MountainStems));
                Assert.That(mountain.Substring(separator + 1),
                    Is.AnyOf(MountainForms));
            });
            observed.Add(mountain);
            index++;
        }

        Assert.That(observed.Count, Is.GreaterThan(18));
    }

    [Test]
    public void AnimalRecordsPreserveAllCorrelationsAndSafetyFlags()
    {
        const long seed = 12704;
        AnimalProvider first = Fake.Create(seed).Animal();
        AnimalProvider second = Fake.Create(seed).Animal();

        int index = 0;
        while (index < 512)
        {
            AnimalRecord value = first.Record();
            AnimalRecord same = second.Record();

            Assert.Multiple(() =>
            {
                AssertRecordEqual(value, same);
                Assert.That(value.Species,
                    Is.EqualTo(Species[value.Animal]));
                Assert.That(value.Habitat,
                    Is.EqualTo(Habitats[value.Animal]));
                Assert.That(value.Breed, Is.EqualTo(Breeds[value.Pet]));
                Assert.That(value.Plant, Is.AnyOf(Plants));
                Assert.That(value.Tree, Is.AnyOf(Trees));
                Assert.That(value.Flower, Is.AnyOf(Flowers));
                Assert.That(value.Weather, Is.AnyOf(Weather));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.RepresentsObservation, Is.False);
                Assert.That(value.MountainIsReal, Is.False);
            });
            AssertMountain(value.Mountain);
            index++;
        }
    }

    [Test]
    public void LimitsFailBeforeAnimalEntropy()
    {
        const long seed = 12705;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        AnimalProvider tinyInspected = Fake.Create(tinyOptions).Animal();
        AnimalProvider tinyUntouched = Fake.Create(tinyOptions).Animal();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 3;
        AnimalProvider countInspected = Fake.Create(countOptions).Animal();
        AnimalProvider countUntouched = Fake.Create(countOptions).Animal();

        Assert.Multiple(() =>
        {
            Assert.That(() => tinyInspected.Animal(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Species(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Habitat(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Pet(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Breed(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Plant(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Tree(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Flower(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Mountain(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Weather(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Animal(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Species(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Habitat(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Pet(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Breed(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Plant(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Tree(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Flower(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Mountain(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Weather(),
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
    public void AnimalGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(12706);
        Fake untouched = Fake.Create(12706);

        int index = 0;
        while (index < 32)
        {
            inspected.Animal().Animal();
            inspected.Animal().Species();
            inspected.Animal().Habitat();
            inspected.Animal().Pet();
            inspected.Animal().Breed();
            inspected.Animal().Plant();
            inspected.Animal().Tree();
            inspected.Animal().Flower();
            inspected.Animal().Mountain();
            inspected.Animal().Weather();
            inspected.Animal().Record();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertMountain(string value)
    {
        int separator = value.LastIndexOf(' ');
        Assert.Multiple(() =>
        {
            Assert.That(separator, Is.GreaterThan(0));
            Assert.That(value.Substring(0, separator),
                Is.AnyOf(MountainStems));
            Assert.That(value.Substring(separator + 1),
                Is.AnyOf(MountainForms));
        });
    }

    static void AssertRecordEqual(AnimalRecord first, AnimalRecord second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first.Animal, Is.EqualTo(second.Animal));
            Assert.That(first.Species, Is.EqualTo(second.Species));
            Assert.That(first.Habitat, Is.EqualTo(second.Habitat));
            Assert.That(first.Pet, Is.EqualTo(second.Pet));
            Assert.That(first.Breed, Is.EqualTo(second.Breed));
            Assert.That(first.Plant, Is.EqualTo(second.Plant));
            Assert.That(first.Tree, Is.EqualTo(second.Tree));
            Assert.That(first.Flower, Is.EqualTo(second.Flower));
            Assert.That(first.Mountain, Is.EqualTo(second.Mountain));
            Assert.That(first.Weather, Is.EqualTo(second.Weather));
            Assert.That(first.IsSynthetic, Is.EqualTo(second.IsSynthetic));
            Assert.That(first.RepresentsObservation,
                Is.EqualTo(second.RepresentsObservation));
            Assert.That(first.MountainIsReal,
                Is.EqualTo(second.MountainIsReal));
        });
    }
}
