namespace icc;

internal static class DeterministicTagSerialization
{
    internal static string NormalizeUnicode(string value, IccWriteMode mode, string description)
    {
        if (value == null)
        {
            throw new IccProfileWriteException($"{description} cannot be null.");
        }

        if (!Icc2TextTagCodec.IsValidUnicodeString(value))
        {
            throw new IccProfileWriteException($"{description} contains an invalid UTF-16 surrogate sequence.");
        }

        if (mode == IccWriteMode.Canonical)
        {
            return value.Normalize(System.Text.NormalizationForm.FormC);
        }

        return value;
    }

    internal static double NormalizeFloatingPoint(double value, IccWriteMode mode)
    {
        if (mode == IccWriteMode.Canonical && value == 0.0)
        {
            return 0.0;
        }

        return value;
    }
}
