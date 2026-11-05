namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailIssueCodeRanges"/>.
/// </summary>
public interface IEmailIssueCodeRangesOperationsContract
{
    /// <summary>
    /// Determines whether the current value or state is library code.
    /// </summary>
    /// <param name = "code">The code used by the operation.</param>
    /// <returns><c>true</c> when the value or state is library code; otherwise, <c>false</c>.</returns>
    bool IsLibraryCode(int code);
    /// <summary>
    /// Gets the range start.
    /// </summary>
    /// <param name = "code">The code used by the operation.</param>
    /// <returns>The current range start.</returns>
    int GetRangeStart(EmailIssueCode code);
}
