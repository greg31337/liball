namespace svg;

/// <summary>Defines separator policies for numeric list grammars.</summary>
internal enum SvgListSeparatorMode
{
    /// <summary>Accepts a comma with optional SVG whitespace or one or more SVG whitespace characters.</summary>
    CommaOrWhitespace = 0,

    /// <summary>Requires a comma and permits SVG whitespace around it.</summary>
    Comma = 1,

    /// <summary>Requires one or more SVG whitespace characters.</summary>
    Whitespace = 2,

    /// <summary>Requires a semicolon and permits SVG whitespace around it.</summary>
    Semicolon = 3
}

/// <summary>Defines stable failures produced by numeric list parsing.</summary>
internal enum SvgListParseError
{
    /// <summary>No list failure occurred.</summary>
    None = 0,

    /// <summary>A list item was required at the reported offset.</summary>
    ExpectedItem = 1,

    /// <summary>A numeric item was malformed, too long, or out of range.</summary>
    InvalidItem = 2,

    /// <summary>The configured separator was missing.</summary>
    MissingSeparator = 3,

    /// <summary>An explicit comma or semicolon was not followed by an item.</summary>
    TrailingSeparator = 4,

    /// <summary>The configured item or point limit was exceeded.</summary>
    ItemLimitExceeded = 5,

    /// <summary>A points list contained an unpaired coordinate.</summary>
    OddCoordinateCount = 6,

    /// <summary>A parser argument, limit, or separator mode was invalid.</summary>
    InvalidArgument = 7
}

/// <summary>Contains a bounded floating-point list parse result.</summary>
internal class SvgNumberListResult
{
    /// <summary>Indicates whether the complete list was valid.</summary>
    public bool Success;

    /// <summary>The list-level failure kind.</summary>
    public SvgListParseError Error;

    /// <summary>The item-level failure kind, or no error for separator and limit failures.</summary>
    public SvgNumberParseError NumberError;

    /// <summary>The successfully parsed prefix, including every value after success.</summary>
    public double[] Values;

    /// <summary>The first item offset after leading whitespace.</summary>
    public int Start;

    /// <summary>The next unread offset after success or failure.</summary>
    public int NextOffset;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Creates a floating-point list result.</summary>
    /// <param name="success">Whether the complete list was valid.</param>
    /// <param name="error">The list-level failure kind.</param>
    /// <param name="numberError">The item-level failure kind.</param>
    /// <param name="values">The successfully parsed prefix.</param>
    /// <param name="start">The first item offset.</param>
    /// <param name="nextOffset">The next unread offset.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    public SvgNumberListResult(bool success, SvgListParseError error, SvgNumberParseError numberError, double[] values, int start, int nextOffset, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        Values = values;
        Start = start;
        NextOffset = nextOffset;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a bounded integer list parse result.</summary>
internal class SvgIntegerListResult
{
    /// <summary>Indicates whether the complete list was valid.</summary>
    public bool Success;

    /// <summary>The list-level failure kind.</summary>
    public SvgListParseError Error;

    /// <summary>The item-level failure kind, or no error for separator and limit failures.</summary>
    public SvgNumberParseError NumberError;

    /// <summary>The successfully parsed prefix, including every value after success.</summary>
    public int[] Values;

    /// <summary>The first item offset after leading whitespace.</summary>
    public int Start;

    /// <summary>The next unread offset after success or failure.</summary>
    public int NextOffset;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Creates an integer list result.</summary>
    /// <param name="success">Whether the complete list was valid.</param>
    /// <param name="error">The list-level failure kind.</param>
    /// <param name="numberError">The item-level failure kind.</param>
    /// <param name="values">The successfully parsed prefix.</param>
    /// <param name="start">The first item offset.</param>
    /// <param name="nextOffset">The next unread offset.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    public SvgIntegerListResult(bool success, SvgListParseError error, SvgNumberParseError numberError, int[] values, int start, int nextOffset, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        Values = values;
        Start = start;
        NextOffset = nextOffset;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a bounded SVG points-list parse result using dense coordinate storage.</summary>
internal class SvgPointListResult
{
    /// <summary>Indicates whether the complete points list was valid.</summary>
    public bool Success;

