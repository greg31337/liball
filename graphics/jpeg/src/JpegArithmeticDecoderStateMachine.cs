namespace jpeg;

internal class JpegArithmeticDecoderStateMachine
{
    internal static readonly int[] QeValues = new int[]
    {
        0x5a1d, 0x2586, 0x1114, 0x080b, 0x03d8, 0x01da, 0x00e5, 0x006f,
        0x0036, 0x001a, 0x000d, 0x0006, 0x0003, 0x0001, 0x5a7f, 0x3f25,
        0x2cf2, 0x207c, 0x17b9, 0x1182, 0x0cef, 0x09a1, 0x072f, 0x055c,
        0x0406, 0x0303, 0x0240, 0x01b1, 0x0144, 0x00f5, 0x00b7, 0x008a,
        0x0068, 0x004e, 0x003b, 0x002c, 0x5ae1, 0x484c, 0x3a0d, 0x2ef1,
        0x261f, 0x1f33, 0x19a8, 0x1518, 0x1177, 0x0e74, 0x0bfb, 0x09f8,
        0x0861, 0x0706, 0x05cd, 0x04de, 0x040f, 0x0363, 0x02d4, 0x025c,
        0x01f8, 0x01a4, 0x0160, 0x0125, 0x00f6, 0x00cb, 0x00ab, 0x008f,
        0x5b12, 0x4d04, 0x412c, 0x37d8, 0x2fe8, 0x293c, 0x2379, 0x1edf,
        0x1aa9, 0x174e, 0x1424, 0x119c, 0x0f6b, 0x0d51, 0x0bb6, 0x0a40,
        0x5832, 0x4d1c, 0x438e, 0x3bdd, 0x34ee, 0x2eae, 0x299a, 0x2516,
        0x5570, 0x4ca9, 0x44d9, 0x3e22, 0x3824, 0x32b4, 0x2e17, 0x56a8,
        0x4f46, 0x47e5, 0x41cf, 0x3c3d, 0x375e, 0x5231, 0x4c0f, 0x4639,
        0x415e, 0x5627, 0x50e7, 0x4b85, 0x5597, 0x504f, 0x5a10, 0x5522,
        0x59eb, 0x5a1d
    };

    internal static readonly int[] NextMps = new int[]
    {
        1, 2, 3, 4, 5, 6, 7, 8,
        9, 10, 11, 12, 13, 13, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31, 32,
        33, 34, 35, 9, 37, 38, 39, 40,
        41, 42, 43, 44, 45, 46, 47, 48,
        49, 50, 51, 52, 53, 54, 55, 56,
        57, 58, 59, 60, 61, 62, 63, 32,
        65, 66, 67, 68, 69, 70, 71, 72,
        73, 74, 75, 76, 77, 78, 79, 48,
        81, 82, 83, 84, 85, 86, 87, 71,
        89, 90, 91, 92, 93, 94, 86, 96,
        97, 98, 99, 100, 93, 102, 103, 104,
        99, 106, 107, 103, 109, 107, 111, 109,
        111, 113
    };

    internal static readonly int[] NextLps = new int[]
    {
        1, 14, 16, 18, 20, 23, 25, 28,
        30, 33, 35, 9, 10, 12, 15, 36,
        38, 39, 40, 42, 43, 45, 46, 48,
        49, 51, 52, 54, 56, 57, 59, 60,
        62, 63, 32, 33, 37, 64, 65, 67,
        68, 69, 70, 72, 73, 74, 75, 77,
        78, 79, 48, 50, 50, 51, 52, 53,
        54, 55, 56, 57, 58, 59, 61, 61,
        65, 80, 81, 82, 83, 84, 86, 87,
        87, 72, 72, 74, 74, 75, 77, 77,
        80, 88, 89, 90, 91, 92, 93, 86,
        88, 95, 96, 97, 99, 99, 93, 95,
        101, 102, 103, 104, 99, 105, 106, 107,
        103, 105, 108, 109, 110, 111, 110, 112,
        112, 113
    };

    internal static readonly int[] SwitchMps = new int[]
    {
        1, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 1, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 1, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 0, 0, 0, 0, 0, 0, 0,
        1, 0, 0, 0, 0, 0, 0, 1,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 1, 0, 0, 0, 0, 1, 0,
        1, 0
    };

    internal JpegByteReader Reader = new JpegByteReader(new byte[0]);
    internal long C;
    internal long A;
    internal int Ct;
    internal bool HasMarker;
    internal int MarkerValue;
    internal int MarkerCode;
    internal long MarkerOffset;
    internal long EntropyStartOffset;
    internal long EntropyBytesRead;
    internal int StuffedByteCount;

    internal JpegArithmeticDecoderStateMachine(byte[] data)
    {
        Reader = new JpegByteReader(data);
        Initialize();
    }

    internal static JpegArithmeticDecoderStateMachine FromStream(System.IO.Stream stream)
    {
        return FromReader(JpegByteReader.FromStream(stream));
    }

    internal static JpegArithmeticDecoderStateMachine FromReader(JpegByteReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        JpegArithmeticDecoderStateMachine stateMachine = new JpegArithmeticDecoderStateMachine(new byte[0]);
        stateMachine.Reader = reader;
        stateMachine.Initialize();
        return stateMachine;
    }

