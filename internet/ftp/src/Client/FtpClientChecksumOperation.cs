using System;
using System.Collections.Generic;
using System.IO;

namespace ftp;

internal class FtpClientChecksumOperation
{
    internal FtpClientState ClientState;

    internal FtpClientChecksumOperation(FtpClientState? clientState)
    {
        if (clientState == null || clientState.Options == null ||
            clientState.Connection == null)
        {
            throw new FtpConnectionException(
                "An open configured FTP connection is required for checksum verification.",
                null);
        }

        ClientState = clientState;
    }

    internal FtpTransferResult VerifyTransfer(FtpTransferRequest? request)
    {
        FtpTransferValidator.ValidateTransferRequest(request);
        if (request == null)
        {
            throw new FtpConfigurationException(
                "A checksum verification request is required.", null);
        }
        if (request.Options.VerificationMode != FtpVerificationMode.Checksum)
        {
            throw new FtpConfigurationException(
                "Remote checksum verification requires checksum verification mode.",
                null);
        }

        ClientState.RequireOpenSession("VerifyTransfer");
        Stream? local = request.Content;
        bool closeLocal = request.Content != null && !request.LeaveContentOpen;
        if (local == null && request.LocalPath != null)
        {
            FtpFaultIsolationResult opened = FtpFaultIsolationRunner.RunOperation(() =>
            {
                return new FileStream(
                    request.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            });
            local = opened.Value as Stream;
            closeLocal = true;
            if (!opened.Succeeded || local == null)
            {
                throw new FtpTransferException(
                    "The local checksum source could not be opened.", opened.Failure);
            }
        }

        FtpTransferResult result = new FtpTransferResult();
        result.Request = request;
        result.Succeeded = true;
        result.VerificationMode = FtpVerificationMode.Checksum;
        result.Progress.Direction = request.Direction;
        result.Progress.Path = request.LocalPath == null ?
            request.RemotePath : request.LocalPath;
        result.Progress.Completed = true;
        List<FtpCommandResult> commands = new List<FtpCommandResult>();
        FtpFaultIsolationResult operation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            VerifyChecksum(request, request.RemotePath, local, result, commands);
            return null;
        });
        FtpFaultIsolationResult cleanup = new FtpFaultIsolationResult();
        cleanup.Succeeded = true;
        if (closeLocal && local != null)
        {
            cleanup = new FtpResourceLifetime(local).DisposeResource();
        }
        if (!operation.Succeeded)
        {
            throw new FtpTransferException(
                "Remote checksum verification did not complete.", operation.Failure);
        }
        if (!cleanup.Succeeded)
        {
            throw new FtpTransferException(
                "The local checksum source could not be closed.", cleanup.Failure);
        }

