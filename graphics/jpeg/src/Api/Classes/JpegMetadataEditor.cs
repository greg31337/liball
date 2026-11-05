namespace jpeg;

/// <summary>Default service for editing JPEG metadata without changing encoded scan data.</summary>
public class JpegMetadataEditor : IJpegMetadataEditor
{
    /// <summary>Creates an independent JPEG metadata editor.</summary>
    public JpegMetadataEditor()
    {
    }

    /// <summary>Replaces the complete metadata-marker sequence using the selected policy.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="markers">The complete replacement metadata-marker sequence.</param>
    /// <param name="policy">The policy applied to the replacement sequence.</param>
    /// <returns>A newly allocated JPEG stream whose non-metadata bytes are unchanged.</returns>
    public byte[] EditMarkers(byte[] data, System.Collections.Generic.List<JpegRawMarker> markers, JpegMetadataPolicy policy)
    {
        return new JpegMetadataContainerEditor().EditMarkers(data, markers, policy);
    }

    /// <summary>Replaces the complete metadata-marker sequence in a read-only span.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="markers">The complete replacement metadata-marker sequence.</param>
    /// <param name="policy">The policy applied to the replacement sequence.</param>
    /// <returns>A newly allocated JPEG stream whose non-metadata bytes are unchanged.</returns>
    public byte[] EditMarkersSpan(System.ReadOnlySpan<byte> data, System.Collections.Generic.List<JpegRawMarker> markers, JpegMetadataPolicy policy)
    {
        return new JpegMetadataContainerEditor().EditMarkers(data.ToArray(), markers, policy);
    }

    /// <summary>Replaces the complete metadata-marker sequence while copying between streams.</summary>
    /// <param name="input">A readable source stream positioned at JPEG data.</param>
    /// <param name="output">A writable destination stream.</param>
    /// <param name="markers">The complete replacement metadata-marker sequence.</param>
    /// <param name="policy">The policy applied to the replacement sequence.</param>
    public void EditMarkersStream(System.IO.Stream input, System.IO.Stream output, System.Collections.Generic.List<JpegRawMarker> markers, JpegMetadataPolicy policy)
    {
        byte[] data = new JpegMetadataContainerEditor().ReadStreamBytes(input);
        byte[] result = new JpegMetadataContainerEditor().EditMarkers(data, markers, policy);
        new JpegMetadataContainerEditor().WriteStreamBytes(output, result);
    }

    /// <summary>Replaces the complete metadata-marker sequence while copying between files.</summary>
    /// <param name="inputPath">The source JPEG file path.</param>
    /// <param name="outputPath">The destination JPEG file path.</param>
    /// <param name="markers">The complete replacement metadata-marker sequence.</param>
    /// <param name="policy">The policy applied to the replacement sequence.</param>
    public void EditMarkersFile(string inputPath, string outputPath, System.Collections.Generic.List<JpegRawMarker> markers, JpegMetadataPolicy policy)
    {
        Jpeg.ValidateFilePath(inputPath);
        new JpegMetadataContainerEditor().ValidateOutputPath(outputPath);
        JpegIo io = new JpegIo();
        byte[] result = new JpegMetadataContainerEditor().EditMarkers(io.ReadFile(inputPath, default(System.Threading.CancellationToken)), markers, policy);
        io.WriteFile(outputPath, result, default(System.Threading.CancellationToken));
    }

    /// <summary>Filters source metadata markers using a metadata policy.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="policy">The policy applied to existing metadata markers.</param>
    /// <returns>A newly allocated JPEG stream whose retained markers remain in their original locations.</returns>
    public byte[] ApplyMetadataPolicy(byte[] data, JpegMetadataPolicy policy)
    {
        return new JpegMetadataContainerEditor().ApplyPolicy(data, policy);
    }

    /// <summary>Filters metadata markers in a read-only span using a metadata policy.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="policy">The policy applied to existing metadata markers.</param>
    /// <returns>A newly allocated JPEG stream whose retained markers remain in their original locations.</returns>
    public byte[] ApplyMetadataPolicySpan(System.ReadOnlySpan<byte> data, JpegMetadataPolicy policy)
    {
        return new JpegMetadataContainerEditor().ApplyPolicy(data.ToArray(), policy);
    }

    /// <summary>Filters source metadata while copying between streams.</summary>
    /// <param name="input">A readable source stream positioned at JPEG data.</param>
    /// <param name="output">A writable destination stream.</param>
    /// <param name="policy">The policy applied to existing metadata markers.</param>
    public void ApplyMetadataPolicyStream(System.IO.Stream input, System.IO.Stream output, JpegMetadataPolicy policy)
    {
        byte[] data = new JpegMetadataContainerEditor().ReadStreamBytes(input);
        byte[] result = new JpegMetadataContainerEditor().ApplyPolicy(data, policy);
        new JpegMetadataContainerEditor().WriteStreamBytes(output, result);
    }

    /// <summary>Filters source metadata while copying between files.</summary>
    /// <param name="inputPath">The source JPEG file path.</param>
    /// <param name="outputPath">The destination JPEG file path.</param>
    /// <param name="policy">The policy applied to existing metadata markers.</param>
    public void ApplyMetadataPolicyFile(string inputPath, string outputPath, JpegMetadataPolicy policy)
    {
        Jpeg.ValidateFilePath(inputPath);
        new JpegMetadataContainerEditor().ValidateOutputPath(outputPath);
        JpegIo io = new JpegIo();
        byte[] result = new JpegMetadataContainerEditor().ApplyPolicy(io.ReadFile(inputPath, default(System.Threading.CancellationToken)), policy);
        io.WriteFile(outputPath, result, default(System.Threading.CancellationToken));
    }

