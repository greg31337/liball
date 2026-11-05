namespace ttf;

/// <summary>Describes a policy-neutral action consistent with embedding flags.</summary>
public enum FontEmbeddingAction
{
    /// <summary>A subset or complete outline font may be embedded.</summary>
    EmbedSubsetOrFullFont = 0,

    /// <summary>The complete font must be embedded.</summary>
    EmbedFullFont = 1,

    /// <summary>Only an embedded bitmap subset may be used.</summary>
    EmbedBitmapDataOnly = 2,

    /// <summary>Only the complete embedded bitmap data may be used.</summary>
    EmbedCompleteBitmapDataOnly = 3,

    /// <summary>Do not embed without explicit rights-holder permission.</summary>
    DoNotEmbedWithoutExplicitPermission = 4
}
