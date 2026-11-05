using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace htmlgui;

internal class RouteTemplateResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string CanonicalTemplate = string.Empty;
    internal UiRouteSegment[] Segments = Array.Empty<UiRouteSegment>();
}

internal class RouteMatchResult
{
    internal bool Success;
    internal bool Matched;
    internal string Code = string.Empty;
    internal UiRouteData? RouteData;
}

internal class RouteCollectionResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string FirstRouteId = string.Empty;
    internal string SecondRouteId = string.Empty;
    internal UiRoute[] Routes = Array.Empty<UiRoute>();
}

internal class RouteParameterResult
{
    internal bool Matched;
    internal UiValue Value = new UiValue();
}

internal class RouteIntegerResult
{
    internal bool Success;
    internal long Value;
}

internal class RouteTemplateCore
{
    internal const string LiteralSegment = "literal";
    internal const string ParameterSegment = "parameter";
    internal const string CatchAllSegment = "catch-all";

    internal RouteCore UrlCore = new RouteCore();

    internal RouteTemplateResult ParseRouteTemplate(string? template)
    {
        if (string.IsNullOrWhiteSpace(template) || template.Length > RouteCore.MaximumRouteUrlLength || template[0] != '/' || template.IndexOf('#') >= 0 || template.IndexOf('\\') >= 0 || UrlCore.ContainsInvalidRouteText(template))
        {
            return CreateRouteTemplateFailure("route.invalid-template");
        }

        if (ValueComparer.TextEquals(template, "/"))
        {
            RouteTemplateResult root = new RouteTemplateResult();
            root.Success = true;
            root.CanonicalTemplate = "/";
            return root;
        }

        string content = template.Substring(1);
        if (content.EndsWith("/", StringComparison.Ordinal))
        {
            content = content.Substring(0, content.Length - 1);
        }

        if (content.Length == 0)
        {
            RouteTemplateResult root = new RouteTemplateResult();
            root.Success = true;
            root.CanonicalTemplate = "/";
            return root;
        }

        string[] rawSegments = content.Split('/');
        if (rawSegments.Length > RouteCore.MaximumRouteSegmentCount)
        {
            return CreateRouteTemplateFailure("route.too-many-template-segments");
        }

        List<UiRouteSegment> segments = new List<UiRouteSegment>();
        int index = 0;
        bool optionalSeen = false;
        while (index < rawSegments.Length)
        {
            if (rawSegments[index].Length == 0)
            {
                return CreateRouteTemplateFailure("route.empty-template-segment");
            }

            RouteTemplateResult parsedSegment = ParseRouteTemplateSegment(rawSegments[index]);
            if (!parsedSegment.Success || parsedSegment.Segments.Length != 1)
            {
                return parsedSegment;
            }

            UiRouteSegment segment = parsedSegment.Segments[0];
            if (!ValueComparer.TextEquals(segment.Kind, LiteralSegment))
            {
                if (ContainsRouteParameterName(segments, segment.Value))
                {
                    return CreateRouteTemplateFailure("route.duplicate-parameter");
                }

                if (ValueComparer.TextEquals(segment.Kind, CatchAllSegment) && index != rawSegments.Length - 1)
                {
                    return CreateRouteTemplateFailure("route.catch-all-not-last");
                }

                if (segment.IsOptional)
                {
                    optionalSeen = true;
                }
                else if (optionalSeen)
                {
                    return CreateRouteTemplateFailure("route.required-after-optional");
                }
            }
            else if (optionalSeen)
            {
                return CreateRouteTemplateFailure("route.required-after-optional");
            }

            segments.Add(segment);
            index++;
        }

        RouteTemplateResult result = new RouteTemplateResult();
        result.Success = true;
        result.Segments = CopyRouteTemplateSegments(segments.ToArray());
        result.CanonicalTemplate = BuildCanonicalRouteTemplate(result.Segments);
        return result;
    }

