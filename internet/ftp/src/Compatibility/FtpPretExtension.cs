namespace ftp;

internal static class FtpPretExtension
{
    internal static FtpCommand ParseIntent(
        string? argument,
        FtpSequenceState state)
    {
        if (string.IsNullOrEmpty(argument))
        {
            throw new FtpProtocolException(
                "PRET requires an upcoming data-transfer command.", null);
        }
        FtpCommand intent = FtpCommandParser.ParseDecodedCommand(argument);
        FtpCommandDefinition? definition =
            FtpCommandCatalog.FindByKind(intent.Kind);
        if (definition == null ||
            !FtpReplyCoordinator.IsDataCommand(intent.Kind) ||
            (definition.ArgumentRequired &&
                string.IsNullOrEmpty(intent.Argument)) ||
            (!definition.ArgumentAllowed && intent.Argument != null))
        {
            throw new FtpProtocolException(
                "PRET accepts only a complete FTP data-transfer command.",
                null);
        }
        FtpSequenceValidationResult sequence =
            FtpSequenceValidator.InspectCommand(intent, state);
        if (!sequence.Allowed)
        {
            throw new FtpProtocolException(
                "The PRET transfer intent is not valid in this session.",
                null);
        }
        return intent;
    }

    internal static bool MatchesIntent(
        FtpCommand? intent,
        FtpCommand command)
    {
        return intent != null && intent.Kind == command.Kind &&
            string.Equals(
                intent.Argument,
                command.Argument,
                System.StringComparison.Ordinal);
    }

    internal static bool IsExtendedPassiveAll(FtpCommand command)
    {
        return command.Kind == FtpCommandKind.Epsv &&
            string.Equals(
                command.Argument,
                "ALL",
                System.StringComparison.OrdinalIgnoreCase);
    }
}
