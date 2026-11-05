using System;
using System.Collections.Generic;
using System.Text;

namespace ftp;

internal static class FtpFeatureCodec
{
    internal static FtpFeatureSet ParseFeatureReply(FtpReply? reply)
    {
        return ParseFeatureReplyCore(reply, true);
    }

    internal static FtpFeatureSet ParseLenientFeatureReply(FtpReply? reply)
    {
        return ParseFeatureReplyCore(reply, false);
    }

    internal static FtpFeatureSet ParseFeatureReplyCore(
        FtpReply? reply,
        bool strict)
    {
        FtpReply checkedReply = FtpReplyExtractor.RequireReply(reply, "FEAT");
        FtpFeatureSet featureSet = new FtpFeatureSet();
        featureSet.SourceReply = checkedReply;
        FtpReplyClassification classification = FtpReplyClassifier.ClassifyCode(checkedReply.Code);
        if (classification.TransientFailure || classification.PermanentFailure)
        {
            return featureSet;
        }
        if (checkedReply.Code != FtpReplyCodes.SystemStatus)
        {
            throw new FtpProtocolException("A successful FTP FEAT response must use reply code 211.", null);
        }
        if (!checkedReply.Multiline)
        {
            featureSet.Negotiated = true;
            return featureSet;
        }
        if (checkedReply.Lines == null || checkedReply.Lines.Length < 2)
        {
            throw new FtpProtocolException("An FTP FEAT response requires opening and terminating lines.", null);
        }

        FtpReplyLine opening = RequireFeatureReplyLine(checkedReply.Lines[0], "opening");
        FtpReplyLine terminal = RequireFeatureReplyLine(
            checkedReply.Lines[checkedReply.Lines.Length - 1], "terminating");
        if (!opening.HasCode || opening.Code != FtpReplyCodes.SystemStatus ||
            !terminal.HasCode || terminal.Code != FtpReplyCodes.SystemStatus)
        {
            throw new FtpProtocolException("An FTP FEAT response must use code 211 on its boundary lines.", null);
        }
        bool standardTerminator = string.Equals(
            terminal.Text, "End", StringComparison.OrdinalIgnoreCase);
        bool compatibleTerminator = !strict && terminal.Text.StartsWith(
            "End ", StringComparison.OrdinalIgnoreCase);
        if (!standardTerminator && !compatibleTerminator)
        {
            throw new FtpProtocolException("A multiline FTP FEAT response must terminate with 211 End.", null);
        }

        int featureCount = checkedReply.Lines.Length - 2;
        if (featureCount == 0)
        {
            throw new FtpProtocolException("A multiline FTP FEAT response must advertise at least one feature.", null);
        }
        if (featureCount > FtpDefaults.MaximumFeatureCount)
        {
            throw new FtpProtocolException("The FTP FEAT response exceeded the configured feature limit.", null);
        }

        FtpFeature[] features = new FtpFeature[featureCount];
        int index = 0;
        while (index < featureCount)
        {
            FtpReplyLine line = RequireFeatureReplyLine(checkedReply.Lines[index + 1], "feature");
            string rawLine = line.RawText;
            if (rawLine.Length == 0)
            {
                rawLine = line.Text;
            }
            features[index] = ParseFeatureLine(rawLine);
            index++;
        }

        featureSet.Features = features;
        featureSet.Negotiated = true;
        return featureSet;
    }

