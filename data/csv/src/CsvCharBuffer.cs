using System.IO;

namespace csv;

internal class CsvCharBuffer
{
    internal TextReader Reader;
    internal char[] Buffer;
    internal int Position;
    internal int Length;
    internal int BufferSize;
    internal bool EndOfReader;
    internal long CharacterOffset;

    internal CsvCharBuffer()
    {
        Reader = TextReader.Null;
        BufferSize = 4096;
        Buffer = new char[BufferSize];
        Position = 0;
        Length = 0;
        EndOfReader = false;
        CharacterOffset = 0;
    }

    internal static CsvCharBuffer Create(TextReader reader, int bufferSize)
    {
        CsvCharBuffer charBuffer = new CsvCharBuffer();
        charBuffer.Reader = reader;
        if (bufferSize > 0)
        {
            charBuffer.BufferSize = bufferSize;
        }
        else
        {
            charBuffer.BufferSize = 4096;
        }

        charBuffer.Buffer = new char[charBuffer.BufferSize];
        charBuffer.Position = 0;
        charBuffer.Length = 0;
        charBuffer.EndOfReader = false;
        charBuffer.CharacterOffset = 0;
        return charBuffer;
    }

    internal void Reset(TextReader reader)
    {
        Reader = reader;
        Position = 0;
        Length = 0;
        EndOfReader = false;
        CharacterOffset = 0;
    }

    internal int Fill()
    {
        if (EndOfReader)
        {
            return 0;
        }

        Position = 0;
        Length = Reader.Read(Buffer, 0, Buffer.Length);
        if (Length == 0)
        {
            EndOfReader = true;
        }

        return Length;
    }

    internal int Peek()
    {
        if (Position >= Length)
        {
            Fill();
        }

        if (Position >= Length)
        {
            return -1;
        }

        int value = Buffer[Position];
        return value;
    }

    internal int Read()
    {
        int value = Peek();
        if (value < 0)
        {
            return -1;
        }

        Position = Position + 1;
        CharacterOffset = CharacterOffset + 1;
        return value;
    }

    internal int ReadBlock(char[] destination, int startIndex, int count)
    {
        int readCount = 0;
        while (readCount < count)
        {
            int value = Read();
            if (value < 0)
            {
                return readCount;
            }

            destination[startIndex + readCount] = (char)value;
            readCount = readCount + 1;
        }

        return readCount;
    }

    internal bool IsEnd()
    {
        int value = Peek();
        if (value < 0)
        {
            return true;
        }

        return false;
    }
}
