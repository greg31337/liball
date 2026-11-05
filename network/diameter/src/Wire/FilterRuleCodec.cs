using System;
using System.Collections.Generic;
using System.Net;

namespace diameter;

internal class FilterRuleParseState
{
    internal string Value = string.Empty;
    internal List<string> Tokens = new List<string>();
    internal List<int> Offsets = new List<int>();
    internal int Protocol;
    internal bool HasPorts;
    internal int OptionIndex;
    internal int ErrorOffset;
}

internal static class FilterRuleCodec
{
    internal static DiameterValueResult ValidateIpFilterRule(string? value)
    {
        FilterRuleParseState state = CreateFilterRuleState(value);
        if (state.ErrorOffset >= 0)
        {
            return CreateFilterFailure(false, state, state.ErrorOffset);
        }

        if (!ParseCommonFilterRule(state, false))
        {
            return CreateFilterFailure(false, state, state.ErrorOffset);
        }

        if (!ParseIpFilterOptions(state))
        {
            return CreateFilterFailure(false, state, state.ErrorOffset);
        }

        return CreateFilterSuccess(state.Value);
    }

    internal static DiameterValueResult ValidateQosFilterRule(string? value)
    {
        FilterRuleParseState state = CreateFilterRuleState(value);
        if (state.ErrorOffset >= 0)
        {
            return CreateFilterFailure(true, state, state.ErrorOffset);
        }

        if (!ParseCommonFilterRule(state, true))
        {
            return CreateFilterFailure(true, state, state.ErrorOffset);
        }

        if (!ParseQosFilterOptions(state))
        {
            return CreateFilterFailure(true, state, state.ErrorOffset);
        }

        return CreateFilterSuccess(state.Value);
    }

    internal static FilterRuleParseState CreateFilterRuleState(string? value)
    {
        FilterRuleParseState state = new FilterRuleParseState();
        state.ErrorOffset = -1;
        if (value == null || value.Length == 0)
        {
            state.ErrorOffset = 0;
            return state;
        }

        state.Value = value;
        if (value[0] == ' ' || value[value.Length - 1] == ' ')
        {
            state.ErrorOffset = 0;
            if (value[value.Length - 1] == ' ')
            {
                state.ErrorOffset = value.Length - 1;
            }

            return state;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 32 || character > 126)
            {
                state.ErrorOffset = index;
                return state;
            }

            if (character == ' ')
            {
                index = index + 1;
                continue;
            }

            int start = index;
            while (index < value.Length && value[index] != ' ')
            {
                character = value[index];
                if (character < 33 || character > 126)
                {
                    state.ErrorOffset = index;
                    return state;
                }

                index = index + 1;
            }

            state.Tokens.Add(value.Substring(start, index - start));
            state.Offsets.Add(start);
        }

        if (state.Tokens.Count == 0)
        {
            state.ErrorOffset = 0;
        }

