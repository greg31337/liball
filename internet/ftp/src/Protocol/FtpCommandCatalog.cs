using System;

namespace ftp;

internal static class FtpCommandCatalog
{
    internal static readonly FtpCommandDefinition[] Definitions = CreateDefinitions();

    internal static FtpCommandDefinition[] GetDefinitions()
    {
        FtpCommandDefinition[] copy = new FtpCommandDefinition[Definitions.Length];
        Array.Copy(Definitions, copy, Definitions.Length);
        return copy;
    }

    internal static FtpCommandDefinition? FindByKind(FtpCommandKind kind)
    {
        int index = 0;
        while (index < Definitions.Length)
        {
            if (Definitions[index].Kind == kind)
            {
                return Definitions[index];
            }
            index++;
        }
        return null;
    }

    internal static FtpCommandDefinition? FindByVerb(string? verb)
    {
        if (verb == null)
        {
            return null;
        }

        int index = 0;
        while (index < Definitions.Length)
        {
            FtpCommandDefinition definition = Definitions[index];
            if (definition.Kind != FtpCommandKind.Unknown &&
                string.Equals(definition.Verb, verb, StringComparison.OrdinalIgnoreCase))
            {
                return definition;
            }
            index++;
        }
        return null;
    }

    internal static FtpCommandDefinition? FindHistoricAliasForTarget(
        FtpCommandKind target)
    {
        int index = 0;
        while (index < Definitions.Length)
        {
            FtpCommandDefinition definition = Definitions[index];
            if (definition.HistoricAlias && definition.HasAliasTarget &&
                definition.AliasTarget == target)
            {
                return definition;
            }
            index++;
        }
        return null;
    }

    internal static FtpCommandKind ResolveKind(string? verb)
    {
        FtpCommandDefinition? definition = FindByVerb(verb);
        return definition == null ? FtpCommandKind.Unknown : definition.Kind;
    }

    internal static string ResolveVerb(FtpCommandKind kind)
    {
        FtpCommandDefinition? definition = FindByKind(kind);
        if (definition == null || definition.Kind == FtpCommandKind.Unknown)
        {
            throw new FtpProtocolException("The FTP command kind has no canonical verb.", null);
        }
        return definition.Verb;
    }

