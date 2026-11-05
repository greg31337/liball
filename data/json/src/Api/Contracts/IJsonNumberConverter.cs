namespace json;

/// <summary>Converts exact JSON numbers without throwing for expected conversion failures.</summary>
public interface IJsonNumberConverter
{
    /// <summary>Converts an exact JSON number to a signed 64-bit value.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The converted value.</returns>
    long ToInt64(JsonNumber number);

    /// <summary>Attempts signed 64-bit conversion.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The conversion outcome.</returns>
    JsonInt64Result TryInt64(JsonNumber number);

    /// <summary>Converts an exact JSON number to an unsigned 64-bit value.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The converted value.</returns>
    ulong ToUInt64(JsonNumber number);

    /// <summary>Attempts unsigned 64-bit conversion.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The conversion outcome.</returns>
    JsonUInt64Result TryUInt64(JsonNumber number);

    /// <summary>Converts an exact JSON number to a decimal value.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The converted value.</returns>
    decimal ToDecimal(JsonNumber number);

    /// <summary>Attempts decimal conversion.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The conversion outcome.</returns>
    JsonDecimalResult TryDecimal(JsonNumber number);

    /// <summary>Converts an exact JSON number to a finite double-precision value.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The converted value.</returns>
    double ToDouble(JsonNumber number);

    /// <summary>Attempts finite double-precision conversion.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The conversion outcome.</returns>
    JsonDoubleResult TryDouble(JsonNumber number);
}
