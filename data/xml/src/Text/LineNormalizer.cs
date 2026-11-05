using System;

namespace xml;

internal class XmlLineNormalizationResult
{
    internal string Text;
    internal long[] RawByteOffsets;
    internal int[] Lines;
    internal int[] Columns;

    internal XmlLineNormalizationResult(string text, long[] rawByteOffsets, int[] lines, int[] columns)
    {
        Text = text;
        RawByteOffsets = rawByteOffsets;
        Lines = lines;
        Columns = columns;
    }

    internal SourceLocation GetNormalizedSourceLocation(int normalizedUtf16Offset, string? sourceUri)
    {
        Guard.NonNegativeInt(normalizedUtf16Offset, "normalizedUtf16Offset");
        if (normalizedUtf16Offset > Text.Length)
        {
            throw new ArgumentOutOfRangeException("normalizedUtf16Offset");
        }

        SourceLocation location = new SourceLocation();
        location.SourceUri = sourceUri;
        location.CharacterOffset = normalizedUtf16Offset;
        location.ByteOffset = RawByteOffsets[normalizedUtf16Offset];
        location.Line = Lines[normalizedUtf16Offset];
        location.Column = Columns[normalizedUtf16Offset];
        return location;
    }
}

internal static class LineNormalizer
{
    internal static XmlLineNormalizationResult NormalizeXmlLineEndings(XmlDecodedInput input, XmlVersion version, XmlEncodingEntityKind entityKind)
    {
        Guard.NotNull(input, "input");
        if (version != XmlVersion.Xml10 && version != XmlVersion.Xml11)
        {
            throw new ArgumentOutOfRangeException("version");
        }

        if (entityKind != XmlEncodingEntityKind.Document && entityKind != XmlEncodingEntityKind.ExternalParsedEntity)
        {
            throw new ArgumentOutOfRangeException("entityKind");
        }

        string text = input.Text;
        long[] inputByteOffsets = input.RawByteOffsets;
        if (inputByteOffsets.Length != text.Length + 1)
        {
            throw new ArgumentException("The decoded input byte map does not match its text.", "input");
        }

        ValueBuffer normalized = new ValueBuffer(text.Length);
        long[] byteOffsets = new long[text.Length + 1];
        int[] lines = new int[text.Length + 1];
        int[] columns = new int[text.Length + 1];
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(text.AsMemory());
        int line = 1;
        int column = 1;
        byteOffsets[0] = inputByteOffsets[0];
        lines[0] = line;
        columns[0] = column;
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            int inputEnd = scanner.CurrentUtf16Offset();
            if (version == XmlVersion.Xml11 && input.HasDeclaration && scalar.Utf16Offset < input.DeclarationCharacterCount && (scalar.Value == 0x85 || scalar.Value == 0x2028))
            {
                throw new FormatException("XML 1.1 NEL and line-separator characters are not permitted in an XML or text declaration at UTF-16 offset " + scalar.Utf16Offset + ".");
            }

            bool lineBreak = scalar.Value == 0xA || scalar.Value == 0xD;
            if (version == XmlVersion.Xml11 && (scalar.Value == 0x85 || scalar.Value == 0x2028))
            {
                lineBreak = true;
            }

            if (scalar.Value == 0xD && inputEnd < text.Length)
            {
                int following = text[inputEnd];
                if (following == 0xA || version == XmlVersion.Xml11 && following == 0x85)
                {
                    if (version == XmlVersion.Xml11 && input.HasDeclaration && inputEnd < input.DeclarationCharacterCount && following == 0x85)
                    {
                        throw new FormatException("XML 1.1 NEL and line-separator characters are not permitted in an XML or text declaration at UTF-16 offset " + inputEnd + ".");
                    }

                    UnicodeScalar consumed = scanner.ReadUnicodeScalar();
                    inputEnd = consumed.Utf16Offset + consumed.Utf16CodeUnitCount;
                }
            }

            int outputStart = normalized.CharacterCount();
            byteOffsets[outputStart] = inputByteOffsets[scalar.Utf16Offset];
            lines[outputStart] = line;
            columns[outputStart] = column;
            if (lineBreak)
            {
                normalized.AppendCharacter('\n');
                line = line + 1;
                column = 1;
                int outputEnd = normalized.CharacterCount();
                byteOffsets[outputEnd] = inputByteOffsets[inputEnd];
                lines[outputEnd] = line;
                columns[outputEnd] = column;
            }
            else
            {
                normalized.AppendCharacters(text.AsSpan(scalar.Utf16Offset, scalar.Utf16CodeUnitCount));
                int inner = 0;
                while (inner < scalar.Utf16CodeUnitCount)
                {
                    int outputOffset = outputStart + inner;
                    byteOffsets[outputOffset] = inputByteOffsets[scalar.Utf16Offset + inner];
                    lines[outputOffset] = line;
                    columns[outputOffset] = column;
                    inner = inner + 1;
                }

                column = column + 1;
                int outputEnd = normalized.CharacterCount();
                byteOffsets[outputEnd] = inputByteOffsets[inputEnd];
                lines[outputEnd] = line;
                columns[outputEnd] = column;
            }
        }

        int normalizedLength = normalized.CharacterCount();
        long[] exactByteOffsets = new long[normalizedLength + 1];
        int[] exactLines = new int[normalizedLength + 1];
        int[] exactColumns = new int[normalizedLength + 1];
        Array.Copy(byteOffsets, exactByteOffsets, normalizedLength + 1);
        Array.Copy(lines, exactLines, normalizedLength + 1);
        Array.Copy(columns, exactColumns, normalizedLength + 1);
        return new XmlLineNormalizationResult(normalized.GetText(), exactByteOffsets, exactLines, exactColumns);
    }
}
