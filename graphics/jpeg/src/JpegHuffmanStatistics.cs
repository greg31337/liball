namespace jpeg;

internal class JpegHuffmanStatistics
{
    internal int TableClass;
    internal int Identifier;
    internal int[] Counts = new int[256];
    internal int TotalCount;
}

internal class JpegHuffmanStatisticsCollector
{
    internal int[] DcPredictors = new int[256];

    internal JpegHuffmanStatistics CreateStatistics(int tableClass, int identifier)
    {
        ValidateTableClass(tableClass);
        ValidateIdentifier(identifier);

        JpegHuffmanStatistics statistics = new JpegHuffmanStatistics();
        statistics.TableClass = tableClass;
        statistics.Identifier = identifier;
        return statistics;
    }

    internal void CollectBlock(int[] coefficients, int componentIndex, JpegHuffmanStatistics dcStatistics, JpegHuffmanStatistics acStatistics)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateComponentIndex(componentIndex);
        ValidateStatistics(dcStatistics, 0);
        ValidateStatistics(acStatistics, 1);

        CollectDcCoefficient(coefficients[0], componentIndex, dcStatistics);
        CollectAcCoefficients(coefficients, 1, JpegConstants.BlockSampleCount - 1, acStatistics);
    }

    internal void CollectDcCoefficient(int coefficient, int componentIndex, JpegHuffmanStatistics dcStatistics)
    {
        ValidateComponentIndex(componentIndex);
        ValidateStatistics(dcStatistics, 0);

        long wideDifference = (long)coefficient - (long)DcPredictors[componentIndex];
        if (wideDifference < int.MinValue || wideDifference > int.MaxValue)
        {
            throw JpegWriteValidationException.Create("DC coefficient difference is outside the supported integer range.");
        }

        int difference = (int)wideDifference;
        int category = GetMagnitudeCategory(difference);
        ValidateDcCategory(category);
        IncrementSymbol(dcStatistics, category);
        DcPredictors[componentIndex] = coefficient;
    }

    internal void CollectAcCoefficients(int[] coefficients, int spectralStart, int spectralEnd, JpegHuffmanStatistics acStatistics)
    {
        ValidateCoefficientBlock(coefficients);
        ValidateAcSpectralRange(spectralStart, spectralEnd);
        ValidateStatistics(acStatistics, 1);

        int zeroRun = 0;
        int index = spectralStart;
        while (index <= spectralEnd)
        {
            int coefficient = coefficients[index];
            if (coefficient == 0)
            {
                zeroRun = zeroRun + 1;
            }
            else
            {
                while (zeroRun > 15)
                {
                    IncrementSymbol(acStatistics, 0xF0);
                    zeroRun = zeroRun - 16;
                }

                int category = GetMagnitudeCategory(coefficient);
                ValidateAcCategory(category);
                IncrementSymbol(acStatistics, (zeroRun << 4) + category);
                zeroRun = 0;
            }

            index = index + 1;
        }

        if (zeroRun > 0)
        {
            IncrementSymbol(acStatistics, 0);
        }
    }

    internal void IncrementSymbol(JpegHuffmanStatistics statistics, int symbol)
    {
        if (statistics == null)
        {
            throw new System.ArgumentNullException("statistics");
        }

        if (symbol < 0 || symbol > 255)
        {
            throw new System.ArgumentOutOfRangeException("symbol");
        }

        statistics.Counts[symbol] = statistics.Counts[symbol] + 1;
        statistics.TotalCount = statistics.TotalCount + 1;
    }

    internal int GetMagnitudeCategory(int value)
    {
        if (value == 0)
        {
            return 0;
        }

        long magnitude = value;
        if (magnitude < 0)
        {
            magnitude = -magnitude;
        }

        int category = 0;
        while (magnitude > 0)
        {
            category = category + 1;
            magnitude = magnitude >> 1;
        }

        return category;
    }

    internal void ResetDcPredictors()
    {
        int index = 0;
        while (index < DcPredictors.Length)
        {
            DcPredictors[index] = 0;
            index = index + 1;
        }
    }

    internal void ValidateStatistics(JpegHuffmanStatistics statistics, int tableClass)
    {
        if (statistics == null)
        {
            throw new System.ArgumentNullException("statistics");
        }

        ValidateTableClass(statistics.TableClass);
        if (statistics.TableClass != tableClass)
        {
            throw JpegWriteValidationException.Create("Huffman statistics table class does not match the coding context.");
        }

        ValidateIdentifier(statistics.Identifier);
        if (statistics.Counts == null || statistics.Counts.Length < 256)
        {
            throw JpegWriteValidationException.Create("Huffman statistics count array must contain 256 entries.");
        }
    }

    internal void ValidateCoefficientBlock(int[] coefficients)
    {
        if (coefficients == null)
        {
            throw new System.ArgumentNullException("coefficients");
        }

        if (coefficients.Length < JpegConstants.BlockSampleCount)
        {
            throw JpegWriteValidationException.Create("Coefficient block must contain 64 entries.");
        }
    }

    internal void ValidateAcSpectralRange(int spectralStart, int spectralEnd)
    {
        if (spectralStart < 1 || spectralStart >= JpegConstants.BlockSampleCount)
        {
            throw new System.ArgumentOutOfRangeException("spectralStart");
        }

        if (spectralEnd < spectralStart || spectralEnd >= JpegConstants.BlockSampleCount)
        {
            throw new System.ArgumentOutOfRangeException("spectralEnd");
        }
    }

    internal void ValidateComponentIndex(int componentIndex)
    {
        if (componentIndex < 0 || componentIndex >= DcPredictors.Length)
        {
            throw new System.ArgumentOutOfRangeException("componentIndex");
        }
    }

    internal void ValidateDcCategory(int category)
    {
        if (category < 0 || category > 11)
        {
            throw JpegWriteValidationException.Create("DC coefficient difference exceeds the baseline Huffman category range.");
        }
    }

    internal void ValidateAcCategory(int category)
    {
        if (category < 1 || category > 10)
        {
            throw JpegWriteValidationException.Create("AC coefficient exceeds the baseline Huffman category range.");
        }
    }

    internal void ValidateTableClass(int tableClass)
    {
        if (tableClass != 0 && tableClass != 1)
        {
            throw JpegWriteValidationException.Create("Huffman statistics table class must be DC or AC.");
        }
    }

    internal void ValidateIdentifier(int identifier)
    {
        if (identifier < 0 || identifier > 3)
        {
            throw JpegWriteValidationException.Create("Huffman statistics table identifier must be between 0 and 3.");
        }
    }
}

