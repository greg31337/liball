namespace qrcode;

internal class StructuredAppendSplitter
{
    internal static int CalculateParity(byte[] message)
    {
        if (message == null)
        {
            throw new QrInputException("The structured-append message cannot be null.", "message");
        }

        int parity = 0;
        for (int index = 0; index < message.Length; index++)
        {
            parity ^= message[index];
        }

        return parity;
    }

    internal static QrSymbolSequence SplitBytes(byte[] message, QrEncodeOptions options)
    {
        ValidateInputs(message, options);
        int[][] candidates = GetPartCandidates(options);
        int maximumPartByteCount = -1;
        if (candidates[0].Length > 0)
        {
            maximumPartByteCount = candidates[0][candidates[0].Length - 1];
        }

        int symbolCount = GetSymbolCount(
            message.Length,
            maximumPartByteCount,
            options.MaximumStructuredAppendSymbols);
        int[] partByteCounts = GetOptimizedPartByteCounts(
            message.Length,
            symbolCount,
            candidates);
        int parity = CalculateParity(message);
        QrSymbol[] symbols = new QrSymbol[symbolCount];
        int sourceOffset = 0;
        for (int index = 0; index < symbolCount; index++)
        {
            int partByteCount = partByteCounts[index];
            byte[] part = new byte[partByteCount];
            for (int byteIndex = 0; byteIndex < partByteCount; byteIndex++)
            {
                part[byteIndex] = message[sourceOffset + byteIndex];
            }

            sourceOffset += partByteCount;
            QrStructuredAppendInfo info = new QrStructuredAppendInfo
            {
                Index = index,
                Total = symbolCount,
                Parity = parity,
                IsComplete = true
            };
            QrSegment header = new QrSegment
            {
                Mode = QrEncodingMode.StructuredAppend,
                StructuredAppend = info
            };
            int segmentCount = GetRequiredSegmentCount(options);
            QrSegment[] segments = new QrSegment[segmentCount];
            int segmentIndex = 0;
            segments[segmentIndex] = header;
            segmentIndex++;
            if (options.UseFnc1First)
            {
                segments[segmentIndex] = Fnc1Encoder.CreateFirstPositionControlSegment();
                segmentIndex++;
            }
            else if (options.Fnc1SecondApplicationIndicator >= 0)
            {
                segments[segmentIndex] = Fnc1Encoder.CreateSecondPositionControlSegment(
                    options.Fnc1SecondApplicationIndicator);
                segmentIndex++;
            }

            if (options.EciAssignment >= 0)
            {
                segments[segmentIndex] = EciEncoder.CreateControlSegment(
                    options.EciAssignment);
                segmentIndex++;
            }

            QrSegment payload = new QrSegment
            {
                Mode = QrEncodingMode.Byte,
                CharacterCount = part.Length,
                Data = part,
                EciAssignment = options.EciAssignment
            };
            segments[segmentIndex] = payload;
            symbols[index] = Model2Encoder.EncodeSegments(segments, options);
        }

        byte[] rawBytes = new byte[message.Length];
        for (int index = 0; index < message.Length; index++)
        {
            rawBytes[index] = message[index];
        }

        return new QrSymbolSequence
        {
            Symbols = symbols,
            StructuredAppend = new QrStructuredAppendInfo
            {
                Index = 0,
                Total = symbolCount,
                Parity = parity,
                IsComplete = true
            },
            RawBytes = rawBytes,
            IsComplete = true,
            FailureReason = QrFailureReason.None
        };
    }

