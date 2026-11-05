namespace sip;

internal static class SipXmlBodyValidator
{
    internal static SipValidationIssue[] ValidateBodyModel(SipXmlBody? body, bool strict)
    {
        System.Collections.Generic.List<SipValidationIssue> issues =
            new System.Collections.Generic.List<SipValidationIssue>();
        if (body == null)
        {
            issues.Add(CreateIssue("xml-body-required", "An XML event body is required.", "xmlBody", strict));
            return issues.ToArray();
        }
        SipXmlMediaProfile? profile = SipXmlMediaProfiles.FindProfile(body.MediaType);
        if (profile == null)
        {
            issues.Add(CreateIssue("xml-media-type", "The event body media type is not supported.",
                "xmlBody.mediaType", strict));
            return issues.ToArray();
        }
        AppendMediaTypeIssues(body.MediaType, issues, strict);
        if (body.XmlText == null)
        {
            issues.Add(CreateIssue("xml-text", "The decoded event body text is required.",
                "xmlBody.xmlText", strict));
            return issues.ToArray();
        }
        if (body.OriginalBytes == null)
            issues.Add(CreateIssue("xml-original", "The original event body bytes cannot be null.",
                "xmlBody.originalBytes", strict));
        if (profile.MessageSummary)
        {
            string? summaryError = SipMessageSummarySyntax.ValidateSummaryText(
                body.XmlText, false, SipDefaults.MaxHeaderLineBytes);
            if (summaryError != null)
                issues.Add(CreateIssue("message-summary", summaryError, "xmlBody.xmlText", strict));
            if (body.RootName != profile.RootName || body.NamespaceUri != null)
                issues.Add(CreateIssue("xml-root", "The message-summary body identity is invalid.",
                    "xmlBody.rootName", strict));
            return issues.ToArray();
        }
        SipXmlScanResult scan = new SipXmlScanner(body.XmlText, SipDefaults.MaxXmlDepth).ScanDocument();
        if (!scan.Success)
        {
            issues.Add(CreateIssue("xml-malformed",
                scan.ErrorMessage == null ? "The XML event body is malformed." : scan.ErrorMessage,
                "xmlBody.xmlText", strict));
            return issues.ToArray();
        }
        if (body.RootName != scan.RootName || body.NamespaceUri != scan.NamespaceUri)
            issues.Add(CreateIssue("xml-model-identity",
                "The stored XML root identity does not match the document.", "xmlBody.rootName", strict));
        ValidateProfile(scan, profile, issues, strict);
        return issues.ToArray();
    }

    internal static SipValidationIssue[] ValidateScanForProfile(SipXmlScanResult scan,
        SipXmlMediaProfile profile, bool strict)
    {
        System.Collections.Generic.List<SipValidationIssue> issues =
            new System.Collections.Generic.List<SipValidationIssue>();
        ValidateProfile(scan, profile, issues, strict);
        return issues.ToArray();
    }

    internal static SipValidationIssue[] ValidateMediaTypeModel(SipMediaType? mediaType, bool strict)
    {
        System.Collections.Generic.List<SipValidationIssue> issues =
            new System.Collections.Generic.List<SipValidationIssue>();
        AppendMediaTypeIssues(mediaType, issues, strict);
        return issues.ToArray();
    }

