namespace fake;

internal static class FoodGenerator
{
    const int MinimumServingGrams = 50;
    const int MaximumServingGrams = 500;
    const int MaximumProteinGrams = 40;
    const int MinimumCarbohydrateGrams = 5;
    const int MaximumCarbohydrateGrams = 100;
    const int MaximumFatGrams = 30;
    const int MaximumSodiumMilligrams = 1500;

    internal static string Cuisine(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateDishData(context);
        return Value(context, random, FoodData.Cuisines, "cuisine");
    }

    internal static string Dish(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateDishData(context);
        return Value(context, random, FoodData.Dishes, "dish");
    }

    internal static string Ingredient(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, FoodData.Ingredients, "ingredientData");
        return Value(context, random, FoodData.Ingredients, "ingredient");
    }

    internal static string Produce(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, FoodData.Produce, "produceData");
        return Value(context, random, FoodData.Produce, "produce");
    }

    internal static string Meat(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, FoodData.Meats, "meatData");
        return Value(context, random, FoodData.Meats, "meat");
    }

    internal static string Spice(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, FoodData.Spices, "spiceData");
        return Value(context, random, FoodData.Spices, "spice");
    }

    internal static string Beverage(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, FoodData.Beverages, "beverageData");
        return Value(context, random, FoodData.Beverages, "beverage");
    }

    internal static string Diet(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, FoodData.Diets, "dietData");
        return Value(context, random, FoodData.Diets, "diet");
    }

    internal static string Allergen(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, FoodData.Allergens, "allergenData");
        return Value(context, random, FoodData.Allergens, "allergen");
    }

    internal static string Menu(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateDishData(context);
        ValidateValues(context, FoodData.Beverages, "beverageData");
        Guard.OutputSize(
            MaximumMenuLength(),
            context.CapturedOptions.MaximumOutputSize,
            "menu");
        int dishIndex = random.IntFromMaximumExclusive(FoodData.Dishes.Length);
        int beverageIndex = random.IntFromMaximumExclusive(FoodData.Beverages.Length);
        return MenuUnchecked(dishIndex, beverageIndex);
    }

    internal static NutritionRecord Nutrition(FakeRandom random)
    {
        return NutritionUnchecked(random);
    }

    internal static FoodRecord Record(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateDishData(context);
        ValidateValues(context, FoodData.Produce, "produceData");
        ValidateValues(context, FoodData.Meats, "meatData");
        ValidateValues(context, FoodData.Spices, "spiceData");
        ValidateValues(context, FoodData.Beverages, "beverageData");
        ValidateValues(context, FoodData.Diets, "dietData");
        ValidateValues(context, FoodData.Allergens, "allergenData");
        Guard.OutputSize(
            MaximumRecordSize(),
            context.CapturedOptions.MaximumOutputSize,
            "foodRecord");
        int dishIndex = random.IntFromMaximumExclusive(FoodData.Dishes.Length);
        int beverageIndex = random.IntFromMaximumExclusive(FoodData.Beverages.Length);
        FoodRecord result = new FoodRecord();
        result.Cuisine = FoodData.Cuisines[dishIndex];
        result.Dish = FoodData.Dishes[dishIndex];
        result.MainIngredient = FoodData.MainIngredients[dishIndex];
        result.Produce = FoodData.Produce[random.IntFromMaximumExclusive(FoodData.Produce.Length)];
        result.Meat = FoodData.Meats[random.IntFromMaximumExclusive(FoodData.Meats.Length)];
        result.Spice = FoodData.Spices[random.IntFromMaximumExclusive(FoodData.Spices.Length)];
        result.Beverage = FoodData.Beverages[beverageIndex];
        result.Diet = FoodData.Diets[random.IntFromMaximumExclusive(FoodData.Diets.Length)];
        result.Allergen = FoodData.Allergens[
            random.IntFromMaximumExclusive(FoodData.Allergens.Length)];
        result.Menu = MenuUnchecked(dishIndex, beverageIndex);
        result.Nutrition = NutritionUnchecked(random);
        result.IsSynthetic = true;
        result.ClaimsSuitability = false;
        return result;
    }

    static string MenuUnchecked(int dishIndex, int beverageIndex)
    {
        return FoodData.Dishes[dishIndex] + " with " +
            FoodData.Beverages[beverageIndex];
    }

    static NutritionRecord NutritionUnchecked(FakeRandom random)
    {
        NutritionRecord result = new NutritionRecord();
        result.ServingGrams = random.IntFromMinimumAndMaximumAndMaximumInclusive(
            MinimumServingGrams,
            MaximumServingGrams,
            true);
        result.ProteinGrams = random.IntFromMinimumAndMaximumAndMaximumInclusive(0, MaximumProteinGrams, true);
        result.CarbohydrateGrams = random.IntFromMinimumAndMaximumAndMaximumInclusive(
            MinimumCarbohydrateGrams,
            MaximumCarbohydrateGrams,
            true);
        result.FatGrams = random.IntFromMinimumAndMaximumAndMaximumInclusive(0, MaximumFatGrams, true);
        result.Calories = result.ProteinGrams * 4 +
            result.CarbohydrateGrams * 4 + result.FatGrams * 9;
        result.SodiumMilligrams = random.IntFromMinimumAndMaximumAndMaximumInclusive(
            0,
            MaximumSodiumMilligrams,
            true);
        result.IsSynthetic = true;
        result.IsMeasured = false;
        return result;
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

    static void ValidateDishData(GenerationContext context)
    {
        if (FoodData.Cuisines.Length != FoodData.Dishes.Length ||
            FoodData.Dishes.Length != FoodData.MainIngredients.Length)
        {
            throw FakeException.Create(
                "The project-authored food table has inconsistent column lengths.");
        }
        ValidateValues(context, FoodData.Cuisines, "dishData");
        ValidateValues(context, FoodData.Dishes, "dishData");
        ValidateValues(context, FoodData.MainIngredients, "dishData");
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
                    "A project-authored food data table is malformed.");
            }
            index = index + 1;
        }
    }

    static int MaximumMenuLength()
    {
        return MaximumLength(FoodData.Dishes) + 6 +
            MaximumLength(FoodData.Beverages);
    }

    static int MaximumRecordSize()
    {
        return MaximumLength(FoodData.Cuisines) +
            MaximumLength(FoodData.Dishes) +
            MaximumLength(FoodData.MainIngredients) +
            MaximumLength(FoodData.Produce) +
            MaximumLength(FoodData.Meats) +
            MaximumLength(FoodData.Spices) +
            MaximumLength(FoodData.Beverages) +
            MaximumLength(FoodData.Diets) +
            MaximumLength(FoodData.Allergens) + MaximumMenuLength();
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
