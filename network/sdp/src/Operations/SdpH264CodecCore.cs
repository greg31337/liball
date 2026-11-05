using System;
using System.Collections.Generic;

namespace sdp;

internal static class SdpH264CodecCore
{
    internal const string DefaultProfileLevelId = "42000A";

    internal static bool IsH264Encoding(string encodingName)
    {
        return string.Equals(encodingName, SdpCodecNames.H264, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool AreCodecParametersValid(SdpCodec codec)
    {
        if (SdpCodecServiceCore.HasUnparsedFormatParameters(codec))
        {
            return false;
        }
        return AreParameterValuesValid(SdpCodecServiceCore.CollectCodecParameters(codec));
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
                if (SdpOpusCodecCore.ContainsParameterName(recognizedNames, parameter.Name))
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
            return "An H.264 format parameter requires an equals-delimited value.";
        }
        if (NameEquals(parameter.Name, "packetization-mode") && parameter.Value != "0" && parameter.Value != "1" && parameter.Value != "2")
        {
            return "The H.264 packetization mode must be zero, one, or two.";
        }
        if (NameEquals(parameter.Name, "level-asymmetry-allowed") && parameter.Value != "0" && parameter.Value != "1")
        {
            return "The H.264 level-asymmetry-allowed value must be zero or one.";
        }
        if (NameEquals(parameter.Name, "profile-level-id") && !IsProfileLevelIdValid(parameter.Value))
        {
            return "The H.264 profile-level-id must contain exactly three hexadecimal octets and a nonzero level.";
        }
        return string.Empty;
    }

    internal static bool IsRecognizedParameter(string name)
    {
        return NameEquals(name, "packetization-mode")
            || NameEquals(name, "profile-level-id")
            || NameEquals(name, "level-asymmetry-allowed");
    }

    internal static bool AreCompatible(List<SdpFormatParameter> offered, List<SdpFormatParameter> supported)
    {
        if (!AreParameterValuesValid(offered) || !AreParameterValuesValid(supported))
        {
            return false;
        }
        if (ReadIntegerParameter(offered, "packetization-mode", 0) != ReadIntegerParameter(supported, "packetization-mode", 0))
        {
            return false;
        }
        string offeredProfileLevel = ReadStringParameter(offered, "profile-level-id", DefaultProfileLevelId);
        string supportedProfileLevel = ReadStringParameter(supported, "profile-level-id", DefaultProfileLevelId);
        if (!ProfilesMatch(offeredProfileLevel, supportedProfileLevel))
        {
            return false;
        }
        return true;
    }

    internal static bool IsProfileLevelIdValid(string value)
    {
        if (value.Length != 6)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (ReadHexDigit(value[index]) < 0)
            {
                return false;
            }
            index = index + 1;
        }
        return ReadHexByte(value, 0) > 0 && ReadHexByte(value, 4) > 0;
    }

    internal static bool ProfilesMatch(string first, string second)
    {
        int firstProfile = ReadHexByte(first, 0);
        int firstConstraints = ReadHexByte(first, 2);
        int secondProfile = ReadHexByte(second, 0);
        int secondConstraints = ReadHexByte(second, 2);
        if (firstProfile == secondProfile && firstConstraints == secondConstraints)
        {
            return true;
        }
        int firstClass = ReadProfileClass(firstProfile, firstConstraints);
        int secondClass = ReadProfileClass(secondProfile, secondConstraints);
        return firstClass > 0 && firstClass == secondClass;
    }

    internal static int ReadProfileClass(int profile, int constraints)
    {
        if (profile == 0x42 && (constraints & 0x4F) == 0x40
            || profile == 0x4D && (constraints & 0x8F) == 0x80
            || profile == 0x58 && (constraints & 0xCF) == 0xC0)
        {
            return 1;
        }
        if (profile == 0x42 && (constraints & 0x4F) == 0
            || profile == 0x58 && (constraints & 0xCF) == 0x80)
        {
            return 2;
        }
        if (profile == 0x4D && (constraints & 0xAF) == 0)
        {
            return 3;
        }
        if (profile == 0x58 && (constraints & 0xCF) == 0)
        {
            return 4;
        }
        if (profile == 0x64 && constraints == 0)
        {
            return 5;
        }
        if (profile == 0x6E && constraints == 0)
        {
            return 6;
        }
        if (profile == 0x7A && constraints == 0)
        {
            return 7;
        }
        if (profile == 0xF4 && constraints == 0)
        {
            return 8;
        }
        if (profile == 0x6E && constraints == 0x10)
        {
            return 9;
        }
        if (profile == 0x7A && constraints == 0x10)
        {
            return 10;
        }
        if (profile == 0xF4 && constraints == 0x10)
        {
            return 11;
        }
        if (profile == 0x2C && constraints == 0x10)
        {
            return 12;
        }
        return 0;
    }

    internal static int ReadIntegerParameter(List<SdpFormatParameter> parameters, string name, int defaultValue)
    {
        int index = 0;
        while (index < parameters.Count)
        {
            SdpFormatParameter parameter = parameters[index];
            if (NameEquals(parameter.Name, name))
            {
                return parameter.Value[0] - '0';
            }
            index = index + 1;
        }
        return defaultValue;
    }

    internal static string ReadStringParameter(List<SdpFormatParameter> parameters, string name, string defaultValue)
    {
        int index = 0;
        while (index < parameters.Count)
        {
            SdpFormatParameter parameter = parameters[index];
            if (NameEquals(parameter.Name, name))
            {
                return parameter.Value;
            }
            index = index + 1;
        }
        return defaultValue;
    }

    internal static int ReadHexByte(string value, int index)
    {
        return ReadHexDigit(value[index]) * 16 + ReadHexDigit(value[index + 1]);
    }

    internal static int ReadHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        return -1;
    }

    internal static bool NameEquals(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }
}
