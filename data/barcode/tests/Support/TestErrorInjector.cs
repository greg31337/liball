namespace barcode.Tests;

internal class ByteErrorInjection
{
    internal byte[] Values = new byte[0];
    internal int[] Indices = new int[0];
}

internal class IntegerErrorInjection
{
    internal int[] Values = new int[0];
    internal int[] Indices = new int[0];
}

internal class ModuleErrorInjection
{
    internal ModuleMatrix Matrix = new ModuleMatrix(1, 1);
    internal int[] Indices = new int[0];
}

internal static class TestErrorInjector
{
    internal static ByteErrorInjection InjectByteErrors(
        byte[] source,
        int errorCount,
        int seed)
    {
        DeterministicTestRandom random =
            new DeterministicTestRandom(seed);
        int[] indices = random.SelectDistinctIndices(
            source.Length,
            errorCount);
        byte[] damaged = FlipBytesAtIndices(source, indices, 1);
        ByteErrorInjection result = new ByteErrorInjection();
        result.Values = damaged;
        result.Indices = indices;
        return result;
    }

    internal static byte[] FlipBytesAtIndices(
        byte[] source,
        int[] indices,
        byte mask)
    {
        if (mask == 0)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(mask));
        }

        byte[] damaged = BarcodeTestBuilder.CopyRequiredBytes(source);
        int index = 0;
        while (index < indices.Length)
        {
            ValidateIndex(indices[index], damaged.Length);
            damaged[indices[index]] = (byte)(damaged[indices[index]] ^ mask);
            index = index + 1;
        }

        return damaged;
    }

    internal static IntegerErrorInjection InjectCodewordErrors(
        int[] source,
        int errorCount,
        int modulus,
        int seed)
    {
        DeterministicTestRandom random =
            new DeterministicTestRandom(seed);
        int[] indices = random.SelectDistinctIndices(
            source.Length,
            errorCount);
        int[] damaged = ChangeCodewordsAtIndices(
            source,
            indices,
            modulus,
            seed);
        IntegerErrorInjection result = new IntegerErrorInjection();
        result.Values = damaged;
        result.Indices = indices;
        return result;
    }

    internal static int[] ChangeCodewordsAtIndices(
        int[] source,
        int[] indices,
        int modulus,
        int seed)
    {
        if (modulus <= 1)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(modulus));
        }

        int[] damaged = BarcodeTestBuilder.CopyIntegers(source);
        DeterministicTestRandom random =
            new DeterministicTestRandom(seed);
        int index = 0;
        while (index < indices.Length)
        {
            int target = indices[index];
            ValidateIndex(target, damaged.Length);
            int magnitude = random.NextInteger(modulus - 1) + 1;
            damaged[target] = (damaged[target] + magnitude) % modulus;
            index = index + 1;
        }

        return damaged;
    }

    internal static int[] ChangePdf417CodewordsAtIndices(
        int[] source,
        int[] indices)
    {
        int[] damaged = BarcodeTestBuilder.CopyIntegers(source);
        int index = 0;
        while (index < indices.Length)
        {
            int target = indices[index];
            ValidateIndex(target, damaged.Length);
            int magnitude = ((index * 37) % 928) + 1;
            damaged[target] = (damaged[target] + magnitude) % 929;
            index = index + 1;
        }

        return damaged;
    }

    internal static ModuleErrorInjection InjectModuleErrors(
        ModuleMatrix source,
        int errorCount,
        int seed)
    {
        int length = source.Width * source.Height;
        DeterministicTestRandom random =
            new DeterministicTestRandom(seed);
        int[] indices = random.SelectDistinctIndices(length, errorCount);
        ModuleMatrix damaged = FlipModulesAtIndices(source, indices);
        ModuleErrorInjection result = new ModuleErrorInjection();
        result.Matrix = damaged;
        result.Indices = indices;
        return result;
    }

    internal static ModuleMatrix FlipModulesAtIndices(
        ModuleMatrix source,
        int[] indices)
    {
        ModuleMatrix damaged = BarcodeTestBuilder.CopyMatrix(source);
        int length = damaged.Width * damaged.Height;
        int index = 0;
        while (index < indices.Length)
        {
            int target = indices[index];
            ValidateIndex(target, length);
            int x = target % damaged.Width;
            int y = target / damaged.Width;
            damaged.SetModule(x, y, !damaged.GetModule(x, y));
            index = index + 1;
        }

        return damaged;
    }

    internal static BarcodeImage AddGrayNoise(
        BarcodeImage source,
        int changedPixelCount,
        int maximumDelta,
        int seed)
    {
        RequirePackedGrayImage(source);
        if (maximumDelta <= 0 || maximumDelta > 255)
        {
            throw new global::System.ArgumentOutOfRangeException(
                nameof(maximumDelta));
        }

        BarcodeImage damaged = BarcodeTestBuilder.CopyImage(source);
        byte[] pixels = damaged.Pixels!;
        DeterministicTestRandom random =
            new DeterministicTestRandom(seed);
        int[] indices = random.SelectDistinctIndices(
            pixels.Length,
            changedPixelCount);
        int index = 0;
        while (index < indices.Length)
        {
            int target = indices[index];
            int delta = random.NextInteger(maximumDelta) + 1;
            int original = pixels[target];
            int direction;
            if (original == 0)
            {
                direction = 1;
            }
            else if (original == 255)
            {
                direction = -1;
            }
            else
            {
                direction = random.NextInteger(2) == 0 ? -1 : 1;
            }

            int value = pixels[target] + (direction * delta);
            if (value < 0)
            {
                value = 0;
            }
            else if (value > 255)
            {
                value = 255;
            }

            pixels[target] = (byte)value;
            index = index + 1;
        }

        return damaged;
    }

    static void ValidateIndex(int index, int length)
    {
        if (index < 0 || index >= length)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(index));
        }
    }

    static void RequirePackedGrayImage(BarcodeImage image)
    {
        if (image.PixelFormat != BarcodePixelFormat.Gray8 ||
            image.Pixels == null || image.Offset != 0 ||
            image.Stride != image.Width ||
            image.Pixels.Length != image.Width * image.Height)
        {
            throw new global::System.ArgumentException(
                "The test image must be a packed Gray8 image.");
        }
    }
}