        result.Commands = commands.ToArray();
        return result;
    }

    internal bool VerifyChecksum(
        FtpTransferRequest request,
        string remotePath,
        Stream? local,
        FtpTransferResult result,
        List<FtpCommandResult> commands)
    {
        result.VerificationAttempted = true;
        string algorithm = TransferHash.NormalizeAlgorithm(
            request.Options.ExpectedChecksumAlgorithm);
        string? expected = TransferHash.NormalizeChecksum(
            request.Options.ExpectedChecksum, algorithm);
        result.ChecksumAlgorithm = algorithm;
        result.ExpectedChecksum = expected;
        if (expected == null)
        {
            throw new FtpConfigurationException(
                "Checksum verification requires valid expected hexadecimal text.",
                null);
        }
        if (local == null || !local.CanRead || !local.CanSeek)
        {
            return FailVerification(
                result,
                "Checksum verification requires a readable, seekable local source.");
        }

        string localChecksum = CalculateLocalChecksum(request, local, algorithm);
        bool remoteAvailable = RetrieveRemoteChecksum(
            remotePath, algorithm, result, commands);
        if (!remoteAvailable)
        {
            return FailVerification(
                result,
                "The FTP server could not provide the requested remote checksum.");
        }
        if (!string.Equals(localChecksum, expected, StringComparison.Ordinal))
        {
            return FailVerification(
                result,
                "The local content checksum does not match the expected checksum.");
        }
        if (!string.Equals(result.ActualChecksum, localChecksum,
            StringComparison.Ordinal))
        {
            return FailVerification(
                result,
                "The remote checksum does not match the local content checksum.");
        }

        result.Verified = true;
        return true;
    }

    internal string CalculateLocalChecksum(
        FtpTransferRequest request,
        Stream local,
        string algorithm)
    {
        long position = local.Position;
        local.Seek(0, SeekOrigin.Begin);
        FtpTransferHashRequest hashRequest = new FtpTransferHashRequest();
        hashRequest.Source = local;
        hashRequest.Algorithm = algorithm;
        hashRequest.BufferBytes = request.Options.BufferBytes;
        hashRequest.CancellationToken =
            request.Options.Timeouts.CancellationToken;
        hashRequest.DeadlineMilliseconds =
            request.Options.Timeouts.IdleTimeoutMilliseconds;
        hashRequest.LeaveOpen = true;
        FtpFaultIsolationResult calculation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return TransferHash.Calculate(hashRequest);
        });
        FtpFaultIsolationResult restore = FtpFaultIsolationRunner.RunOperation(() =>
        {
            local.Seek(position, SeekOrigin.Begin);
            return null;
        });
        if (!calculation.Succeeded)
        {
            throw new FtpTransferException(
                "The local checksum could not be calculated.", calculation.Failure);
        }
        if (!restore.Succeeded)
        {
            throw new FtpTransferException(
                "The local checksum source position could not be restored.",
                restore.Failure);
        }
        FtpTransferHashResult? hash = calculation.Value as FtpTransferHashResult;
        if (hash == null)
        {
            throw new FtpTransferException(
                "The local checksum calculation did not produce a result.", null);
        }
        return hash.Checksum;
    }

    internal bool RetrieveRemoteChecksum(
        string remotePath,
        string algorithm,
        FtpTransferResult result,
        List<FtpCommandResult> commands)
    {
        if (CanUseHash(algorithm))
        {
            FtpCommandResult option = ExecuteSimple(
                FtpCommandKind.Opts, FtpCommandNames.Hash + " " + algorithm);
            commands.Add(option);
            if (option.ConnectionClosed)
            {
                return false;
            }

            FtpCommandResult hash = ExecuteSimple(FtpCommandKind.Hash, remotePath);
            commands.Add(hash);
            string? checksum = ExtractChecksum(hash, algorithm);
            if (checksum != null)
            {
                result.ActualChecksum = checksum;
                return true;
            }
            if (hash.ConnectionClosed)
            {
                return false;
            }
        }

        FtpCommandKind[] vendorCommands = GetVendorCommands(algorithm);
        int index = 0;
        while (index < vendorCommands.Length)
        {
            FtpCommandResult vendor = ExecuteSimple(
                vendorCommands[index], remotePath);
            commands.Add(vendor);
            string? checksum = ExtractChecksum(vendor, algorithm);
            if (checksum != null)
            {
                result.ActualChecksum = checksum;
                return true;
            }
            if (vendor.ConnectionClosed)
            {
                return false;
            }
            index++;
        }
        return false;
    }

    internal bool CanUseHash(string algorithm)
    {
        if (!ClientState.Features.Negotiated)
        {
            return true;
        }
        if (!ClientState.FeatureState.Hash)
        {
            return false;
        }

        FtpFeature[] features = ClientState.Features.Features;
        int index = 0;
        while (index < features.Length)
        {
            FtpFeature feature = features[index];
            if (string.Equals(feature.Name, FtpFeatureNames.Hash,
                StringComparison.OrdinalIgnoreCase))
            {
                return SupportsHashAlgorithm(feature.Parameters, algorithm);
            }
            index++;
        }
        return true;
    }

    internal bool SupportsHashAlgorithm(string? parameters, string algorithm)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return true;
        }

        string[] names = parameters.Split(
            new char[] { ' ', '\t', ';', ',' },
            StringSplitOptions.RemoveEmptyEntries);
        bool foundAlgorithm = false;
        int index = 0;
        while (index < names.Length)
        {
            string name = names[index].TrimEnd('*');
            string? normalized = TransferHash.TryNormalizeAlgorithm(name);
            if (normalized != null)
            {
                foundAlgorithm = true;
                if (normalized == algorithm)
                {
                    return true;
                }
            }
            index++;
        }
        return !foundAlgorithm;
    }

    internal FtpCommandKind[] GetVendorCommands(string algorithm)
    {
        if (algorithm == "MD5")
        {
            return new FtpCommandKind[]
            {
                FtpCommandKind.Md5,
                FtpCommandKind.Xmd5,
            };
        }
        if (algorithm == "SHA-1")
        {
            return new FtpCommandKind[] { FtpCommandKind.Xsha1 };
        }
        if (algorithm == "SHA-256")
        {
            return new FtpCommandKind[] { FtpCommandKind.Xsha256 };
        }
        if (algorithm == "SHA-512")
        {
            return new FtpCommandKind[] { FtpCommandKind.Xsha512 };
        }
        return new FtpCommandKind[] { FtpCommandKind.Xcrc };
    }

    internal string? ExtractChecksum(
        FtpCommandResult command,
        string algorithm)
    {
        return FtpChecksumExtensions.ExtractReplyChecksum(
            command.Command.Kind, command.FinalReply, algorithm);
    }

    internal bool FailVerification(
        FtpTransferResult result,
        string message)
    {
        result.Succeeded = false;
        result.Verified = false;
        result.FailureMessage = message;
        return false;
    }

    internal FtpCommandResult ExecuteSimple(
        FtpCommandKind kind,
        string argument)
    {
        FtpClientCommandExecutor executor =
            new FtpClientCommandExecutor(ClientState);
        FtpCommandResult result = executor.ExecuteRawCommand(
            executor.CreateSimpleCommand(kind, argument));
        if (!result.ConnectionClosed)
        {
            ClientState.UpdateReconnectState();
        }
        return result;
    }
}
