namespace pop3;

/// <summary>Provides the default platform-validated POP3 client TLS handshake.</summary>
public class Pop3DefaultClientTlsProvider : IPop3ClientTlsProvider
{
    /// <summary>Creates a client TLS provider that uses the platform certificate trust policy.</summary>
    public Pop3DefaultClientTlsProvider()
    {
    }

    /// <summary>Creates and authenticates a TLS stream over an established transport.</summary>
    /// <param name="stream">The established readable and writable transport stream.</param>
    /// <param name="options">The target identity, protocol, revocation, certificate, and ownership policy.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking TLS handshake.</param>
    /// <returns>The authenticated TLS stream.</returns>
    public System.IO.Stream SecureClientStream(
        System.IO.Stream stream,
        Pop3TlsClientOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateInputs(stream, options);
        cancellationToken.ThrowIfCancellationRequested();

        System.Net.Security.SslClientAuthenticationOptions authenticationOptions =
            CreateAuthenticationOptions(options);
        System.Net.Security.SslStream sslStream = new System.Net.Security.SslStream(
            stream,
            options.LeaveInnerStreamOpen);
        System.Threading.Tasks.Task handshakeTask = sslStream.AuthenticateAsClientAsync(
            authenticationOptions,
            cancellationToken);
        System.Threading.Tasks.Task<System.IO.Stream> completionTask = handshakeTask.ContinueWith(
            task => CompleteHandshake(task, sslStream, cancellationToken),
            System.Threading.CancellationToken.None,
            System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
            System.Threading.Tasks.TaskScheduler.Default);
        return completionTask.GetAwaiter().GetResult();
    }

    internal System.Net.Security.SslClientAuthenticationOptions CreateAuthenticationOptions(
        Pop3TlsClientOptions options)
    {
        System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates =
            new System.Security.Cryptography.X509Certificates.X509CertificateCollection();
        clientCertificates.AddRange(options.ClientCertificates);

        System.Net.Security.SslClientAuthenticationOptions authenticationOptions =
            new System.Net.Security.SslClientAuthenticationOptions();
        authenticationOptions.TargetHost = options.TargetHost;
        authenticationOptions.EnabledSslProtocols = options.EnabledProtocols;
        authenticationOptions.CertificateRevocationCheckMode = options.CheckCertificateRevocation ?
            System.Security.Cryptography.X509Certificates.X509RevocationMode.Online :
            System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
        authenticationOptions.ClientCertificates = clientCertificates;
        authenticationOptions.EncryptionPolicy = System.Net.Security.EncryptionPolicy.RequireEncryption;
        return authenticationOptions;
    }

    void ValidateInputs(System.IO.Stream stream, Pop3TlsClientOptions options)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (!stream.CanRead)
        {
            throw new System.ArgumentException("The POP3 TLS transport must be readable.", "stream");
        }
        if (!stream.CanWrite)
        {
            throw new System.ArgumentException("The POP3 TLS transport must be writable.", "stream");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (options.TargetHost == null || string.IsNullOrWhiteSpace(options.TargetHost))
        {
            throw new System.ArgumentException("A POP3 TLS target host or service identity is required.", "options");
        }
        if (options.ClientCertificates == null)
        {
            throw new System.ArgumentException("The POP3 TLS client certificate collection cannot be null.", "options");
        }

        System.Security.Authentication.SslProtocols modernProtocols =
            System.Security.Authentication.SslProtocols.Tls12 |
            System.Security.Authentication.SslProtocols.Tls13;
        if ((options.EnabledProtocols & modernProtocols) == 0 ||
            (options.EnabledProtocols & ~modernProtocols) != 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
    }

    System.IO.Stream CompleteHandshake(
        System.Threading.Tasks.Task task,
        System.Net.Security.SslStream sslStream,
        System.Threading.CancellationToken cancellationToken)
    {
        if (task.IsCompletedSuccessfully)
        {
            return sslStream;
        }

        System.Exception? failure = GetTaskFailure(task);
        sslStream.Dispose();
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        throw new System.Security.Authentication.AuthenticationException(
            "The POP3 client TLS handshake failed.",
            failure);
    }

    System.Exception? GetTaskFailure(System.Threading.Tasks.Task task)
    {
        if (task.Exception == null)
        {
            return null;
        }
        return task.Exception.GetBaseException();
    }
}
