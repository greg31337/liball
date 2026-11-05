namespace ftp;

/// <summary>
/// Defines named FTP reply codes from the supported standards and extensions.
/// </summary>
public static class FtpReplyCodes
{
    /// <summary>
    /// A restart marker reply.
    /// </summary>
    public const int RestartMarker = 110;

    /// <summary>
    /// The service will be ready after a delay.
    /// </summary>
    public const int ServiceReadySoon = 120;

    /// <summary>
    /// The data connection is already open and transfer is starting.
    /// </summary>
    public const int DataConnectionAlreadyOpen = 125;

    /// <summary>
    /// File status is okay and a data connection will open.
    /// </summary>
    public const int FileStatusOkayOpeningDataConnection = 150;

    /// <summary>
    /// The command completed successfully.
    /// </summary>
    public const int CommandOkay = 200;

    /// <summary>
    /// The command is implemented but unnecessary at this site.
    /// </summary>
    public const int CommandSuperfluous = 202;

    /// <summary>
    /// System status or a feature-listing reply.
    /// </summary>
    public const int SystemStatus = 211;

    /// <summary>
    /// Directory status.
    /// </summary>
    public const int DirectoryStatus = 212;

    /// <summary>
    /// File status, size, timestamp, or checksum data.
    /// </summary>
    public const int FileStatus = 213;

    /// <summary>
    /// A help message.
    /// </summary>
    public const int HelpMessage = 214;

    /// <summary>
    /// The server system type.
    /// </summary>
    public const int SystemType = 215;

    /// <summary>
    /// The service is ready for a new user.
    /// </summary>
    public const int ServiceReady = 220;

    /// <summary>
    /// The service is closing the control connection.
    /// </summary>
    public const int ServiceClosingControlConnection = 221;

    /// <summary>
    /// The data connection is open with no transfer in progress.
    /// </summary>
    public const int DataConnectionOpenNoTransfer = 225;

    /// <summary>
    /// The server is closing the data connection after completion.
    /// </summary>
    public const int ClosingDataConnection = 226;

    /// <summary>
    /// The server is entering passive mode.
    /// </summary>
    public const int EnteringPassiveMode = 227;

    /// <summary>
    /// The server is entering extended passive mode.
    /// </summary>
    public const int EnteringExtendedPassiveMode = 229;

    /// <summary>
    /// The user is logged in.
    /// </summary>
    public const int UserLoggedIn = 230;

    /// <summary>
    /// The user was authorized by a security exchange.
    /// </summary>
    public const int UserLoggedInBySecurityExchange = 232;

    /// <summary>
    /// The security exchange completed or AUTH TLS was accepted.
    /// </summary>
    public const int SecurityExchangeComplete = 234;

    /// <summary>
    /// The security exchange completed with optional security data.
    /// </summary>
    public const int SecurityExchangeCompleteWithData = 235;

    /// <summary>
    /// The requested file action completed.
    /// </summary>
    public const int RequestedFileActionOkay = 250;

    /// <summary>
    /// A pathname was created or reported.
    /// </summary>
    public const int PathCreated = 257;

    /// <summary>
    /// The user name is accepted and a password is required.
    /// </summary>
    public const int UserNameOkayNeedPassword = 331;

    /// <summary>
    /// Account information is required for login.
    /// </summary>
    public const int NeedAccountForLogin = 332;

    /// <summary>
    /// The security mechanism is accepted and security data may follow.
    /// </summary>
    public const int SecurityMechanismOkay = 334;

    /// <summary>
    /// Security data was accepted and more is required.
    /// </summary>
    public const int SecurityDataAcceptedNeedMore = 335;

    /// <summary>
    /// The user name is accepted and a challenge password is required.
    /// </summary>
    public const int UserNameOkayNeedChallengePassword = 336;

    /// <summary>
    /// The requested file action awaits more information.
    /// </summary>
    public const int RequestedFileActionPending = 350;

    /// <summary>
    /// The service is unavailable and is closing the control connection.
    /// </summary>
    public const int ServiceUnavailable = 421;

