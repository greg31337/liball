using System;

namespace ftp;

internal class FtpClientReconnectState
{
    internal bool HasSession;
    internal bool Authenticated;
    internal FtpCredentials? Credentials;
    internal string? SelectedHost;
    internal bool FeaturesNegotiated;
    internal bool Utf8Configured;
    internal bool Utf8Enabled;
    internal bool MlstConfigured;
    internal string[] MlstFacts;
    internal FtpOptionRequest[] ExtensionOptions;
    internal string CurrentDirectory;
    internal FtpTransferType TransferType;
    internal FtpTextFormat TextFormat;
    internal bool HasLocalByteSize;
    internal int LocalByteSize;
    internal FtpFileStructure FileStructure;
    internal FtpTransferMode TransferMode;
    internal FtpDataProtectionLevel DataProtectionLevel;

    internal FtpClientReconnectState()
    {
        HasSession = false;
        Authenticated = false;
        Credentials = null;
        SelectedHost = null;
        FeaturesNegotiated = false;
        Utf8Configured = false;
        Utf8Enabled = false;
        MlstConfigured = false;
        MlstFacts = new string[0];
        ExtensionOptions = new FtpOptionRequest[0];
        CurrentDirectory = "/";
        TransferType = FtpTransferType.Ascii;
        TextFormat = FtpTextFormat.NonPrint;
        HasLocalByteSize = false;
        LocalByteSize = 0;
        FileStructure = FtpFileStructure.File;
        TransferMode = FtpTransferMode.Stream;
        DataProtectionLevel = FtpDataProtectionLevel.Clear;
    }

    internal static FtpClientReconnectState Capture(FtpClientState state)
    {
        FtpClientReconnectState snapshot = new FtpClientReconnectState();
        FtpSessionState sessionState = state.Session.State.SessionState;
        snapshot.HasSession = sessionState != FtpSessionState.Disconnected &&
            sessionState != FtpSessionState.Closed;
        snapshot.Authenticated = sessionState == FtpSessionState.Authenticated ||
            sessionState == FtpSessionState.Transferring ||
            (sessionState == FtpSessionState.Closing &&
                state.ActiveCredentials != null);
        snapshot.Credentials = CopyCredentials(state.ActiveCredentials);
        snapshot.SelectedHost = state.Session.State.SelectedHost;
        snapshot.FeaturesNegotiated = state.Features.Negotiated;
        snapshot.CurrentDirectory = state.Session.State.CurrentDirectory;
        snapshot.TransferType = state.Session.State.TransferType;
        snapshot.TextFormat = state.Session.State.TextFormat;
        snapshot.HasLocalByteSize = state.Session.State.HasLocalByteSize;
        snapshot.LocalByteSize = state.Session.State.LocalByteSize;
        snapshot.FileStructure = state.Session.State.FileStructure;
        snapshot.TransferMode = state.Session.State.TransferMode;
        snapshot.DataProtectionLevel = state.Session.State.DataProtectionLevel;

        FtpOptionProcessor? processor = state.FeatureState.OptionProcessor;
        if (processor != null)
        {
            snapshot.Utf8Configured = processor.State.Utf8Configured;
            snapshot.Utf8Enabled = processor.State.Utf8Enabled;
            snapshot.MlstConfigured = processor.MlstAvailable;
            snapshot.MlstFacts = CopyStrings(processor.State.MlstFacts);
            snapshot.ExtensionOptions = CopyOptions(
                processor.State.ExtensionOptions);
        }
        return snapshot;
    }

    internal static FtpCredentials? CopyCredentials(FtpCredentials? credentials)
    {
        if (credentials == null)
        {
            return null;
        }
        FtpCredentials copy = new FtpCredentials();
        copy.UserName = credentials.UserName;
        copy.Password = credentials.Password;
        copy.Account = credentials.Account;
        copy.Anonymous = credentials.Anonymous;
        return copy;
    }

    internal static string[] CopyStrings(string[] values)
    {
        string[] copy = new string[values.Length];
        Array.Copy(values, copy, values.Length);
        return copy;
    }

    internal static FtpOptionRequest[] CopyOptions(FtpOptionRequest[] values)
    {
        FtpOptionRequest[] copy = new FtpOptionRequest[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            FtpOptionRequest option = new FtpOptionRequest();
            option.CommandName = values[index].CommandName;
            option.Value = values[index].Value;
            copy[index] = option;
            index++;
        }
        return copy;
    }
}
