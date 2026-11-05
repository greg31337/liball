using System;
using System.IO;
using System.Net.Security;

namespace ftp;

internal class FtpProtectedDataStreamRequest
{
    internal FtpClearDataStreamRequest? Transfer;
    internal FtpDataChannelRole Role;
    internal FtpTlsOptions? TlsOptions;
    internal string TargetHost;
    internal int HandshakeTimeoutMilliseconds;
    internal RemoteCertificateValidationCallback? CertificateValidation;
    internal ServerCertificateSelectionCallback? CertificateSelection;
    internal IFtpCertificateValidator? CertificateValidator;
    internal IFtpCertificateProvider? CertificateProvider;
    internal string HostName;
    internal FtpEndpoint? LocalEndpoint;
    internal FtpEndpoint? RemoteEndpoint;
    internal Func<FtpTlsClientStreamRequest, Stream>? ClientAuthentication;
    internal Func<FtpTlsServerStreamRequest, Stream>? ServerAuthentication;
    internal FtpTlsSessionReuseEvidence? SessionReuseEvidence;

    internal FtpProtectedDataStreamRequest()
    {
        Transfer = new FtpClearDataStreamRequest();
        Role = FtpDataChannelRole.Client;
        TlsOptions = new FtpTlsOptions();
        TargetHost = "";
        HandshakeTimeoutMilliseconds = FtpDefaults.TlsHandshakeTimeoutMilliseconds;
        CertificateValidation = null;
        CertificateSelection = null;
        CertificateValidator = null;
        CertificateProvider = null;
        HostName = "";
        LocalEndpoint = null;
        RemoteEndpoint = null;
        ClientAuthentication = null;
        ServerAuthentication = null;
        SessionReuseEvidence = null;
    }
}
