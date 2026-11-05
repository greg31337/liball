using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

internal static class TlsCertificateSupport
{
    internal static TlsCertificateLoadResult LoadTlsCertificates(
        DiameterSecurityConfiguration security,
        int timeoutMilliseconds)
    {
        TlsCertificateLoadResult result = new TlsCertificateLoadResult();
        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        Task<X509Certificate2> localTask = Task.Run(() =>
            LoadTlsLocalCertificate(security));
        bool localCompleted = SpinWait.SpinUntil(
            () => localTask.IsCompleted,
            timeoutMilliseconds);
        if (!localCompleted)
        {
            result.Status = DiameterTransportStatus.TimedOut;
            result.Code = "certificate-load-timeout";
            result.Message = "The local TLS certificate did not load before the timeout.";
            return result;
        }

        if (!localTask.IsCompletedSuccessfully)
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "local-certificate-invalid";
            result.Message = TlsStreamDiameterConnection.ReadTlsTaskFailure(
                localTask,
                "The local TLS certificate could not be loaded.");
            return result;
        }

        X509Certificate2 localCertificate = localTask.Result;
        result.LocalCertificate = localCertificate;
        if (!localCertificate.HasPrivateKey)
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "local-private-key-required";
            result.Message = "The local TLS certificate requires a private key.";
            localCertificate.Dispose();
            result.LocalCertificate = null;
            return result;
        }

        if (string.IsNullOrWhiteSpace(security.TrustAnchorPath))
        {
            return result;
        }

        int remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
        Task<X509Certificate2> trustTask = Task.Run(() =>
            X509CertificateLoader.LoadCertificateFromFile(security.TrustAnchorPath));
        bool trustCompleted = SpinWait.SpinUntil(
            () => trustTask.IsCompleted,
            remaining);
        if (!trustCompleted)
        {
            result.Status = DiameterTransportStatus.TimedOut;
            result.Code = "certificate-load-timeout";
            result.Message = "The TLS trust anchor did not load before the timeout.";
            localCertificate.Dispose();
            return result;
        }

        if (!trustTask.IsCompletedSuccessfully)
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "trust-anchor-invalid";
            result.Message = TlsStreamDiameterConnection.ReadTlsTaskFailure(
                trustTask,
                "The TLS trust anchor could not be loaded.");
            localCertificate.Dispose();
            return result;
        }

        result.TrustAnchor = trustTask.Result;
        return result;
    }

    internal static X509Certificate2 LoadTlsLocalCertificate(
        DiameterSecurityConfiguration security)
    {
        if (security.LocalCertificatePath.EndsWith(
            ".pem",
            StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(security.LocalCertificatePassword))
            {
                return X509Certificate2.CreateFromEncryptedPemFile(
                    security.LocalCertificatePath,
                    security.LocalCertificatePassword);
            }

            return X509Certificate2.CreateFromPemFile(
                security.LocalCertificatePath);
        }

        return X509CertificateLoader.LoadPkcs12FromFile(
            security.LocalCertificatePath,
            security.LocalCertificatePassword,
            X509KeyStorageFlags.DefaultKeySet);
    }

    internal static DiameterConnectionResult AuthenticateTlsClient(
        IDiameterConnection connection,
        DiameterConnectionMetadata metadata,
        DiameterSecurityConfiguration security,
        X509Certificate2 localCertificate,
        X509Certificate2? trustAnchor,
        string expectedPeerName,
        DiameterIdentity peerIdentity,
        DiameterIdentity peerRealm,
        int timeoutMilliseconds)
    {
        TcpSocketDiameterConnection tcp = (TcpSocketDiameterConnection)connection;
        TlsCertificateValidator validator = new TlsCertificateValidator(
            trustAnchor,
            security.CheckCertificateRevocation,
            string.Empty);
        System.Net.Sockets.NetworkStream networkStream =
            new System.Net.Sockets.NetworkStream(tcp.OwnedSocket, false);
        SslStream sslStream = new SslStream(
            networkStream,
            false,
            (sender, certificate, chain, errors) =>
                validator.ValidateTlsCertificate(sender, certificate, chain, errors));
        SslClientAuthenticationOptions options = new SslClientAuthenticationOptions();
        options.TargetHost = expectedPeerName;
        options.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        options.CertificateRevocationCheckMode = ReadRevocationMode(
            security.CheckCertificateRevocation);
        options.ClientCertificates = new X509CertificateCollection();
        options.ClientCertificates.Add(localCertificate);
        Task authentication = sslStream.AuthenticateAsClientAsync(
            options,
            CancellationToken.None);
        return CompleteTlsAuthentication(
            connection,
            sslStream,
            authentication,
            metadata,
            security,
            localCertificate,
            expectedPeerName,
            peerIdentity,
            peerRealm,
            timeoutMilliseconds);
    }

    internal static DiameterConnectionResult AuthenticateTlsServer(
        IDiameterConnection connection,
        DiameterConnectionMetadata metadata,
        DiameterSecurityConfiguration security,
        X509Certificate2 localCertificate,
        X509Certificate2? trustAnchor,
        int timeoutMilliseconds)
    {
        TcpSocketDiameterConnection tcp = (TcpSocketDiameterConnection)connection;
        TlsCertificateValidator validator = new TlsCertificateValidator(
            trustAnchor,
            security.CheckCertificateRevocation,
            security.ExpectedPeerName);
        System.Net.Sockets.NetworkStream networkStream =
            new System.Net.Sockets.NetworkStream(tcp.OwnedSocket, false);
        SslStream sslStream = new SslStream(
            networkStream,
            false,
            (sender, certificate, chain, errors) =>
                validator.ValidateTlsCertificate(sender, certificate, chain, errors));
        SslServerAuthenticationOptions options = new SslServerAuthenticationOptions();
        options.ServerCertificate = localCertificate;
        options.ClientCertificateRequired = true;
        options.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        options.CertificateRevocationCheckMode = ReadRevocationMode(
            security.CheckCertificateRevocation);
        Task authentication = sslStream.AuthenticateAsServerAsync(
            options,
            CancellationToken.None);
        DiameterIdentity peerIdentity = ReadConfiguredTlsPeerIdentity(security);
        DiameterIdentity peerRealm = ReadConfiguredTlsPeerRealm(
            security,
            peerIdentity);
        return CompleteTlsAuthentication(
            connection,
            sslStream,
            authentication,
            metadata,
            security,
            localCertificate,
            security.ExpectedPeerName,
            peerIdentity,
            peerRealm,
            timeoutMilliseconds);
    }

    internal static DiameterConnectionResult CompleteTlsAuthentication(
        IDiameterConnection connection,
        SslStream sslStream,
        Task authentication,
        DiameterConnectionMetadata metadata,
        DiameterSecurityConfiguration security,
        X509Certificate2 localCertificate,
        string expectedPeerName,
        DiameterIdentity peerIdentity,
        DiameterIdentity peerRealm,
        int timeoutMilliseconds)
    {
        bool completed = timeoutMilliseconds > 0 && SpinWait.SpinUntil(
            () => authentication.IsCompleted,
            timeoutMilliseconds);
        if (!completed)
        {
            Task closeStream = Task.Run(() => sslStream.Close());
            SpinWait.SpinUntil(() => closeStream.IsCompleted, 1000);
            connection.CloseDiameterConnection();
            return CreateTlsConnectionFailure(
                DiameterTransportStatus.TimedOut,
                "tls-authentication-timeout",
                "TLS authentication did not complete before its timeout.",
                metadata);
        }

        if (!authentication.IsCompletedSuccessfully)
        {
            string message = TlsStreamDiameterConnection.ReadTlsTaskFailure(
                authentication,
                "TLS authentication failed.");
            Task closeStream = Task.Run(() => sslStream.Close());
            SpinWait.SpinUntil(() => closeStream.IsCompleted, 1000);
            connection.CloseDiameterConnection();
            return CreateTlsConnectionFailure(
                DiameterTransportStatus.AuthenticationFailed,
                "tls-authentication-failed",
                message,
                metadata);
        }

        PopulateTlsMetadata(metadata, sslStream, localCertificate, expectedPeerName);
        DiameterPeerAdmissionResult admission = AuthorizeTlsPeer(
            metadata,
            security,
            peerIdentity,
            peerRealm);
        if (!admission.Admitted)
        {
            Task closeStream = Task.Run(() => sslStream.Close());
            SpinWait.SpinUntil(() => closeStream.IsCompleted, 1000);
            connection.CloseDiameterConnection();
            return CreateTlsConnectionFailure(
                DiameterTransportStatus.AuthenticationFailed,
                admission.Code,
                admission.Message,
                metadata);
        }

        TlsStreamDiameterConnection tlsConnection = new TlsStreamDiameterConnection(
            connection,
            sslStream,
            metadata);
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Connection = tlsConnection;
        result.Metadata = QueuedDiameterConnection.CloneConnectionMetadata(metadata);
        return result;
    }

    internal static DiameterPeerAdmissionResult AuthorizeTlsPeer(
        DiameterConnectionMetadata metadata,
        DiameterSecurityConfiguration security,
        DiameterIdentity peerIdentity,
        DiameterIdentity peerRealm)
    {
        DiameterPeerAdmissionResult certificateDecision;
        if (security.CertificatePeerPolicy == null)
        {
            certificateDecision = AuthorizeDefaultCertificatePeer(
                metadata,
                peerIdentity,
                peerRealm);
        }
        else
        {
            DiameterPeerAdmissionResult? suppliedDecision =
                security.CertificatePeerPolicy.AuthorizeCertificatePeer(
                    CreatePeerAdmissionRequest(metadata, peerIdentity, peerRealm));
            certificateDecision = NormalizePeerAdmissionDecision(
                suppliedDecision,
                "certificate-peer-rejected",
                "The certificate policy rejected the Diameter peer identity.");
        }

        if (!certificateDecision.Admitted)
        {
            return certificateDecision;
        }

        if (security.PeerAdmissionPolicy == null)
        {
            return CreatePeerAdmissionDecision(true, string.Empty, string.Empty);
        }

        DiameterPeerAdmissionResult? peerDecision =
            security.PeerAdmissionPolicy.EvaluatePeerAdmission(
                CreatePeerAdmissionRequest(metadata, peerIdentity, peerRealm));
        return NormalizePeerAdmissionDecision(
            peerDecision,
            "peer-admission-rejected",
            "The peer admission policy rejected the authenticated connection.");
    }

    internal static DiameterPeerAdmissionResult AuthorizeDefaultCertificatePeer(
        DiameterConnectionMetadata metadata,
        DiameterIdentity peerIdentity,
        DiameterIdentity peerRealm)
    {
        DiameterValueResult normalizedIdentity =
            ValueCodecs.NormalizeDiameterIdentity(peerIdentity);
        DiameterValueResult normalizedRealm = ValueCodecs.NormalizeDiameterRealm(peerRealm);
        if (normalizedIdentity.Status != DiameterCodecStatus.Success ||
            normalizedRealm.Status != DiameterCodecStatus.Success)
        {
            return CreatePeerAdmissionDecision(
                false,
                "peer-identity-required",
                "Certificate authorization requires a valid Diameter identity and realm.");
        }

        string realmSuffix = "." + normalizedRealm.Text;
        if (!string.Equals(
            normalizedIdentity.Text,
            normalizedRealm.Text,
            StringComparison.Ordinal) &&
            !normalizedIdentity.Text.EndsWith(realmSuffix, StringComparison.Ordinal))
        {
            return CreatePeerAdmissionDecision(
                false,
                "certificate-peer-realm-mismatch",
                "The Diameter peer identity does not belong to the expected realm.");
        }

        int nameIndex = 0;
        while (nameIndex < metadata.Security.PeerCertificateDnsNames.Count)
        {
            if (DoesCertificateDnsNameMatchIdentity(
                metadata.Security.PeerCertificateDnsNames[nameIndex],
                normalizedIdentity.Text))
            {
                return CreatePeerAdmissionDecision(true, string.Empty, string.Empty);
            }

            nameIndex = nameIndex + 1;
        }

        return CreatePeerAdmissionDecision(
            false,
            "certificate-peer-identity-mismatch",
            "The peer certificate does not authorize the expected Diameter identity.");
    }

    internal static bool DoesCertificateDnsNameMatchIdentity(
        string certificateName,
        string peerIdentity)
    {
        string normalizedCertificateName = certificateName.TrimEnd('.').ToLowerInvariant();
        string normalizedPeerIdentity = peerIdentity.TrimEnd('.').ToLowerInvariant();
        if (string.Equals(
            normalizedCertificateName,
            normalizedPeerIdentity,
            StringComparison.Ordinal))
        {
            return true;
        }

        if (!normalizedCertificateName.StartsWith("*.", StringComparison.Ordinal))
        {
            return false;
        }

        string suffix = normalizedCertificateName.Substring(1);
        if (!normalizedPeerIdentity.EndsWith(suffix, StringComparison.Ordinal))
        {
            return false;
        }

        int prefixLength = normalizedPeerIdentity.Length - suffix.Length;
        return prefixLength > 0 &&
            normalizedPeerIdentity.IndexOf('.', 0, prefixLength) < 0;
    }

    internal static DiameterPeerAdmissionRequest CreatePeerAdmissionRequest(
        DiameterConnectionMetadata metadata,
        DiameterIdentity peerIdentity,
        DiameterIdentity peerRealm)
    {
        DiameterPeerAdmissionRequest result = new DiameterPeerAdmissionRequest();
        result.Connection = QueuedDiameterConnection.CloneConnectionMetadata(metadata);
        result.PeerIdentity.Value = peerIdentity.Value;
        result.PeerRealm.Value = peerRealm.Value;
        return result;
    }

    internal static DiameterPeerAdmissionResult NormalizePeerAdmissionDecision(
        DiameterPeerAdmissionResult? decision,
        string rejectionCode,
        string rejectionMessage)
    {
        if (decision == null)
        {
            return CreatePeerAdmissionDecision(false, rejectionCode, rejectionMessage);
        }

        DiameterPeerAdmissionResult result = new DiameterPeerAdmissionResult();
        result.Admitted = decision.Admitted;
        result.Code = decision.Code;
        result.Message = decision.Message;
        if (!result.Admitted && string.IsNullOrWhiteSpace(result.Code))
        {
            result.Code = rejectionCode;
        }

        if (!result.Admitted && string.IsNullOrWhiteSpace(result.Message))
        {
            result.Message = rejectionMessage;
        }

        return result;
    }

    internal static DiameterPeerAdmissionResult CreatePeerAdmissionDecision(
        bool admitted,
        string code,
        string message)
    {
        DiameterPeerAdmissionResult result = new DiameterPeerAdmissionResult();
        result.Admitted = admitted;
        result.Code = code;
        result.Message = message;
        return result;
    }

    internal static DiameterIdentity ReadConfiguredTlsPeerIdentity(
        DiameterSecurityConfiguration security)
    {
        DiameterIdentity result = new DiameterIdentity();
        if (security.ExpectedPeerIdentity != null &&
            !string.IsNullOrWhiteSpace(security.ExpectedPeerIdentity.Value))
        {
            result.Value = security.ExpectedPeerIdentity.Value;
        }
        else
        {
            result.Value = security.ExpectedPeerName;
        }

        DiameterValueResult normalized = ValueCodecs.NormalizeDiameterIdentity(result);
        if (normalized.Status == DiameterCodecStatus.Success)
        {
            result.Value = normalized.Text;
        }

        return result;
    }

    internal static DiameterIdentity ReadConfiguredTlsPeerRealm(
        DiameterSecurityConfiguration security,
        DiameterIdentity peerIdentity)
    {
        if (security.ExpectedPeerRealm != null &&
            !string.IsNullOrWhiteSpace(security.ExpectedPeerRealm.Value))
        {
            DiameterIdentity result = new DiameterIdentity();
            result.Value = security.ExpectedPeerRealm.Value;
            DiameterValueResult normalized = ValueCodecs.NormalizeDiameterRealm(result);
            if (normalized.Status == DiameterCodecStatus.Success)
            {
                result.Value = normalized.Text;
            }

            return result;
        }

        return DeriveDiameterRealm(peerIdentity);
    }

    internal static DiameterIdentity DeriveDiameterRealm(DiameterIdentity peerIdentity)
    {
        DiameterIdentity result = new DiameterIdentity();
        DiameterValueResult normalized = ValueCodecs.NormalizeDiameterIdentity(peerIdentity);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            return result;
        }

        int separator = normalized.Text.IndexOf('.');
        if (separator >= 0 && separator + 1 < normalized.Text.Length)
        {
            result.Value = normalized.Text.Substring(separator + 1);
        }
        else
        {
            result.Value = normalized.Text;
        }

        return result;
    }

    internal static void PopulateTlsMetadata(
        DiameterConnectionMetadata metadata,
        SslStream sslStream,
        X509Certificate2 localCertificate,
        string expectedPeerName)
    {
        metadata.LocalEndpoint.Security = DiameterSecurityMode.Tls;
        metadata.RemoteEndpoint.Security = DiameterSecurityMode.Tls;
        metadata.Security.Mode = DiameterSecurityMode.Tls;
        metadata.Security.Authenticated = true;
        metadata.Security.Encrypted = true;
        metadata.Security.MutuallyAuthenticated = sslStream.IsMutuallyAuthenticated;
        metadata.Security.Protocol = sslStream.SslProtocol.ToString();
        metadata.Security.CipherSuite = sslStream.NegotiatedCipherSuite.ToString();
        metadata.Security.LocalCertificateSubject = localCertificate.Subject;
        X509Certificate? remoteValue = sslStream.RemoteCertificate;
        if (remoteValue != null)
        {
            using (X509Certificate2 remote = X509CertificateLoader.LoadCertificate(
                remoteValue.GetRawCertData()))
            {
                metadata.Security.PeerCertificateSubject = remote.Subject;
                metadata.Security.PeerCertificateIssuer = remote.Issuer;
                metadata.Security.PeerCertificateThumbprint = remote.GetCertHashString();
                metadata.Security.PeerCertificateNotBefore =
                    new DateTimeOffset(remote.NotBefore);
                metadata.Security.PeerCertificateNotAfter =
                    new DateTimeOffset(remote.NotAfter);
                AddCertificateDnsNames(metadata.Security, remote);
                if (metadata.Security.PeerCertificateDnsNames.Count == 0 &&
                    !string.IsNullOrWhiteSpace(expectedPeerName))
                {
                    metadata.Security.PeerCertificateDnsNames.Add(expectedPeerName);
                }
            }
        }

        metadata.Open = true;
    }

    internal static void AddCertificateDnsNames(
        DiameterSecurityMetadata metadata,
        X509Certificate2 certificate)
    {
        int extensionIndex = 0;
        while (extensionIndex < certificate.Extensions.Count)
        {
            X509Extension extension = certificate.Extensions[extensionIndex];
            if (extension.Oid != null && extension.Oid.Value == "2.5.29.17")
            {
                X509SubjectAlternativeNameExtension alternativeNames =
                    new X509SubjectAlternativeNameExtension(
                        extension.RawData,
                        extension.Critical);
                foreach (string dnsName in alternativeNames.EnumerateDnsNames())
                {
                    AddCertificateDnsName(metadata, dnsName);
                }
            }

            extensionIndex = extensionIndex + 1;
        }

        if (metadata.PeerCertificateDnsNames.Count == 0)
        {
            AddCertificateDnsName(
                metadata,
                certificate.GetNameInfo(X509NameType.DnsName, false));
        }
    }

    internal static void AddCertificateDnsName(
        DiameterSecurityMetadata metadata,
        string dnsName)
    {
        if (string.IsNullOrWhiteSpace(dnsName))
        {
            return;
        }

        int nameIndex = 0;
        while (nameIndex < metadata.PeerCertificateDnsNames.Count)
        {
            if (string.Equals(
                metadata.PeerCertificateDnsNames[nameIndex],
                dnsName,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            nameIndex = nameIndex + 1;
        }

        metadata.PeerCertificateDnsNames.Add(dnsName);
    }

    internal static X509RevocationMode ReadRevocationMode(bool checkRevocation)
    {
        if (checkRevocation)
        {
            return X509RevocationMode.Online;
        }

        return X509RevocationMode.NoCheck;
    }

    internal static DiameterConnectionResult CreateTlsConnectionFailure(
        DiameterTransportStatus status,
        string code,
        string message,
        DiameterConnectionMetadata metadata)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        result.Metadata = QueuedDiameterConnection.CloneConnectionMetadata(metadata);
        result.Metadata.Open = false;
        result.Metadata.Security.Mode = DiameterSecurityMode.Tls;
        return result;
    }
}