    /// <summary>Removes all APPn, COM, and unknown metadata markers.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated JPEG stream containing the original non-metadata bytes.</returns>
    public byte[] StripMetadata(byte[] data)
    {
        return new JpegMetadataContainerEditor().StripMetadata(data);
    }

    /// <summary>Removes all metadata markers from a read-only span.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated JPEG stream containing the original non-metadata bytes.</returns>
    public byte[] StripMetadataSpan(System.ReadOnlySpan<byte> data)
    {
        return new JpegMetadataContainerEditor().StripMetadata(data.ToArray());
    }

    /// <summary>Removes all metadata while copying between streams.</summary>
    /// <param name="input">A readable source stream positioned at JPEG data.</param>
    /// <param name="output">A writable destination stream.</param>
    public void StripMetadataStream(System.IO.Stream input, System.IO.Stream output)
    {
        byte[] data = new JpegMetadataContainerEditor().ReadStreamBytes(input);
        byte[] result = new JpegMetadataContainerEditor().StripMetadata(data);
        new JpegMetadataContainerEditor().WriteStreamBytes(output, result);
    }

    /// <summary>Removes all metadata while copying between files.</summary>
    /// <param name="inputPath">The source JPEG file path.</param>
    /// <param name="outputPath">The destination JPEG file path.</param>
    public void StripMetadataFile(string inputPath, string outputPath)
    {
        Jpeg.ValidateFilePath(inputPath);
        new JpegMetadataContainerEditor().ValidateOutputPath(outputPath);
        JpegIo io = new JpegIo();
        byte[] result = new JpegMetadataContainerEditor().StripMetadata(io.ReadFile(inputPath, default(System.Threading.CancellationToken)));
        io.WriteFile(outputPath, result, default(System.Threading.CancellationToken));
    }

    /// <summary>Replaces all COM marker text.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="comments">The replacement comments.</param>
    /// <returns>A JPEG byte array containing the replacement comments.</returns>
    public byte[] SetComments(byte[] data, System.Collections.Generic.List<string> comments)
    {
        return Jpeg.SetComments(data, comments);
    }

    /// <summary>Replaces the ICC profile.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="profile">The raw ICC profile bytes.</param>
    /// <returns>A JPEG byte array containing the replacement profile.</returns>
    public byte[] SetIccProfile(byte[] data, byte[] profile)
    {
        return Jpeg.SetIccProfile(data, profile);
    }

    /// <summary>Replaces the Exif APP1 payload.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="exifPayload">The raw Exif payload.</param>
    /// <returns>A JPEG byte array containing the replacement Exif data.</returns>
    public byte[] SetExif(byte[] data, byte[] exifPayload)
    {
        return Jpeg.SetExif(data, exifPayload);
    }

    /// <summary>Applies selected Exif convenience-field replacements while preserving encoded image data.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="metadata">Parsed Exif metadata whose selected fields contain replacement values.</param>
    /// <returns>A JPEG byte array containing the replaced Exif fields.</returns>
    public byte[] ReplaceExifFields(byte[] data, JpegExifMetadata metadata)
    {
        return Jpeg.ReplaceExifFields(data, metadata);
    }

    /// <summary>Replaces the standard XMP APP1 payload.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="xmpPayload">The raw XMP payload.</param>
    /// <returns>A JPEG byte array containing the replacement XMP data.</returns>
    public byte[] SetXmp(byte[] data, byte[] xmpPayload)
    {
        return Jpeg.SetXmp(data, xmpPayload);
    }

    /// <summary>Replaces the standard XMP packet using its public packet model.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="metadata">The standard XMP packet whose packet bytes are written.</param>
    /// <returns>A JPEG byte array containing the replacement standard XMP packet.</returns>
    public byte[] ReplaceXmp(byte[] data, JpegXmpMetadata metadata)
    {
        JpegMetadataUpdater updater = new JpegMetadataUpdater();
        return updater.ReplaceXmp(data, metadata);
    }

    /// <summary>Removes standard XMP while preserving Extended XMP and unrelated metadata.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A JPEG byte array without standard XMP.</returns>
    public byte[] RemoveXmp(byte[] data)
    {
        JpegMetadataUpdater updater = new JpegMetadataUpdater();
        return updater.RemoveXmp(data);
    }

    /// <summary>Replaces Extended XMP using its public packet model.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="metadata">The Extended XMP GUID and complete packet bytes to write.</param>
    /// <returns>A JPEG byte array containing the replacement Extended XMP chunks.</returns>
    public byte[] ReplaceExtendedXmp(byte[] data, JpegExtendedXmpMetadata metadata)
    {
        JpegMetadataUpdater updater = new JpegMetadataUpdater();
        return updater.ReplaceExtendedXmp(data, metadata);
    }

    /// <summary>Removes Extended XMP while preserving standard XMP and unrelated metadata.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A JPEG byte array without Extended XMP.</returns>
    public byte[] RemoveExtendedXmp(byte[] data)
    {
        JpegMetadataUpdater updater = new JpegMetadataUpdater();
        return updater.RemoveExtendedXmp(data);
    }
}
