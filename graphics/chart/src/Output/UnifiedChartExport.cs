namespace chart;

internal static class UnifiedChartExportOperations
{
    internal static ChartExportResult ExportScene(ChartRenderScene scene, ChartExportOptions options)
    {
        ValidateSceneOptions(scene, options);
        return ExportPrepared(scene, options, options.Accessibility);
    }

    internal static ChartFrameExportResult[] ExportTimeline(ChartTimeline timeline, ChartExportOptions options)
    {
        if (timeline == null)
        {
            throw new System.ArgumentNullException("timeline");
        }

        ValidateOptions(options);
        ChartFrameExportResult[] results = new ChartFrameExportResult[timeline.FrameCount];
        long start = 0;
        int index = 0;
        while (index < timeline.FrameCount)
        {
            ChartTimelineFrame frame = timeline.Frames[index];
            ChartFrameExportResult result = new ChartFrameExportResult();
            result.FrameId = frame.FrameId;
            result.FrameIndex = index;
            result.StartMilliseconds = start;
            result.DurationMilliseconds = frame.DurationMilliseconds;
            result.Export = ExportPrepared(frame.Scene, options, options.Accessibility);
            results[index] = result;
            start = start + frame.DurationMilliseconds;
            index = index + 1;
        }

        return results;
    }

    internal static ChartExportResult ExportDocument(ChartDocument document, IChartRenderer renderer, ChartExportOptions options)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        if (renderer == null)
        {
            throw new System.ArgumentNullException("renderer");
        }

        ValidateOptions(options);
        ChartRenderScene scene = document.Render(renderer);
        ChartAccessibilityMetadata accessibility = options.Accessibility;
        if (!accessibility.HasMetadata())
        {
            accessibility = document.Accessibility;
        }

