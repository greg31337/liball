using System;

namespace sdp;

internal static class SdpImageAttributeCodecCore
{
    internal static SdpAttributeDecodeResult DecodeImageAttribute(SdpAttribute attribute, SdpAttributeDecodeResult result)
    {
        result.Recognized = true;
        if (attribute.Kind != SdpAttributeKind.Value)
        {
            return FailImageDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP imageattr attribute requires a colon-delimited value.");
        }
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(attribute.Value))
        {
            return FailImageDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP imageattr value must contain a payload selector and directional constraints.");
        }

        string[] tokens = ReadImageTokens(attribute.Value);
        if (tokens.Length == 0)
        {
            return FailImageDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP imageattr value requires an asterisk or canonical payload type followed by directional constraints.");
        }
        if (!IsValidImagePayload(tokens[0]))
        {
            return FailImageDecode(attribute, result, SdpDiagnosticCodes.ValidationPayloadType, "The SDP imageattr payload selector must be an asterisk or a canonical integer from zero through 127.");
        }
        if (tokens.Length < 3)
        {
            return FailImageDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP imageattr payload selector must be followed by directional constraints.");
        }

        SdpImageAttribute projection = new SdpImageAttribute();
        projection.SourceAttribute = attribute;
        projection.PayloadType = tokens[0];
        int index = 1;
        int directionCount = 0;
        while (index < tokens.Length)
        {
            string direction = tokens[index];
            if (direction != "send" && direction != "recv")
            {
                return FailImageDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "Every SDP imageattr constraint list must begin with send or recv.");
            }
            int constraintsStart = index + 1;
            int constraintsEnd = constraintsStart;
            while (constraintsEnd < tokens.Length && tokens[constraintsEnd] != "send" && tokens[constraintsEnd] != "recv")
            {
                constraintsEnd = constraintsEnd + 1;
            }
            if (constraintsEnd == constraintsStart)
            {
                return FailImageDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "Every SDP imageattr direction requires a non-empty constraint expression.");
            }
            string constraints = JoinImageTokens(tokens, constraintsStart, constraintsEnd);
            string constraintError = ReadImageConstraintError(constraints);
            if (constraintError.Length > 0)
            {
                return FailImageDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, constraintError);
            }
            if (direction == "send")
            {
                if (projection.HasSendConstraints)
                {
                    return FailImageDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP imageattr value must not repeat the send direction.");
                }
                projection.HasSendConstraints = true;
                projection.SendConstraints = constraints;
            }
            else
            {
                if (projection.HasReceiveConstraints)
                {
                    return FailImageDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP imageattr value must not repeat the recv direction.");
                }
                projection.HasReceiveConstraints = true;
                projection.ReceiveConstraints = constraints;
            }
            directionCount = directionCount + 1;
            if (directionCount > 2)
            {
                return FailImageDecode(attribute, result, SdpDiagnosticCodes.ValidationAttribute, "The SDP imageattr value may contain at most one send and one recv constraint list.");
            }
            index = constraintsEnd;
        }

        result.ImageAttribute = projection;
        result.TypedAttribute = projection;
        return result;
    }

    internal static SdpAttributeEncodeResult EncodeImageAttribute(SdpImageAttribute projection)
    {
        SdpAttributeEncodeResult result = new SdpAttributeEncodeResult();
        result.TypedAttribute = projection;
        if (!IsValidImagePayload(projection.PayloadType))
        {
            result.Diagnostics.Add(CreateImageEncodeDiagnostic(SdpDiagnosticCodes.ValidationPayloadType, "The SDP imageattr payload selector must be an asterisk or a canonical integer from zero through 127."));
            return result;
        }
        if (!projection.HasSendConstraints && projection.SendConstraints.Length > 0 || !projection.HasReceiveConstraints && projection.ReceiveConstraints.Length > 0)
        {
            result.Diagnostics.Add(CreateImageEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "SDP imageattr constraint text requires its corresponding direction to be enabled."));
            return result;
        }
        if (!projection.HasSendConstraints && !projection.HasReceiveConstraints)
        {
            result.Diagnostics.Add(CreateImageEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, "The SDP imageattr value requires send or recv constraints."));
            return result;
        }

        string value = projection.PayloadType;
        if (projection.HasSendConstraints)
        {
            string sendError = ReadImageConstraintError(projection.SendConstraints);
            if (sendError.Length > 0)
            {
                result.Diagnostics.Add(CreateImageEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, sendError));
                return result;
            }
            value = value + " send " + NormalizeImageConstraints(projection.SendConstraints);
        }
        if (projection.HasReceiveConstraints)
        {
            string receiveError = ReadImageConstraintError(projection.ReceiveConstraints);
            if (receiveError.Length > 0)
            {
                result.Diagnostics.Add(CreateImageEncodeDiagnostic(SdpDiagnosticCodes.ValidationAttribute, receiveError));
                return result;
            }
            value = value + " recv " + NormalizeImageConstraints(projection.ReceiveConstraints);
        }

        result.Attribute = CreateImageValueAttribute(value);
        result.Success = true;
        return result;
    }

    internal static bool IsValidImagePayload(string value)
    {
        if (value == "*")
        {
            return true;
        }
        if (!SdpCommonAttributeCodecCore.IsZeroBasedIntegerValue(value))
        {
            return false;
        }
        SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(value.AsSpan());
        return payload.Success;
    }

    internal static string ReadImageConstraintError(string value)
    {
        if (!SdpCommonAttributeCodecCore.IsByteStringValue(value))
        {
            return "An SDP imageattr constraint expression must not be empty or contain prohibited line-ending characters.";
        }
        string[] constraints = ReadImageTokens(value);
        if (constraints.Length == 1 && constraints[0] == "*")
        {
            return string.Empty;
        }
        int index = 0;
        while (index < constraints.Length)
        {
            string constraint = constraints[index];
            string setError = ReadImageSetError(constraint);
            if (setError.Length > 0)
            {
                return setError;
            }
            index = index + 1;
        }
        return constraints.Length > 0 ? string.Empty : "An SDP imageattr constraint expression must not be empty.";
    }

    internal static string ReadImageSetError(string value)
    {
        if (!IsBalancedImageSet(value))
        {
            return "Every SDP imageattr constraint must be an asterisk or a balanced bracketed set.";
        }
        string content = value.Substring(1, value.Length - 2);
        int componentStart = 0;
        int componentIndex = 0;
        int depth = 0;
        bool hasSar = false;
        bool hasPar = false;
        bool hasQuality = false;
        int index = 0;
        while (index <= content.Length)
        {
            bool atEnd = index == content.Length;
            char character = atEnd ? '\0' : content[index];
            if (!atEnd && character == '[')
            {
                depth = depth + 1;
            }
            else if (!atEnd && character == ']')
            {
                depth = depth - 1;
            }
            if (atEnd || character == ',' && depth == 0)
            {
                if (index == componentStart)
                {
                    return "An SDP imageattr set must not contain an empty key-value component.";
                }
                string component = content.Substring(componentStart, index - componentStart);
                int equalsIndex = component.IndexOf('=');
                if (equalsIndex <= 0 || equalsIndex + 1 >= component.Length)
                {
                    return "Every SDP imageattr set component requires a token name and non-empty value.";
                }
                string name = component.Substring(0, equalsIndex);
                string componentValue = component.Substring(equalsIndex + 1);
                if (!SdpCommonAttributeCodecCore.IsTokenValue(name))
                {
                    return "Every SDP imageattr set component name must use token syntax.";
                }
                if (componentIndex == 0 && name != "x" || componentIndex == 1 && name != "y")
                {
                    return "Every SDP imageattr set must begin with x and y constraints in that order.";
                }
                if (name == "x" || name == "y")
                {
                    if (componentIndex > 1 || !IsValidImageDimensionRange(componentValue))
                    {
                        return "SDP imageattr x and y values must be positive dimensions no greater than six decimal digits with valid ascending ranges.";
                    }
                }
                else if (name == "sar")
                {
                    if (hasSar || !IsValidImageSampleRange(componentValue))
                    {
                        return "An SDP imageattr set may contain one valid sar constraint.";
                    }
                    hasSar = true;
                }
                else if (name == "par")
                {
                    if (hasPar || !IsValidImagePictureRange(componentValue))
                    {
                        return "An SDP imageattr set may contain one valid ascending par range.";
                    }
                    hasPar = true;
                }
                else if (name == "q")
                {
                    if (hasQuality || !IsValidImageQuality(componentValue))
                    {
                        return "An SDP imageattr set may contain one q value from 0.0 through 1.00.";
                    }
                    hasQuality = true;
                }
                else if (!SdpCommonAttributeCodecCore.IsNonWhitespaceValue(componentValue))
                {
                    return "An extension SDP imageattr set value must be non-whitespace text.";
                }
                componentIndex = componentIndex + 1;
                componentStart = index + 1;
            }
            index = index + 1;
        }
        if (componentIndex < 2)
        {
            return "Every SDP imageattr set requires x and y constraints.";
        }
        return string.Empty;
    }

    internal static bool IsValidImageDimensionRange(string value)
    {
        if (IsValidImageDimension(value))
        {
            return true;
        }
        if (value.Length < 3 || value[0] != '[' || value[value.Length - 1] != ']')
        {
            return false;
        }
        string inner = value.Substring(1, value.Length - 2);
        if (inner.IndexOf(':') >= 0)
        {
            string[] range = inner.Split(':');
            if (range.Length != 2 && range.Length != 3)
            {
                return false;
            }
            if (!IsValidImageDimension(range[0]) || !IsValidImageDimension(range[range.Length - 1]))
            {
                return false;
            }
            if (range.Length == 3 && !IsValidImageDimension(range[1]))
            {
                return false;
            }
            int lower = ReadImageDimension(range[0]);
            int upper = ReadImageDimension(range[range.Length - 1]);
            return upper > lower;
        }
        string[] values = inner.Split(',');
        if (values.Length < 2)
        {
            return false;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!IsValidImageDimension(values[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsValidImageDimension(string value)
    {
        if (value.Length < 1 || value.Length > 6 || value[0] < '1' || value[0] > '9')
        {
            return false;
        }
        int index = 1;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static int ReadImageDimension(string value)
    {
        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            result = result * 10 + value[index] - '0';
            index = index + 1;
        }
        return result;
    }

    internal static bool IsValidImageSampleRange(string value)
    {
        if (IsValidImageSpatialValue(value))
        {
            return true;
        }
        if (value.Length < 3 || value[0] != '[' || value[value.Length - 1] != ']')
        {
            return false;
        }
        string inner = value.Substring(1, value.Length - 2);
        int dashIndex = inner.IndexOf('-');
        if (dashIndex >= 0)
        {
            if (inner.IndexOf('-', dashIndex + 1) >= 0)
            {
                return false;
            }
            string lower = inner.Substring(0, dashIndex);
            string upper = inner.Substring(dashIndex + 1);
            return IsValidImageSpatialValue(lower) && IsValidImageSpatialValue(upper) && ReadImageSpatialValue(upper) > ReadImageSpatialValue(lower);
        }
        string[] values = inner.Split(',');
        if (values.Length < 2)
        {
            return false;
        }
        double previous = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (!IsValidImageSpatialValue(values[index]))
            {
                return false;
            }
            double current = ReadImageSpatialValue(values[index]);
            if (index > 0 && current <= previous)
            {
                return false;
            }
            previous = current;
            index = index + 1;
        }
        return true;
    }

    internal static bool IsValidImagePictureRange(string value)
    {
        if (value.Length < 5 || value[0] != '[' || value[value.Length - 1] != ']')
        {
            return false;
        }
        string inner = value.Substring(1, value.Length - 2);
        int dashIndex = inner.IndexOf('-');
        if (dashIndex <= 0 || dashIndex + 1 >= inner.Length || inner.IndexOf('-', dashIndex + 1) >= 0)
        {
            return false;
        }
        string lower = inner.Substring(0, dashIndex);
        string upper = inner.Substring(dashIndex + 1);
        return IsValidImageSpatialValue(lower) && IsValidImageSpatialValue(upper) && ReadImageSpatialValue(upper) > ReadImageSpatialValue(lower);
    }

    internal static bool IsValidImageSpatialValue(string value)
    {
        int dotIndex = value.IndexOf('.');
        if (dotIndex != 1 || value.Length < 3 || value.Length > 6)
        {
            return false;
        }
        if (value[0] == '0')
        {
            if (value[2] < '1' || value[2] > '9')
            {
                return false;
            }
        }
        else if (value[0] < '1' || value[0] > '9')
        {
            return false;
        }
        int index = 2;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static double ReadImageSpatialValue(string value)
    {
        double result = value[0] - '0';
        double scale = 0.1;
        int index = 2;
        while (index < value.Length)
        {
            result = result + (value[index] - '0') * scale;
            scale = scale / 10;
            index = index + 1;
        }
        return result;
    }

    internal static bool IsValidImageQuality(string value)
    {
        if (value.Length < 3 || value.Length > 4 || value[1] != '.')
        {
            return false;
        }
        if (value[0] != '0' && value[0] != '1')
        {
            return false;
        }
        int index = 2;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9' || value[0] == '1' && value[index] != '0')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static bool IsBalancedImageSet(string value)
    {
        if (value.Length < 2 || value[0] != '[' || value[value.Length - 1] != ']')
        {
            return false;
        }
        int depth = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!SdpCharacterClasses.IsNonWhitespaceCharacter(character))
            {
                return false;
            }
            if (character == '[')
            {
                depth = depth + 1;
            }
            else if (character == ']')
            {
                depth = depth - 1;
                if (depth == 0 && index != value.Length - 1 || depth < 0)
                {
                    return false;
                }
            }
            index = index + 1;
        }
        return depth == 0;
    }

    internal static string NormalizeImageConstraints(string value)
    {
        string[] tokens = ReadImageTokens(value);
        return JoinImageTokens(tokens, 0, tokens.Length);
    }

    internal static string JoinImageTokens(string[] tokens, int start, int end)
    {
        string value = string.Empty;
        int index = start;
        while (index < end)
        {
            if (index > start)
            {
                value = value + " ";
            }
            value = value + tokens[index];
            index = index + 1;
        }
        return value;
    }

    internal static string[] ReadImageTokens(string value)
    {
        return value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    internal static SdpAttribute CreateImageValueAttribute(string value)
    {
        SdpAttribute attribute = new SdpAttribute();
        attribute.Kind = SdpAttributeKind.Value;
        attribute.Name = SdpAttributeNames.ImageAttribute;
        attribute.Value = value;
        attribute.RawText = SdpAttributeNames.ImageAttribute + ":" + value;
        return attribute;
    }

    internal static SdpAttributeDecodeResult FailImageDecode(SdpAttribute attribute, SdpAttributeDecodeResult result, string code, string detail)
    {
        return SdpAttributeDecoderCore.FailDecode(attribute, result, code, detail);
    }

    internal static SdpDiagnostic CreateImageEncodeDiagnostic(string code, string detail)
    {
        SdpDiagnostic diagnostic = SdpAttributeEncoderCore.CreateEncodeDiagnostic(SdpAttributeNames.ImageAttribute, detail);
        diagnostic.Code = code;
        return diagnostic;
    }
}
