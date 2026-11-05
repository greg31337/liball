namespace sip;

internal class SipSdpParseState
{
    internal System.Collections.Generic.List<SipValidationIssue> Issues;
    internal int ErrorOffset;
    SipParseOptions options;
    SipSessionDescription description;
    System.Collections.Generic.List<string> emails;
    System.Collections.Generic.List<string> phones;
    System.Collections.Generic.List<string> bandwidths;
    System.Collections.Generic.List<string> timings;
    System.Collections.Generic.List<string> repeats;
    System.Collections.Generic.List<SipSdpAttribute> attributes;
    System.Collections.Generic.List<SipMediaDescription> media;
    System.Collections.Generic.List<string> unknown;
    SipMediaDescription? currentMedia;
    System.Collections.Generic.List<string>? currentFormats;
    System.Collections.Generic.List<string>? currentBandwidths;
    System.Collections.Generic.List<SipSdpAttribute>? currentAttributes;
    System.Collections.Generic.List<string>? currentUnknown;
    int sessionStage = -1;
    int mediaStage;
    int versionCount;
    int originCount;
    int nameCount;
    int informationCount;
    int uriCount;
    int connectionCount;
    int zoneCount;
    int keyCount;
    int mediaInformationCount;
    int mediaConnectionCount;
    int mediaKeyCount;

    internal SipSdpParseState(SipParseOptions optionsValue)
    {
        options = optionsValue;
        description = new SipSessionDescription();
        emails = new System.Collections.Generic.List<string>();
        phones = new System.Collections.Generic.List<string>();
        bandwidths = new System.Collections.Generic.List<string>();
        timings = new System.Collections.Generic.List<string>();
        repeats = new System.Collections.Generic.List<string>();
        attributes = new System.Collections.Generic.List<SipSdpAttribute>();
        media = new System.Collections.Generic.List<SipMediaDescription>();
        unknown = new System.Collections.Generic.List<string>();
        Issues = new System.Collections.Generic.List<SipValidationIssue>();
    }

    internal string? ProcessDescriptionLine(SipSdpLine line)
    {
        ErrorOffset = line.Offset;
        if (line.Text.Length < 2 || line.Text[1] != '=') return "An SDP line must have the form type=value.";
        int type = line.Text[0];
        if (type < 'a' || type > 'z') return "An SDP field type must be one lowercase letter.";
        string value = line.Text.Substring(2);
        if (!SipSdpSyntax.IsByteString(value) && (type != 's' || options.Strict))
            return "An SDP field value cannot be empty.";
        if (type == 'm') return BeginMedia(value, line);
        if (currentMedia != null) return ProcessMediaField(type, value, line);
        return ProcessSessionField(type, value, line);
    }

    internal SipSessionDescription BuildDescription()
    {
        FinishCurrentMedia();
        AddMissingVersionIssue();
        description.Emails = emails.ToArray();
        description.Phones = phones.ToArray();
        description.Bandwidths = bandwidths.ToArray();
        description.Timings = timings.ToArray();
        description.RepeatTimes = repeats.ToArray();
        description.Attributes = attributes.ToArray();
        description.MediaDescriptions = media.ToArray();
        description.UnknownLines = unknown.ToArray();
        return description;
    }

    void AddMissingVersionIssue()
    {
        if (versionCount != 0) return;
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = options.Strict ? SipValidationSeverity.Error : SipValidationSeverity.Warning;
        issue.Code = "sdp-version-required";
        issue.Message = "The SDP version field is required.";
        issue.Path = "sdp.protocolVersion";
        Issues.Add(issue);
    }

