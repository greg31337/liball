using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxCustomProperties values.
/// </summary>
public class PptxCustomPropertiesOperations : IPptxCustomPropertiesOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxCustomPropertiesOperations()
    {
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int Count(PptxCustomProperties target)
    {
        return target.Count();
    }

    /// <summary>
    /// Serializes the current custom properties and writes it to the requested destination.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void Save(PptxCustomProperties target)
    {
        target.Save();
    }

    /// <summary>
    /// Finds the custom property; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The matching custom property, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxCustomProperty? Find(PptxCustomProperties target, string name)
    {
        return target.Find(name);
    }

    /// <summary>
    /// Returns the value without changing the custom properties.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetValue(PptxCustomProperties target, string name)
    {
        return target.GetValue(name);
    }

    /// <summary>
    /// Updates the string and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting custom property value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxCustomProperty SetString(PptxCustomProperties target, string name, string value)
    {
        return target.SetString(name, value);
    }

    /// <summary>
    /// Updates the int and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting custom property value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxCustomProperty SetInt(PptxCustomProperties target, string name, int value)
    {
        return target.SetInt(name, value);
    }

    /// <summary>
    /// Updates the bool and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting custom property value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxCustomProperty SetBool(PptxCustomProperties target, string name, bool value)
    {
        return target.SetBool(name, value);
    }

    /// <summary>
    /// Updates a signed 64-bit integer custom property.
    /// </summary>
    /// <param name="target">The custom properties to update.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The signed integer value.</param>
    /// <returns>The resulting custom property.</returns>
    public PptxCustomProperty SetLong(PptxCustomProperties target, string name, long value)
    {
        return target.SetLong(name, value);
    }

    /// <summary>
    /// Updates a double-precision custom property using invariant text.
    /// </summary>
    /// <param name="target">The custom properties to update.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The numeric value.</param>
    /// <returns>The resulting custom property.</returns>
    public PptxCustomProperty SetDouble(PptxCustomProperties target, string name, double value)
    {
        return target.SetDouble(name, value);
    }

    /// <summary>
    /// Updates a file-time custom property.
    /// </summary>
    /// <param name="target">The custom properties to update.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The ISO date-time text.</param>
    /// <returns>The resulting custom property.</returns>
    public PptxCustomProperty SetDateTime(PptxCustomProperties target, string name, string value)
    {
        return target.SetDateTime(name, value);
    }

    /// <summary>
    /// Updates the value and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="valueType">The value type value to use.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting custom property value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxCustomProperty SetValue(PptxCustomProperties target, string name, string valueType, string value)
    {
        return target.SetValue(name, valueType, value);
    }

    /// <summary>
    /// Removes the remove and updates dependent presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Remove(PptxCustomProperties target, string name)
    {
        return target.Remove(name);
    }

    /// <summary>
    /// Renames a custom property when it exists.
    /// </summary>
    /// <param name="target">The custom properties to update.</param>
    /// <param name="oldName">The existing property name.</param>
    /// <param name="newName">The replacement property name.</param>
    /// <returns><c>true</c> when a property was renamed; otherwise, <c>false</c>.</returns>
    public bool Rename(PptxCustomProperties target, string oldName, string newName)
    {
        return target.Rename(oldName, newName);
    }

    /// <summary>
    /// Removes all custom properties.
    /// </summary>
    /// <param name="target">The custom properties to update.</param>
    /// <returns>The number of removed properties.</returns>
    public int Clear(PptxCustomProperties target)
    {
        return target.Clear();
    }

    /// <summary>
    /// Returns the value type without changing the custom properties.
    /// </summary>
    /// <param name="item">The item value to use.</param>
    /// <returns>The resulting text.</returns>
    public string GetValueType(PptxCustomProperty item)
    {
        return PptxCustomProperties.GetValueType(item);
    }
}
