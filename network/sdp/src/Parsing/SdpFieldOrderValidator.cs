namespace sdp;

internal static class SdpFieldOrderValidator
{
    internal static bool IsKnownField(char fieldName)
    {
        return fieldName == 'v' || fieldName == 'o' || fieldName == 's' || fieldName == 'i' || fieldName == 'u' || fieldName == 'e' || fieldName == 'p' || fieldName == 'c' || fieldName == 'b' || fieldName == 't' || fieldName == 'r' || fieldName == 'z' || fieldName == 'k' || fieldName == 'a' || fieldName == 'm';
    }

    internal static void ValidateFieldOrder(SdpParseContext context, SdpRawLine line, char fieldName)
    {
        if (fieldName == 'm')
        {
            ValidateMediaBoundary(context, line);
            return;
        }
        if (context.Scope == SdpScope.Session)
        {
            ValidateSessionFieldOrder(context, line, fieldName);
            return;
        }
        ValidateMediaFieldOrder(context, line, fieldName);
    }

    internal static void ValidateMediaBoundary(SdpParseContext context, SdpRawLine line)
    {
        if (context.MediaIndex == 0 && (context.ProtocolVersionCount == 0 || context.OriginCount == 0 || context.SessionNameCount == 0 || context.TimingFieldCount == 0))
        {
            AddFieldOrderDiagnostic(context, line, 'm', ReadOccurrenceIndex(context, 'm'), "A media description cannot begin before the required session fields and timing description.");
        }
    }

    internal static void ValidateSessionFieldOrder(SdpParseContext context, SdpRawLine line, char fieldName)
    {
        int stage = ReadSessionFieldStage(fieldName);
        if (stage == 0)
        {
            return;
        }
        if (fieldName != 'v' && fieldName != 'r' && fieldName != 'z' && stage < context.SessionOrderStage)
        {
            AddFieldOrderDiagnostic(context, line, fieldName, ReadOccurrenceIndex(context, fieldName), "The SDP session field appears after a field that must follow it.");
        }
        if (stage > context.SessionOrderStage)
        {
            context.SessionOrderStage = stage;
        }
    }

    internal static void ValidateMediaFieldOrder(SdpParseContext context, SdpRawLine line, char fieldName)
    {
        int stage = ReadMediaFieldStage(fieldName);
        if (stage == 0)
        {
            return;
        }
        if (stage < context.MediaOrderStage)
        {
            AddFieldOrderDiagnostic(context, line, fieldName, ReadOccurrenceIndex(context, fieldName), "The SDP media field appears after a field that must follow it.");
        }
        if (stage > context.MediaOrderStage)
        {
            context.MediaOrderStage = stage;
        }
    }

    internal static int ReadSessionFieldStage(char fieldName)
    {
        if (fieldName == 'v')
        {
            return 1;
        }
        if (fieldName == 'o')
        {
            return 2;
        }
        if (fieldName == 's')
        {
            return 3;
        }
        if (fieldName == 'i')
        {
            return 4;
        }
        if (fieldName == 'u')
        {
            return 5;
        }
        if (fieldName == 'e')
        {
            return 6;
        }
        if (fieldName == 'p')
        {
            return 7;
        }
        if (fieldName == 'c')
        {
            return 8;
        }
        if (fieldName == 'b')
        {
            return 9;
        }
        if (fieldName == 't' || fieldName == 'r' || fieldName == 'z')
        {
            return 10;
        }
        if (fieldName == 'k')
        {
            return 11;
        }
        if (fieldName == 'a')
        {
            return 12;
        }
        return 0;
    }

    internal static int ReadMediaFieldStage(char fieldName)
    {
        if (fieldName == 'i')
        {
            return 1;
        }
        if (fieldName == 'c')
        {
            return 2;
        }
        if (fieldName == 'b')
        {
            return 3;
        }
        if (fieldName == 'k')
        {
            return 4;
        }
        if (fieldName == 'a')
        {
            return 5;
        }
        return 0;
    }

    internal static void AddFieldOrderDiagnostic(SdpParseContext context, SdpRawLine line, char fieldName, int occurrenceIndex, string detail)
    {
        SdpValidationSeverity severity = SdpValidationSeverity.Warning;
        if (context.Options.Mode == SdpParseMode.Strict)
        {
            severity = SdpValidationSeverity.Error;
        }
        context.AddDiagnostic(SdpDiagnosticCodes.ParseFieldOutOfOrder, severity, context.Scope, fieldName, context.MediaIndex, occurrenceIndex, detail, line.Location);
    }

    internal static void AddUnknownFieldDiagnostic(SdpParseContext context, SdpRawLine line, char fieldName)
    {
        SdpValidationSeverity severity = SdpValidationSeverity.Warning;
        if (context.Options.Mode == SdpParseMode.Strict)
        {
            severity = SdpValidationSeverity.Error;
        }
        context.AddDiagnostic(SdpDiagnosticCodes.ParseUnknownField, severity, context.Scope, fieldName, context.MediaIndex, 0, "The SDP field discriminator is not defined by the supported grammar.", line.Location);
    }

    internal static int ReadOccurrenceIndex(SdpParseContext context, char fieldName)
    {
        if (fieldName == 'm')
        {
            return context.MediaIndex;
        }
        if (fieldName == 'v')
        {
            return context.ProtocolVersionCount;
        }
        if (fieldName == 'o')
        {
            return context.OriginCount;
        }
        if (fieldName == 's')
        {
            return context.SessionNameCount;
        }
        if (fieldName == 'i')
        {
            if (context.Scope == SdpScope.Media)
            {
                return context.MediaInformationCount;
            }
            return context.SessionInformationCount;
        }
        if (fieldName == 'u')
        {
            return context.SessionUriCount;
        }
        if (fieldName == 'e')
        {
            return context.Description.Emails.Count;
        }
        if (fieldName == 'p')
        {
            return context.Description.Phones.Count;
        }
        if (fieldName == 'c')
        {
            if (context.Scope == SdpScope.Media)
            {
                return context.MediaConnectionCount;
            }
            return context.SessionConnectionCount;
        }
        if (fieldName == 'b')
        {
            if (context.Scope == SdpScope.Media)
            {
                return context.MediaBandwidthCount;
            }
            return context.SessionBandwidthCount;
        }
        if (fieldName == 't')
        {
            return context.TimingFieldCount;
        }
        if (fieldName == 'r')
        {
            return context.RepeatFieldCount;
        }
        if (fieldName == 'z')
        {
            return context.ZoneFieldCount;
        }
        if (fieldName == 'a')
        {
            if (context.Scope == SdpScope.Media)
            {
                return context.MediaAttributeCount - 1;
            }
            return context.SessionAttributeCount - 1;
        }
        if (fieldName == 'k')
        {
            if (context.Scope == SdpScope.Media)
            {
                return context.MediaEncryptionKeyCount;
            }
            return context.SessionEncryptionKeyCount;
        }
        return 0;
    }
}
