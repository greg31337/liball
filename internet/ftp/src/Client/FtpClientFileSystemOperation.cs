using System;
using System.Collections.Generic;

namespace ftp;

internal class FtpClientFileSystemOperation
{
    internal FtpClientState ClientState;

    internal FtpClientFileSystemOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for file-system operations.",
                null);
        }
        ClientState = clientState;
    }

    internal FtpCommandResult CreateDirectory(string path)
    {
        return ExecutePathCommand(FtpCommandKind.Mkd, path, "remote directory");
    }

    internal FtpCommandResult RemoveDirectory(string path)
    {
        return ExecutePathCommand(FtpCommandKind.Rmd, path, "remote directory");
    }

    internal FtpCommandResult DeleteFile(string path)
    {
        return ExecutePathCommand(FtpCommandKind.Dele, path, "remote file");
    }

    internal FtpCommandResult[] RenamePath(
        string sourcePath,
        string destinationPath)
    {
        FtpTransferValidator.ValidateRequiredPath(sourcePath, "rename source");
        FtpTransferValidator.ValidateRequiredPath(destinationPath, "rename destination");
        ClientState.RequireOpenSession("RenamePath");

        List<FtpCommandResult> results = new List<FtpCommandResult>();
        FtpCommandResult source = Execute(FtpCommandKind.Rnfr, sourcePath);
        results.Add(source);
        if (source.FinalReply == null ||
            source.FinalReply.Code != FtpReplyCodes.RequestedFileActionPending)
        {
            return results.ToArray();
        }

        FtpReplyExtractor.ExtractRenameReadiness(source.FinalReply);
        results.Add(Execute(FtpCommandKind.Rnto, destinationPath));
        return results.ToArray();
    }

    internal FtpCommandResult[] CreateParentDirectories(string path)
    {
        FtpTransferValidator.ValidateRequiredPath(path, "remote parent directory");
        ClientState.RequireOpenSession("CreateParentDirectories");

        List<FtpCommandResult> results = new List<FtpCommandResult>();
        FtpCommandResult pwd = Execute(FtpCommandKind.Pwd, null);
        results.Add(pwd);
        if (!pwd.Succeeded || pwd.FinalReply == null)
        {
            return results.ToArray();
        }

        string originalDirectory = FtpReplyExtractor.ExtractCreatedPath(pwd.FinalReply);
        string[] directories = BuildDirectoryPaths(originalDirectory, path);
        int index = 0;
        while (index < directories.Length)
        {
            string directory = directories[index];
            FtpCommandResult probe = Execute(FtpCommandKind.Cwd, directory);
            results.Add(probe);
            if (probe.Succeeded)
            {
                RestoreDirectory(originalDirectory, results);
                index++;
                continue;
            }
            if (probe.ConnectionClosed)
            {
                break;
            }

            FtpCommandResult creation = Execute(FtpCommandKind.Mkd, directory);
            results.Add(creation);
            if (creation.Succeeded)
            {
                index++;
                continue;
            }
            if (creation.ConnectionClosed)
            {
                break;
            }

            FtpCommandResult collisionProbe = Execute(FtpCommandKind.Cwd, directory);
            results.Add(collisionProbe);
            if (!collisionProbe.Succeeded)
            {
                break;
            }
            RestoreDirectory(originalDirectory, results);
            index++;
        }
        return results.ToArray();
    }

    internal void RestoreDirectory(
        string originalDirectory,
        List<FtpCommandResult> results)
    {
        FtpFaultIsolationResult restoration = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return Execute(FtpCommandKind.Cwd, originalDirectory);
        });
        FtpCommandResult? restored = restoration.Value as FtpCommandResult;
        if (restored != null)
        {
            results.Add(restored);
        }
        if (restoration.Succeeded && restored != null && restored.Succeeded)
        {
            return;
        }

        FtpFaultIsolationRunner.RunOperation(() =>
        {
            ClientState.CloseSession();
            return null;
        });
        throw new FtpConnectionException(
            "The FTP parent-directory operation could not restore its working directory.",
            restoration.Failure);
    }

    internal FtpCommandResult ExecutePathCommand(
        FtpCommandKind kind,
        string path,
        string pathDescription)
    {
        FtpTransferValidator.ValidateRequiredPath(path, pathDescription);
        ClientState.RequireOpenSession(kind.ToString());
        return Execute(kind, path);
    }

    internal FtpCommandResult Execute(FtpCommandKind kind, string? argument)
    {
        FtpClientCommandExecutor executor = new FtpClientCommandExecutor(ClientState);
        FtpCommandResult result = executor.ExecuteRawCommand(
            executor.CreateSimpleCommand(kind, argument));
        if (!result.ConnectionClosed)
        {
            ClientState.UpdateReconnectState();
        }
        return result;
    }

    internal static string[] BuildDirectoryPaths(
        string currentDirectory,
        string path)
    {
        string resolved = FtpPathCodec.ResolveWorkingPath(currentDirectory, path);
        if (resolved == "/")
        {
            return new string[0];
        }

        string[] parts = resolved.Split('/');
        int firstNewPart = 1;
        if (path[0] != '/')
        {
            string[] currentParts = currentDirectory.Split('/');
            while (firstNewPart < parts.Length &&
                firstNewPart < currentParts.Length &&
                string.Equals(
                    parts[firstNewPart],
                    currentParts[firstNewPart],
                    StringComparison.Ordinal))
            {
                firstNewPart++;
            }
        }

        List<string> directories = new List<string>();
        int index = firstNewPart;
        while (index < parts.Length)
        {
            if (parts[index].Length != 0)
            {
                string[] prefix = new string[index];
                int prefixIndex = 1;
                while (prefixIndex <= index)
                {
                    prefix[prefixIndex - 1] = parts[prefixIndex];
                    prefixIndex++;
                }
                directories.Add("/" + string.Join("/", prefix));
            }
            index++;
        }
        return directories.ToArray();
    }
}
