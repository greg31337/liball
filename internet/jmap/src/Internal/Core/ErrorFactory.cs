using System;
using System.Collections.Generic;
using System.Text.Json;

namespace jmap;

internal class ErrorFactory
{
    internal IJmapStringLocalizer? Localizer;
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal ProtocolJsonCodec Protocol;
    internal ValueValidator Values = new ValueValidator();

    internal ErrorFactory(IJmapStringLocalizer? localizer)
    {
        Localizer = localizer;
        Protocol = new ProtocolJsonCodec(Reader);
    }

    internal JmapMethodError CreateMethodError(string? type, string? description, bool stateChanged)
    {
        JmapMethodError source = new JmapMethodError();
        source.Type = type;
        source.Description = description;
        return NormalizeMethodError(source, stateChanged);
    }

    internal JmapMethodError NormalizeMethodError(JmapMethodError? source, bool stateChanged)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        ValidateErrorText(source.Type, source.Description, "methodError");
        string type = source.Type == null ? "" : source.Type;
        JmapMethodErrorKind kind = Protocol.ClassifyMethodErrorType(type);
        if (stateChanged && kind != JmapMethodErrorKind.ServerPartialFail)
        {
            throw ProtocolFailure("A method error other than serverPartialFail cannot accompany a state change.", "type", default(JsonElement));
        }

        JmapMethodError result = new JmapMethodError();
        result.Type = type;
        result.Kind = kind;
        result.Description = source.Description;
        CopyUnknownProperties(source.UnknownProperties, result.UnknownProperties, "methodError");
        return result;
    }

    internal JmapMethodErrorKind EffectiveMethodErrorKind(JmapMethodError? error)
    {
        if (error == null || error.Type == null || error.Type.Length == 0 || !Values.IsValidUnicode(error.Type))
        {
            throw new ArgumentException("A valid method error is required.", "error");
        }

        JmapMethodErrorKind kind = Protocol.ClassifyMethodErrorType(error.Type);
        return kind == JmapMethodErrorKind.Unknown ? JmapMethodErrorKind.ServerFail : kind;
    }

    internal JmapSetError CreateSetError(string? type, string? description)
    {
        JmapSetError source = new JmapSetError();
        source.Type = type;
        source.Description = description;
        return NormalizeSetError(source);
    }

    internal JmapSetError CreateUserVisibleSetError(
        string? type,
        string? localizationKey,
        string? fallbackDescription,
        string? acceptLanguage)
    {
        if (type != JmapErrorCodes.ForbiddenToSend)
        {
            throw new ArgumentException("Only forbiddenToSend has a user-visible SetError description.", "type");
        }

        if (localizationKey == null || localizationKey.Length == 0 || !Values.IsValidUnicode(localizationKey))
        {
            throw new ArgumentException("A valid localization key is required.", "localizationKey");
        }

        string? description = fallbackDescription;
        if (Localizer != null)
        {
            description = Localizer.LocalizeJmapString(localizationKey, acceptLanguage);
        }

        return CreateSetError(type, description);
    }

    internal JmapSetError NormalizeSetError(JmapSetError? source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        ValidateErrorText(source.Type, source.Description, "setError");
        ValidateSetErrorFields(source);
        string type = source.Type == null ? "" : source.Type;
        JmapSetError result = new JmapSetError();
        result.Type = type;
        result.Kind = Protocol.ClassifySetErrorType(type);
        result.Description = source.Description;
        result.ExistingId = source.ExistingId;
        result.MaxSize = source.MaxSize;
        result.HasMaxSize = source.HasMaxSize;
        result.MaxRecipients = source.MaxRecipients;
        result.HasMaxRecipients = source.HasMaxRecipients;
        result.Properties = CopyStringList(source.Properties);
        result.InvalidRecipients = CopyStringList(source.InvalidRecipients);
        result.NotFound = CopyStringList(source.NotFound);
        CopyUnknownProperties(source.UnknownProperties, result.UnknownProperties, "setError");
        return result;
    }

    internal void ValidateErrorText(string? type, string? description, string path)
    {
        if (type == null || type.Length == 0 || !Values.IsValidUnicode(type))
        {
            throw new ArgumentException("An error type must be a non-empty valid Unicode string.", path);
        }

        if (description != null && !Values.IsValidUnicode(description))
        {
            throw new ArgumentException("An error description must be valid Unicode.", path);
        }
    }

    internal void ValidateSetErrorFields(JmapSetError source)
    {
        if (source.ExistingId != null && !Values.IsId(source.ExistingId))
        {
            throw new ArgumentException("existingId must be an Id.", "source");
        }

        if (source.HasMaxSize && !Values.IsUnsignedInt(source.MaxSize))
        {
            throw new ArgumentException("maxSize must be an UnsignedInt.", "source");
        }

        if (source.HasMaxRecipients && !Values.IsUnsignedInt(source.MaxRecipients))
        {
            throw new ArgumentException("maxRecipients must be an UnsignedInt.", "source");
        }

        ValidateStringList(source.Properties, "properties", false);
        ValidateStringList(source.InvalidRecipients, "invalidRecipients", false);
        ValidateStringList(source.NotFound, "notFound", true);
    }

    internal void ValidateStringList(List<string>? values, string path, bool requireIds)
    {
        if (values == null)
        {
            return;
        }

        foreach (string value in values)
        {
            if ((requireIds && !Values.IsId(value)) || (!requireIds && !Values.IsValidUnicode(value)))
            {
                throw new ArgumentException("An error detail list contains an invalid value.", path);
            }
        }
    }

    internal List<string>? CopyStringList(List<string>? values)
    {
        return values == null ? null : new List<string>(values);
    }

    internal void CopyUnknownProperties(
        Dictionary<string, JsonElement>? source,
        Dictionary<string, JsonElement> destination,
        string path)
    {
        if (source == null)
        {
            throw new ArgumentException("An unknown-property map cannot be null.", path);
        }

        foreach (KeyValuePair<string, JsonElement> property in source)
        {
            if (!Values.IsValidUnicode(property.Key) || property.Value.ValueKind == JsonValueKind.Undefined)
            {
                throw new ArgumentException("An unknown error property is invalid.", path);
            }

            destination.Add(property.Key, property.Value.Clone());
        }
    }

    internal JmapProtocolException ProtocolFailure(string message, string path, JsonElement data)
    {
        JsonElement context = data.ValueKind == JsonValueKind.Undefined ? default(JsonElement) : data.Clone();
        return new JmapProtocolException(message, path, context);
    }
}
