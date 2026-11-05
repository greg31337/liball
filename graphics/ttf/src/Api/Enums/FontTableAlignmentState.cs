namespace ttf;

/// <summary>Describes offset and padding alignment validation for an SFNT table.</summary>
public enum FontTableAlignmentState
{
    /// <summary>Alignment validation was not performed.</summary>
    NotChecked = 0,

    /// <summary>The table offset and zero padding are valid.</summary>
    Valid = 1,

    /// <summary>The table offset is not aligned to four bytes.</summary>
    MisalignedOffset = 2,

    /// <summary>The required alignment padding extends beyond the container.</summary>
    MissingPadding = 3,

    /// <summary>The alignment padding contains a nonzero byte.</summary>
    NonZeroPadding = 4
}
