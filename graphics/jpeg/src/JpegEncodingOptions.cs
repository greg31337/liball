namespace jpeg;

internal class JpegEncodingOptionsResolver
{
    internal JpegProcess ResolveProcess(JpegWriteOptions options)
    {
        ValidateOptions(options);
        JpegProcess process;
        if (options.Process != JpegProcess.Unknown)
        {
            process = options.Process;
        }
        else if (options.Progressive
            || (options.ProgressiveScanScript != null
                && options.ProgressiveScanScript.Scans != null
                && options.ProgressiveScanScript.Scans.Count > 0))
        {
            process = JpegProcess.ProgressiveDct;
        }
        else if (options.EntropyCoding == JpegEntropyCoding.Arithmetic)
        {
            process = JpegProcess.ExtendedSequentialDctArithmetic;
        }
        else
        {
            process = JpegProcess.BaselineDct;
        }

        ValidateEntropyForProcess(options, process);
        return process;
    }

    internal int ResolvePrecision(JpegImage image, JpegWriteOptions options, JpegProcess process)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        int precision = options.SamplePrecision;
        if (precision == 0)
        {
            precision = image.Precision;
        }

        if (precision == 0)
        {
            precision = 8;
            if (process == JpegProcess.ExtendedSequentialDct)
            {
                precision = 12;
            }
        }