    internal RouteTemplateResult ParseRouteTemplateSegment(string rawSegment)
    {
        if (rawSegment[0] != '{' && rawSegment[rawSegment.Length - 1] != '}')
        {
            if (rawSegment.IndexOf('{') >= 0 || rawSegment.IndexOf('}') >= 0)
            {
                return CreateRouteTemplateFailure("route.invalid-template-brace");
            }

            if (rawSegment.IndexOf('?') >= 0)
            {
                return CreateRouteTemplateFailure("route.invalid-template");
            }

            RouteSegmentResult literal = UrlCore.DecodeRouteComponent(rawSegment, false, true);
            if (!literal.Success || literal.Text.Length == 0 || ValueComparer.TextEquals(literal.Text, ".") || ValueComparer.TextEquals(literal.Text, ".."))
            {
                return CreateRouteTemplateFailure(literal.Success ? "route.invalid-literal" : literal.Code);
            }

            UiRouteSegment literalSegment = new UiRouteSegment();
            literalSegment.Kind = LiteralSegment;
            literalSegment.Value = literal.Text;
            return CreateSingleRouteTemplateSuccess(literalSegment);
        }

        if (rawSegment[0] != '{' || rawSegment[rawSegment.Length - 1] != '}' || rawSegment.Length < 3 || rawSegment.IndexOf('{', 1) >= 0 || rawSegment.IndexOf('}') != rawSegment.Length - 1)
        {
            return CreateRouteTemplateFailure("route.invalid-template-brace");
        }

        string body = rawSegment.Substring(1, rawSegment.Length - 2);
        bool catchAll = body[0] == '*';
        if (catchAll)
        {
            body = body.Substring(1);
        }

        bool explicitOptional = body.EndsWith("?", StringComparison.Ordinal);
        if (explicitOptional)
        {
            body = body.Substring(0, body.Length - 1);
        }

        int equalsIndex = body.IndexOf('=');
        int constraintIndex = body.IndexOf(':');
        if (body.IndexOf('?', 0) >= 0 || equalsIndex >= 0 && body.IndexOf('=', equalsIndex + 1) >= 0 || constraintIndex >= 0 && body.IndexOf(':', constraintIndex + 1) >= 0 || constraintIndex >= 0 && equalsIndex >= 0 && constraintIndex > equalsIndex)
        {
            return CreateRouteTemplateFailure("route.invalid-parameter-syntax");
        }

        int nameEnd = body.Length;
        if (constraintIndex >= 0)
        {
            nameEnd = constraintIndex;
        }
        else if (equalsIndex >= 0)
        {
            nameEnd = equalsIndex;
        }

        string name = body.Substring(0, nameEnd);
        if (!IsValidRouteParameterName(name))
        {
            return CreateRouteTemplateFailure("route.invalid-parameter-name");
        }

        string? constraint = null;
        if (constraintIndex >= 0)
        {
            int constraintEnd = equalsIndex >= 0 ? equalsIndex : body.Length;
            constraint = body.Substring(constraintIndex + 1, constraintEnd - constraintIndex - 1).ToLowerInvariant();
            if (!IsKnownRouteConstraint(constraint))
            {
                return CreateRouteTemplateFailure("route.unknown-constraint");
            }
        }

        string? defaultValue = null;
        if (equalsIndex >= 0)
        {
            string rawDefault = body.Substring(equalsIndex + 1);
            RouteSegmentResult decodedDefault = UrlCore.DecodeRouteComponent(rawDefault, false, true);
            if (!decodedDefault.Success)
            {
                return CreateRouteTemplateFailure(decodedDefault.Code);
            }

            defaultValue = decodedDefault.Text;
        }

        if (catchAll && (explicitOptional || defaultValue != null))
        {
            return CreateRouteTemplateFailure("route.invalid-catch-all-modifier");
        }

        if (explicitOptional && defaultValue != null)
        {
            return CreateRouteTemplateFailure("route.conflicting-parameter-modifiers");
        }

        if (defaultValue != null && !ConvertRouteParameter(defaultValue, constraint).Matched)
        {
            return CreateRouteTemplateFailure("route.invalid-default");
        }

        UiRouteSegment parameter = new UiRouteSegment();
        parameter.Kind = catchAll ? CatchAllSegment : ParameterSegment;
        parameter.Value = name;
        parameter.Constraint = constraint;
        parameter.DefaultValue = defaultValue;
        parameter.IsOptional = catchAll || explicitOptional || defaultValue != null;
        return CreateSingleRouteTemplateSuccess(parameter);
    }

