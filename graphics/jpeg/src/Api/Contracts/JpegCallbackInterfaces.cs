namespace jpeg;

/// <summary>Receives decoded pixel rows through a reusable bounded buffer.</summary>
public interface IJpegDecodedRowCallback
{
    /// <summary>Processes one decoded row before the buffer is reused for the next row.</summary>
    /// <param name="row">The current decoded row and its reusable sample buffer.</param>
    void OnDecodedRow(JpegRowBuffer row);
}

/// <summary>Supplies source pixel rows through a reusable bounded buffer.</summary>
public interface IJpegEncodeRowCallback
{
    /// <summary>Fills one source row before it is consumed by the encoder.</summary>
    /// <param name="row">The requested row and its reusable sample buffer.</param>
    void FillEncodeRow(JpegRowBuffer row);
}

/// <summary>Receives a decoded preview after each progressive JPEG scan.</summary>
public interface IJpegProgressivePreviewCallback
{
    /// <summary>Processes the current progressive preview before decoding continues.</summary>
    /// <param name="preview">The scan position and decoded preview image.</param>
    void OnProgressivePreview(JpegProgressivePreview preview);
}
