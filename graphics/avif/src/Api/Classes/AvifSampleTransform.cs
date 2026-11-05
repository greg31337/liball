using System;
using System.Collections.Generic;

namespace avif;

/// <summary>
/// Parses, validates, and evaluates an AVIF sample-transform postfix expression.
/// </summary>
public class AvifSampleTransform : IAvifSampleTransform, IAvifDiagnosticSource
{
    AvifResultCode code;
    int inputCount;
    int intermediateBitDepth;
    List<AvifSampleTransformToken>? tokens;
    byte[]? data;
    AvifImage[]? inputImages;
    int outputBitDepth;

    /// <summary>Parses a complete <c>sato</c> payload for a known input-image count.</summary>
    /// <param name="payload">The complete aligned <c>SampleTransform</c> payload.</param>
    /// <param name="inputCount">The number of ordered <c>dimg</c> input images.</param>
    /// <returns>The parsed sample transform.</returns>
    internal static AvifSampleTransform Parse(byte[] payload, int inputCount)
    {
        AvifSampleTransform result = new AvifSampleTransform(
            payload,
            System.Array.Empty<AvifImage>(),
            0);
        result.Initialize(payload, inputCount);
        result.inputImages = System.Array.Empty<AvifImage>();
        result.outputBitDepth = 0;
        return result;
    }

    /// <summary>Creates an authoring transform from an arbitrary validated expression.</summary>
    /// <param name="payload">The complete aligned <c>SampleTransform</c> payload.</param>
    /// <param name="inputImages">
    /// The ordered input images. The first is the visible fallback and later inputs are hidden.
    /// </param>
    /// <param name="outputBitDepth">The derived image depth: 8, 10, 12, or 16.</param>
    public AvifSampleTransform(
        byte[] payload,
        AvifImage[] inputImages,
        int outputBitDepth)
    {
        int count = inputImages == null ? 0 : inputImages.Length;
        Initialize(payload, count);
        InitializeAuthoring(inputImages, outputBitDepth);
    }

    /// <summary>Creates an authoring transform from a validated expression.</summary>
    /// <returns>The initialized sample transform.</returns>
    internal static AvifSampleTransform Create(
        byte[] payload,
        AvifImage[] inputImages,
        int outputBitDepth)
    {
        return new AvifSampleTransform(
            payload,
            inputImages,
            outputBitDepth);
    }

    /// <summary>Parses a complete sample-transform payload.</summary>
    /// <returns>The parsed sample transform.</returns>
    internal static AvifSampleTransform Create(
        byte[] payload,
        int inputCount)
    {
        return Parse(payload, inputCount);
    }

    internal static AvifSampleTransform FromTokens(
        int inputCount,
        int intermediateBitDepth,
        List<AvifSampleTransformToken> tokens)
    {
        byte[] data = Serialize(intermediateBitDepth, tokens);
        AvifSampleTransform result = new AvifSampleTransform(
            data,
            System.Array.Empty<AvifImage>(),
            0);
        result.code = AvifResultCode.Success;
        result.inputCount = inputCount;
        result.intermediateBitDepth = intermediateBitDepth;
        result.tokens = new List<AvifSampleTransformToken>(tokens);
        result.data = (byte[])data.Clone();
        result.inputImages = System.Array.Empty<AvifImage>();
        result.outputBitDepth = 0;
        return result;
    }

    /// <summary>Creates the standard 8-bit suffix to 16-bit extension expression.</summary>
    /// <returns>An expression computing <c>256 * input1 + input2</c>.</returns>
    internal static AvifSampleTransform CreateSuffixBitDepthExtension()
    {
        List<AvifSampleTransformToken> recipe =
            new List<AvifSampleTransformToken>();
        recipe.Add(CreateConstant(256));
        recipe.Add(CreateInput(0));
        recipe.Add(CreateOperator(AvifSampleTransformOperator.Multiply));
        recipe.Add(CreateInput(1));
        recipe.Add(CreateOperator(AvifSampleTransformOperator.Add));
        return FromTokens(2, 32, recipe);
    }

    /// <summary>Creates an authoring recipe for an 8-bit suffix to 16-bit extension.</summary>
    /// <param name="mostSignificantImage">The visible eight-bit fallback and first input.</param>
    /// <param name="suffixImage">The hidden eight-bit suffix and second input.</param>
    /// <returns>An expression computing <c>256 * input1 + input2</c>.</returns>
    internal static AvifSampleTransform CreateSuffixBitDepthExtension(
        AvifImage mostSignificantImage,
        AvifImage suffixImage)
    {
        AvifSampleTransform result = CreateSuffixBitDepthExtension();
        result.InitializeAuthoring(
            new AvifImage[] { mostSignificantImage, suffixImage },
            16);
        return result;
    }

