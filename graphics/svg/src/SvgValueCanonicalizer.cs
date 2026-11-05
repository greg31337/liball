namespace svg;

internal static class SvgValueCanonicalizer
{
    internal static void Run(SvgProcessContext context, SvgCanonicalizeOptions options)
    {
        if (!Valid(options))
        {
            context.Fail("Canonicalization settings are invalid.");
            return;
        }
        SvgTypedAttributeAccessor accessor = new SvgTypedAttributeAccessor();
        SvgPropertyCatalog properties = SvgPropertyCatalog.CreateDefault();
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[nodeIndex];
            if (node.Kind == SvgNodeKind.Element && object.ReferenceEquals(node.OwnerDocument, context.Document)) CanonicalizeNode(context, node, accessor, properties, options);
            nodeIndex = nodeIndex + 1;
        }
    }

    internal static bool Valid(SvgCanonicalizeOptions? options)
    {
        return options != null && Valid(options.Numbers) && Valid(options.Colors) && Valid(options.Lists) && Valid(options.Paths) && Valid(options.Transforms);
    }

    internal static bool Valid(SvgValueFormatOptions options)
    {
        return options != null && options.Mode >= SvgValueFormatMode.Canonical && options.Mode <= SvgValueFormatMode.Compact && options.FractionalDigits >= 0 && options.FractionalDigits <= SvgNumberFormatter.MaximumFixedPrecision && SvgFiniteMath.IsFinite(options.MaximumAbsoluteError) && options.MaximumAbsoluteError >= 0;
    }

    static void CanonicalizeNode(SvgProcessContext context, SvgNode node, SvgTypedAttributeAccessor accessor, SvgPropertyCatalog properties, SvgCanonicalizeOptions options)
    {
        System.Collections.Generic.List<SvgAttribute> attributes = new System.Collections.Generic.List<SvgAttribute>(node.Attributes);
        int index = 0;
        while (index < attributes.Count && !context.IsFailed())
        {
            SvgAttribute attribute = attributes[index];
            string? formatted = FormatAttribute(node, attribute, accessor, properties, options);
            if (formatted != null && formatted != attribute.Value) ReplaceAttribute(context, node, attribute, formatted);
            index = index + 1;
        }
    }

    internal static string? FormatAttribute(SvgNode? node, SvgAttribute? attribute, SvgCanonicalizeOptions? options)
    {
        if (node == null || attribute == null || !Valid(options)) return null;
        return FormatAttribute(node, attribute, new SvgTypedAttributeAccessor(), SvgPropertyCatalog.CreateDefault(), options);
    }

    static string? FormatAttribute(SvgNode node, SvgAttribute attribute, SvgTypedAttributeAccessor accessor, SvgPropertyCatalog properties, SvgCanonicalizeOptions? options)
    {
        if (attribute.NamespaceUri.Length != 0) return null;
        SvgPropertySupportEntry? property = properties.FindPresentationAttribute(attribute.LocalName);
        string? formatted = null;
        if (property != null && (property.ValueKind == SvgPropertyValueSupportKind.Color || property.ValueKind == SvgPropertyValueSupportKind.Paint)) formatted = Color(attribute.Value, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Colors);
        if (formatted != null) return formatted;
        SvgTypedAttributeValue typed = accessor.Get(node, attribute.LocalName);
        if (typed.Success && typed.Origin == SvgTypedAttributeOrigin.Authored) return Typed(typed, options);
        return null;
    }

    static string? Typed(SvgTypedAttributeValue typed, SvgCanonicalizeOptions? options)
    {
        if (typed.ValueKind == SvgTypedAttributeKind.Number) return Number((double)(typed.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Numbers);
        if (typed.ValueKind == SvgTypedAttributeKind.Integer) return ((int)(typed.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (typed.ValueKind == SvgTypedAttributeKind.Length)
        {
            SvgLength? length = (SvgLength?)typed.Value;
            return Number((length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Numbers) + SvgLengthUnits.GetText(length.Unit);
        }
        if (typed.ValueKind == SvgTypedAttributeKind.Angle)
        {
            SvgAngle? angle = (SvgAngle?)typed.Value;
            return Number((angle ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Numbers) + SvgScalarUnitNames.AngleText(angle.Unit);
        }
        if (typed.ValueKind == SvgTypedAttributeKind.PathLength) return Number((((SvgPathLength?)typed.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))).Value, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Numbers);
        if (typed.ValueKind == SvgTypedAttributeKind.LengthList) return LengthList((SvgLengthListValue?)typed.Value, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Lists);
        if (typed.ValueKind == SvgTypedAttributeKind.AngleList) return AngleList((System.Collections.Generic.List<SvgRotateItem>?)typed.Value, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Lists);
        if (typed.ValueKind == SvgTypedAttributeKind.NumberList) return DoubleList((double[]?)typed.Value, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Lists, false);
        if (typed.ValueKind == SvgTypedAttributeKind.IntegerList) return IntegerList((int[]?)typed.Value, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Lists);
        if (typed.ValueKind == SvgTypedAttributeKind.Points) return DoubleList((double[]?)typed.Value, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Lists, true);
        if (typed.ValueKind == SvgTypedAttributeKind.ViewBox)
        {
            SvgViewBox? box = (SvgViewBox?)typed.Value;
            return Join(new double[] { (box ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, box.Y, box.Width, box.Height }, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Lists, false);
        }
        if (typed.ValueKind == SvgTypedAttributeKind.PathData) return Path((SvgPathData?)typed.Value, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Paths);
        if (typed.ValueKind == SvgTypedAttributeKind.TransformList) return TransformList((SvgTransformList?)typed.Value, (options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transforms);
        return null;
    }

    internal static string? Number(double value, SvgValueFormatOptions options)
    {
        if (options.UseFractionalPrecision)
        {
            SvgNumberFormatResult rounded = SvgNumberFormatter.FormatFixedTrimmed(value, options.FractionalDigits);
            double parsed;
            if (rounded.Success && double.TryParse(rounded.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed) && System.Math.Abs(value - parsed) <= options.MaximumAbsoluteError) return options.Mode == SvgValueFormatMode.Compact ? CompactFraction(rounded.Text) : rounded.Text;
        }
        SvgNumberFormatResult exact = options.Mode == SvgValueFormatMode.Compact ? SvgNumberFormatter.FormatCompact(value) : SvgNumberFormatter.FormatCanonical(value);
        return exact.Success ? exact.Text : null;
    }

    static string LengthList(SvgLengthListValue? value, SvgValueFormatOptions options)
    {
        if ((value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Keyword != SvgListKeyword.Values) return value.ToText();
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Items.Count)
        {
            if (index > 0) builder.Append(options.Mode == SvgValueFormatMode.Compact ? "," : " ");
            SvgLength length = value.Items[index].Length;
            builder.Append(Number(length.Value, options));
            builder.Append(SvgLengthUnits.GetText(length.Unit));
            index = index + 1;
        }
        return builder.ToString();
    }

    static string AngleList(System.Collections.Generic.List<SvgRotateItem>? items, SvgValueFormatOptions options)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < (items ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count)
        {
            if (index > 0) builder.Append(options.Mode == SvgValueFormatMode.Compact ? "," : " ");
            builder.Append(Number(items[index].Angle.Value, options));
            index = index + 1;
        }
        return builder.ToString();
    }

    static string DoubleList(double[]? values, SvgValueFormatOptions options, bool points)
    {
        return Join(values, options, points);
    }

    static string Join(double[]? values, SvgValueFormatOptions options, bool points)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < (values ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length)
        {
            if (index > 0)
            {
                bool pair = points && index % 2 == 0;
                builder.Append(options.Mode == SvgValueFormatMode.Compact && !pair ? "," : " ");
            }
            builder.Append(Number(values[index], options));
            index = index + 1;
        }
        return builder.ToString();
    }

    static string IntegerList(int[]? values, SvgValueFormatOptions options)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < (values ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length)
        {
            if (index > 0) builder.Append(options.Mode == SvgValueFormatMode.Compact ? "," : " ");
            builder.Append(values[index].ToString(System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }
        return builder.ToString();
    }

    static string? Path(SvgPathData? path, SvgValueFormatOptions options)
    {
        bool rounded = options.UseFractionalPrecision && CanRoundPath(path, options);
        SvgPathFormatOptions format = SvgPathFormatOptions.Create(options.Mode == SvgValueFormatMode.Compact ? SvgPathFormatMode.Minified : SvgPathFormatMode.Canonical, rounded, options.FractionalDigits);
        SvgPathFormatResult result = new SvgPathFormatter().Format((path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), format);
        return result.Success ? result.Text : null;
    }

    static bool CanRoundPath(SvgPathData? path, SvgValueFormatOptions options)
    {
        int commandIndex = 0;
        while (commandIndex < (path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count)
        {
            double[] values = path.Commands[commandIndex].Values;
            int index = 0;
            while (index < values.Length)
            {
                if (!CanRound(values[index], options)) return false;
                index = index + 1;
            }
            commandIndex = commandIndex + 1;
        }
        return true;
    }

    static string TransformList(SvgTransformList? list, SvgValueFormatOptions options)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < (list ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Items.Count)
        {
            if (index > 0) builder.Append(options.Mode == SvgValueFormatMode.Compact ? ',' : ' ');
            SvgTransform item = list.Items[index];
            builder.Append(TransformName(item.Kind));
            builder.Append('(');
            int count = TransformValueCount(item, options.Mode == SvgValueFormatMode.Compact);
            int valueIndex = 0;
            while (valueIndex < count)
            {
                if (valueIndex > 0) builder.Append(options.Mode == SvgValueFormatMode.Compact ? "," : " ");
                builder.Append(Number(item.Values[valueIndex], options));
                valueIndex = valueIndex + 1;
            }
            builder.Append(')');
            index = index + 1;
        }
        return builder.ToString();
    }

    static int TransformValueCount(SvgTransform transform, bool compact)
    {
        if (!compact) return transform.Values.Length;
        if (transform.Kind == SvgTransformKind.Translate && transform.Values.Length == 2 && transform.Values[1] == 0) return 1;
        if (transform.Kind == SvgTransformKind.Scale && transform.Values.Length == 2 && transform.Values[0] == transform.Values[1]) return 1;
        if (transform.Kind == SvgTransformKind.Rotate && transform.Values.Length == 3 && transform.Values[1] == 0 && transform.Values[2] == 0) return 1;
        return transform.Values.Length;
    }

    static string TransformName(SvgTransformKind kind)
    {
        if (kind == SvgTransformKind.Matrix) return "matrix";
        if (kind == SvgTransformKind.Translate) return "translate";
        if (kind == SvgTransformKind.Scale) return "scale";
        if (kind == SvgTransformKind.Rotate) return "rotate";
        if (kind == SvgTransformKind.SkewX) return "skewX";
        return "skewY";
    }

    internal static string? Color(string value, SvgValueFormatOptions options)
    {
        SvgColorParseResult parsed = new SvgColorParser().Parse(value, 4096);
        if (!parsed.Success) return null;
        SvgColor? color = parsed.Color;
        if ((color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IsCurrentColor) return "currentColor";
        SvgColor formatted = RoundedColor(color, options);
        string canonical = formatted.ToText();
        if (options.Mode != SvgValueFormatMode.Compact || formatted.Space != SvgColorSpace.Srgb || formatted.FirstMissing || formatted.SecondMissing || formatted.ThirdMissing || formatted.AlphaMissing) return canonical;
        string? hex = Hex(color, options);
        return hex != null && hex.Length < canonical.Length ? hex : canonical;
    }

    static SvgColor RoundedColor(SvgColor color, SvgValueFormatOptions options)
    {
        if (!options.UseFractionalPrecision || !CanRound(color.Red, options) || !CanRound(color.Green, options) || !CanRound(color.Blue, options) || !CanRound(color.Alpha, options)) return color;
        SvgColor result = new SvgColor(color.Space, RoundedValue(color.Red, options.FractionalDigits), RoundedValue(color.Green, options.FractionalDigits), RoundedValue(color.Blue, options.FractionalDigits), RoundedValue(color.Alpha, options.FractionalDigits));
        result.FirstMissing = color.FirstMissing;
        result.SecondMissing = color.SecondMissing;
        result.ThirdMissing = color.ThirdMissing;
        result.AlphaMissing = color.AlphaMissing;
        return result;
    }

    static string? Hex(SvgColor color, SvgValueFormatOptions options)
    {
        int red = Byte(color.Red);
        int green = Byte(color.Green);
        int blue = Byte(color.Blue);
        int alpha = Byte(color.Alpha);
        double tolerance = options.UseFractionalPrecision ? options.MaximumAbsoluteError : 0;
        if (red < 0 || green < 0 || blue < 0 || alpha < 0 || System.Math.Abs(color.Red - red / 255.0) > tolerance || System.Math.Abs(color.Green - green / 255.0) > tolerance || System.Math.Abs(color.Blue - blue / 255.0) > tolerance || System.Math.Abs(color.Alpha - alpha / 255.0) > tolerance) return null;
        string text = red.ToString("x2", System.Globalization.CultureInfo.InvariantCulture) + green.ToString("x2", System.Globalization.CultureInfo.InvariantCulture) + blue.ToString("x2", System.Globalization.CultureInfo.InvariantCulture);
        if (alpha != 255) text = text + alpha.ToString("x2", System.Globalization.CultureInfo.InvariantCulture);
        bool shortForm = text[0] == text[1] && text[2] == text[3] && text[4] == text[5] && (text.Length == 6 || text[6] == text[7]);
        if (shortForm) text = text[0].ToString() + text[2].ToString() + text[4].ToString() + (text.Length == 8 ? text[6].ToString() : "");
        return "#" + text;
    }

    static int Byte(double value)
    {
        if (value < 0 || value > 1) return -1;
        return (int)System.Math.Round(value * 255, System.MidpointRounding.AwayFromZero);
    }

    static bool CanRound(double value, SvgValueFormatOptions options)
    {
        return System.Math.Abs(value - RoundedValue(value, options.FractionalDigits)) <= options.MaximumAbsoluteError;
    }

    static double RoundedValue(double value, int digits)
    {
        SvgNumberFormatResult formatted = SvgNumberFormatter.FormatFixedTrimmed(value, digits);
        double rounded;
        if (!formatted.Success || !double.TryParse(formatted.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rounded)) return value;
        return rounded;
    }

    static string CompactFraction(string text)
    {
        if (text.Length > 1 && text[0] == '0' && text[1] == '.') return text.Substring(1);
        if (text.Length > 2 && text[0] == '-' && text[1] == '0' && text[2] == '.') return "-" + text.Substring(2);
        return text;
    }

    static void ReplaceAttribute(SvgProcessContext context, SvgNode node, SvgAttribute current, string value)
    {
        SvgAttribute replacement = context.Documents.CreateAttribute(current.QualifiedName, current.Prefix, current.LocalName, current.NamespaceUri, value);
        SvgMutationResult mutation = context.Mutations.ReplaceAttribute(node, current, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static System.Collections.Generic.List<SvgNode> CollectNodes(SvgDocument document)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(document.DocumentNode);
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            result.Add(node);
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(node.Children[index]);
                index = index - 1;
            }
        }
        return result;
    }
}
