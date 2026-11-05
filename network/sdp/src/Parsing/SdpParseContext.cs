namespace sdp;

internal class SdpParseContext
{
    internal SdpParseOptions Options;
    internal bool ValidateDescriptionStructure;
    internal SdpParseResult Result;
    internal SdpSessionDescription Description;
    internal SdpDiagnosticLimiter DiagnosticLimiter;
    internal SdpScope Scope;
    internal SdpMediaDescription? CurrentMedia;
    internal int MediaIndex;
    internal int SessionAttributeCount;
    internal int MediaAttributeCount;
    internal int ProtocolVersionCount;
    internal int OriginCount;
    internal int SessionNameCount;
    internal int SessionInformationCount;
    internal int SessionUriCount;
    internal int SessionConnectionCount;
    internal int SessionBandwidthCount;
    internal int SessionEncryptionKeyCount;
    internal int MediaInformationCount;
    internal int MediaConnectionCount;
    internal int MediaBandwidthCount;
    internal int MediaEncryptionKeyCount;
    internal int TimingFieldCount;
    internal int RepeatFieldCount;
    internal int ZoneFieldCount;
    internal SdpTiming? CurrentTiming;
    internal bool CanAttachRepeat;
    internal bool CurrentTimingHasZone;
    internal int SessionOrderStage;
    internal int MediaOrderStage;
    internal int CompletionLineNumber;
    internal int CompletionColumnNumber;
    internal bool InputIsUtf8;
    internal bool CurrentLineMalformed;
    internal bool HasError;
    internal bool Stop;

    internal SdpParseContext(SdpParseOptions options, bool inputIsUtf8, bool validateDescriptionStructure)
    {
        Options = options;
        ValidateDescriptionStructure = validateDescriptionStructure;
        Result = new SdpParseResult();
        Result.AppliedOptions = options;
        Description = new SdpSessionDescription();
        Result.Description = Description;
        DiagnosticLimiter = new SdpDiagnosticLimiter(Result.Diagnostics, options.MaximumDiagnosticCount);
        Scope = SdpScope.Session;
        CurrentMedia = null;
        MediaIndex = -1;
        SessionAttributeCount = 0;
        MediaAttributeCount = 0;
        ProtocolVersionCount = 0;
        OriginCount = 0;
        SessionNameCount = 0;
        SessionInformationCount = 0;
        SessionUriCount = 0;
        SessionConnectionCount = 0;
        SessionBandwidthCount = 0;
        SessionEncryptionKeyCount = 0;
        MediaInformationCount = 0;
        MediaConnectionCount = 0;
        MediaBandwidthCount = 0;
        MediaEncryptionKeyCount = 0;
        TimingFieldCount = 0;
        RepeatFieldCount = 0;
        ZoneFieldCount = 0;
        CurrentTiming = null;
        CanAttachRepeat = false;
        CurrentTimingHasZone = false;
        SessionOrderStage = 0;
        MediaOrderStage = 0;
        CompletionLineNumber = 1;
        CompletionColumnNumber = 1;
        InputIsUtf8 = inputIsUtf8;
        CurrentLineMalformed = false;
        HasError = false;
        Stop = false;
    }

    internal void AddDiagnostic(string code, SdpValidationSeverity severity, SdpScope scope, char fieldName, int mediaIndex, int occurrenceIndex, string detail, SdpSourceLocation location)
    {
        SdpSourcePath path = new SdpSourcePath();
        path.Scope = scope;
        path.MediaIndex = mediaIndex;
        path.FieldName = fieldName;
        path.OccurrenceIndex = occurrenceIndex;
        SdpDiagnostic diagnostic = SdpDiagnosticBuilder.CreateDiagnostic(code, severity, scope, fieldName, string.Empty, mediaIndex, detail, path, location);
        DiagnosticLimiter.AddDiagnostic(diagnostic);
        Result.DiagnosticLimitReached = DiagnosticLimiter.DiagnosticLimitReached;
        if (DiagnosticLimiter.DiagnosticLimitReached)
        {
            HasError = true;
            Stop = true;
        }
        if (IsMalformedLineDiagnostic(code))
        {
            CurrentLineMalformed = true;
        }
        if (severity == SdpValidationSeverity.Error || severity == SdpValidationSeverity.Fatal)
        {
            HasError = true;
        }
    }