    /// <summary>The list-level failure kind.</summary>
    public SvgListParseError Error;

    /// <summary>The item-level number failure kind.</summary>
    public SvgNumberParseError NumberError;

    /// <summary>Alternating x and y coordinates for the successfully parsed prefix.</summary>
    public double[] Coordinates;

    /// <summary>The number of complete points in the coordinate prefix.</summary>
    public int PointCount;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Creates a points-list result.</summary>
    /// <param name="success">Whether the complete points list was valid.</param>
    /// <param name="error">The list-level failure kind.</param>
    /// <param name="numberError">The item-level number failure kind.</param>
    /// <param name="coordinates">The alternating coordinate prefix.</param>
    /// <param name="pointCount">The complete point count.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    public SvgPointListResult(bool success, SvgListParseError error, SvgNumberParseError numberError, double[] coordinates, int pointCount, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        Coordinates = coordinates;
        PointCount = pointCount;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a required first number and an optional second number.</summary>
internal class SvgOptionalNumberPairResult
{
    /// <summary>Indicates whether the complete pair grammar was valid.</summary>
    public bool Success;

    /// <summary>The list-level failure kind.</summary>
    public SvgListParseError Error;

    /// <summary>The item-level number failure kind.</summary>
    public SvgNumberParseError NumberError;

    /// <summary>The required first number, or zero after failure before the first item.</summary>
    public double First;

    /// <summary>The optional second number, or zero when it was absent.</summary>
    public double Second;

