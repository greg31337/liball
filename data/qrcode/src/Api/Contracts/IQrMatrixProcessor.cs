namespace qrcode;

/// <summary>Builds, extracts, transforms, classifies, and validates QR module matrices.</summary>
public interface IQrMatrixProcessor
{
    /// <summary>Builds and classifies a symbol matrix from placement-order codewords.</summary>
    /// <param name="symbol">The symbol metadata and codewords to build; the selected mask and matrix are updated.</param>
    /// <param name="quietZoneModules">The quiet-zone width, or -1 for the profile default.</param>
    /// <returns>The newly allocated rendered module matrix.</returns>
    QrMatrix BuildMatrix(QrSymbol symbol, int quietZoneModules);

    /// <summary>Extracts placement-order codewords from a completed module matrix.</summary>
    /// <param name="matrix">The rendered or encoding-region matrix.</param>
    /// <param name="symbolKind">The QR profile represented by the matrix.</param>
    /// <param name="version">The numbered version or rMQR version-table index.</param>
    /// <param name="errorCorrectionLevel">The selected error-correction level.</param>
    /// <param name="maskPattern">The explicit mask present in the matrix.</param>
    /// <returns>The extracted interleaved codewords in placement order.</returns>
    byte[] ExtractCodewords(
        QrMatrix matrix,
        QrSymbolKind symbolKind,
        int version,
        QrErrorCorrectionLevel errorCorrectionLevel,
        QrMaskPattern maskPattern);

    /// <summary>Applies a deterministic crop, orientation, quiet-zone, polarity, and scaling pipeline.</summary>
    /// <param name="matrix">The source matrix.</param>
    /// <param name="symbolKind">The profile used when replacing the quiet zone.</param>
    /// <param name="quarterTurnsClockwise">The clockwise quarter-turn count from zero through three.</param>
    /// <param name="mirror">Whether to transpose the oriented matrix across its diagonal.</param>
    /// <param name="invert">Whether to exchange dark and light modules.</param>
    /// <param name="cropLeft">The number of columns removed from the left edge.</param>
    /// <param name="cropTop">The number of rows removed from the top edge.</param>
    /// <param name="cropRight">The number of columns removed from the right edge.</param>
    /// <param name="cropBottom">The number of rows removed from the bottom edge.</param>
    /// <param name="replaceQuietZone">Whether to replace retained quiet-zone margins.</param>
    /// <param name="quietZoneModules">The replacement width, or -1 for the profile default.</param>
    /// <param name="scale">The positive integer output scale.</param>
    /// <returns>A newly allocated transformed matrix.</returns>
    QrMatrix TransformMatrix(
        QrMatrix matrix,
        QrSymbolKind symbolKind,
        int quarterTurnsClockwise,
        bool mirror,
        bool invert,
        int cropLeft,
        int cropTop,
        int cropRight,
        int cropBottom,
        bool replaceQuietZone,
        int quietZoneModules,
        int scale);

    /// <summary>Classifies every rendered matrix cell by its structural or payload role.</summary>
    /// <param name="matrix">The matrix to classify; its role array is replaced.</param>
    /// <param name="symbolKind">The QR profile represented by the matrix.</param>
    /// <param name="version">The numbered version or rMQR version-table index.</param>
    /// <param name="errorCorrectionLevel">The selected error-correction level.</param>
    /// <returns>The newly allocated row-major role map.</returns>
    QrModuleRole[] ClassifyModules(
        QrMatrix matrix,
        QrSymbolKind symbolKind,
        int version,
        QrErrorCorrectionLevel errorCorrectionLevel);

    /// <summary>Validates matrix geometry, metadata, placement, padding, and recoverability.</summary>
    /// <param name="symbol">The symbol and retained construction metadata to validate.</param>
    /// <returns>The detailed logical validation result, also attached to the symbol.</returns>
    QrValidationResult ValidateMatrix(QrSymbol symbol);
}
