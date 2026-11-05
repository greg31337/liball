using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Builds a two-to-four-layer AV1 progressive image for item authoring.</summary>
/// <remarks>
/// Layers are ordered from the earliest preview to the final presentation. Source images remain
/// caller-owned and are copied only when the layered image is added to a writer.
/// </remarks>
public class AvifLayeredImage : IAvifLayeredImage
{
    List<AvifLayer>? layers;
    int operatingPointIndex;
    bool progressive;
    AvifResultCode code;

    /// <summary>Creates a progressive layered image using operating point zero.</summary>
    /// <returns>The initialized progressive layered image.</returns>
    internal static AvifLayeredImage Create()
    {
        return new AvifLayeredImage(0, true);
    }

    /// <summary>Creates a layered image with explicit selection signaling.</summary>
    /// <param name="operatingPointIndex">The AV1 operating-point index from zero through 31.</param>
    /// <param name="progressive">Whether every indexed layer may be presented progressively.</param>
    public AvifLayeredImage(int operatingPointIndex, bool progressive)
    {
        Initialize(operatingPointIndex, progressive);
    }

    /// <summary>Creates a layered image with explicit selection signaling.</summary>
    /// <returns>The initialized layered image.</returns>
    internal static AvifLayeredImage Create(
        int operatingPointIndex,
        bool progressive)
    {
        return new AvifLayeredImage(operatingPointIndex, progressive);
    }

    /// <summary>Returns the construction validation result.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Adds one caller-owned image as the next progressive layer.</summary>
    /// <param name="image">The image to retain until writer insertion.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode AddLayer(AvifImage image)
    {
        return AddLayerDescriptor(new AvifLayer(image));
    }

    /// <summary>Adds one validated layer descriptor.</summary>
    /// <param name="layer">The layer to retain until writer insertion.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode AddLayerDescriptor(AvifLayer layer)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        if (layer == null || layer.GetCode() != AvifResultCode.Success)
        {
            return AvifResultCode.InvalidArgument;
        }
        if ((layers ?? throw new InvalidOperationException("Invalid internal state.")).Count == 4)
        {
            return AvifResultCode.LimitExceeded;
        }
        if (layers.Count > 0
            && !LayersAreCompatible(
                layers[layers.Count - 1].GetImage(),
                layer.GetImage()))
        {
            return AvifResultCode.NonConformingData;
        }
        layer.SetLayerId(layers.Count);
        layers.Add(layer);
        return AvifResultCode.Success;
    }

    /// <summary>Returns the number of inserted layers.</summary>
    /// <returns>Zero through four.</returns>
    public int GetLayerCount()
    {
        return (layers ?? throw new InvalidOperationException("Invalid internal state.")).Count;
    }

    /// <summary>Returns one inserted layer descriptor.</summary>
    /// <param name="index">The zero-based layer index.</param>
    /// <returns>The layer, or null for an invalid index.</returns>
    public AvifLayer? GetLayer(int index)
    {
        if (index < 0 || index >= (layers ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            return null;
        }
        return layers[index];
    }

    /// <summary>Returns the selected AV1 operating-point index.</summary>
    /// <returns>Zero through 31.</returns>
    public int GetOperatingPointIndex()
    {
        return operatingPointIndex;
    }

    /// <summary>Reports whether intermediate layers may be presented progressively.</summary>
    /// <returns><c>true</c> for progressive presentation.</returns>
    public bool IsProgressive()
    {
        return progressive;
    }

    void Initialize(int requestedOperatingPoint, bool requestedProgressive)
    {
        layers = new List<AvifLayer>();
        operatingPointIndex = requestedOperatingPoint;
        progressive = requestedProgressive;
        code = operatingPointIndex >= 0 && operatingPointIndex <= 31
            ? AvifResultCode.Success
            : AvifResultCode.InvalidArgument;
    }

    static bool LayersAreCompatible(AvifImage previous, AvifImage next)
    {
        if (previous == null || next == null
            || previous.GetCode() != AvifResultCode.Success
            || next.GetCode() != AvifResultCode.Success
            || next.GetWidth() < previous.GetWidth()
            || next.GetHeight() < previous.GetHeight()
            || next.GetBitDepth() != previous.GetBitDepth()
            || next.GetChromaFormat() != previous.GetChromaFormat()
            || next.GetChromaSamplePosition()
                != previous.GetChromaSamplePosition()
            || next.GetRange() != previous.GetRange()
            || next.GetAlphaMode() != previous.GetAlphaMode())
        {
            return false;
        }
        AvifColorInfo? previousColor = previous.GetColorInfo();
        AvifColorInfo? nextColor = next.GetColorInfo();
        if ((previousColor ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries != (nextColor ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries
            || previousColor.TransferCharacteristics
                != nextColor.TransferCharacteristics
            || previousColor.MatrixCoefficients != nextColor.MatrixCoefficients
            || previousColor.Range != nextColor.Range)
        {
            return false;
        }
        return IccProfilesEqual(
            previous.GetIccProfile(),
            next.GetIccProfile());
    }

    static bool IccProfilesEqual(AvifIccProfile? first, AvifIccProfile? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }
        byte[] firstData = first.GetData();
        byte[] secondData = second.GetData();
        if (firstData.Length != secondData.Length)
        {
            return false;
        }
        int index = 0;
        while (index < firstData.Length)
        {
            if (firstData[index] != secondData[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }
}