    internal void Initialize()
    {
        C = 0;
        A = 0;
        Ct = -16;
        HasMarker = false;
        MarkerValue = 0;
        MarkerCode = 0;
        MarkerOffset = -1;
        EntropyStartOffset = Reader.AbsoluteOffset;
        EntropyBytesRead = 0;
        StuffedByteCount = 0;
    }

    internal int DecodeBit(int[] statistics, int contextIndex)
    {
        ValidateContext(statistics, contextIndex);
        Renormalize();

        int stateValue = statistics[contextIndex];
        int mps = stateValue >> 7;
        int decodedLps = mps ^ 1;
        int stateIndex = stateValue & 127;
        int qe = QeValues[stateIndex];
        int nextMps = NextMps[stateIndex];
        int nextLps = NextLps[stateIndex];

        long temp = A - qe;
        A = temp;
        temp = temp << Ct;
        if (C >= temp)
        {
            C = C - temp;
            if (A < qe)
            {
                A = qe;
                statistics[contextIndex] = (mps << 7) | nextMps;
                return mps;
            }

            A = qe;
            if (SwitchMps[stateIndex] != 0)
            {
                mps = mps ^ 1;
            }

            statistics[contextIndex] = (mps << 7) | nextLps;
            return decodedLps;
        }

        if (A < 0x8000)
        {
            if (A < qe)
            {
                if (SwitchMps[stateIndex] != 0)
                {
                    mps = mps ^ 1;
                }

                statistics[contextIndex] = (mps << 7) | nextLps;
                return decodedLps;
            }

            statistics[contextIndex] = (mps << 7) | nextMps;
        }

        return mps;
    }

    internal void Renormalize()
    {
        while (A < 0x8000)
        {
            Ct = Ct - 1;
            if (Ct < 0)
            {
                int data = ReadArithmeticByte();
                C = (C << 8) | (long)data;
                Ct = Ct + 8;
                if (Ct < 0)
                {
                    Ct = Ct + 1;
                    if (Ct == 0)
                    {
                        A = 0x8000;
                    }
                }
            }

            A = A << 1;
        }
    }

    internal int ReadArithmeticByte()
    {
        if (HasMarker)
        {
            return 0;
        }

        long offset = Reader.AbsoluteOffset;
        int value = Reader.ReadByte();
        if (value == JpegConstants.MarkerPrefix)
        {
            int code = Reader.ReadByte();
            while (code == JpegConstants.MarkerPrefix)
            {
                code = Reader.ReadByte();
            }

            if (code == JpegConstants.MarkerStuffedByte)
            {
                value = JpegConstants.MarkerPrefix;
                StuffedByteCount = StuffedByteCount + 1;
            }
            else
            {
                SetMarkerLookahead(code, offset);
                value = 0;
            }
        }

        EntropyBytesRead = Reader.AbsoluteOffset - EntropyStartOffset;
        return value;
    }

    internal void SetMarkerLookahead(int code, long offset)
    {
        HasMarker = true;
        MarkerCode = code;
        MarkerValue = (JpegConstants.MarkerPrefix << JpegConstants.MarkerPrefixShift) + code;
        MarkerOffset = offset;
    }

    internal JpegScannedMarker GetMarkerLookahead()
    {
        if (!HasMarker)
        {
            throw JpegInvalidDataException.Create("No JPEG marker is available.");
        }

        JpegScannedMarker marker = new JpegScannedMarker();
        marker.Value = MarkerValue;
        marker.Code = MarkerCode;
        marker.Marker = (JpegMarker)MarkerValue;
        marker.Offset = MarkerOffset;
        marker.FoundAfterEntropyData = true;
        marker.EntropyBytesSkipped = EntropyBytesRead;
        return marker;
    }

    internal void ConsumeMarkerLookahead()
    {
        HasMarker = false;
        MarkerValue = 0;
        MarkerCode = 0;
        MarkerOffset = -1;
    }

    internal void ResetStatistics(int[] statistics)
    {
        if (statistics == null)
        {
            throw new System.ArgumentNullException("statistics");
        }

        int index = 0;
        while (index < statistics.Length)
        {
            statistics[index] = 0;
            index = index + 1;
        }
    }

    internal void ValidateContext(int[] statistics, int contextIndex)
    {
        if (statistics == null)
        {
            throw new System.ArgumentNullException("statistics");
        }

        if (contextIndex < 0 || contextIndex >= statistics.Length)
        {
            throw new System.ArgumentOutOfRangeException("contextIndex");
        }

        int stateIndex = statistics[contextIndex] & 127;
        if (stateIndex < 0 || stateIndex >= QeValues.Length)
        {
            throw JpegInvalidDataException.Create("Arithmetic probability state index is invalid.");
        }
    }

    internal int GetQeValue(int stateIndex)
    {
        ValidateStateIndex(stateIndex);
        return QeValues[stateIndex];
    }

    internal int GetNextMpsIndex(int stateIndex)
    {
        ValidateStateIndex(stateIndex);
        return NextMps[stateIndex];
    }

    internal int GetNextLpsIndex(int stateIndex)
    {
        ValidateStateIndex(stateIndex);
        return NextLps[stateIndex];
    }

    internal bool ShouldSwitchMps(int stateIndex)
    {
        ValidateStateIndex(stateIndex);
        return SwitchMps[stateIndex] != 0;
    }

    internal void ValidateStateIndex(int stateIndex)
    {
        if (stateIndex < 0 || stateIndex >= QeValues.Length)
        {
            throw new System.ArgumentOutOfRangeException("stateIndex");
        }
    }
}