    internal bool IsMalformedLineDiagnostic(string code)
    {
        return code == SdpDiagnosticCodes.ParseInvalidLineEnding || code == SdpDiagnosticCodes.ParseEmptyLine || code == SdpDiagnosticCodes.ParseMalformedLine || code == SdpDiagnosticCodes.ParseInvalidToken || code == SdpDiagnosticCodes.ParseInvalidNumber || code == SdpDiagnosticCodes.ParseNumberOverflow || code == SdpDiagnosticCodes.ParseMalformedAttribute;
    }

    internal void AddRecoveryDiagnostic(SdpRawLine line, char fieldName, int occurrenceIndex)
    {
        if (Options.Mode != SdpParseMode.Lossless || Stop)
        {
            return;
        }
        AddDiagnostic(SdpDiagnosticCodes.ParseRecoveredMalformedLine, SdpValidationSeverity.Warning, Scope, fieldName, MediaIndex, occurrenceIndex, "Lossless parsing retained the malformed line and resumed at the next line boundary.", line.Location);
    }

    internal void AddAttributeDiagnostic(string code, SdpValidationSeverity severity, string attributeName, int occurrenceIndex, string detail, SdpSourceLocation location)
    {
        SdpSourcePath path = new SdpSourcePath();
        path.Scope = Scope;
        path.MediaIndex = MediaIndex;
        path.FieldName = 'a';
        path.AttributeName = attributeName;
        path.OccurrenceIndex = occurrenceIndex;
        SdpDiagnostic diagnostic = SdpDiagnosticBuilder.CreateDiagnostic(code, severity, Scope, 'a', attributeName, MediaIndex, detail, path, location);
        DiagnosticLimiter.AddDiagnostic(diagnostic);
        Result.DiagnosticLimitReached = DiagnosticLimiter.DiagnosticLimitReached;
        if (DiagnosticLimiter.DiagnosticLimitReached)
        {
            HasError = true;
            Stop = true;
        }
        if (IsMalformedLineDiagnostic(code))
        {
            CurrentLineMalformed = true;
        }
        if (severity == SdpValidationSeverity.Error || severity == SdpValidationSeverity.Fatal)
        {
            HasError = true;
        }
    }

    internal bool ValidateLineEnding(SdpRawLine line, char fieldName)
    {
        bool accepted = line.LineEnding == SdpLineEnding.CrLf || line.LineEnding == SdpLineEnding.None;
        if (line.LineEnding == SdpLineEnding.Lf && Options.AllowLfLineEndings)
        {
            accepted = true;
        }
        if (line.LineEnding == SdpLineEnding.Cr && Options.AllowCrLineEndings)
        {
            accepted = true;
        }
        if (!accepted)
        {
            AddDiagnostic(SdpDiagnosticCodes.ParseInvalidLineEnding, SdpValidationSeverity.Error, Scope, fieldName, MediaIndex, -1, "An SDP line uses a line ending that is not enabled by the parse options.", line.Location);
        }
        if (Description.LineEnding == SdpLineEnding.Unknown)
        {
            Description.LineEnding = line.LineEnding;
        }
        return !accepted;
    }

    internal bool EnterMedia(SdpRawLine line)
    {
        int nextMediaIndex = Description.MediaDescriptions.Count;
        if (nextMediaIndex >= Options.MaximumMediaCount)
        {
            AddDiagnostic(SdpDiagnosticCodes.ParseMediaCountLimitExceeded, SdpValidationSeverity.Fatal, SdpScope.Media, 'm', nextMediaIndex, nextMediaIndex, "The SDP input exceeds the configured media-description limit.", line.Location);
            Stop = true;
            return false;
        }
        SdpMediaDescription media = new SdpMediaDescription();
        media.Location = SdpDiagnosticBuilder.CopySourceLocation(line.Location);
        Description.MediaDescriptions.Add(media);
        CurrentMedia = media;
        MediaIndex = nextMediaIndex;
        MediaAttributeCount = 0;
        MediaInformationCount = 0;
        MediaConnectionCount = 0;
        MediaBandwidthCount = 0;
        MediaEncryptionKeyCount = 0;
        MediaOrderStage = 0;
        Scope = SdpScope.Media;
        return true;
    }

