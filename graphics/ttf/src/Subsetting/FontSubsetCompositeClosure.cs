namespace ttf;

internal class FontSubsetCompositeClosure
{
    int[] glyphIds;
    FontDiagnostic[] diagnostics;

    internal FontSubsetCompositeClosure(
        int[] glyphIds,
        FontDiagnostic[] diagnostics)
    {
        this.glyphIds = CopyIntegers(glyphIds);
        this.diagnostics = CopyDiagnostics(diagnostics);
    }

    internal int[] CopyGlyphIds()
    {
        return CopyIntegers(glyphIds);
    }

    internal FontDiagnostic[] CopyDiagnostics()
    {
        return CopyDiagnostics(diagnostics);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static FontDiagnostic[] CopyDiagnostics(FontDiagnostic[] values)
    {
        FontDiagnostic[] copy = new FontDiagnostic[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}

internal class FontSubsetCompositeClosureBuilder
{
    FontInstance instance;
    SfntLocaTable loca;
    int maximumEntries;
    int configuredMaximumDepth;
    int declaredMaximumDepth;
    int[] states;
    bool[] cycleReported;
    bool[] depthReported;
    System.Collections.Generic.List<int> glyphIds;
    System.Collections.Generic.HashSet<int> glyphIdSet;
    System.Collections.Generic.List<FontDiagnostic> diagnostics;

    FontSubsetCompositeClosureBuilder(
        FontInstance instance,
        SfntLocaTable loca,
        int maximumEntries,
        int configuredMaximumDepth,
        int declaredMaximumDepth)
    {
        this.instance = instance;
        this.loca = loca;
        this.maximumEntries = maximumEntries;
        this.configuredMaximumDepth = configuredMaximumDepth;
        this.declaredMaximumDepth = declaredMaximumDepth;
        states = new int[instance.GetGlyphCount()];
        cycleReported = new bool[instance.GetGlyphCount()];
        depthReported = new bool[instance.GetGlyphCount()];
        glyphIds = new System.Collections.Generic.List<int>();
        glyphIdSet = new System.Collections.Generic.HashSet<int>();
        diagnostics = new System.Collections.Generic.List<FontDiagnostic>();
    }

    internal static FontSubsetCompositeClosure Create(
        FontInstance instance,
        int[] initialGlyphIds,
        int maximumEntries)
    {
        if (instance.GetFace().GetOutlineKind() != FontOutlineKind.TrueType)
        {
            return new FontSubsetCompositeClosure(
                initialGlyphIds,
                new FontDiagnostic[0]);
        }

        FontFace face = instance.GetFace();
        SfntLocaTable? loca = face.GetLocaTable();

        if (loca == null)
        {
            throw FontFormatException.Create(
                "TrueType subset closure requires a supported loca table.");
        }

        FontLimits limits = face.GetTableCache().GetLimits();
        SfntMaxpTable? maxp = face.GetMaxpTable();
        System.ArgumentNullException.ThrowIfNull(maxp);
        FontSubsetCompositeClosureBuilder builder =
            new FontSubsetCompositeClosureBuilder(
                instance,
                loca,
                maximumEntries,
                limits.GetMaxComponentDepth(),
                maxp.GetMaxComponentDepth());
        return builder.Build(initialGlyphIds);
    }

    FontSubsetCompositeClosure Build(int[] initialGlyphIds)
    {
        int index = 0;

        while (index < initialGlyphIds.Length)
        {
            AddGlyph(initialGlyphIds[index]);
            index++;
        }

        index = 0;
        while (index < initialGlyphIds.Length)
        {
            VisitGlyph(initialGlyphIds[index], 1);
            index++;
        }

        int[] sortedGlyphIds = glyphIds.ToArray();
        System.Array.Sort(sortedGlyphIds);
        return new FontSubsetCompositeClosure(
            sortedGlyphIds,
            diagnostics.ToArray());
    }

    void VisitGlyph(int glyphId, int nestingLevel)
    {
        if (states[glyphId] == 2)
        {
            return;
        }

        states[glyphId] = 1;
        SfntCompositeGlyph? composite = loca.GetCompositeGlyph(glyphId);

        if (composite == null)
        {
            states[glyphId] = 2;
            return;
        }

        if (nestingLevel > configuredMaximumDepth)
        {
            AddDepthDiagnostic(glyphId);
            states[glyphId] = 2;
            return;
        }

        if (nestingLevel > declaredMaximumDepth)
        {
            AddDepthDiagnostic(glyphId);
        }

        int componentIndex = 0;

        while (componentIndex < composite.GetComponentCount())
        {
            int childGlyphId = composite
                .GetComponent(componentIndex)
                .GetGlyphId();
            AddGlyph(childGlyphId);

            if (states[childGlyphId] == 1)
            {
                AddCycleDiagnostic(
                    glyphId,
                    componentIndex,
                    childGlyphId);
            }
            else
            {
                VisitGlyph(childGlyphId, nestingLevel + 1);
            }

            componentIndex++;
        }

        states[glyphId] = 2;
    }

    void AddGlyph(int glyphId)
    {
        if (!glyphIdSet.Contains(glyphId))
        {
            if (glyphIds.Count >= maximumEntries)
            {
                throw FontLimitException.Create(
                    "TrueType component closure exceeds the configured entry limit.");
            }

            glyphIdSet.Add(glyphId);
            glyphIds.Add(glyphId);
        }
    }

    void AddCycleDiagnostic(
        int glyphId,
        int componentIndex,
        int childGlyphId)
    {
        if (cycleReported[childGlyphId])
        {
            return;
        }

        cycleReported[childGlyphId] = true;
        diagnostics.Add(new FontDiagnostic(
            FontDiagnosticSeverity.Error,
            FontDiagnosticCode.CompositeCycle,
            "A TrueType composite component cycle reaches glyph " +
                childGlyphId + " from glyph " + glyphId + ".",
            instance.GetFace().GetFaceIndex(),
            FontTags.Glyf,
            -1,
            "subset.glyf.glyph[" + glyphId + "].component[" +
                componentIndex + "]"));
    }

    void AddDepthDiagnostic(int glyphId)
    {
        if (depthReported[glyphId])
        {
            return;
        }

        depthReported[glyphId] = true;
        diagnostics.Add(new FontDiagnostic(
            FontDiagnosticSeverity.Error,
            FontDiagnosticCode.CompositeDepthExceeded,
            "TrueType component closure exceeds the permitted depth at glyph " +
                glyphId + ".",
            instance.GetFace().GetFaceIndex(),
            FontTags.Glyf,
            -1,
            "subset.glyf.glyph[" + glyphId + "]"));
    }
}
