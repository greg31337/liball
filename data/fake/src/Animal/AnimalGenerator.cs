namespace fake;

internal static class AnimalGenerator
{
    internal static string Animal(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAnimalData(context);
        return Value(context, random, AnimalData.Animals, "animal");
    }

    internal static string Species(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAnimalData(context);
        return Value(context, random, AnimalData.Species, "species");
    }

    internal static string Habitat(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAnimalData(context);
        return Value(context, random, AnimalData.Habitats, "habitat");
    }

    internal static string Pet(
        GenerationContext context,
        FakeRandom random)
    {
        ValidatePetData(context);
        return Value(context, random, AnimalData.Pets, "pet");
    }

    internal static string Breed(
        GenerationContext context,
        FakeRandom random)
    {
        ValidatePetData(context);
        return Value(context, random, AnimalData.Breeds, "breed");
    }

    internal static string Plant(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, AnimalData.Plants, "plantData");
        return Value(context, random, AnimalData.Plants, "plant");
    }

    internal static string Tree(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, AnimalData.Trees, "treeData");
        return Value(context, random, AnimalData.Trees, "tree");
    }

    internal static string Flower(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, AnimalData.Flowers, "flowerData");
        return Value(context, random, AnimalData.Flowers, "flower");
    }

    internal static string Mountain(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateMountainData(context);
        Guard.OutputSize(
            MaximumMountainLength(),
            context.CapturedOptions.MaximumOutputSize,
            "mountain");
        return MountainUnchecked(random);
    }

    internal static string Weather(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, AnimalData.Weather, "weatherData");
        return Value(context, random, AnimalData.Weather, "weather");
    }

    internal static AnimalRecord Record(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAnimalData(context);
        ValidatePetData(context);
        ValidateValues(context, AnimalData.Plants, "plantData");
        ValidateValues(context, AnimalData.Trees, "treeData");
        ValidateValues(context, AnimalData.Flowers, "flowerData");
        ValidateMountainData(context);
        ValidateValues(context, AnimalData.Weather, "weatherData");
        Guard.OutputSize(
            MaximumRecordSize(),
            context.CapturedOptions.MaximumOutputSize,
            "animalRecord");
        int animalIndex = random.IntFromMaximumExclusive(AnimalData.Animals.Length);
        int petIndex = random.IntFromMaximumExclusive(AnimalData.Pets.Length);
        AnimalRecord result = new AnimalRecord();
        result.Animal = AnimalData.Animals[animalIndex];
        result.Species = AnimalData.Species[animalIndex];
        result.Habitat = AnimalData.Habitats[animalIndex];
        result.Pet = AnimalData.Pets[petIndex];
        result.Breed = AnimalData.Breeds[petIndex];
        result.Plant = AnimalData.Plants[
            random.IntFromMaximumExclusive(AnimalData.Plants.Length)];
        result.Tree = AnimalData.Trees[random.IntFromMaximumExclusive(AnimalData.Trees.Length)];
        result.Flower = AnimalData.Flowers[
            random.IntFromMaximumExclusive(AnimalData.Flowers.Length)];
        result.Mountain = MountainUnchecked(random);
        result.Weather = AnimalData.Weather[
            random.IntFromMaximumExclusive(AnimalData.Weather.Length)];
        result.IsSynthetic = true;
        result.RepresentsObservation = false;
        result.MountainIsReal = false;
        return result;
    }

    static string MountainUnchecked(FakeRandom random)
    {
        return AnimalData.MountainStems[
            random.IntFromMaximumExclusive(AnimalData.MountainStems.Length)] + " " +
            AnimalData.MountainForms[
                random.IntFromMaximumExclusive(AnimalData.MountainForms.Length)];
    }

    static string Value(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string outputName)
    {
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            outputName);
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static void ValidateAnimalData(GenerationContext context)
    {
        if (AnimalData.Animals.Length != AnimalData.Species.Length ||
            AnimalData.Species.Length != AnimalData.Habitats.Length)
        {
            throw FakeException.Create(
                "The project-authored animal table has inconsistent column lengths.");
        }
        ValidateValues(context, AnimalData.Animals, "animalData");
        ValidateValues(context, AnimalData.Species, "animalData");
        ValidateValues(context, AnimalData.Habitats, "animalData");
    }

    static void ValidatePetData(GenerationContext context)
    {
        if (AnimalData.Pets.Length != AnimalData.Breeds.Length)
        {
            throw FakeException.Create(
                "The project-authored pet table has inconsistent column lengths.");
        }
        ValidateValues(context, AnimalData.Pets, "petData");
        ValidateValues(context, AnimalData.Breeds, "petData");
    }

    static void ValidateMountainData(GenerationContext context)
    {
        ValidateValues(context, AnimalData.MountainStems, "mountainData");
        ValidateValues(context, AnimalData.MountainForms, "mountainData");
    }

    static void ValidateValues(
        GenerationContext context,
        string[] values,
        string dataName)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            dataName);
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null || values[index].Length == 0)
            {
                throw FakeException.Create(
                    "A project-authored nature data table is malformed.");
            }
            index = index + 1;
        }
    }

    static int MaximumMountainLength()
    {
        return MaximumLength(AnimalData.MountainStems) + 1 +
            MaximumLength(AnimalData.MountainForms);
    }

    static int MaximumRecordSize()
    {
        return MaximumLength(AnimalData.Animals) +
            MaximumLength(AnimalData.Species) +
            MaximumLength(AnimalData.Habitats) +
            MaximumLength(AnimalData.Pets) +
            MaximumLength(AnimalData.Breeds) +
            MaximumLength(AnimalData.Plants) +
            MaximumLength(AnimalData.Trees) +
            MaximumLength(AnimalData.Flowers) + MaximumMountainLength() +
            MaximumLength(AnimalData.Weather);
    }

    static int MaximumLength(string[] values)
    {
        int maximum = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > maximum)
            {
                maximum = values[index].Length;
            }
            index = index + 1;
        }
        return maximum;
    }
}
