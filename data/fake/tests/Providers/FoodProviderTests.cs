using fake;

namespace fake.Tests;

[TestFixture]
public class FoodProviderTests
{
    static Dictionary<string, string> Dishes = new Dictionary<string, string>
    {
        ["coastal"] = "Citrus Grain Bowl",
        ["garden"] = "Herb Vegetable Tart",
        ["hearth"] = "Roasted Root Plate",
        ["market"] = "Spiced Lentil Wrap",
        ["orchard"] = "Apple Oat Bake",
        ["pantry"] = "Tomato Bean Stew"
    };

    static Dictionary<string, string> MainIngredients =
        new Dictionary<string, string>
        {
            ["Citrus Grain Bowl"] = "brown rice",
            ["Herb Vegetable Tart"] = "zucchini",
            ["Roasted Root Plate"] = "carrot",
            ["Spiced Lentil Wrap"] = "lentils",
            ["Apple Oat Bake"] = "apple",
            ["Tomato Bean Stew"] = "white beans"
        };

    static string[] Ingredients = new string[]
    {
        "barley", "chickpeas", "garlic", "lemon", "olive oil", "oats",
        "onion", "tomato"
    };

    static string[] Produce = new string[]
    {
        "apple", "broccoli", "carrot", "pear", "spinach", "tomato",
        "zucchini"
    };

    static string[] Meats = new string[]
    {
        "beef", "chicken", "lamb", "pork", "turkey"
    };

    static string[] Spices = new string[]
    {
        "black pepper", "cardamom", "cinnamon", "cumin", "paprika",
        "turmeric"
    };

    static string[] Beverages = new string[]
    {
        "apple juice", "black tea", "coffee", "herbal tea", "lemonade",
        "sparkling water"
    };

    static string[] Diets = new string[]
    {
        "dairy-free", "gluten-free", "omnivore", "pescatarian", "vegan",
        "vegetarian"
    };

    static string[] Allergens = new string[]
    {
        "egg", "fish", "milk", "peanut", "sesame", "shellfish", "soy",
        "tree nut", "wheat"
    };

    [Test]
    public void CuisineAndDishFixturesArePairedAndDeterministic()
    {
        int seed = 12600;
        while (seed < 12700)
        {
            string cuisine = Fake.Create(seed).Food().Cuisine();
            string dish = Fake.Create(seed).Food().Dish();

            Assert.Multiple(() =>
            {
                Assert.That(Dishes.ContainsKey(cuisine), Is.True);
                Assert.That(dish, Is.EqualTo(Dishes[cuisine]));
                Assert.That(Fake.Create(seed).Food().Cuisine(),
                    Is.EqualTo(cuisine));
                Assert.That(Fake.Create(seed).Food().Dish(), Is.EqualTo(dish));
            });
            seed++;
        }
    }

