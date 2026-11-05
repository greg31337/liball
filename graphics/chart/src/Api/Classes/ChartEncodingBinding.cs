namespace chart;
/// <summary>
/// Represents an encoding binding used by the chart model or rendering pipeline.
/// </summary>
public class ChartEncodingBinding : IChartEncodingBinding
{
    internal ChartEncodingChannel Channel;
    internal string FieldName;
    internal ChartDataKind FieldKind;
    internal bool IsConstant;
    internal double NumberValue;
    internal string TextValue;
    internal ChartColor ColorValue;
    internal ChartMarkerShape ShapeValue;
    /// <summary>
    /// Initializes an encoding binding from the supplied channel, field name, and field kind.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    public ChartEncodingBinding(ChartEncodingChannel channel, string fieldName, ChartDataKind fieldKind)
    {
        ValidateChannel(channel);
        ChartDataColumn.ValidateName(fieldName);
        Channel = channel;
        FieldName = fieldName;
        FieldKind = fieldKind;
        IsConstant = false;
        NumberValue = 0.0;
        TextValue = "";
        ColorValue = ChartColor.Black();
        ShapeValue = ChartMarkerShape.Circle;
    }

    internal static ChartEncodingBinding CreateNumberConstant(ChartEncodingChannel channel, double value)
    {
        ValidateChannel(channel);
        ChartLinearScale.ValidateFinite(value, "value");
        ChartEncodingBinding binding = new ChartEncodingBinding(channel, ChannelName(channel), ChartDataKind.Number);
        binding.IsConstant = true;
        binding.NumberValue = value;
        return binding;
    }

    internal static ChartEncodingBinding CreateTextConstant(ChartEncodingChannel channel, string value)
    {
        ValidateChannel(channel);
        ChartTextBlock.ValidateText(value, "value");
        ChartEncodingBinding binding = new ChartEncodingBinding(channel, ChannelName(channel), ChartDataKind.Text);
        binding.IsConstant = true;
        binding.TextValue = value;
        return binding;
    }

    internal static ChartEncodingBinding CreateColorConstant(ChartEncodingChannel channel, ChartColor value)
    {
        ValidateChannel(channel);
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        ChartEncodingBinding binding = new ChartEncodingBinding(channel, ChannelName(channel), ChartDataKind.Text);
        binding.IsConstant = true;
        binding.ColorValue = value.Copy();
        return binding;
    }