        return state;
    }

    internal static bool ParseCommonFilterRule(FilterRuleParseState state, bool qos)
    {
        if (state.Tokens.Count < 7)
        {
            state.ErrorOffset = state.Value.Length;
            return false;
        }

        string action = state.Tokens[0];
        if (qos)
        {
            if (action != "tag" && action != "meter")
            {
                state.ErrorOffset = state.Offsets[0];
                return false;
            }
        }
        else if (action != "permit" && action != "deny")
        {
            state.ErrorOffset = state.Offsets[0];
            return false;
        }

        if (state.Tokens[1] != "in" && state.Tokens[1] != "out")
        {
            state.ErrorOffset = state.Offsets[1];
            return false;
        }

        string protocol = state.Tokens[2];
        if (protocol == "ip")
        {
            state.Protocol = -1;
        }
        else
        {
            state.Protocol = ParseDecimalRange(protocol, 255);
            if (state.Protocol < 0)
            {
                state.ErrorOffset = state.Offsets[2];
                return false;
            }
        }

        if (state.Tokens[3] != "from")
        {
            state.ErrorOffset = state.Offsets[3];
            return false;
        }

        if (!ValidateAddressSpecification(state.Tokens[4]))
        {
            state.ErrorOffset = state.Offsets[4];
            return false;
        }

        int index = 5;
        if (state.Tokens[index] != "to")
        {
            if (!ValidatePortSpecification(state.Tokens[index]))
            {
                state.ErrorOffset = state.Offsets[index];
                return false;
            }

            state.HasPorts = true;
            index = index + 1;
            if (index >= state.Tokens.Count || state.Tokens[index] != "to")
            {
                state.ErrorOffset = FindFilterOffset(state, index);
                return false;
            }
        }

        index = index + 1;
        if (index >= state.Tokens.Count || !ValidateAddressSpecification(state.Tokens[index]))
        {
            state.ErrorOffset = FindFilterOffset(state, index);
            return false;
        }

        index = index + 1;
        if (index < state.Tokens.Count)
        {
            bool option = IsIpFilterOption(state.Tokens[index]);
            if (qos)
            {
                option = IsQosFilterOption(state.Tokens[index]);
            }

            if (!option)
            {
                if (!ValidatePortSpecification(state.Tokens[index]))
                {
                    state.ErrorOffset = state.Offsets[index];
                    return false;
                }

                state.HasPorts = true;
                index = index + 1;
            }
        }

        if (state.HasPorts &&
            state.Protocol != 6 &&
            state.Protocol != 17 &&
            state.Protocol != 132)
        {
            state.ErrorOffset = state.Offsets[2];
            return false;
        }

        state.OptionIndex = index;
        return true;
    }

    internal static bool ParseIpFilterOptions(FilterRuleParseState state)
    {
        bool hasFrag = false;
        bool hasTcpFlags = false;
        int index = state.OptionIndex;
        while (index < state.Tokens.Count)
        {
            string option = state.Tokens[index];
            if (option == "frag")
            {
                if (hasFrag)
                {
                    state.ErrorOffset = state.Offsets[index];
                    return false;
                }

                hasFrag = true;
                index = index + 1;
                continue;
            }

            if (option == "established" || option == "setup")
            {
                if (state.Protocol != 6)
                {
                    state.ErrorOffset = state.Offsets[index];
                    return false;
                }

                index = index + 1;
                continue;
            }

            if (option == "ipoptions" || option == "tcpoptions" || option == "tcpflags")
            {
                if (index + 1 >= state.Tokens.Count)
                {
                    state.ErrorOffset = state.Value.Length;
                    return false;
                }

                if (option != "ipoptions" && state.Protocol != 6)
                {
                    state.ErrorOffset = state.Offsets[index];
                    return false;
                }

                if (!ValidateNamedOptionList(state.Tokens[index + 1], option))
                {
                    state.ErrorOffset = state.Offsets[index + 1];
                    return false;
                }

                if (option == "tcpflags")
                {
                    hasTcpFlags = true;
                }

                index = index + 2;
                continue;
            }

            if (option == "icmptypes")
            {
                if (state.Protocol != 1 && state.Protocol != 58)
                {
                    state.ErrorOffset = state.Offsets[index];
                    return false;
                }

                if (index + 1 >= state.Tokens.Count)
                {
                    state.ErrorOffset = FindFilterOffset(state, index + 1);
                    return false;
                }

                int specificationIndex = index + 1;
                int nextOptionIndex = specificationIndex + 1;
                while (nextOptionIndex < state.Tokens.Count &&
                    !IsIpFilterOption(state.Tokens[nextOptionIndex]))
                {
                    nextOptionIndex = nextOptionIndex + 1;
                }

                int specificationStart = state.Offsets[specificationIndex];
                int specificationEnd = state.Value.Length;
                if (nextOptionIndex < state.Tokens.Count)
                {
                    specificationEnd = state.Offsets[nextOptionIndex];
                }

                while (specificationEnd > specificationStart &&
                    state.Value[specificationEnd - 1] == ' ')
                {
                    specificationEnd = specificationEnd - 1;
                }

                string specification = state.Value.Substring(
                    specificationStart,
                    specificationEnd - specificationStart);
                if (!ValidateIcmpTypeList(specification))
                {
                    state.ErrorOffset = specificationStart;
                    return false;
                }

                index = nextOptionIndex;
                continue;
            }

            state.ErrorOffset = state.Offsets[index];
            return false;
        }

        if (hasFrag && (state.HasPorts || hasTcpFlags))
        {
            state.ErrorOffset = state.Value.Length;
            return false;
        }

        return true;
    }

    internal static bool ParseQosFilterOptions(FilterRuleParseState state)
    {
        int index = state.OptionIndex;
        if (state.Tokens[0] == "tag")
        {
            if (index + 2 != state.Tokens.Count || state.Tokens[index] != "DSCP")
            {
                state.ErrorOffset = FindFilterOffset(state, index);
                return false;
            }

            if (!ValidateDscpColor(state.Tokens[index + 1], false))
            {
                state.ErrorOffset = state.Offsets[index + 1];
                return false;
            }

            return true;
        }

        if (index + 4 != state.Tokens.Count || state.Tokens[index] != "metering")
        {
            state.ErrorOffset = FindFilterOffset(state, index);
            return false;
        }

        if (!ValidatePositiveDecimal(state.Tokens[index + 1]))
        {
            state.ErrorOffset = state.Offsets[index + 1];
            return false;
        }

        if (!ValidateDscpColor(state.Tokens[index + 2], false))
        {
            state.ErrorOffset = state.Offsets[index + 2];
            return false;
        }

        if (!ValidateDscpColor(state.Tokens[index + 3], true))
        {
            state.ErrorOffset = state.Offsets[index + 3];
            return false;
        }

        return true;
    }

    internal static bool ValidateAddressSpecification(string token)
    {
        int start = 0;
        if (token.Length > 0 && token[0] == '!')
        {
            start = 1;
        }

        if (start == token.Length)
        {
            return false;
        }

        string value = token.Substring(start);
        if (value == "any" || value == "assigned")
        {
            return true;
        }

        int slash = value.IndexOf('/');
        string addressText = value;
        int prefix = -1;
        if (slash >= 0)
        {
            if (slash == 0 || slash == value.Length - 1 || value.IndexOf('/', slash + 1) >= 0)
            {
                return false;
            }

            addressText = value.Substring(0, slash);
        }

        bool ipv4 = ValidateIpv4Address(addressText);
        bool ipv6 = false;
        if (!ipv4 && addressText.IndexOf('%') < 0)
        {
            ipv6 = Uri.CheckHostName(addressText) == UriHostNameType.IPv6;
        }

        if (!ipv4 && !ipv6)
        {
            return false;
        }

        int maximumPrefix = 128;
        if (ipv4)
        {
            maximumPrefix = 32;
        }

        if (slash >= 0)
        {
            prefix = ParseDecimalRange(value.Substring(slash + 1), maximumPrefix);
            if (prefix < 0)
            {
                return false;
            }
        }

        if (prefix < 0 || prefix == maximumPrefix)
        {
            return true;
        }

        IPAddress address = IPAddress.Parse(addressText);
        byte[] bytes = address.GetAddressBytes();
        return HasClearHostBits(bytes, prefix);
    }

    internal static bool ValidateIpv4Address(string value)
    {
        int components = 0;
        int start = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == '.')
            {
                if (index == start || index - start > 3)
                {
                    return false;
                }

                int component = ParseDecimalRange(value.Substring(start, index - start), 255);
                if (component < 0)
                {
                    return false;
                }

                components = components + 1;
                start = index + 1;
            }

            index = index + 1;
        }

        return components == 4;
    }

    internal static bool HasClearHostBits(byte[] bytes, int prefix)
    {
        int fullBytes = prefix / 8;
        int remainingBits = prefix % 8;
        int index = fullBytes;
        if (remainingBits > 0)
        {
            int hostMask = (1 << (8 - remainingBits)) - 1;
            if ((bytes[index] & hostMask) != 0)
            {
                return false;
            }

            index = index + 1;
        }

        while (index < bytes.Length)
        {
            if (bytes[index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ValidatePortSpecification(string value)
    {
        int itemStart = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == ',')
            {
                if (index == itemStart)
                {
                    return false;
                }

                string item = value.Substring(itemStart, index - itemStart);
                int dash = item.IndexOf('-');
                if (dash < 0)
                {
                    if (ParseDecimalRange(item, 65535) < 0)
                    {
                        return false;
                    }
                }
                else
                {
                    if (dash == 0 || dash == item.Length - 1 || item.IndexOf('-', dash + 1) >= 0)
                    {
                        return false;
                    }

                    int first = ParseDecimalRange(item.Substring(0, dash), 65535);
                    int last = ParseDecimalRange(item.Substring(dash + 1), 65535);
                    if (first < 0 || last < first)
                    {
                        return false;
                    }
                }

                itemStart = index + 1;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ValidateNamedOptionList(string value, string option)
    {
        int itemStart = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == ',')
            {
                if (index == itemStart)
                {
                    return false;
                }

                string item = value.Substring(itemStart, index - itemStart);
                if (item.Length > 0 && item[0] == '!')
                {
                    item = item.Substring(1);
                }

                bool valid = false;
                if (option == "ipoptions")
                {
                    valid = item == "ssrr" || item == "lsrr" || item == "rr" || item == "ts";
                }
                else if (option == "tcpoptions")
                {
                    valid = item == "mss" || item == "window" || item == "sack" ||
                        item == "ts" || item == "cc";
                }
                else if (option == "tcpflags")
                {
                    valid = item == "fin" || item == "syn" || item == "rst" ||
                        item == "psh" || item == "ack" || item == "urg";
                }

                if (!valid)
                {
                    return false;
                }

                itemStart = index + 1;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ValidateIcmpTypeList(string value)
    {
        int itemStart = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == ',')
            {
                if (index == itemStart)
                {
                    return false;
                }

                string item = value.Substring(itemStart, index - itemStart).Trim();
                int dash = item.IndexOf('-');
                if (dash < 0)
                {
                    if (ParseDecimalRange(item, 255) < 0 && !ValidateIcmpTypeName(item))
                    {
                        return false;
                    }
                }
                else
                {
                    if (dash == 0 || dash == item.Length - 1 || item.IndexOf('-', dash + 1) >= 0)
                    {
                        return false;
                    }

                    int first = ParseDecimalRange(item.Substring(0, dash), 255);
                    int last = ParseDecimalRange(item.Substring(dash + 1), 255);
                    if (first < 0 || last < first)
                    {
                        return false;
                    }
                }

                itemStart = index + 1;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ValidateIcmpTypeName(string value)
    {
        return value == "echo reply" || value == "destination unreachable" ||
            value == "source quench" || value == "redirect" || value == "echo request" ||
            value == "router advertisement" || value == "router solicitation" ||
            value == "time-to-live exceeded" || value == "IP header bad" ||
            value == "timestamp request" || value == "timestamp reply" ||
            value == "information request" || value == "information reply" ||
            value == "address mask request" || value == "address mask reply" ||
            value == "echoreply" || value == "unreach" || value == "sourcequench" ||
            value == "echo" || value == "routeradvert" || value == "routersolicit" ||
            value == "timxceed" || value == "paramprob" || value == "timereq" ||
            value == "timerep" || value == "inforeq" || value == "inforep" ||
            value == "maskreq" || value == "maskrep";
    }

    internal static bool ValidateDscpColor(string value, bool allowDrop)
    {
        if (allowDrop && value == "drop")
        {
            return true;
        }

        if (value == "BE" || value == "EF")
        {
            return true;
        }

        if (value.Length == 3 && value[0] == 'C' && value[1] == 'S' &&
            value[2] >= '0' && value[2] <= '7')
        {
            return true;
        }

        if (value.Length == 4 && value[0] == 'A' && value[1] == 'F' &&
            value[2] >= '1' && value[2] <= '4' &&
            value[3] >= '1' && value[3] <= '3')
        {
            return true;
        }

        return ParseDecimalRange(value, 63) >= 0;
    }

    internal static bool ValidatePositiveDecimal(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        bool nonzero = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return false;
            }

            if (character != '0')
            {
                nonzero = true;
            }

            index = index + 1;
        }

        return nonzero;
    }

    internal static int ParseDecimalRange(string value, int maximum)
    {
        if (value.Length == 0)
        {
            return -1;
        }

        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            int digit = character - '0';
            if (result > (maximum - digit) / 10)
            {
                return -1;
            }

            result = result * 10 + digit;
            index = index + 1;
        }

        return result;
    }

    internal static bool IsIpFilterOption(string value)
    {
        return value == "frag" || value == "ipoptions" || value == "tcpoptions" ||
            value == "established" || value == "setup" || value == "tcpflags" ||
            value == "icmptypes";
    }

    internal static bool IsQosFilterOption(string value)
    {
        return value == "DSCP" || value == "metering";
    }

    internal static int FindFilterOffset(FilterRuleParseState state, int index)
    {
        if (index >= 0 && index < state.Offsets.Count)
        {
            return state.Offsets[index];
        }

        return state.Value.Length;
    }

    internal static DiameterValueResult CreateFilterSuccess(string value)
    {
        DiameterValueResult result = new DiameterValueResult();
        result.Status = DiameterCodecStatus.Success;
        result.RequiredCount = value.Length;
        result.Text = value;
        return result;
    }

    internal static DiameterValueResult CreateFilterFailure(
        bool qos,
        FilterRuleParseState state,
        int offset)
    {
        string code = "invalid-ip-filter-rule";
        string message = "The value is not a valid IPFilterRule.";
        if (qos)
        {
            code = "invalid-qos-filter-rule";
            message = "The value is not a valid QoSFilterRule.";
        }

        return ValueCodecs.CreateValueFailure(
            DiameterCodecStatus.Invalid,
            state.Value.Length,
            offset,
            code,
            message);
    }
}
