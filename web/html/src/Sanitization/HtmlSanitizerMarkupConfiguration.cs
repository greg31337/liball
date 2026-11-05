using System;

namespace html;

internal class HtmlSanitizerMarkupConfiguration
{
    internal bool allowCustomElements;
    internal bool allowCustomizedBuiltInElements;
    internal bool allowComments;
    internal bool allowDoctype;
    internal bool allowTemplateElements;
    internal bool neutralizeServerTemplateMarkers;
    internal string[] foreignNamespaces = Array.Empty<string>();

    internal HtmlSanitizerMarkupConfiguration(bool allowCustomElementsValue,
        bool allowCustomizedBuiltInElementsValue, bool allowCommentsValue,
        bool allowDoctypeValue, bool allowTemplateElementsValue,
        bool neutralizeServerTemplateMarkersValue,
        string[] foreignNamespacesValue)
    {
        this.Initialize(allowCustomElementsValue,
            allowCustomizedBuiltInElementsValue, allowCommentsValue,
            allowDoctypeValue, allowTemplateElementsValue,
            neutralizeServerTemplateMarkersValue,
            foreignNamespacesValue);
    }

    internal void Initialize(bool allowCustomElementsValue,
        bool allowCustomizedBuiltInElementsValue, bool allowCommentsValue,
        bool allowDoctypeValue, bool allowTemplateElementsValue,
        bool neutralizeServerTemplateMarkersValue,
        string[] foreignNamespacesValue)
    {
        this.allowCustomElements = allowCustomElementsValue;
        this.allowCustomizedBuiltInElements =
            allowCustomizedBuiltInElementsValue;
        this.allowComments = allowCommentsValue;
        this.allowDoctype = allowDoctypeValue;
        this.allowTemplateElements = allowTemplateElementsValue;
        this.neutralizeServerTemplateMarkers =
            neutralizeServerTemplateMarkersValue;
        this.foreignNamespaces = CopyAndSortNamespaces(
            foreignNamespacesValue);
    }

    internal bool GetAllowCustomElements()
    {
        return this.allowCustomElements;
    }

    internal bool GetAllowCustomizedBuiltInElements()
    {
        return this.allowCustomizedBuiltInElements;
    }

    internal bool GetAllowComments()
    {
        return this.allowComments;
    }

    internal bool GetAllowDoctype()
    {
        return this.allowDoctype;
    }

    internal bool GetAllowTemplateElements()
    {
        return this.allowTemplateElements;
    }

    internal bool GetNeutralizeServerTemplateMarkers()
    {
        return this.neutralizeServerTemplateMarkers;
    }

    internal string[] GetForeignNamespaces()
    {
        string[] result = new string[this.foreignNamespaces.Length];
        Array.Copy(this.foreignNamespaces, result,
            this.foreignNamespaces.Length);
        return result;
    }

    internal bool IsForeignNamespaceAllowed(string namespaceUri)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (namespaceUri.Length == 0
            || namespaceUri
                == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri())
        {
            return false;
        }

        string value = namespaceUri;
        int low = 0;
        int high = this.foreignNamespaces.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = string.CompareOrdinal(
                this.foreignNamespaces[middle], value);
            if (comparison == 0)
            {
                return true;
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return false;
    }

    internal HtmlSanitizerMarkupConfiguration Copy()
    {
        return new HtmlSanitizerMarkupConfiguration(this.allowCustomElements,
            this.allowCustomizedBuiltInElements, this.allowComments,
            this.allowDoctype, this.allowTemplateElements,
            this.neutralizeServerTemplateMarkers,
            this.foreignNamespaces);
    }

    internal static string ValidateForeignNamespace(string value,
        string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0
            || value == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri())
        {
            throw new ArgumentException(
                "A foreign namespace must be non-empty and non-HTML.",
                parameterName);
        }

        return value;
    }

    internal static string[] CopyAndSortNamespaces(string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = ValidateForeignNamespace(values[index],
                "values");
            index = index + 1;
        }

        index = 1;
        while (index < result.Length)
        {
            string current = result[index];
            int position = index;
            while (position > 0 && string.CompareOrdinal(
                result[position - 1], current) > 0)
            {
                result[position] = result[position - 1];
                position = position - 1;
            }

            result[position] = current;
            index = index + 1;
        }

        index = 1;
        while (index < result.Length)
        {
            if (result[index - 1] == result[index])
            {
                throw new ArgumentException(
                    "Foreign namespaces must be unique.", "values");
            }

            index = index + 1;
        }

        return result;
    }
}

internal static class HtmlSanitizerMarkupNames
{
    internal static bool IsDataAttribute(string namespaceUri,
        string localName)
    {
        return namespaceUri.Length == 0 && localName.Length > 5
            && StartsWith(localName, "data-");
    }

    internal static bool IsAriaAttribute(string namespaceUri,
        string localName)
    {
        return namespaceUri.Length == 0 && localName.Length > 5
            && StartsWith(localName, "aria-");
    }

    internal static bool IsCustomizedBuiltInAttribute(string namespaceUri,
        string localName)
    {
        return namespaceUri.Length == 0 && localName == "is";
    }

    internal static bool IsCustomElementName(string value)
    {
        if (value == null || value.Length < 3
            || !Ascii.IsLower(value[0]) || value.IndexOf('-') < 0
            || IsReservedCustomElementName(value))
        {
            return false;
        }

        int index = 1;
        while (index < value.Length)
        {
            int scalar = value[index];
            if (UnicodeTools.IsHighSurrogate(value[index]))
            {
                if (index + 1 >= value.Length
                    || !UnicodeTools.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }

                scalar = UnicodeTools.CombineSurrogates(value[index],
                    value[index + 1]);
                index = index + 1;
            }
            else if (UnicodeTools.IsLowSurrogate(value[index]))
            {
                return false;
            }

            if (!IsCustomElementNameCharacter(scalar))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsCustomElementNameCharacter(int value)
    {
        return value == '-' || value == '.' || value == '_'
            || value >= '0' && value <= '9'
            || value >= 'a' && value <= 'z'
            || value == 183
            || value >= 192 && value <= 214
            || value >= 216 && value <= 246
            || value >= 248 && value <= 893
            || value >= 895 && value <= 8191
            || value >= 8204 && value <= 8205
            || value >= 8255 && value <= 8256
            || value >= 8304 && value <= 8591
            || value >= 11264 && value <= 12271
            || value >= 12289 && value <= 55295
            || value >= 63744 && value <= 64975
            || value >= 65008 && value <= 65533
            || value >= 65536 && value <= 983039;
    }

    internal static bool IsReservedCustomElementName(string value)
    {
        return value == "annotation-xml" || value == "color-profile"
            || value == "font-face" || value == "font-face-src"
            || value == "font-face-uri" || value == "font-face-format"
            || value == "font-face-name" || value == "missing-glyph";
    }

    internal static bool StartsWith(string value, string prefix)
    {
        if (value.Length < prefix.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (value[index] != prefix[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
