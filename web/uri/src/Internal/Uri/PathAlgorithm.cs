using System;

namespace uri;

internal static class PathAlgorithm
{
    internal static string RemoveDotSegments(ReadOnlySpan<char> path)
    {
        char[] output = new char[path.Length];
        int inputOffset = 0;
        int outputLength = 0;
        while (inputOffset < path.Length)
        {
            if (StartsWithText(path, inputOffset, "../"))
            {
                inputOffset = inputOffset + 3;
                continue;
            }

            if (StartsWithText(path, inputOffset, "./"))
            {
                inputOffset = inputOffset + 2;
                continue;
            }

            if (StartsWithText(path, inputOffset, "/./"))
            {
                inputOffset = inputOffset + 2;
                continue;
            }

            int remainingLength = path.Length - inputOffset;
            if (remainingLength == 2 &&
                path[inputOffset] == '/' &&
                path[inputOffset + 1] == '.')
            {
                output[outputLength] = '/';
                outputLength = outputLength + 1;
                inputOffset = path.Length;
                continue;
            }

            if (StartsWithText(path, inputOffset, "/../"))
            {
                inputOffset = inputOffset + 3;
                outputLength = RemoveLastOutputSegment(output, outputLength);
                continue;
            }

            if (remainingLength == 3 &&
                path[inputOffset] == '/' &&
                path[inputOffset + 1] == '.' &&
                path[inputOffset + 2] == '.')
            {
                outputLength = RemoveLastOutputSegment(output, outputLength);
                output[outputLength] = '/';
                outputLength = outputLength + 1;
                inputOffset = path.Length;
                continue;
            }

            if (remainingLength == 1 && path[inputOffset] == '.')
            {
                inputOffset = path.Length;
                continue;
            }

            if (remainingLength == 2 &&
                path[inputOffset] == '.' &&
                path[inputOffset + 1] == '.')
            {
                inputOffset = path.Length;
                continue;
            }

            int segmentEnd = inputOffset;
            if (path[segmentEnd] == '/')
            {
                segmentEnd = segmentEnd + 1;
            }

            while (segmentEnd < path.Length && path[segmentEnd] != '/')
            {
                segmentEnd = segmentEnd + 1;
            }

            int copyOffset = inputOffset;
            while (copyOffset < segmentEnd)
            {
                output[outputLength] = path[copyOffset];
                outputLength = outputLength + 1;
                copyOffset = copyOffset + 1;
            }

            inputOffset = segmentEnd;
        }

        return new string(output, 0, outputLength);
    }

    internal static bool StartsWithText(
        ReadOnlySpan<char> text,
        int offset,
        string prefix)
    {
        int remainingLength = text.Length - offset;
        if (remainingLength < prefix.Length)
        {
            return false;
        }

        int prefixOffset = 0;
        while (prefixOffset < prefix.Length)
        {
            if (text[offset + prefixOffset] != prefix[prefixOffset])
            {
                return false;
            }

            prefixOffset = prefixOffset + 1;
        }

        return true;
    }

    internal static int RemoveLastOutputSegment(char[] output, int outputLength)
    {
        int offset = outputLength - 1;
        while (offset >= 0)
        {
            if (output[offset] == '/')
            {
                return offset;
            }

            offset = offset - 1;
        }

        return 0;
    }
}