    string? ProcessSessionField(int type, string value, SipSdpLine line)
    {
        int rank = SessionRank(type);
        if (rank < 0)
        {
            if (!CanAdd(unknown.Count + 1)) return "The SDP unknown-line limit was exceeded.";
            unknown.Add(line.Text);
            return null;
        }
        if (type == 't' && sessionStage >= 9 && sessionStage <= 11) sessionStage = 8;
        string? orderError = CheckSessionOrder(rank, line.Offset);
        if (orderError != null) return orderError;
        if (type == 'o' || type == 'c' || type == 't' || type == 'r' || type == 'z')
        {
            string normalized = SipSdpSyntax.NormalizeFields(value);
            if (value != normalized)
            {
                string? whitespaceError = CompatibleWhitespace(
                    "Non-canonical whitespace was accepted in an SDP field.", line.Offset);
                if (whitespaceError != null) return whitespaceError;
                value = normalized;
            }
        }
        if (type == 'v') return SetVersion(value);
        if (type == 'o') return SetOrigin(value);
        if (type == 's') return SetName(value);
        if (type == 'i') return SetInformation(value);
        if (type == 'u') return SetUri(value);
        if (type == 'e') return AddEmail(value);
        if (type == 'p') return AddPhone(value);
        if (type == 'c') return SetConnection(value);
        if (type == 'b') return AddBandwidth(value);
        if (type == 't') return AddTiming(value);
        if (type == 'r') return AddRepeat(value);
        if (type == 'z') return SetZones(value);
        if (type == 'k') return SetKey(value);
        return AddAttribute(value, attributes);
    }

    string? ProcessMediaField(int type, string value, SipSdpLine line)
    {
        int rank = MediaRank(type);
        if (rank < 0)
        {
            if (type == 'v' || type == 'o' || type == 's' || type == 'u'
                || type == 'e' || type == 'p' || type == 't' || type == 'r' || type == 'z')
            {
                string? misplaced = Compatibility("A session-level SDP field appeared after a media description.",
                    line.Offset);
                if (misplaced != null) return misplaced;
            }
            if (currentUnknown == null || !CanAdd(currentUnknown.Count + 1))
                return "The SDP media unknown-line limit was exceeded.";
            currentUnknown.Add(line.Text);
            return null;
        }
        string? orderError = CheckMediaOrder(rank, line.Offset);
        if (orderError != null) return orderError;
        if (type == 'c')
        {
            string normalized = SipSdpSyntax.NormalizeFields(value);
            if (value != normalized)
            {
                string? whitespaceError = CompatibleWhitespace(
                    "Non-canonical whitespace was accepted in an SDP field.", line.Offset);
                if (whitespaceError != null) return whitespaceError;
                value = normalized;
            }
        }
        if (type == 'i') return SetMediaTitle(value);
        if (type == 'c') return SetMediaConnection(value);
        if (type == 'b') return AddMediaBandwidth(value);
        if (type == 'k') return SetMediaKey(value);
        if (currentAttributes == null) return "The SDP media attribute collection is unavailable.";
        return AddAttribute(value, currentAttributes);
    }

    string? BeginMedia(string value, SipSdpLine line)
    {
        FinishCurrentMedia();
        string normalized = SipSdpSyntax.NormalizeFields(value);
        if (value != normalized)
        {
            string? whitespaceError = CompatibleWhitespace(
                "Non-canonical whitespace was accepted in an SDP field.", line.Offset);
            if (whitespaceError != null) return whitespaceError;
        }
        string[] fields = SipSdpSyntax.SplitFields(normalized);
        if (fields.Length < 4 || !SipSdpSyntax.IsToken(fields[0])) return "The SDP media field is invalid.";
        string[] portParts = fields[1].Split('/');
        if (portParts.Length < 1 || portParts.Length > 2
            || !SipSdpSyntax.ParseBoundedInteger(portParts[0], 65535, true, -1))
            return "The SDP media port is invalid.";
        int portCount = 1;
        if (portParts.Length == 2)
        {
            if (!SipSdpSyntax.ParseBoundedInteger(portParts[1], 65535, false, -1))
                return "The SDP media port count is invalid.";
            portCount = SipSdpSyntax.ToBoundedInteger(portParts[1]);
        }
        if (!SipSdpSyntax.IsProtocol(fields[2])) return "The SDP media protocol is invalid.";
        if (!CanAdd(media.Count + 1) || !CanAdd(fields.Length - 3))
            return "The SDP media collection limit was exceeded.";
        currentMedia = new SipMediaDescription();
        currentMedia.Media = fields[0];
        currentMedia.Port = SipSdpSyntax.ToBoundedInteger(portParts[0]);
        currentMedia.PortCount = portCount;
        currentMedia.Protocol = fields[2];
        currentFormats = new System.Collections.Generic.List<string>();
        int index = 3;
        while (index < fields.Length)
        {
            if (!SipSdpSyntax.IsToken(fields[index])) return "An SDP media format token is invalid.";
            currentFormats.Add(fields[index]);
            index = index + 1;
        }
        currentBandwidths = new System.Collections.Generic.List<string>();
        currentAttributes = new System.Collections.Generic.List<SipSdpAttribute>();
        currentUnknown = new System.Collections.Generic.List<string>();
        mediaStage = 0;
        mediaInformationCount = 0;
        mediaConnectionCount = 0;
        mediaKeyCount = 0;
        return null;
    }