    /// <summary>
    /// The data connection could not be opened.
    /// </summary>
    public const int CannotOpenDataConnection = 425;

    /// <summary>
    /// The data connection closed and the transfer was aborted.
    /// </summary>
    public const int ConnectionClosedTransferAborted = 426;

    /// <summary>
    /// A resource required for security processing is unavailable.
    /// </summary>
    public const int SecurityResourceUnavailable = 431;

    /// <summary>
    /// The requested file action was not taken because of a temporary condition.
    /// </summary>
    public const int RequestedFileActionNotTaken = 450;

    /// <summary>
    /// The requested action was aborted because of a local processing error.
    /// </summary>
    public const int RequestedActionAbortedLocalError = 451;

    /// <summary>
    /// The requested action was not taken because storage is insufficient.
    /// </summary>
    public const int RequestedActionNotTakenInsufficientStorage = 452;

    /// <summary>
    /// The command is unrecognized or has a syntax error.
    /// </summary>
    public const int SyntaxError = 500;

    /// <summary>
    /// A command parameter or argument has a syntax error.
    /// </summary>
    public const int SyntaxErrorInParameters = 501;

    /// <summary>
    /// The command is not implemented.
    /// </summary>
    public const int CommandNotImplemented = 502;

    /// <summary>
    /// The command was issued in an invalid sequence.
    /// </summary>
    public const int BadCommandSequence = 503;

    /// <summary>
    /// The command is not implemented for the supplied parameter.
    /// </summary>
    public const int CommandParameterNotImplemented = 504;

    /// <summary>
    /// A data connection cannot be opened with the selected protection level.
    /// </summary>
    public const int DataProtectionLevelNotSupported = 521;

    /// <summary>
    /// The requested network protocol is not supported.
    /// </summary>
    public const int NetworkProtocolNotSupported = 522;

    /// <summary>
    /// The session is not logged in.
    /// </summary>
    public const int NotLoggedIn = 530;

    /// <summary>
    /// Account information is required to store files.
    /// </summary>
    public const int NeedAccountForStorage = 532;

    /// <summary>
    /// The command protection level was denied by policy.
    /// </summary>
    public const int CommandProtectionDenied = 533;

    /// <summary>
    /// The request was denied by security policy.
    /// </summary>
    public const int RequestDeniedByPolicy = 534;

    /// <summary>
    /// A security integrity or sequencing check failed.
    /// </summary>
    public const int SecurityCheckFailed = 535;

    /// <summary>
    /// The requested protection level is not supported by the mechanism.
    /// </summary>
    public const int ProtectionLevelNotSupported = 536;

    /// <summary>
    /// The security mechanism does not support the command protection level.
    /// </summary>
    public const int CommandProtectionNotSupported = 537;

    /// <summary>
    /// The requested action was not taken.
    /// </summary>
    public const int RequestedActionNotTaken = 550;

    /// <summary>
    /// The requested action was aborted because the page type is unknown.
    /// </summary>
    public const int PageTypeUnknown = 551;

    /// <summary>
    /// The file action was aborted because storage allocation was exceeded.
    /// </summary>
    public const int FileActionAbortedStorageExceeded = 552;

    /// <summary>
    /// The requested action was not taken because the file name is not allowed.
    /// </summary>
    public const int FileNameNotAllowed = 553;

    /// <summary>
    /// The requested hash is unavailable for the selected entry type.
    /// </summary>
    public const int HashUnavailableForEntry = 556;

    /// <summary>
    /// The enclosed reply has integrity protection.
    /// </summary>
    public const int IntegrityProtectedReply = 631;

    /// <summary>
    /// The enclosed reply has confidentiality and integrity protection.
    /// </summary>
    public const int ConfidentialityAndIntegrityProtectedReply = 632;

    /// <summary>
    /// The enclosed reply has confidentiality protection.
    /// </summary>
    public const int ConfidentialityProtectedReply = 633;
}

