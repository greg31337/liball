using System;

namespace ftp;

internal class FtpOptionProcessor
{
    internal FtpOptionSessionState State;
    internal bool MlstAvailable;
    internal string[] SupportedMlstFacts;
    internal bool Utf8Available;
    internal string[] ExtensionOptionNames;

    internal FtpOptionProcessor(
        FtpMlstFeatureInfo? mlstInfo, bool utf8Available,
        bool initialUtf8Enabled, string[]? extensionOptionNames)
    {
        if (initialUtf8Enabled && !utf8Available)
        {
            throw new FtpConfigurationException("Initial UTF-8 option state requires UTF8 availability.", null);
        }

        State = new FtpOptionSessionState();
        MlstAvailable = mlstInfo != null && mlstInfo.Advertised;
        SupportedMlstFacts = CopyMlstFacts(mlstInfo == null ? null : mlstInfo.Facts);
        Utf8Available = utf8Available;
        State.Utf8Enabled = initialUtf8Enabled;
        ExtensionOptionNames = NormalizeExtensionNames(extensionOptionNames);
        if (MlstAvailable && mlstInfo != null)
        {
            State.MlstFacts = SelectSupportedMlstFacts(mlstInfo.DefaultFacts);
            State.MlstFactsConfigured = true;
        }
    }

    internal FtpOptionApplicationResult ApplyOptionRequest(FtpOptionRequest? request)
    {
        FtpOptionRequest normalized = FtpOptionCodec.NormalizeOptionRequest(request);
        if (string.Equals(normalized.CommandName, FtpFeatureNames.Mlst, StringComparison.Ordinal))
        {
            return ApplyMlstRequest(normalized);
        }
        if (string.Equals(normalized.CommandName, FtpFeatureNames.Utf8, StringComparison.Ordinal))
        {
            return ApplyUtf8Request(normalized);
        }
        if (string.Equals(normalized.CommandName, FtpCommandNames.Feat, StringComparison.Ordinal) ||
            string.Equals(normalized.CommandName, FtpCommandNames.Opts, StringComparison.Ordinal))
        {
            return RejectKnownRequest(normalized, "The target command does not define OPTS behavior.");
        }
        if (ContainsExtensionName(normalized.CommandName))
        {
            return ApplyExtensionRequest(normalized);
        }

        FtpOptionApplicationResult unhandled = new FtpOptionApplicationResult();
        unhandled.Request = normalized;
        unhandled.ExtensionOwned = true;
        return unhandled;
    }

    internal FtpOptionApplicationResult AcceptOptionReply(
        FtpOptionRequest? request, FtpReply? reply)
    {
        FtpOptionRequest normalized = FtpOptionCodec.NormalizeOptionRequest(request);
        FtpReply checkedReply = FtpReplyExtractor.RequireReply(reply, "OPTS");
        FtpOptionApplicationResult result = new FtpOptionApplicationResult();
        result.Handled = true;
        result.Request = normalized;
        result.Reply = checkedReply;
        FtpReplyClassification classification = FtpReplyClassifier.ClassifyCode(checkedReply.Code);
        if (classification.TransientFailure || classification.PermanentFailure)
        {
            return result;
        }
        if (checkedReply.Code != FtpReplyCodes.CommandOkay)
        {
            throw new FtpProtocolException("A successful FTP OPTS reply must use code 200.", null);
        }

        if (string.Equals(normalized.CommandName, FtpFeatureNames.Mlst, StringComparison.Ordinal))
        {
            State.MlstFacts = FtpOptionCodec.ParseMlstOptionReply(checkedReply);
        }
        else if (string.Equals(normalized.CommandName, FtpFeatureNames.Utf8, StringComparison.Ordinal))
        {
            if (!FtpOptionCodec.TryParseUtf8Value(normalized.Value, result))
            {
                throw new FtpProtocolException("An accepted OPTS UTF8 request must contain ON or OFF.", null);
            }
            State.Utf8Enabled = string.Equals(normalized.Value, "ON", StringComparison.OrdinalIgnoreCase);
            State.Utf8Configured = true;
        }
        else
        {
            result.ExtensionOwned = true;
            StoreExtensionOption(normalized);
        }

        result.Accepted = true;
        return result;
    }

    internal FtpOptionApplicationResult ApplyMlstRequest(FtpOptionRequest request)
    {
        if (!MlstAvailable)
        {
            return RejectKnownRequest(request, "MLST options are unavailable.");
        }

        FtpOptionFactSelection parsed = FtpOptionCodec.ParseMlstFactSelection(request.Value);
        if (!parsed.Valid)
        {
            return RejectKnownRequest(request, parsed.FailureMessage);
        }

        string[] selected = SelectSupportedMlstFacts(parsed.Facts);
        State.MlstFacts = selected;
        State.MlstFactsConfigured = true;
        string responseText = "MLST OPTS";
        if (selected.Length > 0)
        {
            responseText = responseText + " " + FormatFactList(selected);
        }
        return AcceptKnownRequest(request, responseText);
    }

