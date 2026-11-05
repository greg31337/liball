using System.Collections.Generic;

namespace jpeg;

internal class JpegEncoderPresetResolver
{
    internal JpegWriteOptions Resolve(JpegImage image, JpegWriteOptions options)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        JpegWriteOptions resolved = Copy(options);
        ValidatePreset(resolved.Preset);
        ApplyPreset(resolved, image);
        if (resolved.Deterministic)
        {
            resolved.OptimizeHuffmanTables = false;
        }

        return resolved;
    }

    internal JpegWriteOptions CreateInspectedOptions(JpegWriteOptions resolved, JpegInfo info)
    {
        if (resolved == null)
        {
            throw new System.ArgumentNullException("resolved");
        }

        if (info == null)
        {
            throw new System.ArgumentNullException("info");
        }

        JpegWriteOptions inspected = Copy(resolved);
        inspected.Process = info.Process;
        inspected.SamplePrecision = info.Precision;
        inspected.Progressive = info.Progressive;
        inspected.EntropyCoding = JpegEntropyCoding.Huffman;
        if (info.ArithmeticCoding)
        {
            inspected.EntropyCoding = JpegEntropyCoding.Arithmetic;
        }

        inspected.Subsampling = info.Subsampling;
        inspected.ScanLayout = ResolveScanLayout(info);
        inspected.RestartInterval = info.ResolvedRestartInterval;
        inspected.RestartIntervalUnit = JpegRestartIntervalUnit.McuBlocks;
        return inspected;
    }

    internal JpegWriteOptions Copy(JpegWriteOptions source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        JpegWriteOptions copy = new JpegWriteOptions();
        copy.Preset = source.Preset;
        copy.Deterministic = source.Deterministic;
        copy.MetadataPolicy = source.MetadataPolicy;
        copy.ColorPolicy = source.ColorPolicy;
        copy.IccBehaviorPolicy = source.IccBehaviorPolicy;
        copy.ErrorPolicy = source.ErrorPolicy;
        copy.Process = source.Process;
        copy.SamplePrecision = source.SamplePrecision;
        copy.EntropyCoding = source.EntropyCoding;
        copy.LosslessPredictor = source.LosslessPredictor;
        copy.PointTransform = source.PointTransform;
        copy.Hierarchical = source.Hierarchical;
        copy.Components = CopyComponents(source.Components);
        copy.Tables = CopyTables(source.Tables);
        copy.PreserveQuantizationTables = source.PreserveQuantizationTables;
        copy.ScanTableAssignments = CopyScanTableAssignments(source.ScanTableAssignments);
        copy.ScanLayout = source.ScanLayout;
        copy.Subsampling = source.Subsampling;
        copy.ChromaDownsamplingFilter = source.ChromaDownsamplingFilter;
        copy.Quality = source.Quality;
        copy.LuminanceQuality = source.LuminanceQuality;
        copy.ChrominanceQuality = source.ChrominanceQuality;
        copy.Progressive = source.Progressive;
        copy.ProgressiveScanScript = CopyProgressiveScanScript(source.ProgressiveScanScript);
        copy.OptimizeHuffmanTables = source.OptimizeHuffmanTables;
        copy.RestartInterval = source.RestartInterval;
        copy.RestartIntervalUnit = source.RestartIntervalUnit;
        return copy;
    }

    internal void ApplyPreset(JpegWriteOptions options, JpegImage image)
    {
        if (options.Preset == JpegEncoderPreset.None)
        {
            return;
        }

        if (options.Preset == JpegEncoderPreset.Web)
        {
            ApplyDctPreset(options, image, JpegProcess.ProgressiveDct, 82, JpegSubsampling.Ratio420, JpegMetadataPolicy.StripUnsafe, false);
            return;
        }

        if (options.Preset == JpegEncoderPreset.Archival)
        {
            ApplyDctPreset(options, image, JpegProcess.ProgressiveDct, 95, JpegSubsampling.Ratio444, JpegMetadataPolicy.PreserveAll, false);
            return;
        }

        if (options.Preset == JpegEncoderPreset.MaximumCompatibility)
        {
            ApplyDctPreset(options, image, JpegProcess.BaselineDct, 75, JpegSubsampling.Ratio420, JpegMetadataPolicy.PreserveKnown, false);
            return;
        }

        if (options.Preset == JpegEncoderPreset.MaximumCompression)
        {
            ApplyDctPreset(options, image, JpegProcess.BaselineDct, 60, JpegSubsampling.Ratio420, JpegMetadataPolicy.StripAll, true);
            return;
        }

        options.MetadataPolicy = JpegMetadataPolicy.PreserveAll;
        options.Subsampling = ResolveMetadataSampling(image);
        options.PreserveQuantizationTables = image.QuantizationTables != null
            && image.QuantizationTables.Count > 0
            && options.Tables.QuantizationTables.Count == 0;
    }

    internal void ApplyDctPreset(
        JpegWriteOptions options,
        JpegImage image,
        JpegProcess process,
        int quality,
        JpegSubsampling subsampling,
        JpegMetadataPolicy metadataPolicy,
        bool optimizeHuffmanTables)
    {
        options.Process = process;
        options.SamplePrecision = 8;
        options.EntropyCoding = JpegEntropyCoding.Huffman;
        options.ScanLayout = JpegScanLayout.Interleaved;
        options.Subsampling = ResolveDctSampling(image, subsampling);
        options.Quality = quality;
        options.LuminanceQuality = 0;
        options.ChrominanceQuality = 0;
        options.Progressive = process == JpegProcess.ProgressiveDct;
        options.ProgressiveScanScript = new JpegProgressiveScanScript();
        options.OptimizeHuffmanTables = optimizeHuffmanTables;
        options.MetadataPolicy = metadataPolicy;
        options.PreserveQuantizationTables = false;
    }

    internal JpegSubsampling ResolveDctSampling(JpegImage image, JpegSubsampling requested)
    {
        if (image.ComponentCount == 1)
        {
            return JpegSubsampling.Grayscale;
        }

        if (image.ComponentCount == 3)
        {
            return requested;
        }

        return JpegSubsampling.Ratio444;
    }

    internal JpegSubsampling ResolveMetadataSampling(JpegImage image)
    {
        if (image.ComponentCount == 1)
        {
            return JpegSubsampling.Grayscale;
        }

        if (image.ComponentCount == 3
            && image.Subsampling != JpegSubsampling.Unknown
            && image.Subsampling != JpegSubsampling.Grayscale
            && image.Subsampling != JpegSubsampling.Custom)
        {
            return image.Subsampling;
        }

        return JpegSubsampling.Ratio444;
    }

    internal JpegScanLayout ResolveScanLayout(JpegInfo info)
    {
        if (info.ComponentCount > 1 && info.ScanHeaders.Count > 1)
        {
            bool allSeparate = true;
            foreach (JpegScanHeader scan in info.ScanHeaders)
            {
                if (scan.Components.Count != 1)
                {
                    allSeparate = false;
                }
            }

            if (allSeparate)
            {
                return JpegScanLayout.NonInterleaved;
            }
        }

        return JpegScanLayout.Interleaved;
    }

    internal void ValidatePreset(JpegEncoderPreset preset)
    {
        if (preset != JpegEncoderPreset.None
            && preset != JpegEncoderPreset.Web
            && preset != JpegEncoderPreset.Archival
            && preset != JpegEncoderPreset.MaximumCompatibility
            && preset != JpegEncoderPreset.MaximumCompression
            && preset != JpegEncoderPreset.MetadataPreserving)
        {
            throw JpegWriteValidationException.Create("The selected encoder preset is not recognized.");
        }
    }

    internal List<JpegEncoderComponent> CopyComponents(List<JpegEncoderComponent> source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("components");
        }

        return new List<JpegEncoderComponent>(source);
    }

    internal List<JpegEncoderScanTableAssignment> CopyScanTableAssignments(List<JpegEncoderScanTableAssignment> source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("scanTableAssignments");
        }

        return new List<JpegEncoderScanTableAssignment>(source);
    }

    internal JpegTableSet CopyTables(JpegTableSet source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        JpegTableSet copy = new JpegTableSet();
        copy.QuantizationTables = new List<JpegQuantizationTable>(source.QuantizationTables);
        copy.HuffmanTables = new List<JpegHuffmanTable>(source.HuffmanTables);
        copy.ArithmeticConditioningTables = new List<JpegArithmeticConditioningTable>(source.ArithmeticConditioningTables);
        return copy;
    }

    internal JpegProgressiveScanScript CopyProgressiveScanScript(JpegProgressiveScanScript source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("progressiveScanScript");
        }

        JpegProgressiveScanScript copy = new JpegProgressiveScanScript();
        copy.Scans = new List<JpegScanHeader>(source.Scans);
        return copy;
    }
}
