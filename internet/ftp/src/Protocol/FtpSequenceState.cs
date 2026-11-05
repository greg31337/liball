namespace ftp;

internal class FtpSequenceState
{
    internal FtpSessionState SessionState;
    internal string? SelectedHost;
    internal string? Language;
    internal string? UserName;
    internal FtpPrincipal? Principal;
    internal bool UserNameSupplied;
    internal bool AccountRequired;
    internal bool SecurityExchangeInProgress;
    internal bool SecurityExchangeComplete;
    internal bool SecurityExchangeIsTls;
    internal bool ControlProtected;
    internal bool ProtectionBufferSizeSet;
    internal bool ClearCommandChannelCompleted;
    internal FtpDataProtectionLevel DataProtectionLevel;
    internal bool RenameSourceReady;
    internal string? RenameSource;
    internal bool HasRestartMarker;
    internal long RestartMarker;
    internal string? RestartMarkerText;
    internal bool DataConnectionConfigured;
    internal bool PassiveDataConnection;
    internal bool ExtendedDataConnection;
    internal FtpEndpoint? DataEndpoint;
    internal bool ExtendedPassiveOnly;
    internal bool TransferInProgress;
    internal bool QuitPending;
    internal FtpTransferType TransferType;
    internal FtpTextFormat TextFormat;
    internal FtpFileStructure FileStructure;
    internal FtpTransferMode TransferMode;
    internal bool HasLocalByteSize;
    internal int LocalByteSize;
    internal string CurrentDirectory;

    internal FtpSequenceState()
    {
        SessionState = FtpSessionState.Disconnected;
        SelectedHost = null;
        Language = null;
        UserName = null;
        Principal = null;
        UserNameSupplied = false;
        AccountRequired = false;
        SecurityExchangeInProgress = false;
        SecurityExchangeComplete = false;
        SecurityExchangeIsTls = false;
        ControlProtected = false;
        ProtectionBufferSizeSet = false;
        ClearCommandChannelCompleted = false;
        DataProtectionLevel = FtpDataProtectionLevel.Clear;
        RenameSourceReady = false;
        RenameSource = null;
        HasRestartMarker = false;
        RestartMarker = 0;
        RestartMarkerText = null;
        DataConnectionConfigured = false;
        PassiveDataConnection = false;
        ExtendedDataConnection = false;
        DataEndpoint = null;
        ExtendedPassiveOnly = false;
        TransferInProgress = false;
        QuitPending = false;
        TransferType = FtpTransferType.Ascii;
        TextFormat = FtpTextFormat.NonPrint;
        FileStructure = FtpFileStructure.File;
        TransferMode = FtpTransferMode.Stream;
        HasLocalByteSize = false;
        LocalByteSize = 0;
        CurrentDirectory = "/";
    }
}
