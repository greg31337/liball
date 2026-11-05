namespace chart;
/// <summary>Contains the format-specific products of a unified chart export.</summary>
public class ChartExportResult
{
    /// <summary>The format that was produced.</summary>
    public ChartOutputFormat Format;
    /// <summary>The rendered bitmap for raw bitmap output, or <c>null</c> for encoded formats.</summary>
    public RawBitmap? Bitmap;
    /// <summary>The raw or encoded bytes produced by the export.</summary>
    public byte[] Bytes;
    /// <summary>The SVG document text, or an empty string for bitmap formats.</summary>
    public string Text;

    /// <summary>Initializes an empty result.</summary>
    public ChartExportResult()
    {
        Format = ChartOutputFormat.Png;
        Bitmap = null;
        Bytes = new byte[0];
        Text = "";
    }
}
