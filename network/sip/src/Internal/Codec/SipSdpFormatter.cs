namespace sip;

internal static class SipSdpFormatter
{
    internal static SipWriteResult FormatDescriptionModel(SipSessionDescription? description,
        SipWriteOptions? options)
    {
        if (description == null) return CreateError("An SDP session description is required.");
        if (options == null) return CreateError("SDP write options are required.");
        if (options.MaxHeaderLineBytes < 0)
            return CreateError("The maximum SDP line size cannot be negative.");
        SipValidationIssue[] issues = SipSdpValidator.ValidateDescriptionModel(description, true);
        if (issues.Length > 0)
        {
            string? message = issues[0].Message;
            return CreateError(message == null ? "The SDP session description is invalid." : message);
        }
        if (options.PreserveOriginal && description.OriginalBytes.Length > 0)
            return CreateSuccess(CopyOriginalBytes(description.OriginalBytes));

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        AddLine(builder, "v=0");
        AddLine(builder, "o=" + description.Origin);
        AddLine(builder, "s=" + description.SessionName);
        if (description.SessionInformation != null)
            AddLine(builder, "i=" + description.SessionInformation);
        if (description.Uri != null) AddLine(builder, "u=" + description.Uri);
        AddStringLines(builder, "e=", description.Emails);
        AddStringLines(builder, "p=", description.Phones);
        if (description.Connection != null) AddLine(builder, "c=" + description.Connection);
        AddStringLines(builder, "b=", description.Bandwidths);
        AddStringLines(builder, "t=", description.Timings);
        AddStringLines(builder, "r=", description.RepeatTimes);
        if (description.TimeZones != null) AddLine(builder, "z=" + description.TimeZones);
        if (description.EncryptionKey != null) AddLine(builder, "k=" + description.EncryptionKey);
        AddAttributeLines(builder, description.Attributes);
        AddStringLines(builder, "", description.UnknownLines);
        int mediaIndex = 0;
        while (mediaIndex < description.MediaDescriptions.Length)
        {
            AddMediaLines(builder, description.MediaDescriptions[mediaIndex]);
            mediaIndex = mediaIndex + 1;
        }
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
        if (!LinesWithinLimit(bytes, options.MaxHeaderLineBytes))
            return CreateError("An SDP output line exceeds the configured maximum size.");
        return CreateSuccess(bytes);
    }

    static void AddMediaLines(System.Text.StringBuilder builder, SipMediaDescription media)
    {
        string port = media.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (media.PortCount != 1)
            port = port + "/" + media.PortCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        System.Text.StringBuilder line = new System.Text.StringBuilder();
        line.Append("m=");
        line.Append(media.Media);
        line.Append(' ');
        line.Append(port);
        line.Append(' ');
        line.Append(media.Protocol);
        int formatIndex = 0;
        while (formatIndex < media.Formats.Length)
        {
            line.Append(' ');
            line.Append(media.Formats[formatIndex]);
            formatIndex = formatIndex + 1;
        }
        AddLine(builder, line.ToString());
        if (media.Title != null) AddLine(builder, "i=" + media.Title);
        if (media.Connection != null) AddLine(builder, "c=" + media.Connection);
        AddStringLines(builder, "b=", media.Bandwidths);
        if (media.EncryptionKey != null) AddLine(builder, "k=" + media.EncryptionKey);
        AddAttributeLines(builder, media.Attributes);
        AddStringLines(builder, "", media.UnknownLines);
    }

    static void AddAttributeLines(System.Text.StringBuilder builder, SipSdpAttribute[] attributes)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            SipSdpAttribute attribute = attributes[index];
            string line = "a=" + attribute.Name;
            if (attribute.HasValue) line = line + ":" + attribute.Value;
            AddLine(builder, line);
            index = index + 1;
        }
    }

    static void AddStringLines(System.Text.StringBuilder builder, string prefix, string[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            AddLine(builder, prefix + values[index]);
            index = index + 1;
        }
    }

    static void AddLine(System.Text.StringBuilder builder, string line)
    {
        builder.Append(line);
        builder.Append("\r\n");
    }

    static bool LinesWithinLimit(byte[] bytes, int maximum)
    {
        int lineLength = 0;
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] == (byte)'\r' && index + 1 < bytes.Length
                && bytes[index + 1] == (byte)'\n')
            {
                if (lineLength > maximum) return false;
                lineLength = 0;
                index = index + 2;
            }
            else
            {
                lineLength = lineLength + 1;
                index = index + 1;
            }
        }
        return lineLength == 0;
    }

    static byte[] CopyOriginalBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        System.Array.Copy(source, result, source.Length);
        return result;
    }

    static SipWriteResult CreateSuccess(byte[] bytes)
    {
        SipWriteResult result = new SipWriteResult();
        result.Success = true;
        result.Bytes = bytes;
        result.Written = bytes.Length;
        result.RequiredBytes = bytes.Length;
        return result;
    }

    static SipWriteResult CreateError(string message)
    {
        SipWriteResult result = new SipWriteResult();
        result.ErrorMessage = message;
        return result;
    }
}
