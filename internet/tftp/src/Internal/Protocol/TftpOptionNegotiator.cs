using System;
using System.Collections.Generic;
using System.Globalization;

namespace tftp;

internal static class TftpOptionNegotiator
{
    internal static TftpOptionNegotiationResult CreateBlockSizeRequest(
        TftpTransferOptions transferOptions,
        int requestBaseLength)
    {
        if (transferOptions == null)
        {
            return CreateOptionFailure(
                "The transfer options cannot be null.",
                TftpProtocolValues.BlockSizeOption,
                "");
        }

        List<TftpOption> requestedOptions = new List<TftpOption>();
        if (transferOptions.RequestBlockSize)
        {
            if (transferOptions.BlockSize < TftpProtocolValues.MinimumBlockSize ||
                transferOptions.BlockSize > TftpProtocolValues.MaximumBlockSize)
            {
                return CreateOptionFailure(
                    "The requested block size is outside the RFC 2348 range.",
                    TftpProtocolValues.BlockSizeOption,
                    transferOptions.BlockSize.ToString(CultureInfo.InvariantCulture));
            }

            TftpOption option = new TftpOption();
            option.Name = TftpProtocolValues.BlockSizeOption;
            option.Value = transferOptions.BlockSize.ToString(CultureInfo.InvariantCulture);
            requestedOptions.Add(option);
        }

        return ValidateRequestOptions(requestedOptions, requestBaseLength);
    }

    internal static TftpOptionNegotiationResult CreateLegacyFallbackOptions()
    {
        TftpOptionNegotiationResult result = new TftpOptionNegotiationResult();
        result.Success = true;
        return result;
    }

    internal static TftpOptionNegotiationResult CreateTimeoutRequest(
        TftpTransferOptions transferOptions,
        int requestBaseLength)
    {
        if (transferOptions == null)
        {
            return CreateOptionFailure(
                "The transfer options cannot be null.",
                TftpProtocolValues.TimeoutOption,
                "");
        }

        List<TftpOption> requestedOptions = new List<TftpOption>();
        if (transferOptions.RequestTimeout)
        {
            if (transferOptions.TimeoutSeconds < TftpProtocolValues.MinimumTimeoutSeconds ||
                transferOptions.TimeoutSeconds > TftpProtocolValues.MaximumTimeoutSeconds)
            {
                return CreateOptionFailure(
                    "The requested timeout is outside the RFC 2349 range.",
                    TftpProtocolValues.TimeoutOption,
                    transferOptions.TimeoutSeconds.ToString(CultureInfo.InvariantCulture));
            }

            TftpOption option = new TftpOption();
            option.Name = TftpProtocolValues.TimeoutOption;
            option.Value = transferOptions.TimeoutSeconds.ToString(CultureInfo.InvariantCulture);
            requestedOptions.Add(option);
        }

        TftpOptionNegotiationResult result = ValidateRequestOptions(
            requestedOptions,
            requestBaseLength);
        if (result.Success)
        {
            result.EffectiveTimeoutSeconds = transferOptions.InitialTimeoutSeconds;
        }

        return result;
    }

    internal static TftpOptionNegotiationResult CreateLegacyTimeoutFallback(
        int localTimeoutSeconds)
    {
        if (localTimeoutSeconds < 1)
        {
            return CreateOptionFailure(
                "The local timeout interval must be positive.",
                TftpProtocolValues.TimeoutOption,
                localTimeoutSeconds.ToString(CultureInfo.InvariantCulture));
        }

        TftpOptionNegotiationResult result = CreateLegacyFallbackOptions();
        result.EffectiveTimeoutSeconds = localTimeoutSeconds;
        return result;
    }