    internal bool CountAttribute(SdpRawLine line)
    {
        int occurrenceIndex;
        if (Scope == SdpScope.Media)
        {
            occurrenceIndex = MediaAttributeCount;
            if (MediaAttributeCount >= Options.MaximumAttributesPerScope)
            {
                AddDiagnostic(SdpDiagnosticCodes.ParseAttributeCountLimitExceeded, SdpValidationSeverity.Fatal, Scope, 'a', MediaIndex, occurrenceIndex, "The media scope exceeds the configured attribute-count limit.", line.Location);
                Stop = true;
                return false;
            }
            MediaAttributeCount = MediaAttributeCount + 1;
        }
        else
        {
            occurrenceIndex = SessionAttributeCount;
            if (SessionAttributeCount >= Options.MaximumAttributesPerScope)
            {
                AddDiagnostic(SdpDiagnosticCodes.ParseAttributeCountLimitExceeded, SdpValidationSeverity.Fatal, Scope, 'a', -1, occurrenceIndex, "The session scope exceeds the configured attribute-count limit.", line.Location);
                Stop = true;
                return false;
            }
            SessionAttributeCount = SessionAttributeCount + 1;
        }
        return true;
    }

    internal void RetainLine(SdpRawLine line, bool malformed)
    {
        bool retain = Options.PreserveRawLines || Options.Mode == SdpParseMode.Lossless;
        if (malformed && (Options.PreserveMalformedLines || Options.Mode == SdpParseMode.Lossless))
        {
            retain = true;
        }
        if (!retain)
        {
            return;
        }
        line.IsMalformed = malformed;
        if (line.Text.Length >= 2 && line.Text[1] == '=')
        {
            line.FieldName = line.Text[0];
            line.Value = line.Text.Substring(2);
        }
        if (Scope == SdpScope.Media && CurrentMedia != null)
        {
            CurrentMedia.RawLines.Add(line);
        }
        else
        {
            Description.RawLines.Add(line);
        }
    }

    internal void RecordLineEnd(SdpLineReadResult lineResult)
    {
        if (lineResult.Line.LineEnding == SdpLineEnding.None)
        {
            CompletionLineNumber = lineResult.Line.Location.LineNumber;
            CompletionColumnNumber = lineResult.CharacterLength + 1;
        }
        else
        {
            CompletionLineNumber = lineResult.Line.Location.LineNumber + 1;
            CompletionColumnNumber = 1;
        }
    }

    internal void Complete()
    {
        if (!Stop && ValidateDescriptionStructure)
        {
            AddMissingRequiredFields();
        }
        Result.DiagnosticLimitReached = DiagnosticLimiter.DiagnosticLimitReached;
        Result.Success = !HasError && !Stop;
    }

    internal void AddMissingRequiredFields()
    {
        SdpSourceLocation location = new SdpSourceLocation();
        location.CharacterOffset = Result.CharactersConsumed;
        location.ByteOffset = InputIsUtf8 ? Result.BytesConsumed : -1;
        location.LineNumber = CompletionLineNumber;
        location.ColumnNumber = CompletionColumnNumber;
        if (ProtocolVersionCount == 0)
        {
            AddDiagnostic(SdpDiagnosticCodes.ParseMissingRequiredField, SdpValidationSeverity.Error, SdpScope.Session, 'v', -1, 0, "The required SDP protocol-version field is missing.", location);
        }
        if (OriginCount == 0)
        {
            AddDiagnostic(SdpDiagnosticCodes.ParseMissingRequiredField, SdpValidationSeverity.Error, SdpScope.Session, 'o', -1, 0, "The required SDP origin field is missing.", location);
        }
        if (SessionNameCount == 0)
        {
            AddDiagnostic(SdpDiagnosticCodes.ParseMissingRequiredField, SdpValidationSeverity.Error, SdpScope.Session, 's', -1, 0, "The required SDP session-name field is missing.", location);
        }
        if (TimingFieldCount == 0)
        {
            AddDiagnostic(SdpDiagnosticCodes.ParseMissingRequiredField, SdpValidationSeverity.Error, SdpScope.Session, 't', -1, 0, "At least one required SDP timing field is missing.", location);
        }
    }
}
