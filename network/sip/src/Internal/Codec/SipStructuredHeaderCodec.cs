namespace sip;

internal static class SipStructuredHeaderCodec
{
    internal static bool IsStructuredHeaderName(string? name)
    {
        return IsRouteHeaderName(name) || IsContactHeaderName(name) || IsFromHeaderName(name)
            || IsToHeaderName(name) || IsCallIdHeaderName(name) || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CSeq)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MaxForwards);
    }

    internal static SipStructuredHeaderResult ParseStructuredHeaderValue(string? name, string? text,
        SipParseOptions? options)
    {
        if (name == null || text == null || options == null)
        {
            return CreateError(0, "A structured header name, value, and parse options are required.");
        }

        if (IsRouteHeaderName(name))
        {
            return ParseRouteValues(text, options);
        }

        if (IsContactHeaderName(name))
        {
            return ParseContactValues(text, options);
        }

        if (IsFromHeaderName(name) || IsToHeaderName(name))
        {
            return ParseSingleAddress(text, options);
        }

        if (IsCallIdHeaderName(name))
        {
            return ParseCallIdValue(text);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CSeq))
        {
            return ParseCSeqValue(text);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MaxForwards))
        {
            return ParseMaxForwardsValue(text);
        }

        return CreateError(0, "The header field is not a supported structured header.");
    }

    internal static SipWriteResult FormatStructuredHeaderValue(string? name, SipStructuredHeaderResult? parsed,
        SipWriteOptions? options)
    {
        if (name == null || parsed == null || options == null)
        {
            return CreateWriteError("A structured header value and write options are required.");
        }

        if (!parsed.Success)
        {
            return CreateWriteError(parsed.ErrorMessage == null ? "The structured header value is invalid."
                : parsed.ErrorMessage);
        }

        string headerName = GetFormattedHeaderName(name, options.CompactHeaderNames);
        string? value = null;
        if (IsRouteHeaderName(name))
        {
            value = FormatRouteValues(parsed.RouteValues, options);
        }
        else if (IsContactHeaderName(name))
        {
            value = FormatContactValues(parsed.ContactValues, options);
        }
        else if (IsFromHeaderName(name) || IsToHeaderName(name))
        {
            if (parsed.NameAddress != null)
            {
                value = SipHeaderPrimitives.FormatNameAddressValue(parsed.NameAddress, options);
            }
        }
        else if (IsCallIdHeaderName(name))
        {
            value = parsed.CallId;
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CSeq))
        {
            value = FormatCSeqValue(parsed.CSeq);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.MaxForwards) && parsed.MaxForwards >= 0)
        {
            value = parsed.MaxForwards.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (value == null)
        {
            return CreateWriteError("The structured header model is incomplete.");
        }

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(headerName + ": " + value + "\r\n");
        if (bytes.Length > options.MaxHeaderLineBytes)
        {
            SipWriteResult limited = CreateWriteError("The formatted header field exceeds the configured output limit.");
            limited.RequiredBytes = bytes.Length;
            return limited;
        }

        SipWriteResult result = new SipWriteResult();
        result.Success = true;
        result.Bytes = bytes;
        result.Written = bytes.Length;
        result.RequiredBytes = bytes.Length;
        return result;
    }

    static SipStructuredHeaderResult ParseRouteValues(string text, SipParseOptions options)
    {
        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, options.MaxListValueCount);
        if (!split.Success)
        {
            return CreateError(split.ErrorOffset, split.ErrorMessage);
        }

        SipRouteValue[] routes = new SipRouteValue[split.TextValues.Length];
        int index = 0;
        int offset = 0;
        while (index < split.TextValues.Length)
        {
            string item = split.TextValues[index];
            int itemOffset = text.IndexOf(item, offset, System.StringComparison.Ordinal);
            if (itemOffset < 0)
            {
                itemOffset = offset;
            }

            if (item.IndexOf('<') < 0)
            {
                return CreateError(itemOffset, "A Route or Record-Route value must use the name-address form.");
            }

            SipHeaderPrimitiveResult address = SipHeaderPrimitives.ParseNameAddressText(item, options);
            if (!address.Success || address.NameAddress == null || address.NameAddress.Uri == null)
            {
                return CreateError(itemOffset + address.ErrorOffset, address.ErrorMessage);
            }

            SipRouteValue route = new SipRouteValue();
            route.Address = address.NameAddress;
            route.LooseRouting = ContainsParameter(address.NameAddress.Uri.Parameters, "lr");
            routes[index] = route;
            ClearParameterOriginals(address.NameAddress, options.PreserveOriginal);
            offset = itemOffset + item.Length;
            index = index + 1;
        }

        SipStructuredHeaderResult result = new SipStructuredHeaderResult();
        result.Success = true;
        result.RouteValues = routes;
        return result;
    }

    static SipStructuredHeaderResult ParseContactValues(string text, SipParseOptions options)
    {
        string trimmed = TrimWhitespace(text);
        if (trimmed == "*")
        {
            SipContactValue wildcard = new SipContactValue();
            wildcard.Wildcard = true;
            SipStructuredHeaderResult wildcardResult = new SipStructuredHeaderResult();
            wildcardResult.Success = true;
            wildcardResult.ContactValues = new SipContactValue[] { wildcard };
            return wildcardResult;
        }

        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, options.MaxListValueCount);
        if (!split.Success)
        {
            return CreateError(split.ErrorOffset, split.ErrorMessage);
        }

        SipContactValue[] contacts = new SipContactValue[split.TextValues.Length];
        int index = 0;
        int offset = 0;
        while (index < split.TextValues.Length)
        {
            string item = split.TextValues[index];
            int itemOffset = text.IndexOf(item, offset, System.StringComparison.Ordinal);
            if (itemOffset < 0)
            {
                itemOffset = offset;
            }

            if (item == "*")
            {
                return CreateError(itemOffset, "A wildcard Contact cannot be combined with other values or parameters.");
            }

            SipHeaderPrimitiveResult address = SipHeaderPrimitives.ParseNameAddressText(item, options);
            if (!address.Success || address.NameAddress == null)
            {
                return CreateError(itemOffset + address.ErrorOffset, address.ErrorMessage);
            }

            SipContactValue contact = new SipContactValue();
            contact.Address = address.NameAddress;
            string? issue = ApplyContactParameters(contact);
            if (issue != null)
            {
                return CreateError(itemOffset, issue);
            }

            contacts[index] = contact;
            ClearParameterOriginals(address.NameAddress, options.PreserveOriginal);
            offset = itemOffset + item.Length;
            index = index + 1;
        }

        SipStructuredHeaderResult result = new SipStructuredHeaderResult();
        result.Success = true;
        result.ContactValues = contacts;
        return result;
    }

    static SipStructuredHeaderResult ParseSingleAddress(string text, SipParseOptions options)
    {
        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, 1);
        if (!split.Success)
        {
            return CreateError(split.ErrorOffset, "A From or To header must contain exactly one address.");
        }

        SipHeaderPrimitiveResult address = SipHeaderPrimitives.ParseNameAddressText(split.TextValues[0], options);
        if (!address.Success || address.NameAddress == null)
        {
            return CreateError(address.ErrorOffset, address.ErrorMessage);
        }

        string? issue = ValidateAddressParameters(address.NameAddress.Parameters);
        if (issue != null)
        {
            return CreateError(0, issue);
        }

        ClearParameterOriginals(address.NameAddress, options.PreserveOriginal);
        SipStructuredHeaderResult result = new SipStructuredHeaderResult();
        result.Success = true;
        result.NameAddress = address.NameAddress;
        return result;
    }

    static SipStructuredHeaderResult ParseCallIdValue(string text)
    {
        if (!IsCallIdValue(text))
        {
            return CreateError(0, "The Call-ID value is invalid.");
        }

        SipStructuredHeaderResult result = new SipStructuredHeaderResult();
        result.Success = true;
        result.CallId = text;
        return result;
    }

    static SipStructuredHeaderResult ParseCSeqValue(string text)
    {
        int separator = 0;
        while (separator < text.Length && text[separator] >= '0' && text[separator] <= '9')
        {
            separator = separator + 1;
        }

        if (separator == 0 || separator == text.Length
            || text[separator] != ' ' && text[separator] != '\t')
        {
            return CreateError(separator, "The CSeq value requires a sequence number and method separated by whitespace.");
        }

        SipNumericParseResult number = SipNumericParser.ParseNonNegativeInt32Text(text.Substring(0, separator), int.MaxValue);
        if (!number.Success)
        {
            return CreateError(number.ErrorOffset, "The CSeq sequence number is invalid or too large.");
        }

        int methodStart = SkipWhitespace(text, separator);
        string methodText = text.Substring(methodStart);
        if (!SipAscii.IsToken(methodText))
        {
            return CreateError(methodStart, "The CSeq method must be a token.");
        }

        SipCSeqValue cseq = new SipCSeqValue();
        cseq.SequenceNumber = number.Value;
        cseq.MethodToken = methodText;
        cseq.Method = GetMethod(methodText);
        SipStructuredHeaderResult result = new SipStructuredHeaderResult();
        result.Success = true;
        result.CSeq = cseq;
        return result;
    }

    static SipStructuredHeaderResult ParseMaxForwardsValue(string text)
    {
        SipNumericParseResult number = SipNumericParser.ParseNonNegativeInt32Text(text, 255);
        if (!number.Success)
        {
            return CreateError(number.ErrorOffset, "The Max-Forwards value must be an integer from zero through 255.");
        }

        SipStructuredHeaderResult result = new SipStructuredHeaderResult();
        result.Success = true;
        result.MaxForwards = number.Value;
        return result;
    }

    static string? ApplyContactParameters(SipContactValue contact)
    {
        if (contact.Address == null)
        {
            return "A non-wildcard Contact requires an address.";
        }

        bool quality = false;
        bool expires = false;
        bool instance = false;
        bool registration = false;
        bool publicGruu = false;
        bool temporaryGruu = false;
        int index = 0;
        while (index < contact.Address.Parameters.Length)
        {
            SipUriParameter parameter = contact.Address.Parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "q"))
            {
                if (quality)
                {
                    return "The Contact q parameter cannot be repeated.";
                }

                if (!parameter.HasValue || IsQuotedParameter(parameter))
                {
                    return "The Contact q parameter requires an unquoted q-value.";
                }

                SipHeaderPrimitiveResult parsed = SipHeaderPrimitives.ParseQualityText(parameter.Value);
                if (!parsed.Success)
                {
                    return "The Contact q parameter is invalid.";
                }

                quality = true;
                contact.QualityThousandths = parsed.Number;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "expires"))
            {
                if (expires)
                {
                    return "The Contact expires parameter cannot be repeated.";
                }

                if (!parameter.HasValue || IsQuotedParameter(parameter))
                {
                    return "The Contact expires parameter requires unquoted delta-seconds.";
                }

                SipHeaderPrimitiveResult parsed = SipHeaderPrimitives.ParseDeltaSecondsText(parameter.Value);
                if (!parsed.Success)
                {
                    return "The Contact expires parameter is invalid.";
                }

                expires = true;
                contact.ExpiresSeconds = parsed.Number;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "+sip.instance"))
            {
                if (instance || !parameter.HasValue || !IsQuotedParameter(parameter)
                    || !IsInstanceIdentifier(parameter.Value))
                {
                    return "The Contact +sip.instance parameter is invalid or repeated.";
                }

                instance = true;
                contact.InstanceId = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "reg-id"))
            {
                SipNumericParseResult parsed = SipNumericParser.ParseNonNegativeInt32Text(
                    parameter.Value, int.MaxValue);
                if (registration || !parameter.HasValue || IsQuotedParameter(parameter)
                    || !parsed.Success || parsed.Value == 0)
                {
                    return "The Contact reg-id parameter must be an unrepeated integer from one through 2147483647.";
                }

                registration = true;
                contact.RegistrationId = parsed.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "pub-gruu"))
            {
                if (publicGruu || !IsQuotedSipUriParameter(parameter))
                {
                    return "The Contact pub-gruu parameter must be an unrepeated quoted SIP URI.";
                }

                publicGruu = true;
                contact.PublicGruu = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "temp-gruu"))
            {
                if (temporaryGruu || !IsQuotedSipUriParameter(parameter))
                {
                    return "The Contact temp-gruu parameter must be an unrepeated quoted SIP URI.";
                }

                temporaryGruu = true;
                contact.TemporaryGruu = parameter.Value;
            }

            index = index + 1;
        }

        if (contact.Address.Uri == null)
        {
            return "A non-wildcard Contact requires a URI.";
        }

        bool outbound = false;
        bool provider = false;
        bool pushParameter = false;
        bool resource = false;
        bool reference = false;
        index = 0;
        while (index < contact.Address.Uri.Parameters.Length)
        {
            SipUriParameter parameter = contact.Address.Uri.Parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "ob"))
            {
                if (outbound || parameter.HasValue)
                {
                    return "The Contact ob URI parameter must be unrepeated and valueless.";
                }

                outbound = true;
                contact.Outbound = true;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "pn-provider"))
            {
                if (provider)
                {
                    return "The Contact pn-provider URI parameter cannot be repeated.";
                }

                provider = true;
                contact.PushProvider = parameter.HasValue ? parameter.Value : "";
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "pn-param"))
            {
                if (pushParameter || !HasUriParameterValue(parameter))
                {
                    return "The Contact pn-param URI parameter is invalid or repeated.";
                }

                pushParameter = true;
                contact.PushParameter = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "pn-prid"))
            {
                if (resource || !HasUriParameterValue(parameter))
                {
                    return "The Contact pn-prid URI parameter is invalid or repeated.";
                }

                resource = true;
                contact.PushResourceId = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "pn-purr"))
            {
                if (reference || !HasUriParameterValue(parameter))
                {
                    return "The Contact pn-purr URI parameter is invalid or repeated.";
                }

                reference = true;
                contact.PushRegistrationReference = parameter.Value;
            }

            index = index + 1;
        }

        return null;
    }

    static bool IsQuotedSipUriParameter(SipUriParameter parameter)
    {
        if (!parameter.HasValue || !IsQuotedParameter(parameter) || parameter.Value == null)
        {
            return false;
        }

        SipParseOptions options = new SipParseOptions();
        options.PreserveOriginal = false;
        SipParseResult parsed = SipUriParser.ParseUriBytes(System.Text.Encoding.ASCII.GetBytes(parameter.Value), options);
        return parsed.Status == SipParseStatus.Complete && parsed.Uri != null
            && (parsed.Uri.Scheme == SipUriScheme.Sip || parsed.Uri.Scheme == SipUriScheme.Sips);
    }

    static bool HasUriParameterValue(SipUriParameter parameter)
    {
        return parameter.HasValue && parameter.Value != null && parameter.Value.Length > 0;
    }

    static bool IsInstanceIdentifier(string? value)
    {
        if (value == null || value.Length != 47 || value[0] != '<' || value[46] != '>'
            || !value.Substring(1, 9).Equals("urn:uuid:", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int index = 10;
        while (index < 46)
        {
            int offset = index - 10;
            if (offset == 8 || offset == 13 || offset == 18 || offset == 23)
            {
                if (value[index] != '-') return false;
            }
            else if (!IsHexCharacter(value[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsHexCharacter(int character)
    {
        return character >= '0' && character <= '9'
            || character >= 'a' && character <= 'f'
            || character >= 'A' && character <= 'F';
    }

    static string? ValidateAddressParameters(SipUriParameter[] parameters)
    {
        bool tag = false;
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter parameter = parameters[index];
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "tag"))
            {
                if (tag)
                {
                    return "The tag parameter cannot be repeated.";
                }

                if (!parameter.HasValue || IsQuotedParameter(parameter) || !SipAscii.IsToken(parameter.Value))
                {
                    return "The tag parameter requires an unquoted token value.";
                }

                tag = true;
            }

            index = index + 1;
        }

        return null;
    }

    static string FormatRouteValues(SipRouteValue[] routes, SipWriteOptions options)
    {
        if (routes.Length == 0)
        {
            return "";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < routes.Length)
        {
            if (routes[index] == null || routes[index].Address == null)
            {
                return "";
            }

            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(SipHeaderPrimitives.FormatNameAddressValue(routes[index].Address, options));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string FormatContactValues(SipContactValue[] contacts, SipWriteOptions options)
    {
        if (contacts.Length == 1 && contacts[0] != null && contacts[0].Wildcard)
        {
            return "*";
        }

        if (contacts.Length == 0)
        {
            return "";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < contacts.Length)
        {
            SipContactValue contact = contacts[index];
            if (contact == null || contact.Wildcard || contact.Address == null)
            {
                return "";
            }

            SipNameAddress address = CreateFormattedContactAddress(contact);
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(SipHeaderPrimitives.FormatNameAddressValue(address, options));
            index = index + 1;
        }

        return builder.ToString();
    }

    static SipNameAddress CreateFormattedContactAddress(SipContactValue contact)
    {
        SipNameAddress output = new SipNameAddress();
        if (contact.Address == null)
        {
            return output;
        }

        output.DisplayName = contact.Address.DisplayName;
        output.Uri = contact.Address.Uri == null
            ? null : CopyFormattedContactUri(contact.Address.Uri);
        System.Collections.Generic.List<SipUriParameter> parameters = new System.Collections.Generic.List<SipUriParameter>();
        bool quality = false;
        bool expires = false;
        bool instance = false;
        bool registration = false;
        bool publicGruu = false;
        bool temporaryGruu = false;
        int index = 0;
        while (index < contact.Address.Parameters.Length)
        {
            SipUriParameter source = contact.Address.Parameters[index];
            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = source.Name;
            parameter.Value = source.Value;
            parameter.HasValue = source.HasValue;
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "q") && contact.QualityThousandths >= 0)
            {
                parameter.Value = SipHeaderPrimitives.FormatQualityValue(contact.QualityThousandths);
                parameter.HasValue = true;
                quality = true;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "expires") && contact.ExpiresSeconds >= 0)
            {
                parameter.Value = SipHeaderPrimitives.FormatDeltaSecondsValue(contact.ExpiresSeconds);
                parameter.HasValue = true;
                expires = true;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "+sip.instance")
                && contact.InstanceId != null)
            {
                parameter.Value = contact.InstanceId;
                parameter.HasValue = true;
                instance = true;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "reg-id")
                && contact.RegistrationId > 0)
            {
                parameter.Value = contact.RegistrationId.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
                parameter.HasValue = true;
                registration = true;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "pub-gruu")
                && contact.PublicGruu != null)
            {
                parameter.Value = contact.PublicGruu;
                parameter.HasValue = true;
                publicGruu = true;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "temp-gruu")
                && contact.TemporaryGruu != null)
            {
                parameter.Value = contact.TemporaryGruu;
                parameter.HasValue = true;
                temporaryGruu = true;
            }

            if (SipAscii.EqualsIgnoreCase(parameter.Name, "q"))
            {
                quality = true;
            }

            if (SipAscii.EqualsIgnoreCase(parameter.Name, "expires"))
            {
                expires = true;
            }
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "+sip.instance"))
            {
                instance = true;
            }
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "reg-id"))
            {
                registration = true;
            }
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "pub-gruu"))
            {
                publicGruu = true;
            }
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "temp-gruu"))
            {
                temporaryGruu = true;
            }

            parameters.Add(parameter);
            index = index + 1;
        }

        if (!quality && contact.QualityThousandths >= 0)
        {
            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = "q";
            parameter.Value = SipHeaderPrimitives.FormatQualityValue(contact.QualityThousandths);
            parameter.HasValue = true;
            parameters.Add(parameter);
        }

        if (!expires && contact.ExpiresSeconds >= 0)
        {
            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = "expires";
            parameter.Value = SipHeaderPrimitives.FormatDeltaSecondsValue(contact.ExpiresSeconds);
            parameter.HasValue = true;
            parameters.Add(parameter);
        }

        if (!instance && contact.InstanceId != null)
        {
            AddFormattedContactParameter(
                parameters, "+sip.instance", contact.InstanceId, true);
        }
        if (!registration && contact.RegistrationId > 0)
        {
            AddFormattedContactParameter(parameters, "reg-id",
                contact.RegistrationId.ToString(
                    System.Globalization.CultureInfo.InvariantCulture), true);
        }
        if (!publicGruu && contact.PublicGruu != null)
        {
            AddFormattedContactParameter(
                parameters, "pub-gruu", contact.PublicGruu, true);
        }
        if (!temporaryGruu && contact.TemporaryGruu != null)
        {
            AddFormattedContactParameter(
                parameters, "temp-gruu", contact.TemporaryGruu, true);
        }

        output.Parameters = parameters.ToArray();
        if (output.Uri != null)
        {
            System.Collections.Generic.List<SipUriParameter> uriParameters =
                new System.Collections.Generic.List<SipUriParameter>();
            bool outbound = false;
            bool provider = false;
            bool pushParameter = false;
            bool resource = false;
            bool reference = false;
            index = 0;
            while (index < output.Uri.Parameters.Length)
            {
                SipUriParameter parameter = output.Uri.Parameters[index];
                if (SipAscii.EqualsIgnoreCase(parameter.Name, "ob"))
                {
                    outbound = true;
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "pn-provider"))
                {
                    provider = true;
                    if (contact.PushProvider != null)
                    {
                        parameter.Value = contact.PushProvider;
                        parameter.HasValue = contact.PushProvider.Length > 0;
                    }
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "pn-param"))
                {
                    pushParameter = true;
                    if (contact.PushParameter != null)
                    {
                        parameter.Value = contact.PushParameter;
                        parameter.HasValue = true;
                    }
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "pn-prid"))
                {
                    resource = true;
                    if (contact.PushResourceId != null)
                    {
                        parameter.Value = contact.PushResourceId;
                        parameter.HasValue = true;
                    }
                }
                else if (SipAscii.EqualsIgnoreCase(parameter.Name, "pn-purr"))
                {
                    reference = true;
                    if (contact.PushRegistrationReference != null)
                    {
                        parameter.Value = contact.PushRegistrationReference;
                        parameter.HasValue = true;
                    }
                }
                uriParameters.Add(parameter);
                index = index + 1;
            }
            if (!outbound && contact.Outbound)
            {
                AddFormattedContactParameter(uriParameters, "ob", null, false);
            }
            if (!provider && contact.PushProvider != null)
            {
                AddFormattedContactParameter(uriParameters, "pn-provider",
                    contact.PushProvider, contact.PushProvider.Length > 0);
            }
            if (!pushParameter && contact.PushParameter != null)
            {
                AddFormattedContactParameter(uriParameters, "pn-param",
                    contact.PushParameter, true);
            }
            if (!resource && contact.PushResourceId != null)
            {
                AddFormattedContactParameter(uriParameters, "pn-prid",
                    contact.PushResourceId, true);
            }
            if (!reference && contact.PushRegistrationReference != null)
            {
                AddFormattedContactParameter(uriParameters, "pn-purr",
                    contact.PushRegistrationReference, true);
            }
            output.Uri.Parameters = uriParameters.ToArray();
        }
        return output;
    }

    static void AddFormattedContactParameter(
        System.Collections.Generic.List<SipUriParameter> parameters,
        string name, string? value, bool hasValue)
    {
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = name;
        parameter.Value = value;
        parameter.HasValue = hasValue;
        parameters.Add(parameter);
    }

    static SipUri CopyFormattedContactUri(SipUri source)
    {
        SipUri copy = new SipUri();
        copy.Scheme = source.Scheme;
        copy.SchemeToken = source.SchemeToken;
        copy.User = source.User;
        copy.Password = source.Password;
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.Parameters = new SipUriParameter[source.Parameters.Length];
        int index = 0;
        while (index < source.Parameters.Length)
        {
            SipUriParameter sourceParameter = source.Parameters[index];
            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = sourceParameter.Name;
            parameter.Value = sourceParameter.Value;
            parameter.HasValue = sourceParameter.HasValue;
            parameter.OriginalText = sourceParameter.OriginalText;
            parameter.Quoted = sourceParameter.Quoted;
            copy.Parameters[index] = parameter;
            index = index + 1;
        }
        copy.Headers = source.Headers;
        copy.OpaquePart = source.OpaquePart;
        copy.OriginalText = source.OriginalText;
        copy.ComparisonUserSource = source.ComparisonUserSource;
        copy.ComparisonUserValue = source.ComparisonUserValue;
        copy.ComparisonPasswordSource = source.ComparisonPasswordSource;
        copy.ComparisonPasswordValue = source.ComparisonPasswordValue;
        return copy;
    }

    static string FormatCSeqValue(SipCSeqValue? cseq)
    {
        if (cseq == null || cseq.SequenceNumber < 0 || cseq.MethodToken == null
            || !SipAscii.IsToken(cseq.MethodToken))
        {
            return "";
        }

        return cseq.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + cseq.MethodToken;
    }

    static SipMethod GetMethod(string method)
    {
        switch (method)
        {
            case SipMethods.Ack: return SipMethod.Ack;
            case SipMethods.Bye: return SipMethod.Bye;
            case SipMethods.Cancel: return SipMethod.Cancel;
            case SipMethods.Info: return SipMethod.Info;
            case SipMethods.Invite: return SipMethod.Invite;
            case SipMethods.Message: return SipMethod.Message;
            case SipMethods.Notify: return SipMethod.Notify;
            case SipMethods.Options: return SipMethod.Options;
            case SipMethods.Prack: return SipMethod.Prack;
            case SipMethods.Publish: return SipMethod.Publish;
            case SipMethods.Refer: return SipMethod.Refer;
            case SipMethods.Register: return SipMethod.Register;
            case SipMethods.Subscribe: return SipMethod.Subscribe;
            case SipMethods.Update: return SipMethod.Update;
            default: return SipMethod.Unknown;
        }
    }

    static bool IsCallIdValue(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        bool at = false;
        bool needWord = true;
        int index = 0;
        while (index < text.Length)
        {
            int character = text[index];
            if (character == '@')
            {
                if (at || needWord || index + 1 == text.Length)
                {
                    return false;
                }

                at = true;
                needWord = true;
            }
            else
            {
                if (!IsWordCharacter(character))
                {
                    return false;
                }

                needWord = false;
            }

            index = index + 1;
        }

        return !needWord;
    }

    static bool IsWordCharacter(int character)
    {
        return SipAscii.IsTokenCharacter(character) || character == '(' || character == ')' || character == '<'
            || character == '>' || character == ':' || character == '\\' || character == '"' || character == '/'
            || character == '[' || character == ']' || character == '?' || character == '{' || character == '}';
    }

    static bool ContainsParameter(SipUriParameter[] parameters, string name)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            if (parameters[index] != null && SipAscii.EqualsIgnoreCase(parameters[index].Name, name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsQuotedParameter(SipUriParameter parameter)
    {
        if (parameter.Quoted)
        {
            return true;
        }

        if (parameter.OriginalText == null)
        {
            return false;
        }

        int equals = parameter.OriginalText.IndexOf('=');
        if (equals < 0)
        {
            return false;
        }

        int index = SkipWhitespace(parameter.OriginalText, equals + 1);
        return index < parameter.OriginalText.Length && parameter.OriginalText[index] == '"';
    }

    static void ClearParameterOriginals(SipNameAddress address, bool preserveOriginal)
    {
        if (preserveOriginal)
        {
            return;
        }

        int index = 0;
        while (index < address.Parameters.Length)
        {
            address.Parameters[index].OriginalText = null;
            index = index + 1;
        }
    }

    static string GetFormattedHeaderName(string name, bool compact)
    {
        if (IsContactHeaderName(name))
        {
            return compact ? SipHeaderNames.CompactContact : SipHeaderNames.Contact;
        }

        if (IsFromHeaderName(name))
        {
            return compact ? SipHeaderNames.CompactFrom : SipHeaderNames.From;
        }

        if (IsToHeaderName(name))
        {
            return compact ? SipHeaderNames.CompactTo : SipHeaderNames.To;
        }

        if (IsCallIdHeaderName(name))
        {
            return compact ? SipHeaderNames.CompactCallId : SipHeaderNames.CallId;
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RecordRoute))
        {
            return SipHeaderNames.RecordRoute;
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Path))
        {
            return SipHeaderNames.Path;
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ServiceRoute))
        {
            return SipHeaderNames.ServiceRoute;
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Route))
        {
            return SipHeaderNames.Route;
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CSeq))
        {
            return SipHeaderNames.CSeq;
        }

        return SipHeaderNames.MaxForwards;
    }

    static bool IsRouteHeaderName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Route)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.RecordRoute)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Path)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ServiceRoute);
    }

    static bool IsContactHeaderName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Contact)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactContact);
    }

    static bool IsFromHeaderName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.From)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactFrom);
    }

    static bool IsToHeaderName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.To)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactTo);
    }

    static bool IsCallIdHeaderName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CallId)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.CompactCallId);
    }

    static int SkipWhitespace(string text, int start)
    {
        int index = start;
        while (index < text.Length && (text[index] == ' ' || text[index] == '\t'))
        {
            index = index + 1;
        }

        return index;
    }

    static string TrimWhitespace(string text)
    {
        int start = SkipWhitespace(text, 0);
        int end = text.Length;
        while (end > start && (text[end - 1] == ' ' || text[end - 1] == '\t'))
        {
            end = end - 1;
        }

        return text.Substring(start, end - start);
    }

    static SipStructuredHeaderResult CreateError(int offset, string? message)
    {
        SipStructuredHeaderResult result = new SipStructuredHeaderResult();
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        return result;
    }

    static SipWriteResult CreateWriteError(string message)
    {
        SipWriteResult result = new SipWriteResult();
        result.ErrorMessage = message;
        return result;
    }
}