    [Test]
    public void FoodCategoriesUseReviewedGenericVocabulary()
    {
        const long seed = 12601;
        FoodProvider first = Fake.Create(seed).Food();
        FoodProvider second = Fake.Create(seed).Food();
        HashSet<string> ingredients = new HashSet<string>();
        HashSet<string> produce = new HashSet<string>();
        HashSet<string> meats = new HashSet<string>();
        HashSet<string> spices = new HashSet<string>();
        HashSet<string> beverages = new HashSet<string>();
        HashSet<string> diets = new HashSet<string>();
        HashSet<string> allergens = new HashSet<string>();

        int index = 0;
        while (index < 512)
        {
            string ingredient = first.Ingredient();
            string produceItem = first.Produce();
            string meat = first.Meat();
            string spice = first.Spice();
            string beverage = first.Beverage();
            string diet = first.Diet();
            string allergen = first.Allergen();

            Assert.Multiple(() =>
            {
                Assert.That(ingredient, Is.EqualTo(second.Ingredient()));
                Assert.That(produceItem, Is.EqualTo(second.Produce()));
                Assert.That(meat, Is.EqualTo(second.Meat()));
                Assert.That(spice, Is.EqualTo(second.Spice()));
                Assert.That(beverage, Is.EqualTo(second.Beverage()));
                Assert.That(diet, Is.EqualTo(second.Diet()));
                Assert.That(allergen, Is.EqualTo(second.Allergen()));
                Assert.That(ingredient, Is.AnyOf(Ingredients));
                Assert.That(produceItem, Is.AnyOf(Produce));
                Assert.That(meat, Is.AnyOf(Meats));
                Assert.That(spice, Is.AnyOf(Spices));
                Assert.That(beverage, Is.AnyOf(Beverages));
                Assert.That(diet, Is.AnyOf(Diets));
                Assert.That(allergen, Is.AnyOf(Allergens));
            });
            ingredients.Add(ingredient);
            produce.Add(produceItem);
            meats.Add(meat);
            spices.Add(spice);
            beverages.Add(beverage);
            diets.Add(diet);
            allergens.Add(allergen);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(ingredients, Is.EquivalentTo(Ingredients));
            Assert.That(produce, Is.EquivalentTo(Produce));
            Assert.That(meats, Is.EquivalentTo(Meats));
            Assert.That(spices, Is.EquivalentTo(Spices));
            Assert.That(beverages, Is.EquivalentTo(Beverages));
            Assert.That(diets, Is.EquivalentTo(Diets));
            Assert.That(allergens, Is.EquivalentTo(Allergens));
        });
    }

    [Test]
    public void MenuEntriesComposeReviewedDishesAndBeverages()
    {
        const long seed = 12602;
        FoodProvider first = Fake.Create(seed).Food();
        FoodProvider second = Fake.Create(seed).Food();

        int index = 0;
        while (index < 256)
        {
            string menu = first.Menu();
            int separator = menu.IndexOf(" with ",
                StringComparison.Ordinal);

            Assert.Multiple(() =>
            {
                Assert.That(menu, Is.EqualTo(second.Menu()));
                Assert.That(separator, Is.GreaterThan(0));
                Assert.That(menu.Substring(0, separator),
                    Is.AnyOf(Dishes.Values));
                Assert.That(menu.Substring(separator + 6),
                    Is.AnyOf(Beverages));
            });
            index++;
        }
    }

    [Test]
    public void NutritionFixturesAreDeterministicAndInternallyConsistent()
    {
        const long seed = 12603;
        FoodProvider first = Fake.Create(seed).Food();
        FoodProvider second = Fake.Create(seed).Food();

        int index = 0;
        while (index < 512)
        {
            NutritionRecord value = first.Nutrition();
            NutritionRecord same = second.Nutrition();

            Assert.Multiple(() =>
            {
                AssertNutritionEqual(value, same);
                Assert.That(value.ServingGrams, Is.InRange(50, 500));
                Assert.That(value.ProteinGrams, Is.InRange(0, 40));
                Assert.That(value.CarbohydrateGrams, Is.InRange(5, 100));
                Assert.That(value.FatGrams, Is.InRange(0, 30));
                Assert.That(value.Calories, Is.EqualTo(
                    value.ProteinGrams * 4 +
                    value.CarbohydrateGrams * 4 +
                    value.FatGrams * 9));
                Assert.That(value.SodiumMilligrams, Is.InRange(0, 1500));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.IsMeasured, Is.False);
            });
            index++;
        }
    }

    [Test]
    public void FoodRecordsKeepDishIngredientMenuAndNutritionCorrelated()
    {
        const long seed = 12604;
        FoodProvider first = Fake.Create(seed).Food();
        FoodProvider second = Fake.Create(seed).Food();

        int index = 0;
        while (index < 512)
        {
            FoodRecord value = first.Record();
            FoodRecord same = second.Record();

            Assert.Multiple(() =>
            {
                AssertFoodEqual(value, same);
                Assert.That(value.Dish, Is.EqualTo(Dishes[value.Cuisine]));
                Assert.That(value.MainIngredient,
                    Is.EqualTo(MainIngredients[value.Dish]));
                Assert.That(value.Produce, Is.AnyOf(Produce));
                Assert.That(value.Meat, Is.AnyOf(Meats));
                Assert.That(value.Spice, Is.AnyOf(Spices));
                Assert.That(value.Beverage, Is.AnyOf(Beverages));
                Assert.That(value.Diet, Is.AnyOf(Diets));
                Assert.That(value.Allergen, Is.AnyOf(Allergens));
                Assert.That(value.Menu,
                    Is.EqualTo(value.Dish + " with " + value.Beverage));
                Assert.That(value.Nutrition.Calories, Is.EqualTo(
                    value.Nutrition.ProteinGrams * 4 +
                    value.Nutrition.CarbohydrateGrams * 4 +
                    value.Nutrition.FatGrams * 9));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.ClaimsSuitability, Is.False);
            });
            index++;
        }
    }

    [Test]
    public void LimitsFailBeforeFoodEntropy()
    {
        const long seed = 12605;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        FoodProvider tinyInspected = Fake.Create(tinyOptions).Food();
        FoodProvider tinyUntouched = Fake.Create(tinyOptions).Food();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 4;
        FoodProvider countInspected = Fake.Create(countOptions).Food();
        FoodProvider countUntouched = Fake.Create(countOptions).Food();

        Assert.Multiple(() =>
        {
            Assert.That(() => tinyInspected.Cuisine(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Dish(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Ingredient(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Produce(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Meat(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Spice(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Beverage(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Diet(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Allergen(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Menu(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tinyInspected.Record(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Cuisine(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Dish(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Ingredient(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Produce(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Meat(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Spice(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Beverage(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Diet(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Allergen(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => countInspected.Menu(),
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
    public void FoodGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(12606);
        Fake untouched = Fake.Create(12606);

        int index = 0;
        while (index < 32)
        {
            inspected.Food().Cuisine();
            inspected.Food().Dish();
            inspected.Food().Ingredient();
            inspected.Food().Produce();
            inspected.Food().Meat();
            inspected.Food().Spice();
            inspected.Food().Beverage();
            inspected.Food().Diet();
            inspected.Food().Allergen();
            inspected.Food().Menu();
            inspected.Food().Nutrition();
            inspected.Food().Record();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertNutritionEqual(
        NutritionRecord first,
        NutritionRecord second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first.ServingGrams, Is.EqualTo(second.ServingGrams));
            Assert.That(first.Calories, Is.EqualTo(second.Calories));
            Assert.That(first.ProteinGrams,
                Is.EqualTo(second.ProteinGrams));
            Assert.That(first.CarbohydrateGrams,
                Is.EqualTo(second.CarbohydrateGrams));
            Assert.That(first.FatGrams, Is.EqualTo(second.FatGrams));
            Assert.That(first.SodiumMilligrams,
                Is.EqualTo(second.SodiumMilligrams));
            Assert.That(first.IsSynthetic, Is.EqualTo(second.IsSynthetic));
            Assert.That(first.IsMeasured, Is.EqualTo(second.IsMeasured));
        });
    }

    static void AssertFoodEqual(FoodRecord first, FoodRecord second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first.Cuisine, Is.EqualTo(second.Cuisine));
            Assert.That(first.Dish, Is.EqualTo(second.Dish));
            Assert.That(first.MainIngredient,
                Is.EqualTo(second.MainIngredient));
            Assert.That(first.Produce, Is.EqualTo(second.Produce));
            Assert.That(first.Meat, Is.EqualTo(second.Meat));
            Assert.That(first.Spice, Is.EqualTo(second.Spice));
            Assert.That(first.Beverage, Is.EqualTo(second.Beverage));
            Assert.That(first.Diet, Is.EqualTo(second.Diet));
            Assert.That(first.Allergen, Is.EqualTo(second.Allergen));
            Assert.That(first.Menu, Is.EqualTo(second.Menu));
            AssertNutritionEqual(first.Nutrition, second.Nutrition);
            Assert.That(first.IsSynthetic, Is.EqualTo(second.IsSynthetic));
            Assert.That(first.ClaimsSuitability,
                Is.EqualTo(second.ClaimsSuitability));
        });
    }
}
