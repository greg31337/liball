namespace sip;

internal static class SipMimeSyntax
{
    internal static bool IsMultipartType(SipMediaType? mediaType)
    {
        return mediaType != null && SipAscii.EqualsIgnoreCase(mediaType.Type, "multipart");
    }

    internal static bool IsSipFragmentType(SipMediaType? mediaType)
    {
        return mediaType != null && SipAscii.EqualsIgnoreCase(mediaType.Type, "message")
            && SipAscii.EqualsIgnoreCase(mediaType.Subtype, "sipfrag");
    }

    internal static string? GetBoundaryValue(SipMediaType? mediaType)
    {
        if (mediaType == null || mediaType.Parameters == null) return null;
        int index = 0;
        string? boundary = null;
        while (index < mediaType.Parameters.Length)
        {
            SipUriParameter? parameter = mediaType.Parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, "boundary"))
            {
                if (boundary != null || !parameter.HasValue) return null;
                boundary = parameter.Value;
            }
            index = index + 1;
        }
        return boundary;
    }

    internal static bool IsBoundaryValue(string? boundary)
    {
        if (boundary == null || boundary.Length == 0 || boundary.Length > 70
            || boundary[boundary.Length - 1] == ' ') return false;
        int index = 0;
        while (index < boundary.Length)
        {
            int value = boundary[index];
            if (!IsBoundaryCharacter(value)) return false;
            index = index + 1;
        }
        return true;
    }

    internal static string GetEffectiveDisposition(SipMimeEntity entity)
    {
        SipContentDisposition? disposition = entity.ContentDisposition;
        if (disposition != null)
        {
            switch (disposition.Disposition)
            {
                case SipBodyDisposition.Session: return "session";
                case SipBodyDisposition.Render: return "render";
                case SipBodyDisposition.Icon: return "icon";
                case SipBodyDisposition.Alert: return "alert";
                case SipBodyDisposition.Signal: return "signal";
                case SipBodyDisposition.ByReference: return "by-reference";
                case SipBodyDisposition.RecordingSession: return "recording-session";
                default: return disposition.DispositionToken == null
                    ? "" : disposition.DispositionToken.ToLowerInvariant();
            }
        }
        if (entity.ContentType != null && SipAscii.EqualsIgnoreCase(entity.ContentType.Type, "application")
            && SipAscii.EqualsIgnoreCase(entity.ContentType.Subtype, "sdp")) return "session";
        return "render";
    }

    internal static string? GetHandlingValue(SipContentDisposition? disposition)
    {
        if (disposition == null || disposition.Parameters == null) return "required";
        string? handling = null;
        int index = 0;
        while (index < disposition.Parameters.Length)
        {
            SipUriParameter? parameter = disposition.Parameters[index];
            if (parameter != null && SipAscii.EqualsIgnoreCase(parameter.Name, "handling"))
            {
                if (handling != null || !parameter.HasValue) return null;
                handling = parameter.Value;
            }
            index = index + 1;
        }
        return handling == null ? "required" : handling;
    }

    internal static byte[] CopyByteRange(byte[] source, int start, int count)
    {
        byte[] result = new byte[count];
        if (count > 0) System.Array.Copy(source, start, result, 0, count);
        return result;
    }

    internal static bool HeaderNameIs(string? actual, string expected, string? compact)
    {
        return SipAscii.EqualsIgnoreCase(actual, expected)
            || compact != null && SipAscii.EqualsIgnoreCase(actual, compact);
    }

    static bool IsBoundaryCharacter(int value)
    {
        return value >= '0' && value <= '9' || value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z' || value == '\'' || value == '(' || value == ')'
            || value == '+' || value == '_' || value == ',' || value == '-' || value == '.'
            || value == '/' || value == ':' || value == '=' || value == '?' || value == ' ';
    }
}
