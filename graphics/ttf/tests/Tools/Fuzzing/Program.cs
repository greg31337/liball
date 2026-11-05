namespace ttf.Fuzzing;

internal static class Program
{
    const int MaximumInputLength = 2 * 1024 * 1024;

    static int Main(string[] arguments)
    {
        if (arguments.Length == 0 || arguments.Length > 3)
        {
            WriteUsage();
            return 2;
        }

        string target = arguments[0];
        bool wrapSeedFont = false;
        string path = null;
        int argumentIndex = 1;

        while (argumentIndex < arguments.Length)
        {
            if (arguments[argumentIndex] == "--raw-font")
            {
                wrapSeedFont = true;
            }
            else if (path == null)
            {
                path = arguments[argumentIndex];
            }
            else
            {
                WriteUsage();
                return 2;
            }

            argumentIndex += 1;
        }

        byte[] input;

        if (path == null)
        {
            using (Stream stream = Console.OpenStandardInput())
            {
                if (!TryReadBounded(stream, out input))
                {
                    return 0;
                }
            }
        }
        else
        {
            using (FileStream stream = File.OpenRead(path))
            {
                if (!TryReadBounded(stream, out input))
                {
                    return 0;
                }
            }
        }

        if (wrapSeedFont && target != "container-load")
        {
            input = FontFuzzTargets.WrapSeedFont(input);
        }

        if (!FontFuzzTargets.Run(target, input))
        {
            WriteUsage();
            return 2;
        }

        return 0;
    }

    static bool TryReadBounded(Stream stream, out byte[] input)
    {
        byte[] buffer = new byte[MaximumInputLength + 1];
        int length = 0;

        while (length < buffer.Length)
        {
            int readLength = stream.Read(
                buffer,
                length,
                buffer.Length - length);

            if (readLength == 0)
            {
                break;
            }

            length += readLength;
        }

        if (length > MaximumInputLength)
        {
            input = Array.Empty<byte>();
            return false;
        }

        input = new byte[length];
        Array.Copy(buffer, input, length);
        return true;
    }

    static void WriteUsage()
    {
        Console.Error.WriteLine(
            "usage: ttf-fuzz <target> [--raw-font] [input-file]");
        Console.Error.WriteLine(
            "targets: container-load, face-inspection, glyph-outline, " +
            "shaping, instancing, subsetting, pdf-finalization");
    }
}