    internal static FtpCommandDefinition[] CreateDefinitions()
    {
        return new FtpCommandDefinition[]
        {
            CreateUnknownCommand(),
            CreateStandardCommand(FtpCommandKind.Abor, FtpCommandNames.Abor, false, false, true, true, false),
            CreateStandardCommand(FtpCommandKind.Acct, FtpCommandNames.Acct, true, true, false, false, false),
            CreateStandardCommand(FtpCommandKind.Allo, FtpCommandNames.Allo, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Appe, FtpCommandNames.Appe, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Cdup, FtpCommandNames.Cdup, false, false, true, false, false),
            CreateStandardCommand(FtpCommandKind.Cwd, FtpCommandNames.Cwd, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Dele, FtpCommandNames.Dele, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Help, FtpCommandNames.Help, false, true, false, false, false),
            CreateStandardCommand(FtpCommandKind.List, FtpCommandNames.List, false, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Mkd, FtpCommandNames.Mkd, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Mode, FtpCommandNames.Mode, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Nlst, FtpCommandNames.Nlst, false, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Noop, FtpCommandNames.Noop, false, false, false, true, false),
            CreateStandardCommand(FtpCommandKind.Pass, FtpCommandNames.Pass, true, true, false, false, false),
            CreateStandardCommand(FtpCommandKind.Pasv, FtpCommandNames.Pasv, false, false, true, false, false),
            CreateStandardCommand(FtpCommandKind.Port, FtpCommandNames.Port, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Quit, FtpCommandNames.Quit, false, false, false, true, false),
            CreateStandardCommand(FtpCommandKind.Rein, FtpCommandNames.Rein, false, false, false, false, false),
            CreateStandardCommand(FtpCommandKind.Rest, FtpCommandNames.Rest, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Retr, FtpCommandNames.Retr, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Rmd, FtpCommandNames.Rmd, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Rnfr, FtpCommandNames.Rnfr, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Rnto, FtpCommandNames.Rnto, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Site, FtpCommandNames.Site, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Smnt, FtpCommandNames.Smnt, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Stat, FtpCommandNames.Stat, false, true, true, true, false),
            CreateStandardCommand(FtpCommandKind.Stor, FtpCommandNames.Stor, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Stou, FtpCommandNames.Stou, false, false, true, false, false),
            CreateStandardCommand(FtpCommandKind.Stru, FtpCommandNames.Stru, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Syst, FtpCommandNames.Syst, false, false, false, false, false),
            CreateStandardCommand(FtpCommandKind.Type, FtpCommandNames.Type, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.User, FtpCommandNames.User, true, true, false, false, false),
            CreateStandardCommand(FtpCommandKind.Pwd, FtpCommandNames.Pwd, false, false, true, false, false),
            CreateStandardCommand(FtpCommandKind.Feat, FtpCommandNames.Feat, false, false, false, false, false),
            CreateStandardCommand(FtpCommandKind.Opts, FtpCommandNames.Opts, true, true, false, false, false),
            CreateStandardCommand(FtpCommandKind.Eprt, FtpCommandNames.Eprt, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Epsv, FtpCommandNames.Epsv, false, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Lang, FtpCommandNames.Lang, false, true, false, true, false),
            CreateStandardCommand(FtpCommandKind.Mdtm, FtpCommandNames.Mdtm, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Mlst, FtpCommandNames.Mlst, false, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Mlsd, FtpCommandNames.Mlsd, false, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Size, FtpCommandNames.Size, true, true, true, false, false),
            CreateStandardCommand(FtpCommandKind.Auth, FtpCommandNames.Auth, true, true, false, false, true),
            CreateStandardCommand(FtpCommandKind.Pbsz, FtpCommandNames.Pbsz, true, true, false, false, true),
            CreateStandardCommand(FtpCommandKind.Prot, FtpCommandNames.Prot, true, true, false, false, true),
            CreateStandardCommand(FtpCommandKind.Ccc, FtpCommandNames.Ccc, false, false, false, false, true),
            CreateStandardCommand(FtpCommandKind.Adat, FtpCommandNames.Adat, true, true, false, false, true),
            CreateStandardCommand(FtpCommandKind.Mic, FtpCommandNames.Mic, true, true, false, true, true),
            CreateStandardCommand(FtpCommandKind.Conf, FtpCommandNames.Conf, true, true, false, true, true),
            CreateStandardCommand(FtpCommandKind.Enc, FtpCommandNames.Enc, true, true, false, true, true),
            CreateStandardCommand(FtpCommandKind.Host, FtpCommandNames.Host, true, true, false, false, false),
            CreateStandardCommand(FtpCommandKind.Algs, FtpCommandNames.Algs, true, true, false, false, false),
            CreateObsoleteCommand(FtpCommandKind.Lprt, FtpCommandNames.Lprt, true, true),
            CreateObsoleteCommand(FtpCommandKind.Lpsv, FtpCommandNames.Lpsv, false, false),
            CreateHistoricAlias(FtpCommandKind.Xcup, FtpCommandNames.Xcup, false, false, FtpCommandKind.Cdup),
            CreateHistoricAlias(FtpCommandKind.Xcwd, FtpCommandNames.Xcwd, true, true, FtpCommandKind.Cwd),
            CreateHistoricAlias(FtpCommandKind.Xmkd, FtpCommandNames.Xmkd, true, true, FtpCommandKind.Mkd),
            CreateHistoricAlias(FtpCommandKind.Xpwd, FtpCommandNames.Xpwd, false, false, FtpCommandKind.Pwd),
            CreateHistoricAlias(FtpCommandKind.Xrmd, FtpCommandNames.Xrmd, true, true, FtpCommandKind.Rmd),
            CreateCompatibilityCommand(FtpCommandKind.Pret, FtpCommandNames.Pret, true, true, true),
            CreateCompatibilityCommand(FtpCommandKind.Clnt, FtpCommandNames.Clnt, true, true, false),
            CreateCompatibilityCommand(FtpCommandKind.Mfmt, FtpCommandNames.Mfmt, true, true, true),
            CreateCompatibilityCommand(FtpCommandKind.Hash, FtpCommandNames.Hash, true, true, true),
            CreateCompatibilityCommand(FtpCommandKind.Md5, FtpCommandNames.Md5, true, true, true),
            CreateCompatibilityCommand(FtpCommandKind.Xmd5, FtpCommandNames.Xmd5, true, true, true),
            CreateCompatibilityCommand(FtpCommandKind.Xsha1, FtpCommandNames.Xsha1, true, true, true),
            CreateCompatibilityCommand(FtpCommandKind.Xsha256, FtpCommandNames.Xsha256, true, true, true),
            CreateCompatibilityCommand(FtpCommandKind.Xsha512, FtpCommandNames.Xsha512, true, true, true),
            CreateCompatibilityCommand(FtpCommandKind.Xcrc, FtpCommandNames.Xcrc, true, true, true),
            CreateCompatibilityCommand(FtpCommandKind.Rmda, FtpCommandNames.Rmda, true, true, true)
        };
    }

