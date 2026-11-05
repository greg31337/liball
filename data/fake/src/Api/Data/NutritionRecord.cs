namespace fake;

/// <summary>Represents synthetic nutrition-shaped fixture values.</summary>
/// <remarks>The values are generated for software tests, are not measurements, and are not dietary or medical advice.</remarks>
public class NutritionRecord
{
    /// <summary>The synthetic serving mass in grams.</summary>
    public int ServingGrams;

    /// <summary>The synthetic energy total calculated from the macronutrient fields.</summary>
    public int Calories;

    /// <summary>The synthetic protein amount in grams.</summary>
    public int ProteinGrams;

    /// <summary>The synthetic carbohydrate amount in grams.</summary>
    public int CarbohydrateGrams;

    /// <summary>The synthetic fat amount in grams.</summary>
    public int FatGrams;

    /// <summary>The synthetic sodium amount in milligrams.</summary>
    public int SodiumMilligrams;

    /// <summary>Whether the values are synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the values claim laboratory measurement, always <c>false</c>.</summary>
    public bool IsMeasured;

    internal NutritionRecord()
    {
    }
}