    static void AppendMediaTypeIssues(SipMediaType? mediaType,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (mediaType == null) return;
        if (mediaType.Parameters == null)
        {
            issues.Add(CreateIssue("xml-media-parameters", "The media type parameter collection cannot be null.",
                "xmlBody.mediaType.parameters", strict));
            return;
        }
        bool charsetSeen = false;
        int index = 0;
        while (index < mediaType.Parameters.Length)
        {
            SipUriParameter? parameter = mediaType.Parameters[index];
            if (parameter == null || !SipAscii.IsToken(parameter.Name)
                || !parameter.HasValue || parameter.Value == null || parameter.Value.Length == 0)
                issues.Add(CreateIssue("xml-media-parameter", "An event body media type parameter is invalid.",
                    "xmlBody.mediaType.parameters", strict));
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "charset"))
            {
                if (charsetSeen || !parameter.HasValue
                    || !SipAscii.EqualsIgnoreCase(parameter.Value, "UTF-8"))
                    issues.Add(CreateIssue("xml-charset", "SIP event XML must use one UTF-8 charset parameter.",
                        "xmlBody.mediaType.parameters", strict));
                charsetSeen = true;
            }
            index = index + 1;
        }
    }

    static void ValidateProfile(SipXmlScanResult scan, SipXmlMediaProfile profile,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (scan.RootName != profile.RootName && scan.RootName != profile.AlternateRootName
            || scan.NamespaceUri != profile.NamespaceUri)
            issues.Add(CreateIssue("xml-root", "The XML root element does not match the event body media type.",
                "xmlBody.rootName", strict));
        if (profile.RootName == "presence" && !scan.HasXmlDeclaration)
            issues.Add(CreateIssue("pidf-declaration", "A PIDF document requires an XML declaration.",
                "xmlBody.xmlText", strict));
        if (profile.RequiresEntity)
        {
            string? entity = SipXmlMediaProfiles.FindAttributeValue(scan, "entity");
            if (entity == null || entity.Length == 0 || entity.IndexOf(':') <= 0)
                issues.Add(CreateIssue("xml-entity", "The XML event root requires an entity URI.",
                    "xmlBody.xmlText", strict));
        }
        if (profile.RequiresVersionState)
        {
            string? version = SipXmlMediaProfiles.FindAttributeValue(scan, "version");
            string? state = SipXmlMediaProfiles.FindAttributeValue(scan, "state");
            if (!IsUnsigned32(version))
                issues.Add(CreateIssue("xml-version", "The XML event root requires a 32-bit version.",
                    "xmlBody.xmlText", strict));
            if (state != "full" && state != "partial")
                issues.Add(CreateIssue("xml-state", "The XML event root state must be full or partial.",
                    "xmlBody.xmlText", strict));
        }
        if (profile.AllowsVersion)
        {
            string? version = SipXmlMediaProfiles.FindAttributeValue(scan, "version");
            if (version != null && !IsUnsigned32(version))
                issues.Add(CreateIssue("xml-version", "The XML event root version must be 32-bit when present.",
                    "xmlBody.xmlText", strict));
            string? entity = SipXmlMediaProfiles.FindAttributeValue(scan, "entity");
            if (scan.RootName == "pidf-full"
                && (entity == null || entity.Length == 0 || entity.IndexOf(':') <= 0))
                issues.Add(CreateIssue("xml-entity", "A full PIDF document requires an entity URI.",
                    "xmlBody.xmlText", strict));
            if (entity != null && entity.Length > 0 && entity.IndexOf(':') <= 0)
                issues.Add(CreateIssue("xml-entity", "The PIDF entity attribute must be a URI.",
                    "xmlBody.xmlText", strict));
        }
        if (profile.RequiresListMetadata)
        {
            string? uri = SipXmlMediaProfiles.FindAttributeValue(scan, "uri");
            string? version = SipXmlMediaProfiles.FindAttributeValue(scan, "version");
            string? fullState = SipXmlMediaProfiles.FindAttributeValue(scan, "fullState");
            if (uri == null || uri.Length == 0 || uri.IndexOf(':') <= 0)
                issues.Add(CreateIssue("xml-list-uri", "RLMI requires a list URI.",
                    "xmlBody.xmlText", strict));
            if (!IsUnsigned32(version))
                issues.Add(CreateIssue("xml-version", "RLMI requires a 32-bit version.",
                    "xmlBody.xmlText", strict));
            if (fullState != "true" && fullState != "false"
                && fullState != "1" && fullState != "0")
                issues.Add(CreateIssue("xml-full-state", "RLMI fullState must be a boolean value.",
                    "xmlBody.xmlText", strict));
        }
        if (profile.RequiresStateChild && profile.NamespaceUri != null
            && !SipXmlMediaProfiles.HasDirectChild(scan, "state", profile.NamespaceUri))
            issues.Add(CreateIssue("xml-state", "An isComposing body requires a state child element.",
                "xmlBody.xmlText", strict));
    }

    static bool IsUnsigned32(string? value)
    {
        if (!SipSdpSyntax.IsDigits(value) || value == null) return false;
        int first = 0;
        while (first < value.Length - 1 && value[first] == '0') first = first + 1;
        string significant = value.Substring(first);
        return significant.Length < 10
            || significant.Length == 10 && string.CompareOrdinal(significant, "4294967295") <= 0;
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
