using System;
namespace token;

/// <summary>
/// Validates integrity-protected COSE critical header parameters.
/// </summary>
internal static class CoseCriticalHeaderValidator
{
    static readonly int[] definedParameters = new int[]
    {
        CoseHeaderLabels.Algorithm,
        CoseHeaderLabels.Critical,
        CoseHeaderLabels.ContentType,
        CoseHeaderLabels.KeyId,
        CoseHeaderLabels.InitializationVector,
        CoseHeaderLabels.PartialInitializationVector
    };

    /// <summary>Validates critical labels using only the common parameters processed by this library.</summary>
    /// <param name="header">The COSE header buckets to validate.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>header</c> is null.</exception>
    internal static TokenResult Validate(CoseHeader header)
    {
        return Validate(header, Array.Empty<CborItem>());
    }

    /// <summary>Validates critical labels using common and caller-understood parameters.</summary>
    /// <param name="header">The COSE header buckets to validate.</param>
    /// <param name="understoodLabels">Additional integer or text labels the caller understands and processes.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult Validate(
        CoseHeader header,
        CborItem[] understoodLabels)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (understoodLabels == null)
        {
            throw new ArgumentNullException("understoodLabels");
        }

        TokenResult structure = ValidateStructure(header);
        if (!structure.Success)
        {
            return structure;
        }

        TokenResult understood = ValidateUnderstoodLabels(understoodLabels);
        if (!understood.Success)
        {
            return understood;
        }

        CborItem criticalLabel = IntegerLabel(CoseHeaderLabels.Critical);
        CborItem? unprotectedCritical = Find(
            header.UnprotectedParameters,
            criticalLabel);
        if (unprotectedCritical != null)
        {
            return Failure(
                "cose_critical_header_not_protected",
                "The COSE crit parameter must occur only in the protected header.",
                -1);
        }

        CborItem? critical = Find(header.ProtectedParameters, criticalLabel);
        if (critical == null)
        {
            return Success();
        }

        if (critical.Kind != TokenCborValueKind.Array
            || critical.ArrayValue == null
            || critical.ArrayValue.Length == 0)
        {
            return Failure(
                "invalid_cose_critical_header",
                "The COSE crit parameter must be a nonempty array of labels.",
                -1);
        }

        int index = 0;
        while (index < critical.ArrayValue.Length)
        {
            CborItem label = critical.ArrayValue[index];
            if (!IsLabel(label))
            {
                return Failure(
                    "invalid_cose_critical_header",
                    "The COSE crit parameter must contain only integer or text labels.",
                    index);
            }

            int previous = 0;
            while (previous < index)
            {
                if (CborMapKeyComparer.AreEqual(
                    critical.ArrayValue[previous],
                    label))
                {
                    return Failure(
                        "duplicate_cose_critical_header",
                        "The COSE crit parameter contains a duplicate label.",
                        index);
                }

                previous += 1;
            }

            if (Find(header.ProtectedParameters, label) == null)
            {
                return Failure(
                    "missing_cose_critical_header",
                    "A parameter listed by crit is absent from the protected header.",
                    index);
            }

            if (!IsDefined(label) && !Contains(understoodLabels, label))
            {
                return Failure(
                    "unknown_cose_critical_header",
                    "A critical COSE header parameter is not understood and supported.",
                    index);
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult ValidateStructure(CoseHeader header)
    {
        if (header.ProtectedParameters == null
            || header.UnprotectedParameters == null)
        {
            return Failure(
                "invalid_cose_header",
                "A COSE header parameter array is missing.",
                -1);
        }

        TokenResult protectedResult = ValidateBucket(
            header.ProtectedParameters,
            -1);
        if (!protectedResult.Success)
        {
            return protectedResult;
        }

        TokenResult unprotectedResult = ValidateBucket(
            header.UnprotectedParameters,
            -1);
        if (!unprotectedResult.Success)
        {
            return unprotectedResult;
        }

        int protectedIndex = 0;
        while (protectedIndex < header.ProtectedParameters.Length)
        {
            CborItem? label = header.ProtectedParameters[protectedIndex].Key;
            if (Find(header.UnprotectedParameters, label) != null)
            {
                return Failure(
                    "duplicate_cose_header_label",
                    "A COSE header label occurs in both parameter buckets.",
                    protectedIndex);
            }

            protectedIndex += 1;
        }

        return Success();
    }

    static TokenResult ValidateBucket(
        CborMapEntry[] parameters,
        int headerIndex)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            CborMapEntry entry = parameters[index];
            if (entry == null || entry.Key == null || entry.Value == null)
            {
                return Failure(
                    "invalid_cose_header",
                    "A COSE header parameter is incomplete.",
                    headerIndex);
            }

            if (!IsLabel(entry.Key))
            {
                return Failure(
                    "invalid_cose_header_label",
                    "A COSE header label must be an integer or text string.",
                    index);
            }

            int previous = 0;
            while (previous < index)
            {
                if (CborMapKeyComparer.AreEqual(
                    (parameters[previous].Key ?? throw new InvalidOperationException("A required value is null.")),
                    entry.Key))
                {
                    return Failure(
                        "duplicate_cose_header_label",
                        "A COSE header bucket contains a duplicate label.",
                        index);
                }

                previous += 1;
            }

            index += 1;
        }

        return Success();
    }

    static TokenResult ValidateUnderstoodLabels(CborItem[] labels)
    {
        int index = 0;
        while (index < labels.Length)
        {
            if (!IsLabel(labels[index]))
            {
                return Failure(
                    "invalid_cose_understood_header_label",
                    "An understood COSE header label must be an integer or text string.",
                    index);
            }

            index += 1;
        }

        return Success();
    }

    static bool IsDefined(CborItem label)
    {
        if (label.Kind != TokenCborValueKind.UnsignedInteger
            || label.IntegerArgument > int.MaxValue)
        {
            return false;
        }

        int value = (int)label.IntegerArgument;
        int index = 0;
        while (index < definedParameters.Length)
        {
            if (definedParameters[index] == value)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static bool Contains(CborItem[] labels, CborItem target)
    {
        int index = 0;
        while (index < labels.Length)
        {
            if (CborMapKeyComparer.AreEqual(labels[index], target))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static CborItem? Find(CborMapEntry[]? parameters, CborItem? label)
    {
        int index = 0;
        while (index < (parameters ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            CborMapEntry entry = parameters[index];
            if (CborMapKeyComparer.AreEqual((entry.Key ?? throw new InvalidOperationException("A required value is null.")), (label ?? throw new InvalidOperationException("A required value is null."))))
            {
                return entry.Value;
            }

            index += 1;
        }

        return null;
    }

    static bool IsLabel(CborItem item)
    {
        return item != null
            && (item.Kind == TokenCborValueKind.UnsignedInteger
                || item.Kind == TokenCborValueKind.NegativeInteger
                || item.Kind == TokenCborValueKind.TextString
                    && item.Text != null);
    }

    static CborItem IntegerLabel(int value)
    {
        CborItem result = new CborItem();
        if (value >= 0)
        {
            result.Kind = TokenCborValueKind.UnsignedInteger;
            result.IntegerArgument = (ulong)value;
        }
        else
        {
            result.Kind = TokenCborValueKind.NegativeInteger;
            result.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return result;
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult Failure(
        string code,
        string message,
        int position)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, position);
        return result;
    }
}
