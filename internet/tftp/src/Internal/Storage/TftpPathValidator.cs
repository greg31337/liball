using System;
using System.IO;

namespace tftp;

internal class TftpPathValidationResult
{
    internal bool Success = false;
    internal string FullPath = "";
    internal TftpErrorCode ErrorCode = TftpErrorCode.AccessViolation;
    internal string ErrorMessage = "";
}

internal class TftpPathValidator
{
    internal string RootPath;
    internal string RootPrefix;
    internal bool AllowSubdirectories;
    internal bool RejectSymbolicLinks;
    internal StringComparison PathComparison;

    internal TftpPathValidator(TftpDirectoryStorageOptions options)
    {
        if (options == null)
        {
            throw new TftpStorageException(
                "The directory storage options cannot be null.",
                "ValidateRootPath",
                null);
        }

        if (options.RootPath == null || options.RootPath.Length == 0)
        {
            throw new TftpStorageException(
                "The directory storage root path cannot be empty.",
                "ValidateRootPath",
                null);
        }

        if (options.RootPath.IndexOf('\0') >= 0)
        {
            throw new TftpStorageException(
                "The directory storage root path cannot contain NUL.",
                "ValidateRootPath",
                null);
        }

        RootPath = Path.GetFullPath(options.RootPath);
        RootPrefix = RootPath;
        if (!RootPrefix.EndsWith(Path.DirectorySeparatorChar))
        {
            RootPrefix = RootPrefix + Path.DirectorySeparatorChar;
        }

        AllowSubdirectories = options.AllowSubdirectories;
        RejectSymbolicLinks = options.RejectSymbolicLinks;
        PathComparison = StringComparison.Ordinal;
        if (OperatingSystem.IsWindows())
        {
            PathComparison = StringComparison.OrdinalIgnoreCase;
        }
    }

    internal TftpPathValidationResult ValidatePath(string requestedPath)
    {
        if (requestedPath == null || requestedPath.Length == 0)
        {
            return CreateFailure(
                "The requested storage path cannot be empty.");
        }

        if (requestedPath.IndexOf('\0') >= 0)
        {
            return CreateFailure(
                "The requested storage path cannot contain NUL.");
        }

        if (IsPortableAbsolutePath(requestedPath))
        {
            return CreateFailure(
                "Absolute and device storage paths are not permitted.");
        }

        string normalizedPath = requestedPath.Replace(
            '\\',
            Path.DirectorySeparatorChar);
        normalizedPath = normalizedPath.Replace(
            '/',
            Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalizedPath))
        {
            return CreateFailure(
                "Absolute and device storage paths are not permitted.");
        }

        string[] components = normalizedPath.Split(
            Path.DirectorySeparatorChar);
        bool hasNamedComponent = false;
        int index = 0;
        while (index < components.Length)
        {
            string component = components[index];
            if (component.Length > 0)
            {
                string portableComponent = component.TrimEnd(' ');
                if (portableComponent == "." ||
                    portableComponent == ".." ||
                    IsOnlyDots(portableComponent))
                {
                    return CreateFailure(
                        "Storage path traversal is not permitted.");
                }

                if (IsDeviceComponent(portableComponent))
                {
                    return CreateFailure(
                        "Device storage paths are not permitted.");
                }

                hasNamedComponent = true;
            }

            index = index + 1;
        }

        if (!hasNamedComponent)
        {
            return CreateFailure(
                "The requested storage path must identify content.");
        }

        if (!AllowSubdirectories && components.Length > 1)
        {
            return CreateFailure(
                "Storage subdirectories are not permitted.");
        }

        string fullPath = Path.GetFullPath(normalizedPath, RootPath);
        if (!IsConfinedPath(fullPath))
        {
            return CreateFailure(
                "The requested storage path escapes the configured root.");
        }

        TftpPathValidationResult linkResult = ValidateLinkConfinement(
            components,
            fullPath);
        if (!linkResult.Success)
        {
            return linkResult;
        }

