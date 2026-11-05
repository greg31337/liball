using System;

namespace avif;

/// <summary>
/// Reconstructs the alternate image represented by a tone-map derived image item.
/// </summary>
/// <remarks>
/// The base and gain-map images remain owned by the caller. Automatic rendering uses the
/// alternate preference supplied by the container resolver and gracefully returns the base image
/// when the tone-map derivation uses an unsupported feature.
/// </remarks>
public class AvifToneMap : IAvifToneMap, IAvifDiagnosticSource
{
    AvifResultCode code;
    AvifImage? baseImage;
    AvifGainMap? gainMap;
    bool alternateImagePreferred;
    uint toneMapItemId;
    uint baseItemId;
    uint gainMapItemId;

    /// <summary>Creates a directly selected tone-map derived image.</summary>
    /// <param name="baseImage">The decoded first <c>dimg</c> input.</param>
    /// <param name="gainMap">The decoded second <c>dimg</c> input and <c>tmap</c> metadata.</param>
    /// <returns>The initialized tone-map association.</returns>
    internal static AvifToneMap Create(
        AvifImage baseImage,
        AvifGainMap gainMap)
    {
        return new AvifToneMap(baseImage, gainMap, true);
    }

    /// <summary>Creates a tone-map derived image with its alternate-group preference.</summary>
    /// <param name="baseImage">The decoded first <c>dimg</c> input.</param>
    /// <param name="gainMap">The decoded second <c>dimg</c> input and <c>tmap</c> metadata.</param>
    /// <param name="alternateImagePreferred">
    /// Whether the <c>tmap</c> item precedes the base item in their <c>altr</c> group.
    /// </param>
    public AvifToneMap(
        AvifImage baseImage,
        AvifGainMap gainMap,
        bool alternateImagePreferred)
    {
        Initialize(baseImage, gainMap, alternateImagePreferred);
    }

    /// <summary>Creates a tone-map association with explicit preference.</summary>
    /// <returns>The initialized tone-map association.</returns>
    internal static AvifToneMap Create(
        AvifImage baseImage,
        AvifGainMap gainMap,
        bool alternateImagePreferred)
    {
        return new AvifToneMap(
            baseImage,
            gainMap,
            alternateImagePreferred);
    }

    /// <summary>Creates a tone-map derivation from ordered item relationships.</summary>
    /// <returns>The initialized tone-map association.</returns>
    internal static AvifToneMap Create(
        AvifImage baseImage,
        AvifGainMap gainMap,
        uint toneMapItemId,
        uint[] dependencyItemIds,
        AvifEntityGroup alternateGroup)
    {
        return FromItems(
            baseImage,
            gainMap,
            toneMapItemId,
            dependencyItemIds,
            alternateGroup);
    }

    /// <summary>Creates a tone-map derivation from ordered item relationships.</summary>
    /// <param name="baseImage">The decoded first <c>dimg</c> input.</param>
    /// <param name="gainMap">The decoded second <c>dimg</c> input and <c>tmap</c> metadata.</param>
    /// <param name="toneMapItemId">The nonzero <c>tmap</c> item identifier.</param>
    /// <param name="dependencyItemIds">
    /// Exactly two distinct identifiers in <c>dimg</c> order: base, then gain map.
    /// </param>
    /// <param name="alternateGroup">
    /// The containing <c>altr</c> group, or null when no preference is signaled.
    /// </param>
    /// <returns>The initialized tone-map association.</returns>
    internal static AvifToneMap FromItems(
        AvifImage baseImage,
        AvifGainMap gainMap,
        uint toneMapItemId,
        uint[] dependencyItemIds,
        AvifEntityGroup alternateGroup)
    {
        AvifToneMap result = new AvifToneMap(
            baseImage,
            gainMap,
            true);
        result.InitializeFromItems(
            baseImage,
            gainMap,
            toneMapItemId,
            dependencyItemIds,
            alternateGroup);
        return result;
    }

