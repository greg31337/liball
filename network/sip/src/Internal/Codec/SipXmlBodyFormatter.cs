namespace sip;

internal static class SipXmlBodyFormatter
{
    internal static SipWriteResult FormatBodyModel(SipXmlBody? body, SipWriteOptions? options)
    {
        if (body == null) return CreateError("An XML event body is required.");
        if (options == null) return CreateError("XML event body write options are required.");
        if (options.MaxHeaderLineBytes < 0)
            return CreateError("The maximum output line size cannot be negative.");
        SipValidationIssue[] issues = SipXmlBodyValidator.ValidateBodyModel(body, true);
        if (issues.Length > 0)
        {
            string? message = issues[0].Message;
            return CreateError(message == null ? "The XML event body is invalid." : message);
        }
        if (options.PreserveOriginal && body.OriginalBytes.Length > 0)
            return CreateSuccess(CopyBytes(body.OriginalBytes));
        string text = body.XmlText == null ? "" : body.XmlText;
        SipXmlMediaProfile? profile = SipXmlMediaProfiles.FindProfile(body.MediaType);
        if (profile != null && profile.MessageSummary)
        {
            text = SipMessageSummarySyntax.NormalizeSummaryText(text);
            string? summaryError = SipMessageSummarySyntax.ValidateSummaryText(
                text, false, options.MaxHeaderLineBytes);
            if (summaryError != null) return CreateError(summaryError);
        }
        return CreateSuccess(System.Text.Encoding.UTF8.GetBytes(text));
    }

    static byte[] CopyBytes(byte[] source)
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