    internal FtpOptionApplicationResult ApplyUtf8Request(FtpOptionRequest request)
    {
        FtpOptionApplicationResult validation = new FtpOptionApplicationResult();
        if (!Utf8Available)
        {
            return RejectKnownRequest(request, "UTF8 options are unavailable.");
        }
        if (!FtpOptionCodec.TryParseUtf8Value(request.Value, validation))
        {
            return RejectKnownRequest(request, "OPTS UTF8 requires ON or OFF.");
        }

        State.Utf8Enabled = string.Equals(request.Value, "ON", StringComparison.OrdinalIgnoreCase);
        State.Utf8Configured = true;
        string responseText = State.Utf8Enabled ? "UTF8 ON" : "UTF8 OFF";
        return AcceptKnownRequest(request, responseText);
    }

    internal FtpOptionApplicationResult ApplyExtensionRequest(FtpOptionRequest request)
    {
        StoreExtensionOption(request);
        FtpOptionApplicationResult result = AcceptKnownRequest(
            request, request.CommandName + " OPTS");
        result.ExtensionOwned = true;
        return result;
    }

    internal FtpOptionApplicationResult AcceptKnownRequest(
        FtpOptionRequest request, string responseText)
    {
        FtpOptionApplicationResult result = new FtpOptionApplicationResult();
        result.Handled = true;
        result.Accepted = true;
        result.Request = request;
        result.Reply = FtpOptionCodec.CreateOptionReply(FtpReplyCodes.CommandOkay, responseText);
        return result;
    }

    internal FtpOptionApplicationResult RejectKnownRequest(
        FtpOptionRequest request, string responseText)
    {
        FtpOptionApplicationResult result = new FtpOptionApplicationResult();
        result.Handled = true;
        result.Request = request;
        result.Reply = FtpOptionCodec.CreateOptionReply(
            FtpReplyCodes.SyntaxErrorInParameters, responseText);
        return result;
    }

    internal string[] SelectSupportedMlstFacts(string[]? requestedFacts)
    {
        if (requestedFacts == null || requestedFacts.Length == 0)
        {
            return new string[0];
        }

        string[] selected = new string[SupportedMlstFacts.Length];
        int count = 0;
        int supportedIndex = 0;
        while (supportedIndex < SupportedMlstFacts.Length)
        {
            if (ContainsRequestedFact(requestedFacts, SupportedMlstFacts[supportedIndex]))
            {
                selected[count] = SupportedMlstFacts[supportedIndex];
                count++;
            }
            supportedIndex++;
        }

        string[] result = new string[count];
        int index = 0;
        while (index < count)
        {
            result[index] = selected[index];
            index++;
        }
        return result;
    }

    internal void StoreExtensionOption(FtpOptionRequest request)
    {
        int index = 0;
        while (index < State.ExtensionOptions.Length)
        {
            if (string.Equals(
                State.ExtensionOptions[index].CommandName, request.CommandName, StringComparison.Ordinal))
            {
                State.ExtensionOptions[index] = CopyOptionRequest(request);
                return;
            }
            index++;
        }

        if (State.ExtensionOptions.Length >= FtpDefaults.MaximumFeatureCount)
        {
            throw new FtpProtocolException("The session extension-option limit was exceeded.", null);
        }
        FtpOptionRequest[] options = new FtpOptionRequest[State.ExtensionOptions.Length + 1];
        index = 0;
        while (index < State.ExtensionOptions.Length)
        {
            options[index] = State.ExtensionOptions[index];
            index++;
        }
        options[options.Length - 1] = CopyOptionRequest(request);
        State.ExtensionOptions = options;
    }

    internal bool ContainsExtensionName(string name)
    {
        int index = 0;
        while (index < ExtensionOptionNames.Length)
        {
            if (string.Equals(ExtensionOptionNames[index], name, StringComparison.Ordinal))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static string[] CopyMlstFacts(string[]? facts)
    {
        if (facts == null)
        {
            return new string[0];
        }
        if (facts.Length > FtpDefaults.MaximumFactCount)
        {
            throw new FtpConfigurationException("The supported MLST fact list exceeded its configured limit.", null);
        }

        string[] copy = new string[facts.Length];
        int index = 0;
        while (index < facts.Length)
        {
            string? fact = FtpOptionCodec.TryNormalizeMlstFact(facts[index]);
            if (fact == null)
            {
                throw new FtpConfigurationException("The supported MLST fact list contains an invalid name.", null);
            }
            copy[index] = fact;
            index++;
        }
        return copy;
    }

    internal static string[] NormalizeExtensionNames(string[]? names)
    {
        if (names == null)
        {
            return new string[0];
        }
        if (names.Length > FtpDefaults.MaximumFeatureCount)
        {
            throw new FtpConfigurationException("The extension OPTS target list exceeded its configured limit.", null);
        }

        string[] normalized = new string[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            normalized[index] = FtpOptionCodec.NormalizeOptionTarget(names[index]);
            int earlier = 0;
            while (earlier < index)
            {
                if (string.Equals(normalized[earlier], normalized[index], StringComparison.Ordinal))
                {
                    throw new FtpConfigurationException("The extension OPTS target list contains a duplicate.", null);
                }
                earlier++;
            }
            index++;
        }
        return normalized;
    }

    internal static bool ContainsRequestedFact(string[] facts, string expected)
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

    internal static string FormatFactList(string[] facts)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < facts.Length)
        {
            builder.Append(facts[index]);
            builder.Append(';');
            index++;
        }
        return builder.ToString();
    }

    internal static FtpOptionRequest CopyOptionRequest(FtpOptionRequest request)
    {
        FtpOptionRequest copy = new FtpOptionRequest();
        copy.CommandName = request.CommandName;
        copy.Value = request.Value;
        return copy;
    }
}