    /// <summary>Returns the decoded dependency association state.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        AvifResultCode baseCode = (baseImage ?? throw new InvalidOperationException("Invalid internal state.")).GetCode();
        if (baseCode != AvifResultCode.Success)
        {
            return baseCode;
        }
        return (gainMap ?? throw new InvalidOperationException("Invalid internal state.")).GetCode();
    }

    /// <summary>Returns diagnostics for tone-map association and rendering state.</summary>
    /// <returns>The structured rendering diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            GetCode(),
            AvifDiagnosticCategory.Rendering,
            "The tone-map rendering operation did not complete successfully.");
    }

    /// <summary>Returns the caller-owned base image.</summary>
    /// <returns>The first derived-image input.</returns>
    public AvifImage? GetBaseImage()
    {
        return baseImage;
    }

    /// <summary>Returns the caller-owned gain-map association.</summary>
    /// <returns>The second derived-image input and metadata.</returns>
    public AvifGainMap? GetGainMap()
    {
        return gainMap;
    }

    /// <summary>Reports whether the tone-mapped alternate precedes its base alternative.</summary>
    /// <returns><c>true</c> when automatic rendering should prefer reconstruction.</returns>
    public bool IsAlternateImagePreferred()
    {
        return alternateImagePreferred;
    }

    /// <summary>Returns the associated tone-map item identifier.</summary>
    /// <returns>The identifier, or zero for a directly constructed derivation.</returns>
    public uint GetToneMapItemId()
    {
        return toneMapItemId;
    }

    /// <summary>Returns the first <c>dimg</c> item identifier.</summary>
    /// <returns>The base item identifier, or zero when item relationships were not supplied.</returns>
    public uint GetBaseItemId()
    {
        return baseItemId;
    }

    /// <summary>Returns the second <c>dimg</c> item identifier.</summary>
    /// <returns>The gain-map item identifier, or zero when item relationships were not supplied.</returns>
    public uint GetGainMapItemId()
    {
        return gainMapItemId;
    }

    /// <summary>Renders the selected base or reconstructed alternate image.</summary>
    /// <param name="options">The tone-map policy, display headroom, color, and limits.</param>
    /// <returns>An owning linear BT.709 half-float RGBA buffer.</returns>
    public AvifPixelBuffer Render(AvifRenderOptions options)
    {
        return ToneMapRenderer.Render(this, options);
    }

    void Initialize(
        AvifImage requestedBaseImage,
        AvifGainMap requestedGainMap,
        bool requestedAlternateImagePreferred)
    {
        baseImage = requestedBaseImage;
        gainMap = requestedGainMap;
        alternateImagePreferred = requestedAlternateImagePreferred;
        toneMapItemId = 0;
        baseItemId = 0;
        gainMapItemId = 0;
        if (requestedBaseImage == null || requestedGainMap == null)
        {
            code = AvifResultCode.InvalidArgument;
            return;
        }
        code = AvifResultCode.Success;
    }

    void InitializeFromItems(
        AvifImage requestedBaseImage,
        AvifGainMap requestedGainMap,
        uint requestedToneMapItemId,
        uint[] dependencyItemIds,
        AvifEntityGroup alternateGroup)
    {
        Initialize(requestedBaseImage, requestedGainMap, false);
        if (code != AvifResultCode.Success)
        {
            return;
        }
        if (requestedToneMapItemId == 0 || dependencyItemIds == null
            || dependencyItemIds.Length != 2
            || dependencyItemIds[0] == 0 || dependencyItemIds[1] == 0
            || dependencyItemIds[0] == dependencyItemIds[1]
            || requestedToneMapItemId == dependencyItemIds[0]
            || requestedToneMapItemId == dependencyItemIds[1])
        {
            code = AvifResultCode.InvalidItem;
            return;
        }

        toneMapItemId = requestedToneMapItemId;
        baseItemId = dependencyItemIds[0];
        gainMapItemId = dependencyItemIds[1];
        if (alternateGroup == null)
        {
            return;
        }
        if (alternateGroup.GetKind() != AvifEntityGroupKind.Alternate)
        {
            code = AvifResultCode.InvalidArgument;
            return;
        }

        int toneMapPosition = -1;
        int basePosition = -1;
        int index = 0;
        while (index < alternateGroup.GetEntityCount())
        {
            uint entityId = alternateGroup.GetEntityId(index);
            if (entityId == toneMapItemId)
            {
                toneMapPosition = index;
            }
            else if (entityId == baseItemId)
            {
                basePosition = index;
            }
            index++;
        }
        if (toneMapPosition < 0 || basePosition < 0)
        {
            code = AvifResultCode.NonConformingData;
            return;
        }
        alternateImagePreferred = toneMapPosition < basePosition;
    }

    internal AvifResultCode GetAssociationCode()
    {
        return code;
    }
}
