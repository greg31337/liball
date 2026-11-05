namespace uuid;

internal interface IIdentifierFormat
{
    IdentifierValidationResult Validate(string text);
    IdentifierParseResult Parse(string text);
}

internal class UuidIdentifierFormat : IIdentifierFormat
{
    public IdentifierValidationResult Validate(string text)
    {
        return new Uuid().Validate(text);
    }

    public IdentifierParseResult Parse(string text)
    {
        UuidParseResult result = new Uuid().Parse(text);
        if (!result.Ok || result.Value == null)
        {
            return IdentifierParseResult.Failure(result.Code, result.Error);
        }

        if (!result.Value.HasRfc9562Variant() && !result.Value.IsNil() && !result.Value.IsMax())
        {
            return IdentifierParseResult.Failure(IdentifierErrorCode.InvalidUuidVariant, "UUID does not use RFC 9562 variant bits");
        }

        return IdentifierParseResult.Success(result.Value, result.NormalizedText);
    }
}

internal class UlidIdentifierFormat : IIdentifierFormat
{
    public IdentifierValidationResult Validate(string text)
    {
        return new Ulid().Validate(text);
    }

    public IdentifierParseResult Parse(string text)
    {
        UlidParseResult result = new Ulid().Parse(text);
        if (!result.Ok || result.Value == null)
        {
            return IdentifierParseResult.Failure(result.Code, result.Error);
        }

        return IdentifierParseResult.Success(result.Value, result.NormalizedText);
    }
}

internal class KsuidIdentifierFormat : IIdentifierFormat
{
    public IdentifierValidationResult Validate(string text)
    {
        return new Ksuid().Validate(text);
    }

    public IdentifierParseResult Parse(string text)
    {
        KsuidParseResult result = new Ksuid().Parse(text);
        if (!result.Ok || result.Value == null)
        {
            return IdentifierParseResult.Failure(result.Code, result.Error);
        }

        return IdentifierParseResult.Success(result.Value, result.NormalizedText);
    }
}

internal class ObjectIdIdentifierFormat : IIdentifierFormat
{
    public IdentifierValidationResult Validate(string text)
    {
        return new ObjectId().Validate(text);
    }

    public IdentifierParseResult Parse(string text)
    {
        ObjectIdParseResult result = new ObjectId().Parse(text);
        if (!result.Ok || result.Value == null)
        {
            return IdentifierParseResult.Failure(result.Code, result.Error);
        }

        return IdentifierParseResult.Success(result.Value, result.NormalizedText);
    }
}

internal class SnowflakeIdentifierFormat : IIdentifierFormat
{
    public IdentifierValidationResult Validate(string text)
    {
        SnowflakeParseResult result = SnowflakeGenerator.ParseDecimalString(text, SnowflakeSettings.TwitterDefault());
        if (!result.Ok)
        {
            return IdentifierValidationResult.Failure(result.Code, result.Error);
        }

        return IdentifierValidationResult.Success(IdentifierKind.Snowflake, result.NormalizedText);
    }

    public IdentifierParseResult Parse(string text)
    {
        SnowflakeParseResult result = SnowflakeGenerator.ParseDecimalString(text, SnowflakeSettings.TwitterDefault());
        if (!result.Ok || result.Value == null)
        {
            return IdentifierParseResult.Failure(result.Code, result.Error);
        }

        return IdentifierParseResult.Success(result.Value, result.NormalizedText);
    }
}

internal class NanoIdIdentifierFormat : IIdentifierFormat
{
    public IdentifierValidationResult Validate(string text)
    {
        return new NanoId().Validate(text);
    }

    public IdentifierParseResult Parse(string text)
    {
        IdentifierValidationResult result = new NanoId().Validate(text);
        if (!result.Ok)
        {
            return IdentifierParseResult.Failure(result.Code, result.Error);
        }

        IIdentifierValue value = new TextIdentifierValue(IdentifierKind.NanoId, result.NormalizedText);
        return IdentifierParseResult.Success(value, result.NormalizedText);
    }
}

internal class TextIdentifierValue : IIdentifierValue
{
    internal IdentifierKind IdentifierKind;
    internal string Text;

    internal TextIdentifierValue(IdentifierKind kind, string text)
    {
        IdentifierKind = kind;
        Text = text;
    }

    public IdentifierKind Kind()
    {
        return IdentifierKind;
    }

    public string ToCanonicalString()
    {
        return Text;
    }
}