    internal static TftpOptionNegotiationResult CreateTransferSizeRequest(
        TftpTransferOptions transferOptions,
        TftpTransferDirection direction,
        bool hasEncodedSize,
        long encodedSize,
        int requestBaseLength)
    {
        if (transferOptions == null)
        {
            return CreateOptionFailure(
                "The transfer options cannot be null.",
                TftpProtocolValues.TransferSizeOption,
                "");
        }

        if (direction != TftpTransferDirection.Read &&
            direction != TftpTransferDirection.Write)
        {
            return CreateOptionFailure(
                "The transfer direction is invalid for transfer-size negotiation.",
                TftpProtocolValues.TransferSizeOption,
                "");
        }

        List<TftpOption> requestedOptions = new List<TftpOption>();
        if (transferOptions.RequestTransferSize)
        {
            string transferSizeValue = "";
            bool includeOption = true;
            if (direction == TftpTransferDirection.Read)
            {
                transferSizeValue = "0";
            }
            else if (!hasEncodedSize)
            {
                includeOption = false;
            }
            else if (encodedSize < 0)
            {
                return CreateOptionFailure(
                    "The encoded transfer size cannot be negative.",
                    TftpProtocolValues.TransferSizeOption,
                    encodedSize.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                transferSizeValue = encodedSize.ToString(CultureInfo.InvariantCulture);
            }

            if (includeOption)
            {
                TftpOption option = new TftpOption();
                option.Name = TftpProtocolValues.TransferSizeOption;
                option.Value = transferSizeValue;
                requestedOptions.Add(option);
            }
        }

        return ValidateRequestOptions(requestedOptions, requestBaseLength);
    }

    internal static TftpOptionNegotiationResult CreateCombinedRequestOptions(
        TftpTransferOptions transferOptions,
        TftpTransferDirection direction,
        bool hasEncodedSize,
        long encodedSize,
        int requestBaseLength)
    {
        if (transferOptions == null)
        {
            return CreateOptionFailure(
                "The transfer options cannot be null.",
                "",
                "");
        }

        if (direction != TftpTransferDirection.Read &&
            direction != TftpTransferDirection.Write)
        {
            return CreateOptionFailure(
                "The transfer direction is invalid for option negotiation.",
                "",
                "");
        }

        List<TftpOption> requestedOptions = new List<TftpOption>();
        if (transferOptions.RequestBlockSize)
        {
            if (transferOptions.BlockSize < TftpProtocolValues.MinimumBlockSize ||
                transferOptions.BlockSize > TftpProtocolValues.MaximumBlockSize)
            {
                return CreateOptionFailure(
                    "The requested block size is outside the RFC 2348 range.",
                    TftpProtocolValues.BlockSizeOption,
                    transferOptions.BlockSize.ToString(CultureInfo.InvariantCulture));
            }

            TftpOption blockSizeOption = new TftpOption();
            blockSizeOption.Name = TftpProtocolValues.BlockSizeOption;
            blockSizeOption.Value = transferOptions.BlockSize.ToString(
                CultureInfo.InvariantCulture);
            requestedOptions.Add(blockSizeOption);
        }

        if (transferOptions.RequestTimeout)
        {
            if (transferOptions.TimeoutSeconds < TftpProtocolValues.MinimumTimeoutSeconds ||
                transferOptions.TimeoutSeconds > TftpProtocolValues.MaximumTimeoutSeconds)
            {
                return CreateOptionFailure(
                    "The requested timeout is outside the RFC 2349 range.",
                    TftpProtocolValues.TimeoutOption,
                    transferOptions.TimeoutSeconds.ToString(CultureInfo.InvariantCulture));
            }

            TftpOption timeoutOption = new TftpOption();
            timeoutOption.Name = TftpProtocolValues.TimeoutOption;
            timeoutOption.Value = transferOptions.TimeoutSeconds.ToString(
                CultureInfo.InvariantCulture);
            requestedOptions.Add(timeoutOption);
        }

        if (transferOptions.RequestTransferSize)
        {
            bool includeTransferSize = true;
            string transferSizeValue = "0";
            if (direction == TftpTransferDirection.Write)
            {
                if (!hasEncodedSize)
                {
                    includeTransferSize = false;
                }
                else if (encodedSize < 0)
                {
                    return CreateOptionFailure(
                        "The encoded transfer size cannot be negative.",
                        TftpProtocolValues.TransferSizeOption,
                        encodedSize.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    transferSizeValue = encodedSize.ToString(CultureInfo.InvariantCulture);
                }
            }

            if (includeTransferSize)
            {
                TftpOption transferSizeOption = new TftpOption();
                transferSizeOption.Name = TftpProtocolValues.TransferSizeOption;
                transferSizeOption.Value = transferSizeValue;
                requestedOptions.Add(transferSizeOption);
            }
        }

        TftpOptionNegotiationResult result = ValidateRequestOptions(
            requestedOptions,
            requestBaseLength);
        if (result.Success)
        {
            result.EffectiveTimeoutSeconds = transferOptions.InitialTimeoutSeconds;
        }

        return result;
    }

    internal static TftpOptionNegotiationResult ValidateRequestOptions(
        List<TftpOption> requestedOptions,
        int requestBaseLength)
    {
        return ValidateOptionCollection(
            requestedOptions,
            requestBaseLength,
            false,
            "request");
    }

    internal static TftpOptionNegotiationResult SelectServerOptions(
        List<TftpOption> requestedOptions,
        TftpServerOptions serverOptions,
        int requestBaseLength)
    {
        TftpOptionNegotiationResult requestResult = ValidateRequestOptions(
            requestedOptions,
            requestBaseLength);
        if (!requestResult.Success)
        {
            return requestResult;
        }

        if (serverOptions == null)
        {
            return CreateOptionFailure(
                "The server option policy cannot be null.",
                "",
                "");
        }

        TftpOptionNegotiationResult result = new TftpOptionNegotiationResult();
        result.Success = true;
        result.EncodedLength = requestResult.EncodedLength;
        result.EffectiveTimeoutSeconds = serverOptions.InitialTimeoutSeconds;
        foreach (TftpOption option in requestResult.Options)
        {
            bool accepted = false;
            if (string.Equals(
                option.Name,
                TftpProtocolValues.BlockSizeOption,
                StringComparison.OrdinalIgnoreCase))
            {
                accepted = serverOptions.EnableBlockSize;
                if (accepted)
                {
                    int requestedBlockSize = ParsePositiveDecimalOption(option.Value);
                    if (requestedBlockSize < TftpProtocolValues.MinimumBlockSize ||
                        requestedBlockSize > TftpProtocolValues.MaximumBlockSize)
                    {
                        return CreateOptionFailure(
                            "The requested block size is outside the RFC 2348 range.",
                            option.Name,
                            option.Value);
                    }

                    if (serverOptions.MaximumBlockSize < TftpProtocolValues.MinimumBlockSize ||
                        serverOptions.MaximumBlockSize > TftpProtocolValues.MaximumBlockSize)
                    {
                        return CreateOptionFailure(
                            "The server block-size limit is outside the RFC 2348 range.",
                            option.Name,
                            option.Value);
                    }

                    int effectiveBlockSize = requestedBlockSize;
                    if (effectiveBlockSize > serverOptions.MaximumBlockSize)
                    {
                        effectiveBlockSize = serverOptions.MaximumBlockSize;
                    }

                    TftpOption effectiveOption = new TftpOption();
                    effectiveOption.Name = option.Name;
                    effectiveOption.Value = effectiveBlockSize.ToString(
                        CultureInfo.InvariantCulture);
                    result.Options.Add(effectiveOption);
                    result.EffectiveBlockSize = effectiveBlockSize;
                    accepted = false;
                }
            }
            else if (string.Equals(
                option.Name,
                TftpProtocolValues.TimeoutOption,
                StringComparison.OrdinalIgnoreCase))
            {
                accepted = serverOptions.EnableTimeout;
                if (accepted)
                {
                    int requestedTimeout = ParsePositiveDecimalOption(option.Value);
                    if (requestedTimeout < TftpProtocolValues.MinimumTimeoutSeconds ||
                        requestedTimeout > TftpProtocolValues.MaximumTimeoutSeconds)
                    {
                        return CreateOptionFailure(
                            "The requested timeout is outside the RFC 2349 range.",
                            option.Name,
                            option.Value);
                    }

                    result.Options.Add(CopyOption(option));
                    result.EffectiveTimeoutSeconds = requestedTimeout;
                    accepted = false;
                }
            }
            else if (string.Equals(
                option.Name,
                TftpProtocolValues.TransferSizeOption,
                StringComparison.OrdinalIgnoreCase))
            {
                accepted = serverOptions.EnableTransferSize;
            }

            if (accepted)
            {
                result.Options.Add(CopyOption(option));
            }
        }

        return result;
    }

    internal static TftpOptionNegotiationResult ValidateAcknowledgedOptions(
        List<TftpOption> requestedOptions,
        List<TftpOption> acknowledgedOptions)
    {
        TftpOptionNegotiationResult requestResult = ValidateOptionCollection(
            requestedOptions,
            TftpProtocolValues.OpcodeLength,
            false,
            "request");
        if (!requestResult.Success)
        {
            return requestResult;
        }

        TftpOptionNegotiationResult acknowledgedResult = ValidateOptionCollection(
            acknowledgedOptions,
            TftpProtocolValues.OpcodeLength,
            true,
            "OACK");
        if (!acknowledgedResult.Success)
        {
            return acknowledgedResult;
        }

        TftpOption? requestedBlockSizeOption = FindOptionByName(
            requestResult.Options,
            TftpProtocolValues.BlockSizeOption);
        int requestedBlockSize = TftpProtocolValues.DefaultBlockSize;
        if (requestedBlockSizeOption != null)
        {
            requestedBlockSize = ParsePositiveDecimalOption(requestedBlockSizeOption.Value);
            if (requestedBlockSize < TftpProtocolValues.MinimumBlockSize ||
                requestedBlockSize > TftpProtocolValues.MaximumBlockSize)
            {
                return CreateOptionFailure(
                    "The requested block size is outside the RFC 2348 range.",
                    requestedBlockSizeOption.Name,
                    requestedBlockSizeOption.Value);
            }
        }

        foreach (TftpOption acknowledgedOption in acknowledgedResult.Options)
        {
            TftpOption? requestedOption = FindOptionByName(
                requestResult.Options,
                acknowledgedOption.Name);
            if (requestedOption == null)
            {
                return CreateOptionFailure(
                    "The OACK packet contains an option that was not requested.",
                    acknowledgedOption.Name,
                    acknowledgedOption.Value);
            }

            if (string.Equals(
                acknowledgedOption.Name,
                TftpProtocolValues.BlockSizeOption,
                StringComparison.OrdinalIgnoreCase))
            {
                int acknowledgedBlockSize = ParsePositiveDecimalOption(
                    acknowledgedOption.Value);
                if (acknowledgedBlockSize < TftpProtocolValues.MinimumBlockSize ||
                    acknowledgedBlockSize > TftpProtocolValues.MaximumBlockSize)
                {
                    return CreateOptionFailure(
                        "The acknowledged block size is outside the RFC 2348 range.",
                        acknowledgedOption.Name,
                        acknowledgedOption.Value);
                }

                if (acknowledgedBlockSize > requestedBlockSize)
                {
                    return CreateOptionFailure(
                        "The acknowledged block size exceeds the requested value.",
                        acknowledgedOption.Name,
                        acknowledgedOption.Value);
                }

                acknowledgedResult.EffectiveBlockSize = acknowledgedBlockSize;
            }
        }

        return acknowledgedResult;
    }

    internal static TftpOptionNegotiationResult ValidateAcknowledgedTimeoutOptions(
        List<TftpOption> requestedOptions,
        List<TftpOption> acknowledgedOptions,
        int localTimeoutSeconds)
    {
        TftpOptionNegotiationResult result = ValidateAcknowledgedOptions(
            requestedOptions,
            acknowledgedOptions);
        if (!result.Success)
        {
            return result;
        }

        if (localTimeoutSeconds < 1)
        {
            return CreateOptionFailure(
                "The local timeout interval must be positive.",
                TftpProtocolValues.TimeoutOption,
                localTimeoutSeconds.ToString(CultureInfo.InvariantCulture));
        }

        result.EffectiveTimeoutSeconds = localTimeoutSeconds;
        TftpOption? requestedTimeoutOption = FindOptionByName(
            requestedOptions,
            TftpProtocolValues.TimeoutOption);
        if (requestedTimeoutOption == null)
        {
            return result;
        }

        int requestedTimeout = ParsePositiveDecimalOption(requestedTimeoutOption.Value);
        if (requestedTimeout < TftpProtocolValues.MinimumTimeoutSeconds ||
            requestedTimeout > TftpProtocolValues.MaximumTimeoutSeconds)
        {
            return CreateOptionFailure(
                "The requested timeout is outside the RFC 2349 range.",
                requestedTimeoutOption.Name,
                requestedTimeoutOption.Value);
        }

        TftpOption? acknowledgedTimeoutOption = FindOptionByName(
            acknowledgedOptions,
            TftpProtocolValues.TimeoutOption);
        if (acknowledgedTimeoutOption == null)
        {
            return result;
        }

        int acknowledgedTimeout = ParsePositiveDecimalOption(acknowledgedTimeoutOption.Value);
        if (acknowledgedTimeout < TftpProtocolValues.MinimumTimeoutSeconds ||
            acknowledgedTimeout > TftpProtocolValues.MaximumTimeoutSeconds)
        {
            return CreateOptionFailure(
                "The acknowledged timeout is outside the RFC 2349 range.",
                acknowledgedTimeoutOption.Name,
                acknowledgedTimeoutOption.Value);
        }

        if (!string.Equals(
            acknowledgedTimeoutOption.Value,
            requestedTimeoutOption.Value,
            StringComparison.Ordinal))
        {
            return CreateOptionFailure(
                "The acknowledged timeout does not exactly match the requested value.",
                acknowledgedTimeoutOption.Name,
                acknowledgedTimeoutOption.Value);
        }

        result.EffectiveTimeoutSeconds = acknowledgedTimeout;
        return result;
    }

    internal static TftpOptionNegotiationResult SelectServerTransferSizeOptions(
        List<TftpOption> requestedOptions,
        TftpServerOptions serverOptions,
        int requestBaseLength,
        TftpTransferDirection direction,
        bool hasEncodedReadSize,
        long encodedReadSize)
    {
        TftpOptionNegotiationResult selected = SelectServerOptions(
            requestedOptions,
            serverOptions,
            requestBaseLength);
        if (!selected.Success)
        {
            return selected;
        }

        if (direction != TftpTransferDirection.Read &&
            direction != TftpTransferDirection.Write)
        {
            return CreateOptionFailure(
                "The transfer direction is invalid for transfer-size negotiation.",
                TftpProtocolValues.TransferSizeOption,
                "");
        }

        TftpOptionNegotiationResult result = new TftpOptionNegotiationResult();
        result.Success = true;
        result.EncodedLength = selected.EncodedLength;
        result.EffectiveBlockSize = selected.EffectiveBlockSize;
        result.EffectiveTimeoutSeconds = selected.EffectiveTimeoutSeconds;
        foreach (TftpOption option in selected.Options)
        {
            if (!string.Equals(
                option.Name,
                TftpProtocolValues.TransferSizeOption,
                StringComparison.OrdinalIgnoreCase))
            {
                result.Options.Add(CopyOption(option));
                continue;
            }

            if (direction == TftpTransferDirection.Read)
            {
                if (!string.Equals(option.Value, "0", StringComparison.Ordinal))
                {
                    return CreateOptionFailure(
                        "An RRQ transfer-size request must contain exactly zero.",
                        option.Name,
                        option.Value);
                }

                if (!hasEncodedReadSize)
                {
                    continue;
                }

                if (encodedReadSize < 0)
                {
                    return CreateOptionFailure(
                        "The encoded read size cannot be negative.",
                        option.Name,
                        encodedReadSize.ToString(CultureInfo.InvariantCulture));
                }

                if (serverOptions.MaximumTransferSize < 0 ||
                    encodedReadSize > serverOptions.MaximumTransferSize)
                {
                    return CreateCapacityFailure(
                        "The requested read exceeds the server transfer capacity.",
                        option.Name,
                        encodedReadSize.ToString(CultureInfo.InvariantCulture));
                }

                TftpOption readSizeOption = new TftpOption();
                readSizeOption.Name = option.Name;
                readSizeOption.Value = encodedReadSize.ToString(CultureInfo.InvariantCulture);
                result.Options.Add(readSizeOption);
                result.HasTransferSize = true;
                result.TransferSize = encodedReadSize;
                continue;
            }

            long announcedWriteSize = ParseNonnegativeDecimalOption(option.Value);
            if (announcedWriteSize < 0)
            {
                return CreateOptionFailure(
                    "The announced write size is not a nonnegative 64-bit decimal value.",
                    option.Name,
                    option.Value);
            }

            if (serverOptions.MaximumTransferSize < 0 ||
                announcedWriteSize > serverOptions.MaximumTransferSize)
            {
                return CreateCapacityFailure(
                    "The announced write exceeds the server transfer capacity.",
                    option.Name,
                    option.Value);
            }

            result.Options.Add(CopyOption(option));
            result.HasTransferSize = true;
            result.TransferSize = announcedWriteSize;
        }

        return result;
    }

    internal static TftpOptionNegotiationResult ValidateAcknowledgedTransferSizeOptions(
        List<TftpOption> requestedOptions,
        List<TftpOption> acknowledgedOptions,
        TftpTransferDirection direction,
        long maximumTransferSize)
    {
        TftpOptionNegotiationResult result = ValidateAcknowledgedOptions(
            requestedOptions,
            acknowledgedOptions);
        if (!result.Success)
        {
            return result;
        }

        if (direction != TftpTransferDirection.Read &&
            direction != TftpTransferDirection.Write)
        {
            return CreateOptionFailure(
                "The transfer direction is invalid for transfer-size negotiation.",
                TftpProtocolValues.TransferSizeOption,
                "");
        }

        TftpOption? requestedTransferSize = FindOptionByName(
            requestedOptions,
            TftpProtocolValues.TransferSizeOption);
        if (requestedTransferSize == null)
        {
            return result;
        }

        long requestedSize = ParseNonnegativeDecimalOption(requestedTransferSize.Value);
        if (requestedSize < 0)
        {
            return CreateOptionFailure(
                "The requested transfer size is not a nonnegative 64-bit decimal value.",
                requestedTransferSize.Name,
                requestedTransferSize.Value);
        }

        if (direction == TftpTransferDirection.Read &&
            !string.Equals(requestedTransferSize.Value, "0", StringComparison.Ordinal))
        {
            return CreateOptionFailure(
                "An RRQ transfer-size request must contain exactly zero.",
                requestedTransferSize.Name,
                requestedTransferSize.Value);
        }

        TftpOption? acknowledgedTransferSize = FindOptionByName(
            acknowledgedOptions,
            TftpProtocolValues.TransferSizeOption);
        if (acknowledgedTransferSize == null)
        {
            return result;
        }

        long acknowledgedSize = ParseNonnegativeDecimalOption(acknowledgedTransferSize.Value);
        if (acknowledgedSize < 0)
        {
            return CreateOptionFailure(
                "The acknowledged transfer size is not a nonnegative 64-bit decimal value.",
                acknowledgedTransferSize.Name,
                acknowledgedTransferSize.Value);
        }

        if (direction == TftpTransferDirection.Write &&
            !string.Equals(
                acknowledgedTransferSize.Value,
                requestedTransferSize.Value,
                StringComparison.Ordinal))
        {
            return CreateOptionFailure(
                "The acknowledged write size does not exactly match the requested value.",
                acknowledgedTransferSize.Name,
                acknowledgedTransferSize.Value);
        }

        if (maximumTransferSize < 0 || acknowledgedSize > maximumTransferSize)
        {
            return CreateCapacityFailure(
                "The acknowledged transfer size exceeds the local transfer capacity.",
                acknowledgedTransferSize.Name,
                acknowledgedTransferSize.Value);
        }

        result.HasTransferSize = true;
        result.TransferSize = acknowledgedSize;
        return result;
    }

    internal static TftpOptionNegotiationResult ValidateCombinedAcknowledgedOptions(
        List<TftpOption> requestedOptions,
        List<TftpOption> acknowledgedOptions,
        TftpTransferDirection direction,
        int localTimeoutSeconds,
        long maximumTransferSize)
    {
        TftpOptionNegotiationResult timeoutResult = ValidateAcknowledgedTimeoutOptions(
            requestedOptions,
            acknowledgedOptions,
            localTimeoutSeconds);
        if (!timeoutResult.Success)
        {
            return timeoutResult;
        }

        TftpOptionNegotiationResult transferSizeResult =
            ValidateAcknowledgedTransferSizeOptions(
                requestedOptions,
                acknowledgedOptions,
                direction,
                maximumTransferSize);
        if (!transferSizeResult.Success)
        {
            return transferSizeResult;
        }

        timeoutResult.HasTransferSize = transferSizeResult.HasTransferSize;
        timeoutResult.TransferSize = transferSizeResult.TransferSize;
        return timeoutResult;
    }

    internal static bool CanRetryRequestWithoutOptions(
        TftpTransferOptions transferOptions,
        List<TftpOption> requestedOptions,
        bool retryAlreadyAttempted,
        TftpErrorCode errorCode)
    {
        if (transferOptions == null || requestedOptions == null)
        {
            return false;
        }

        if (!transferOptions.RetryWithoutOptions ||
            requestedOptions.Count == 0 ||
            retryAlreadyAttempted)
        {
            return false;
        }

        return errorCode == TftpErrorCode.IllegalOperation ||
            errorCode == TftpErrorCode.OptionNegotiationFailed;
    }

    internal static int ParsePositiveDecimalOption(string value)
    {
        if (value == null || value.Length == 0)
        {
            return -1;
        }

        int parsedValue = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            int digit = character - '0';
            if (parsedValue > (int.MaxValue - digit) / 10)
            {
                return -1;
            }

            parsedValue = (parsedValue * 10) + digit;
            index = index + 1;
        }

        return parsedValue;
    }

    internal static long ParseNonnegativeDecimalOption(string value)
    {
        if (value == null || value.Length == 0)
        {
            return -1;
        }

        long parsedValue = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            int digit = character - '0';
            if (parsedValue > (long.MaxValue - digit) / 10)
            {
                return -1;
            }

            parsedValue = (parsedValue * 10) + digit;
            index = index + 1;
        }

        return parsedValue;
    }

