namespace svg;

internal class SvgCascadeSheetInput
{
    internal SvgStyleSheet? StyleSheet;

    internal SvgCascadeSheetInput(SvgStyleSheet? styleSheet)
    {
        StyleSheet = styleSheet;
    }
}

internal class SvgCascadeTextResult
{
    internal bool Success;
    internal string Text;

    internal SvgCascadeTextResult(bool success, string text)
    {
        Success = success;
        Text = text;
    }
}

internal class SvgCascadeLayerRecord
{
    internal SvgStyleOrigin Origin;
    internal string Name;
    internal int Order;

    internal SvgCascadeLayerRecord(SvgStyleOrigin origin, string name, int order)
    {
        Origin = origin;
        Name = name;
        Order = order;
    }
}

internal class SvgCascadeExclusion
{
    internal SvgStyleOrigin Origin;
    internal bool Important;
    internal string LayerKey;
    internal bool LayerOnly;

    internal SvgCascadeExclusion(SvgStyleOrigin origin, bool important, string layerKey, bool layerOnly)
    {
        Origin = origin;
        Important = important;
        LayerKey = layerKey;
        LayerOnly = layerOnly;
    }
}

internal class SvgCascadeContext
{
    internal SvgNode Node;
    internal SvgCascadeOptions? Options;
    internal System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    internal System.Collections.Generic.List<SvgCascadeCandidate> Candidates;
    internal System.Collections.Generic.List<SvgCascadeLayerRecord> Layers;
    internal SvgCapabilitySet Capabilities;
    internal long RemainingSelectorWork;
    internal long NextSourceOrder;
    internal bool Success;

    internal SvgCascadeContext(SvgNode node, SvgCascadeOptions? options, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        Node = node;
        Options = options;
        Diagnostics = diagnostics;
        Candidates = new System.Collections.Generic.List<SvgCascadeCandidate>();
        Layers = new System.Collections.Generic.List<SvgCascadeLayerRecord>();
        Capabilities = new SvgCapabilitySet();
        Capabilities.Add(SvgFeatureIdentifiers.CssParsing);
        RemainingSelectorWork = (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SelectorOptions.MaxWork;
        NextSourceOrder = 0;
        Success = true;
    }

    internal int RegisterLayer(SvgStyleOrigin origin, string name)
    {
        int found = LayerOrder(origin, name);
        if (found >= 0) return found;
        int order = 0;
        int index = 0;
        while (index < Layers.Count)
        {
            if (Layers[index].Origin == origin) order = order + 1;
            index = index + 1;
        }
        Layers.Add(new SvgCascadeLayerRecord(origin, name, order));
        return order;
    }

    internal int LayerOrder(SvgStyleOrigin origin, string name)
    {
        int index = 0;
        while (index < Layers.Count)
        {
            SvgCascadeLayerRecord layer = Layers[index];
            if (layer.Origin == origin && string.Equals(layer.Name, name, System.StringComparison.Ordinal)) return layer.Order;
            index = index + 1;
        }
        return -1;
    }
}