internal class JpegHuffmanTableGenerator
{
    internal JpegHuffmanTable GenerateTable(JpegHuffmanStatistics statistics)
    {
        ValidateStatistics(statistics);

        int[] symbols = new int[257];
        long[] frequencies = new long[257];
        long[] frequenciesBySymbol = new long[257];
        int symbolCount = CopyActiveSymbols(statistics, symbols, frequencies, frequenciesBySymbol);
        if (symbolCount == 0)
        {
            throw JpegWriteValidationException.Create("Huffman statistics must contain at least one used symbol.");
        }

        symbols[symbolCount] = 256;
        frequencies[symbolCount] = 1;
        frequenciesBySymbol[256] = 1;
        symbolCount = symbolCount + 1;

        int[] codeLengths = BuildCodeLengths(symbols, frequencies, symbolCount);
        int[] lengthCounts = CountCodeLengths(codeLengths);
        LimitCodeLengths(lengthCounts);
        int[] finalLengths = AssignLimitedCodeLengths(symbols, frequenciesBySymbol, symbolCount, lengthCounts);
        int[] orderedSymbols = SortSymbolsForTable(symbols, finalLengths, frequenciesBySymbol, symbolCount);

        return CreateTable(statistics, finalLengths, orderedSymbols, symbolCount);
    }

    internal int CopyActiveSymbols(JpegHuffmanStatistics statistics, int[] symbols, long[] frequencies, long[] frequenciesBySymbol)
    {
        int symbolCount = 0;
        int symbol = 0;
        while (symbol < 256)
        {
            int count = statistics.Counts[symbol];
            if (count < 0)
            {
                throw JpegWriteValidationException.Create("Huffman statistics counts cannot be negative.");
            }

            if (count > 0)
            {
                symbols[symbolCount] = symbol;
                frequencies[symbolCount] = count;
                frequenciesBySymbol[symbol] = count;
                symbolCount = symbolCount + 1;
            }

            symbol = symbol + 1;
        }

        return symbolCount;
    }

