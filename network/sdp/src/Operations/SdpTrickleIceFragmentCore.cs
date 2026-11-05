using System;
using System.Collections.Generic;
using System.Text;

namespace sdp;

internal static class SdpTrickleIceFragmentCore
{
    internal static SdpParseResult ParseFragment(string input, SdpParseOptions options)
    {
        SdpParseOptions workingOptions = SdpParserCore.CopyOptions(options);
        workingOptions.PreserveRawLines = true;
        workingOptions.PreserveMalformedLines = true;
        SdpParseResult result = SdpParserCore.ParseTextInput(input, workingOptions, false);
        if (result.Description != null && !result.DiagnosticLimitReached)
        {
            SdpDiagnosticLimiter limiter = new SdpDiagnosticLimiter(result.Diagnostics, options.MaximumDiagnosticCount);
            ValidateFragmentDescription(result, limiter);
            result.DiagnosticLimitReached = limiter.DiagnosticLimitReached;
        }
        result.AppliedOptions = SdpParserCore.CopyOptions(options);
        RestoreRequestedRawLines(result.Description, options);
        return result;
    }

    internal static SdpWriteResult WriteFragment(SdpSessionDescription fragment, SdpWriteOptions options)
    {
        ValidateWriteOptions(options);
        SdpWriteResult result = new SdpWriteResult();
        result.AppliedOptions = CopyWriteOptions(options);
        string lineEnding = ReadLineEnding(options.LineEnding);
        StringBuilder builder = new StringBuilder();
        long buildLimit = (long)options.MaximumOutputCharacters + lineEnding.Length;

        int attributeIndex = 0;
        while (attributeIndex < fragment.Attributes.Count)
        {
            SdpAttribute? attribute = fragment.Attributes[attributeIndex];
            string line = ReadAttributeLine(attribute);
            if (line.Length == 0)
            {
                string attributeName = attribute == null ? string.Empty : attribute.Name;
                SdpSourceLocation location = attribute == null ? new SdpSourceLocation() : attribute.Location;
                AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteInvalidDescription, SdpScope.Session, 'a', attributeName, -1, "A fragment attribute cannot be represented as an SDP attribute line.", location);
                return result;
            }
            if (!AppendFragmentLine(builder, line, lineEnding, buildLimit))
            {
                string attributeName = attribute == null ? string.Empty : attribute.Name;
                SdpSourceLocation location = attribute == null ? new SdpSourceLocation() : attribute.Location;
                AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded, SdpScope.Session, 'a', attributeName, -1, "The generated SDP fragment would exceed the configured character limit.", location);
                return result;
            }
            attributeIndex = attributeIndex + 1;
        }

        int mediaIndex = 0;
        while (mediaIndex < fragment.MediaDescriptions.Count)
        {
            SdpMediaDescription? mediaValue = fragment.MediaDescriptions[mediaIndex];
            if (mediaValue == null)
            {
                AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteInvalidDescription, SdpScope.Media, 'm', string.Empty, mediaIndex, "A fragment media-description entry must not be null.", new SdpSourceLocation());
                return result;
            }
            SdpMediaDescription media = mediaValue;
            if (!AppendFragmentLine(builder, "m=audio 9 RTP/AVP 0", lineEnding, buildLimit))
            {
                AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded, SdpScope.Media, 'm', string.Empty, mediaIndex, "The generated SDP fragment would exceed the configured character limit.", media.Location);
                return result;
            }
            attributeIndex = 0;
            while (attributeIndex < media.Attributes.Count)
            {
                SdpAttribute? attribute = media.Attributes[attributeIndex];
                string line = ReadAttributeLine(attribute);
                if (line.Length == 0)
                {
                    string attributeName = attribute == null ? string.Empty : attribute.Name;
                    SdpSourceLocation location = attribute == null ? new SdpSourceLocation() : attribute.Location;
                    AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteInvalidDescription, SdpScope.Media, 'a', attributeName, mediaIndex, "A fragment attribute cannot be represented as an SDP attribute line.", location);
                    return result;
                }
                if (!AppendFragmentLine(builder, line, lineEnding, buildLimit))
                {
                    string attributeName = attribute == null ? string.Empty : attribute.Name;
                    SdpSourceLocation location = attribute == null ? new SdpSourceLocation() : attribute.Location;
                    AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded, SdpScope.Media, 'a', attributeName, mediaIndex, "The generated SDP fragment would exceed the configured character limit.", location);
                    return result;
                }
                attributeIndex = attributeIndex + 1;
            }
            mediaIndex = mediaIndex + 1;
        }

        if (!options.IncludeFinalLineEnding && builder.Length >= lineEnding.Length)
        {
            builder.Remove(builder.Length - lineEnding.Length, lineEnding.Length);
        }
        if (builder.Length > options.MaximumOutputCharacters)
        {
            AddWriteDiagnostic(result, options, SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded, SdpScope.Session, '\0', string.Empty, -1, "The generated SDP fragment would exceed the configured character limit.", new SdpSourceLocation());
            return result;
        }

        string text = builder.ToString();
        SdpParseOptions parseOptions = CreateValidationParseOptions(options, text.Length);
        SdpParseResult validation = ParseFragment(text, parseOptions);
        if (!validation.Success)
        {
            CopyValidationDiagnostics(result, validation, options.MaximumDiagnosticCount);
            return result;
        }
        result.Text = text;
        result.CharactersWritten = text.Length;
        result.Success = true;
        return result;
    }

    internal static void ValidateFragmentDescription(SdpParseResult result, SdpDiagnosticLimiter limiter)
    {
        SdpSessionDescription description = result.Description == null ? new SdpSessionDescription() : result.Description;
        ValidateSessionRawLines(result, description, limiter);
        ValidateAttributeScope(result, description.Attributes, SdpScope.Session, -1, limiter);
        ValidateAttributeCardinality(result, description.Attributes, SdpScope.Session, -1, limiter);
        bool sessionEnded = ContainsValidMarker(description.Attributes, SdpAttributeNames.EndOfCandidates);
        bool sessionHasCredentials = HasCompleteCredentials(description.Attributes, new List<SdpAttribute>());
        if (description.MediaDescriptions.Count == 0 && !sessionHasCredentials)
        {
            AddFragmentDiagnostic(result, limiter, SdpScope.Session, 'a', SdpAttributeNames.IceUsernameFragment, -1, "A session-only trickle ICE fragment requires a complete ICE credential pair.", new SdpSourceLocation());
        }
        if (sessionEnded && !sessionHasCredentials)
        {
            AddFragmentDiagnostic(result, limiter, SdpScope.Session, 'a', SdpAttributeNames.EndOfCandidates, -1, "A session-level end-of-candidates marker requires session-level ICE credentials.", FindAttributeLocation(description.Attributes, SdpAttributeNames.EndOfCandidates));
        }

        List<string> mediaIds = new List<string>();
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count && !limiter.DiagnosticLimitReached)
        {
            SdpMediaDescription media = description.MediaDescriptions[mediaIndex];
            ValidateMediaRawLines(result, media, mediaIndex, limiter);
            ValidateAttributeScope(result, media.Attributes, SdpScope.Media, mediaIndex, limiter);
            ValidateAttributeCardinality(result, media.Attributes, SdpScope.Media, mediaIndex, limiter);
            ValidateMediaAssociation(result, media, mediaIndex, mediaIds, sessionEnded, limiter);
            if (!HasCompleteCredentials(description.Attributes, media.Attributes))
            {
                AddFragmentDiagnostic(result, limiter, SdpScope.Media, 'a', SdpAttributeNames.IceUsernameFragment, mediaIndex, "Every fragment media section requires a complete effective ICE credential pair.", media.Location);
            }
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void ValidateSessionRawLines(SdpParseResult result, SdpSessionDescription description, SdpDiagnosticLimiter limiter)
    {
        int index = 0;
        while (index < description.RawLines.Count && !limiter.DiagnosticLimitReached)
        {
            SdpRawLine line = description.RawLines[index];
            if (!line.IsMalformed && line.FieldName != 'a')
            {
                AddFragmentDiagnostic(result, limiter, SdpScope.Session, line.FieldName, string.Empty, -1, "A trickle ICE fragment may contain only attributes before its pseudo media sections.", line.Location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateMediaRawLines(SdpParseResult result, SdpMediaDescription media, int mediaIndex, SdpDiagnosticLimiter limiter)
    {
        bool pseudoLineSeen = false;
        int index = 0;
        while (index < media.RawLines.Count && !limiter.DiagnosticLimitReached)
        {
            SdpRawLine line = media.RawLines[index];
            if (!line.IsMalformed && line.FieldName == 'm' && !pseudoLineSeen)
            {
                pseudoLineSeen = true;
            }
            else if (!line.IsMalformed && line.FieldName != 'a')
            {
                AddFragmentDiagnostic(result, limiter, SdpScope.Media, line.FieldName, string.Empty, mediaIndex, "A pseudo media section may contain only its media line and attributes.", line.Location);
            }
            index = index + 1;
        }
        bool validPseudoLine = media.MediaType == "audio" && media.Port == 9 && !media.HasPortCount && media.Protocol == "RTP/AVP" && media.Formats.Count == 1 && media.Formats[0] == "0";
        if (!validPseudoLine)
        {
            AddFragmentDiagnostic(result, limiter, SdpScope.Media, 'm', string.Empty, mediaIndex, "A trickle ICE pseudo media line must be exactly m=audio 9 RTP/AVP 0.", media.Location);
        }
    }

    internal static void ValidateAttributeScope(SdpParseResult result, List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, SdpDiagnosticLimiter limiter)
    {
        int index = 0;
        while (index < attributes.Count && !limiter.DiagnosticLimitReached)
        {
            SdpAttribute attribute = attributes[index];
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
            if (decoded.Recognized && !decoded.Success)
            {
                AddFragmentDiagnostic(result, limiter, scope, 'a', attribute.Name, mediaIndex, "A recognized fragment attribute has invalid syntax.", attribute.Location);
            }
            if (scope == SdpScope.Session && IsKnownMediaFragmentAttribute(attribute.Name))
            {
                AddFragmentDiagnostic(result, limiter, scope, 'a', attribute.Name, mediaIndex, "This trickle ICE fragment attribute is not permitted at session scope.", attribute.Location);
            }
            if (scope == SdpScope.Media && IsKnownSessionFragmentAttribute(attribute.Name))
            {
                AddFragmentDiagnostic(result, limiter, scope, 'a', attribute.Name, mediaIndex, "This trickle ICE fragment attribute is not permitted at media scope.", attribute.Location);
            }
            if (scope == SdpScope.Session && string.Equals(attribute.Name, SdpAttributeNames.Group, StringComparison.OrdinalIgnoreCase) && decoded.Success && decoded.GroupAttribute != null && decoded.GroupAttribute.Semantic != SdpGroupSemantic.Bundle)
            {
                AddFragmentDiagnostic(result, limiter, scope, 'a', attribute.Name, mediaIndex, "A fragment group attribute must use the BUNDLE semantic.", attribute.Location);
            }
            index = index + 1;
        }
    }

    internal static void ValidateAttributeCardinality(SdpParseResult result, List<SdpAttribute> attributes, SdpScope scope, int mediaIndex, SdpDiagnosticLimiter limiter)
    {
        ValidateSingleAttribute(result, attributes, SdpAttributeNames.IceUsernameFragment, scope, mediaIndex, limiter);
        ValidateSingleAttribute(result, attributes, SdpAttributeNames.IcePassword, scope, mediaIndex, limiter);
        ValidateSingleAttribute(result, attributes, SdpAttributeNames.IceOptions, scope, mediaIndex, limiter);
        ValidateSingleAttribute(result, attributes, SdpAttributeNames.EndOfCandidates, scope, mediaIndex, limiter);
        if (scope == SdpScope.Session)
        {
            ValidateSingleAttribute(result, attributes, SdpAttributeNames.IceLite, scope, mediaIndex, limiter);
            ValidateSingleAttribute(result, attributes, SdpAttributeNames.Group, scope, mediaIndex, limiter);
        }
        else
        {
            ValidateSingleAttribute(result, attributes, SdpAttributeNames.MediaId, scope, mediaIndex, limiter);
            ValidateSingleAttribute(result, attributes, SdpAttributeNames.RemoteCandidates, scope, mediaIndex, limiter);
            ValidateSingleAttribute(result, attributes, SdpAttributeNames.Rtcp, scope, mediaIndex, limiter);
            ValidateSingleAttribute(result, attributes, SdpAttributeNames.RtcpMux, scope, mediaIndex, limiter);
            ValidateSingleAttribute(result, attributes, SdpAttributeNames.RtcpMuxOnly, scope, mediaIndex, limiter);
        }
    }

    internal static void ValidateSingleAttribute(SdpParseResult result, List<SdpAttribute> attributes, string name, SdpScope scope, int mediaIndex, SdpDiagnosticLimiter limiter)
    {
        int count = 0;
        int index = 0;
        while (index < attributes.Count && !limiter.DiagnosticLimitReached)
        {
            if (string.Equals(attributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                count = count + 1;
                if (count > 1)
                {
                    AddFragmentDiagnostic(result, limiter, scope, 'a', attributes[index].Name, mediaIndex, "A singleton fragment attribute appears more than once in the same scope.", attributes[index].Location);
                }
            }
            index = index + 1;
        }
    }

    internal static void ValidateMediaAssociation(SdpParseResult result, SdpMediaDescription media, int mediaIndex, List<string> mediaIds, bool sessionEnded, SdpDiagnosticLimiter limiter)
    {
        bool midSeen = false;
        bool mediaEnded = false;
        string mediaId = string.Empty;
        int index = 0;
        while (index < media.Attributes.Count && !limiter.DiagnosticLimitReached)
        {
            SdpAttribute attribute = media.Attributes[index];
            SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
            if (decoded.Success && decoded.MediaIdAttribute != null)
            {
                midSeen = true;
                mediaId = decoded.MediaIdAttribute.Id;
            }
            bool candidate = string.Equals(attribute.Name, SdpAttributeNames.Candidate, StringComparison.OrdinalIgnoreCase);
            bool remoteCandidates = string.Equals(attribute.Name, SdpAttributeNames.RemoteCandidates, StringComparison.OrdinalIgnoreCase);
            bool completion = string.Equals(attribute.Name, SdpAttributeNames.EndOfCandidates, StringComparison.OrdinalIgnoreCase) && decoded.Success;
            if ((candidate || remoteCandidates || completion) && !midSeen)
            {
                AddFragmentDiagnostic(result, limiter, SdpScope.Media, 'a', attribute.Name, mediaIndex, "Candidate signaling in a pseudo media section must follow its MID attribute.", attribute.Location);
            }
            if (candidate && (mediaEnded || sessionEnded))
            {
                AddFragmentDiagnostic(result, limiter, SdpScope.Media, 'a', attribute.Name, mediaIndex, "A candidate must not follow an applicable end-of-candidates marker.", attribute.Location);
            }
            if (completion)
            {
                mediaEnded = true;
            }
            index = index + 1;
        }
        if (!midSeen)
        {
            AddFragmentDiagnostic(result, limiter, SdpScope.Media, 'a', SdpAttributeNames.MediaId, mediaIndex, "Every trickle ICE pseudo media section requires one valid MID attribute.", media.Location);
            return;
        }
        index = 0;
        while (index < mediaIds.Count)
        {
            if (string.Equals(mediaIds[index], mediaId, StringComparison.Ordinal))
            {
                AddFragmentDiagnostic(result, limiter, SdpScope.Media, 'a', SdpAttributeNames.MediaId, mediaIndex, "Each pseudo media section must identify a distinct MID.", FindAttributeLocation(media.Attributes, SdpAttributeNames.MediaId));
                return;
            }
            index = index + 1;
        }
        mediaIds.Add(mediaId);
    }

    internal static bool HasCompleteCredentials(List<SdpAttribute> sessionAttributes, List<SdpAttribute> mediaAttributes)
    {
        SdpIceCredentials? credentials = SdpIceServiceCore.ResolveCredentials(sessionAttributes, mediaAttributes);
        return credentials != null && credentials.UsernameFragment.Length > 0 && credentials.Password.Length > 0;
    }

    internal static bool ContainsValidMarker(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SdpAttribute attribute = attributes[index];
            if (string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                SdpAttributeDecodeResult decoded = SdpAttributeDecoderCore.DecodeAttribute(attribute);
                return decoded.Success && decoded.CommonAttribute != null;
            }
            index = index + 1;
        }
        return false;
    }

    internal static bool IsKnownMediaFragmentAttribute(string name)
    {
        return string.Equals(name, SdpAttributeNames.MediaId, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.Candidate, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.RemoteCandidates, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.Rtcp, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.RtcpMux, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.RtcpMuxOnly, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsKnownSessionFragmentAttribute(string name)
    {
        return string.Equals(name, SdpAttributeNames.IceLite, StringComparison.OrdinalIgnoreCase) || string.Equals(name, SdpAttributeNames.Group, StringComparison.OrdinalIgnoreCase);
    }

    internal static SdpSourceLocation FindAttributeLocation(List<SdpAttribute> attributes, string name)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (string.Equals(attributes[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return attributes[index].Location;
            }
            index = index + 1;
        }
        return new SdpSourceLocation();
    }

    internal static void AddFragmentDiagnostic(SdpParseResult result, SdpDiagnosticLimiter limiter, SdpScope scope, char fieldName, string attributeName, int mediaIndex, string detail, SdpSourceLocation location)
    {
        SdpSourcePath path = new SdpSourcePath();
        path.Scope = scope;
        path.MediaIndex = mediaIndex;
        path.FieldName = fieldName;
        path.AttributeName = attributeName;
        SdpDiagnostic diagnostic = SdpDiagnosticBuilder.CreateDiagnostic(SdpDiagnosticCodes.ValidationIce, SdpValidationSeverity.Error, scope, fieldName, attributeName, mediaIndex, detail, path, SdpDiagnosticBuilder.CopySourceLocation(location));
        limiter.AddDiagnostic(diagnostic);
        result.Success = false;
    }

    internal static void RestoreRequestedRawLines(SdpSessionDescription? description, SdpParseOptions options)
    {
        if (description == null || options.PreserveRawLines || options.Mode == SdpParseMode.Lossless)
        {
            return;
        }
        RemoveUnrequestedRawLines(description.RawLines, options.PreserveMalformedLines);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Count)
        {
            RemoveUnrequestedRawLines(description.MediaDescriptions[mediaIndex].RawLines, options.PreserveMalformedLines);
            mediaIndex = mediaIndex + 1;
        }
    }

    internal static void RemoveUnrequestedRawLines(List<SdpRawLine> lines, bool preserveMalformedLines)
    {
        int index = lines.Count - 1;
        while (index >= 0)
        {
            if (!preserveMalformedLines || !lines[index].IsMalformed)
            {
                lines.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    internal static void ValidateWriteOptions(SdpWriteOptions options)
    {
        if (options.Mode != SdpWriteMode.Canonical && options.Mode != SdpWriteMode.Lossless)
        {
            throw new ArgumentOutOfRangeException("options");
        }
        if (options.LineEnding != SdpLineEnding.CrLf && options.LineEnding != SdpLineEnding.Lf && options.LineEnding != SdpLineEnding.Cr)
        {
            throw new ArgumentOutOfRangeException("options");
        }
        if (options.MaximumOutputCharacters < 0 || options.MaximumOutputBytes < 0 || options.MaximumDiagnosticCount < 0)
        {
            throw new ArgumentOutOfRangeException("options");
        }
    }

    internal static SdpWriteOptions CopyWriteOptions(SdpWriteOptions options)
    {
        SdpWriteOptions result = new SdpWriteOptions();
        result.Mode = options.Mode;
        result.LineEnding = options.LineEnding;
        result.IncludeFinalLineEnding = options.IncludeFinalLineEnding;
        result.IncludeObsoleteFields = options.IncludeObsoleteFields;
        result.IncludeUnknownFields = options.IncludeUnknownFields;
        result.MaximumOutputCharacters = options.MaximumOutputCharacters;
        result.MaximumOutputBytes = options.MaximumOutputBytes;
        result.MaximumDiagnosticCount = options.MaximumDiagnosticCount;
        return result;
    }

    internal static string ReadLineEnding(SdpLineEnding lineEnding)
    {
        if (lineEnding == SdpLineEnding.Lf)
        {
            return "\n";
        }
        if (lineEnding == SdpLineEnding.Cr)
        {
            return "\r";
        }
        return "\r\n";
    }

    internal static string ReadAttributeLine(SdpAttribute? attribute)
    {
        if (attribute == null || !SdpCommonAttributeCodecCore.IsTokenValue(attribute.Name))
        {
            return string.Empty;
        }
        if (attribute.Kind != SdpAttributeKind.Property && attribute.Kind != SdpAttributeKind.Value)
        {
            return string.Empty;
        }
        int index = 0;
        while (index < attribute.Value.Length)
        {
            char value = attribute.Value[index];
            if (value == '\0' || value == '\r' || value == '\n' || char.IsControl(value))
            {
                return string.Empty;
            }
            index = index + 1;
        }
        if (attribute.Kind == SdpAttributeKind.Value)
        {
            return "a=" + attribute.Name + ":" + attribute.Value;
        }
        return "a=" + attribute.Name;
    }

    internal static bool AppendFragmentLine(StringBuilder builder, string line, string lineEnding, long buildLimit)
    {
        long required = (long)builder.Length + line.Length + lineEnding.Length;
        if (required > buildLimit)
        {
            return false;
        }
        builder.Append(line);
        builder.Append(lineEnding);
        return true;
    }

    internal static SdpParseOptions CreateValidationParseOptions(SdpWriteOptions options, int textLength)
    {
        SdpParseOptions result = new SdpParseOptions();
        result.AllowLfLineEndings = options.LineEnding == SdpLineEnding.Lf;
        result.AllowCrLineEndings = options.LineEnding == SdpLineEnding.Cr;
        result.MaximumInputCharacters = textLength;
        result.MaximumInputBytes = options.MaximumOutputBytes;
        result.MaximumLineLength = textLength;
        result.MaximumLineCount = int.MaxValue;
        result.MaximumMediaCount = int.MaxValue;
        result.MaximumAttributesPerScope = int.MaxValue;
        result.MaximumTokenLength = int.MaxValue;
        result.MaximumCandidateCount = int.MaxValue;
        result.MaximumDiagnosticCount = options.MaximumDiagnosticCount;
        return result;
    }

    internal static void CopyValidationDiagnostics(SdpWriteResult result, SdpParseResult validation, int maximumDiagnosticCount)
    {
        SdpDiagnosticLimiter limiter = new SdpDiagnosticLimiter(result.Diagnostics, maximumDiagnosticCount);
        int index = 0;
        while (index < validation.Diagnostics.Count && !limiter.DiagnosticLimitReached)
        {
            SdpDiagnostic validationDiagnostic = validation.Diagnostics[index];
            SdpDiagnostic diagnostic = SdpDiagnosticBuilder.CreateDiagnostic(SdpDiagnosticCodes.WriteInvalidDescription, validationDiagnostic.Severity, validationDiagnostic.Scope, validationDiagnostic.FieldName, validationDiagnostic.AttributeName, validationDiagnostic.MediaIndex, validationDiagnostic.Detail, SdpDiagnosticBuilder.CopySourcePath(validationDiagnostic.Path), SdpDiagnosticBuilder.CopySourceLocation(validationDiagnostic.Location));
            limiter.AddDiagnostic(diagnostic);
            index = index + 1;
        }
        result.DiagnosticLimitReached = limiter.DiagnosticLimitReached || validation.DiagnosticLimitReached;
    }

    internal static void AddWriteDiagnostic(SdpWriteResult result, SdpWriteOptions options, string code, SdpScope scope, char fieldName, string attributeName, int mediaIndex, string detail, SdpSourceLocation location)
    {
        SdpSourcePath path = new SdpSourcePath();
        path.Scope = scope;
        path.MediaIndex = mediaIndex;
        path.FieldName = fieldName;
        path.AttributeName = attributeName;
        SdpDiagnostic diagnostic = SdpDiagnosticBuilder.CreateDiagnostic(code, SdpValidationSeverity.Error, scope, fieldName, attributeName, mediaIndex, detail, path, SdpDiagnosticBuilder.CopySourceLocation(location));
        SdpDiagnosticLimiter limiter = new SdpDiagnosticLimiter(result.Diagnostics, options.MaximumDiagnosticCount);
        limiter.AddDiagnostic(diagnostic);
        result.DiagnosticLimitReached = limiter.DiagnosticLimitReached;
    }
}