    internal static ChartEncodingBinding CreateShapeConstant(ChartEncodingChannel channel, ChartMarkerShape value)
    {
        ValidateChannel(channel);
        ChartEncodingBinding binding = new ChartEncodingBinding(channel, ChannelName(channel), ChartDataKind.Text);
        binding.IsConstant = true;
        binding.ShapeValue = value;
        return binding;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartEncodingBinding Copy()
    {
        ChartEncodingBinding binding = new ChartEncodingBinding(Channel, FieldName, FieldKind);
        binding.IsConstant = IsConstant;
        binding.NumberValue = NumberValue;
        binding.TextValue = TextValue;
        binding.ColorValue = ColorValue.Copy();
        binding.ShapeValue = ShapeValue;
        return binding;
    }

    internal static void ValidateChannel(ChartEncodingChannel channel)
    {
        int value = (int)channel;
        if (value < 0 || value > 24)
        {
            throw new System.ArgumentOutOfRangeException("channel");
        }
    }

    internal static string ChannelName(ChartEncodingChannel channel)
    {
        ValidateChannel(channel);
        if (channel == ChartEncodingChannel.X)
        {
            return "x";
        }

        if (channel == ChartEncodingChannel.Y)
        {
            return "y";
        }

        if (channel == ChartEncodingChannel.X2)
        {
            return "x2";
        }

        if (channel == ChartEncodingChannel.Y2)
        {
            return "y2";
        }

        if (channel == ChartEncodingChannel.Color)
        {
            return "color";
        }

        if (channel == ChartEncodingChannel.Size)
        {
            return "size";
        }

        if (channel == ChartEncodingChannel.Shape)
        {
            return "shape";
        }

        if (channel == ChartEncodingChannel.Opacity)
        {
            return "opacity";
        }

        if (channel == ChartEncodingChannel.Text)
        {
            return "text";
        }

        if (channel == ChartEncodingChannel.Tooltip)
        {
            return "tooltip";
        }

        if (channel == ChartEncodingChannel.Order)
        {
            return "order";
        }

        if (channel == ChartEncodingChannel.Detail)
        {
            return "detail";
        }

        if (channel == ChartEncodingChannel.Row)
        {
            return "row";
        }

        if (channel == ChartEncodingChannel.Column)
        {
            return "column";
        }

        if (channel == ChartEncodingChannel.Radius)
        {
            return "radius";
        }

        if (channel == ChartEncodingChannel.Angle)
        {
            return "angle";
        }

        if (channel == ChartEncodingChannel.Latitude)
        {
            return "latitude";
        }

        if (channel == ChartEncodingChannel.Longitude)
        {
            return "longitude";
        }

        if (channel == ChartEncodingChannel.StrokeColor)
        {
            return "strokeColor";
        }

        if (channel == ChartEncodingChannel.FillColor)
        {
            return "fillColor";
        }

        if (channel == ChartEncodingChannel.StrokeWidth)
        {
            return "strokeWidth";
        }

        if (channel == ChartEncodingChannel.Dash)
        {
            return "dash";
        }

        if (channel == ChartEncodingChannel.ValueLabel)
        {
            return "valueLabel";
        }

        if (channel == ChartEncodingChannel.ZIndex)
        {
            return "zIndex";
        }

        return "wrap";
    }

    /// <summary>Returns the current channel.</summary>
    /// <returns>The current field value.</returns>
    public ChartEncodingChannel GetChannel()
    {
        return Channel;
    }

    /// <summary>Replaces the current channel.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetChannel(ChartEncodingChannel value)
    {
        Channel = value;
    }

    /// <summary>Returns the current field name.</summary>
    /// <returns>The current field value.</returns>
    public string GetFieldName()
    {
        return FieldName;
    }

    /// <summary>Replaces the current field name.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetFieldName(string value)
    {
        FieldName = value;
    }

    /// <summary>Returns the current field kind.</summary>
    /// <returns>The current field value.</returns>
    public ChartDataKind GetFieldKind()
    {
        return FieldKind;
    }

    /// <summary>Replaces the current field kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetFieldKind(ChartDataKind value)
    {
        FieldKind = value;
    }

    /// <summary>Returns the current is constant.</summary>
    /// <returns>The current field value.</returns>
    public bool GetIsConstant()
    {
        return IsConstant;
    }

    /// <summary>Replaces the current is constant.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetIsConstant(bool value)
    {
        IsConstant = value;
    }

    /// <summary>Returns the current number value.</summary>
    /// <returns>The current field value.</returns>
    public double GetNumberValue()
    {
        return NumberValue;
    }

    /// <summary>Replaces the current number value.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetNumberValue(double value)
    {
        NumberValue = value;
    }

    /// <summary>Returns the current text value.</summary>
    /// <returns>The current field value.</returns>
    public string GetTextValue()
    {
        return TextValue;
    }

    /// <summary>Replaces the current text value.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTextValue(string value)
    {
        TextValue = value;
    }

    /// <summary>Returns the current color value.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetColorValue()
    {
        return ColorValue;
    }

    /// <summary>Replaces the current color value.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColorValue(ChartColor value)
    {
        ColorValue = value;
    }

    /// <summary>Returns the current shape value.</summary>
    /// <returns>The current field value.</returns>
    public ChartMarkerShape GetShapeValue()
    {
        return ShapeValue;
    }

    /// <summary>Replaces the current shape value.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetShapeValue(ChartMarkerShape value)
    {
        ShapeValue = value;
    }
}
