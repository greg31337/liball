using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPartialMessageCollector"/>.
/// </summary>
public interface IEmailPartialMessageCollectorContract
{
    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    EmailPartialMessageAssemblyOptions GetOptions();
    /// <summary>
    /// Adds a value to the current collection or model.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void Add(EmailMessage message);
    /// <summary>
    /// Adds a value to the current collection or model.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void AddEntity(MimeEntity entity);
    /// <summary>
    /// Gets the observed fragment count.
    /// </summary>
    /// <returns>The current observed fragment count.</returns>
    int GetObservedFragmentCount();
    /// <summary>
    /// Gets the rejected fragment count.
    /// </summary>
    /// <returns>The current rejected fragment count.</returns>
    int GetRejectedFragmentCount();
    /// <summary>
    /// Gets the group count.
    /// </summary>
    /// <returns>The current group count.</returns>
    int GetGroupCount();
    /// <summary>
    /// Gets the group ID at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested group ID.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    string GetGroupIdAt(int index);
    /// <summary>
    /// Gets the group ids.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current group ids.</returns>
    List<string> GetGroupIds();
    /// <summary>
    /// Gets the fragment count.
    /// </summary>
    /// <param name = "fragmentId">The fragment ID used by the operation.</param>
    /// <returns>The current fragment count.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    int GetFragmentCount(string fragmentId);
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Assembles the supplied value.
    /// </summary>
    /// <param name = "fragmentId">The fragment ID used by the operation.</param>
    /// <returns>The partial message assembly result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailPartialMessageAssemblyResult Assemble(string fragmentId);
    /// <summary>
    /// Assembles the all.
    /// </summary>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailPartialMessageAssemblyResult> AssembleAll();
}
