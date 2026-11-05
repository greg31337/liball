using System;
using System.Globalization;

namespace gltf;

internal enum AttributeSemanticKind
{
    Invalid = 0,
    Position = 1,
    Normal = 2,
    Tangent = 3,
    TextureCoordinate = 4,
    Color = 5,
    Joints = 6,
    Weights = 7,
    Custom = 8,
    Extension = 9
}

internal class AttributeSemantic
{
    internal bool Valid;
    internal AttributeSemanticKind Kind = AttributeSemanticKind.Invalid;
    internal string Text = "";
    internal string Name = "";
    internal bool HasSetIndex;
    internal int SetIndex;
    internal string ExtensionName = "";
}

internal class AttributeSemanticParser
{
    internal AttributeSemantic ParseAttributeSemantic(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        AttributeSemantic result = new AttributeSemantic();
        result.Text = text;
        if (text == GltfSemantics.Position)
        {
            SetCoreSemantic(result, AttributeSemanticKind.Position, GltfSemantics.Position);
        }
        else if (text == GltfSemantics.Normal)
        {
            SetCoreSemantic(result, AttributeSemanticKind.Normal, GltfSemantics.Normal);
        }
        else if (text == GltfSemantics.Tangent)
        {
            SetCoreSemantic(result, AttributeSemanticKind.Tangent, GltfSemantics.Tangent);
        }
        else if (ParseNumberedSemantic(
            result,
            text,
            GltfSemantics.TextureCoordinatePrefix,
            AttributeSemanticKind.TextureCoordinate))
        {
        }
        else if (ParseNumberedSemantic(
            result,
            text,
            GltfSemantics.ColorPrefix,
            AttributeSemanticKind.Color))
        {
        }
        else if (ParseNumberedSemantic(
            result,
            text,
            GltfSemantics.JointsPrefix,
            AttributeSemanticKind.Joints))
        {
        }
        else if (ParseNumberedSemantic(
            result,
            text,
            GltfSemantics.WeightsPrefix,
            AttributeSemanticKind.Weights))
        {
        }
        else if (text.Length > 1 && text[0] == '_')
        {
            result.Valid = true;
            result.Kind = AttributeSemanticKind.Custom;
            result.Name = text.Substring(1);
        }
        else if (IsGpuInstancingSemantic(text))
        {
            result.Valid = true;
            result.Kind = AttributeSemanticKind.Extension;
            result.Name = text;
            result.ExtensionName = GltfExtensionNames.ExtMeshGpuInstancing;
        }
        else
        {
            ParseNamespacedExtensionSemantic(result, text);
        }

        return result;
    }

    internal string FormatParsedSemantic(AttributeSemantic semantic)
    {
        if (semantic == null)
        {
            throw new ArgumentNullException("semantic");
        }

        if (!semantic.Valid)
        {
            throw new ArgumentException(
                "An invalid attribute semantic cannot be formatted.",
                "semantic");
        }

        switch (semantic.Kind)
        {
            case AttributeSemanticKind.Position:
            case AttributeSemanticKind.Normal:
            case AttributeSemanticKind.Tangent:
                return FormatUnnumberedCoreSemantic(semantic.Kind);
            case AttributeSemanticKind.TextureCoordinate:
            case AttributeSemanticKind.Color:
            case AttributeSemanticKind.Joints:
            case AttributeSemanticKind.Weights:
                if (!semantic.HasSetIndex)
                {
                    throw new ArgumentException(
                        "A numbered core semantic requires a set index.",
                        "semantic");
                }

                return FormatNumberedCoreSemantic(
                    semantic.Kind,
                    semantic.SetIndex);
            case AttributeSemanticKind.Custom:
                return FormatCustomSemantic(semantic.Name);
            case AttributeSemanticKind.Extension:
                return FormatExtensionSemantic(
                    semantic.ExtensionName,
                    semantic.Name);
            default:
                throw new ArgumentException(
                    "The attribute semantic kind cannot be formatted.",
                    "semantic");
        }
    }

    internal string FormatUnnumberedCoreSemantic(AttributeSemanticKind kind)
    {
        switch (kind)
        {
            case AttributeSemanticKind.Position:
                return GltfSemantics.Position;
            case AttributeSemanticKind.Normal:
                return GltfSemantics.Normal;
            case AttributeSemanticKind.Tangent:
                return GltfSemantics.Tangent;
            default:
                throw new ArgumentOutOfRangeException("kind");
        }
    }

