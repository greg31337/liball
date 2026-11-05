namespace ftp;

internal static class FtpReplyClassifier
{
    internal static FtpReplyClassification ClassifyCode(int code)
    {
        FtpReplyClassification classification = new FtpReplyClassification();
        if (code < 100 || code > 999)
        {
            classification.Malformed = true;
            return classification;
        }

        int categoryDigit = code / 100;
        if (categoryDigit == 1)
        {
            classification.Category = FtpReplyCategory.PositivePreliminary;
        }
        else if (categoryDigit == 2)
        {
            classification.Category = FtpReplyCategory.PositiveCompletion;
            classification.Succeeded = true;
        }
        else if (categoryDigit == 3)
        {
            classification.Category = FtpReplyCategory.PositiveIntermediate;
        }
        else if (categoryDigit == 4)
        {
            classification.Category = FtpReplyCategory.TransientNegativeCompletion;
            classification.TransientFailure = true;
        }
        else if (categoryDigit == 5)
        {
            classification.Category = FtpReplyCategory.PermanentNegativeCompletion;
            classification.PermanentFailure = true;
        }
        else
        {
            classification.Malformed = true;
        }

        int kindDigit = (code / 10) % 10;
        if (kindDigit == 0)
        {
            classification.Kind = FtpReplyKind.Syntax;
        }
        else if (kindDigit == 1)
        {
            classification.Kind = FtpReplyKind.Information;
        }
        else if (kindDigit == 2)
        {
            classification.Kind = FtpReplyKind.Connections;
        }
        else if (kindDigit == 3)
        {
            classification.Kind = FtpReplyKind.AuthenticationAndAccounting;
        }
        else if (kindDigit == 4)
        {
            classification.Kind = FtpReplyKind.Unspecified;
        }
        else if (kindDigit == 5)
        {
            classification.Kind = FtpReplyKind.FileSystem;
        }
        else
        {
            classification.Malformed = true;
        }

        if ((classification.TransientFailure || classification.PermanentFailure) &&
            classification.Kind == FtpReplyKind.AuthenticationAndAccounting)
        {
            classification.AuthenticationFailure = true;
        }

        return classification;
    }

    internal static FtpReplyClassification ApplyClassification(FtpReply? reply)
    {
        if (reply == null)
        {
            throw new System.ArgumentNullException("reply");
        }

        FtpReplyClassification classification = ClassifyCode(reply.Code);
        reply.Classification = classification;
        return classification;
    }
}
