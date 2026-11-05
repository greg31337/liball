namespace jpeg;

internal class JpegArithmeticEncoderStateMachine
{
    internal JpegByteWriter Writer = new JpegByteWriter();
    internal long C;
    internal long A;
    internal long Sc;
    internal long Zc;
    internal int Ct;
    internal int Buffer;
    internal bool Finished;

    internal JpegArithmeticEncoderStateMachine()
    {
        Writer = new JpegByteWriter();
        Initialize();
    }

    internal static JpegArithmeticEncoderStateMachine FromStream(System.IO.Stream stream)
    {
        return FromWriter(JpegByteWriter.FromStream(stream));
    }

    internal static JpegArithmeticEncoderStateMachine FromWriter(JpegByteWriter writer)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        JpegArithmeticEncoderStateMachine stateMachine = new JpegArithmeticEncoderStateMachine();
        stateMachine.Writer = writer;
        stateMachine.Initialize();
        return stateMachine;
    }

    internal void Initialize()
    {
        C = 0;
        A = 0x10000;
        Sc = 0;
        Zc = 0;
        Ct = 11;
        Buffer = -1;
        Finished = false;
    }

    internal void EncodeBit(int[] statistics, int contextIndex, int value)
    {
        if (Finished)
        {
            throw JpegWriteValidationException.Create("Arithmetic encoder cannot write after the scan is finished.");
        }

        ValidateBit(value);
        ValidateContext(statistics, contextIndex);

        int stateValue = statistics[contextIndex];
        int mps = stateValue >> 7;
        int stateIndex = stateValue & 127;
        int qe = JpegArithmeticDecoderStateMachine.QeValues[stateIndex];
        int nextMps = JpegArithmeticDecoderStateMachine.NextMps[stateIndex];
        int nextLps = JpegArithmeticDecoderStateMachine.NextLps[stateIndex];

        A = A - qe;
        if (value != mps)
        {
            if (A >= qe)
            {
                C = C + A;
                A = qe;
            }

            if (JpegArithmeticDecoderStateMachine.SwitchMps[stateIndex] != 0)
            {
                mps = mps ^ 1;
            }

            statistics[contextIndex] = (mps << 7) | nextLps;
        }
        else
        {
            if (A >= 0x8000)
            {
                return;
            }

            if (A < qe)
            {
                C = C + A;
                A = qe;
            }

            statistics[contextIndex] = (mps << 7) | nextMps;
        }

        Renormalize();
    }

    internal void Renormalize()
    {
        while (A < 0x8000)
        {
            A = A << 1;
            C = C << 1;
            Ct = Ct - 1;
            if (Ct == 0)
            {
                OutputPendingByte(C >> 19);
                C = C & 0x7FFFF;
                Ct = Ct + 8;
            }
        }
    }

    internal void Finish()
    {
        if (Finished)
        {
            return;
        }

        long temp = (A - 1 + C) & 0xFFFF0000L;
        if (temp < C)
        {
            C = temp + 0x8000;
        }
        else
        {
            C = temp;
        }

        C = C << Ct;
        if ((C & 0xF8000000L) != 0)
        {
            if (Buffer >= 0)
            {
                EmitPendingZeros();
                EmitByte(Buffer + 1);
                if (Buffer + 1 == 0xFF)
                {
                    EmitByte(0);
                }
            }

            Zc = Zc + Sc;
            Sc = 0;
        }
        else
        {
            if (Buffer == 0)
            {
                Zc = Zc + 1;
            }
            else if (Buffer >= 0)
            {
                EmitPendingZeros();
                EmitByte(Buffer);
            }

            EmitStackedFF();
        }

        if ((C & 0x7FFF800L) != 0)
        {
            EmitPendingZeros();
            int first = (int)((C >> 19) & 255);
            EmitByte(first);
            if (first == 0xFF)
            {
                EmitByte(0);
            }

            if ((C & 0x7F800L) != 0)
            {
                int second = (int)((C >> 11) & 255);
                EmitByte(second);
                if (second == 0xFF)
                {
                    EmitByte(0);
                }
            }
        }

        Finished = true;
    }

    internal void OutputPendingByte(long temp)
    {
        if (temp > 0xFF)
        {
            if (Buffer >= 0)
            {
                EmitPendingZeros();
                EmitByte(Buffer + 1);
                if (Buffer + 1 == 0xFF)
                {
                    EmitByte(0);
                }
            }

            Zc = Zc + Sc;
            Sc = 0;
            Buffer = (int)(temp & 255);
        }
        else if (temp == 0xFF)
        {
            Sc = Sc + 1;
        }
        else
        {
            if (Buffer == 0)
            {
                Zc = Zc + 1;
            }
            else if (Buffer >= 0)
            {
                EmitPendingZeros();
                EmitByte(Buffer);
            }

            EmitStackedFF();
            Buffer = (int)(temp & 255);
        }
    }

    internal void EmitPendingZeros()
    {
        while (Zc > 0)
        {
            EmitByte(0);
            Zc = Zc - 1;
        }
    }

    internal void EmitStackedFF()
    {
        while (Sc > 0)
        {
            EmitPendingZeros();
            EmitByte(0xFF);
            EmitByte(0);
            Sc = Sc - 1;
        }
    }

    internal void EmitByte(int value)
    {
        Writer.WriteByte(value);
    }

    internal byte[] ToArray()
    {
        return Writer.ToArray();
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
        if (stateIndex < 0 || stateIndex >= JpegArithmeticDecoderStateMachine.QeValues.Length)
        {
            throw JpegWriteValidationException.Create("Arithmetic probability state index is invalid.");
        }
    }

    internal void ValidateBit(int value)
    {
        if (value != 0 && value != 1)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
    }
}
