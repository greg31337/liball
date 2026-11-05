namespace svg;

internal class SvgWoff2Point
{
    internal int X;
    internal int Y;
    internal bool OnCurve;

    internal SvgWoff2Point(int x, int y, bool onCurve)
    {
        X = x;
        Y = y;
        OnCurve = onCurve;
    }
}

internal class SvgWoff2GlyphResult
{
    internal bool Success;
    internal SvgOpenTypeError Error;
    internal string Message;
    internal byte[]? Glyf;
    internal byte[]? Loca;
    internal int[]? XMinimums;
    internal int GlyphCount;
    internal int IndexFormat;

    internal SvgWoff2GlyphResult()
    {
        Success = false;
        Error = SvgOpenTypeError.None;
        Message = "";
        Glyf = null;
        Loca = null;
        XMinimums = null;
        GlyphCount = 0;
        IndexFormat = 0;
    }
}

internal static class SvgWoff2GlyphDecoder
{
    internal static SvgWoff2GlyphResult Decode(byte[]? transformed, int locaLength, SvgOpenTypeReadOptions options)
    {
        if (transformed == null || transformed.Length < 36) return Failure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 glyf header is truncated.");
        SvgWebFontCursor header = new SvgWebFontCursor(transformed, 0, transformed.Length);
        int reserved = header.ReadUInt16();
        int optionFlags = header.ReadUInt16();
        int glyphCount = header.ReadUInt16();
        int indexFormat = header.ReadUInt16();
        if (reserved != 0 || (optionFlags & 65534) != 0 || glyphCount < 1 || indexFormat != 0 && indexFormat != 1) return Failure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 glyf header is invalid.");
        if (glyphCount > options.Limits.MaxGlyphsPerFont) return Failure(SvgOpenTypeError.LimitExceeded, "The transformed WOFF2 glyph count exceeds MaxGlyphsPerFont.");
        int expectedLocaLength = (glyphCount + 1) * (indexFormat == 0 ? 2 : 4);
        if (locaLength != expectedLocaLength) return Failure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 loca length is inconsistent with its glyph count.");
        int[] sizes = new int[7];
        long streamEnd = 36;
        int index = 0;
        while (index < sizes.Length)
        {
            long value = header.ReadUInt32();
            if (value < 0 || value > System.Int32.MaxValue) return Failure(SvgOpenTypeError.InvalidTable, "A transformed WOFF2 glyf substream length is invalid.");
            sizes[index] = (int)value;
            streamEnd = streamEnd + value;
            if (streamEnd > transformed.Length) return Failure(SvgOpenTypeError.InvalidTable, "A transformed WOFF2 glyf substream is truncated.");
            index = index + 1;
        }
        int overlapLength = (optionFlags & 1) != 0 ? (glyphCount + 7) / 8 : 0;
        if (streamEnd + overlapLength != transformed.Length) return Failure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 glyf stream lengths are inconsistent.");
        int start = 36;
        SvgWebFontCursor contours = new SvgWebFontCursor(transformed, start, sizes[0]);
        start = start + sizes[0];
        SvgWebFontCursor pointCounts = new SvgWebFontCursor(transformed, start, sizes[1]);
        start = start + sizes[1];
        SvgWebFontCursor flags = new SvgWebFontCursor(transformed, start, sizes[2]);
        start = start + sizes[2];
        SvgWebFontCursor glyphs = new SvgWebFontCursor(transformed, start, sizes[3]);
        start = start + sizes[3];
        SvgWebFontCursor composites = new SvgWebFontCursor(transformed, start, sizes[4]);
        start = start + sizes[4];
        SvgWebFontCursor boxes = new SvgWebFontCursor(transformed, start, sizes[5]);
        start = start + sizes[5];
        SvgWebFontCursor instructions = new SvgWebFontCursor(transformed, start, sizes[6]);
        start = start + sizes[6];
        byte[] overlap = new byte[overlapLength];
        if (overlapLength != 0) System.Array.Copy(transformed, start, overlap, 0, overlapLength);
        int boxBitmapLength = 4 * ((glyphCount + 31) / 32);
        if (boxes.Remaining() < boxBitmapLength || contours.Remaining() != glyphCount * 2) return Failure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 contour or bounding-box bitmap stream is invalid.");
        byte[] boxBitmap = new byte[boxBitmapLength];
        System.Array.Copy(boxes.Bytes, boxes.Position, boxBitmap, 0, boxBitmapLength);
        boxes.Position = boxes.Position + boxBitmapLength;
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        int[] locations = new int[glyphCount + 1];
        int[] minimums = new int[glyphCount];
        int glyphIndex = 0;
        while (glyphIndex < glyphCount)
        {
            if (options.CancellationToken.IsCancellationRequested) return Failure(SvgOpenTypeError.Cancelled, "WOFF2 glyph reconstruction was cancelled.");
            if (output.Length > options.Limits.MaxBytesPerFont) return Failure(SvgOpenTypeError.LimitExceeded, "The reconstructed WOFF2 glyf table exceeds MaxBytesPerFont.");
            locations[glyphIndex] = (int)output.Length;
            int contourValue = contours.ReadUInt16();
            if (contourValue < 0) return Failure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 contour stream is truncated.");
            int contourCount = contourValue == 65535 ? -1 : contourValue;
            bool explicitBox = (boxBitmap[glyphIndex >> 3] & 128 >> (glyphIndex & 7)) != 0;
            byte[] record;
            if (contourCount == 0)
            {
                if (explicitBox) return Failure(SvgOpenTypeError.InvalidTable, "An empty transformed WOFF2 glyph has an explicit bounding box.");
                record = new byte[0];
                minimums[glyphIndex] = 0;
            }
            else if (contourCount > 0)
            {
                SvgWoff2SimpleResult simple = DecodeSimple(contourCount, explicitBox, glyphIndex, pointCounts, flags, glyphs, boxes, instructions, overlap, options);
                if (!simple.Success) return Failure(simple.Error, simple.Message);
                record = (simple.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                minimums[glyphIndex] = simple.XMinimum;
            }
            else
            {
                SvgWoff2SimpleResult composite = DecodeComposite(explicitBox, glyphCount, composites, glyphs, boxes, instructions);
                if (!composite.Success) return Failure(composite.Error, composite.Message);
                record = (composite.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                minimums[glyphIndex] = composite.XMinimum;
            }
            if (output.Length + (record ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length > options.Limits.MaxBytesPerFont) return Failure(SvgOpenTypeError.LimitExceeded, "The reconstructed WOFF2 glyf table exceeds MaxBytesPerFont.");
            output.Write(record, 0, record.Length);
            while (output.Length % 4 != 0) output.WriteByte(0);
            glyphIndex = glyphIndex + 1;
        }
        locations[glyphCount] = (int)output.Length;
        if (contours.Remaining() != 0 || pointCounts.Remaining() != 0 || flags.Remaining() != 0 || glyphs.Remaining() != 0 || composites.Remaining() != 0 || boxes.Remaining() != 0 || instructions.Remaining() != 0) return Failure(SvgOpenTypeError.InvalidTable, "A transformed WOFF2 glyph substream contains unused data.");
        byte[]? loca = BuildLoca(locations, indexFormat);
        if (loca == null || loca.Length != locaLength) return Failure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 loca table cannot represent the reconstructed offsets.");
        SvgWoff2GlyphResult result = new SvgWoff2GlyphResult();
        result.Success = true;
        result.Glyf = output.ToArray();
        result.Loca = loca;
        result.XMinimums = minimums;
        result.GlyphCount = glyphCount;
        result.IndexFormat = indexFormat;
        return result;
    }

    internal static SvgWoff2GlyphResult ReadMinimums(System.Collections.Generic.List<SvgWebFontTable> tables, SvgOpenTypeReadOptions options)
    {
        SvgWebFontTable? glyf = FindTable(tables, "glyf");
        SvgWebFontTable? loca = FindTable(tables, "loca");
        SvgWebFontTable? head = FindTable(tables, "head");
        SvgWebFontTable? maxp = FindTable(tables, "maxp");
        if (glyf == null || loca == null || head == null || maxp == null || (head.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 54 || (maxp.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 6) return Failure(SvgOpenTypeError.InvalidTable, "A transformed WOFF2 hmtx table is missing glyph prerequisites.");
        int glyphCount = ReadUInt16(maxp.Data, 4);
        int indexFormat = ReadInt16(head.Data, 50);
        if (glyphCount < 1 || glyphCount > options.Limits.MaxGlyphsPerFont || indexFormat != 0 && indexFormat != 1) return Failure(SvgOpenTypeError.InvalidTable, "The WOFF2 glyph prerequisites are invalid.");
        int width = indexFormat == 0 ? 2 : 4;
        if ((loca.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length != (glyphCount + 1) * width) return Failure(SvgOpenTypeError.InvalidTable, "The WOFF2 loca table length is invalid.");
        int[] minimums = new int[glyphCount];
        long preceding = 0;
        int index = 0;
        while (index < glyphCount)
        {
            long start = ReadLocation(loca.Data, index, indexFormat);
            long end = ReadLocation(loca.Data, index + 1, indexFormat);
            if (index == 0 && start != 0 || start < preceding || end < start || end > (glyf.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length) return Failure(SvgOpenTypeError.InvalidTable, "A WOFF2 loca offset is invalid.");
            if (end != start)
            {
                if (end - start < 10) return Failure(SvgOpenTypeError.InvalidTable, "A WOFF2 glyph record is truncated.");
                minimums[index] = ReadInt16(glyf.Data, (int)start + 2);
            }
            preceding = end;
            index = index + 1;
        }
        if (preceding != (glyf.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length) return Failure(SvgOpenTypeError.InvalidTable, "The WOFF2 loca table does not span the glyf table.");
        SvgWoff2GlyphResult result = new SvgWoff2GlyphResult();
        result.Success = true;
        result.XMinimums = minimums;
        result.GlyphCount = glyphCount;
        result.IndexFormat = indexFormat;
        return result;
    }

    internal static byte[]? ReconstructMetrics(byte[]? transformed, System.Collections.Generic.List<SvgWebFontTable> tables, SvgWoff2GlyphResult glyphs, SvgOpenTypeReadOptions options)
    {
        SvgWebFontTable? hhea = FindTable(tables, "hhea");
        if (transformed == null || hhea == null || (hhea.Data ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length < 36 || glyphs == null || !glyphs.Success || (glyphs.XMinimums ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length != glyphs.GlyphCount) return null;
        int metricCount = ReadUInt16(hhea.Data, 34);
        if (metricCount < 1 || metricCount > glyphs.GlyphCount) return null;
        long outputLength = 2L * glyphs.GlyphCount + 2L * metricCount;
        if (outputLength > options.Limits.MaxBytesPerFont || outputLength > System.Int32.MaxValue) return null;
        SvgWebFontCursor cursor = new SvgWebFontCursor(transformed, 0, transformed.Length);
        int flags = cursor.ReadByte();
        if (flags < 1 || flags > 3) return null;
        int[] advances = new int[metricCount];
        int index = 0;
        while (index < metricCount)
        {
            advances[index] = cursor.ReadUInt16();
            if (advances[index] < 0) return null;
            index = index + 1;
        }
        int[] bearings = new int[glyphs.GlyphCount];
        index = 0;
        while (index < glyphs.GlyphCount)
        {
            bool omitted = index < metricCount ? (flags & 1) != 0 : (flags & 2) != 0;
            if (omitted) bearings[index] = glyphs.XMinimums[index];
            else
            {
                int value = cursor.ReadUInt16();
                if (value < 0) return null;
                bearings[index] = value >= 32768 ? value - 65536 : value;
            }
            index = index + 1;
        }
        if (cursor.Remaining() != 0) return null;
        byte[] output = new byte[(int)outputLength];
        int position = 0;
        index = 0;
        while (index < glyphs.GlyphCount)
        {
            if (index < metricCount)
            {
                WriteUInt16(output, position, advances[index]);
                position = position + 2;
            }
            WriteUInt16(output, position, bearings[index]);
            position = position + 2;
            index = index + 1;
        }
        return output;
    }

    static SvgWoff2SimpleResult DecodeSimple(int contourCount, bool explicitBox, int glyphIndex, SvgWebFontCursor pointCounts, SvgWebFontCursor flags, SvgWebFontCursor glyphs, SvgWebFontCursor boxes, SvgWebFontCursor instructions, byte[] overlap, SvgOpenTypeReadOptions options)
    {
        int[] counts = new int[contourCount];
        int pointCount = 0;
        int index = 0;
        while (index < contourCount)
        {
            SvgWebFontNumber count = Read255(pointCounts);
            if (!count.Valid || count.Value < 1) return SimpleFailure(SvgOpenTypeError.InvalidTable, "A transformed WOFF2 contour has no points.");
            if (pointCount > options.Limits.MaxPointsPerGlyph - count.Value) return SimpleFailure(SvgOpenTypeError.LimitExceeded, "A transformed WOFF2 simple glyph exceeds its point limit.");
            counts[index] = count.Value;
            pointCount = pointCount + count.Value;
            index = index + 1;
        }
        if (flags.Remaining() < pointCount) return SimpleFailure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 point-flag stream is truncated.");
        byte[] pointFlags = new byte[pointCount];
        System.Array.Copy(flags.Bytes, flags.Position, pointFlags, 0, pointCount);
        flags.Position = flags.Position + pointCount;
        SvgWoff2Point[]? points = DecodeTriplets(pointFlags, glyphs);
        if (points == null) return SimpleFailure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 point triplets are invalid.");
        SvgWebFontNumber instructionLength = Read255(glyphs);
        if (!instructionLength.Valid || instructions.Remaining() < instructionLength.Value) return SimpleFailure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 instruction stream is truncated.");
        byte[] instructionBytes = new byte[instructionLength.Value];
        System.Array.Copy(instructions.Bytes, instructions.Position, instructionBytes, 0, instructionBytes.Length);
        instructions.Position = instructions.Position + instructionBytes.Length;
        int[]? bounds = explicitBox ? ReadBounds(boxes) : ComputeBounds(points);
        if (bounds == null) return SimpleFailure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 simple-glyph bounds are invalid.");
        bool hasOverlap = overlap.Length != 0 && (overlap[glyphIndex >> 3] & 128 >> (glyphIndex & 7)) != 0;
        byte[]? record = BuildSimple(contourCount, counts, points, instructionBytes, bounds, hasOverlap);
        if (record == null) return SimpleFailure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 simple glyph cannot be encoded.");
        SvgWoff2SimpleResult result = new SvgWoff2SimpleResult();
        result.Success = true;
        result.Bytes = record;
        result.XMinimum = bounds[0];
        return result;
    }

    static SvgWoff2SimpleResult DecodeComposite(bool explicitBox, int glyphCount, SvgWebFontCursor composites, SvgWebFontCursor glyphs, SvgWebFontCursor boxes, SvgWebFontCursor instructions)
    {
        if (!explicitBox) return SimpleFailure(SvgOpenTypeError.InvalidTable, "A transformed WOFF2 composite glyph lacks an explicit bounding box.");
        int[]? bounds = ReadBounds(boxes);
        if (bounds == null) return SimpleFailure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 composite bounds are invalid.");
        int compositeStart = composites.Position;
        int componentFlags = 32;
        bool hasInstructions = false;
        while ((componentFlags & 32) != 0)
        {
            componentFlags = composites.ReadUInt16();
            if (componentFlags < 0) return SimpleFailure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 composite stream is truncated.");
            int knownFlags = 8191;
            int transformFlags = componentFlags & 200;
            if ((componentFlags & ~knownFlags) != 0 || transformFlags != 0 && transformFlags != 8 && transformFlags != 64 && transformFlags != 128 || (componentFlags & 6144) == 6144) return SimpleFailure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 composite flags are invalid.");
            hasInstructions = hasInstructions || (componentFlags & 256) != 0;
            int componentGlyph = composites.ReadUInt16();
            if (componentGlyph < 0 || componentGlyph >= glyphCount) return SimpleFailure(SvgOpenTypeError.InvalidTable, "A transformed WOFF2 component glyph index is invalid.");
            int extra = (componentFlags & 1) != 0 ? 4 : 2;
            if ((componentFlags & 8) != 0) extra = extra + 2;
            else if ((componentFlags & 64) != 0) extra = extra + 4;
            else if ((componentFlags & 128) != 0) extra = extra + 8;
            if (composites.Remaining() < extra) return SimpleFailure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 composite component is truncated.");
            composites.Position = composites.Position + extra;
        }
        int compositeLength = composites.Position - compositeStart;
        int instructionLength = 0;
        if (hasInstructions)
        {
            SvgWebFontNumber length = Read255(glyphs);
            if (!length.Valid || instructions.Remaining() < length.Value) return SimpleFailure(SvgOpenTypeError.InvalidTable, "The transformed WOFF2 composite instructions are truncated.");
            instructionLength = length.Value;
        }
        int recordLength = 10 + compositeLength + (hasInstructions ? 2 + instructionLength : 0);
        byte[] record = new byte[recordLength];
        WriteUInt16(record, 0, 65535);
        WriteBounds(record, 2, bounds);
        System.Array.Copy(composites.Bytes, compositeStart, record, 10, compositeLength);
        if (hasInstructions)
        {
            int position = 10 + compositeLength;
            WriteUInt16(record, position, instructionLength);
            System.Array.Copy(instructions.Bytes, instructions.Position, record, position + 2, instructionLength);
            instructions.Position = instructions.Position + instructionLength;
        }
        SvgWoff2SimpleResult result = new SvgWoff2SimpleResult();
        result.Success = true;
        result.Bytes = record;
        result.XMinimum = bounds[0];
        return result;
    }

    static SvgWoff2Point[]? DecodeTriplets(byte[] flags, SvgWebFontCursor glyphs)
    {
        SvgWoff2Point[] result = new SvgWoff2Point[flags.Length];
        int x = 0;
        int y = 0;
        int index = 0;
        while (index < flags.Length)
        {
            int raw = flags[index];
            bool onCurve = (raw & 128) == 0;
            int flag = raw & 127;
            int byteCount = flag < 84 ? 1 : flag < 120 ? 2 : flag < 124 ? 3 : 4;
            if (glyphs.Remaining() < byteCount) return null;
            int first = glyphs.ReadByte();
            int dx;
            int dy;
            if (flag < 10)
            {
                dx = 0;
                dy = WithSign(flag, ((flag & 14) << 7) + first);
            }
            else if (flag < 20)
            {
                dx = WithSign(flag, (((flag - 10) & 14) << 7) + first);
                dy = 0;
            }
            else if (flag < 84)
            {
                int firstFlag = flag - 20;
                dx = WithSign(flag, 1 + (firstFlag & 48) + (first >> 4));
                dy = WithSign(flag >> 1, 1 + ((firstFlag & 12) << 2) + (first & 15));
            }
            else if (flag < 120)
            {
                int firstFlag = flag - 84;
                int second = glyphs.ReadByte();
                dx = WithSign(flag, 1 + (firstFlag / 12 << 8) + first);
                dy = WithSign(flag >> 1, 1 + ((firstFlag % 12 >> 2) << 8) + second);
            }
            else if (flag < 124)
            {
                int second = glyphs.ReadByte();
                int third = glyphs.ReadByte();
                dx = WithSign(flag, (first << 4) + (second >> 4));
                dy = WithSign(flag >> 1, ((second & 15) << 8) + third);
            }
            else
            {
                int second = glyphs.ReadByte();
                int third = glyphs.ReadByte();
                int fourth = glyphs.ReadByte();
                dx = WithSign(flag, first << 8 | second);
                dy = WithSign(flag >> 1, third << 8 | fourth);
            }
            long nextX = (long)x + dx;
            long nextY = (long)y + dy;
            if (nextX < System.Int16.MinValue || nextX > System.Int16.MaxValue || nextY < System.Int16.MinValue || nextY > System.Int16.MaxValue) return null;
            x = (int)nextX;
            y = (int)nextY;
            result[index] = new SvgWoff2Point(x, y, onCurve);
            index = index + 1;
        }
        return result;
    }

    static byte[]? BuildSimple(int contourCount, int[] counts, SvgWoff2Point[] points, byte[] instructions, int[] bounds, bool overlap)
    {
        byte[] pointFlags = new byte[points.Length];
        System.Collections.Generic.List<byte> xBytes = new System.Collections.Generic.List<byte>();
        System.Collections.Generic.List<byte> yBytes = new System.Collections.Generic.List<byte>();
        int lastX = 0;
        int lastY = 0;
        int index = 0;
        while (index < points.Length)
        {
            SvgWoff2Point point = points[index];
            int flag = point.OnCurve ? 1 : 0;
            if (overlap && index == 0) flag = flag | 64;
            int dx = point.X - lastX;
            int dy = point.Y - lastY;
            flag = EncodeCoordinate(dx, true, flag, xBytes);
            flag = EncodeCoordinate(dy, false, flag, yBytes);
            pointFlags[index] = (byte)flag;
            lastX = point.X;
            lastY = point.Y;
            index = index + 1;
        }
        long length = 12L + 2L * contourCount + instructions.Length + pointFlags.Length + xBytes.Count + yBytes.Count;
        if (length > System.Int32.MaxValue) return null;
        byte[] output = new byte[(int)length];
        WriteUInt16(output, 0, contourCount);
        WriteBounds(output, 2, bounds);
        int position = 10;
        int endPoint = -1;
        index = 0;
        while (index < counts.Length)
        {
            endPoint = endPoint + counts[index];
            if (endPoint > 65535) return null;
            WriteUInt16(output, position, endPoint);
            position = position + 2;
            index = index + 1;
        }
        WriteUInt16(output, position, instructions.Length);
        position = position + 2;
        System.Array.Copy(instructions, 0, output, position, instructions.Length);
        position = position + instructions.Length;
        System.Array.Copy(pointFlags, 0, output, position, pointFlags.Length);
        position = position + pointFlags.Length;
        xBytes.CopyTo(output, position);
        position = position + xBytes.Count;
        yBytes.CopyTo(output, position);
        return output;
    }

    static int EncodeCoordinate(int delta, bool x, int flag, System.Collections.Generic.List<byte> bytes)
    {
        int shortBit = x ? 2 : 4;
        int sameBit = x ? 16 : 32;
        if (delta == 0) return flag | sameBit;
        if (delta > -256 && delta < 256)
        {
            flag = flag | shortBit;
            if (delta > 0) flag = flag | sameBit;
            bytes.Add((byte)System.Math.Abs(delta));
            return flag;
        }
        bytes.Add((byte)((delta >> 8) & 255));
        bytes.Add((byte)(delta & 255));
        return flag;
    }

    static int[]? ReadBounds(SvgWebFontCursor boxes)
    {
        if (boxes.Remaining() < 8) return null;
        int[] bounds = new int[4];
        int index = 0;
        while (index < 4)
        {
            int value = boxes.ReadUInt16();
            bounds[index] = value >= 32768 ? value - 65536 : value;
            index = index + 1;
        }
        if (bounds[0] > bounds[2] || bounds[1] > bounds[3]) return null;
        return bounds;
    }

    static int[]? ComputeBounds(SvgWoff2Point[] points)
    {
        if (points.Length == 0) return null;
        int[] bounds = new int[] { points[0].X, points[0].Y, points[0].X, points[0].Y };
        int index = 1;
        while (index < points.Length)
        {
            if (points[index].X < bounds[0]) bounds[0] = points[index].X;
            if (points[index].Y < bounds[1]) bounds[1] = points[index].Y;
            if (points[index].X > bounds[2]) bounds[2] = points[index].X;
            if (points[index].Y > bounds[3]) bounds[3] = points[index].Y;
            index = index + 1;
        }
        return bounds;
    }

    static byte[]? BuildLoca(int[] locations, int indexFormat)
    {
        int width = indexFormat == 0 ? 2 : 4;
        byte[] output = new byte[locations.Length * width];
        int index = 0;
        while (index < locations.Length)
        {
            int value = locations[index];
            if (indexFormat == 0)
            {
                if (value % 2 != 0 || value / 2 > 65535) return null;
                WriteUInt16(output, index * 2, value / 2);
            }
            else WriteUInt32(output, index * 4, (uint)value);
            index = index + 1;
        }
        return output;
    }

    static SvgWebFontNumber Read255(SvgWebFontCursor cursor)
    {
        int code = cursor.ReadByte();
        if (code < 0) return new SvgWebFontNumber(false, 0);
        if (code == 253)
        {
            int value = cursor.ReadUInt16();
            return new SvgWebFontNumber(value >= 0, value < 0 ? 0 : value);
        }
        if (code == 254 || code == 255)
        {
            int next = cursor.ReadByte();
            if (next < 0) return new SvgWebFontNumber(false, 0);
            return new SvgWebFontNumber(true, next + (code == 255 ? 253 : 506));
        }
        return new SvgWebFontNumber(true, code);
    }

    static int WithSign(int flag, int value)
    {
        return (flag & 1) != 0 ? value : -value;
    }

    static SvgWebFontTable? FindTable(System.Collections.Generic.List<SvgWebFontTable> tables, string tag)
    {
        int index = 0;
        while (index < tables.Count)
        {
            if (tables[index].Tag == tag) return tables[index];
            index = index + 1;
        }
        return null;
    }

    static long ReadLocation(byte[] bytes, int index, int format)
    {
        if (format == 0) return ReadUInt16(bytes, index * 2) * 2L;
        return ReadUInt32(bytes, index * 4);
    }

    static int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    static int ReadInt16(byte[] bytes, int offset)
    {
        int value = ReadUInt16(bytes, offset);
        return value >= 32768 ? value - 65536 : value;
    }

    static uint ReadUInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }

    static void WriteBounds(byte[] bytes, int offset, int[] bounds)
    {
        WriteUInt16(bytes, offset, bounds[0]);
        WriteUInt16(bytes, offset + 2, bounds[1]);
        WriteUInt16(bytes, offset + 4, bounds[2]);
        WriteUInt16(bytes, offset + 6, bounds[3]);
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 8) & 255);
        bytes[offset + 1] = (byte)(value & 255);
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)((value >> 24) & 255);
        bytes[offset + 1] = (byte)((value >> 16) & 255);
        bytes[offset + 2] = (byte)((value >> 8) & 255);
        bytes[offset + 3] = (byte)(value & 255);
    }

    static SvgWoff2GlyphResult Failure(SvgOpenTypeError error, string message)
    {
        SvgWoff2GlyphResult result = new SvgWoff2GlyphResult();
        result.Error = error;
        result.Message = message;
        return result;
    }

    static SvgWoff2SimpleResult SimpleFailure(SvgOpenTypeError error, string message)
    {
        SvgWoff2SimpleResult result = new SvgWoff2SimpleResult();
        result.Error = error;
        result.Message = message;
        return result;
    }
}

internal class SvgWoff2SimpleResult
{
    internal bool Success;
    internal SvgOpenTypeError Error;
    internal string Message;
    internal byte[]? Bytes;
    internal int XMinimum;

    internal SvgWoff2SimpleResult()
    {
        Success = false;
        Error = SvgOpenTypeError.None;
        Message = "";
        Bytes = null;
        XMinimum = 0;
    }
}
