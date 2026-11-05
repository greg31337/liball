using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeader"/>.
/// </summary>
public interface IEmailHeaderContract
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string GetName();
    /// <summary>
    /// Gets the normalized name.
    /// </summary>
    /// <returns>The current normalized name.</returns>
    string GetNormalizedName();
    /// <summary>
    /// Gets the original name.
    /// </summary>
    /// <returns>The current original name.</returns>
    string GetOriginalName();
    /// <summary>
    /// Gets the name bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current name bytes.</returns>
    byte[] GetNameBytes();
    /// <summary>
    /// Gets the original name bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original name bytes.</returns>
    byte[] GetOriginalNameBytes();
    /// <summary>
    /// Gets the raw value.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw value.</returns>
    byte[] GetRawValue();
    /// <summary>
    /// Gets the original raw value.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original raw value.</returns>
    byte[] GetOriginalRawValue();
    /// <summary>
    /// Gets the unfolded value.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current unfolded value.</returns>
    byte[] GetUnfoldedValue();
    /// <summary>
    /// Gets the decoded value.
    /// </summary>
    /// <returns>The current decoded value.</returns>
    string? GetDecodedValue();
    /// <summary>
    /// Gets the value kind.
    /// </summary>
    /// <returns>The current value kind.</returns>
    EmailHeaderValueKind GetValueKind();
    /// <summary>
    /// Gets the structured value.
    /// </summary>
    /// <returns>The current structured value.</returns>
    object? GetStructuredValue();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Adds an issue to the current collection or model.
    /// </summary>
    /// <param name = "issue">The issue used by the operation.</param>
    void AddIssue(EmailIssue issue);
    /// <summary>
    /// Gets the fold count.
    /// </summary>
    /// <returns>The current fold count.</returns>
    int GetFoldCount();
    /// <summary>
    /// Gets the fold.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current fold.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailHeaderFold GetFold(int index);
    /// <summary>
    /// Gets the folds.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current folds.</returns>
    List<EmailHeaderFold> GetFolds();
    /// <summary>
    /// Gets the original fold count.
    /// </summary>
    /// <returns>The current original fold count.</returns>
    int GetOriginalFoldCount();
    /// <summary>
    /// Gets the original fold.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current original fold.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailHeaderFold GetOriginalFold(int index);
    /// <summary>
    /// Gets the line ending.
    /// </summary>
    /// <returns>The current line ending.</returns>
    EmailLineEnding GetLineEnding();
    /// <summary>
    /// Gets the original line ending.
    /// </summary>
    /// <returns>The current original line ending.</returns>
    EmailLineEnding GetOriginalLineEnding();
    /// <summary>
    /// Determines whether the source range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source range is available; otherwise, <c>false</c>.</returns>
    bool HasSourceRange();
    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    EmailRange? GetSourceRange();
    /// <summary>
    /// Gets the value source position.
    /// </summary>
    /// <returns>The current value source position.</returns>
    EmailSourcePosition? GetValueSourcePosition();
    /// <summary>
    /// Gets the raw field bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw field bytes.</returns>
    byte[] GetRawFieldBytes();
    /// <summary>
    /// Gets the original raw field bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original raw field bytes.</returns>
    byte[] GetOriginalRawFieldBytes();
    /// <summary>
    /// Gets the dirty state.
    /// </summary>
    /// <returns>The current dirty state.</returns>
    EmailHeaderDirtyState GetDirtyState();
    /// <summary>
    /// Determines whether the current value or state is dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is dirty; otherwise, <c>false</c>.</returns>
    bool IsDirty();
    /// <summary>
    /// Determines whether the current value or state is name dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is name dirty; otherwise, <c>false</c>.</returns>
    bool IsNameDirty();
    /// <summary>
    /// Determines whether the current value or state is value dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is value dirty; otherwise, <c>false</c>.</returns>
    bool IsValueDirty();
    /// <summary>
    /// Determines whether the current value or state is line ending dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is line ending dirty; otherwise, <c>false</c>.</returns>
    bool IsLineEndingDirty();
    /// <summary>
    /// Sets the name used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetName(string value);
    /// <summary>
    /// Sets the value used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetValue(string value);
    /// <summary>
    /// Sets the raw value used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetRawValue(ReadOnlySpan<byte> value);
    /// <summary>
    /// Sets the line ending used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetLineEnding(EmailLineEnding value);
    /// <summary>
    /// Sets the value kind used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetValueKind(EmailHeaderValueKind value);
    /// <summary>
    /// Sets the parse mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetParseMode(EmailStructuredValueParseMode value);
    /// <summary>
    /// Gets the parse mode.
    /// </summary>
    /// <returns>The current parse mode.</returns>
    EmailStructuredValueParseMode GetParseMode();
    /// <summary>
    /// Resets the to original.
    /// </summary>
    void ResetToOriginal();
    /// <summary>
    /// Accepts the changes.
    /// </summary>
    void AcceptChanges();
    /// <summary>
    /// Detaches the source.
    /// </summary>
    void DetachSource();
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailHeader Clone();
}