    internal static QrSymbolSequence EncodeManualParts(
        QrSegment[][] parts,
        QrEncodeOptions options)
    {
        ValidateManualPartCollection(parts, options);
        QrStructuredAppendInfo commonInfo = GetManualCommonInfo(parts, options);
        int payloadByteCount = GetManualPayloadByteCount(parts, options.MaximumInputBytes);
        byte[] rawBytes = new byte[payloadByteCount];
        string combinedText = "";
        bool hasCompleteText = true;
        int rawIndex = 0;
        int calculatedParity = 0;
        for (int partIndex = 0; partIndex < parts.Length; partIndex++)
        {
            QrSegment[] part = parts[partIndex];
            for (int segmentIndex = 1; segmentIndex < part.Length; segmentIndex++)
            {
                QrSegment segment = part[segmentIndex];
                if (!IsDataMode(segment.Mode))
                {
                    continue;
                }

                byte[] payload = GetManualPayloadBytes(segment);
                for (int byteIndex = 0; byteIndex < payload.Length; byteIndex++)
                {
                    byte value = payload[byteIndex];
                    rawBytes[rawIndex] = value;
                    rawIndex++;
                    calculatedParity ^= value;
                }

                if (segment.Text == null)
                {
                    hasCompleteText = false;
                }
                else if (hasCompleteText)
                {
                    combinedText += segment.Text;
                }
            }
        }

        if (calculatedParity != commonInfo.Parity)
        {
            throw new QrStructuredAppendException(
                "The manual structured-append parity does not match the combined payload.",
                QrFailureReason.ConflictingSequence,
                commonInfo.Total,
                parts.Length,
                commonInfo.Parity);
        }

        QrSymbol[] symbols = new QrSymbol[parts.Length];
        for (int index = 0; index < parts.Length; index++)
        {
            symbols[index] = Model2Encoder.EncodeSegments(
                CopyManualPart(parts[index], commonInfo, index),
                options);
        }

        string? text = combinedText;
        if (!hasCompleteText)
        {
            text = null;
        }

        return new QrSymbolSequence
        {
            Symbols = symbols,
            StructuredAppend = new QrStructuredAppendInfo
            {
                Index = 0,
                Total = commonInfo.Total,
                Parity = commonInfo.Parity,
                ApplicationIdentifier = commonInfo.ApplicationIdentifier,
                IsComplete = true
            },
            RawBytes = rawBytes,
            Text = text,
            IsComplete = true,
            FailureReason = QrFailureReason.None
        };
    }

    static void ValidateInputs(byte[] message, QrEncodeOptions options)
    {
        if (message == null)
        {
            throw new QrInputException("The structured-append message cannot be null.", "message");
        }

        if (options == null)
        {
            throw new QrInputException("The encode options are required.", "options");
        }

        if (!options.AllowModel2)
        {
            throw new QrInputException(
                "Structured append requires Model 2 encoding.",
                "AllowModel2");
        }

        if (options.ForceSymbolKind && options.SymbolKind != QrSymbolKind.Model2)
        {
            throw new QrInputException(
                "Structured append requires the Model 2 symbol kind.",
                "SymbolKind");
        }

        if (options.MinimumVersion < 0)
        {
            throw new QrInputException(
                "The minimum version cannot be negative.",
                "MinimumVersion");
        }

        if (options.MaximumVersion <= 0
            || options.MinimumVersion > options.MaximumVersion)
        {
            throw new QrInputException(
                "The numbered version range is invalid.",
                "MaximumVersion");
        }

        if (options.MinimumWidth < 0
            || options.MaximumWidth < 0
            || options.MinimumHeight < 0
            || options.MaximumHeight < 0)
        {
            throw new QrInputException(
                "Symbol dimension bounds cannot be negative.",
                "MinimumWidth");
        }

        if ((options.MaximumWidth != 0
                && options.MinimumWidth > options.MaximumWidth)
            || (options.MaximumHeight != 0
                && options.MinimumHeight > options.MaximumHeight))
        {
            throw new QrInputException(
                "The symbol dimension range is invalid.",
                "MaximumWidth");
        }

        int errorCorrectionLevel = (int)options.ErrorCorrectionLevel;
        if (errorCorrectionLevel < EccBlockTable.Low
            || errorCorrectionLevel > EccBlockTable.High)
        {
            throw new QrInputException(
                "Model 2 requires a defined error-correction level.",
                "ErrorCorrectionLevel");
        }

        int mask = (int)options.MaskPattern;
        if (mask < (int)QrMaskPattern.Automatic || mask > (int)QrMaskPattern.Seven)
        {
            throw new QrInputException(
                "The requested Model 2 mask is not defined.",
                "MaskPattern");
        }

        if (options.QuietZoneModules < -1)
        {
            throw new QrInputException(
                "The quiet-zone width must be nonnegative or -1 for the profile default.",
                "QuietZoneModules");
        }

        int requiredSegmentCount = GetRequiredSegmentCount(options);
        if (options.MaximumSegments < requiredSegmentCount)
        {
            throw new QrResourceLimitException(
                "The structured-append controls exceed the configured segment limit.",
                "MaximumSegments",
                requiredSegmentCount,
                options.MaximumSegments);
        }

        if (options.MaximumInputBytes < 0)
        {
            throw new QrInputException(
                "The maximum input byte count cannot be negative.",
                "MaximumInputBytes");
        }

        if (message.Length > options.MaximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The structured-append message exceeds the configured input byte limit.",
                "MaximumInputBytes",
                message.Length,
                options.MaximumInputBytes);
        }

