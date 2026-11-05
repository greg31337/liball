namespace pop3;

/// <summary>Provides the default encrypted POP3 server TLS handshake.</summary>
public class Pop3DefaultServerTlsProvider : IPop3ServerTlsProvider
{
    /// <summary>Creates a server TLS provider that uses platform certificate validation.</summary>
    public Pop3DefaultServerTlsProvider()
    {
    }

    /// <summary>Creates and authenticates a TLS stream over an accepted transport.</summary>
    /// <param name="stream">The accepted readable and writable transport stream.</param>
    /// <param name="options">The certificate, protocol, client-certificate, revocation, and ownership policy.</param>
    /// <param name="cancellationToken">A token that can cancel the blocking TLS handshake.</param>
    /// <returns>The authenticated TLS stream.</returns>
    public System.IO.Stream SecureServerStream(
        System.IO.Stream stream,
        Pop3TlsServerOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        ValidateInputs(stream, options);
        cancellationToken.ThrowIfCancellationRequested();

        System.Net.Security.SslServerAuthenticationOptions authenticationOptions =
            CreateAuthenticationOptions(options);
        System.Net.Security.SslStream sslStream = new System.Net.Security.SslStream(
            stream,
            options.LeaveInnerStreamOpen);
        System.Threading.Tasks.Task handshakeTask = sslStream.AuthenticateAsServerAsync(
            authenticationOptions,
            cancellationToken);
        System.Threading.Tasks.Task<System.IO.Stream> completionTask =
            handshakeTask.ContinueWith(
                task => CompleteHandshake(task, sslStream, cancellationToken),
                System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
                System.Threading.Tasks.TaskScheduler.Default);
        return completionTask.GetAwaiter().GetResult();
    }

    internal System.Net.Security.SslServerAuthenticationOptions CreateAuthenticationOptions(
        Pop3TlsServerOptions options)
    {
        System.Net.Security.SslServerAuthenticationOptions authenticationOptions =
            new System.Net.Security.SslServerAuthenticationOptions();
        authenticationOptions.ServerCertificate = options.ServerCertificate;
        authenticationOptions.EnabledSslProtocols = options.EnabledProtocols;
        authenticationOptions.ClientCertificateRequired =
            options.RequireClientCertificate;
        authenticationOptions.CertificateRevocationCheckMode =
            options.CheckCertificateRevocation ?
                System.Security.Cryptography.X509Certificates.X509RevocationMode.Online :
                System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
        authenticationOptions.EncryptionPolicy =
            System.Net.Security.EncryptionPolicy.RequireEncryption;
        return authenticationOptions;
    }

    void ValidateInputs(
        System.IO.Stream stream,
        Pop3TlsServerOptions options)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }
        if (!stream.CanRead)
        {
            throw new System.ArgumentException(
                "The POP3 TLS transport must be readable.",
                "stream");
        }
        if (!stream.CanWrite)
        {
            throw new System.ArgumentException(
                "The POP3 TLS transport must be writable.",
                "stream");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (options.ServerCertificate == null)
        {
            throw new System.ArgumentException(
                "A POP3 TLS server certificate is required.",
                "options");
        }
        if (!options.ServerCertificate.HasPrivateKey)
        {
            throw new System.ArgumentException(
                "The POP3 TLS server certificate must have a private key.",
                "options");
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
            "The POP3 server TLS handshake failed.",
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