    internal static FtpReply GenerateFeatureReply(
        FtpFeatureSet? featureSet, string? openingText, string? terminalText)
    {
        FtpFeatureSet checkedSet = RequireFeatureSet(featureSet);
        if (openingText == null)
        {
            throw new ArgumentNullException("openingText");
        }
        if (terminalText == null)
        {
            throw new ArgumentNullException("terminalText");
        }
        ValidateAsciiPrintable(openingText, true, "FEAT opening text");
        ValidateAsciiPrintable(terminalText, true, "FEAT terminating text");
        if (checkedSet.Features.Length > FtpDefaults.MaximumFeatureCount)
        {
            throw new FtpProtocolException("The generated FTP FEAT response exceeded the configured feature limit.", null);
        }

        if (checkedSet.Features.Length == 0)
        {
            FtpReply emptyReply = new FtpReply();
            emptyReply.Code = FtpReplyCodes.SystemStatus;
            emptyReply.Text = terminalText;
            emptyReply.Classification = FtpReplyClassifier.ClassifyCode(emptyReply.Code);
            return emptyReply;
        }
        if (!string.Equals(terminalText, "End", StringComparison.OrdinalIgnoreCase))
        {
            throw new FtpProtocolException("A generated multiline FTP FEAT response must terminate with 211 End.", null);
        }

        FtpReplyLine[] lines = new FtpReplyLine[checkedSet.Features.Length + 2];
        FtpReplyLine opening = new FtpReplyLine();
        opening.HasCode = true;
        opening.Code = FtpReplyCodes.SystemStatus;
        opening.Text = openingText;
        opening.RawText = $"211-{openingText}";
        lines[0] = opening;

        int index = 0;
        while (index < checkedSet.Features.Length)
        {
            FtpFeature feature = RequireFeature(checkedSet.Features[index]);
            string rawLine = " " + FormatFeatureLine(feature);
            FtpReplyLine line = new FtpReplyLine();
            line.Text = rawLine;
            line.RawText = rawLine;
            lines[index + 1] = line;
            index++;
        }

        FtpReplyLine terminal = new FtpReplyLine();
        terminal.HasCode = true;
        terminal.Code = FtpReplyCodes.SystemStatus;
        terminal.Text = terminalText;
        terminal.RawText = $"211 {terminalText}";
        terminal.TerminatesMultiline = true;
        lines[lines.Length - 1] = terminal;

        FtpReply reply = new FtpReply();
        reply.Code = FtpReplyCodes.SystemStatus;
        reply.Text = terminalText;
        reply.Lines = lines;
        reply.Multiline = true;
        reply.Classification = FtpReplyClassifier.ClassifyCode(reply.Code);
        return reply;
    }

    internal static FtpFeature ParseFeatureLine(string? rawLine)
    {
        if (string.IsNullOrEmpty(rawLine) || rawLine[0] != ' ')
        {
            throw new FtpProtocolException("An FTP FEAT feature line must begin with one space.", null);
        }
        if (rawLine.Length == 1 || rawLine[1] == ' ')
        {
            throw new FtpProtocolException("An FTP FEAT feature line must contain a feature label after one space.", null);
        }
        ValidateAsciiPrintable(rawLine, false, "FEAT feature line");

        string content = rawLine.Substring(1);
        int nameLength = FindCompoundFeatureNameLength(content);
        if (nameLength == 0)
        {
            int separator = content.IndexOf(' ');
            nameLength = separator < 0 ? content.Length : separator;
        }

        string name = NormalizeFeatureName(content.Substring(0, nameLength));
        string? parameters = null;
        if (content.Length > nameLength)
        {
            if (content[nameLength] != ' ')
            {
                throw new FtpProtocolException("An FTP FEAT label and parameter section must be separated by a space.", null);
            }
            parameters = content.Substring(nameLength + 1);
            if (parameters.Length == 0)
            {
                throw new FtpProtocolException("An FTP FEAT parameter section cannot be empty.", null);
            }
        }

        FtpFeature feature = new FtpFeature();
        feature.Name = name;
        feature.Parameters = parameters;
        feature.RawLine = rawLine;
        feature.Support = FtpFeatureSupport.Supported;
        return feature;
    }

    internal static string FormatFeatureLine(FtpFeature? feature)
    {
        FtpFeature checkedFeature = RequireFeature(feature);
        string name = NormalizeFeatureName(checkedFeature.Name);
        if (checkedFeature.Parameters == null)
        {
            return name;
        }

        ValidateAsciiPrintable(checkedFeature.Parameters, true, "FEAT parameters");
        return name + " " + checkedFeature.Parameters;
    }

