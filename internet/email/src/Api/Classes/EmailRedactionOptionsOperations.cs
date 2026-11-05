using System;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailRedactionOptions"/>.
/// </summary>
public class EmailRedactionOptionsOperations : IEmailRedactionOptionsOperationsContract
{
    /// <summary>
    /// Creates an all configured for the requested behavior.
    /// </summary>
    /// <returns>The redaction options produced by the operation.</returns>
    public EmailRedactionOptions CreateAll()
    {
        return EmailRedactionOptions.CreateAllImplementation();
    }
}
