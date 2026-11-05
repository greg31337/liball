using System;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with address IDN.
/// </summary>
public class EmailAddressIdn : IEmailAddressIdnContract
{
    internal static string ToAsciiImplementation(string domain)
    {
        ValidateDomain(domain, "domain");
        string normalized = domain.Normalize(NormalizationForm.FormC);
        IdnMapping mapping = CreateMapping();
        return mapping.GetAscii(normalized).ToLowerInvariant();
    }

    /// <summary>
    /// Converts to the ascii.
    /// </summary>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string ToAscii(string domain)
    {
        return EmailAddressIdn.ToAsciiImplementation(domain);
    }

    internal static string ToUnicodeImplementation(string domain)
    {
        ValidateDomain(domain, "domain");
        string normalized = domain.Normalize(NormalizationForm.FormC);
        if (ContainsNonAscii(normalized))
        {
            return normalized.ToLowerInvariant();
        }

        IdnMapping mapping = CreateMapping();
        return mapping.GetUnicode(normalized).Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>
    /// Converts to the unicode.
    /// </summary>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string ToUnicode(string domain)
    {
        return EmailAddressIdn.ToUnicodeImplementation(domain);
    }

    internal static bool IsInternationalizedImplementation(string domain)
    {
        if (domain == null)
        {
            throw new ArgumentNullException("domain");
        }

        return ContainsNonAscii(domain);
    }

    /// <summary>
    /// Determines whether the current value or state is internationalized.
    /// </summary>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <returns><c>true</c> when the value or state is internationalized; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool IsInternationalized(string domain)
    {
        return EmailAddressIdn.IsInternationalizedImplementation(domain);
    }

    static IdnMapping CreateMapping()
    {
        IdnMapping mapping = new IdnMapping();
        mapping.AllowUnassigned = false;
        mapping.UseStd3AsciiRules = true;
        return mapping;
    }

    static void ValidateDomain(string domain, string parameterName)
    {
        if (domain == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (domain.Length == 0)
        {
            throw new ArgumentException("A domain cannot be empty.", parameterName);
        }

        if (!EmailAddressFormattingText.IsValidUnicode(domain))
        {
            throw new ArgumentException("A domain must contain valid Unicode scalar values.", parameterName);
        }

        int index = 0;
        int labelLength = 0;
        while (index < domain.Length)
        {
            char value = domain[index];
            if (value == '.')
            {
                if (labelLength == 0 && index + 1 < domain.Length)
                {
                    throw new ArgumentException("A domain cannot contain an empty label.", parameterName);
                }

                labelLength = 0;
            }
            else
            {
                if (value <= 32 || value == 127 || value == '@' || value == '[' || value == ']' || value == '\\' || value == '/' || value == ':' || value == ';' || value == ',' || value == '<' || value == '>' || value == '"' || value == '(' || value == ')')
                {
                    throw new ArgumentException("A domain contains a character that is unsafe or invalid in an address.", parameterName);
                }

                labelLength++;
            }

            index++;
        }
    }

    static bool ContainsNonAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
