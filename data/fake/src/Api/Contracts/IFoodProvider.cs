using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="FoodProvider"/>.</summary>
public interface IFoodProvider
{
    /// <summary>Selects a generic culinary style.</summary>
    /// <returns>A project-authored cuisine-style label.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Cuisine();
    /// <summary>Selects a project-fictional dish name.</summary>
    /// <returns>A dish name from the reviewed fixture table.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Dish();
    /// <summary>Selects a generic ingredient.</summary>
    /// <returns>An ingredient name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Ingredient();
    /// <summary>Selects a produce item.</summary>
    /// <returns>A fruit or vegetable name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Produce();
    /// <summary>Selects a generic meat category.</summary>
    /// <returns>A meat name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Meat();
    /// <summary>Selects a spice.</summary>
    /// <returns>A spice name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Spice();
    /// <summary>Selects a non-branded beverage.</summary>
    /// <returns>A generic beverage name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Beverage();
    /// <summary>Selects a diet-shaped test label.</summary>
    /// <remarks>The label does not claim that any generated dish is suitable for the selected diet.</remarks>
    /// <returns>A conventional diet label.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Diet();
    /// <summary>Selects an allergen-shaped test label.</summary>
    /// <remarks>The label does not establish allergen presence, absence, or food safety.</remarks>
    /// <returns>A generic allergen label.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Allergen();
    /// <summary>Generates a menu entry from a fictional dish and generic beverage.</summary>
    /// <returns>A compositional menu entry.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Menu();
    /// <summary>Generates internally consistent nutrition-shaped fixture values.</summary>
    /// <remarks>The values are synthetic, unmeasured, and not dietary or medical advice.</remarks>
    /// <returns>A synthetic nutrition record.</returns>
    NutritionRecord Nutrition();
    /// <summary>Generates a correlated fictional food and menu fixture.</summary>
    /// <remarks>Diet and allergen fields are test labels only and make no suitability or safety claim.</remarks>
    /// <returns>Correlated cuisine, dish, ingredient, menu, and nutrition data.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    FoodRecord Record();
}
