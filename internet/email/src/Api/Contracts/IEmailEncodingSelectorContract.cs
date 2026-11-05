using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEncodingSelector"/>.
/// </summary>
public interface IEmailEncodingSelectorContract
{
    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <param name = "constraint">The constraint used by the operation.</param>
    /// <returns>The encoding selection result produced by the operation.</returns>
    EmailEncodingSelectionResult SelectWithInputAndMediaTypeAndConstraint(ReadOnlySpan<byte> input, string mediaType, EmailTransferEncodingConstraint constraint);
    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The encoding selection result produced by the operation.</returns>
    EmailEncodingSelectionResult SelectWithInputAndOptions(ReadOnlySpan<byte> input, EmailEncodingSelectionOptions options);
    /// <summary>
    /// Chooses the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <param name = "constraint">The constraint used by the operation.</param>
    /// <returns>The encoding selection result produced by the operation.</returns>
    EmailEncodingSelectionResult ChooseWithInputAndMediaTypeAndConstraint(ReadOnlySpan<byte> input, string mediaType, EmailTransferEncodingConstraint constraint);
    /// <summary>
    /// Chooses the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The encoding selection result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailEncodingSelectionResult ChooseWithInputAndOptions(ReadOnlySpan<byte> input, EmailEncodingSelectionOptions options);
}
