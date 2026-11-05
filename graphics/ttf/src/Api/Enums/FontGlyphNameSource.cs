namespace ttf;

/// <summary>Describes the provenance of a public glyph name.</summary>
public enum FontGlyphNameSource
{
    /// <summary>No <c>post</c> table exists, so a stable <c>gidNNNN</c> name was created.</summary>
    SyntheticNoPostTable = 0,

    /// <summary>A standard Macintosh glyph name was supplied by <c>post</c>.</summary>
    PostStandard = 1,

    /// <summary>A valid custom Pascal name was supplied by <c>post</c> version 2.</summary>
    PostCustom = 2,

    /// <summary>A name was derived from an Apple <c>post</c> version 4 character code.</summary>
    PostVersion4CharacterCode = 3,

    /// <summary><c>post</c> version 3 intentionally omitted glyph names.</summary>
    SyntheticPostVersion3 = 4,

    /// <summary>The supplied <c>post</c> name index was invalid.</summary>
    SyntheticInvalidPostNameIndex = 5,

    /// <summary>The supplied custom <c>post</c> name was invalid.</summary>
    SyntheticInvalidPostName = 6,

    /// <summary><c>post</c> version 4 supplied its missing-name marker.</summary>
    SyntheticMissingPostVersion4Name = 7,

    /// <summary>A present <c>post</c> table uses an unsupported version.</summary>
    SyntheticUnsupportedPostTable = 8
}
