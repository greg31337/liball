namespace ttf;

internal class CffGlyph
{
    int glyphId;
    double advanceWidth;
    SfntGlyphOutline outline;
    Type2CharStringResult executionResult;
    FontDiagnostic[] diagnostics;

    internal CffGlyph(
        int glyphId,
        double advanceWidth,
        SfntGlyphOutline outline,
        Type2CharStringResult executionResult,
        FontDiagnostic[] diagnostics)
    {
        this.glyphId = glyphId;
        this.advanceWidth = advanceWidth;
        this.outline = outline;
        this.executionResult = executionResult;
        this.diagnostics = diagnostics;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal double GetAdvanceWidth()
    {
        return advanceWidth;
    }

    internal SfntGlyphOutline GetOutline()
    {
        return outline;
    }

    internal Type2CharStringResult GetExecutionResult()
    {
        return executionResult;
    }

    internal int GetDiagnosticCount()
    {
        return diagnostics.Length;
    }

    internal FontDiagnostic GetDiagnostic(int index)
    {
        if (index < 0 || index >= diagnostics.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return diagnostics[index];
    }
}

internal class CffContourAccumulator
{
    System.Collections.Generic.List<SfntOutlinePoint> points;
    System.Collections.Generic.List<SfntPathCommand> commands;
    bool hasSegments;

    internal CffContourAccumulator(double x, double y)
    {
        points = new System.Collections.Generic.List<SfntOutlinePoint>();
        commands = new System.Collections.Generic.List<SfntPathCommand>();
        points.Add(new SfntOutlinePoint(x, y, true, false, -1));
        commands.Add(SfntPathCommand.Create(
            SfntPathCommandKind.Move,
            0,
            0,
            x,
            y));
    }

    internal bool HasSegments()
    {
        return hasSegments;
    }

    internal int GetPointCount()
    {
        return points.Count;
    }

    internal int GetCommandCount()
    {
        return commands.Count + 1;
    }

    internal SfntPathCommand GetCommand(int index)
    {
        if (index < 0 || index >= commands.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return commands[index];
    }

    internal void AddLine(double x, double y)
    {
        points.Add(new SfntOutlinePoint(x, y, true, false, -1));
        commands.Add(SfntPathCommand.Create(
            SfntPathCommandKind.Line,
            0,
            0,
            x,
            y));
        hasSegments = true;
    }

    internal void AddCubic(
        double firstX,
        double firstY,
        double secondX,
        double secondY,
        double endX,
        double endY)
    {
        points.Add(new SfntOutlinePoint(
            firstX,
            firstY,
            false,
            false,
            -1));
        points.Add(new SfntOutlinePoint(
            secondX,
            secondY,
            false,
            false,
            -1));
        points.Add(new SfntOutlinePoint(
            endX,
            endY,
            true,
            false,
            -1));
        commands.Add(SfntPathCommand.Create(
            SfntPathCommandKind.Cubic,
            firstX,
            firstY,
            secondX,
            secondY,
            endX,
            endY));
        hasSegments = true;
    }

    internal SfntOutlineContour CreateContour()
    {
        if (!hasSegments)
        {
            throw new System.InvalidOperationException(
                "An empty Type 2 contour cannot be normalized.");
        }

        SfntPathCommand[] normalizedCommands =
            new SfntPathCommand[commands.Count + 1];
        int index = 0;

        while (index < commands.Count)
        {
            normalizedCommands[index] = commands[index];
            index += 1;
        }

        normalizedCommands[commands.Count] = SfntPathCommand.Create(
            SfntPathCommandKind.Close,
            0,
            0,
            0,
            0);
        return new SfntOutlineContour(
            points.ToArray(),
            normalizedCommands,
            0);
    }
}

internal class CffGlyphNormalizer
{
    SfntCffTable? table;
    SfntCff2Table? cff2Table;
    int glyphId;
    FontLimits limits;
    double[]? normalizedCoordinates;
    System.Collections.Generic.List<SfntOutlineContour> contours;
    System.Collections.Generic.List<SfntPathCommand> commands;
    CffContourAccumulator? currentContour;
    int normalizedPointCount;
    bool deprecatedOperator;

    CffGlyphNormalizer(
        SfntCffTable? table,
        SfntCff2Table? cff2Table,
        int glyphId,
        double[]? normalizedCoordinates,
        FontLimits limits)
    {
        this.table = table;
        this.cff2Table = cff2Table;
        this.glyphId = glyphId;
        this.normalizedCoordinates = normalizedCoordinates;
        this.limits = limits;
        contours = new System.Collections.Generic.List<SfntOutlineContour>();
        commands = new System.Collections.Generic.List<SfntPathCommand>();
    }

    static CffGlyphNormalizer Create(
        SfntCffTable table,
        int glyphId,
        FontLimits limits)
    {
        return new CffGlyphNormalizer(table, null, glyphId, null, limits);
    }

    static CffGlyphNormalizer Create(
        SfntCff2Table table,
        int glyphId,
        double[] normalizedCoordinates,
        FontLimits limits)
    {
        return new CffGlyphNormalizer(
            null,
            table,
            glyphId,
            normalizedCoordinates,
            limits);
    }

    internal static CffGlyph Normalize(
        SfntCffTable table,
        int glyphId,
        FontLimits limits)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        CffGlyphNormalizer normalizer = CffGlyphNormalizer.Create(
            table,
            glyphId,
            limits);
        return normalizer.Run();
    }

    internal static CffGlyph Normalize(
        SfntCff2Table table,
        int glyphId,
        double[] normalizedCoordinates,
        FontLimits limits)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (normalizedCoordinates == null)
        {
            throw new System.ArgumentNullException("normalizedCoordinates");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        CffGlyphNormalizer normalizer = CffGlyphNormalizer.Create(
            table,
            glyphId,
            normalizedCoordinates,
            limits);
        return normalizer.Run();
    }

    CffGlyph Run()
    {
        Type2CharStringResult result;
        if (table == null)
        {
            System.ArgumentNullException.ThrowIfNull(cff2Table);
            result = cff2Table.ExecuteCharString(glyphId, normalizedCoordinates);
        }
        else
        {
            result = table.ExecuteCharString(glyphId);
        }
        deprecatedOperator = result.UsesDeprecatedOperator();

        if (result.HasCompatibilityComposite())
        {
            AppendCompatibilityComposite(result);
        }
        else
        {
            AppendCommands(result, 0, 0);
        }

        FinishContour();
        SfntGlyphOutline outline = new SfntGlyphOutline(
            glyphId,
            contours.ToArray(),
            commands.ToArray(),
            normalizedPointCount,
            0);
        FontDiagnostic[] diagnostics = CreateDiagnostics();
        return new CffGlyph(
            glyphId,
            result.GetWidth(),
            outline,
            result,
            diagnostics);
    }

    void AppendCompatibilityComposite(Type2CharStringResult result)
    {
        System.ArgumentNullException.ThrowIfNull(table);
        CffCharset charset = table.GetCharset();

        if (charset.IsCidKeyed())
        {
            throw FontFormatException.Create(
                "A CID-keyed CFF font cannot use StandardEncoding composition.");
        }

        int baseGlyphId = ResolveStandardEncodingGlyph(
            charset,
            result.GetCompatibilityBaseCode());
        int accentGlyphId = ResolveStandardEncodingGlyph(
            charset,
            result.GetCompatibilityAccentCode());
        Type2CharStringResult baseResult =
            table.ExecuteCharString(baseGlyphId);
        Type2CharStringResult accentResult =
            table.ExecuteCharString(accentGlyphId);

        if (baseResult.HasCompatibilityComposite() ||
            accentResult.HasCompatibilityComposite())
        {
            throw FontFormatException.Create(
                "Type 2 compatibility composition must not be nested.");
        }

        deprecatedOperator = true;

        AppendCommands(baseResult, 0, 0);
        AppendCommands(
            accentResult,
            result.GetCompatibilityAccentX(),
            result.GetCompatibilityAccentY());
    }

    int ResolveStandardEncodingGlyph(CffCharset charset, int code)
    {
        int sid = CffPredefinedData.GetEncodingSid(0, code);

        if (sid == 0)
        {
            throw FontFormatException.Create(
                "A Type 2 compatibility component code is not in StandardEncoding.");
        }

        int componentGlyphId = charset.FindGlyphIndex(sid);

        if (componentGlyphId <= 0)
        {
            throw FontFormatException.Create(
                "A Type 2 compatibility component is absent from the charset.");
        }

        return componentGlyphId;
    }

    void AppendCommands(
        Type2CharStringResult result,
        double translateX,
        double translateY)
    {
        int index = 0;

        while (index < result.GetCommandCount())
        {
            Type2DrawingCommand command = result.GetCommand(index);
            Type2DrawingCommandKind kind = command.GetKind();

            if (kind == Type2DrawingCommandKind.Move)
            {
                FinishContour();
                currentContour = new CffContourAccumulator(
                    Translate(command.GetEndX(), translateX),
                    Translate(command.GetEndY(), translateY));
            }
            else
            {
                if (currentContour == null)
                {
                    throw FontFormatException.Create(
                        "A Type 2 normalized path segment has no contour.");
                }

                if (kind == Type2DrawingCommandKind.Line)
                {
                    currentContour.AddLine(
                        Translate(command.GetEndX(), translateX),
                        Translate(command.GetEndY(), translateY));
                }
                else
                {
                    currentContour.AddCubic(
                        Translate(command.GetControlX(), translateX),
                        Translate(command.GetControlY(), translateY),
                        Translate(command.GetSecondControlX(), translateX),
                        Translate(command.GetSecondControlY(), translateY),
                        Translate(command.GetEndX(), translateX),
                        Translate(command.GetEndY(), translateY));
                }
            }

            index += 1;
        }
    }

    void FinishContour()
    {
        if (currentContour == null)
        {
            return;
        }

        if (!currentContour.HasSegments())
        {
            currentContour = null;
            return;
        }

        if (contours.Count >= limits.GetMaxContoursPerGlyph())
        {
            throw FontLimitException.Create(
                "A normalized CFF glyph exceeds the configured contour limit.");
        }

        int pointCount = currentContour.GetPointCount();

        if (pointCount > limits.GetMaxPointsPerGlyph() -
            normalizedPointCount)
        {
            throw FontLimitException.Create(
                "A normalized CFF glyph exceeds the configured point limit.");
        }

        SfntOutlineContour contour = currentContour.CreateContour();
        contours.Add(contour);
        normalizedPointCount += pointCount;
        int commandIndex = 0;

        while (commandIndex < contour.GetCommandCount())
        {
            commands.Add(contour.GetCommand(commandIndex));
            commandIndex += 1;
        }

        currentContour = null;
    }

    FontDiagnostic[] CreateDiagnostics()
    {
        if (!deprecatedOperator)
        {
            return new FontDiagnostic[0];
        }

        string glyphText = glyphId.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        return new FontDiagnostic[]
        {
            new FontDiagnostic(
                FontDiagnosticSeverity.Information,
                FontDiagnosticCode.DeprecatedOutlineOperator,
                "The Type 2 charstring uses a deprecated compatibility operator.",
                -1,
                table == null ? FontTags.Cff2 : FontTags.Cff,
                -1,
                (table == null ? "CFF2.CharStrings[" : "CFF.CharStrings[") +
                    glyphText + "]")
        };
    }

    double Translate(double value, double offset)
    {
        double translated = value + offset;

        if (!double.IsFinite(translated))
        {
            throw FontFormatException.Create(
                "A translated Type 2 path coordinate is not finite.");
        }

        return translated;
    }
}
