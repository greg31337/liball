using System;
using System.Collections.Generic;

namespace html;

internal class HtmlMetadataCandidateCollector
{
    internal int maximumCandidates;
    internal int maximumRawCharacters;
    internal List<HtmlMetadataCandidate> candidates =
        new List<HtmlMetadataCandidate>();
    internal long totalCount;
    internal long droppedCount;
    internal int retainedRawCharacters;
    internal bool rawValuesTruncated;
    internal long nextRetentionOrder;

    internal HtmlMetadataCandidateCollector(int maximumCandidatesValue,
        int maximumRawCharactersValue)
    {
        this.Initialize(maximumCandidatesValue, maximumRawCharactersValue);
    }

    internal static HtmlMetadataCandidateCollector Create(HtmlLimits limits)
    {
        return new HtmlMetadataCandidateCollector(GetCandidateLimit(limits),
            GetRawCharacterLimit(limits));
    }

    internal void Initialize(int maximumCandidatesValue,
        int maximumRawCharactersValue)
    {
        if (maximumCandidatesValue < 0)
        {
            throw new ArgumentOutOfRangeException(
                "maximumCandidatesValue");
        }

        if (maximumRawCharactersValue < 0)
        {
            throw new ArgumentOutOfRangeException(
                "maximumRawCharactersValue");
        }

        this.maximumCandidates = maximumCandidatesValue;
        this.maximumRawCharacters = maximumRawCharactersValue;
        int capacity = maximumCandidatesValue < 8
            ? maximumCandidatesValue : 8;
        this.candidates = new List<HtmlMetadataCandidate>(capacity);
        this.totalCount = 0;
        this.droppedCount = 0;
        this.retainedRawCharacters = 0;
        this.rawValuesTruncated = false;
        this.nextRetentionOrder = 0;
    }

    internal bool Add(HtmlMetadataField field, string value, string? rawValue,
        HtmlMetadataSource source, int precedence, int documentOrder)
    {
        HtmlMetadataValidation.ValidateField(field, "field");
        HtmlMetadataValidation.ValidateSource(source, "source");
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (precedence < 0)
        {
            throw new ArgumentOutOfRangeException("precedence");
        }

        if (documentOrder < 0)
        {
            throw new ArgumentOutOfRangeException("documentOrder");
        }

        this.IncrementTotalCount();
        if (this.candidates.Count >= this.maximumCandidates)
        {
            this.IncrementDroppedCount();
            if (rawValue != null && rawValue.Length > 0)
            {
                this.rawValuesTruncated = true;
            }

            return false;
        }

        bool rawTruncated = false;
        string? retainedRawValue = null;
        if (rawValue != null)
        {
            string retained = rawValue;
            int remaining = this.maximumRawCharacters
                - this.retainedRawCharacters;
            if (rawValue.Length > remaining)
            {
                retained = rawValue.Substring(0, remaining);
                rawTruncated = true;
                this.rawValuesTruncated = true;
            }

            retainedRawValue = retained;
            this.retainedRawCharacters = this.retainedRawCharacters
                + retained.Length;
        }

        HtmlMetadataCandidate candidate = new HtmlMetadataCandidate(field,
            value, retainedRawValue, rawTruncated, source, precedence,
            documentOrder, this.nextRetentionOrder);
        this.nextRetentionOrder = this.nextRetentionOrder + 1;
        this.candidates.Add(candidate);
        return true;
    }

    internal HtmlMetadataCandidate[] GetCandidates()
    {
        HtmlMetadataCandidate[] result = this.candidates.ToArray();
        Array.Sort(result, HtmlMetadataCandidateComparer.Compare);
        return result;
    }

    internal long GetTotalCount()
    {
        return this.totalCount;
    }

    internal long GetDroppedCount()
    {
        return this.droppedCount;
    }

    internal int GetRetainedRawCharacterCount()
    {
        return this.retainedRawCharacters;
    }

    internal bool WereRawValuesTruncated()
    {
        return this.rawValuesTruncated;
    }

    internal HtmlMetadata CreateMetadata(HtmlDiagnostic[] diagnostics,
        long diagnosticCount, long droppedDiagnosticCount)
    {
        return new HtmlMetadata(this.GetCandidates(), this.totalCount,
            this.droppedCount, 0, this.rawValuesTruncated, diagnostics,
            diagnosticCount, droppedDiagnosticCount);
    }

    internal static int GetCandidateLimit(HtmlLimits limits)
    {
        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        return limits.GetMaximumJsonProperties();
    }

    internal static int GetRawCharacterLimit(HtmlLimits limits)
    {
        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        return limits.GetMaximumOutputCharacters();
    }

    internal void IncrementTotalCount()
    {
        if (this.totalCount < long.MaxValue)
        {
            this.totalCount = this.totalCount + 1;
        }
    }

    internal void IncrementDroppedCount()
    {
        if (this.droppedCount < long.MaxValue)
        {
            this.droppedCount = this.droppedCount + 1;
        }
    }
}

internal static class HtmlMetadataCandidateComparer
{
    internal static int Compare(HtmlMetadataCandidate left,
        HtmlMetadataCandidate right)
    {
        if (object.ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return -1;
        }

        if (right == null)
        {
            return 1;
        }

        int result = ((int)left.GetField()).CompareTo(
            (int)right.GetField());
        if (result != 0)
        {
            return result;
        }

        result = left.GetPrecedence().CompareTo(right.GetPrecedence());
        if (result != 0)
        {
            return result;
        }

        result = left.GetDocumentOrder().CompareTo(
            right.GetDocumentOrder());
        if (result != 0)
        {
            return result;
        }

        result = ((int)left.GetSource()).CompareTo(
            (int)right.GetSource());
        if (result != 0)
        {
            return result;
        }

        result = string.CompareOrdinal(left.GetValue(), right.GetValue());
        if (result != 0)
        {
            return result;
        }

        return left.GetRetentionOrder().CompareTo(
            right.GetRetentionOrder());
    }
}

internal static class HtmlMetadataValidation
{
    internal static void ValidateField(HtmlMetadataField value,
        string parameterName)
    {
        if ((int)value < (int)HtmlMetadataField.Title
            || (int)value > (int)HtmlMetadataField.ImageUrl)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    internal static void ValidateSource(HtmlMetadataSource value,
        string parameterName)
    {
        if ((int)value < (int)HtmlMetadataSource.Document
            || (int)value > (int)HtmlMetadataSource.Heuristic)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
