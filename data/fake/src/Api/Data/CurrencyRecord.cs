namespace fake;

/// <summary>
/// Represents one current ISO 4217 currency or funds code from the bundled registry snapshot.
/// </summary>
/// <remarks>
/// Registry presence is not legal-tender advice, an exchange rate, or evidence that a
/// currency is accepted for a particular transaction.
/// </remarks>
public class CurrencyRecord
{
    /// <summary>The three-letter alphabetic currency or funds code.</summary>
    public string Code = string.Empty;

    /// <summary>The three-digit numeric code, retained as text to preserve leading zeroes.</summary>
    public string NumericCode = string.Empty;

    /// <summary>The number of minor-unit decimal places, or zero when minor units do not apply.</summary>
    public int MinorUnits;

    /// <summary>Whether <c>MinorUnits</c> contains an applicable value.</summary>
    public bool HasMinorUnits;

    /// <summary>The English currency or funds name recorded by the registry.</summary>
    public string Name = string.Empty;

    /// <summary>The entities mapped to the code in the current registry snapshot.</summary>
    public string[] Entities = new string[0];

    /// <summary>The publication version of the bundled registry snapshot.</summary>
    public string RegistryVersion = string.Empty;

    internal CurrencyRecord()
    {
    }
}
