namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailIssueCodeRanges"/>.
/// </summary>
public class EmailIssueCodeRangesOperations : IEmailIssueCodeRangesOperationsContract
{
    /// <summary>
    /// Determines whether the current value or state is library code.
    /// </summary>
    /// <param name = "code">The code used by the operation.</param>
    /// <returns><c>true</c> when the value or state is library code; otherwise, <c>false</c>.</returns>
    public bool IsLibraryCode(int code)
    {
        return EmailIssueCodeRanges.IsLibraryCodeImplementation(code);
    }

    /// <summary>
    /// Gets the range start.
    /// </summary>
    /// <param name = "code">The code used by the operation.</param>
    /// <returns>The current range start.</returns>
    public int GetRangeStart(EmailIssueCode code)
    {
        return EmailIssueCodeRanges.GetRangeStartImplementation(code);
    }
}
