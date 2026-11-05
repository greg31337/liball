using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPolicy"/>.
/// </summary>
public interface IEmailPolicyOperationsContract
{
    /// <summary>
    /// Creates a preserve configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    EmailPolicy CreatePreserve();
    /// <summary>
    /// Creates a strict internet configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    EmailPolicy CreateStrictInternet();
    /// <summary>
    /// Creates a tolerant internet configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    EmailPolicy CreateTolerantInternet();
    /// <summary>
    /// Creates a forensic configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    EmailPolicy CreateForensic();
    /// <summary>
    /// Creates a seven bit transport configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    EmailPolicy CreateSevenBitTransport();
    /// <summary>
    /// Creates a smtp UTF-8 configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    EmailPolicy CreateSmtpUtf8();
    /// <summary>
    /// Creates a safe display configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    EmailPolicy CreateSafeDisplay();
    /// <summary>
    /// Creates a deterministic test configured for the requested behavior.
    /// </summary>
    /// <returns>The policy produced by the operation.</returns>
    EmailPolicy CreateDeterministicTest();
}
