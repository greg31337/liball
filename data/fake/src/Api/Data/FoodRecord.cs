namespace fake;

/// <summary>Represents a correlated project-fictional food and menu fixture.</summary>
/// <remarks>Diet and allergen values are test labels only. They do not establish ingredient suitability, allergen presence or absence, or food safety.</remarks>
public class FoodRecord
{
    /// <summary>The generic culinary style correlated with <c>Dish</c>.</summary>
    public string Cuisine = string.Empty;

    /// <summary>The project-fictional dish correlated with <c>Cuisine</c>.</summary>
    public string Dish = string.Empty;

    /// <summary>The principal ingredient correlated with <c>Dish</c>.</summary>
    public string MainIngredient = string.Empty;

    /// <summary>A produce item for the fixture.</summary>
    public string Produce = string.Empty;

    /// <summary>A meat category for the fixture.</summary>
    public string Meat = string.Empty;

    /// <summary>A spice for the fixture.</summary>
    public string Spice = string.Empty;

    /// <summary>The beverage used in <c>Menu</c>.</summary>
    public string Beverage = string.Empty;

    /// <summary>A simulated diet label without a suitability claim.</summary>
    public string Diet = string.Empty;

    /// <summary>A simulated allergen label without a composition or safety claim.</summary>
    public string Allergen = string.Empty;

    /// <summary>The menu entry composed from <c>Dish</c> and <c>Beverage</c>.</summary>
    public string Menu = string.Empty;

    /// <summary>The synthetic nutrition-shaped values for the fixture.</summary>
    public NutritionRecord Nutrition = new NutritionRecord();

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the record claims food safety or dietary suitability, always <c>false</c>.</summary>
    public bool ClaimsSuitability;

    internal FoodRecord()
    {
    }
}