    internal static string[] ExtractAuthenticationMechanisms(FtpFeatureSet? featureSet)
    {
        FtpFeatureSet checkedSet = RequireFeatureSet(featureSet);
        List<string> mechanisms = new List<string>();
        int featureIndex = 0;
        while (featureIndex < checkedSet.Features.Length)
        {
            FtpFeature feature = RequireFeature(checkedSet.Features[featureIndex]);
            if (string.Equals(feature.Name, FtpFeatureNames.Auth, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(feature.Parameters))
                {
                    throw new FtpProtocolException("An FTP AUTH feature must advertise at least one mechanism.", null);
                }
                AppendAuthenticationMechanisms(mechanisms, feature.Parameters);
            }
            featureIndex++;
        }
        return mechanisms.ToArray();
    }

    internal static FtpMlstFeatureInfo ExtractMlstFacts(FtpFeatureSet? featureSet)
    {
        FtpFeatureSet checkedSet = RequireFeatureSet(featureSet);
        FtpMlstFeatureInfo info = new FtpMlstFeatureInfo();
        List<string> facts = new List<string>();
        List<string> defaults = new List<string>();
        int featureIndex = 0;
        while (featureIndex < checkedSet.Features.Length)
        {
            FtpFeature feature = RequireFeature(checkedSet.Features[featureIndex]);
            if (string.Equals(feature.Name, FtpFeatureNames.Mlst, StringComparison.OrdinalIgnoreCase))
            {
                info.Advertised = true;
                if (feature.Parameters != null && feature.Parameters.Length > 0)
                {
                    AppendMlstFacts(facts, defaults, feature.Parameters);
                }
            }
            featureIndex++;
        }

        info.Facts = facts.ToArray();
        info.DefaultFacts = defaults.ToArray();
        return info;
    }

    internal static FtpFeature CreateAuthenticationFeature(string[]? mechanisms)
    {
        if (mechanisms == null)
        {
            throw new ArgumentNullException("mechanisms");
        }
        if (mechanisms.Length == 0 || mechanisms.Length > FtpDefaults.MaximumFeatureCount)
        {
            throw new FtpProtocolException("An FTP AUTH feature requires a bounded non-empty mechanism list.", null);
        }

        StringBuilder parameters = new StringBuilder();
        int index = 0;
        while (index < mechanisms.Length)
        {
            if (index > 0)
            {
                parameters.Append(' ');
            }
            parameters.Append(NormalizeAuthenticationMechanism(mechanisms[index]));
            index++;
        }

        FtpFeature feature = new FtpFeature();
        feature.Name = FtpFeatureNames.Auth;
        feature.Parameters = parameters.ToString();
        feature.RawLine = " " + feature.Name + " " + feature.Parameters;
        feature.Support = FtpFeatureSupport.Supported;
        return feature;
    }

    internal static FtpFeature CreateMlstFeature(string[]? facts, string[]? defaultFacts)
    {
        if (facts == null)
        {
            throw new ArgumentNullException("facts");
        }
        if (defaultFacts == null)
        {
            throw new ArgumentNullException("defaultFacts");
        }
        if (facts.Length > FtpDefaults.MaximumFactCount || defaultFacts.Length > FtpDefaults.MaximumFactCount)
        {
            throw new FtpProtocolException("An FTP MLST feature exceeded the configured fact limit.", null);
        }

        string[] normalizedDefaults = new string[defaultFacts.Length];
        int defaultIndex = 0;
        while (defaultIndex < defaultFacts.Length)
        {
            normalizedDefaults[defaultIndex] = NormalizeFactName(defaultFacts[defaultIndex]);
            defaultIndex++;
        }

        StringBuilder parameters = new StringBuilder();
        int factIndex = 0;
        while (factIndex < facts.Length)
        {
            string fact = NormalizeFactName(facts[factIndex]);
            parameters.Append(fact);
            if (ContainsFact(normalizedDefaults, fact))
            {
                parameters.Append('*');
            }
            parameters.Append(';');
            factIndex++;
        }

        defaultIndex = 0;
        while (defaultIndex < normalizedDefaults.Length)
        {
            if (!ContainsFactInSource(facts, normalizedDefaults[defaultIndex]))
            {
                throw new FtpProtocolException("Every default MLST fact must also be advertised.", null);
            }
            defaultIndex++;
        }

        FtpFeature feature = new FtpFeature();
        feature.Name = FtpFeatureNames.Mlst;
        feature.Parameters = parameters.ToString();
        feature.RawLine = " " + feature.Name + " " + feature.Parameters;
        feature.Support = FtpFeatureSupport.Supported;
        return feature;
    }

