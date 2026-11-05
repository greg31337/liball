namespace sip;

internal static class SipSdpValidator
{
    internal static SipValidationIssue[] ValidateDescriptionModel(SipSessionDescription? description,
        bool strict)
    {
        System.Collections.Generic.List<SipValidationIssue> issues =
            new System.Collections.Generic.List<SipValidationIssue>();
        if (description == null)
        {
            issues.Add(CreateIssue("sdp-required", "An SDP session description is required.", "sdp", strict));
            return issues.ToArray();
        }
        if (description.ProtocolVersion != 0)
            issues.Add(CreateIssue("sdp-version", "The SDP protocol version must be zero.",
                "sdp.protocolVersion", strict));
        if (!SipSdpSyntax.IsOrigin(description.Origin))
            issues.Add(CreateIssue("sdp-origin", "The SDP origin field is invalid.", "sdp.origin", strict));
        if (!SipSdpSyntax.IsByteString(description.SessionName))
            issues.Add(CreateIssue("sdp-session-name", "The SDP session name is required and cannot be empty.",
                "sdp.sessionName", strict));
        if (description.SessionInformation != null && !SipSdpSyntax.IsByteString(description.SessionInformation))
            issues.Add(CreateIssue("sdp-information", "The SDP session information is invalid.",
                "sdp.sessionInformation", strict));
        if (description.Uri != null && !SipSdpSyntax.IsUriReference(description.Uri))
            issues.Add(CreateIssue("sdp-uri", "The SDP session URI is invalid.", "sdp.uri", strict));
        ValidateTextCollection(description.Emails, "emails", issues, strict);
        ValidateTextCollection(description.Phones, "phones", issues, strict);
        if (description.Connection != null && !SipSdpSyntax.IsSessionConnection(description.Connection))
            issues.Add(CreateIssue("sdp-connection", "The SDP session connection field is invalid.",
                "sdp.connection", strict));
        ValidateBandwidthCollection(description.Bandwidths, "sdp.bandwidths", issues, strict);
        ValidateTimingCollection(description.Timings, issues, strict);
        ValidateRepeatCollection(description.RepeatTimes, issues, strict);
        if (description.TimeZones != null && !SipSdpSyntax.IsTimeZones(description.TimeZones))
            issues.Add(CreateIssue("sdp-time-zones", "The SDP time-zone field is invalid.",
                "sdp.timeZones", strict));
        if (description.EncryptionKey != null && !SipSdpSyntax.IsEncryptionKey(description.EncryptionKey))
            issues.Add(CreateIssue("sdp-key", "The SDP encryption key field is invalid.",
                "sdp.encryptionKey", strict));
        ValidateAttributeCollection(description.Attributes, "sdp.attributes", issues, strict);
        ValidateUnknownCollection(description.UnknownLines, "sdp.unknownLines", issues, strict);
        if (description.OriginalBytes == null)
            issues.Add(CreateIssue("sdp-original", "The original SDP byte collection cannot be null.",
                "sdp.originalBytes", strict));
        if (description.MediaDescriptions == null)
            issues.Add(CreateIssue("sdp-media", "The SDP media collection cannot be null.",
                "sdp.mediaDescriptions", strict));
        else ValidateMediaCollection(description, issues, strict);
        if (description.Connection == null && !EveryMediaHasConnection(description.MediaDescriptions))
            issues.Add(CreateIssue("sdp-connection-required",
                "A session connection or a connection in every media description is required.",
                "sdp.connection", strict));
        return issues.ToArray();
    }