    internal int[] BuildCodeLengths(int[] symbols, long[] frequencies, int symbolCount)
    {
        int nodeCapacity = (symbolCount * 2) + 1;
        long[] nodeFrequencies = new long[nodeCapacity];
        int[] parent = new int[nodeCapacity];
        InitializeParents(parent);

        int index = 0;
        while (index < symbolCount)
        {
            nodeFrequencies[index] = frequencies[index];
            index = index + 1;
        }

        int nodeCount = symbolCount;
        int roots = symbolCount;
        while (roots > 1)
        {
            int first = FindSmallestRoot(nodeFrequencies, parent, nodeCount, -1);
            int second = FindSmallestRoot(nodeFrequencies, parent, nodeCount, first);

            parent[first] = nodeCount;
            parent[second] = nodeCount;
            nodeFrequencies[nodeCount] = nodeFrequencies[first] + nodeFrequencies[second];
            nodeCount = nodeCount + 1;
            roots = roots - 1;
        }

        int[] codeLengths = new int[257];
        index = 0;
        while (index < symbolCount)
        {
            int length = 0;
            int node = index;
            while (parent[node] >= 0)
            {
                length = length + 1;
                node = parent[node];
            }

            codeLengths[symbols[index]] = length;
            index = index + 1;
        }

        return codeLengths;
    }

    internal int[] CountCodeLengths(int[] codeLengths)
    {
        int[] lengthCounts = new int[257];
        int symbol = 0;
        while (symbol < codeLengths.Length)
        {
            int length = codeLengths[symbol];
            if (length > 0)
            {
                lengthCounts[length] = lengthCounts[length] + 1;
            }

            symbol = symbol + 1;
        }

        return lengthCounts;
    }

    internal void LimitCodeLengths(int[] lengthCounts)
    {
        int length = lengthCounts.Length - 1;
        while (length > 16)
        {
            while (lengthCounts[length] > 0)
            {
                int donorLength = length - 2;
                while (donorLength > 0 && lengthCounts[donorLength] == 0)
                {
                    donorLength = donorLength - 1;
                }

                if (donorLength == 0)
                {
                    throw JpegWriteValidationException.Create("Huffman code lengths cannot be limited to 16 bits.");
                }

                lengthCounts[length] = lengthCounts[length] - 2;
                lengthCounts[length - 1] = lengthCounts[length - 1] + 1;
                lengthCounts[donorLength + 1] = lengthCounts[donorLength + 1] + 2;
                lengthCounts[donorLength] = lengthCounts[donorLength] - 1;
            }

            length = length - 1;
        }
    }

    internal int[] AssignLimitedCodeLengths(int[] symbols, long[] frequenciesBySymbol, int symbolCount, int[] lengthCounts)
    {
        int[] sortedSymbols = CopySymbols(symbols, symbolCount);
        SortSymbolsByFrequency(sortedSymbols, frequenciesBySymbol, symbolCount);

        int[] codeLengths = new int[257];
        int symbolIndex = 0;
        int length = 1;
        while (length <= 16)
        {
            int count = lengthCounts[length];
            int index = 0;
            while (index < count)
            {
                if (symbolIndex >= symbolCount)
                {
                    throw JpegWriteValidationException.Create("Huffman length counts exceed the used symbol count.");
                }

                codeLengths[sortedSymbols[symbolIndex]] = length;
                symbolIndex = symbolIndex + 1;
                index = index + 1;
            }

            length = length + 1;
        }

        if (symbolIndex != symbolCount)
        {
            throw JpegWriteValidationException.Create("Huffman length counts do not cover every used symbol.");
        }

        return codeLengths;
    }

    internal int[] SortSymbolsForTable(int[] symbols, int[] codeLengths, long[] frequenciesBySymbol, int symbolCount)
    {
        int[] sortedSymbols = CopySymbols(symbols, symbolCount);
        int index = 0;
        while (index < symbolCount - 1)
        {
            int bestIndex = index;
            int candidateIndex = index + 1;
            while (candidateIndex < symbolCount)
            {
                if (IsTableOrderBefore(sortedSymbols[candidateIndex], sortedSymbols[bestIndex], codeLengths, frequenciesBySymbol))
                {
                    bestIndex = candidateIndex;
                }

                candidateIndex = candidateIndex + 1;
            }

            Swap(sortedSymbols, index, bestIndex);
            index = index + 1;
        }

        return sortedSymbols;
    }