        if (options.MaximumStructuredAppendSymbols < QrLimits.StructuredAppendMinimumSymbols
            || options.MaximumStructuredAppendSymbols > QrLimits.StructuredAppendMaximumSymbols)
        {
            throw new QrInputException(
                "The structured-append symbol limit must be from 2 through 16.",
                "MaximumStructuredAppendSymbols");
        }

        if (options.EciAssignment < -1)
        {
            throw new QrInputException(
                "The ECI assignment sentinel is invalid.",
                "EciAssignment");
        }

        if (options.EciAssignment >= 0)
        {
            EciEncoder.ValidateAssignment(options.EciAssignment);
        }

        if (options.Fnc1SecondApplicationIndicator < -1)
        {
            throw new QrInputException(
                "The FNC1 second-position sentinel is invalid.",
                "Fnc1SecondApplicationIndicator");
        }

        if (options.UseFnc1First && options.Fnc1SecondApplicationIndicator >= 0)
        {
            throw new QrInputException(
                "FNC1 first and second position cannot both be requested.",
                "Fnc1SecondApplicationIndicator");
        }

        if (options.Fnc1SecondApplicationIndicator >= 0)
        {
            Fnc1Encoder.ValidateSecondPositionIndicator(
                options.Fnc1SecondApplicationIndicator);
        }
    }

    static int GetRequiredSegmentCount(QrEncodeOptions options)
    {
        int count = 2;
        if (options.UseFnc1First || options.Fnc1SecondApplicationIndicator >= 0)
        {
            count++;
        }

        if (options.EciAssignment >= 0)
        {
            count++;
        }

        return count;
    }

    static int[][] GetPartCandidates(QrEncodeOptions options)
    {
        int minimumVersion = options.MinimumVersion;
        if (minimumVersion < QrLimits.Model2MinimumVersion)
        {
            minimumVersion = QrLimits.Model2MinimumVersion;
        }

        int maximumVersion = options.MaximumVersion;
        if (maximumVersion > QrLimits.Model2MaximumVersion)
        {
            maximumVersion = QrLimits.Model2MaximumVersion;
        }

        int[] capacities = new int[QrLimits.Model2MaximumVersion];
        int[] dimensions = new int[QrLimits.Model2MaximumVersion];
        int[] areas = new int[QrLimits.Model2MaximumVersion];
        int candidateCount = 0;
        int errorCorrectionLevel = (int)options.ErrorCorrectionLevel;
        for (int version = minimumVersion; version <= maximumVersion; version++)
        {
            int dimension = Model2VersionTable.GetDimension(version);
            if (!AreDimensionsAllowed(dimension, options))
            {
                continue;
            }

            int capacityBitCount = CheckedDimensions.CalculateProduct(
                EccBlockTable.GetDataCodewordCount(version, errorCorrectionLevel),
                8,
                int.MaxValue,
                "CapacityBitCount");
            int countBitWidth = Model2VersionTable.GetCharacterCountBitWidth(2, version);
            int fixedBitCount = 24 + countBitWidth;
            if (options.UseFnc1First)
            {
                fixedBitCount += 4;
            }
            else if (options.Fnc1SecondApplicationIndicator >= 0)
            {
                fixedBitCount += 12;
            }

            if (options.EciAssignment >= 0)
            {
                fixedBitCount += 4 + EciEncoder.CalculateBitLength(options.EciAssignment);
            }

            int payloadBitCount = capacityBitCount - fixedBitCount;
            if (payloadBitCount < 0)
            {
                continue;
            }

            int partByteCount = payloadBitCount / 8;
            int maximumCharacterCount = (1 << countBitWidth) - 1;
            if (partByteCount > maximumCharacterCount)
            {
                partByteCount = maximumCharacterCount;
            }

            if (candidateCount > 0 && partByteCount <= capacities[candidateCount - 1])
            {
                continue;
            }

            capacities[candidateCount] = partByteCount;
            dimensions[candidateCount] = dimension;
            areas[candidateCount] = dimension * dimension;
            candidateCount++;
        }

        int[] selectedCapacities = new int[candidateCount];
        int[] selectedDimensions = new int[candidateCount];
        int[] selectedAreas = new int[candidateCount];
        for (int index = 0; index < candidateCount; index++)
        {
            selectedCapacities[index] = capacities[index];
            selectedDimensions[index] = dimensions[index];
            selectedAreas[index] = areas[index];
        }

        return new int[][] { selectedCapacities, selectedDimensions, selectedAreas };
    }

    static bool AreDimensionsAllowed(int dimension, QrEncodeOptions options)
    {
        if (dimension < options.MinimumWidth || dimension < options.MinimumHeight)
        {
            return false;
        }

        if (options.MaximumWidth != 0 && dimension > options.MaximumWidth)
        {
            return false;
        }

        return options.MaximumHeight == 0 || dimension <= options.MaximumHeight;
    }

    static int GetSymbolCount(
        int messageByteCount,
        int maximumPartByteCount,
        int maximumSymbolCount)
    {
        if (maximumPartByteCount < 0
            || (maximumPartByteCount == 0 && messageByteCount > 0))
        {
            throw new QrCapacityException(
                "No permitted Model 2 symbol can contain a structured-append part.",
                QrSymbolKind.Model2,
                CheckedDimensions.CalculateProduct(
                    messageByteCount,
                    8,
                    int.MaxValue,
                    "RequiredBitCount"),
                0);
        }

        int symbolCount = QrLimits.StructuredAppendMinimumSymbols;
        if (messageByteCount > 0)
        {
            symbolCount = (messageByteCount - 1) / maximumPartByteCount + 1;
            if (symbolCount < QrLimits.StructuredAppendMinimumSymbols)
            {
                symbolCount = QrLimits.StructuredAppendMinimumSymbols;
            }
        }

        if (symbolCount > maximumSymbolCount)
        {
            throw new QrCapacityException(
                "The message requires more structured-append symbols than permitted.",
                QrSymbolKind.Model2,
                CheckedDimensions.CalculateProduct(
                    messageByteCount,
                    8,
                    int.MaxValue,
                    "RequiredBitCount"),
                CheckedDimensions.CalculateProduct(
                    maximumPartByteCount,
                    maximumSymbolCount * 8,
                    int.MaxValue,
                    "AvailableBitCount"));
        }

        return symbolCount;
    }

    static int[] GetOptimizedPartByteCounts(
        int messageByteCount,
        int symbolCount,
        int[][] candidates)
    {
        int[] balanced = CreateBalancedPartByteCounts(messageByteCount, symbolCount);
        if (messageByteCount == 0 || candidates[0].Length == 0)
        {
            return balanced;
        }

        int balancedArea = CalculatePartitionArea(balanced, candidates);
        int boundaryCount = messageByteCount + 1;
        int[][] bestAreas = new int[symbolCount][];
        int[][] bestMaximumDimensions = new int[symbolCount][];
        int[][] previousSums = new int[symbolCount][];
        int[][] selectedCandidates = new int[symbolCount][];
        for (int part = 0; part < symbolCount; part++)
        {
            bestAreas[part] = new int[boundaryCount];
            bestMaximumDimensions[part] = new int[boundaryCount];
            previousSums[part] = new int[boundaryCount];
            selectedCandidates[part] = new int[boundaryCount];
            for (int sum = 0; sum < boundaryCount; sum++)
            {
                bestAreas[part][sum] = int.MaxValue;
                bestMaximumDimensions[part][sum] = int.MaxValue;
                previousSums[part][sum] = -1;
                selectedCandidates[part][sum] = -1;
            }
        }

        bestAreas[0][0] = 0;
        bestMaximumDimensions[0][0] = 0;
        for (int part = 1; part < symbolCount; part++)
        {
            for (int sum = 0; sum <= messageByteCount; sum++)
            {
                if (bestAreas[part - 1][sum] == int.MaxValue)
                {
                    continue;
                }

                for (int candidate = 0; candidate < candidates[0].Length; candidate++)
                {
                    int nextSum = sum + candidates[0][candidate];
                    if (nextSum > messageByteCount)
                    {
                        continue;
                    }

                    int area = bestAreas[part - 1][sum] + candidates[2][candidate];
                    int maximumDimension = bestMaximumDimensions[part - 1][sum];
                    if (candidates[1][candidate] > maximumDimension)
                    {
                        maximumDimension = candidates[1][candidate];
                    }

                    if (area < bestAreas[part][nextSum]
                        || area == bestAreas[part][nextSum]
                            && maximumDimension < bestMaximumDimensions[part][nextSum])
                    {
                        bestAreas[part][nextSum] = area;
                        bestMaximumDimensions[part][nextSum] = maximumDimension;
                        previousSums[part][nextSum] = sum;
                        selectedCandidates[part][nextSum] = candidate;
                    }
                }
            }
        }

        int selectedSum = -1;
        int selectedArea = int.MaxValue;
        int selectedMaximumDimension = int.MaxValue;
        int selectedUnusedCapacity = int.MaxValue;
        for (int sum = 0; sum <= messageByteCount; sum++)
        {
            if (bestAreas[symbolCount - 1][sum] == int.MaxValue)
            {
                continue;
            }

            int remaining = messageByteCount - sum;
            int finalCandidate = FindCandidateForLength(remaining, candidates[0]);
            if (finalCandidate < 0)
            {
                continue;
            }

            int area = bestAreas[symbolCount - 1][sum]
                + candidates[2][finalCandidate];
            int maximumDimension = bestMaximumDimensions[symbolCount - 1][sum];
            if (candidates[1][finalCandidate] > maximumDimension)
            {
                maximumDimension = candidates[1][finalCandidate];
            }

            int unusedCapacity = candidates[0][finalCandidate] - remaining;
            if (area < selectedArea
                || area == selectedArea && maximumDimension < selectedMaximumDimension
                || area == selectedArea
                    && maximumDimension == selectedMaximumDimension
                    && unusedCapacity < selectedUnusedCapacity)
            {
                selectedSum = sum;
                selectedArea = area;
                selectedMaximumDimension = maximumDimension;
                selectedUnusedCapacity = unusedCapacity;
            }
        }

        if (selectedSum < 0 || selectedArea >= balancedArea)
        {
            return balanced;
        }

        int[] optimized = new int[symbolCount];
        optimized[symbolCount - 1] = messageByteCount - selectedSum;
        int currentSum = selectedSum;
        for (int part = symbolCount - 1; part > 0; part--)
        {
            int candidate = selectedCandidates[part][currentSum];
            optimized[part - 1] = candidates[0][candidate];
            currentSum = previousSums[part][currentSum];
        }

        return optimized;
    }

    static int[] CreateBalancedPartByteCounts(int messageByteCount, int symbolCount)
    {
        int[] counts = new int[symbolCount];
        int basePartByteCount = messageByteCount / symbolCount;
        int largerPartCount = messageByteCount % symbolCount;
        for (int index = 0; index < symbolCount; index++)
        {
            counts[index] = basePartByteCount;
            if (index < largerPartCount)
            {
                counts[index]++;
            }
        }

        return counts;
    }

    static int CalculatePartitionArea(int[] partByteCounts, int[][] candidates)
    {
        int area = 0;
        for (int index = 0; index < partByteCounts.Length; index++)
        {
            int candidate = FindCandidateForLength(partByteCounts[index], candidates[0]);
            if (candidate < 0)
            {
                return int.MaxValue;
            }

            area += candidates[2][candidate];
        }

        return area;
    }

    static int FindCandidateForLength(int byteCount, int[] capacities)
    {
        for (int index = 0; index < capacities.Length; index++)
        {
            if (byteCount <= capacities[index])
            {
                return index;
            }
        }

        return -1;
    }

    static void ValidateManualPartCollection(
        QrSegment[][] parts,
        QrEncodeOptions options)
    {
        if (parts == null)
        {
            throw new QrInputException(
                "The manual structured-append parts cannot be null.",
                "parts");
        }

        if (options == null)
        {
            throw new QrInputException("The encode options are required.", "options");
        }

        if (parts.Length < QrLimits.StructuredAppendMinimumSymbols
            || parts.Length > QrLimits.StructuredAppendMaximumSymbols)
        {
            throw new QrInputException(
                "A manual structured-append sequence must contain from 2 through 16 parts.",
                "parts");
        }

        if (options.MaximumStructuredAppendSymbols < QrLimits.StructuredAppendMinimumSymbols
            || options.MaximumStructuredAppendSymbols > QrLimits.StructuredAppendMaximumSymbols)
        {
            throw new QrInputException(
                "The structured-append symbol limit must be from 2 through 16.",
                "MaximumStructuredAppendSymbols");
        }

        if (parts.Length > options.MaximumStructuredAppendSymbols)
        {
            throw new QrResourceLimitException(
                "The manual sequence exceeds the configured structured-append symbol limit.",
                "MaximumStructuredAppendSymbols",
                parts.Length,
                options.MaximumStructuredAppendSymbols);
        }
    }

    static QrStructuredAppendInfo GetManualCommonInfo(
        QrSegment[][] parts,
        QrEncodeOptions options)
    {
        QrStructuredAppendInfo? commonInfo = null;
        for (int index = 0; index < parts.Length; index++)
        {
            QrSegment[] part = parts[index];
            if (part == null)
            {
                throw new QrInputException(
                    "The manual structured-append collection contains a null part.",
                    "parts");
            }

            SegmentValidator.ValidateSegments(
                part,
                QrSymbolKind.Model2,
                QrLimits.Model2MaximumVersion,
                options.MaximumSegments,
                options.MaximumInputBytes,
                options.EnableHanziCompatibility);
            if (part.Length == 0
                || part[0].Mode != QrEncodingMode.StructuredAppend)
            {
                throw new QrInputException(
                    "Every manual part must begin with a structured-append header.",
                    "parts");
            }

            QrStructuredAppendInfo? candidateInfo = part[0].StructuredAppend;
            if (candidateInfo == null)
            {
                throw new QrInputException(
                    "Every manual header must contain structured-append metadata.",
                    "parts");
            }

            QrStructuredAppendInfo info = candidateInfo;
            if (commonInfo == null)
            {
                commonInfo = info;
            }
            else
            {
                ValidateManualIdentity(commonInfo, info, parts.Length);
            }

            if (info.Index != index)
            {
                throw new QrStructuredAppendException(
                    "A manual structured-append header is not in its declared position.",
                    QrFailureReason.ConflictingSequence,
                    info.Total,
                    parts.Length,
                    info.Parity);
            }
        }

        if (commonInfo == null)
        {
            throw new QrInputException(
                "The manual sequence does not contain structured-append metadata.",
                "parts");
        }

        QrStructuredAppendInfo selected = commonInfo;
        if (selected.Total != parts.Length)
        {
            QrFailureReason reason = QrFailureReason.ConflictingSequence;
            if (selected.Total > parts.Length)
            {
                reason = QrFailureReason.IncompleteSequence;
            }

            throw new QrStructuredAppendException(
                "The number of manual parts does not match the declared total.",
                reason,
                selected.Total,
                parts.Length,
                selected.Parity);
        }

        return selected;
    }

    static void ValidateManualIdentity(
        QrStructuredAppendInfo expected,
        QrStructuredAppendInfo actual,
        int actualPartCount)
    {
        int parity = expected.Parity;
        if (actual.Parity != expected.Parity)
        {
            parity = -1;
        }

        if (actual.Total != expected.Total
            || actual.Parity != expected.Parity
            || actual.ApplicationIdentifier != expected.ApplicationIdentifier)
        {
            throw new QrStructuredAppendException(
                "The manual structured-append headers do not identify one common message.",
                QrFailureReason.ConflictingSequence,
                expected.Total,
                actualPartCount,
                parity);
        }
    }

    static int GetManualPayloadByteCount(
        QrSegment[][] parts,
        int maximumInputBytes)
    {
        int byteCount = 0;
        for (int partIndex = 0; partIndex < parts.Length; partIndex++)
        {
            QrSegment[] part = parts[partIndex];
            for (int segmentIndex = 1; segmentIndex < part.Length; segmentIndex++)
            {
                QrSegment segment = part[segmentIndex];
                if (IsDataMode(segment.Mode))
                {
                    byteCount = CheckedDimensions.CalculateSum(
                        byteCount,
                        GetManualPayloadLength(segment),
                        maximumInputBytes,
                        "MaximumInputBytes");
                }
            }
        }

        return byteCount;
    }

    static int GetManualPayloadLength(QrSegment segment)
    {
        if (segment.Mode == QrEncodingMode.Byte || segment.Text == null)
        {
            return segment.Data.Length;
        }

        int bytesPerCharacter = 1;
        if (segment.Mode == QrEncodingMode.Kanji || segment.Mode == QrEncodingMode.Hanzi)
        {
            bytesPerCharacter = 2;
        }

        return CheckedDimensions.CalculateProduct(
            segment.Text.Length,
            bytesPerCharacter,
            int.MaxValue,
            "MaximumInputBytes");
    }

    static byte[] GetManualPayloadBytes(QrSegment segment)
    {
        if (segment.Mode == QrEncodingMode.Byte || segment.Text == null)
        {
            return segment.Data;
        }

        // Text modes encode Text, so optional retained Data cannot determine parity.
        int assignment = QrEciAssignments.Iso8859_1;
        if (segment.Mode == QrEncodingMode.Kanji)
        {
            assignment = QrEciAssignments.ShiftJis;
        }
        else if (segment.Mode == QrEncodingMode.Hanzi)
        {
            assignment = QrEciAssignments.Gb2312;
        }

        return EciTextTranscoder.EncodeText(segment.Text, assignment);
    }

    static bool IsDataMode(QrEncodingMode mode)
    {
        return mode == QrEncodingMode.Numeric
            || mode == QrEncodingMode.Alphanumeric
            || mode == QrEncodingMode.Byte
            || mode == QrEncodingMode.Kanji
            || mode == QrEncodingMode.Hanzi;
    }

    static QrSegment[] CopyManualPart(
        QrSegment[] source,
        QrStructuredAppendInfo commonInfo,
        int index)
    {
        QrSegment[] copy = new QrSegment[source.Length];
        copy[0] = new QrSegment
        {
            Mode = QrEncodingMode.StructuredAppend,
            StructuredAppend = new QrStructuredAppendInfo
            {
                Index = index,
                Total = commonInfo.Total,
                Parity = commonInfo.Parity,
                ApplicationIdentifier = commonInfo.ApplicationIdentifier,
                IsComplete = true
            }
        };
        for (int segmentIndex = 1; segmentIndex < source.Length; segmentIndex++)
        {
            copy[segmentIndex] = source[segmentIndex];
        }

        return copy;
    }
}