    internal static TftpPacket CreateOptionErrorPacket(
        TftpOptionNegotiationResult failure)
    {
        if (failure == null || failure.Success || !failure.HasError)
        {
            throw new TftpOptionNegotiationException(
                "An option-negotiation failure is required to create an ERROR packet.",
                "",
                "");
        }

        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = failure.ErrorCode;
        packet.ErrorMessage = failure.FailureMessage;
        return packet;
    }

    internal static TftpOptionNegotiationResult ValidateOptionCollection(
        List<TftpOption> options,
        int initialEncodedLength,
        bool requireOption,
        string packetName)
    {
        if (initialEncodedLength < TftpProtocolValues.OpcodeLength ||
            initialEncodedLength > TftpProtocolValues.MaximumRequestPacketLength)
        {
            return CreateOptionFailure(
                "The option packet base length is outside the protocol range.",
                "",
                "");
        }

        if (options == null)
        {
            return CreateOptionFailure(
                "The " + packetName + " option list cannot be null.",
                "",
                "");
        }

        if (requireOption && options.Count == 0)
        {
            return CreateOptionFailure(
                "The " + packetName + " packet must contain at least one option.",
                "",
                "");
        }

        TftpOptionNegotiationResult result = new TftpOptionNegotiationResult();
        result.EncodedLength = initialEncodedLength;
        HashSet<string> optionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (TftpOption option in options)
        {
            if (option == null)
            {
                return CreateOptionFailure(
                    "The " + packetName + " packet cannot contain a null option.",
                    "",
                    "");
            }

            string nameFailure = ValidateOptionText(option.Name, "name");
            if (nameFailure.Length != 0)
            {
                return CreateOptionFailure(
                    nameFailure,
                    GetAvailableOptionName(option),
                    GetAvailableOptionValue(option));
            }

            string valueFailure = ValidateOptionText(option.Value, "value");
            if (valueFailure.Length != 0)
            {
                return CreateOptionFailure(
                    valueFailure,
                    GetAvailableOptionName(option),
                    GetAvailableOptionValue(option));
            }

            if (!optionNames.Add(option.Name))
            {
                return CreateOptionFailure(
                    "The " + packetName + " packet cannot contain duplicate option names.",
                    option.Name,
                    option.Value);
            }

            int availableLength = TftpProtocolValues.MaximumRequestPacketLength -
                result.EncodedLength;
            if (option.Name.Length >= availableLength)
            {
                return CreateOptionFailure(
                    "The " + packetName + " packet exceeds the 512-byte maximum length.",
                    option.Name,
                    option.Value);
            }

            result.EncodedLength = result.EncodedLength + option.Name.Length + 1;
            availableLength = TftpProtocolValues.MaximumRequestPacketLength -
                result.EncodedLength;
            if (option.Value.Length >= availableLength)
            {
                return CreateOptionFailure(
                    "The " + packetName + " packet exceeds the 512-byte maximum length.",
                    option.Name,
                    option.Value);
            }

            result.EncodedLength = result.EncodedLength + option.Value.Length + 1;
            result.Options.Add(CopyOption(option));
        }

        result.Success = true;
        return result;
    }

