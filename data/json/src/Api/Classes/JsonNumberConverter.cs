namespace json;

/// <summary>Converts exact JSON numbers to supported CLR numeric forms.</summary>
public class JsonNumberConverter : IJsonNumberConverter
{
    /// <summary>Initializes a reusable JSON number converter.</summary>
    public JsonNumberConverter()
    {
    }

    /// <summary>Converts an exact JSON number to a signed 64-bit value.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The converted value.</returns>
    public long ToInt64(JsonNumber number)
    {
        return JsonNumberConversion.ConvertToInt64(number);
    }

    /// <summary>Attempts signed 64-bit conversion.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The conversion outcome.</returns>
    public JsonInt64Result TryInt64(JsonNumber number)
    {
        return JsonNumberConversion.TryConvertToInt64(number);
    }

    /// <summary>Converts an exact JSON number to an unsigned 64-bit value.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The converted value.</returns>
    public ulong ToUInt64(JsonNumber number)
    {
        return JsonNumberConversion.ConvertToUInt64(number);
    }

    /// <summary>Attempts unsigned 64-bit conversion.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The conversion outcome.</returns>
    public JsonUInt64Result TryUInt64(JsonNumber number)
    {
        return JsonNumberConversion.TryConvertToUInt64(number);
    }

    /// <summary>Converts an exact JSON number to a decimal value.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The converted value.</returns>
    public decimal ToDecimal(JsonNumber number)
    {
        return JsonNumberConversion.ConvertToDecimal(number);
    }

    /// <summary>Attempts decimal conversion.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The conversion outcome.</returns>
    public JsonDecimalResult TryDecimal(JsonNumber number)
    {
        return JsonNumberConversion.TryConvertToDecimal(number);
    }

    /// <summary>Converts an exact JSON number to a finite double-precision value.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The converted value.</returns>
    public double ToDouble(JsonNumber number)
    {
        return JsonNumberConversion.ConvertToDouble(number);
    }

    /// <summary>Attempts finite double-precision conversion.</summary>
    /// <param name="number">The exact JSON number.</param>
    /// <returns>The conversion outcome.</returns>
    public JsonDoubleResult TryDouble(JsonNumber number)
    {
        return JsonNumberConversion.TryConvertToDouble(number);
    }
}
