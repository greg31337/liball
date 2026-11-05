using System;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ftp;

internal static class FtpTlsDiagnosticsFactory
{
    internal static FtpTlsDiagnostics CreateDiagnostics(
        SslStream stream, bool serverRole, bool implicitTls,
        bool explicitUpgrade, bool dataChannel, bool tlsResumeAllowed)
    {
        FtpFaultIsolationResult inspection = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return InspectDiagnostics(
                stream, serverRole, implicitTls,
                explicitUpgrade, dataChannel, tlsResumeAllowed);
        });
        FtpTlsDiagnostics? inspected = inspection.Value as FtpTlsDiagnostics;
        if (inspection.Succeeded && inspected != null)
        {
            return inspected;
        }

        FtpTlsDiagnostics fallback = new FtpTlsDiagnostics();
        fallback.ServerRole = serverRole;
        fallback.ImplicitTls = implicitTls;
        fallback.ExplicitUpgrade = explicitUpgrade;
        fallback.DataChannel = dataChannel;
        fallback.TlsResumeAllowed = tlsResumeAllowed;
        return fallback;
    }

    internal static FtpTlsDiagnostics InspectDiagnostics(
        SslStream stream, bool serverRole, bool implicitTls,
        bool explicitUpgrade, bool dataChannel, bool tlsResumeAllowed)
    {
        FtpTlsDiagnostics diagnostics = new FtpTlsDiagnostics();
        diagnostics.Authenticated = stream.IsAuthenticated;
        diagnostics.ServerRole = serverRole;
        diagnostics.ImplicitTls = implicitTls;
        diagnostics.ExplicitUpgrade = explicitUpgrade;
        diagnostics.DataChannel = dataChannel;
        diagnostics.TlsResumeAllowed = tlsResumeAllowed;
        diagnostics.Encrypted = stream.IsEncrypted;
        diagnostics.Signed = stream.IsSigned;
        diagnostics.MutuallyAuthenticated = stream.IsMutuallyAuthenticated;
        diagnostics.Protocol = stream.SslProtocol.ToString();
        diagnostics.CipherSuite = stream.NegotiatedCipherSuite.ToString();
        diagnostics.LocalCertificateAvailable = stream.LocalCertificate != null;
        X509Certificate? remoteCertificate = stream.RemoteCertificate;
        diagnostics.RemoteCertificateAvailable = remoteCertificate != null;
        if (remoteCertificate != null)
        {
            diagnostics.RemoteCertificateSha256Fingerprint =
                remoteCertificate.GetCertHashString(HashAlgorithmName.SHA256);
        }
        return diagnostics;
    }

    internal static void ApplySessionReuse(
        FtpTlsDiagnostics diagnostics, FtpTlsSessionReuseEvidence? evidence)
    {
        diagnostics.SessionReuseEvidenceAvailable =
            evidence != null && evidence.Available;
        diagnostics.SessionReused = evidence != null && evidence.Reused;
        diagnostics.SessionMatchesControl =
            evidence != null && evidence.MatchesControlSession;
    }

    internal static FtpTlsDiagnostics CopyDiagnostics(FtpTlsDiagnostics source)
    {
        FtpTlsDiagnostics copy = new FtpTlsDiagnostics();
        copy.Authenticated = source.Authenticated;
        copy.ServerRole = source.ServerRole;
        copy.ImplicitTls = source.ImplicitTls;
        copy.ExplicitUpgrade = source.ExplicitUpgrade;
        copy.DataChannel = source.DataChannel;
        copy.TlsResumeAllowed = source.TlsResumeAllowed;
        copy.Encrypted = source.Encrypted;
        copy.Signed = source.Signed;
        copy.MutuallyAuthenticated = source.MutuallyAuthenticated;
        copy.Protocol = source.Protocol;
        copy.CipherSuite = source.CipherSuite;
        copy.LocalCertificateAvailable = source.LocalCertificateAvailable;
        copy.RemoteCertificateAvailable = source.RemoteCertificateAvailable;
        copy.RemoteCertificateSha256Fingerprint =
            source.RemoteCertificateSha256Fingerprint;
        copy.SessionReuseEvidenceAvailable = source.SessionReuseEvidenceAvailable;
        copy.SessionReused = source.SessionReused;
        copy.SessionMatchesControl = source.SessionMatchesControl;
        return copy;
    }

    internal static FtpEvent CreateEvent(
        FtpTlsStreamResult result, FtpSecurityMode securityMode,
        FtpDataProtectionLevel dataProtectionLevel,
        FtpSessionState sessionState)
    {
        FtpEvent eventData = new FtpEvent();
        eventData.Kind = FtpEventKind.Tls;
        eventData.HasSecurityMode = true;
        eventData.SecurityMode = securityMode;
        eventData.HasDataProtectionLevel = true;
        eventData.DataProtectionLevel = dataProtectionLevel;
        eventData.HasSessionState = true;
        eventData.SessionState = sessionState;
        eventData.Timestamp.HasValue = true;
        eventData.Timestamp.ValueUtc = DateTime.UtcNow;
        eventData.Message = CreateMessage(result.Diagnostics);
        eventData.TlsDiagnostics = CopyDiagnostics(result.Diagnostics);
        eventData.Redacted = true;
        return eventData;
    }

    internal static string CreateMessage(FtpTlsDiagnostics diagnostics)
    {
        string channel = diagnostics.DataChannel ? "data" : "control";
        string role = diagnostics.ServerRole ? "server" : "client";
        return $"FTP {channel}-channel TLS authenticated in the {role} role using " +
            diagnostics.Protocol + " and " + diagnostics.CipherSuite + ".";
    }
}