    internal string FormatNumberedCoreSemantic(
        AttributeSemanticKind kind,
        int setIndex)
    {
        if (setIndex < 0)
        {
            throw new ArgumentOutOfRangeException("setIndex");
        }

        string prefix;
        switch (kind)
        {
            case AttributeSemanticKind.TextureCoordinate:
                prefix = GltfSemantics.TextureCoordinatePrefix;
                break;
            case AttributeSemanticKind.Color:
                prefix = GltfSemantics.ColorPrefix;
                break;
            case AttributeSemanticKind.Joints:
                prefix = GltfSemantics.JointsPrefix;
                break;
            case AttributeSemanticKind.Weights:
                prefix = GltfSemantics.WeightsPrefix;
                break;
            default:
                throw new ArgumentOutOfRangeException("kind");
        }

        return string.Concat(
            prefix,
            setIndex.ToString(CultureInfo.InvariantCulture));
    }

    internal string FormatCustomSemantic(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0 || name[0] == '_')
        {
            throw new ArgumentException(
                "A custom semantic name must be a non-empty value without its underscore prefix.",
                "name");
        }

        return string.Concat(GltfSemantics.CustomAttributePrefix, name);
    }

    internal string FormatExtensionSemantic(
        string extensionName,
        string semanticName)
    {
        if (extensionName == null)
        {
            throw new ArgumentNullException("extensionName");
        }

        if (semanticName == null)
        {
            throw new ArgumentNullException("semanticName");
        }

        if (!IsExtensionName(extensionName)
            || !IsExtensionSemanticName(semanticName))
        {
            throw new ArgumentException(
                "Extension attribute semantic names must be non-empty tokens and use a valid extension prefix.",
                "extensionName");
        }

        if (extensionName == GltfExtensionNames.ExtMeshGpuInstancing
            && IsGpuInstancingSemantic(semanticName))
        {
            return semanticName;
        }

        return string.Concat(extensionName, ":", semanticName);
    }

    internal void SetCoreSemantic(
        AttributeSemantic result,
        AttributeSemanticKind kind,
        string name)
    {
        result.Valid = true;
        result.Kind = kind;
        result.Name = name;
    }

    internal bool ParseNumberedSemantic(
        AttributeSemantic result,
        string text,
        string prefix,
        AttributeSemanticKind kind)
    {
        if (!text.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        string digits = text.Substring(prefix.Length);
        if (!TryParseCanonicalSetIndex(digits, result))
        {
            return true;
        }

        result.Valid = true;
        result.Kind = kind;
        result.Name = prefix.Substring(0, prefix.Length - 1);
        result.HasSetIndex = true;
        return true;
    }

    internal bool TryParseCanonicalSetIndex(
        string digits,
        AttributeSemantic result)
    {
        if (digits.Length == 0
            || (digits.Length > 1 && digits[0] == '0'))
        {
            return false;
        }

        int value = 0;
        int index = 0;
        while (index < digits.Length)
        {
            char character = digits[index];
            if (character < '0' || character > '9')
            {
                return false;
            }

            int digit = character - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                return false;
            }

            value = value * 10 + digit;
            index++;
        }

        result.SetIndex = value;
        return true;
    }

    internal void ParseNamespacedExtensionSemantic(
        AttributeSemantic result,
        string text)
    {
        int separator = text.IndexOf(':');
        if (separator < 1
            || separator != text.LastIndexOf(':')
            || separator == text.Length - 1)
        {
            return;
        }

        string extensionName = text.Substring(0, separator);
        string semanticName = text.Substring(separator + 1);
        if (!IsExtensionName(extensionName)
            || !IsExtensionSemanticName(semanticName))
        {
            return;
        }

        result.Valid = true;
        result.Kind = AttributeSemanticKind.Extension;
        result.ExtensionName = extensionName;
        result.Name = semanticName;
    }

    internal bool IsGpuInstancingSemantic(string text)
    {
        return text == "TRANSLATION"
            || text == "ROTATION"
            || text == "SCALE";
    }

    internal bool IsExtensionName(string text)
    {
        if (text.Length < 3
            || text[0] == '_'
            || text[text.Length - 1] == '_'
            || text.IndexOf('_') < 1)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            bool valid = character >= 'A' && character <= 'Z';
            if (character >= 'a' && character <= 'z')
            {
                valid = true;
            }
            else if (character >= '0' && character <= '9')
            {
                valid = true;
            }
            else if (character == '_')
            {
                valid = true;
            }

            if (!valid)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsExtensionSemanticName(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character <= ' ' || character == ':')
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