    /// <summary>Creates the standard 12-bit plus residual to 16-bit extension expression.</summary>
    /// <returns>An expression computing <c>16 * input1 + input2 - 128</c>.</returns>
    internal static AvifSampleTransform CreateResidualBitDepthExtension()
    {
        List<AvifSampleTransformToken> recipe =
            new List<AvifSampleTransformToken>();
        recipe.Add(CreateConstant(16));
        recipe.Add(CreateInput(0));
        recipe.Add(CreateOperator(AvifSampleTransformOperator.Multiply));
        recipe.Add(CreateInput(1));
        recipe.Add(CreateOperator(AvifSampleTransformOperator.Add));
        recipe.Add(CreateConstant(128));
        recipe.Add(CreateOperator(AvifSampleTransformOperator.Subtract));
        return FromTokens(2, 32, recipe);
    }

    /// <summary>Creates an authoring recipe for a 12-bit residual to 16-bit extension.</summary>
    /// <param name="mostSignificantImage">The visible 12-bit fallback and first input.</param>
    /// <param name="residualImage">The hidden eight-bit biased residual and second input.</param>
    /// <returns>An expression computing <c>16 * input1 + input2 - 128</c>.</returns>
    internal static AvifSampleTransform CreateResidualBitDepthExtension(
        AvifImage mostSignificantImage,
        AvifImage residualImage)
    {
        AvifSampleTransform result = CreateResidualBitDepthExtension();
        result.InitializeAuthoring(
            new AvifImage[] { mostSignificantImage, residualImage },
            16);
        return result;
    }

