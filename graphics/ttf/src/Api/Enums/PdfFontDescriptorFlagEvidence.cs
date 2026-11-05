namespace ttf;

/// <summary>Identifies the metadata used for one descriptor-flag inference.</summary>
public enum PdfFontDescriptorFlagEvidence
{
    /// <summary>The relevant source metadata was unavailable.</summary>
    Unavailable = 0,

    /// <summary>The <c>post</c> fixed-pitch field supplied the evidence.</summary>
    PostFixedPitch = 1,

    /// <summary>The OS/2 IBM family class supplied the evidence.</summary>
    Os2FamilyClass = 2,

    /// <summary>The OS/2 PANOSE classification supplied the evidence.</summary>
    Os2Panose = 3,

    /// <summary>A Windows symbol cmap supplied the evidence.</summary>
    CmapSymbolEncoding = 4,

    /// <summary>Latin uppercase and lowercase cmap coverage supplied the evidence.</summary>
    CmapLatinCoverage = 5,

    /// <summary>OS/2 selection flags supplied the evidence.</summary>
    Os2Selection = 6,

    /// <summary>The <c>post</c> italic angle supplied the evidence.</summary>
    PostItalicAngle = 7,

    /// <summary>The OS/2 weight class supplied the evidence.</summary>
    Os2WeightClass = 8,

    /// <summary>More than one metadata field contributed to the decision.</summary>
    Multiple = 9,

    /// <summary>The bit was conservatively cleared without reliable evidence.</summary>
    ConservativeDefault = 10
}
