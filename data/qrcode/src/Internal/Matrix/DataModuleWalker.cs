namespace qrcode;

internal class DataModuleWalker
{
    internal static void PlaceModel2Codewords(
        byte[] codewords,
        int version,
        BitMatrixBuffer destination)
    {
        if (codewords == null)
        {
            throw new QrInputException("The codeword collection cannot be null.", "codewords");
        }

        ValidateModel2Matrix(version, destination, "destination");
        int expectedCodewordCount = Model2VersionTable.GetTotalCodewordCount(version);
        if (codewords.Length != expectedCodewordCount)
        {
            throw new QrInputException(
                "The codeword count does not match the selected Model 2 version.",
                "codewords");
        }

        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
        int codewordBitCount = codewords.Length * 8;
        int placementIndex = 0;
        int pairCount = Model2PlacementTable.GetDataColumnPairCount(version);
        int dimension = Model2VersionTable.GetDimension(version);
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            int rightColumn = Model2PlacementTable.GetDataColumnPairRight(
                version,
                pairIndex);
            bool upward = Model2PlacementTable.IsDataColumnPairUpward(version, pairIndex);
            for (int rowIndex = 0; rowIndex < dimension; rowIndex++)
            {
                int row = upward ? dimension - 1 - rowIndex : rowIndex;
                for (int columnOffset = 0; columnOffset < 2; columnOffset++)
                {
                    int column = rightColumn - columnOffset;
                    if (reserved.GetModule(row, column))
                    {
                        continue;
                    }

                    bool dark = false;
                    if (placementIndex < codewordBitCount)
                    {
                        int byteIndex = placementIndex / 8;
                        int bitOffset = 7 - placementIndex % 8;
                        dark = ((codewords[byteIndex] >> bitOffset) & 1) != 0;
                    }

                    destination.SetModule(row, column, dark);
                    placementIndex++;
                }
            }
        }
    }

    internal static byte[] ExtractModel2Codewords(
        int version,
        BitMatrixBuffer source)
    {
        ValidateModel2Matrix(version, source, "source");
        int codewordCount = Model2VersionTable.GetTotalCodewordCount(version);
        byte[] codewords = new byte[codewordCount];
        int codewordBitCount = codewordCount * 8;
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
        int placementIndex = 0;
        int pairCount = Model2PlacementTable.GetDataColumnPairCount(version);
        int dimension = Model2VersionTable.GetDimension(version);
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            int rightColumn = Model2PlacementTable.GetDataColumnPairRight(
                version,
                pairIndex);
            bool upward = Model2PlacementTable.IsDataColumnPairUpward(version, pairIndex);
            for (int rowIndex = 0; rowIndex < dimension; rowIndex++)
            {
                int row = upward ? dimension - 1 - rowIndex : rowIndex;
                for (int columnOffset = 0; columnOffset < 2; columnOffset++)
                {
                    int column = rightColumn - columnOffset;
                    if (reserved.GetModule(row, column))
                    {
                        continue;
                    }

                    if (placementIndex < codewordBitCount
                        && source.GetModule(row, column))
                    {
                        int byteIndex = placementIndex / 8;
                        int bitOffset = 7 - placementIndex % 8;
                        codewords[byteIndex] = (byte)(codewords[byteIndex] | 1 << bitOffset);
                    }

                    placementIndex++;
                }
            }
        }

        return codewords;
    }

    internal static void PlaceMicroCodewords(
        byte[] codewords,
        int version,
        int errorCorrectionIndex,
        BitMatrixBuffer destination)
    {
        if (codewords == null)
        {
            throw new QrInputException("The codeword collection cannot be null.", "codewords");
        }

        ValidateMicroMatrix(version, destination, "destination");
        int dataCharacterCount = MicroVersionTable.GetDataSymbolCharacterCount(
            version,
            errorCorrectionIndex);
        int expectedCodewordCount = dataCharacterCount
            + MicroVersionTable.GetEccCodewordCount(version, errorCorrectionIndex);
        if (codewords.Length != expectedCodewordCount)
        {
            throw new QrInputException(
                "The codeword count does not match the selected Micro QR configuration.",
                "codewords");
        }

        int dataBitCount = MicroVersionTable.GetDataBitCapacity(
            version,
            errorCorrectionIndex);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateMicroReservedMap(version);
        int placementIndex = 0;
        int pairCount = MicroPlacementTable.GetDataColumnPairCount(version);
        int dimension = MicroVersionTable.GetDimension(version);
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            int rightColumn = MicroPlacementTable.GetDataColumnPairRight(
                version,
                pairIndex);
            bool upward = MicroPlacementTable.IsDataColumnPairUpward(version, pairIndex);
            for (int rowIndex = 0; rowIndex < dimension; rowIndex++)
            {
                int row = upward ? dimension - 1 - rowIndex : rowIndex;
                for (int columnOffset = 0; columnOffset < 2; columnOffset++)
                {
                    int column = rightColumn - columnOffset;
                    if (reserved.GetModule(row, column))
                    {
                        continue;
                    }

                    int byteIndex;
                    int bitOffset;
                    if (placementIndex < dataBitCount)
                    {
                        byteIndex = placementIndex / 8;
                        bitOffset = 7 - placementIndex % 8;
                    }
                    else
                    {
                        int eccBitIndex = placementIndex - dataBitCount;
                        byteIndex = dataCharacterCount + eccBitIndex / 8;
                        bitOffset = 7 - eccBitIndex % 8;
                    }

                    bool dark = ((codewords[byteIndex] >> bitOffset) & 1) != 0;
                    destination.SetModule(row, column, dark);
                    placementIndex++;
                }
            }
        }
    }

    internal static byte[] ExtractMicroCodewords(
        int version,
        int errorCorrectionIndex,
        BitMatrixBuffer source)
    {
        ValidateMicroMatrix(version, source, "source");
        int dataCharacterCount = MicroVersionTable.GetDataSymbolCharacterCount(
            version,
            errorCorrectionIndex);
        int codewordCount = dataCharacterCount
            + MicroVersionTable.GetEccCodewordCount(version, errorCorrectionIndex);
        byte[] codewords = new byte[codewordCount];
        int dataBitCount = MicroVersionTable.GetDataBitCapacity(
            version,
            errorCorrectionIndex);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateMicroReservedMap(version);
        int placementIndex = 0;
        int pairCount = MicroPlacementTable.GetDataColumnPairCount(version);
        int dimension = MicroVersionTable.GetDimension(version);
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            int rightColumn = MicroPlacementTable.GetDataColumnPairRight(
                version,
                pairIndex);
            bool upward = MicroPlacementTable.IsDataColumnPairUpward(version, pairIndex);
            for (int rowIndex = 0; rowIndex < dimension; rowIndex++)
            {
                int row = upward ? dimension - 1 - rowIndex : rowIndex;
                for (int columnOffset = 0; columnOffset < 2; columnOffset++)
                {
                    int column = rightColumn - columnOffset;
                    if (reserved.GetModule(row, column))
                    {
                        continue;
                    }

                    int byteIndex;
                    int bitOffset;
                    if (placementIndex < dataBitCount)
                    {
                        byteIndex = placementIndex / 8;
                        bitOffset = 7 - placementIndex % 8;
                    }
                    else
                    {
                        int eccBitIndex = placementIndex - dataBitCount;
                        byteIndex = dataCharacterCount + eccBitIndex / 8;
                        bitOffset = 7 - eccBitIndex % 8;
                    }

                    if (source.GetModule(row, column))
                    {
                        codewords[byteIndex] = (byte)(codewords[byteIndex] | 1 << bitOffset);
                    }

                    placementIndex++;
                }
            }
        }

        return codewords;
    }

    internal static void PlaceRectangularMicroCodewords(
        byte[] codewords,
        int versionIndex,
        BitMatrixBuffer destination)
    {
        if (codewords == null)
        {
            throw new QrInputException("The codeword collection cannot be null.", "codewords");
        }

        ValidateRectangularMicroMatrix(versionIndex, destination, "destination");
        int expectedCodewordCount = RectangularMicroVersionTable.GetTotalCodewordCount(
            versionIndex);
        if (codewords.Length != expectedCodewordCount)
        {
            throw new QrInputException(
                "The codeword count does not match the selected rMQR version.",
                "codewords");
        }

        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateRectangularMicroReservedMap(
            versionIndex);
        int codewordBitCount = codewords.Length * 8;
        int placementIndex = 0;
        int pairCount = RectangularMicroPlacementTable.GetDataColumnPairCount(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            int rightColumn = RectangularMicroPlacementTable.GetDataColumnPairRight(
                versionIndex,
                pairIndex);
            bool upward = RectangularMicroPlacementTable.IsDataColumnPairUpward(
                versionIndex,
                pairIndex);
            for (int rowIndex = 0; rowIndex < height; rowIndex++)
            {
                int row = upward ? height - 1 - rowIndex : rowIndex;
                for (int columnOffset = 0; columnOffset < 2; columnOffset++)
                {
                    int column = rightColumn - columnOffset;
                    if (reserved.GetModule(row, column))
                    {
                        continue;
                    }

                    bool dark = false;
                    if (placementIndex < codewordBitCount)
                    {
                        int byteIndex = placementIndex / 8;
                        int bitOffset = 7 - placementIndex % 8;
                        dark = ((codewords[byteIndex] >> bitOffset) & 1) != 0;
                    }

                    destination.SetModule(row, column, dark);
                    placementIndex++;
                }
            }
        }
    }

    internal static byte[] ExtractRectangularMicroCodewords(
        int versionIndex,
        BitMatrixBuffer source)
    {
        ValidateRectangularMicroMatrix(versionIndex, source, "source");
        int codewordCount = RectangularMicroVersionTable.GetTotalCodewordCount(versionIndex);
        byte[] codewords = new byte[codewordCount];
        int codewordBitCount = codewordCount * 8;
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateRectangularMicroReservedMap(
            versionIndex);
        int placementIndex = 0;
        int pairCount = RectangularMicroPlacementTable.GetDataColumnPairCount(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            int rightColumn = RectangularMicroPlacementTable.GetDataColumnPairRight(
                versionIndex,
                pairIndex);
            bool upward = RectangularMicroPlacementTable.IsDataColumnPairUpward(
                versionIndex,
                pairIndex);
            for (int rowIndex = 0; rowIndex < height; rowIndex++)
            {
                int row = upward ? height - 1 - rowIndex : rowIndex;
                for (int columnOffset = 0; columnOffset < 2; columnOffset++)
                {
                    int column = rightColumn - columnOffset;
                    if (reserved.GetModule(row, column))
                    {
                        continue;
                    }

                    if (placementIndex < codewordBitCount
                        && source.GetModule(row, column))
                    {
                        int byteIndex = placementIndex / 8;
                        int bitOffset = 7 - placementIndex % 8;
                        codewords[byteIndex] = (byte)(codewords[byteIndex] | 1 << bitOffset);
                    }

                    placementIndex++;
                }
            }
        }

        return codewords;
    }

    static void ValidateModel2Matrix(
        int version,
        BitMatrixBuffer matrix,
        string inputName)
    {
        if (matrix == null)
        {
            throw new QrInputException("The module matrix cannot be null.", inputName);
        }

        int dimension = Model2VersionTable.GetDimension(version);
        if (matrix.GetWidth() != dimension || matrix.GetHeight() != dimension)
        {
            throw new QrInputException(
                "The module matrix dimensions do not match the selected Model 2 version.",
                inputName);
        }
    }

    static void ValidateMicroMatrix(
        int version,
        BitMatrixBuffer matrix,
        string inputName)
    {
        if (matrix == null)
        {
            throw new QrInputException("The module matrix cannot be null.", inputName);
        }

        int dimension = MicroVersionTable.GetDimension(version);
        if (matrix.GetWidth() != dimension || matrix.GetHeight() != dimension)
        {
            throw new QrInputException(
                "The module matrix dimensions do not match the selected Micro QR version.",
                inputName);
        }
    }

    static void ValidateRectangularMicroMatrix(
        int versionIndex,
        BitMatrixBuffer matrix,
        string inputName)
    {
        if (matrix == null)
        {
            throw new QrInputException("The module matrix cannot be null.", inputName);
        }

        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        if (matrix.GetWidth() != width || matrix.GetHeight() != height)
        {
            throw new QrInputException(
                "The module matrix dimensions do not match the selected rMQR version.",
                inputName);
        }
    }
}
