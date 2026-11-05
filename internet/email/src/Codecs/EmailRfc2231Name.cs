namespace email;

internal class EmailRfc2231Name
{
    EmailRfc2231Parameter parameter;
    string? baseName;
    EmailRfc2231NameKind kind;
    int section;
    bool encoded;
    bool leadingZero;

    internal EmailRfc2231Name(EmailRfc2231Parameter parameter, string? baseName, EmailRfc2231NameKind kind, int section, bool encoded, bool leadingZero)
    {
        this.parameter = parameter;
        this.baseName = baseName;
        this.kind = kind;
        this.section = section;
        this.encoded = encoded;
        this.leadingZero = leadingZero;
    }

    internal EmailRfc2231Parameter GetParameter()
    {
        return parameter;
    }

    internal string? GetBaseName()
    {
        return baseName;
    }

    internal EmailRfc2231NameKind GetKind()
    {
        return kind;
    }

    internal int GetSection()
    {
        return section;
    }

    internal bool IsEncoded()
    {
        return encoded;
    }

    internal bool HasLeadingZero()
    {
        return leadingZero;
    }
}
