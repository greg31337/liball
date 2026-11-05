using System.Globalization;

namespace xml;

internal static class SerializationNormalization
{
    internal static void ValidateFullyNormalizedValue(string value, string? form)
    {
        if (value.Length == 0 || !string.Equals(form, "fully-normalized", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        int first = value[0];
        if (first >= 0xD800 && first <= 0xDFFF)
        {
            return;
        }

        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(value, 0);
        if (category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark || category == UnicodeCategory.EnclosingMark)
        {
            throw CreateFullyNormalizedSerializationError("A value to be fully normalized begins with a combining character.");
        }
    }

    internal static XmlSerializationException CreateFullyNormalizedSerializationError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.SERE0012;
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }
}