        return ExportPrepared(scene, options, accessibility);
    }

    internal static void WriteSceneExport(ChartRenderScene scene, System.IO.Stream stream, ChartExportOptions options)
    {
        ValidateStream(stream);
        ChartExportResult result = ExportScene(scene, options);
        WriteResult(stream, result, options.LeaveStreamOpen);
    }

    internal static void WriteDocumentExport(ChartDocument document, IChartRenderer renderer, System.IO.Stream stream, ChartExportOptions options)
    {
        ValidateStream(stream);
        ChartExportResult result = ExportDocument(document, renderer, options);
        WriteResult(stream, result, options.LeaveStreamOpen);
    }

    internal static void SaveSceneExport(ChartRenderScene scene, string path, ChartExportOptions options)
    {
        ValidatePath(path);
        ChartExportResult result = ExportScene(scene, options);
        System.IO.File.WriteAllBytes(path, result.Bytes);
    }

    internal static void SaveDocumentExport(ChartDocument document, IChartRenderer renderer, string path, ChartExportOptions options)
    {
        ValidatePath(path);
        ChartExportResult result = ExportDocument(document, renderer, options);
        System.IO.File.WriteAllBytes(path, result.Bytes);
    }

    internal static ChartExportResult ExportPrepared(ChartRenderScene source, ChartExportOptions options, ChartAccessibilityMetadata accessibility)
    {
        ChartRenderScene scene = PrepareScene(source, options);
        ChartExportResult result = new ChartExportResult();
        result.Format = options.Format;
        if (options.Format == ChartOutputFormat.Svg)
        {
            result.Text = ChartExportOperations.ToSvgWithOptions(scene, accessibility, options.Svg);
            result.Bytes = ChartExportOperations.ToSvgBytes(result.Text);
            return result;
        }

        RawBitmap bitmap = scene.ToRawBitmap();
        if (options.Format == ChartOutputFormat.RawBitmap)
        {
            result.Bitmap = bitmap;
            result.Bytes = bitmap.CopyBuffer();
            return result;
        }

        if (options.Format == ChartOutputFormat.Bmp)
        {
            result.Bytes = ChartExportOperations.ToBmpBytesUsingOptions(bitmap, options.Bmp);
            return result;
        }

        ChartPngOptions png = ResolvePngOptions(options.Png, accessibility);
        result.Bytes = ChartExportOperations.ToPngBytesWithOptions(bitmap, png);
        return result;
    }

    internal static ChartRenderScene PrepareScene(ChartRenderScene source, ChartExportOptions options)
    {
        double scale = options.Scale.EffectiveScale();
        int width = options.Scale.ScaleWidth(source.Width);
        int height = options.Scale.ScaleHeight(source.Height);
        ChartRenderScene scene = new ChartRenderScene(width, height);
        if (options.BackgroundMode == ChartExportBackgroundMode.SolidColor)
        {
            scene.AddClear(options.BackgroundColor);
        }

        bool scaled = scale != 1.0;
        if (scaled)
        {
            scene.PushTransform("chart-export-scale", ChartMatrix.Scale(scale, scale));
        }

        int index = 0;
        while (index < source.CommandCount)
        {
            ChartRenderCommand command = source.Commands[index];
            if (options.BackgroundMode == ChartExportBackgroundMode.PreserveScene || command.Kind != ChartRenderCommandKind.Clear)
            {
                scene.AddCommand(command);
            }

            index = index + 1;
        }

        if (scaled)
        {
            scene.PopTransform();
        }

        return scene;
    }

    internal static ChartPngOptions ResolvePngOptions(ChartPngOptions source, ChartAccessibilityMetadata accessibility)
    {
        ChartPngOptions options = new ChartPngOptions();
        options.ColorType = source.ColorType;
        options.Filter = source.Filter;
        options.Compression = source.Compression;
        options.Metadata.Title = source.Metadata.Title;
        options.Metadata.Author = source.Metadata.Author;
        options.Metadata.Description = source.Metadata.Description;
        options.Metadata.Copyright = source.Metadata.Copyright;
        options.Metadata.Software = source.Metadata.Software;
        if (options.Metadata.Title.Length == 0)
        {
            options.Metadata.Title = accessibility.Title;
        }

        if (options.Metadata.Description.Length == 0)
        {
            options.Metadata.Description = accessibility.Description;
        }

        return options;
    }

    internal static void WriteResult(System.IO.Stream stream, ChartExportResult result, bool leaveOpen)
    {
        stream.Write(result.Bytes, 0, result.Bytes.Length);
        if (!leaveOpen)
        {
            stream.Dispose();
        }
    }

    internal static void ValidateSceneOptions(ChartRenderScene scene, ChartExportOptions options)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ValidateOptions(options);
    }

    internal static void ValidateOptions(ChartExportOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        int format = (int)options.Format;
        if (format < 0 || format > (int)ChartOutputFormat.Svg)
        {
            throw new System.ArgumentOutOfRangeException("Format");
        }

        int backgroundMode = (int)options.BackgroundMode;
        if (backgroundMode < 0 || backgroundMode > (int)ChartExportBackgroundMode.SolidColor)
        {
            throw new System.ArgumentOutOfRangeException("BackgroundMode");
        }

        if (options.BackgroundColor == null)
        {
            throw new System.ArgumentNullException("BackgroundColor");
        }

        if (options.Scale == null)
        {
            throw new System.ArgumentNullException("Scale");
        }

        options.Scale.EffectiveScale();
        if (options.Accessibility == null)
        {
            throw new System.ArgumentNullException("Accessibility");
        }

        if (options.Bmp == null)
        {
            throw new System.ArgumentNullException("Bmp");
        }

        if (options.Png == null)
        {
            throw new System.ArgumentNullException("Png");
        }

        if (options.Png.Metadata == null)
        {
            throw new System.ArgumentNullException("Metadata");
        }

        if (options.Svg == null)
        {
            throw new System.ArgumentNullException("Svg");
        }
    }

    internal static void ValidateStream(System.IO.Stream stream)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        if (!stream.CanWrite)
        {
            throw new System.ArgumentException("The destination stream must be writable.");
        }
    }

    internal static void ValidatePath(string path)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        if (path.Length == 0)
        {
            throw new System.ArgumentException("The destination path cannot be empty.");
        }
    }
}