    internal static string ValidateOptionText(string value, string fieldName)
    {
        if (value == null)
        {
            return "The option " + fieldName + " cannot be null.";
        }

        if (value.Length == 0)
        {
            return "The option " + fieldName + " cannot be empty.";
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == 0)
            {
                return "The option " + fieldName + " cannot contain an embedded NUL.";
            }

            if (character > 127)
            {
                return "The option " + fieldName + " must contain only ASCII characters.";
            }

            index = index + 1;
        }

        return "";
    }

    internal static TftpOption? FindOptionByName(
        List<TftpOption> options,
        string optionName)
    {
        foreach (TftpOption option in options)
        {
            if (string.Equals(option.Name, optionName, StringComparison.OrdinalIgnoreCase))
            {
                return option;
            }
        }

        return null;
    }

    internal static TftpOption CopyOption(TftpOption source)
    {
        TftpOption copy = new TftpOption();
        copy.Name = source.Name;
        copy.Value = source.Value;
        return copy;
    }

    internal static TftpOptionNegotiationResult CreateOptionFailure(
        string message,
        string optionName,
        string optionValue)
    {
        TftpOptionNegotiationResult result = new TftpOptionNegotiationResult();
        result.HasError = true;
        result.ErrorCode = TftpErrorCode.OptionNegotiationFailed;
        result.FailureMessage = message;
        result.OptionName = optionName;
        result.OptionValue = optionValue;
        return result;
    }

    internal static TftpOptionNegotiationResult CreateCapacityFailure(
        string message,
        string optionName,
        string optionValue)
    {
        TftpOptionNegotiationResult result = new TftpOptionNegotiationResult();
        result.HasError = true;
        result.ErrorCode = TftpErrorCode.StorageExhausted;
        result.FailureMessage = message;
        result.OptionName = optionName;
        result.OptionValue = optionValue;
        return result;
    }

    internal static string GetAvailableOptionName(TftpOption option)
    {
        if (option.Name == null)
        {
            return "";
        }

        return option.Name;
    }

    internal static string GetAvailableOptionValue(TftpOption option)
    {
        if (option.Value == null)
        {
            return "";
        }

        return option.Value;
    }
}