    internal JpegHuffmanTable CreateTable(JpegHuffmanStatistics statistics, int[] codeLengths, int[] orderedSymbols, int orderedSymbolCount)
    {
        JpegHuffmanTable table = new JpegHuffmanTable();
        table.TableClass = statistics.TableClass;
        table.Identifier = statistics.Identifier;

        int actualSymbolCount = 0;
        int symbol = 0;
        while (symbol < 256)
        {
            int length = codeLengths[symbol];
            if (length > 0)
            {
                table.CodeCounts[length - 1] = table.CodeCounts[length - 1] + 1;
                actualSymbolCount = actualSymbolCount + 1;
            }

            symbol = symbol + 1;
        }

        table.SymbolCount = actualSymbolCount;
        table.Symbols = new int[actualSymbolCount];
        table.Codes = new int[actualSymbolCount];
        table.CodeLengths = new int[actualSymbolCount];

        int outputIndex = 0;
        int index = 0;
        while (index < orderedSymbolCount)
        {
            symbol = orderedSymbols[index];
            if (symbol < 256)
            {
                table.Symbols[outputIndex] = symbol;
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        new JpegHuffmanTableParser().BuildDecodeTable(table);
        return table;
    }

    internal int FindSmallestRoot(long[] frequencies, int[] parent, int nodeCount, int excludedIndex)
    {
        int smallestIndex = -1;
        int index = 0;
        while (index < nodeCount)
        {
            if (parent[index] < 0 && index != excludedIndex)
            {
                if (smallestIndex < 0 || frequencies[index] < frequencies[smallestIndex])
                {
                    smallestIndex = index;
                }
                else if (frequencies[index] == frequencies[smallestIndex] && index < smallestIndex)
                {
                    smallestIndex = index;
                }
            }

            index = index + 1;
        }

        if (smallestIndex < 0)
        {
            throw JpegWriteValidationException.Create("Huffman tree requires at least two root nodes.");
        }

        return smallestIndex;
    }

    internal int[] CopySymbols(int[] symbols, int symbolCount)
    {
        int[] copy = new int[symbolCount];
        int index = 0;
        while (index < symbolCount)
        {
            copy[index] = symbols[index];
            index = index + 1;
        }

        return copy;
    }

    internal void SortSymbolsByFrequency(int[] symbols, long[] frequenciesBySymbol, int symbolCount)
    {
        int index = 0;
        while (index < symbolCount - 1)
        {
            int bestIndex = index;
            int candidateIndex = index + 1;
            while (candidateIndex < symbolCount)
            {
                if (IsFrequencyOrderBefore(symbols[candidateIndex], symbols[bestIndex], frequenciesBySymbol))
                {
                    bestIndex = candidateIndex;
                }

                candidateIndex = candidateIndex + 1;
            }

            Swap(symbols, index, bestIndex);
            index = index + 1;
        }
    }

    internal bool IsFrequencyOrderBefore(int candidate, int current, long[] frequenciesBySymbol)
    {
        if (frequenciesBySymbol[candidate] > frequenciesBySymbol[current])
        {
            return true;
        }

        if (frequenciesBySymbol[candidate] < frequenciesBySymbol[current])
        {
            return false;
        }

        return candidate < current;
    }

    internal bool IsTableOrderBefore(int candidate, int current, int[] codeLengths, long[] frequenciesBySymbol)
    {
        if (codeLengths[candidate] < codeLengths[current])
        {
            return true;
        }

        if (codeLengths[candidate] > codeLengths[current])
        {
            return false;
        }

        return IsFrequencyOrderBefore(candidate, current, frequenciesBySymbol);
    }

    internal void Swap(int[] values, int first, int second)
    {
        if (first != second)
        {
            int value = values[first];
            values[first] = values[second];
            values[second] = value;
        }
    }

    internal void InitializeParents(int[] parent)
    {
        int index = 0;
        while (index < parent.Length)
        {
            parent[index] = -1;
            index = index + 1;
        }
    }

    internal void ValidateStatistics(JpegHuffmanStatistics statistics)
    {
        if (statistics == null)
        {
            throw new System.ArgumentNullException("statistics");
        }

        if (statistics.TableClass != 0 && statistics.TableClass != 1)
        {
            throw JpegWriteValidationException.Create("Huffman statistics table class must be DC or AC.");
        }

        if (statistics.Identifier < 0 || statistics.Identifier > 3)
        {
            throw JpegWriteValidationException.Create("Huffman statistics table identifier must be between 0 and 3.");
        }

        if (statistics.Counts == null || statistics.Counts.Length < 256)
        {
            throw JpegWriteValidationException.Create("Huffman statistics count array must contain 256 entries.");
        }
    }
}
