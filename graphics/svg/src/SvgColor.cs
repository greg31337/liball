namespace svg;

/// <summary>Contains one parsed CSS functional color component.</summary>
internal class SvgColorComponent
{
    /// <summary>Whether the component parsed.</summary>
    public bool Success;
    /// <summary>The finite authored numeric value.</summary>
    public double Value;
    /// <summary>Whether the authored number was a percentage.</summary>
    public bool Percentage;
    /// <summary>Whether the component used the modern missing keyword.</summary>
    public bool Missing;
    /// <summary>The component failure kind.</summary>
    public SvgColorParseError Error;
    /// <summary>The component starting offset.</summary>
    public int Start;
    /// <summary>The exact failure offset.</summary>
    public int ErrorOffset;

    /// <summary>Creates a component result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="value">The numeric value.</param>
    /// <param name="percentage">Whether the value was a percentage.</param>
    /// <param name="missing">Whether the value was missing.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="start">The component starting offset.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgColorComponent(bool success, double value, bool percentage, bool missing, SvgColorParseError error, int start, int errorOffset)
    {
        Success = success;
        Value = value;
        Percentage = percentage;
        Missing = missing;
        Error = error;
        Start = start;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Maps CSS named colors to packed sRGB values.</summary>
internal static class SvgNamedColorTable
{
    const string Values = "|aliceblue=f0f8ff|antiquewhite=faebd7|aqua=00ffff|aquamarine=7fffd4|azure=f0ffff|beige=f5f5dc|bisque=ffe4c4|black=000000|blanchedalmond=ffebcd|blue=0000ff|blueviolet=8a2be2|brown=a52a2a|burlywood=deb887|cadetblue=5f9ea0|chartreuse=7fff00|chocolate=d2691e|coral=ff7f50|cornflowerblue=6495ed|cornsilk=fff8dc|crimson=dc143c|cyan=00ffff|darkblue=00008b|darkcyan=008b8b|darkgoldenrod=b8860b|darkgray=a9a9a9|darkgreen=006400|darkgrey=a9a9a9|darkkhaki=bdb76b|darkmagenta=8b008b|darkolivegreen=556b2f|darkorange=ff8c00|darkorchid=9932cc|darkred=8b0000|darksalmon=e9967a|darkseagreen=8fbc8f|darkslateblue=483d8b|darkslategray=2f4f4f|darkslategrey=2f4f4f|darkturquoise=00ced1|darkviolet=9400d3|deeppink=ff1493|deepskyblue=00bfff|dimgray=696969|dimgrey=696969|dodgerblue=1e90ff|firebrick=b22222|floralwhite=fffaf0|forestgreen=228b22|fuchsia=ff00ff|gainsboro=dcdcdc|ghostwhite=f8f8ff|gold=ffd700|goldenrod=daa520|gray=808080|green=008000|greenyellow=adff2f|grey=808080|honeydew=f0fff0|hotpink=ff69b4|indianred=cd5c5c|indigo=4b0082|ivory=fffff0|khaki=f0e68c|lavender=e6e6fa|lavenderblush=fff0f5|lawngreen=7cfc00|lemonchiffon=fffacd|lightblue=add8e6|lightcoral=f08080|lightcyan=e0ffff|lightgoldenrodyellow=fafad2|lightgray=d3d3d3|lightgreen=90ee90|lightgrey=d3d3d3|lightpink=ffb6c1|lightsalmon=ffa07a|lightseagreen=20b2aa|lightskyblue=87cefa|lightslategray=778899|lightslategrey=778899|lightsteelblue=b0c4de|lightyellow=ffffe0|lime=00ff00|limegreen=32cd32|linen=faf0e6|magenta=ff00ff|maroon=800000|mediumaquamarine=66cdaa|mediumblue=0000cd|mediumorchid=ba55d3|mediumpurple=9370db|mediumseagreen=3cb371|mediumslateblue=7b68ee|mediumspringgreen=00fa9a|mediumturquoise=48d1cc|mediumvioletred=c71585|midnightblue=191970|mintcream=f5fffa|mistyrose=ffe4e1|moccasin=ffe4b5|navajowhite=ffdead|navy=000080|oldlace=fdf5e6|olive=808000|olivedrab=6b8e23|orange=ffa500|orangered=ff4500|orchid=da70d6|palegoldenrod=eee8aa|palegreen=98fb98|paleturquoise=afeeee|palevioletred=db7093|papayawhip=ffefd5|peachpuff=ffdab9|peru=cd853f|pink=ffc0cb|plum=dda0dd|powderblue=b0e0e6|purple=800080|rebeccapurple=663399|red=ff0000|rosybrown=bc8f8f|royalblue=4169e1|saddlebrown=8b4513|salmon=fa8072|sandybrown=f4a460|seagreen=2e8b57|seashell=fff5ee|sienna=a0522d|silver=c0c0c0|skyblue=87ceeb|slateblue=6a5acd|slategray=708090|slategrey=708090|snow=fffafa|springgreen=00ff7f|steelblue=4682b4|tan=d2b48c|teal=008080|thistle=d8bfd8|tomato=ff6347|turquoise=40e0d0|violet=ee82ee|wheat=f5deb3|white=ffffff|whitesmoke=f5f5f5|yellow=ffff00|yellowgreen=9acd32|";

    /// <summary>Finds a lowercase CSS named color.</summary>
    /// <param name="name">The lowercase color name.</param>
    /// <returns>The packed RGB value, or <c>-1</c>.</returns>
    public static int Find(string name)
    {
        string marker = "|" + name + "=";
        int offset = Values.IndexOf(marker, System.StringComparison.Ordinal);
        if (offset < 0) return -1;
        int start = offset + marker.Length;
        int value = 0;
        int index = 0;
        while (index < 6)
        {
            value = value * 16 + SvgSpanCharacters.HexValue(Values[start + index]);
            index = index + 1;
        }
        return value;
    }
}
