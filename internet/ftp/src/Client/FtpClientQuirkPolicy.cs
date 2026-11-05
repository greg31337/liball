namespace ftp;

internal class FtpClientQuirkPolicy
{
    internal FtpClientQuirkOptions Options;

    internal FtpClientQuirkPolicy(FtpClientQuirkOptions? options)
    {
        if (options == null)
        {
            throw new FtpConfigurationException(
                "FTP client quirk options are required.", null);
        }
        Options = options;
    }

    internal bool ShouldFallbackFromMlsd(FtpReply? reply)
    {
        return Options.AllowMlsdFallback && IsNegativeCompletion(reply);
    }

    internal bool ShouldFallbackFromSize(FtpReply? reply)
    {
        return Options.AllowSizeFallback && IsNegativeCompletion(reply);
    }

    internal bool ShouldFallbackFromMdtm(FtpReply? reply)
    {
        return Options.AllowMdtmFallback && IsNegativeCompletion(reply);
    }

    internal bool ShouldFallbackToHistoricAlias(
        FtpCommandKind commandKind,
        FtpReply? reply)
    {
        if (!Options.AllowHistoricAliasFallback || reply == null ||
            FtpCommandCatalog.FindHistoricAliasForTarget(commandKind) == null)
        {
            return false;
        }
        return reply.Code == FtpReplyCodes.SyntaxError ||
            reply.Code == FtpReplyCodes.CommandNotImplemented ||
            reply.Code == FtpReplyCodes.CommandParameterNotImplemented;
    }

    internal bool IsNegativeCompletion(FtpReply? reply)
    {
        if (reply == null)
        {
            return false;
        }
        FtpReplyClassification classification =
            FtpReplyClassifier.ClassifyCode(reply.Code);
        return classification.TransientFailure ||
            classification.PermanentFailure;
    }
}