    void FinishCurrentMedia()
    {
        if (currentMedia == null) return;
        currentMedia.Formats = currentFormats == null ? new string[0] : currentFormats.ToArray();
        currentMedia.Bandwidths = currentBandwidths == null ? new string[0] : currentBandwidths.ToArray();
        currentMedia.Attributes = currentAttributes == null
            ? new SipSdpAttribute[0] : currentAttributes.ToArray();
        currentMedia.UnknownLines = currentUnknown == null ? new string[0] : currentUnknown.ToArray();
        media.Add(currentMedia);
        currentMedia = null;
    }

    string? SetVersion(string value)
    {
        versionCount = versionCount + 1;
        if (versionCount > 1) return "The SDP version field cannot be repeated.";
        if (value != "0") return "The SDP protocol version must be zero.";
        description.ProtocolVersion = 0;
        return null;
    }

    string? SetOrigin(string value)
    {
        originCount = originCount + 1;
        if (originCount > 1) return "The SDP origin field cannot be repeated.";
        description.Origin = SipSdpSyntax.NormalizeFields(value);
        return null;
    }

    string? SetName(string value)
    {
        nameCount = nameCount + 1;
        if (nameCount > 1) return "The SDP session name cannot be repeated.";
        description.SessionName = value;
        return null;
    }

    string? SetInformation(string value)
    {
        informationCount = informationCount + 1;
        if (informationCount > 1) return "The SDP session information field cannot be repeated.";
        description.SessionInformation = value;
        return null;
    }

    string? SetUri(string value)
    {
        uriCount = uriCount + 1;
        if (uriCount > 1) return "The SDP URI field cannot be repeated.";
        description.Uri = value;
        return null;
    }

    string? AddEmail(string value)
    {
        if (!CanAdd(emails.Count + 1)) return "The SDP email collection limit was exceeded.";
        emails.Add(value);
        return null;
    }

    string? AddPhone(string value)
    {
        if (!CanAdd(phones.Count + 1)) return "The SDP phone collection limit was exceeded.";
        phones.Add(value);
        return null;
    }

    string? SetConnection(string value)
    {
        connectionCount = connectionCount + 1;
        if (connectionCount > 1) return "The session-level SDP connection field cannot be repeated.";
        description.Connection = SipSdpSyntax.NormalizeFields(value);
        return null;
    }

    string? AddBandwidth(string value)
    {
        if (!CanAdd(bandwidths.Count + 1)) return "The SDP bandwidth collection limit was exceeded.";
        bandwidths.Add(value);
        return null;
    }

    string? AddTiming(string value)
    {
        if (!CanAdd(timings.Count + 1)) return "The SDP timing collection limit was exceeded.";
        timings.Add(SipSdpSyntax.NormalizeFields(value));
        return null;
    }

    string? AddRepeat(string value)
    {
        if (timings.Count == 0) return "An SDP repeat field requires a preceding timing field.";
        if (!CanAdd(repeats.Count + 1)) return "The SDP repeat-time collection limit was exceeded.";
        repeats.Add(SipSdpSyntax.NormalizeFields(value));
        return null;
    }

    string? SetZones(string value)
    {
        zoneCount = zoneCount + 1;
        if (zoneCount > 1) return "The SDP time-zone field cannot be repeated.";
        if (repeats.Count == 0) return "An SDP time-zone field requires a preceding repeat field.";
        description.TimeZones = SipSdpSyntax.NormalizeFields(value);
        return null;
    }

