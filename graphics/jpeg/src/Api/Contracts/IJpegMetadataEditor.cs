namespace jpeg;

/// <summary>Edits, filters, or removes JPEG metadata while preserving encoded scan data.</summary>
public interface IJpegMetadataEditor
{
    /// <summary>Replaces the complete APPn, COM, and unknown-marker sequence using the selected metadata policy.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="markers">The complete replacement metadata-marker sequence.</param>
    /// <param name="policy">The policy applied to the replacement sequence.</param>
    /// <returns>A newly allocated JPEG stream whose non-metadata bytes are unchanged.</returns>
    byte[] EditMarkers(byte[] data, System.Collections.Generic.List<JpegRawMarker> markers, JpegMetadataPolicy policy);

    /// <summary>Replaces the complete metadata-marker sequence in a read-only span.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="markers">The complete replacement metadata-marker sequence.</param>
    /// <param name="policy">The policy applied to the replacement sequence.</param>
    /// <returns>A newly allocated JPEG stream whose non-metadata bytes are unchanged.</returns>
    byte[] EditMarkersSpan(System.ReadOnlySpan<byte> data, System.Collections.Generic.List<JpegRawMarker> markers, JpegMetadataPolicy policy);

    /// <summary>Replaces the complete metadata-marker sequence while copying between streams.</summary>
    /// <param name="input">A readable source stream positioned at JPEG data.</param>
    /// <param name="output">A writable destination stream.</param>
    /// <param name="markers">The complete replacement metadata-marker sequence.</param>
    /// <param name="policy">The policy applied to the replacement sequence.</param>
    void EditMarkersStream(System.IO.Stream input, System.IO.Stream output, System.Collections.Generic.List<JpegRawMarker> markers, JpegMetadataPolicy policy);

    /// <summary>Replaces the complete metadata-marker sequence while copying between files.</summary>
    /// <param name="inputPath">The source JPEG file path.</param>
    /// <param name="outputPath">The destination JPEG file path.</param>
    /// <param name="markers">The complete replacement metadata-marker sequence.</param>
    /// <param name="policy">The policy applied to the replacement sequence.</param>
    void EditMarkersFile(string inputPath, string outputPath, System.Collections.Generic.List<JpegRawMarker> markers, JpegMetadataPolicy policy);

    /// <summary>Filters source metadata markers using a metadata policy.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="policy">The policy applied to existing metadata markers.</param>
    /// <returns>A newly allocated JPEG stream whose retained markers remain in their original locations.</returns>
    byte[] ApplyMetadataPolicy(byte[] data, JpegMetadataPolicy policy);

    /// <summary>Filters metadata markers in a read-only span using a metadata policy.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="policy">The policy applied to existing metadata markers.</param>
    /// <returns>A newly allocated JPEG stream whose retained markers remain in their original locations.</returns>
    byte[] ApplyMetadataPolicySpan(System.ReadOnlySpan<byte> data, JpegMetadataPolicy policy);

    /// <summary>Filters source metadata while copying between streams.</summary>
    /// <param name="input">A readable source stream positioned at JPEG data.</param>
    /// <param name="output">A writable destination stream.</param>
    /// <param name="policy">The policy applied to existing metadata markers.</param>
    void ApplyMetadataPolicyStream(System.IO.Stream input, System.IO.Stream output, JpegMetadataPolicy policy);

    /// <summary>Filters source metadata while copying between files.</summary>
    /// <param name="inputPath">The source JPEG file path.</param>
    /// <param name="outputPath">The destination JPEG file path.</param>
    /// <param name="policy">The policy applied to existing metadata markers.</param>
    void ApplyMetadataPolicyFile(string inputPath, string outputPath, JpegMetadataPolicy policy);

    /// <summary>Removes all APPn, COM, and unknown metadata markers.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated JPEG stream containing the original non-metadata bytes.</returns>
    byte[] StripMetadata(byte[] data);

    /// <summary>Removes all metadata markers from a read-only span.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated JPEG stream containing the original non-metadata bytes.</returns>
    byte[] StripMetadataSpan(System.ReadOnlySpan<byte> data);

    /// <summary>Removes all metadata while copying between streams.</summary>
    /// <param name="input">A readable source stream positioned at JPEG data.</param>
    /// <param name="output">A writable destination stream.</param>
    void StripMetadataStream(System.IO.Stream input, System.IO.Stream output);

    /// <summary>Removes all metadata while copying between files.</summary>
    /// <param name="inputPath">The source JPEG file path.</param>
    /// <param name="outputPath">The destination JPEG file path.</param>
    void StripMetadataFile(string inputPath, string outputPath);

    /// <summary>Replaces all COM marker text.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="comments">The replacement comments, in marker order.</param>
    /// <returns>A newly allocated JPEG stream containing the replacement comments.</returns>
    byte[] SetComments(byte[] data, System.Collections.Generic.List<string> comments);

    /// <summary>Replaces the ICC profile.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="profile">The raw ICC profile bytes.</param>
    /// <returns>A newly allocated JPEG stream containing the replacement profile.</returns>
    byte[] SetIccProfile(byte[] data, byte[] profile);

    /// <summary>Replaces the Exif APP1 payload.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="exifPayload">The raw Exif payload, including its Exif identifier.</param>
    /// <returns>A newly allocated JPEG stream containing the replacement Exif data.</returns>
    byte[] SetExif(byte[] data, byte[] exifPayload);

    /// <summary>Applies selected Exif convenience-field replacements while preserving encoded image data.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="metadata">Parsed Exif metadata whose selected fields contain replacement values.</param>
    /// <returns>A newly allocated JPEG stream containing the replaced Exif fields.</returns>
    byte[] ReplaceExifFields(byte[] data, JpegExifMetadata metadata);

    /// <summary>Replaces the standard XMP APP1 payload.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="xmpPayload">The raw XMP payload, including its namespace identifier.</param>
    /// <returns>A newly allocated JPEG stream containing the replacement XMP data.</returns>
    byte[] SetXmp(byte[] data, byte[] xmpPayload);

    /// <summary>Replaces the standard XMP packet using its public packet model.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="metadata">The standard XMP packet whose <c>PacketData</c> bytes are written.</param>
    /// <returns>A newly allocated JPEG stream containing the replacement standard XMP packet.</returns>
    byte[] ReplaceXmp(byte[] data, JpegXmpMetadata metadata);

    /// <summary>Removes standard XMP while preserving Extended XMP and unrelated metadata.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated JPEG stream without standard XMP.</returns>
    byte[] RemoveXmp(byte[] data);

    /// <summary>Replaces Extended XMP using its public packet model.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <param name="metadata">The Extended XMP GUID and complete <c>PacketData</c> bytes to split into chunks.</param>
    /// <returns>A newly allocated JPEG stream containing the replacement Extended XMP chunks.</returns>
    byte[] ReplaceExtendedXmp(byte[] data, JpegExtendedXmpMetadata metadata);

    /// <summary>Removes Extended XMP while preserving standard XMP and unrelated metadata.</summary>
    /// <param name="data">The complete source JPEG data.</param>
    /// <returns>A newly allocated JPEG stream without Extended XMP.</returns>
    byte[] RemoveExtendedXmp(byte[] data);
}
