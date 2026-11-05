using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailPolicy"/>.
/// </summary>
public class EmailPolicyOperations : IEmailPolicyOperationsContract
{
    /// <summary>
    /// Creates a preserve configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    public EmailPolicy CreatePreserve()
    {
        return EmailPolicy.CreatePreserveImplementation();
    }

    /// <summary>
    /// Creates a strict internet configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    public EmailPolicy CreateStrictInternet()
    {
        return EmailPolicy.CreateStrictInternetImplementation();
    }

    /// <summary>
    /// Creates a tolerant internet configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    public EmailPolicy CreateTolerantInternet()
    {
        return EmailPolicy.CreateTolerantInternetImplementation();
    }

    /// <summary>
    /// Creates a forensic configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    public EmailPolicy CreateForensic()
    {
        return EmailPolicy.CreateForensicImplementation();
    }

    /// <summary>
    /// Creates a seven bit transport configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    public EmailPolicy CreateSevenBitTransport()
    {
        return EmailPolicy.CreateSevenBitTransportImplementation();
    }

    /// <summary>
    /// Creates a smtp UTF-8 configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    public EmailPolicy CreateSmtpUtf8()
    {
        return EmailPolicy.CreateSmtpUtf8Implementation();
    }

    /// <summary>
    /// Creates a safe display configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    public EmailPolicy CreateSafeDisplay()
    {
        return EmailPolicy.CreateSafeDisplayImplementation();
    }

    /// <summary>
    /// Creates a deterministic test configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    public EmailPolicy CreateDeterministicTest()
    {
        return EmailPolicy.CreateDeterministicTestImplementation();
    }
}
