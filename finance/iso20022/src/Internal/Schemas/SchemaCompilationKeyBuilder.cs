using System.Text;

namespace iso20022;

internal static class SchemaCompilationKeyBuilder
{
    internal static string Build(SchemaArtifact schema)
    {
        StringBuilder key = new StringBuilder();
        AppendPart(key, schema.NamespaceUri);
        string messageIdentifier = string.Empty;
        if (schema.MessageIdentifier != null)
        {
            string? formatted = MessageIdentifierLexical.FormatMessageIdentifier(
                schema.MessageIdentifier);
            if (formatted != null)
            {
                messageIdentifier = formatted;
            }
        }

        AppendPart(key, messageIdentifier);
        AppendPart(key, schema.Version);
        AppendPart(key, schema.Profile);
        return key.ToString();
    }

    internal static void AppendPart(StringBuilder key, string value)
    {
        key.Append(value.Length);
        key.Append(':');
        key.Append(value);
    }
}