        TftpPathValidationResult success = new TftpPathValidationResult();
        success.Success = true;
        success.FullPath = fullPath;
        success.ErrorCode = TftpErrorCode.Undefined;
        return success;
    }

    internal bool IsPortableAbsolutePath(string requestedPath)
    {
        char firstCharacter = requestedPath[0];
        if (firstCharacter == '/' || firstCharacter == '\\')
        {
            return true;
        }

        if (requestedPath.Length >= 2 &&
            IsAsciiLetter(requestedPath[0]) &&
            requestedPath[1] == ':')
        {
            return true;
        }

        return false;
    }

    internal bool IsAsciiLetter(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return true;
        }

        return value >= 'a' && value <= 'z';
    }

    internal bool IsOnlyDots(string component)
    {
        if (component.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < component.Length)
        {
            if (component[index] != '.')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsDeviceComponent(string component)
    {
        if (component.IndexOf(':') >= 0)
        {
            return true;
        }

        string deviceCandidate = component.TrimEnd(' ', '.');
        int extensionIndex = deviceCandidate.IndexOf('.');
        if (extensionIndex >= 0)
        {
            deviceCandidate = deviceCandidate.Substring(0, extensionIndex);
        }

        if (string.Equals(
            deviceCandidate,
            "CON",
            StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                deviceCandidate,
                "PRN",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                deviceCandidate,
                "AUX",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                deviceCandidate,
                "NUL",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                deviceCandidate,
                "CONIN$",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                deviceCandidate,
                "CONOUT$",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                deviceCandidate,
                "CLOCK$",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (deviceCandidate.Length == 4)
        {
            string devicePrefix = deviceCandidate.Substring(0, 3);
            char deviceNumber = deviceCandidate[3];
            if ((string.Equals(
                    devicePrefix,
                    "COM",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    devicePrefix,
                    "LPT",
                    StringComparison.OrdinalIgnoreCase)) &&
                deviceNumber >= '1' && deviceNumber <= '9')
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsConfinedPath(string fullPath)
    {
        if (string.Equals(fullPath, RootPath, PathComparison))
        {
            return true;
        }

        return fullPath.StartsWith(RootPrefix, PathComparison);
    }

    internal TftpPathValidationResult ValidateLinkConfinement(
        string[] components,
        string fullPath)
    {
        string currentPath = RootPath;
        int index = 0;
        while (index < components.Length)
        {
            string component = components[index];
            if (component.Length > 0)
            {
                currentPath = Path.Combine(currentPath, component);
                FileSystemInfo pathInfo = new FileInfo(currentPath);
                string? linkTarget = pathInfo.LinkTarget;
                bool fileExists = File.Exists(currentPath);
                bool directoryExists = Directory.Exists(currentPath);
                if (linkTarget != null)
                {
                    if (RejectSymbolicLinks)
                    {
                        return CreateFailure(
                            "Symbolic links and reparse points are not permitted.");
                    }

                    string? linkDirectory = Path.GetDirectoryName(
                        currentPath);
                    if (linkDirectory == null)
                    {
                        return CreateFailure(
                            "The requested symbolic link target is invalid.");
                    }

                    string resolvedTargetPath = Path.GetFullPath(
                        linkTarget,
                        linkDirectory);
                    if (!IsConfinedPath(resolvedTargetPath))
                    {
                        return CreateFailure(
                            "The requested symbolic link escapes the configured root.");
                    }

                    if (fileExists || directoryExists)
                    {
                        FileSystemInfo? finalTarget =
                            pathInfo.ResolveLinkTarget(true);
                        if (finalTarget == null ||
                            !IsConfinedPath(Path.GetFullPath(
                                finalTarget.FullName)))
                        {
                            return CreateFailure(
                                "The requested symbolic link escapes the configured root.");
                        }
                    }
                }
                else if (!fileExists && !directoryExists)
                {
                    break;
                }
                else
                {
                    FileAttributes attributes = File.GetAttributes(
                        currentPath);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        return CreateFailure(
                            "The requested reparse point cannot be safely resolved.");
                    }
                }
            }

            index = index + 1;
        }

        TftpPathValidationResult success = new TftpPathValidationResult();
        success.Success = true;
        success.FullPath = fullPath;
        success.ErrorCode = TftpErrorCode.Undefined;
        return success;
    }

    internal TftpPathValidationResult CreateFailure(string message)
    {
        TftpPathValidationResult failure = new TftpPathValidationResult();
        failure.ErrorMessage = message;
        return failure;
    }
}