    /// <summary>Returns the payload parse and expression validation result.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns diagnostics for sample-transform parsing or rendering state.</summary>
    /// <returns>The structured rendering diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            code,
            AvifDiagnosticCategory.Rendering,
            "The sample-transform rendering operation did not complete successfully.");
    }

    /// <summary>Returns the number of ordered input images.</summary>
    /// <returns>The input count from one through 32.</returns>
    public int GetInputCount()
    {
        return inputCount;
    }

    /// <summary>Returns the signed intermediate arithmetic precision.</summary>
    /// <returns>Eight, 16, 32, or 64 bits.</returns>
    public int GetIntermediateBitDepth()
    {
        return intermediateBitDepth;
    }

    /// <summary>Returns the postfix token count.</summary>
    /// <returns>The validated token count.</returns>
    public int GetTokenCount()
    {
        return (tokens ?? throw new InvalidOperationException("Invalid internal state.")).Count;
    }

    /// <summary>Returns a copy of the complete aligned expression payload.</summary>
    /// <returns>The validated <c>sato</c> item payload.</returns>
    public byte[] GetData()
    {
        return code == AvifResultCode.Success
            ? (byte[])(data ?? throw new InvalidOperationException("Invalid internal state.")).Clone()
            : System.Array.Empty<byte>();
    }

    /// <summary>Returns the requested derived-image bit depth for authoring.</summary>
    /// <returns>Eight, 10, 12, or 16; zero for a parse-only transform.</returns>
    public int GetOutputBitDepth()
    {
        return outputBitDepth;
    }

    /// <summary>Returns one caller-owned authoring input image.</summary>
    /// <param name="index">The zero-based ordered input index.</param>
    /// <returns>The input image, or null when unavailable.</returns>
    public AvifImage? GetInputImage(int index)
    {
        if (index < 0 || index >= (inputImages ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            return null;
        }
        return inputImages[index];
    }

    /// <summary>Returns one postfix token.</summary>
    /// <param name="index">The zero-based token index.</param>
    /// <returns>The token, or null for an invalid index.</returns>
    public AvifSampleTransformToken? GetToken(int index)
    {
        if (index < 0 || index >= (tokens ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            return null;
        }
        return tokens[index];
    }

    /// <summary>Evaluates one output sample without applying output-range clamping.</summary>
    /// <param name="inputSamples">One unsigned sample value per ordered input image.</param>
    /// <param name="destination">A writable array whose first element receives the result.</param>
    /// <returns>A stable result code; the destination is unchanged on failure.</returns>
    public AvifResultCode Evaluate(
        long[] inputSamples,
        long[] destination)
    {
        if (destination == null || destination.Length == 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        SampleTransformEvaluationResult result =
            SampleTransformEvaluator.Evaluate(this, inputSamples);
        if (result.Code == AvifResultCode.Success)
        {
            destination[0] = result.Value;
        }
        return result.Code;
    }

    /// <summary>Evaluates the expression across compatible input images.</summary>
    /// <param name="inputs">The ordered decoded input images.</param>
    /// <param name="outputBitDepth">The output depth: 8, 10, 12, or 16.</param>
    /// <returns>An owning reconstructed image, or an image carrying a stable error code.</returns>
    public AvifImage Render(AvifImage[] inputs, int outputBitDepth)
    {
        return SampleTransformEvaluator.Render(
            this,
            inputs,
            outputBitDepth,
            new AvifSecurityLimits());
    }

    /// <summary>Evaluates the expression with explicit output allocation limits.</summary>
    /// <param name="inputs">The ordered decoded input images.</param>
    /// <param name="outputBitDepth">The output depth: 8, 10, 12, or 16.</param>
    /// <param name="limits">The output dimension, pixel, and decoded-byte limits.</param>
    /// <returns>An owning reconstructed image, or an image carrying a stable error code.</returns>
    public AvifImage RenderWithLimits(
        AvifImage[] inputs,
        int outputBitDepth,
        AvifSecurityLimits limits)
    {
        return SampleTransformEvaluator.Render(
            this,
            inputs,
            outputBitDepth,
            limits);
    }

    void Initialize(byte[] payload, int requestedInputCount)
    {
        tokens = new List<AvifSampleTransformToken>();
        data = System.Array.Empty<byte>();
        inputImages = System.Array.Empty<AvifImage>();
        outputBitDepth = 0;
        inputCount = requestedInputCount;
        SampleTransformParseResult parsed = SampleTransformParser.Parse(
            payload,
            requestedInputCount);
        code = parsed.Code;
        if (code == AvifResultCode.Success)
        {
            intermediateBitDepth = parsed.IntermediateBitDepth;
            tokens = new List<AvifSampleTransformToken>(parsed.Tokens);
            data = (byte[])payload.Clone();
        }
    }

    void InitializeAuthoring(
        AvifImage[]? requestedInputImages,
        int requestedOutputBitDepth)
    {
        if (code != AvifResultCode.Success)
        {
            return;
        }
        if (requestedInputImages == null
            || requestedInputImages.Length != inputCount
            || (requestedOutputBitDepth != 8
                && requestedOutputBitDepth != 10
                && requestedOutputBitDepth != 12
                && requestedOutputBitDepth != 16))
        {
            code = AvifResultCode.InvalidArgument;
            return;
        }
        int index = 0;
        while (index < requestedInputImages.Length)
        {
            if (requestedInputImages[index] == null
                || requestedInputImages[index].GetCode()
                    != AvifResultCode.Success)
            {
                code = AvifResultCode.InvalidArgument;
                return;
            }
            index++;
        }
        inputImages = (AvifImage[])requestedInputImages.Clone();
        outputBitDepth = requestedOutputBitDepth;
    }

    static byte[] Serialize(
        int bitDepth,
        List<AvifSampleTransformToken> recipe)
    {
        int constantBytes = bitDepth >> 3;
        int length = 2;
        int index = 0;
        while (index < recipe.Count)
        {
            length += recipe[index].IsConstant() ? 1 + constantBytes : 1;
            index++;
        }
        byte[] payload = new byte[length];
        int depthCode = bitDepth == 8 ? 0
            : bitDepth == 16 ? 1
            : bitDepth == 32 ? 2 : 3;
        payload[0] = (byte)depthCode;
        payload[1] = (byte)recipe.Count;
        int offset = 2;
        index = 0;
        while (index < recipe.Count)
        {
            AvifSampleTransformToken token = recipe[index];
            payload[offset] = (byte)token.GetEncodedValue();
            offset++;
            if (token.IsConstant())
            {
                WriteConstant(payload, offset, bitDepth, token.GetConstant());
                offset += constantBytes;
            }
            index++;
        }
        return payload;
    }

    static void WriteConstant(
        byte[] payload,
        int offset,
        int bitDepth,
        long value)
    {
        if (bitDepth == 8)
        {
            payload[offset] = (byte)(value & byte.MaxValue);
        }
        else if (bitDepth == 16)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt16BigEndian(
                payload.AsSpan(offset, 2),
                (short)value);
        }
        else if (bitDepth == 32)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(
                payload.AsSpan(offset, 4),
                (int)value);
        }
        else
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(
                payload.AsSpan(offset, 8),
                value);
        }
    }

    static AvifSampleTransformToken CreateConstant(long value)
    {
        return new AvifSampleTransformToken(
            0,
            0,
            value,
            -1,
            default(AvifSampleTransformOperator));
    }

    static AvifSampleTransformToken CreateInput(int index)
    {
        return new AvifSampleTransformToken(
            index + 1,
            1,
            0,
            index,
            default(AvifSampleTransformOperator));
    }

    static AvifSampleTransformToken CreateOperator(
        AvifSampleTransformOperator operation)
    {
        return new AvifSampleTransformToken(
            (int)operation,
            2,
            0,
            -1,
            operation);
    }
}
