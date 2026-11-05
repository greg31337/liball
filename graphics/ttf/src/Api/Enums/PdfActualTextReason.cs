namespace ttf;

/// <summary>Explains why content should carry an <c>ActualText</c> value.</summary>
public enum PdfActualTextReason
{
    /// <summary>One character code was observed with multiple source sequences.</summary>
    MultipleSequencesForCode = 1,

    /// <summary>One source sequence was observed for multiple character codes.</summary>
    SequenceSharedByCodes = 2,

    /// <summary>Both code and sequence ambiguities apply.</summary>
    MultipleSequencesForCodeAndSequenceSharedByCodes = 3
}