    string? SetKey(string value)
    {
        keyCount = keyCount + 1;
        if (keyCount > 1) return "The session-level SDP encryption key cannot be repeated.";
        description.EncryptionKey = value;
        return null;
    }

    string? AddAttribute(string value, System.Collections.Generic.List<SipSdpAttribute> target)
    {
        if (!CanAdd(target.Count + 1)) return "The SDP attribute collection limit was exceeded.";
        int separator = value.IndexOf(':');
        SipSdpAttribute attribute = new SipSdpAttribute();
        if (separator < 0)
        {
            attribute.Name = value;
            attribute.HasValue = false;
        }
        else
        {
            attribute.Name = value.Substring(0, separator);
            attribute.Value = value.Substring(separator + 1);
            attribute.HasValue = true;
        }
        if (!SipSdpSyntax.IsAttribute(attribute)) return "The SDP attribute field is invalid.";
        target.Add(attribute);
        return null;
    }

    string? SetMediaTitle(string value)
    {
        mediaInformationCount = mediaInformationCount + 1;
        if (mediaInformationCount > 1) return "The media-level SDP information field cannot be repeated.";
        if (currentMedia != null) currentMedia.Title = value;
        return null;
    }

    string? SetMediaConnection(string value)
    {
        mediaConnectionCount = mediaConnectionCount + 1;
        if (mediaConnectionCount > 1) return "The media-level SDP connection field cannot be repeated.";
        if (currentMedia != null) currentMedia.Connection = SipSdpSyntax.NormalizeFields(value);
        return null;
    }

    string? AddMediaBandwidth(string value)
    {
        if (currentBandwidths == null || !CanAdd(currentBandwidths.Count + 1))
            return "The SDP media bandwidth collection limit was exceeded.";
        currentBandwidths.Add(value);
        return null;
    }

    string? SetMediaKey(string value)
    {
        mediaKeyCount = mediaKeyCount + 1;
        if (mediaKeyCount > 1) return "The media-level SDP encryption key cannot be repeated.";
        if (currentMedia != null) currentMedia.EncryptionKey = value;
        return null;
    }

    string? CheckSessionOrder(int rank, int offset)
    {
        if (rank < sessionStage)
            return Compatibility("The SDP session fields are not in the required order.", offset);
        sessionStage = rank;
        return null;
    }

    string? CheckMediaOrder(int rank, int offset)
    {
        if (rank < mediaStage)
            return Compatibility("The SDP media fields are not in the required order.", offset);
        mediaStage = rank;
        return null;
    }

    string? Compatibility(string message, int offset)
    {
        if (options.Strict) return message;
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Warning;
        issue.Code = "sdp-compatibility";
        issue.Message = message;
        issue.Path = "sdp";
        issue.ByteOffset = offset;
        Issues.Add(issue);
        return null;
    }

    string? CompatibleWhitespace(string message, int offset)
    {
        if (!options.AllowCompatibleWhitespace) return Compatibility(message, offset);
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Warning;
        issue.Code = "sdp-compatible-whitespace";
        issue.Message = message;
        issue.Path = "sdp";
        issue.ByteOffset = offset;
        Issues.Add(issue);
        return null;
    }

    bool CanAdd(int count)
    {
        return count <= options.MaxListValueCount;
    }

    static int SessionRank(int type)
    {
        if (type == 'v') return 0;
        if (type == 'o') return 1;
        if (type == 's') return 2;
        if (type == 'i') return 3;
        if (type == 'u') return 4;
        if (type == 'e') return 5;
        if (type == 'p') return 6;
        if (type == 'c') return 7;
        if (type == 'b') return 8;
        if (type == 't') return 9;
        if (type == 'r') return 10;
        if (type == 'z') return 11;
        if (type == 'k') return 12;
        if (type == 'a') return 13;
        return -1;
    }

    static int MediaRank(int type)
    {
        if (type == 'i') return 1;
        if (type == 'c') return 2;
        if (type == 'b') return 3;
        if (type == 'k') return 4;
        if (type == 'a') return 5;
        return -1;
    }
}
