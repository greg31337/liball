namespace sip;

internal static class SipMimeValidator
{
    internal static SipValidationIssue[] ValidateEntityModel(SipMimeEntity? entity, bool strict)
    {
        return ValidateEntityWithLimit(entity, strict, SipDefaults.MaxMimeDepth);
    }

    internal static SipValidationIssue[] ValidateEntityWithLimit(SipMimeEntity? entity, bool strict,
        int maximumDepth)
    {
        System.Collections.Generic.List<SipValidationIssue> issues =
            new System.Collections.Generic.List<SipValidationIssue>();
        ValidateEntityDepth(entity, 0, maximumDepth, "mime", issues, strict);
        return issues.ToArray();
    }

    static void ValidateEntityDepth(SipMimeEntity? entity, int depth, int maximumDepth, string path,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (entity == null)
        {
            issues.Add(CreateIssue("mime-required", "A MIME entity is required.", path, null, strict));
            return;
        }
        if (entity.Headers == null)
            issues.Add(CreateIssue("mime-headers", "The MIME header collection cannot be null.",
                path + ".headers", null, strict));
        else
            ValidateHeaders(entity.Headers, path, issues, strict);
        if (entity.Body == null)
            issues.Add(CreateIssue("mime-body", "The MIME body byte collection cannot be null.",
                path + ".body", null, strict));
        if (entity.Parts == null)
            issues.Add(CreateIssue("mime-parts", "The MIME part collection cannot be null.",
                path + ".parts", null, strict));
        if (entity.Preamble == null)
            issues.Add(CreateIssue("mime-preamble", "The MIME preamble cannot be null.",
                path + ".preamble", null, strict));
        if (entity.Epilogue == null)
            issues.Add(CreateIssue("mime-epilogue", "The MIME epilogue cannot be null.",
                path + ".epilogue", null, strict));
        if (entity.OriginalBytes == null)
            issues.Add(CreateIssue("mime-original", "The original MIME byte collection cannot be null.",
                path + ".originalBytes", null, strict));
        ValidateMediaType(entity.ContentType, path, issues, strict);
        ValidateDisposition(entity.ContentDisposition, path, issues, strict);
        if (entity.ContentTransferEncoding != null && !SipAscii.IsToken(entity.ContentTransferEncoding))
            issues.Add(CreateIssue("mime-transfer-encoding",
                "Content-Transfer-Encoding must be a token.", path + ".contentTransferEncoding",
                "Content-Transfer-Encoding", strict));

        bool multipart = SipMimeSyntax.IsMultipartType(entity.ContentType);
        if (multipart)
        {
            if (depth >= maximumDepth)
                issues.Add(CreateIssue("mime-depth", "The multipart nesting depth exceeds the configured limit.",
                    path, SipHeaderNames.ContentType, strict));
            string? boundary = SipMimeSyntax.GetBoundaryValue(entity.ContentType);
            if (!SipMimeSyntax.IsBoundaryValue(boundary))
                issues.Add(CreateIssue("mime-boundary",
                    "A multipart entity requires one valid boundary parameter.",
                    path + ".contentType", SipHeaderNames.ContentType, strict));
            if (entity.Body != null && entity.Body.Length > 0)
                issues.Add(CreateIssue("mime-multipart-body",
                    "A multipart entity stores content in Parts rather than Body.", path + ".body", null, strict));
            if (entity.Parts != null && entity.Parts.Length == 0)
                issues.Add(CreateIssue("mime-parts-required",
                    "A multipart entity requires at least one body part.", path + ".parts", null, strict));
            if (entity.ContentTransferEncoding != null
                && !SipAscii.EqualsIgnoreCase(entity.ContentTransferEncoding, "7bit")
                && !SipAscii.EqualsIgnoreCase(entity.ContentTransferEncoding, "8bit")
                && !SipAscii.EqualsIgnoreCase(entity.ContentTransferEncoding, "binary"))
                issues.Add(CreateIssue("mime-multipart-transfer-encoding",
                    "A multipart entity permits only 7bit, 8bit, or binary transfer encoding.",
                    path + ".contentTransferEncoding", "Content-Transfer-Encoding", strict));
            ValidateAlternativeDispositions(entity, path, issues, strict);
            if (entity.Parts != null)
            {
                int index = 0;
                while (index < entity.Parts.Length)
                {
                    ValidateEntityDepth(entity.Parts[index], depth + 1, maximumDepth,
                        path + ".parts[" + index.ToString(
                            System.Globalization.CultureInfo.InvariantCulture) + "]", issues, strict);
                    index = index + 1;
                }
            }
        }
        else
        {
            if (entity.Parts != null && entity.Parts.Length > 0)
                issues.Add(CreateIssue("mime-unexpected-parts",
                    "A non-multipart entity cannot contain nested parts.", path + ".parts", null, strict));
            if (entity.Preamble != null && entity.Preamble.Length > 0
                || entity.Epilogue != null && entity.Epilogue.Length > 0)
                issues.Add(CreateIssue("mime-unexpected-framing",
                    "Only multipart entities can contain a preamble or epilogue.", path, null, strict));
            if (SipMimeSyntax.IsSipFragmentType(entity.ContentType) && entity.Body != null)
                ValidateSipFragment(entity.Body, path, issues, strict);
        }
    }

