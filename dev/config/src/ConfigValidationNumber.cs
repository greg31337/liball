namespace config;

internal class ConfigValidationNumber
{
    internal bool HasValue;
    internal decimal Value;

    internal ConfigValidationNumber(bool hasValue, decimal value)
    {
        HasValue = hasValue;
        Value = value;
    }
}