    internal static FtpFeatureSupport FindFeatureSupport(FtpFeatureSet? featureSet, string? featureName)
    {
        FtpFeatureSet checkedSet = RequireFeatureSet(featureSet);
        if (!checkedSet.Negotiated)
        {
            return FtpFeatureSupport.Unknown;
        }
        string normalizedName = NormalizeFeatureName(featureName);
        int index = 0;
        while (index < checkedSet.Features.Length)
        {
            FtpFeature feature = RequireFeature(checkedSet.Features[index]);
            if (string.Equals(feature.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return FtpFeatureSupport.Supported;
            }
            index++;
        }
        return FtpFeatureSupport.NotSupported;
    }

    internal static void AppendAuthenticationMechanisms(List<string> mechanisms, string parameters)
    {
        if (IsFeatureWhitespace(parameters[0]) || IsFeatureWhitespace(parameters[parameters.Length - 1]))
        {
            throw new FtpProtocolException("An FTP AUTH mechanism list cannot begin or end with a space.", null);
        }

        int start = 0;
        int index = 0;
        while (index <= parameters.Length)
        {
            if (index == parameters.Length || IsFeatureWhitespace(parameters[index]))
            {
                if (index == start)
                {
                    throw new FtpProtocolException("An FTP AUTH mechanism list cannot contain empty entries.", null);
                }
                if (mechanisms.Count >= FtpDefaults.MaximumFeatureCount)
                {
                    throw new FtpProtocolException("The FTP AUTH mechanism list exceeded its configured limit.", null);
                }
                mechanisms.Add(NormalizeAuthenticationMechanism(parameters.Substring(start, index - start)));
                start = index + 1;
            }
            index++;
        }
    }

    internal static void AppendMlstFacts(
        List<string> facts, List<string> defaults, string parameters)
    {
        if (parameters[parameters.Length - 1] != ';')
        {
            throw new FtpProtocolException("An FTP MLST feature fact list must end each fact with a semicolon.", null);
        }

        int start = 0;
        int index = 0;
        while (index < parameters.Length)
        {
            if (parameters[index] == ';')
            {
                if (index == start)
                {
                    throw new FtpProtocolException("An FTP MLST feature cannot contain an empty fact name.", null);
                }
                if (facts.Count >= FtpDefaults.MaximumFactCount)
                {
                    throw new FtpProtocolException("The FTP MLST feature exceeded the configured fact limit.", null);
                }

                string factText = parameters.Substring(start, index - start);
                bool selected = factText[factText.Length - 1] == '*';
                if (selected)
                {
                    factText = factText.Substring(0, factText.Length - 1);
                }
                string fact = NormalizeFactName(factText);
                facts.Add(fact);
                if (selected)
                {
                    defaults.Add(fact);
                }
                start = index + 1;
            }
            index++;
        }
    }

    internal static int FindCompoundFeatureNameLength(string content)
    {
        if (MatchesCompoundFeature(content, FtpFeatureNames.SiteChmod))
        {
            return FtpFeatureNames.SiteChmod.Length;
        }
        if (MatchesCompoundFeature(content, FtpFeatureNames.SiteUmask))
        {
            return FtpFeatureNames.SiteUmask.Length;
        }
        if (MatchesCompoundFeature(content, FtpFeatureNames.RestStream))
        {
            return FtpFeatureNames.RestStream.Length;
        }
        if (MatchesCompoundFeature(content, FtpFeatureNames.ModeZ))
        {
            return FtpFeatureNames.ModeZ.Length;
        }
        return 0;
    }

    internal static bool MatchesCompoundFeature(string content, string featureName)
    {
        if (string.Equals(content, featureName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return content.Length > featureName.Length && content[featureName.Length] == ' ' &&
            string.Equals(content.Substring(0, featureName.Length), featureName, StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeFeatureName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new FtpProtocolException("An FTP feature name is required.", null);
        }

        bool knownCompound = string.Equals(name, FtpFeatureNames.RestStream, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, FtpFeatureNames.ModeZ, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, FtpFeatureNames.SiteChmod, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, FtpFeatureNames.SiteUmask, StringComparison.OrdinalIgnoreCase);
        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];
            bool permittedCompoundSpace = character == ' ' && knownCompound;
            if ((!permittedCompoundSpace && character < '!') || character > '~')
            {
                throw new FtpProtocolException("An FTP feature name must be a printable ASCII label.", null);
            }
            index++;
        }
        return name.ToUpperInvariant();
    }

    internal static string NormalizeAuthenticationMechanism(string? mechanism)
    {
        if (string.IsNullOrEmpty(mechanism))
        {
            throw new FtpProtocolException("An FTP AUTH mechanism name is required.", null);
        }
        int index = 0;
        while (index < mechanism.Length)
        {
            char character = mechanism[index];
            if (character < '!' || character > '~')
            {
                throw new FtpProtocolException("An FTP AUTH mechanism must be a printable ASCII token.", null);
            }
            index++;
        }
        return mechanism.ToUpperInvariant();
    }

    internal static string NormalizeFactName(string? fact)
    {
        if (string.IsNullOrEmpty(fact))
        {
            throw new FtpProtocolException("An FTP MLST fact name is required.", null);
        }
        int index = 0;
        while (index < fact.Length)
        {
            char character = fact[index];
            if (character < '!' || character > '~' || character == '*' || character == ';')
            {
                throw new FtpProtocolException("An FTP MLST fact name must be a printable token without separators.", null);
            }
            index++;
        }
        return fact.ToLowerInvariant();
    }

    internal static bool ContainsFact(string[] facts, string expected)
    {
        int index = 0;
        while (index < facts.Length)
        {
            if (string.Equals(facts[index], expected, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool ContainsFactInSource(string[] facts, string expected)
    {
        int index = 0;
        while (index < facts.Length)
        {
            string normalized = NormalizeFactName(facts[index]);
            if (string.Equals(normalized, expected, StringComparison.Ordinal))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static void ValidateAsciiPrintable(string text, bool allowEmpty, string valueName)
    {
        if (!allowEmpty && text.Length == 0)
        {
            throw new FtpProtocolException($"The FTP {valueName} cannot be empty.", null);
        }
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if ((character < ' ' && character != '\t') || character > '~')
            {
                throw new FtpProtocolException($"The FTP {valueName} must contain printable ASCII characters.", null);
            }
            index++;
        }
    }

    internal static bool IsFeatureWhitespace(char character)
    {
        return character == ' ' || character == '\t';
    }

    internal static FtpFeatureSet RequireFeatureSet(FtpFeatureSet? featureSet)
    {
        if (featureSet == null)
        {
            throw new ArgumentNullException("featureSet");
        }
        if (featureSet.Features == null)
        {
            throw new FtpProtocolException("The FTP feature collection is required.", null);
        }
        return featureSet;
    }

    internal static FtpFeature RequireFeature(FtpFeature? feature)
    {
        if (feature == null)
        {
            throw new FtpProtocolException("An FTP feature collection cannot contain a null entry.", null);
        }
        return feature;
    }

    internal static FtpReplyLine RequireFeatureReplyLine(FtpReplyLine? line, string lineName)
    {
        if (line == null)
        {
            throw new FtpProtocolException($"The FTP FEAT {lineName} line is required.", null);
        }
        if (line.Text == null || line.RawText == null)
        {
            throw new FtpProtocolException($"The FTP FEAT {lineName} line text is required.", null);
        }
        return line;
    }
}