    /// <summary>Indicates whether a second number was present.</summary>
    public bool HasSecond;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Creates an optional number-pair result.</summary>
    /// <param name="success">Whether the complete pair grammar was valid.</param>
    /// <param name="error">The list-level failure kind.</param>
    /// <param name="numberError">The item-level number failure kind.</param>
    /// <param name="first">The first number.</param>
    /// <param name="second">The optional second number.</param>
    /// <param name="hasSecond">Whether a second number was present.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    public SvgOptionalNumberPairResult(bool success, SvgListParseError error, SvgNumberParseError numberError, double first, double second, bool hasSecond, int errorOffset)
    {
        Success = success;
        Error = error;
        NumberError = numberError;
        First = first;
        Second = second;
        HasSecond = hasSecond;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains the result of scanning between two list items.</summary>
internal class SvgListSeparatorResult
{
    /// <summary>Indicates whether a separator was consumed or the list ended.</summary>
    public bool Success;

    /// <summary>Indicates whether only trailing whitespace remained.</summary>
    public bool EndOfList;

    /// <summary>The separator failure kind.</summary>
    public SvgListParseError Error;

    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a separator scan result.</summary>
    /// <param name="success">Whether scanning succeeded.</param>
    /// <param name="endOfList">Whether the list ended.</param>
    /// <param name="error">The separator failure kind.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    public SvgListSeparatorResult(bool success, bool endOfList, SvgListParseError error, int errorOffset)
    {
        Success = success;
        EndOfList = endOfList;
        Error = error;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Parses bounded SVG numeric lists, coordinates, points, and optional pairs.</summary>
internal static class SvgListParser
{
    /// <summary>Parses one standalone SVG coordinate.</summary>
    /// <param name="value">The coordinate text.</param>
    /// <param name="maxTokenCharacters">The maximum numeric token length.</param>
    /// <returns>A finite number parse result.</returns>
    public static SvgNumberParseResult ParseCoordinate(string value, int maxTokenCharacters)
    {
        return SvgNumberParser.ParseNumber(value, maxTokenCharacters);
    }

    /// <summary>Parses a bounded number list with an explicit separator policy.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="separatorMode">The separator policy.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum length of one numeric token.</param>
    /// <param name="allowEmpty">Whether whitespace-only input is valid.</param>
    /// <returns>A bounded number-list result.</returns>
    public static SvgNumberListResult ParseNumberList(string? value, SvgListSeparatorMode separatorMode, int maxItems, int maxTokenCharacters, bool allowEmpty)
    {
        if (value == null || maxItems < 1 || maxTokenCharacters < 1 || !IsSeparatorMode(separatorMode))
        {
            return new SvgNumberListResult(false, SvgListParseError.InvalidArgument, SvgNumberParseError.None, new double[0], 0, 0, 0);
        }

        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipWhitespace();
        int start = reader.GetPosition();
        System.Collections.Generic.List<double> values = new System.Collections.Generic.List<double>();
        if (reader.IsEnd())
        {
            if (allowEmpty)
            {
                return new SvgNumberListResult(true, SvgListParseError.None, SvgNumberParseError.None, values.ToArray(), start, reader.GetPosition(), -1);
            }
            return new SvgNumberListResult(false, SvgListParseError.ExpectedItem, SvgNumberParseError.ExpectedNumber, values.ToArray(), start, start, start);
        }

        while (true)
        {
            if (values.Count >= maxItems)
            {
                return new SvgNumberListResult(false, SvgListParseError.ItemLimitExceeded, SvgNumberParseError.None, values.ToArray(), start, reader.GetPosition(), reader.GetPosition());
            }
            SvgNumberParseResult item = SvgNumberParser.ReadNumber(reader);
            if (!item.Success)
            {
                SvgListParseError error = item.Error == SvgNumberParseError.ExpectedNumber ? SvgListParseError.ExpectedItem : SvgListParseError.InvalidItem;
                return new SvgNumberListResult(false, error, item.Error, values.ToArray(), start, reader.GetPosition(), item.ErrorOffset);
            }
            values.Add(item.Value);

            SvgListSeparatorResult separator = ReadSeparator(reader, separatorMode);
            if (!separator.Success)
            {
                return new SvgNumberListResult(false, separator.Error, SvgNumberParseError.None, values.ToArray(), start, reader.GetPosition(), separator.ErrorOffset);
            }
            if (separator.EndOfList)
            {
                return new SvgNumberListResult(true, SvgListParseError.None, SvgNumberParseError.None, values.ToArray(), start, reader.GetPosition(), -1);
            }
        }
    }

    /// <summary>Parses a bounded integer list with an explicit separator policy.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="separatorMode">The separator policy.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum length of one integer token.</param>
    /// <param name="allowEmpty">Whether whitespace-only input is valid.</param>
    /// <returns>A bounded integer-list result.</returns>
    public static SvgIntegerListResult ParseIntegerList(string? value, SvgListSeparatorMode separatorMode, int maxItems, int maxTokenCharacters, bool allowEmpty)
    {
        if (value == null || maxItems < 1 || maxTokenCharacters < 1 || !IsSeparatorMode(separatorMode))
        {
            return new SvgIntegerListResult(false, SvgListParseError.InvalidArgument, SvgNumberParseError.None, new int[0], 0, 0, 0);
        }

        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipWhitespace();
        int start = reader.GetPosition();
        System.Collections.Generic.List<int> values = new System.Collections.Generic.List<int>();
        if (reader.IsEnd())
        {
            if (allowEmpty)
            {
                return new SvgIntegerListResult(true, SvgListParseError.None, SvgNumberParseError.None, values.ToArray(), start, reader.GetPosition(), -1);
            }
            return new SvgIntegerListResult(false, SvgListParseError.ExpectedItem, SvgNumberParseError.ExpectedInteger, values.ToArray(), start, start, start);
        }

        while (true)
        {
            if (values.Count >= maxItems)
            {
                return new SvgIntegerListResult(false, SvgListParseError.ItemLimitExceeded, SvgNumberParseError.None, values.ToArray(), start, reader.GetPosition(), reader.GetPosition());
            }
            SvgIntegerParseResult item = SvgNumberParser.ReadInteger(reader);
            if (!item.Success)
            {
                SvgListParseError error = item.Error == SvgNumberParseError.ExpectedInteger ? SvgListParseError.ExpectedItem : SvgListParseError.InvalidItem;
                return new SvgIntegerListResult(false, error, item.Error, values.ToArray(), start, reader.GetPosition(), item.ErrorOffset);
            }
            values.Add(item.Value);

            SvgListSeparatorResult separator = ReadSeparator(reader, separatorMode);
            if (!separator.Success)
            {
                return new SvgIntegerListResult(false, separator.Error, SvgNumberParseError.None, values.ToArray(), start, reader.GetPosition(), separator.ErrorOffset);
            }
            if (separator.EndOfList)
            {
                return new SvgIntegerListResult(true, SvgListParseError.None, SvgNumberParseError.None, values.ToArray(), start, reader.GetPosition(), -1);
            }
        }
    }

    /// <summary>Parses an optionally empty SVG coordinate list separated by commas or whitespace.</summary>
    /// <param name="value">The coordinate-list text.</param>
    /// <param name="maxCoordinates">The maximum coordinate count.</param>
    /// <param name="maxTokenCharacters">The maximum length of one coordinate token.</param>
    /// <returns>A bounded coordinate-list result.</returns>
    public static SvgNumberListResult ParseCoordinateList(string value, int maxCoordinates, int maxTokenCharacters)
    {
        return ParseNumberList(value, SvgListSeparatorMode.CommaOrWhitespace, maxCoordinates, maxTokenCharacters, true);
    }

    /// <summary>Parses a comma-only number list with optional surrounding whitespace.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum length of one number token.</param>
    /// <param name="allowEmpty">Whether whitespace-only input is valid.</param>
    /// <returns>A bounded number-list result.</returns>
    public static SvgNumberListResult ParseCommaNumberList(string value, int maxItems, int maxTokenCharacters, bool allowEmpty)
    {
        return ParseNumberList(value, SvgListSeparatorMode.Comma, maxItems, maxTokenCharacters, allowEmpty);
    }

    /// <summary>Parses a whitespace-only number list.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum length of one number token.</param>
    /// <param name="allowEmpty">Whether whitespace-only input is valid.</param>
    /// <returns>A bounded number-list result.</returns>
    public static SvgNumberListResult ParseWhitespaceNumberList(string value, int maxItems, int maxTokenCharacters, bool allowEmpty)
    {
        return ParseNumberList(value, SvgListSeparatorMode.Whitespace, maxItems, maxTokenCharacters, allowEmpty);
    }

    /// <summary>Parses a semicolon-only number list with optional surrounding whitespace.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum item count.</param>
    /// <param name="maxTokenCharacters">The maximum length of one number token.</param>
    /// <param name="allowEmpty">Whether whitespace-only input is valid.</param>
    /// <returns>A bounded number-list result.</returns>
    public static SvgNumberListResult ParseSemicolonNumberList(string value, int maxItems, int maxTokenCharacters, bool allowEmpty)
    {
        return ParseNumberList(value, SvgListSeparatorMode.Semicolon, maxItems, maxTokenCharacters, allowEmpty);
    }

    /// <summary>Parses an SVG points list using an explicit point limit.</summary>
    /// <param name="value">The points text.</param>
    /// <param name="maxPoints">The maximum point count.</param>
    /// <param name="maxTokenCharacters">The maximum length of one coordinate token.</param>
    /// <returns>A dense points-list result.</returns>
    public static SvgPointListResult ParsePoints(string value, int maxPoints, int maxTokenCharacters)
    {
        if (maxPoints < 1)
        {
            return new SvgPointListResult(false, SvgListParseError.InvalidArgument, SvgNumberParseError.None, new double[0], 0, 0);
        }
        int maxCoordinates = maxPoints > 1073741823 ? 2147483647 : maxPoints * 2;
        SvgNumberListResult coordinates = ParseCoordinateList(value, maxCoordinates, maxTokenCharacters);
        if (!coordinates.Success)
        {
            return new SvgPointListResult(false, coordinates.Error, coordinates.NumberError, coordinates.Values, coordinates.Values.Length / 2, coordinates.ErrorOffset);
        }
        if (coordinates.Values.Length % 2 != 0)
        {
            return new SvgPointListResult(false, SvgListParseError.OddCoordinateCount, SvgNumberParseError.None, coordinates.Values, coordinates.Values.Length / 2, coordinates.NextOffset);
        }
        return new SvgPointListResult(true, SvgListParseError.None, SvgNumberParseError.None, coordinates.Values, coordinates.Values.Length / 2, -1);
    }

    /// <summary>Parses an SVG points list using the configured geometry point limit.</summary>
    /// <param name="value">The points text.</param>
    /// <param name="limits">The geometry limits.</param>
    /// <param name="maxTokenCharacters">The maximum length of one coordinate token.</param>
    /// <returns>A dense points-list result.</returns>
    public static SvgPointListResult ParsePoints(string value, SvgGeometryLimits? limits, int maxTokenCharacters)
    {
        if (limits == null)
        {
            return new SvgPointListResult(false, SvgListParseError.InvalidArgument, SvgNumberParseError.None, new double[0], 0, 0);
        }
        return ParsePoints(value, limits.MaxPoints, maxTokenCharacters);
    }

    /// <summary>Parses one required number and one optional comma-or-whitespace-separated number.</summary>
    /// <param name="value">The pair text.</param>
    /// <param name="maxTokenCharacters">The maximum length of one number token.</param>
    /// <returns>An optional number-pair result.</returns>
    public static SvgOptionalNumberPairResult ParseOptionalNumberPair(string value, int maxTokenCharacters)
    {
        SvgNumberListResult list = ParseNumberList(value, SvgListSeparatorMode.CommaOrWhitespace, 2, maxTokenCharacters, false);
        double first = list.Values.Length > 0 ? list.Values[0] : 0;
        double second = list.Values.Length > 1 ? list.Values[1] : 0;
        bool hasSecond = list.Values.Length > 1;
        return new SvgOptionalNumberPairResult(list.Success, list.Error, list.NumberError, first, second, hasSecond, list.ErrorOffset);
    }

    static SvgListSeparatorResult ReadSeparator(SvgSpanReader reader, SvgListSeparatorMode mode)
    {
        int whitespace = reader.SkipWhitespace();
        if (reader.IsEnd())
        {
            return new SvgListSeparatorResult(true, true, SvgListParseError.None, -1);
        }

        if (mode == SvgListSeparatorMode.Whitespace)
        {
            if (whitespace > 0)
            {
                return new SvgListSeparatorResult(true, false, SvgListParseError.None, -1);
            }
            return new SvgListSeparatorResult(false, false, SvgListParseError.MissingSeparator, reader.GetPosition());
        }

        char delimiter = mode == SvgListSeparatorMode.Semicolon ? ';' : ',';
        if (reader.Consume(delimiter))
        {
            reader.SkipWhitespace();
            if (reader.IsEnd())
            {
                return new SvgListSeparatorResult(false, false, SvgListParseError.TrailingSeparator, reader.GetPosition());
            }
            return new SvgListSeparatorResult(true, false, SvgListParseError.None, -1);
        }

        if (mode == SvgListSeparatorMode.CommaOrWhitespace && whitespace > 0)
        {
            return new SvgListSeparatorResult(true, false, SvgListParseError.None, -1);
        }
        return new SvgListSeparatorResult(false, false, SvgListParseError.MissingSeparator, reader.GetPosition());
    }

    static bool IsSeparatorMode(SvgListSeparatorMode mode)
    {
        return mode == SvgListSeparatorMode.CommaOrWhitespace || mode == SvgListSeparatorMode.Comma || mode == SvgListSeparatorMode.Whitespace || mode == SvgListSeparatorMode.Semicolon;
    }
}
