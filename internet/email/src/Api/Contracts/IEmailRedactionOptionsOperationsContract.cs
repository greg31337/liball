using System;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRedactionOptions"/>.
/// </summary>
public interface IEmailRedactionOptionsOperationsContract
{
    /// <summary>
    /// Creates an all configured for the requested behavior.
    /// </summary>
    /// <returns>The redaction options produced by the operation.</returns>
    EmailRedactionOptions CreateAll();
}