    internal static FtpCommandDefinition CreateUnknownCommand()
    {
        return new FtpCommandDefinition();
    }

    internal static FtpCommandDefinition CreateStandardCommand(
        FtpCommandKind kind, string verb, bool argumentRequired, bool argumentAllowed,
        bool requiresAuthentication, bool availableDuringTransfer, bool securityCommand)
    {
        FtpCommandDefinition definition = CreateBaseCommand(
            kind, verb, argumentRequired, argumentAllowed,
            requiresAuthentication, availableDuringTransfer);
        definition.Standard = true;
        definition.SecurityCommand = securityCommand;
        return definition;
    }

    internal static FtpCommandDefinition CreateObsoleteCommand(
        FtpCommandKind kind, string verb, bool argumentRequired, bool argumentAllowed)
    {
        FtpCommandDefinition definition = CreateBaseCommand(
            kind, verb, argumentRequired, argumentAllowed, true, false);
        definition.Standard = true;
        definition.Obsolete = true;
        return definition;
    }

    internal static FtpCommandDefinition CreateHistoricAlias(
        FtpCommandKind kind, string verb, bool argumentRequired,
        bool argumentAllowed, FtpCommandKind aliasTarget)
    {
        FtpCommandDefinition definition = CreateBaseCommand(
            kind, verb, argumentRequired, argumentAllowed, true, false);
        definition.HistoricAlias = true;
        definition.HasAliasTarget = true;
        definition.AliasTarget = aliasTarget;
        return definition;
    }

    internal static FtpCommandDefinition CreateCompatibilityCommand(
        FtpCommandKind kind, string verb, bool argumentRequired,
        bool argumentAllowed, bool requiresAuthentication)
    {
        FtpCommandDefinition definition = CreateBaseCommand(
            kind, verb, argumentRequired, argumentAllowed,
            requiresAuthentication, false);
        definition.Compatibility = true;
        return definition;
    }

    internal static FtpCommandDefinition CreateBaseCommand(
        FtpCommandKind kind, string verb, bool argumentRequired, bool argumentAllowed,
        bool requiresAuthentication, bool availableDuringTransfer)
    {
        FtpCommandDefinition definition = new FtpCommandDefinition();
        definition.Kind = kind;
        definition.Verb = verb;
        definition.ArgumentRequired = argumentRequired;
        definition.ArgumentAllowed = argumentAllowed;
        definition.RequiresAuthentication = requiresAuthentication;
        definition.AvailableDuringTransfer = availableDuringTransfer;
        return definition;
    }
}
