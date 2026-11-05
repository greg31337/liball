using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpOpusCodecCore
{
    internal static bool IsOpusEncoding(string encodingName)
    {
        return string.Equals(encodingName, SdpCodecNames.Opus, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool AreCodecParametersValid(SdpCodec codec)
    {
        if (SdpCodecServiceCore.HasUnparsedFormatParameters(codec))
        {
            return false;
        }
        List<SdpFormatParameter> parameters = SdpCodecServiceCore.CollectCodecParameters(codec);
        if (!AreParameterValuesValid(parameters))
        {
            return false;
        }
        if (!IsPacketTimeAttributeValid(codec.PacketTime) || !IsPacketTimeAttributeValid(codec.MaximumPacketTime))
        {
            return false;
        }
        if (codec.PacketTime != null && codec.MaximumPacketTime != null && codec.PacketTime.NumericValue > codec.MaximumPacketTime.NumericValue)
        {
            return false;
        }
        return true;
    }

    internal static bool AreCapabilityParametersValid(SdpCodecCapability capability)
    {
        return AreParameterValuesValid(capability.FormatParameters);
    }

    internal static bool AreParameterValuesValid(List<SdpFormatParameter> parameters)
    {
        List<string> recognizedNames = new List<string>();
        int parameterIndex = 0;
        while (parameterIndex < parameters.Count)
        {
            SdpFormatParameter parameter = parameters[parameterIndex];
            if (parameter == null || ReadParameterValidationError(parameter).Length > 0)
            {
                return false;
            }
            if (IsRecognizedParameter(parameter.Name))
            {
                if (ContainsParameterName(recognizedNames, parameter.Name))
                {
                    return false;
                }
                recognizedNames.Add(parameter.Name);
            }
            parameterIndex = parameterIndex + 1;
        }
        return true;
    }

    internal static string ReadParameterValidationError(SdpFormatParameter parameter)
    {
        if (!IsRecognizedParameter(parameter.Name))
        {
            return string.Empty;
        }
        if (!parameter.HasValue)
        {
            return "An Opus format parameter requires an equals-delimited value.";
        }
        if (IsBooleanParameter(parameter.Name) && parameter.Value != "0" && parameter.Value != "1")
        {
            return "An Opus boolean format parameter must be zero or one.";
        }
        if ((NameEquals(parameter.Name, "maxplaybackrate") || NameEquals(parameter.Name, "sprop-maxcapturerate")) && !IsIntegerInRange(parameter.Value, 8000, 48000))
        {
            return "An Opus playback or capture rate must be an integer from 8000 through 48000.";
        }
        if (NameEquals(parameter.Name, "maxaveragebitrate") && !IsPositiveInteger(parameter.Value))
        {
            return "The Opus maximum average bitrate must be a positive integer.";
        }
        if (NameEquals(parameter.Name, "minptime") && !IsPacketTimeTextValid(parameter.Value))
        {
            return "The Opus minimum packet time must be a supported integer duration from 3 through 120 milliseconds.";
        }
        return string.Empty;
    }

    internal static bool IsRecognizedParameter(string name)
    {
        return NameEquals(name, "maxplaybackrate")
            || NameEquals(name, "sprop-maxcapturerate")
            || NameEquals(name, "maxaveragebitrate")
            || NameEquals(name, "stereo")
            || NameEquals(name, "sprop-stereo")
            || NameEquals(name, "cbr")
            || NameEquals(name, "useinbandfec")
            || NameEquals(name, "usedtx")
            || NameEquals(name, "minptime");
    }

    internal static bool IsBooleanParameter(string name)
    {
        return NameEquals(name, "stereo")
            || NameEquals(name, "sprop-stereo")
            || NameEquals(name, "cbr")
            || NameEquals(name, "useinbandfec")
            || NameEquals(name, "usedtx");
    }

    internal static bool IsPacketTimeAttributeValid(SdpCommonAttribute? packetTime)
    {
        if (packetTime == null)
        {
            return true;
        }
        double value = packetTime.NumericValue;
        if (!packetTime.HasNumericValue || double.IsNaN(value) || double.IsInfinity(value) || value < 3 || value > 120 || value != (double)(int)value)
        {
            return false;
        }
        int integerValue = (int)value;
        return integerValue % 5 == 0 || integerValue % 5 == 3;
    }

    internal static bool IsPacketTimeTextValid(string value)
    {
        if (!SdpCommonAttributeCodecCore.IsPositiveIntegerValue(value))
        {
            return false;
        }
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan());
        if (!number.Success || number.UnsignedValue < 3 || number.UnsignedValue > 120)
        {
            return false;
        }
        return number.UnsignedValue % 5 == 0 || number.UnsignedValue % 5 == 3;
    }

    internal static bool IsIntegerInRange(string value, int minimum, int maximum)
    {
        if (!SdpCommonAttributeCodecCore.IsPositiveIntegerValue(value))
        {
            return false;
        }
        SdpNumberParseResult number = SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan());
        return number.Success && number.UnsignedValue >= (ulong)minimum && number.UnsignedValue <= (ulong)maximum;
    }

    internal static bool IsPositiveInteger(string value)
    {
        if (!SdpCommonAttributeCodecCore.IsPositiveIntegerValue(value))
        {
            return false;
        }
        return SdpNumberParser.ParseCharacterUnsignedDecimal(value.AsSpan()).Success;
    }

    internal static bool ContainsParameterName(List<string> names, string name)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (NameEquals(names[index], name))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool NameEquals(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }
}
