namespace whois;

/// <summary>
/// Extracts ordered WHOIS, RWhois, and web referrals from a generic parsed response.
/// </summary>
public class WhoisReferralParser : IWhoisReferralParser
{
    /// <summary>
    /// Initializes the built-in referral parser.
    /// </summary>
    public WhoisReferralParser()
    {
    }

    /// <summary>
    /// Parses every recognized, unsupported, or malformed referral in source order.
    /// </summary>
    /// <param name="response">The generic parsed response.</param>
    /// <returns>All referral metadata in response order.</returns>
    /// <exception cref="System.ArgumentNullException">The response is <c>null</c>.</exception>
    public WhoisReferral[] ParseReferrals(WhoisParsedResponse response)
    {
        InputGuard.RequireReference(response, "response");
        System.Collections.Generic.List<WhoisReferral> referrals = new System.Collections.Generic.List<WhoisReferral>();
        int lineIndex = 0;
        while (lineIndex < response.Lines.Length)
        {
            WhoisLine line = response.Lines[lineIndex];
            if (line.Kind == WhoisLineKind.Field && line.FieldIndex >= 0 && line.FieldIndex < response.Fields.Length)
            {
                WhoisField field = response.Fields[line.FieldIndex];
                if (IsRecognizedField(field.OriginalName))
                {
                    referrals.Add(ReferralValueParser.ParseValue(field.OriginalName, field.SourceLine, field.RawValue));
                }
            }
            else if (line.Kind == WhoisLineKind.Comment)
            {
                string? directiveValue = ExtractDirectiveValue(line.RawContent);
                if (directiveValue != null)
                {
                    referrals.Add(ReferralValueParser.ParseValue(WhoisFieldNames.ReferralDirective, line.LineNumber, directiveValue));
                }
            }
            lineIndex = lineIndex + 1;
        }
        return referrals.ToArray();
    }

    internal static bool IsRecognizedField(string fieldName)
    {
        string normalizedName = ReferralValueParser.NormalizeSourceName(fieldName);
        return normalizedName == "refer" ||
            normalizedName == "whois" ||
            normalizedName == "whois server" ||
            normalizedName == "registrar whois server" ||
            normalizedName == "referralserver" ||
            normalizedName == "referral server";
    }

    internal static string? ExtractDirectiveValue(string rawContent)
    {
        if (rawContent.Length == 0 || rawContent[0] != '%')
        {
            return null;
        }

        int index = 1;
        while (index < rawContent.Length && (rawContent[index] == ' ' || rawContent[index] == '\t'))
        {
            index = index + 1;
        }
        const string directiveName = "referral";
        if (index + directiveName.Length > rawContent.Length)
        {
            return null;
        }
        string candidateName = rawContent.Substring(index, directiveName.Length);
        if (!string.Equals(candidateName, directiveName, System.StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        index = index + directiveName.Length;
        if (index < rawContent.Length && rawContent[index] == ':')
        {
            index = index + 1;
        }
        else if (index < rawContent.Length && rawContent[index] != ' ' && rawContent[index] != '\t')
        {
            return null;
        }
        while (index < rawContent.Length && (rawContent[index] == ' ' || rawContent[index] == '\t'))
        {
            index = index + 1;
        }
        return rawContent.Substring(index);
    }
}
