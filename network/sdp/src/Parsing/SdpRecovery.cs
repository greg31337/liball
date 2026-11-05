using System;

namespace sdp;

internal static class SdpRecovery
{
    internal static SdpRecoveryResult RecoverCharacterLine(ReadOnlySpan<char> input, int startOffset, int lineNumber, int maximumLineLength, int maximumLineCount, bool retainMalformedLine)
    {
        SdpLineReadResult lineResult = SdpLineReader.ReadCharacterLine(input, startOffset, lineNumber, maximumLineLength, maximumLineCount);
        SdpRecoveryResult result = new SdpRecoveryResult();
        result.NextOffset = lineResult.NextOffset;
        result.NextCharacterOffset = lineResult.NextOffset;
        result.NextLineNumber = lineNumber;
        result.EndOfInput = lineResult.EndOfInput;
        if (lineResult.LineCountExceeded)
        {
            result.LineCountExceeded = true;
            result.DiagnosticCode = SdpDiagnosticCodes.ParseLineCountLimitExceeded;
            return result;
        }
        if (!lineResult.HasLine)
        {
            return result;
        }

        result.Recovered = true;
        result.NextLineNumber = lineNumber + 1;
        if (lineResult.LineLengthExceeded)
        {
            result.LineLengthExceeded = true;
            result.DiagnosticCode = SdpDiagnosticCodes.ParseLineLengthLimitExceeded;
        }
        else
        {
            result.DiagnosticCode = SdpDiagnosticCodes.ParseRecoveredMalformedLine;
            RetainLine(result, lineResult.Line, retainMalformedLine);
        }
        InspectCharacterBoundary(result, input, lineResult.NextOffset);
        return result;
    }

    internal static SdpRecoveryResult RecoverUtf8Line(ReadOnlySpan<byte> input, int startByteOffset, int startCharacterOffset, int lineNumber, int maximumLineLength, int maximumLineCount, bool retainMalformedLine)
    {
        SdpLineReadResult lineResult = SdpLineReader.ReadUtf8Line(input, startByteOffset, startCharacterOffset, lineNumber, maximumLineLength, maximumLineCount);
        SdpRecoveryResult result = new SdpRecoveryResult();
        result.NextOffset = lineResult.NextOffset;
        result.NextByteOffset = lineResult.NextOffset;
        result.NextCharacterOffset = startCharacterOffset;
        result.NextLineNumber = lineNumber;
        result.EndOfInput = lineResult.EndOfInput;
        if (lineResult.LineCountExceeded)
        {
            result.LineCountExceeded = true;
            result.DiagnosticCode = SdpDiagnosticCodes.ParseLineCountLimitExceeded;
            return result;
        }
        if (!lineResult.HasLine)
        {
            return result;
        }

        result.Recovered = true;
        result.NextCharacterOffset = startCharacterOffset + lineResult.CharactersConsumed;
        result.NextLineNumber = lineNumber + 1;
        if (lineResult.LineLengthExceeded)
        {
            result.LineLengthExceeded = true;
            result.DiagnosticCode = SdpDiagnosticCodes.ParseLineLengthLimitExceeded;
        }
        else
        {
            result.DiagnosticCode = SdpDiagnosticCodes.ParseRecoveredMalformedLine;
            RetainLine(result, lineResult.Line, retainMalformedLine);
        }
        InspectUtf8Boundary(result, input, lineResult.NextOffset);
        return result;
    }

    internal static void RetainLine(SdpRecoveryResult result, SdpRawLine line, bool retainMalformedLine)
    {
        if (!retainMalformedLine)
        {
            return;
        }
        line.IsMalformed = true;
        if (line.Text.Length >= 2 && line.Text[1] == '=')
        {
            line.FieldName = line.Text[0];
            line.Value = line.Text.Substring(2);
        }
        result.Line = line;
        result.Retained = true;
    }

    internal static void InspectCharacterBoundary(SdpRecoveryResult result, ReadOnlySpan<char> input, int offset)
    {
        if (offset >= input.Length - 1 || input[offset + 1] != '=')
        {
            return;
        }
        char fieldName = input[offset];
        if (fieldName < 'a' || fieldName > 'z')
        {
            return;
        }
        result.NextLineIsFieldBoundary = true;
        result.NextLineIsMediaBoundary = fieldName == 'm';
        result.NextLineIsSessionBoundary = fieldName == 'v';
    }

    internal static void InspectUtf8Boundary(SdpRecoveryResult result, ReadOnlySpan<byte> input, int offset)
    {
        if (offset >= input.Length - 1 || input[offset + 1] != 61)
        {
            return;
        }
        byte fieldName = input[offset];
        if (fieldName < 97 || fieldName > 122)
        {
            return;
        }
        result.NextLineIsFieldBoundary = true;
        result.NextLineIsMediaBoundary = fieldName == 109;
        result.NextLineIsSessionBoundary = fieldName == 118;
    }
}