    static void ValidateSipFragment(byte[] body, string path,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        options.PreserveOriginal = false;
        options.MaxMessageBytes = int.MaxValue;
        options.MaxBodyBytes = int.MaxValue;
        options.MaxHeaderBytes = int.MaxValue;
        options.MaxHeaderLineBytes = int.MaxValue;
        SipMessage? fragment = SipMimeParser.ParseSipFragmentBody(body, options);
        if (fragment == null || !SipMimeParser.SipFragmentHasRequiredBodyHeaders(fragment))
            issues.Add(CreateIssue("mime-sipfrag", "The message/sipfrag body is invalid.",
                path + ".body", SipHeaderNames.ContentType, strict));
    }

    static void ValidateHeaders(SipHeader[] headers, string path,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        int contentType = 0;
        int disposition = 0;
        int transferEncoding = 0;
        int index = 0;
        while (index < headers.Length)
        {
            SipHeader? header = headers[index];
            SipValidationIssue[] headerIssues = SipHeaderValidator.ValidateHeaderModel(header);
            int issueIndex = 0;
            while (issueIndex < headerIssues.Length)
            {
                string? issueMessage = headerIssues[issueIndex].Message;
                string message = issueMessage == null ? "The MIME header field is invalid." : issueMessage;
                issues.Add(CreateIssue("mime-header", message, path + ".headers", null, strict));
                issueIndex = issueIndex + 1;
            }
            string? name = header == null ? null : header.Name;
            if (SipMimeSyntax.HeaderNameIs(name, SipHeaderNames.ContentType,
                SipHeaderNames.CompactContentType)) contentType = contentType + 1;
            if (SipMimeSyntax.HeaderNameIs(name, SipHeaderNames.ContentDisposition, null))
                disposition = disposition + 1;
            if (SipMimeSyntax.HeaderNameIs(name, "Content-Transfer-Encoding", null))
                transferEncoding = transferEncoding + 1;
            index = index + 1;
        }
        if (contentType > 1) issues.Add(CreateIssue("mime-header-duplicate",
            "Content-Type cannot be repeated in one MIME entity.", path + ".headers",
            SipHeaderNames.ContentType, strict));
        if (disposition > 1) issues.Add(CreateIssue("mime-header-duplicate",
            "Content-Disposition cannot be repeated in one MIME entity.", path + ".headers",
            SipHeaderNames.ContentDisposition, strict));
        if (transferEncoding > 1) issues.Add(CreateIssue("mime-header-duplicate",
            "Content-Transfer-Encoding cannot be repeated in one MIME entity.", path + ".headers",
            "Content-Transfer-Encoding", strict));
    }