        return precision;
    }

    internal void ValidateOptions(JpegWriteOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.SamplePrecision < 0 || options.SamplePrecision > 16)
        {
            throw JpegWriteValidationException.Create("Encoder sample precision must be between 1 and 16, or zero for automatic selection.");
        }

        if (options.PointTransform < 0 || options.PointTransform > 15)
        {
            throw JpegWriteValidationException.Create("Lossless point transform must be between 0 and 15.");
        }

        int predictor = (int)options.LosslessPredictor;
        if (predictor < 1 || predictor > 7)
        {
            throw JpegWriteValidationException.Create("Lossless predictor selection must be between 1 and 7.");
        }

        if (options.EntropyCoding != JpegEntropyCoding.Automatic
            && options.EntropyCoding != JpegEntropyCoding.Huffman
            && options.EntropyCoding != JpegEntropyCoding.Arithmetic)
        {
            throw JpegWriteValidationException.Create("The selected entropy coding mode is not recognized.");
        }

        if (options.MetadataPolicy != JpegMetadataPolicy.PreserveAll
            && options.MetadataPolicy != JpegMetadataPolicy.PreserveKnown
            && options.MetadataPolicy != JpegMetadataPolicy.StripAll
            && options.MetadataPolicy != JpegMetadataPolicy.StripUnsafe
            && options.MetadataPolicy != JpegMetadataPolicy.ReplaceSelected)
        {
            throw JpegWriteValidationException.Create("The selected metadata policy is not recognized.");
        }

        if (options.ColorPolicy != JpegColorPolicy.PreserveSource
            && options.ColorPolicy != JpegColorPolicy.ConvertToRgb
            && options.ColorPolicy != JpegColorPolicy.ConvertToCmyk
            && options.ColorPolicy != JpegColorPolicy.UseIccWhenSupported
            && options.ColorPolicy != JpegColorPolicy.FailIfUnsupported)
        {
            throw JpegWriteValidationException.Create("The selected color policy is not recognized.");
        }

        if (options.IccBehaviorPolicy != JpegIccBehaviorPolicy.Preserve
            && options.IccBehaviorPolicy != JpegIccBehaviorPolicy.Ignore
            && options.IccBehaviorPolicy != JpegIccBehaviorPolicy.ConvertWhenSupported
            && options.IccBehaviorPolicy != JpegIccBehaviorPolicy.FailIfUnsupported)
        {
            throw JpegWriteValidationException.Create("The selected ICC behavior policy is not recognized.");
        }

        if (options.ErrorPolicy != JpegErrorPolicy.Strict && options.ErrorPolicy != JpegErrorPolicy.Tolerant)
        {
            throw JpegWriteValidationException.Create("The selected error policy is not recognized.");
        }

        if (options.ScanLayout != JpegScanLayout.Automatic
            && options.ScanLayout != JpegScanLayout.Interleaved
            && options.ScanLayout != JpegScanLayout.NonInterleaved)
        {
            throw JpegWriteValidationException.Create("The selected scan layout is not recognized.");
        }

        int subsampling = (int)options.Subsampling;
        if (subsampling < 0 || subsampling > 7)
        {
            throw JpegWriteValidationException.Create("The selected subsampling mode is not recognized.");
        }

        if (options.ChromaDownsamplingFilter != JpegChromaDownsamplingFilter.Box
            && options.ChromaDownsamplingFilter != JpegChromaDownsamplingFilter.Center)
        {
            throw JpegWriteValidationException.Create("The selected chroma downsampling filter is not recognized.");
        }

        if (options.RestartIntervalUnit != JpegRestartIntervalUnit.McuBlocks
            && options.RestartIntervalUnit != JpegRestartIntervalUnit.McuRows)
        {
            throw JpegWriteValidationException.Create("The selected restart interval unit is not recognized.");
        }

        if (options.Hierarchical)
        {
            throw JpegUnsupportedException.Create("Hierarchical JPEG encoding is recognized but not supported.");
        }

        ValidateProcessSelection(options);
    }

    internal void ValidateProcessSelection(JpegWriteOptions options)
    {
        JpegProcess process = options.Process;
        if (process == JpegProcess.Unknown)
        {
            return;
        }

        if (!IsKnownProcess(process))
        {
            throw JpegWriteValidationException.Create("The selected JPEG process is not recognized.");
        }

        if (options.Progressive && !IsProgressiveProcess(process))
        {
            throw JpegWriteValidationException.Create("The progressive flag conflicts with the selected JPEG process.");
        }

        ValidateEntropyForProcess(options, process);
    }

    internal void ValidateEntropyForProcess(JpegWriteOptions options, JpegProcess process)
    {
        if (options.EntropyCoding == JpegEntropyCoding.Huffman && IsArithmeticProcess(process))
        {
            throw JpegWriteValidationException.Create("Huffman entropy coding conflicts with the selected arithmetic JPEG process.");
        }

        if (options.EntropyCoding == JpegEntropyCoding.Arithmetic && !IsArithmeticProcess(process))
        {
            throw JpegWriteValidationException.Create("Arithmetic entropy coding conflicts with the selected Huffman JPEG process.");
        }
    }

    internal void ValidateProcessSupport(JpegProcess process)
    {
        if (process == JpegProcess.BaselineDct
            || process == JpegProcess.ExtendedSequentialDct
            || process == JpegProcess.ProgressiveDct
            || process == JpegProcess.LosslessSequential
            || process == JpegProcess.LosslessSequentialArithmetic)
        {
            return;
        }

        if (IsDifferentialProcess(process))
        {
            throw JpegUnsupportedException.Create("Differential JPEG encoding is recognized but not supported.");
        }

        if (process == JpegProcess.JpegLs)
        {
            throw JpegUnsupportedException.Create("JPEG-LS encoding is recognized but not supported.");
        }

        if (IsArithmeticProcess(process))
        {
            throw JpegUnsupportedException.Create("Arithmetic DCT encoding is recognized but not supported.");
        }

        throw JpegUnsupportedException.Create("The selected JPEG process is not supported for encoding.");
    }

    internal void ValidatePrecisionForProcess(int precision, JpegProcess process)
    {
        if (process == JpegProcess.BaselineDct || process == JpegProcess.ProgressiveDct)
        {
            if (precision != 8)
            {
                throw JpegWriteValidationException.Create("Baseline and progressive DCT encoding require 8-bit samples.");
            }

            return;
        }

        if (process == JpegProcess.ExtendedSequentialDct)
        {
            if (precision != 12)
            {
                throw JpegWriteValidationException.Create("Extended sequential DCT encoding currently requires 12-bit samples.");
            }

            return;
        }

        if (process == JpegProcess.LosslessSequential || process == JpegProcess.LosslessSequentialArithmetic)
        {
            if (precision < 2 || precision > 16)
            {
                throw JpegWriteValidationException.Create("Lossless JPEG encoding requires sample precision between 2 and 16 bits.");
            }
        }
    }

    internal bool IsKnownProcess(JpegProcess process)
    {
        return process == JpegProcess.BaselineDct
            || process == JpegProcess.ExtendedSequentialDct
            || process == JpegProcess.ProgressiveDct
            || process == JpegProcess.LosslessSequential
            || process == JpegProcess.DifferentialSequentialDct
            || process == JpegProcess.DifferentialProgressiveDct
            || process == JpegProcess.DifferentialLosslessSequential
            || process == JpegProcess.ExtendedSequentialDctArithmetic
            || process == JpegProcess.ProgressiveDctArithmetic
            || process == JpegProcess.LosslessSequentialArithmetic
            || process == JpegProcess.DifferentialSequentialDctArithmetic
            || process == JpegProcess.DifferentialProgressiveDctArithmetic
            || process == JpegProcess.DifferentialLosslessSequentialArithmetic
            || process == JpegProcess.JpegLs;
    }

    internal bool IsProgressiveProcess(JpegProcess process)
    {
        return process == JpegProcess.ProgressiveDct
            || process == JpegProcess.DifferentialProgressiveDct
            || process == JpegProcess.ProgressiveDctArithmetic
            || process == JpegProcess.DifferentialProgressiveDctArithmetic;
    }

    internal bool IsArithmeticProcess(JpegProcess process)
    {
        return process == JpegProcess.ExtendedSequentialDctArithmetic
            || process == JpegProcess.ProgressiveDctArithmetic
            || process == JpegProcess.LosslessSequentialArithmetic
            || process == JpegProcess.DifferentialSequentialDctArithmetic
            || process == JpegProcess.DifferentialProgressiveDctArithmetic
            || process == JpegProcess.DifferentialLosslessSequentialArithmetic;
    }

    internal bool IsDifferentialProcess(JpegProcess process)
    {
        return process == JpegProcess.DifferentialSequentialDct
            || process == JpegProcess.DifferentialProgressiveDct
            || process == JpegProcess.DifferentialLosslessSequential
            || process == JpegProcess.DifferentialSequentialDctArithmetic
            || process == JpegProcess.DifferentialProgressiveDctArithmetic
            || process == JpegProcess.DifferentialLosslessSequentialArithmetic;
    }
}
