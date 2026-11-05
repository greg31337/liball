using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPolicy"/>.
/// </summary>
public interface IEmailPolicyContract
{
    /// <summary>
    /// Creates a parse options configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    EmailParseOptions CreateParseOptions();
    /// <summary>
    /// Creates a write options configured for the requested behavior.
    /// </summary>
    /// <returns>The write options produced by the operation.</returns>
    EmailWriteOptions CreateWriteOptions();
    /// <summary>
    /// Creates a validation options configured for the requested behavior.
    /// </summary>
    /// <returns>The validation options produced by the operation.</returns>
    EmailValidationOptions CreateValidationOptions();
    /// <summary>
    /// Creates a normalization options configured for the requested behavior.
    /// </summary>
    /// <returns>The normalization options produced by the operation.</returns>
    EmailNormalizationOptions CreateNormalizationOptions();
    /// <summary>
    /// Creates a generation options configured for the requested behavior.
    /// </summary>
    /// <returns>The generation options produced by the operation.</returns>
    EmailGenerationOptions CreateGenerationOptions();
    /// <summary>
    /// Creates a redaction options configured for the requested behavior.
    /// </summary>
    /// <returns>The redaction options produced by the operation.</returns>
    EmailRedactionOptions CreateRedactionOptions();
    /// <summary>
    /// Creates a display options configured for the requested behavior.
    /// </summary>
    /// <returns>The display options produced by the operation.</returns>
    EmailDisplayOptions CreateDisplayOptions();
    /// <summary>
    /// Creates a canonical comparison options configured for the requested behavior.
    /// </summary>
    /// <returns>The canonical comparison options produced by the operation.</returns>
    EmailCanonicalComparisonOptions CreateCanonicalComparisonOptions();
}
