using System;

namespace uri;

internal static class UriPathUtilityCore
{
    internal static string RemoveDotSegments(string? path)
    {
        if (path == null)
        {
            throw new ArgumentException("A path is required.");
        }

        return PathAlgorithm.RemoveDotSegments(path.AsSpan());
    }

    internal static string[] SplitPathSegments(string? path)
    {
        if (path == null)
        {
            throw new ArgumentException("A path is required.");
        }

        if (path.Length == 0)
        {
            return new string[0];
        }

        int segmentCount = PathParser.CountSlashes(path.AsSpan()) + 1;
        string[] segments = new string[segmentCount];
        int segmentOffset = 0;
        int segmentStart = 0;
        int pathOffset = 0;
        while (pathOffset <= path.Length)
        {
            if (pathOffset == path.Length || path[pathOffset] == '/')
            {
                segments[segmentOffset] = path.AsSpan(
                    segmentStart,
                    pathOffset - segmentStart).ToString();
                segmentOffset = segmentOffset + 1;
                segmentStart = pathOffset + 1;
            }

            pathOffset = pathOffset + 1;
        }

        return segments;
    }

    internal static string JoinPaths(string[]? paths)
    {
        if (paths == null)
        {
            throw new ArgumentException("A path collection is required.");
        }

        if (paths.Length == 0)
        {
            return "";
        }

        int pathOffset = 0;
        while (pathOffset < paths.Length)
        {
            if (paths[pathOffset] == null)
            {
                throw new ArgumentException("Every path token is required.");
            }

            pathOffset = pathOffset + 1;
        }

        long measuredLength = MeasureJoinedPath(paths);
        if (measuredLength > int.MaxValue)
        {
            throw new ArgumentException("The joined path is too long.");
        }

        char[] output = new char[(int)measuredLength];
        int outputOffset = CopyPathText(paths[0], output, 0, 0);
        pathOffset = 1;
        while (pathOffset < paths.Length)
        {
            string path = paths[pathOffset];
            bool outputEndsWithSlash = outputOffset > 0 &&
                output[outputOffset - 1] == '/';
            bool pathStartsWithSlash = path.Length > 0 && path[0] == '/';
            int inputOffset = 0;
            if (outputEndsWithSlash && pathStartsWithSlash)
            {
                inputOffset = 1;
            }
            else if (!outputEndsWithSlash && !pathStartsWithSlash)
            {
                output[outputOffset] = '/';
                outputOffset = outputOffset + 1;
            }

            outputOffset = CopyPathText(path, output, outputOffset, inputOffset);
            pathOffset = pathOffset + 1;
        }

        return new string(output);
    }

    internal static string FindCommonBasePath(
        string? firstPath,
        string? secondPath)
    {
        if (firstPath == null || secondPath == null)
        {
            throw new ArgumentException("Two paths are required.");
        }

        int sharedLength = firstPath.Length;
        if (secondPath.Length < sharedLength)
        {
            sharedLength = secondPath.Length;
        }

        int commonDirectoryLength = 0;
        int offset = 0;
        while (offset < sharedLength && firstPath[offset] == secondPath[offset])
        {
            if (firstPath[offset] == '/')
            {
                commonDirectoryLength = offset + 1;
            }

            offset = offset + 1;
        }

        return firstPath.Substring(0, commonDirectoryLength);
    }

    internal static long MeasureJoinedPath(string[] paths)
    {
        if (paths.Length == 0)
        {
            return 0;
        }

        long outputLength = paths[0].Length;
        bool outputEndsWithSlash = paths[0].Length > 0 &&
            paths[0][paths[0].Length - 1] == '/';
        int pathOffset = 1;
        while (pathOffset < paths.Length)
        {
            string path = paths[pathOffset];
            bool pathStartsWithSlash = path.Length > 0 && path[0] == '/';
            if (outputEndsWithSlash && pathStartsWithSlash)
            {
                outputLength = outputLength + path.Length - 1;
            }
            else if (!outputEndsWithSlash && !pathStartsWithSlash)
            {
                outputLength = outputLength + path.Length + 1;
            }
            else
            {
                outputLength = outputLength + path.Length;
            }

            outputEndsWithSlash = path.Length == 0 ||
                path[path.Length - 1] == '/';
            pathOffset = pathOffset + 1;
        }

        return outputLength;
    }

    internal static long MeasureJoinedPathSegments(string[] paths)
    {
        if (paths.Length == 0)
        {
            return 0;
        }

        long outputLength = paths[0].Length;
        long slashCount = PathParser.CountSlashes(paths[0].AsSpan());
        bool outputEndsWithSlash = paths[0].Length > 0 &&
            paths[0][paths[0].Length - 1] == '/';
        int pathOffset = 1;
        while (pathOffset < paths.Length)
        {
            string path = paths[pathOffset];
            bool pathStartsWithSlash = path.Length > 0 && path[0] == '/';
            int pathSlashCount = PathParser.CountSlashes(path.AsSpan());
            if (outputEndsWithSlash && pathStartsWithSlash)
            {
                outputLength = outputLength + path.Length - 1;
                slashCount = slashCount + pathSlashCount - 1;
            }
            else if (!outputEndsWithSlash && !pathStartsWithSlash)
            {
                outputLength = outputLength + path.Length + 1;
                slashCount = slashCount + pathSlashCount + 1;
            }
            else
            {
                outputLength = outputLength + path.Length;
                slashCount = slashCount + pathSlashCount;
            }

            outputEndsWithSlash = path.Length == 0 ||
                path[path.Length - 1] == '/';
            pathOffset = pathOffset + 1;
        }

        if (outputLength == 0)
        {
            return 0;
        }

        return slashCount + 1;
    }

    internal static int CopyPathText(
        string path,
        char[] output,
        int outputOffset,
        int inputOffset)
    {
        while (inputOffset < path.Length)
        {
            output[outputOffset] = path[inputOffset];
            outputOffset = outputOffset + 1;
            inputOffset = inputOffset + 1;
        }

        return outputOffset;
    }
}
