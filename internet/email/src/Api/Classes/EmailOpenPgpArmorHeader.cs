using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents an openPGP armor header.
/// </summary>
public class EmailOpenPgpArmorHeader : IEmailOpenPgpArmorHeaderContract
{
    string name;
    string value;
    /// <summary>
    /// Creates a new openPGP armor header.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpArmorHeader(string name, string value)
    {
        if (name == null || value == null)
            throw new ArgumentNullException("name");
        if (name.Length == 0)
            throw new ArgumentException("Armor header names cannot be empty.", "name");
        this.name = name;
        this.value = value;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string GetName()
    {
        return name;
    }

    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    public string GetValue()
    {
        return value;
    }
}