    static void ValidateTextCollection(string[]? values, string name,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (values == null)
        {
            issues.Add(CreateIssue("sdp-collection", "An SDP value collection cannot be null.",
                "sdp." + name, strict));
            return;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!SipSdpSyntax.IsByteString(values[index]))
                issues.Add(CreateIssue("sdp-text", "An SDP text field is invalid.",
                    "sdp." + name + "[" + index.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "]", strict));
            index = index + 1;
        }
    }

    static void ValidateBandwidthCollection(string[]? values, string path,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (values == null)
        {
            issues.Add(CreateIssue("sdp-bandwidths", "An SDP bandwidth collection cannot be null.", path, strict));
            return;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!SipSdpSyntax.IsBandwidth(values[index]))
                issues.Add(CreateIssue("sdp-bandwidth", "An SDP bandwidth field is invalid.", path, strict));
            index = index + 1;
        }
    }

    static void ValidateTimingCollection(string[]? values,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (values == null || values.Length == 0)
        {
            issues.Add(CreateIssue("sdp-timing-required", "At least one SDP timing field is required.",
                "sdp.timings", strict));
            return;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!SipSdpSyntax.IsTiming(values[index]))
                issues.Add(CreateIssue("sdp-timing", "An SDP timing field is invalid.", "sdp.timings", strict));
            index = index + 1;
        }
    }

    static void ValidateRepeatCollection(string[]? values,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (values == null)
        {
            issues.Add(CreateIssue("sdp-repeats", "The SDP repeat-time collection cannot be null.",
                "sdp.repeatTimes", strict));
            return;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!SipSdpSyntax.IsRepeatTime(values[index]))
                issues.Add(CreateIssue("sdp-repeat", "An SDP repeat-time field is invalid.",
                    "sdp.repeatTimes", strict));
            index = index + 1;
        }
    }

    static void ValidateAttributeCollection(SipSdpAttribute[]? attributes, string path,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (attributes == null)
        {
            issues.Add(CreateIssue("sdp-attributes", "An SDP attribute collection cannot be null.", path, strict));
            return;
        }
        int index = 0;
        while (index < attributes.Length)
        {
            if (!SipSdpSyntax.IsAttribute(attributes[index]))
                issues.Add(CreateIssue("sdp-attribute", "An SDP attribute field is invalid.", path, strict));
            index = index + 1;
        }
    }

    static void ValidateUnknownCollection(string[]? lines, string path,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (lines == null)
        {
            issues.Add(CreateIssue("sdp-unknown-lines",
                "An SDP unknown-line collection cannot be null.", path, strict));
            return;
        }
        int index = 0;
        while (index < lines.Length)
        {
            if (!SipSdpSyntax.IsUnknownLine(lines[index]))
                issues.Add(CreateIssue("sdp-unknown-line", "An unrecognized SDP line is invalid.", path, strict));
            index = index + 1;
        }
    }

    static void ValidateMediaCollection(SipSessionDescription description,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        int index = 0;
        while (index < description.MediaDescriptions.Length)
        {
            SipMediaDescription? media = description.MediaDescriptions[index];
            string path = "sdp.mediaDescriptions[" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + "]";
            if (media == null)
            {
                issues.Add(CreateIssue("sdp-media", "An SDP media description cannot be null.", path, strict));
                index = index + 1;
                continue;
            }
            if (!SipSdpSyntax.IsToken(media.Media))
                issues.Add(CreateIssue("sdp-media-token", "The SDP media token is invalid.", path + ".media", strict));
            if (media.Port < 0 || media.Port > 65535)
                issues.Add(CreateIssue("sdp-media-port", "The SDP media port must be between zero and 65535.",
                    path + ".port", strict));
            if (media.PortCount <= 0 || media.PortCount > 65535)
                issues.Add(CreateIssue("sdp-media-port-count", "The SDP media port count is invalid.",
                    path + ".portCount", strict));
            if (!SipSdpSyntax.IsProtocol(media.Protocol))
                issues.Add(CreateIssue("sdp-media-protocol", "The SDP media protocol is invalid.",
                    path + ".protocol", strict));
            ValidateFormats(media.Formats, path, issues, strict);
            if (media.Title != null && !SipSdpSyntax.IsByteString(media.Title))
                issues.Add(CreateIssue("sdp-media-title", "The SDP media title is invalid.", path + ".title", strict));
            if (media.Connection != null && !SipSdpSyntax.IsConnection(media.Connection))
                issues.Add(CreateIssue("sdp-media-connection", "The SDP media connection field is invalid.",
                    path + ".connection", strict));
            ValidateBandwidthCollection(media.Bandwidths, path + ".bandwidths", issues, strict);
            if (media.EncryptionKey != null && !SipSdpSyntax.IsEncryptionKey(media.EncryptionKey))
                issues.Add(CreateIssue("sdp-media-key", "The SDP media encryption key is invalid.",
                    path + ".encryptionKey", strict));
            ValidateAttributeCollection(media.Attributes, path + ".attributes", issues, strict);
            ValidateUnknownCollection(media.UnknownLines, path + ".unknownLines", issues, strict);
            index = index + 1;
        }
    }

    static void ValidateFormats(string[]? formats, string path,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (formats == null || formats.Length == 0)
        {
            issues.Add(CreateIssue("sdp-media-formats", "An SDP media description requires a format token.",
                path + ".formats", strict));
            return;
        }
        int index = 0;
        while (index < formats.Length)
        {
            if (!SipSdpSyntax.IsToken(formats[index]))
                issues.Add(CreateIssue("sdp-media-format", "An SDP media format token is invalid.",
                    path + ".formats", strict));
            index = index + 1;
        }
    }

    static bool EveryMediaHasConnection(SipMediaDescription[]? media)
    {
        if (media == null) return false;
        if (media.Length == 0) return true;
        int index = 0;
        while (index < media.Length)
        {
            if (media[index] == null || media[index].Connection == null) return false;
            index = index + 1;
        }
        return true;
    }

    static SipValidationIssue CreateIssue(string code, string message, string path, bool strict)
    {
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = strict ? SipValidationSeverity.Error : SipValidationSeverity.Warning;
        issue.Code = code;
        issue.Message = message;
        issue.Path = path;
        return issue;
    }
}
