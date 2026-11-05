using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides food and menu generation operations.
/// </summary>
public class FoodProvider : IFoodProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal FoodProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("food");
    }

    /// <summary>Selects a generic culinary style.</summary>
    /// <returns>A project-authored cuisine-style label.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Cuisine()
    {
        return FoodGenerator.Cuisine(Context, RandomValue);
    }

    /// <summary>Selects a project-fictional dish name.</summary>
    /// <returns>A dish name from the reviewed fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Dish()
    {
        return FoodGenerator.Dish(Context, RandomValue);
    }

    /// <summary>Selects a generic ingredient.</summary>
    /// <returns>An ingredient name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Ingredient()
    {
        return FoodGenerator.Ingredient(Context, RandomValue);
    }

    /// <summary>Selects a produce item.</summary>
    /// <returns>A fruit or vegetable name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Produce()
    {
        return FoodGenerator.Produce(Context, RandomValue);
    }

    /// <summary>Selects a generic meat category.</summary>
    /// <returns>A meat name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Meat()
    {
        return FoodGenerator.Meat(Context, RandomValue);
    }

    /// <summary>Selects a spice.</summary>
    /// <returns>A spice name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Spice()
    {
        return FoodGenerator.Spice(Context, RandomValue);
    }

    /// <summary>Selects a non-branded beverage.</summary>
    /// <returns>A generic beverage name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Beverage()
    {
        return FoodGenerator.Beverage(Context, RandomValue);
    }

    /// <summary>Selects a diet-shaped test label.</summary>
    /// <remarks>The label does not claim that any generated dish is suitable for the selected diet.</remarks>
    /// <returns>A conventional diet label.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Diet()
    {
        return FoodGenerator.Diet(Context, RandomValue);
    }

    /// <summary>Selects an allergen-shaped test label.</summary>
    /// <remarks>The label does not establish allergen presence, absence, or food safety.</remarks>
    /// <returns>A generic allergen label.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Allergen()
    {
        return FoodGenerator.Allergen(Context, RandomValue);
    }

    /// <summary>Generates a menu entry from a fictional dish and generic beverage.</summary>
    /// <returns>A compositional menu entry.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Menu()
    {
        return FoodGenerator.Menu(Context, RandomValue);
    }

    /// <summary>Generates internally consistent nutrition-shaped fixture values.</summary>
    /// <remarks>The values are synthetic, unmeasured, and not dietary or medical advice.</remarks>
    /// <returns>A synthetic nutrition record.</returns>
    public NutritionRecord Nutrition()
    {
        return FoodGenerator.Nutrition(RandomValue);
    }

    /// <summary>Generates a correlated fictional food and menu fixture.</summary>
    /// <remarks>Diet and allergen fields are test labels only and make no suitability or safety claim.</remarks>
    /// <returns>Correlated cuisine, dish, ingredient, menu, and nutrition data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public FoodRecord Record()
    {
        return FoodGenerator.Record(Context, RandomValue);
    }
}