    internal RouteMatchResult MatchRouteTemplate(UiRoute? route, RouteUrlResult? url)
    {
        if (route == null)
        {
            return CreateRouteMatchFailure("route.null-route");
        }

        if (url == null || !url.Success || url.IsExternal)
        {
            return CreateRouteMatchFailure("route.invalid-match-url");
        }

        if (string.IsNullOrWhiteSpace(route.Id))
        {
            return CreateRouteMatchFailure("route.invalid-id");
        }

        RouteTemplateResult parsed = ParseRouteTemplate(route.Template);
        if (!parsed.Success)
        {
            return CreateRouteMatchFailure(parsed.Code);
        }

        List<UiValueEntry> parameters = new List<UiValueEntry>();
        int segmentIndex = 0;
        int pathIndex = 0;
        while (segmentIndex < parsed.Segments.Length)
        {
            UiRouteSegment segment = parsed.Segments[segmentIndex];
            if (ValueComparer.TextEquals(segment.Kind, LiteralSegment))
            {
                if (pathIndex >= url.PathSegments.Length || !ValueComparer.TextEquals(segment.Value, url.PathSegments[pathIndex]))
                {
                    return CreateRouteNotMatched();
                }

                pathIndex++;
            }
            else if (ValueComparer.TextEquals(segment.Kind, CatchAllSegment))
            {
                string catchAllValue = JoinCatchAllRouteValue(url.PathSegments, pathIndex);
                RouteParameterResult convertedCatchAll = ConvertRouteParameter(catchAllValue, segment.Constraint);
                if (!convertedCatchAll.Matched)
                {
                    return CreateRouteNotMatched();
                }

                parameters.Add(new UiValueEntry { Key = segment.Value, Value = convertedCatchAll.Value });
                pathIndex = url.PathSegments.Length;
            }
            else
            {
                string? supplied = pathIndex < url.PathSegments.Length ? url.PathSegments[pathIndex] : segment.DefaultValue;
                if (supplied == null)
                {
                    if (!segment.IsOptional)
                    {
                        return CreateRouteNotMatched();
                    }

                    parameters.Add(new UiValueEntry { Key = segment.Value, Value = new UiValue() });
                }
                else
                {
                    RouteParameterResult converted = ConvertRouteParameter(supplied, segment.Constraint);
                    if (!converted.Matched)
                    {
                        return CreateRouteNotMatched();
                    }

                    parameters.Add(new UiValueEntry { Key = segment.Value, Value = converted.Value });
                    if (pathIndex < url.PathSegments.Length)
                    {
                        pathIndex++;
                    }
                }
            }

            segmentIndex++;
        }

        if (pathIndex != url.PathSegments.Length)
        {
            return CreateRouteNotMatched();
        }

        RouteMatchResult result = new RouteMatchResult();
        result.Success = true;
        result.Matched = true;
        result.RouteData = new UiRouteData();
        result.RouteData.RouteId = route.Id;
        result.RouteData.Parameters = parameters.ToArray();
        result.RouteData.Query = CopyRouteQueryEntries(url.Query);
        result.RouteData.Fragment = url.Fragment;
        return result;
    }

