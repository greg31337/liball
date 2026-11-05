namespace fake;

internal static class FoodData
{
    internal const string Source = "Project-authored culinary fixtures and generic food vocabulary";
    internal const string License = "project-license";

    internal static readonly string[] Cuisines = new string[]
    {
        "coastal", "garden", "hearth", "market", "orchard", "pantry"
    };

    internal static readonly string[] Dishes = new string[]
    {
        "Citrus Grain Bowl",
        "Herb Vegetable Tart",
        "Roasted Root Plate",
        "Spiced Lentil Wrap",
        "Apple Oat Bake",
        "Tomato Bean Stew"
    };

    internal static readonly string[] MainIngredients = new string[]
    {
        "brown rice", "zucchini", "carrot", "lentils", "apple",
        "white beans"
    };

    internal static readonly string[] Ingredients = new string[]
    {
        "barley", "chickpeas", "garlic", "lemon", "olive oil", "oats",
        "onion", "tomato"
    };

    internal static readonly string[] Produce = new string[]
    {
        "apple", "broccoli", "carrot", "pear", "spinach", "tomato",
        "zucchini"
    };

    internal static readonly string[] Meats = new string[]
    {
        "beef", "chicken", "lamb", "pork", "turkey"
    };

    internal static readonly string[] Spices = new string[]
    {
        "black pepper", "cardamom", "cinnamon", "cumin", "paprika",
        "turmeric"
    };

    internal static readonly string[] Beverages = new string[]
    {
        "apple juice", "black tea", "coffee", "herbal tea", "lemonade",
        "sparkling water"
    };

    internal static readonly string[] Diets = new string[]
    {
        "dairy-free", "gluten-free", "omnivore", "pescatarian", "vegan",
        "vegetarian"
    };

    internal static readonly string[] Allergens = new string[]
    {
        "egg", "fish", "milk", "peanut", "sesame", "shellfish", "soy",
        "tree nut", "wheat"
    };
}