    static void ValidateMediaType(SipMediaType? mediaType, string path,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (mediaType == null) return;
        if (!SipAscii.IsToken(mediaType.Type) || !SipAscii.IsToken(mediaType.Subtype))
            issues.Add(CreateIssue("mime-content-type", "The MIME content type is invalid.",
                path + ".contentType", SipHeaderNames.ContentType, strict));
        if (mediaType.Parameters == null)
            issues.Add(CreateIssue("mime-content-type-parameters",
                "The content type parameter collection cannot be null.", path + ".contentType.parameters",
                SipHeaderNames.ContentType, strict));
        else
            ValidateParameters(mediaType.Parameters, path + ".contentType.parameters",
                SipHeaderNames.ContentType, issues, strict);
    }

    static void ValidateDisposition(SipContentDisposition? disposition, string path,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (disposition == null) return;
        if (disposition.Disposition == SipBodyDisposition.Unknown
            && !SipAscii.IsToken(disposition.DispositionToken))
            issues.Add(CreateIssue("mime-disposition", "The MIME disposition type is invalid.",
                path + ".contentDisposition", SipHeaderNames.ContentDisposition, strict));
        if (disposition.Parameters == null)
        {
            issues.Add(CreateIssue("mime-disposition-parameters",
                "The disposition parameter collection cannot be null.",
                path + ".contentDisposition.parameters", SipHeaderNames.ContentDisposition, strict));
            return;
        }
        string? handling = SipMimeSyntax.GetHandlingValue(disposition);
        if (handling == null || !SipAscii.EqualsIgnoreCase(handling, "required")
            && !SipAscii.EqualsIgnoreCase(handling, "optional"))
            issues.Add(CreateIssue("mime-handling",
                "The handling parameter must occur once and be required or optional.",
                path + ".contentDisposition.parameters", SipHeaderNames.ContentDisposition, strict));
        ValidateParameters(disposition.Parameters, path + ".contentDisposition.parameters",
            SipHeaderNames.ContentDisposition, issues, strict);
    }

    static void ValidateParameters(SipUriParameter[] parameters, string path, string headerName,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            SipUriParameter? parameter = parameters[index];
            if (parameter == null || !SipAscii.IsToken(parameter.Name) || !parameter.HasValue
                || parameter.Value == null || parameter.Value.Length == 0
                || ContainsInvalidParameterCharacter(parameter.Value))
                issues.Add(CreateIssue("mime-parameter", "A MIME parameter name and value are required.",
                    path, headerName, strict));
            index = index + 1;
        }
    }

    static bool ContainsInvalidParameterCharacter(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character < 32 && character != '\t' || character == 127) return true;
            index = index + 1;
        }
        return false;
    }

    static void ValidateAlternativeDispositions(SipMimeEntity entity, string path,
        System.Collections.Generic.List<SipValidationIssue> issues, bool strict)
    {
        if (entity.ContentType == null || !SipAscii.EqualsIgnoreCase(entity.ContentType.Subtype, "alternative")
            || entity.Parts == null) return;
        string expected = SipMimeSyntax.GetEffectiveDisposition(entity);
        int index = 0;
        while (index < entity.Parts.Length)
        {
            SipMimeEntity? part = entity.Parts[index];
            if (part != null && !SipAscii.EqualsIgnoreCase(
                SipMimeSyntax.GetEffectiveDisposition(part), expected))
                issues.Add(CreateIssue("mime-alternative-disposition",
                    "A multipart/alternative entity and its parts require the same disposition type.",
                    path + ".parts", SipHeaderNames.ContentDisposition, strict));
            index = index + 1;
        }
    }

    static SipValidationIssue CreateIssue(string code, string message, string path,
        string? headerName, bool strict)
    {
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = strict ? SipValidationSeverity.Error : SipValidationSeverity.Warning;
        issue.Code = code;
        issue.Message = message;
        issue.Path = path;
        issue.HeaderName = headerName;
        return issue;
    }
}