    internal RouteCollectionResult ValidateRouteCollection(UiRoute[]? routes)
    {
        if (routes == null)
        {
            return CreateRouteCollectionFailure("route.null-routes", string.Empty, string.Empty);
        }

        List<UiRoute> parsedRoutes = new List<UiRoute>();
        int index = 0;
        while (index < routes.Length)
        {
            UiRoute? route = routes[index];
            if (route == null)
            {
                return CreateRouteCollectionFailure("route.null-route", string.Empty, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(route.Id))
            {
                return CreateRouteCollectionFailure("route.invalid-id", string.Empty, string.Empty);
            }

            if (FindRouteId(parsedRoutes, route.Id) >= 0)
            {
                return CreateRouteCollectionFailure("route.duplicate-id", route.Id, route.Id);
            }

            RouteTemplateResult parsed = ParseRouteTemplate(route.Template);
            if (!parsed.Success)
            {
                return CreateRouteCollectionFailure(parsed.Code, route.Id, string.Empty);
            }

            UiRoute copy = CopyParsedRoute(route, parsed);
            int conflictIndex = 0;
            while (conflictIndex < parsedRoutes.Count)
            {
                if (HaveAmbiguousRouteTemplates(parsedRoutes[conflictIndex].Segments, copy.Segments))
                {
                    return CreateRouteCollectionFailure("route.ambiguous", parsedRoutes[conflictIndex].Id, copy.Id);
                }

                conflictIndex++;
            }

            InsertRouteByPrecedence(parsedRoutes, copy);
            index++;
        }

        RouteCollectionResult result = new RouteCollectionResult();
        result.Success = true;
        result.Routes = parsedRoutes.ToArray();
        return result;
    }

    internal int CompareRoutePrecedence(UiRoute left, UiRoute right)
    {
        int maximum = left.Segments.Length > right.Segments.Length ? left.Segments.Length : right.Segments.Length;
        int index = 0;
        while (index <= maximum)
        {
            int leftRank = index < left.Segments.Length ? GetRouteSegmentPrecedence(left.Segments[index]) : 350;
            int rightRank = index < right.Segments.Length ? GetRouteSegmentPrecedence(right.Segments[index]) : 350;
            if (leftRank != rightRank)
            {
                return rightRank - leftRank;
            }

            index++;
        }

        int templateComparison = ValueComparer.CompareText(left.Template, right.Template);
        return templateComparison != 0 ? templateComparison : ValueComparer.CompareText(left.Id, right.Id);
    }

    internal int GetRouteSegmentPrecedence(UiRouteSegment segment)
    {
        if (ValueComparer.TextEquals(segment.Kind, LiteralSegment))
        {
            return 500;
        }

        if (ValueComparer.TextEquals(segment.Kind, CatchAllSegment))
        {
            return 100;
        }

        if (segment.IsOptional)
        {
            return segment.Constraint == null ? 200 : 250;
        }

        return segment.Constraint == null ? 300 : 400;
    }

    internal bool HaveAmbiguousRouteTemplates(UiRouteSegment[] left, UiRouteSegment[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            UiRouteSegment leftSegment = left[index];
            UiRouteSegment rightSegment = right[index];
            if (!ValueComparer.TextEquals(leftSegment.Kind, rightSegment.Kind) || leftSegment.IsOptional != rightSegment.IsOptional)
            {
                return false;
            }

            if (ValueComparer.TextEquals(leftSegment.Kind, LiteralSegment))
            {
                if (!ValueComparer.TextEquals(leftSegment.Value, rightSegment.Value))
                {
                    return false;
                }
            }
            else if (leftSegment.Constraint == null != (rightSegment.Constraint == null))
            {
                return false;
            }
            else if (leftSegment.Constraint != null && rightSegment.Constraint != null && !RouteConstraintsOverlap(leftSegment.Constraint, rightSegment.Constraint))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool RouteConstraintsOverlap(string left, string right)
    {
        if (ValueComparer.TextEquals(left, right))
        {
            return true;
        }

        if (ValueComparer.TextEquals(left, "string") || ValueComparer.TextEquals(right, "string"))
        {
            return true;
        }

        bool leftNumeric = IsNumericRouteConstraint(left);
        bool rightNumeric = IsNumericRouteConstraint(right);
        return leftNumeric && rightNumeric;
    }

    internal bool IsNumericRouteConstraint(string constraint)
    {
        return ValueComparer.TextEquals(constraint, "int") || ValueComparer.TextEquals(constraint, "long") || ValueComparer.TextEquals(constraint, "decimal") || ValueComparer.TextEquals(constraint, "double") || ValueComparer.TextEquals(constraint, "float");
    }

    internal RouteParameterResult ConvertRouteParameter(string value, string? constraint)
    {
        RouteParameterResult result = new RouteParameterResult();
        if (constraint == null || ValueComparer.TextEquals(constraint, "string"))
        {
            result.Matched = true;
            result.Value.Kind = UiValueKind.String;
            result.Value.StringValue = value;
            return result;
        }

        if (ValueComparer.TextEquals(constraint, "bool"))
        {
            if (!ValueComparer.TextEqualsIgnoreCase(value, "true") && !ValueComparer.TextEqualsIgnoreCase(value, "false"))
            {
                return result;
            }

            result.Matched = true;
            result.Value.Kind = UiValueKind.Boolean;
            result.Value.BooleanValue = ValueComparer.TextEqualsIgnoreCase(value, "true");
            return result;
        }

        if (ValueComparer.TextEquals(constraint, "int") || ValueComparer.TextEquals(constraint, "long"))
        {
            RouteIntegerResult integer = ParseInvariantRouteInteger(value);
            if (!integer.Success || ValueComparer.TextEquals(constraint, "int") && (integer.Value < int.MinValue || integer.Value > int.MaxValue))
            {
                return result;
            }

            result.Matched = true;
            result.Value.Kind = UiValueKind.Integer;
            result.Value.IntegerValue = integer.Value;
            return result;
        }

        if (ValueComparer.TextEquals(constraint, "double") || ValueComparer.TextEquals(constraint, "float") || ValueComparer.TextEquals(constraint, "decimal"))
        {
            if (!IsInvariantRouteNumber(value))
            {
                return result;
            }

            double approximate = double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
            if (double.IsNaN(approximate) || double.IsInfinity(approximate))
            {
                return result;
            }

            if (ValueComparer.TextEquals(constraint, "decimal"))
            {
                double maximumDecimal = (double)decimal.MaxValue;
                if (Math.Abs(approximate) >= maximumDecimal && !IsMaximumRouteDecimal(value))
                {
                    return result;
                }

                result.Value.Kind = UiValueKind.Decimal;
                result.Value.DecimalValue = decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
            else if (ValueComparer.TextEquals(constraint, "float"))
            {
                float parsedFloat = float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
                if (float.IsNaN(parsedFloat) || float.IsInfinity(parsedFloat))
                {
                    return result;
                }

                result.Value.Kind = UiValueKind.FloatingPoint;
                result.Value.FloatingPointValue = parsedFloat;
            }
            else
            {
                result.Value.Kind = UiValueKind.FloatingPoint;
                result.Value.FloatingPointValue = approximate;
            }

            result.Matched = true;
            return result;
        }

        if (ValueComparer.TextEquals(constraint, "guid"))
        {
            if (!IsInvariantRouteGuid(value))
            {
                return result;
            }

            result.Matched = true;
            result.Value.Kind = UiValueKind.String;
            result.Value.StringValue = value.ToLowerInvariant();
            return result;
        }

        if (ValueComparer.TextEquals(constraint, "datetime"))
        {
            if (!IsInvariantRouteDateTime(value))
            {
                return result;
            }

            result.Matched = true;
            result.Value.Kind = UiValueKind.DateTime;
            result.Value.DateTimeValue = value;
        }

        return result;
    }

    internal RouteIntegerResult ParseInvariantRouteInteger(string value)
    {
        RouteIntegerResult result = new RouteIntegerResult();
        if (value.Length == 0)
        {
            return result;
        }

        bool negative = value[0] == '-';
        int index = negative || value[0] == '+' ? 1 : 0;
        if (index == value.Length)
        {
            return result;
        }

        long limit = negative ? long.MinValue : -long.MaxValue;
        long parsed = 0;
        while (index < value.Length)
        {
            char digitCharacter = value[index];
            if (digitCharacter < '0' || digitCharacter > '9')
            {
                return result;
            }

            int digit = digitCharacter - '0';
            if (parsed < limit / 10)
            {
                return result;
            }

            parsed *= 10;
            if (parsed < limit + digit)
            {
                return result;
            }

            parsed -= digit;
            index++;
        }

        result.Success = true;
        result.Value = negative ? parsed : -parsed;
        return result;
    }

    internal bool IsInvariantRouteNumber(string value)
    {
        if (value.Length == 0 || value.Length > 128)
        {
            return false;
        }

        int index = value[0] == '-' || value[0] == '+' ? 1 : 0;
        bool digitSeen = false;
        while (index < value.Length && value[index] >= '0' && value[index] <= '9')
        {
            digitSeen = true;
            index++;
        }

        if (index < value.Length && value[index] == '.')
        {
            index++;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9')
            {
                digitSeen = true;
                index++;
            }
        }

        if (!digitSeen)
        {
            return false;
        }

        if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
        {
            index++;
            if (index < value.Length && (value[index] == '-' || value[index] == '+'))
            {
                index++;
            }

            int exponentStart = index;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9')
            {
                index++;
            }

            if (index == exponentStart)
            {
                return false;
            }
        }

        return index == value.Length;
    }

    internal bool IsMaximumRouteDecimal(string value)
    {
        string unsigned = value.Length > 0 && (value[0] == '-' || value[0] == '+') ? value.Substring(1) : value;
        int decimalIndex = unsigned.IndexOf('.');
        string integer = decimalIndex < 0 ? unsigned : unsigned.Substring(0, decimalIndex);
        string fraction = decimalIndex < 0 ? string.Empty : unsigned.Substring(decimalIndex + 1);
        int index = 0;
        while (index < fraction.Length)
        {
            if (fraction[index] != '0')
            {
                return false;
            }

            index++;
        }

        return ValueComparer.TextEquals(integer, "79228162514264337593543950335");
    }

    internal bool IsInvariantRouteGuid(string value)
    {
        if (value.Length != 36 || value[8] != '-' || value[13] != '-' || value[18] != '-' || value[23] != '-')
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (index != 8 && index != 13 && index != 18 && index != 23 && UrlCore.ParseRouteHexDigit(value[index]) < 0)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsInvariantRouteDateTime(string value)
    {
        if (value.Length < 10 || value[4] != '-' || value[7] != '-')
        {
            return false;
        }

        int year = ReadRouteDatePart(value, 0, 4);
        int month = ReadRouteDatePart(value, 5, 2);
        int day = ReadRouteDatePart(value, 8, 2);
        if (year < 1 || month < 1 || month > 12 || day < 1 || day > GetRouteDaysInMonth(year, month))
        {
            return false;
        }

        if (value.Length == 10)
        {
            return true;
        }

        if (value.Length < 19 || value[10] != 'T' || value[13] != ':' || value[16] != ':')
        {
            return false;
        }

        int hour = ReadRouteDatePart(value, 11, 2);
        int minute = ReadRouteDatePart(value, 14, 2);
        int second = ReadRouteDatePart(value, 17, 2);
        if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
        {
            return false;
        }

        int index = 19;
        if (index < value.Length && value[index] == '.')
        {
            index++;
            int fractionStart = index;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9' && index - fractionStart < 7)
            {
                index++;
            }

            if (index == fractionStart)
            {
                return false;
            }
        }

        if (index == value.Length)
        {
            return true;
        }

        if (value[index] == 'Z')
        {
            return index + 1 == value.Length;
        }

        if ((value[index] == '+' || value[index] == '-') && index + 6 == value.Length && value[index + 3] == ':')
        {
            int offsetHour = ReadRouteDatePart(value, index + 1, 2);
            int offsetMinute = ReadRouteDatePart(value, index + 4, 2);
            return offsetHour >= 0 && offsetHour <= 14 && offsetMinute >= 0 && offsetMinute <= 59 && (offsetHour < 14 || offsetMinute == 0);
        }

        return false;
    }

    internal int ReadRouteDatePart(string value, int start, int length)
    {
        if (start < 0 || start + length > value.Length)
        {
            return -1;
        }

        int parsed = 0;
        int index = start;
        while (index < start + length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return -1;
            }

            parsed = parsed * 10 + value[index] - '0';
            index++;
        }

        return parsed;
    }

    internal int GetRouteDaysInMonth(int year, int month)
    {
        if (month == 2)
        {
            bool leap = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
            return leap ? 29 : 28;
        }

        return month == 4 || month == 6 || month == 9 || month == 11 ? 30 : 31;
    }

    internal bool IsKnownRouteConstraint(string constraint)
    {
        return ValueComparer.TextEquals(constraint, "string") || ValueComparer.TextEquals(constraint, "bool") || ValueComparer.TextEquals(constraint, "int") || ValueComparer.TextEquals(constraint, "long") || ValueComparer.TextEquals(constraint, "decimal") || ValueComparer.TextEquals(constraint, "double") || ValueComparer.TextEquals(constraint, "float") || ValueComparer.TextEquals(constraint, "guid") || ValueComparer.TextEquals(constraint, "datetime");
    }

    internal bool IsValidRouteParameterName(string name)
    {
        if (name.Length == 0 || !IsRouteParameterNameStart(name[0]))
        {
            return false;
        }

        int index = 1;
        while (index < name.Length)
        {
            char character = name[index];
            if (!IsRouteParameterNameStart(character) && !UrlCore.IsAsciiRouteDigit(character) && character != '-')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsRouteParameterNameStart(char value)
    {
        return UrlCore.IsAsciiRouteLetter(value) || value == '_';
    }

    internal bool ContainsRouteParameterName(List<UiRouteSegment> segments, string name)
    {
        int index = 0;
        while (index < segments.Count)
        {
            if (!ValueComparer.TextEquals(segments[index].Kind, LiteralSegment) && ValueComparer.TextEquals(segments[index].Value, name))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal string BuildCanonicalRouteTemplate(UiRouteSegment[] segments)
    {
        if (segments.Length == 0)
        {
            return "/";
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < segments.Length)
        {
            UiRouteSegment segment = segments[index];
            result.Append('/');
            if (ValueComparer.TextEquals(segment.Kind, LiteralSegment))
            {
                result.Append(UrlCore.EncodeRouteComponent(segment.Value));
            }
            else
            {
                result.Append('{');
                if (ValueComparer.TextEquals(segment.Kind, CatchAllSegment))
                {
                    result.Append('*');
                }

                result.Append(segment.Value);
                if (segment.Constraint != null)
                {
                    result.Append(':');
                    result.Append(segment.Constraint);
                }

                if (segment.DefaultValue != null)
                {
                    result.Append('=');
                    result.Append(UrlCore.EncodeRouteComponent(segment.DefaultValue));
                }
                else if (segment.IsOptional && !ValueComparer.TextEquals(segment.Kind, CatchAllSegment))
                {
                    result.Append('?');
                }

                result.Append('}');
            }

            index++;
        }

        return result.ToString();
    }

    internal string JoinCatchAllRouteValue(string[] segments, int start)
    {
        StringBuilder result = new StringBuilder();
        int index = start;
        while (index < segments.Length)
        {
            if (index > start)
            {
                result.Append('/');
            }

            result.Append(segments[index]);
            index++;
        }

        return result.ToString();
    }

    internal UiValueEntry[] CopyRouteQueryEntries(UiValueEntry[] source)
    {
        UiValueEntry[] result = new UiValueEntry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            UiValueEntry entry = new UiValueEntry();
            entry.Key = source[index].Key;
            entry.Value = CopyRouteValue(source[index].Value);
            result[index] = entry;
            index++;
        }

        return result;
    }

    internal UiValue CopyRouteValue(UiValue source)
    {
        UiValue result = new UiValue();
        result.Kind = source.Kind;
        result.BooleanValue = source.BooleanValue;
        result.IntegerValue = source.IntegerValue;
        result.FloatingPointValue = source.FloatingPointValue;
        result.DecimalValue = source.DecimalValue;
        result.StringValue = source.StringValue;
        result.DateTimeValue = source.DateTimeValue;
        if (source.ArrayValue != null)
        {
            result.ArrayValue = new UiValue[source.ArrayValue.Length];
            int index = 0;
            while (index < source.ArrayValue.Length)
            {
                result.ArrayValue[index] = CopyRouteValue(source.ArrayValue[index]);
                index++;
            }
        }

        return result;
    }

    internal UiRoute CopyParsedRoute(UiRoute source, RouteTemplateResult parsed)
    {
        UiRoute result = new UiRoute();
        result.Id = source.Id;
        result.Template = parsed.CanonicalTemplate;
        result.Segments = CopyRouteTemplateSegments(parsed.Segments);
        result.ParentId = source.ParentId;
        result.View = source.View;
        result.OutletName = source.OutletName;
        result.RedirectUrl = source.RedirectUrl;
        result.IsIndex = source.IsIndex;
        result.IsNotFound = source.IsNotFound;
        return result;
    }

    internal UiRouteSegment[] CopyRouteTemplateSegments(UiRouteSegment[] source)
    {
        UiRouteSegment[] result = new UiRouteSegment[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            UiRouteSegment segment = new UiRouteSegment();
            segment.Kind = source[index].Kind;
            segment.Value = source[index].Value;
            segment.Constraint = source[index].Constraint;
            segment.DefaultValue = source[index].DefaultValue;
            segment.IsOptional = source[index].IsOptional;
            result[index] = segment;
            index++;
        }

        return result;
    }

    internal int FindRouteId(List<UiRoute> routes, string routeId)
    {
        int index = 0;
        while (index < routes.Count)
        {
            if (ValueComparer.TextEquals(routes[index].Id, routeId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void InsertRouteByPrecedence(List<UiRoute> routes, UiRoute route)
    {
        int index = routes.Count;
        routes.Add(route);
        while (index > 0 && CompareRoutePrecedence(route, routes[index - 1]) < 0)
        {
            routes[index] = routes[index - 1];
            index--;
        }

        routes[index] = route;
    }

    internal RouteTemplateResult CreateSingleRouteTemplateSuccess(UiRouteSegment segment)
    {
        RouteTemplateResult result = new RouteTemplateResult();
        result.Success = true;
        result.Segments = new UiRouteSegment[] { segment };
        return result;
    }

    internal RouteTemplateResult CreateRouteTemplateFailure(string code)
    {
        return new RouteTemplateResult { Code = code };
    }

    internal RouteMatchResult CreateRouteNotMatched()
    {
        return new RouteMatchResult { Success = true };
    }

    internal RouteMatchResult CreateRouteMatchFailure(string code)
    {
        return new RouteMatchResult { Code = code };
    }

    internal RouteCollectionResult CreateRouteCollectionFailure(string code, string firstRouteId, string secondRouteId)
    {
        RouteCollectionResult result = new RouteCollectionResult();
        result.Code = code;
        result.FirstRouteId = firstRouteId;
        result.SecondRouteId = secondRouteId;
        return result;
    }
}
