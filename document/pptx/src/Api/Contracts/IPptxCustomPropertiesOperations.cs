using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxCustomPropertiesOperations"/>.
/// </summary>
public interface IPptxCustomPropertiesOperations
{
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int Count(PptxCustomProperties target);
    /// <summary>
    /// Serializes the current custom properties and writes it to the requested destination.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void Save(PptxCustomProperties target);
    /// <summary>
    /// Finds the custom property; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The matching custom property, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxCustomProperty? Find(PptxCustomProperties target, string name);
    /// <summary>
    /// Returns the value without changing the custom properties.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetValue(PptxCustomProperties target, string name);
    /// <summary>
    /// Updates the string and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting custom property value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxCustomProperty SetString(PptxCustomProperties target, string name, string value);
    /// <summary>
    /// Updates the int and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting custom property value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxCustomProperty SetInt(PptxCustomProperties target, string name, int value);
    /// <summary>
    /// Updates the bool and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting custom property value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxCustomProperty SetBool(PptxCustomProperties target, string name, bool value);
    /// <summary>
    /// Updates a signed 64-bit integer custom property.
    /// </summary>
    /// <param name="target">The custom properties to update.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The signed integer value.</param>
    /// <returns>The resulting custom property.</returns>
    PptxCustomProperty SetLong(PptxCustomProperties target, string name, long value);
    /// <summary>
    /// Updates a double-precision custom property using invariant text.
    /// </summary>
    /// <param name="target">The custom properties to update.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The numeric value.</param>
    /// <returns>The resulting custom property.</returns>
    PptxCustomProperty SetDouble(PptxCustomProperties target, string name, double value);
    /// <summary>
    /// Updates a file-time custom property.
    /// </summary>
    /// <param name="target">The custom properties to update.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The ISO date-time text.</param>
    /// <returns>The resulting custom property.</returns>
    PptxCustomProperty SetDateTime(PptxCustomProperties target, string name, string value);
    /// <summary>
    /// Updates the value and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="valueType">The value type value to use.</param>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting custom property value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxCustomProperty SetValue(PptxCustomProperties target, string name, string valueType, string value);
    /// <summary>
    /// Removes the remove and updates dependent presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool Remove(PptxCustomProperties target, string name);
    /// <summary>
    /// Renames a custom property when it exists.
    /// </summary>
    /// <param name="target">The custom properties to update.</param>
    /// <param name="oldName">The existing property name.</param>
    /// <param name="newName">The replacement property name.</param>
    /// <returns><c>true</c> when a property was renamed; otherwise, <c>false</c>.</returns>
    bool Rename(PptxCustomProperties target, string oldName, string newName);
    /// <summary>
    /// Removes all custom properties.
    /// </summary>
    /// <param name="target">The custom properties to update.</param>
    /// <returns>The number of removed properties.</returns>
    int Clear(PptxCustomProperties target);
    /// <summary>
    /// Returns the value type without changing the custom properties.
    /// </summary>
    /// <param name="item">The item value to use.</param>
    /// <returns>The resulting text.</returns>
    string GetValueType(PptxCustomProperty item);
}