internal class TlsCertificateValidator
{
    internal X509Certificate2? TrustAnchor;
    internal bool CheckRevocation;
    internal string ExpectedPeerName;

    internal TlsCertificateValidator(
        X509Certificate2? trustAnchor,
        bool checkRevocation,
        string expectedPeerName)
    {
        TrustAnchor = trustAnchor;
        CheckRevocation = checkRevocation;
        ExpectedPeerName = expectedPeerName;
    }

    internal bool ValidateTlsCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors errors)
    {
        if (certificate == null)
        {
            return false;
        }

        if ((errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
        {
            return false;
        }

        using (X509Certificate2 peer = X509CertificateLoader.LoadCertificate(
            certificate.GetRawCertData()))
        {
            if (!string.IsNullOrWhiteSpace(ExpectedPeerName))
            {
                string peerName = peer.GetNameInfo(X509NameType.DnsName, false);
                if (!string.Equals(
                    peerName,
                    ExpectedPeerName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (TrustAnchor == null)
            {
                return errors == SslPolicyErrors.None;
            }

            if ((errors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
            {
                return false;
            }

            using (X509Chain customChain = new X509Chain())
            {
                customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                customChain.ChainPolicy.CustomTrustStore.Add(TrustAnchor);
                customChain.ChainPolicy.RevocationMode =
                    TlsCertificateSupport.ReadRevocationMode(CheckRevocation);
                customChain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                return customChain.Build(peer);
            }
        }
    }
}

internal class TlsCertificateLoadResult
{
    internal DiameterTransportStatus Status;
    internal X509Certificate2? LocalCertificate;
    internal X509Certificate2? TrustAnchor;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}
