using System;
using System.Text;

namespace fake;

internal class VCardOutputBuffer
{
    StringBuilder Builder;
    int MaximumOutputSize;

    internal VCardOutputBuffer(int maximumOutputSize)
    {
        Builder = new StringBuilder();
        MaximumOutputSize = maximumOutputSize;
    }

    internal void Append(string value)
    {
        Guard.OutputSize(
            (long)Builder.Length + value.Length,
            MaximumOutputSize,
            "vCard");
        Builder.Append(value);
    }

    internal string Result()
    {
        return Builder.ToString();
    }
}

internal class VCardWriter
{
    GenerationContext Context;
    VCardOutputBuffer Output;

    internal VCardWriter(GenerationContext context)
    {
        Context = context;
        Output = new VCardOutputBuffer(
            context.CapturedOptions.MaximumOutputSize);
    }

    internal string Person(PersonProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException("profile");
        Guard.NotNullOrWhiteSpace(profile.FullName, "FullName");
        Line("BEGIN:VCARD");
        Line("VERSION:4.0");
        TextProperty("FN", profile.FullName, true);
        Line("N:" + Component(profile.LastName) + ";" +
            Component(profile.FirstName) + ";;;");
        if (profile.BirthDate != default(DateOnly))
            Line("BDAY:" + profile.BirthDate.ToString(
                "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture));
        TextProperty("NICKNAME", profile.Username, false);
        TextProperty("EMAIL", profile.Email, false);
        TextProperty("TEL;VALUE=text", profile.Phone, false);
        if (!String.IsNullOrEmpty(profile.Address))
            Line("ADR;TYPE=home:;;" + Component(profile.Address) + ";;;;");
        TextProperty("ORG", profile.Employer, false);
        Language(profile.Locale);
        TextProperty("TZ", profile.TimeZoneId, false);
        Line("END:VCARD");
        return Output.Result();
    }

    internal string Company(CompanyProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException("profile");
        Guard.NotNullOrWhiteSpace(profile.Name, "Name");
        Line("BEGIN:VCARD");
        Line("VERSION:4.0");
        TextProperty("FN", profile.Name, true);
        Line("KIND:org");
        TextProperty("ORG", profile.Name, true);
        TextProperty("EMAIL", profile.Email, false);
        if (profile.Phone != null)
            TextProperty("TEL;VALUE=text", profile.Phone.Number, false);
        if (profile.Address != null)
            Line("ADR;TYPE=work:" + CompanyAddress(profile.Address));
        Language(profile.Locale);
        Line("END:VCARD");
        return Output.Result();
    }

    internal string TextLine(string name, string value)
    {
        Guard.NotNullOrWhiteSpace(name, "name");
        Guard.Count(
            name.Length,
            Context.CapturedOptions.MaximumCount,
            "name");
        Guard.NotNull(value, "value");
        Guard.Count(
            value.Length,
            Context.CapturedOptions.MaximumCount,
            "value");
        UnicodeText.Validate(name, "name");
        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            bool valid = character >= 'A' && character <= 'Z';
            if (!valid)
                valid = character >= 'a' && character <= 'z';
            if (!valid)
                valid = character >= '0' && character <= '9';
            if (!valid)
                valid = character == '-';
            if (!valid)
            {
                throw new ArgumentException(
                    "name must contain only ASCII letters, digits, and hyphens.",
                    "name");
            }
            index = index + 1;
        }
        Line(name + ":" + Escape(value));
        return Output.Result();
    }

    void TextProperty(string name, string value, bool required)
    {
        if (String.IsNullOrEmpty(value))
        {
            if (required)
                throw new ArgumentException(
                    name + " cannot be null or empty.",
                    name);
            return;
        }
        Line(name + ":" + Escape(value));
    }

    void Language(string locale)
    {
        if (String.IsNullOrEmpty(locale))
            return;
        LanguageTag tag = LanguageTagParser.Parse(locale, "Locale");
        Line("LANG:" + tag.Normalized);
    }

    string CompanyAddress(AddressRecord address)
    {
        return ";" + Component(address.Unit) + ";" +
            Component(address.StreetAddress) + ";" +
            Component(address.City) + ";" +
            Component(address.Subdivision) + ";" +
            Component(address.PostalCode) + ";" +
            Component(address.Country);
    }

    static string Component(string value)
    {
        if (value == null)
            return "";
        return Escape(value);
    }

    static string Escape(string value)
    {
        UnicodeText.Validate(value, "value");
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\\')
                result.Append("\\\\");
            else if (character == ',' || character == ';')
            {
                result.Append('\\');
                result.Append(character);
            }
            else if (character == '\r')
            {
                result.Append("\\n");
                if (index + 1 < value.Length && value[index + 1] == '\n')
                    index = index + 1;
            }
            else if (character == '\n')
                result.Append("\\n");
            else
                result.Append(character);
            index = index + 1;
        }
        return result.ToString();
    }

    void Line(string value)
    {
        UnicodeText.Validate(value, "value");
        int start = 0;
        int limit = 75;
        bool first = true;
        while (start < value.Length)
        {
            int end = start;
            int bytes = 0;
            while (end < value.Length)
            {
                int units = 1;
                if (Char.IsHighSurrogate(value[end]))
                    units = 2;
                int scalarBytes = Encoding.UTF8.GetByteCount(
                    value.AsSpan(end, units));
                if (bytes + scalarBytes > limit)
                    break;
                bytes = bytes + scalarBytes;
                end = end + units;
            }
            if (!first)
                Output.Append(" ");
            Output.Append(value.Substring(start, end - start));
            Output.Append("\r\n");
            first = false;
            start = end;
            limit = 74;
        }
    }
}
