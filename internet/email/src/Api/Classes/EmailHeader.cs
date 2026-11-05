using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents one message header field while retaining raw, unfolded, decoded, and structured forms when available.
/// </summary>
public class EmailHeader : IEmailHeaderContract
{
    byte[]? originalNameBytes;
    byte[]? nameBytes;
    byte[]? originalRawValue;
    byte[]? originalRawFieldBytes;
    byte[]? rawValue;
    EmailLineEnding originalLineEnding;
    EmailLineEnding lineEnding;
    EmailRange? sourceRange;
    EmailSourcePosition? valuePosition;
    EmailHeaderValueKind valueKind;
    EmailStructuredValueParseMode parseMode;
    EmailCompatibilityOptions compatibilityOptions = new EmailCompatibilityOptions();
    EmailIssueList? baseIssues;
    List<EmailHeaderFold>? originalFolds;
    List<EmailHeaderFold>? folds;
    byte[]? unfoldedValue;
    string? decodedValue;
    object? structuredValue;
    EmailIssueList? decodedIssues;
    EmailIssueList? structuredIssues;
    bool unfoldedCached;
    bool decodedCached;
    bool structuredCached;
    bool nameDirty;
    bool valueDirty;
    bool lineEndingDirty;
    Action? mutationGuard;
    /// <summary>
    /// Creates a header with an unknown structured value kind.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "rawValue">The raw value used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHeader(string name, byte[] rawValue)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        Initialize(Encoding.UTF8.GetBytes(name), rawValue, EmailLineEnding.None, null, new EmailSourcePosition(0, 1, 1), EmailHeaderValueKind.Unknown, EmailStructuredValueParseMode.Tolerant, new EmailIssueList());
    }

    internal static EmailHeader CreateImplementationWithNameAndRawValue(string name, byte[] rawValue)
    {
        return new EmailHeader(name, rawValue);
    }

    internal static EmailHeader CreateImplementationWithNameAndRawValueAndValueKind(string name, byte[] rawValue, EmailHeaderValueKind valueKind)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        EmailHeader value = new EmailHeader(name, rawValue);
        value.Initialize(Encoding.UTF8.GetBytes(name), rawValue, EmailLineEnding.None, null, new EmailSourcePosition(0, 1, 1), valueKind, EmailStructuredValueParseMode.Tolerant, new EmailIssueList());
        return value;
    }

    internal static EmailHeader CreateImplementationWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssues(byte[] originalNameBytes, byte[] originalRawValue, EmailLineEnding lineEnding, EmailRange sourceRange, EmailSourcePosition valuePosition, EmailHeaderValueKind valueKind, EmailStructuredValueParseMode parseMode, EmailIssueList issues)
    {
        EmailHeader value = new EmailHeader("X", Array.Empty<byte>());
        value.Initialize(originalNameBytes, originalRawValue, lineEnding, sourceRange, valuePosition, valueKind, parseMode, issues);
        return value;
    }

    internal static EmailHeader CreateImplementationWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssuesAndExactOriginalRawFieldBytes(byte[]? originalNameBytes, byte[]? originalRawValue, EmailLineEnding lineEnding, EmailRange? sourceRange, EmailSourcePosition? valuePosition, EmailHeaderValueKind valueKind, EmailStructuredValueParseMode parseMode, EmailIssueList? issues, byte[] exactOriginalRawFieldBytes)
    {
        if (exactOriginalRawFieldBytes == null)
        {
            throw new ArgumentNullException("exactOriginalRawFieldBytes");
        }

        EmailHeader value = new EmailHeader("X", Array.Empty<byte>());
        value.Initialize(originalNameBytes, originalRawValue, lineEnding, sourceRange, valuePosition, valueKind, parseMode, issues);
        value.originalRawFieldBytes = Copy(exactOriginalRawFieldBytes);
        return value;
    }

    void Initialize(byte[]? suppliedNameBytes, byte[]? suppliedRawValue, EmailLineEnding suppliedLineEnding, EmailRange? suppliedSourceRange, EmailSourcePosition? suppliedValuePosition, EmailHeaderValueKind suppliedValueKind, EmailStructuredValueParseMode suppliedParseMode, EmailIssueList? suppliedIssues)
    {
        if (suppliedNameBytes == null || suppliedRawValue == null)
        {
            throw new ArgumentNullException("suppliedNameBytes");
        }

        if (suppliedLineEnding < EmailLineEnding.None || suppliedLineEnding > EmailLineEnding.Cr)
        {
            throw new ArgumentOutOfRangeException("suppliedLineEnding");
        }

        if (suppliedValuePosition == null || suppliedIssues == null)
        {
            throw new ArgumentNullException("suppliedValuePosition");
        }

        ValidateValueKind(suppliedValueKind);
        ValidateParseMode(suppliedParseMode);
        originalNameBytes = Copy(suppliedNameBytes);
        nameBytes = Copy(suppliedNameBytes);
        originalRawValue = Copy(suppliedRawValue);
        originalRawFieldBytes = null;
        rawValue = Copy(suppliedRawValue);
        originalLineEnding = suppliedLineEnding;
        lineEnding = suppliedLineEnding;
        sourceRange = suppliedSourceRange;
        valuePosition = suppliedValuePosition;
        valueKind = suppliedValueKind;
        parseMode = suppliedParseMode;
        compatibilityOptions = new EmailCompatibilityOptions();
        baseIssues = new EmailIssueList();
        baseIssues.AddRange(suppliedIssues);
        originalFolds = ScanFolds(originalRawValue);
        folds = ScanFolds(rawValue);
        decodedIssues = new EmailIssueList();
        structuredIssues = new EmailIssueList();
        unfoldedValue = null;
        decodedValue = null;
        structuredValue = null;
        unfoldedCached = false;
        decodedCached = false;
        structuredCached = false;
        nameDirty = false;
        valueDirty = false;
        lineEndingDirty = false;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string GetName()
    {
        return Encoding.UTF8.GetString((nameBytes ?? throw new InvalidOperationException()));
    }

    /// <summary>
    /// Gets the normalized name.
    /// </summary>
    /// <returns>The current normalized name.</returns>
    public string GetNormalizedName()
    {
        return GetName().ToLowerInvariant();
    }

    /// <summary>
    /// Gets the original name.
    /// </summary>
    /// <returns>The current original name.</returns>
    public string GetOriginalName()
    {
        return Encoding.UTF8.GetString((originalNameBytes ?? throw new InvalidOperationException()));
    }

    /// <summary>
    /// Gets the name bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current name bytes.</returns>
    public byte[] GetNameBytes()
    {
        return Copy(nameBytes);
    }

    /// <summary>
    /// Gets the original name bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original name bytes.</returns>
    public byte[] GetOriginalNameBytes()
    {
        return Copy(originalNameBytes);
    }

    /// <summary>
    /// Gets the raw value.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw value.</returns>
    public byte[] GetRawValue()
    {
        return Copy(rawValue);
    }

    /// <summary>
    /// Gets the original raw value.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original raw value.</returns>
    public byte[] GetOriginalRawValue()
    {
        return Copy(originalRawValue);
    }

    /// <summary>
    /// Gets the unfolded value.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current unfolded value.</returns>
    public byte[] GetUnfoldedValue()
    {
        EnsureUnfolded();
        return Copy(unfoldedValue);
    }

    /// <summary>
    /// Gets the decoded value.
    /// </summary>
    /// <returns>The current decoded value.</returns>
    public string? GetDecodedValue()
    {
        if (!decodedCached)
        {
            EnsureUnfolded();
            EmailHeaderDecodeResult result = EmailHeaderDecoder.Decode(unfoldedValue, valueKind, parseMode, valuePosition, compatibilityOptions.GetAllowBrokenEncodedWords());
            decodedValue = result.GetValue();
            decodedIssues = result.GetIssues();
            decodedCached = true;
        }

        return decodedValue;
    }

    /// <summary>
    /// Gets the value kind.
    /// </summary>
    /// <returns>The current value kind.</returns>
    public EmailHeaderValueKind GetValueKind()
    {
        return valueKind;
    }

    /// <summary>
    /// Gets the structured value.
    /// </summary>
    /// <returns>The current structured value.</returns>
    public object? GetStructuredValue()
    {
        if (!structuredCached)
        {
            if (valueKind == EmailHeaderValueKind.Unknown)
            {
                structuredValue = null;
                structuredIssues = new EmailIssueList();
            }
            else if (valueKind == EmailHeaderValueKind.Unstructured)
            {
                structuredValue = GetDecodedValue();
                structuredIssues = new EmailIssueList();
            }
            else
            {
                EnsureUnfolded();
                EmailHeaderStructuredResult result = EmailHeaderStructuredParser.Parse(valueKind, unfoldedValue, parseMode, valuePosition, GetName(), compatibilityOptions.GetAllowBrokenRfc2231Parameters());
                structuredValue = result.GetValue();
                structuredIssues = result.GetIssues();
            }

            structuredCached = true;
        }

        return structuredValue;
    }

    internal bool HasDecodedValueCache()
    {
        return decodedCached;
    }

    internal bool HasStructuredValueCache()
    {
        return structuredCached;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList issues = new EmailIssueList();
        issues.AddRange(baseIssues);
        issues.AddRange(decodedIssues);
        issues.AddRange(structuredIssues);
        return issues;
    }

    /// <summary>
    /// Adds an issue to the current collection or model.
    /// </summary>
    /// <param name = "issue">The issue used by the operation.</param>
    public void AddIssue(EmailIssue issue)
    {
        (baseIssues ?? throw new InvalidOperationException()).Add(issue);
    }

    /// <summary>
    /// Gets the fold count.
    /// </summary>
    /// <returns>The current fold count.</returns>
    public int GetFoldCount()
    {
        return (folds ?? throw new InvalidOperationException()).Count;
    }

    /// <summary>
    /// Gets the fold.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current fold.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHeaderFold GetFold(int index)
    {
        if (index < 0 || index >= (folds ?? throw new InvalidOperationException()).Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return folds[index];
    }

    /// <summary>
    /// Gets the folds.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current folds.</returns>
    public List<EmailHeaderFold> GetFolds()
    {
        return new List<EmailHeaderFold>((folds ?? throw new InvalidOperationException()));
    }

    /// <summary>
    /// Gets the original fold count.
    /// </summary>
    /// <returns>The current original fold count.</returns>
    public int GetOriginalFoldCount()
    {
        return (originalFolds ?? throw new InvalidOperationException()).Count;
    }

    /// <summary>
    /// Gets the original fold.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current original fold.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHeaderFold GetOriginalFold(int index)
    {
        if (index < 0 || index >= (originalFolds ?? throw new InvalidOperationException()).Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return originalFolds[index];
    }

    /// <summary>
    /// Gets the line ending.
    /// </summary>
    /// <returns>The current line ending.</returns>
    public EmailLineEnding GetLineEnding()
    {
        return lineEnding;
    }

    /// <summary>
    /// Gets the original line ending.
    /// </summary>
    /// <returns>The current original line ending.</returns>
    public EmailLineEnding GetOriginalLineEnding()
    {
        return originalLineEnding;
    }

    /// <summary>
    /// Determines whether the source range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source range is available; otherwise, <c>false</c>.</returns>
    public bool HasSourceRange()
    {
        return sourceRange != null;
    }

    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    public EmailRange? GetSourceRange()
    {
        return sourceRange;
    }

    /// <summary>
    /// Gets the value source position.
    /// </summary>
    /// <returns>The current value source position.</returns>
    public EmailSourcePosition? GetValueSourcePosition()
    {
        return valuePosition;
    }

    /// <summary>
    /// Gets the raw field bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw field bytes.</returns>
    public byte[] GetRawFieldBytes()
    {
        if (!IsDirty() && originalRawFieldBytes != null)
        {
            return Copy(originalRawFieldBytes);
        }

        return CreateFieldBytes(nameBytes, rawValue, lineEnding);
    }

    /// <summary>
    /// Gets the original raw field bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original raw field bytes.</returns>
    public byte[] GetOriginalRawFieldBytes()
    {
        if (originalRawFieldBytes != null)
        {
            return Copy(originalRawFieldBytes);
        }

        return CreateFieldBytes(originalNameBytes, originalRawValue, originalLineEnding);
    }

    /// <summary>
    /// Gets the dirty state.
    /// </summary>
    /// <returns>The current dirty state.</returns>
    public EmailHeaderDirtyState GetDirtyState()
    {
        int count = 0;
        if (nameDirty)
        {
            count++;
        }

        if (valueDirty)
        {
            count++;
        }

        if (lineEndingDirty)
        {
            count++;
        }

        if (count == 0)
        {
            return EmailHeaderDirtyState.Clean;
        }

        if (count > 1)
        {
            return EmailHeaderDirtyState.MultipleChanges;
        }

        if (nameDirty)
        {
            return EmailHeaderDirtyState.NameChanged;
        }

        return valueDirty ? EmailHeaderDirtyState.ValueChanged : EmailHeaderDirtyState.LineEndingChanged;
    }

    /// <summary>
    /// Determines whether the current value or state is dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is dirty; otherwise, <c>false</c>.</returns>
    public bool IsDirty()
    {
        return nameDirty || valueDirty || lineEndingDirty;
    }

    /// <summary>
    /// Determines whether the current value or state is name dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is name dirty; otherwise, <c>false</c>.</returns>
    public bool IsNameDirty()
    {
        return nameDirty;
    }

    /// <summary>
    /// Determines whether the current value or state is value dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is value dirty; otherwise, <c>false</c>.</returns>
    public bool IsValueDirty()
    {
        return valueDirty;
    }

    /// <summary>
    /// Determines whether the current value or state is line ending dirty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is line ending dirty; otherwise, <c>false</c>.</returns>
    public bool IsLineEndingDirty()
    {
        return lineEndingDirty;
    }

    /// <summary>
    /// Sets the name used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetName(string value)
    {
        ValidateMutableName(value);
        EnsureMutationAllowed();
        nameBytes = Encoding.ASCII.GetBytes(value);
        nameDirty = !Equal(nameBytes, originalNameBytes);
        InvalidateStructured();
    }

    /// <summary>
    /// Sets the value used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\r' || value[index] == '\n' || value[index] == 0)
            {
                throw new ArgumentException("Header values cannot contain line breaks or NUL characters.", "value");
            }

            index++;
        }

        SetValueBytes(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Sets the raw value used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetRawValue(ReadOnlySpan<byte> value)
    {
        byte[] bytes = value.ToArray();
        ValidateMutableRawValue(bytes);
        SetValueBytes(bytes);
    }

    /// <summary>
    /// Sets the line ending used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetLineEnding(EmailLineEnding value)
    {
        if (value < EmailLineEnding.None || value > EmailLineEnding.Cr)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        EnsureMutationAllowed();
        lineEnding = value;
        lineEndingDirty = lineEnding != originalLineEnding;
    }

    /// <summary>
    /// Sets the value kind used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetValueKind(EmailHeaderValueKind value)
    {
        ValidateValueKind(value);
        if (valueKind != value)
        {
            EnsureMutationAllowed();
            valueKind = value;
            InvalidateAllCaches();
        }
    }

    /// <summary>
    /// Sets the parse mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetParseMode(EmailStructuredValueParseMode value)
    {
        ValidateParseMode(value);
        if (parseMode != value)
        {
            EnsureMutationAllowed();
            parseMode = value;
            InvalidateAllCaches();
        }
    }

    /// <summary>
    /// Gets the parse mode.
    /// </summary>
    /// <returns>The current parse mode.</returns>
    public EmailStructuredValueParseMode GetParseMode()
    {
        return parseMode;
    }

    /// <summary>
    /// Resets the to original.
    /// </summary>
    public void ResetToOriginal()
    {
        EnsureMutationAllowed();
        nameBytes = Copy(originalNameBytes);
        rawValue = Copy(originalRawValue);
        lineEnding = originalLineEnding;
        folds = ScanFolds(rawValue);
        nameDirty = false;
        valueDirty = false;
        lineEndingDirty = false;
        InvalidateAllCaches();
    }

    /// <summary>
    /// Accepts the changes.
    /// </summary>
    public void AcceptChanges()
    {
        originalNameBytes = Copy(nameBytes);
        originalRawValue = Copy(rawValue);
        originalLineEnding = lineEnding;
        originalRawFieldBytes = null;
        originalFolds = ScanFolds(originalRawValue);
        sourceRange = null;
        valuePosition = new EmailSourcePosition(0, 1, 1);
        nameDirty = false;
        valueDirty = false;
        lineEndingDirty = false;
    }

    /// <summary>
    /// Detaches the source.
    /// </summary>
    public void DetachSource()
    {
        byte[] exactFieldBytes = GetRawFieldBytes();
        originalNameBytes = Copy(nameBytes);
        originalRawValue = Copy(rawValue);
        originalLineEnding = lineEnding;
        originalRawFieldBytes = exactFieldBytes;
        originalFolds = ScanFolds(originalRawValue);
        sourceRange = null;
        valuePosition = new EmailSourcePosition(0, 1, 1);
        nameDirty = false;
        valueDirty = false;
        lineEndingDirty = false;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailHeader Clone()
    {
        EmailHeader clone = CreateImplementationWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssuesAndExactOriginalRawFieldBytes(originalNameBytes, originalRawValue, originalLineEnding, sourceRange, valuePosition, valueKind, parseMode, baseIssues, GetOriginalRawFieldBytes());
        clone.nameBytes = Copy(nameBytes);
        clone.rawValue = Copy(rawValue);
        clone.lineEnding = lineEnding;
        clone.folds = ScanFolds(clone.rawValue);
        clone.unfoldedValue = unfoldedValue == null ? null : Copy(unfoldedValue);
        clone.decodedValue = decodedValue;
        clone.structuredValue = structuredValue;
        clone.decodedIssues = new EmailIssueList();
        clone.decodedIssues.AddRange(decodedIssues);
        clone.structuredIssues = new EmailIssueList();
        clone.structuredIssues.AddRange(structuredIssues);
        clone.unfoldedCached = unfoldedCached;
        clone.decodedCached = decodedCached;
        clone.structuredCached = structuredCached;
        clone.nameDirty = nameDirty;
        clone.valueDirty = valueDirty;
        clone.lineEndingDirty = lineEndingDirty;
        clone.compatibilityOptions = compatibilityOptions.Clone();
        return clone;
    }

    internal void SetCompatibilityOptions(EmailCompatibilityOptions value)
    {
        compatibilityOptions = value.Clone();
        decodedValue = null;
        structuredValue = null;
        decodedIssues = new EmailIssueList();
        structuredIssues = new EmailIssueList();
        decodedCached = false;
        structuredCached = false;
    }

    internal void SetMutationGuard(Action? value)
    {
        mutationGuard = value;
    }

    void SetValueBytes(byte[] value)
    {
        EnsureMutationAllowed();
        rawValue = Copy(value);
        folds = ScanFolds(rawValue);
        valueDirty = !Equal(rawValue, originalRawValue);
        InvalidateAllCaches();
    }

    void EnsureMutationAllowed()
    {
        if (mutationGuard != null)
            mutationGuard();
    }

    void EnsureUnfolded()
    {
        if (!unfoldedCached)
        {
            unfoldedValue = Unfold(rawValue);
            unfoldedCached = true;
        }
    }

    void InvalidateStructured()
    {
        structuredValue = null;
        structuredIssues = new EmailIssueList();
        structuredCached = false;
    }

    void InvalidateAllCaches()
    {
        unfoldedValue = null;
        decodedValue = null;
        structuredValue = null;
        decodedIssues = new EmailIssueList();
        structuredIssues = new EmailIssueList();
        unfoldedCached = false;
        decodedCached = false;
        structuredCached = false;
    }

    static byte[] Unfold(byte[]? input)
    {
        List<byte> output = new List<byte>();
        int index = 0;
        while (index < (input ?? throw new InvalidOperationException()).Length)
        {
            int endingLength = 0;
            if (input[index] == 13 && index + 1 < input.Length && input[index + 1] == 10)
            {
                endingLength = 2;
            }
            else if (input[index] == 13 || input[index] == 10)
            {
                endingLength = 1;
            }

            if (endingLength > 0)
            {
                int next = index + endingLength;
                if (next < input.Length && (input[next] == 32 || input[next] == 9))
                {
                    index = next;
                    continue;
                }

                if (output.Count > 0 && output[output.Count - 1] != 32)
                {
                    output.Add(32);
                }

                index = next;
                continue;
            }

            output.Add(input[index]);
            index++;
        }

        return output.ToArray();
    }

    static List<EmailHeaderFold> ScanFolds(byte[] input)
    {
        List<EmailHeaderFold> result = new List<EmailHeaderFold>();
        int index = 0;
        while (index < input.Length)
        {
            EmailLineEnding ending = EmailLineEnding.None;
            int endingLength = 0;
            if (input[index] == 13 && index + 1 < input.Length && input[index + 1] == 10)
            {
                ending = EmailLineEnding.CrLf;
                endingLength = 2;
            }
            else if (input[index] == 13)
            {
                ending = EmailLineEnding.Cr;
                endingLength = 1;
            }
            else if (input[index] == 10)
            {
                ending = EmailLineEnding.Lf;
                endingLength = 1;
            }

            if (endingLength > 0)
            {
                int whitespace = 0;
                int cursor = index + endingLength;
                while (cursor < input.Length && (input[cursor] == 32 || input[cursor] == 9))
                {
                    whitespace++;
                    cursor++;
                }

                result.Add(new EmailHeaderFold(index, ending, whitespace));
                index += endingLength;
            }
            else
            {
                index++;
            }
        }

        return result;
    }

    static byte[] CreateFieldBytes(byte[]? fieldName, byte[]? fieldValue, EmailLineEnding ending)
    {
        byte[] endingBytes = GetLineEndingBytes(ending);
        byte[] result = new byte[(fieldName ?? throw new InvalidOperationException()).Length + 1 + (fieldValue ?? throw new InvalidOperationException()).Length + endingBytes.Length];
        fieldName.CopyTo(result, 0);
        result[fieldName.Length] = 58;
        fieldValue.CopyTo(result, fieldName.Length + 1);
        endingBytes.CopyTo(result, fieldName.Length + 1 + fieldValue.Length);
        return result;
    }

    static byte[] GetLineEndingBytes(EmailLineEnding ending)
    {
        if (ending == EmailLineEnding.CrLf)
        {
            return new byte[]
            {
                13,
                10
            };
        }

        if (ending == EmailLineEnding.Lf)
        {
            return new byte[]
            {
                10
            };
        }

        if (ending == EmailLineEnding.Cr)
        {
            return new byte[]
            {
                13
            };
        }

        return new byte[0];
    }

    static void ValidateMutableName(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("Header names cannot be empty.", "value");
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == ':')
            {
                throw new ArgumentException("Header names must use printable ASCII field-name characters.", "value");
            }

            index++;
        }
    }

    static void ValidateMutableRawValue(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == 0)
            {
                throw new ArgumentException("Header values cannot contain NUL characters.", "value");
            }

            if (value[index] == 13)
            {
                if (index + 2 >= value.Length || value[index + 1] != 10 || value[index + 2] != 32 && value[index + 2] != 9)
                {
                    throw new ArgumentException("Raw header line breaks must be CRLF folds followed by whitespace.", "value");
                }

                index += 2;
            }
            else if (value[index] == 10)
            {
                throw new ArgumentException("Raw header values cannot contain bare line feeds.", "value");
            }

            index++;
        }
    }

    static void ValidateValueKind(EmailHeaderValueKind value)
    {
        if (value < EmailHeaderValueKind.Unknown || value > EmailHeaderValueKind.Trace)
        {
            throw new ArgumentOutOfRangeException("value");
        }
    }

    static void ValidateParseMode(EmailStructuredValueParseMode value)
    {
        if (value < EmailStructuredValueParseMode.Strict || value > EmailStructuredValueParseMode.Tolerant)
        {
            throw new ArgumentOutOfRangeException("value");
        }
    }

    static bool Equal(byte[] left, byte[]? right)
    {
        if (left.Length != (right ?? throw new InvalidOperationException()).Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static byte[] Copy(byte[]? value)
    {
        byte[] copy = new byte[(value ?? throw new InvalidOperationException()).Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
