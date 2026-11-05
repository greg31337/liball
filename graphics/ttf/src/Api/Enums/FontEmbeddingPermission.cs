namespace ttf;

/// <summary>Describes the effective OpenType embedding permission.</summary>
public enum FontEmbeddingPermission
{
    /// <summary>Embedding and permanent installation are permitted.</summary>
    Installable = 0,

    /// <summary>Embedding requires explicit permission from the rights holder.</summary>
    Restricted = 1,

    /// <summary>Temporary embedding for preview and printing is permitted.</summary>
    PreviewAndPrint = 2,

    /// <summary>Temporary embedding in editable documents is permitted.</summary>
    Editable = 3
}
