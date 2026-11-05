namespace chart;
/// <summary>
/// Represents a visual encoding used by the chart model or rendering pipeline.
/// </summary>
public class ChartVisualEncoding : IChartVisualEncoding
{
    internal ChartEncodingBinding[] Bindings;
    internal int BindingCount;
    internal ChartConditionalEncoding[] Conditions;
    internal int ConditionCount;
    internal ChartSelection? Selection;
    /// <summary>
    /// Initializes an empty visual encoding with library defaults.
    /// </summary>
    public ChartVisualEncoding()
    {
        Bindings = new ChartEncodingBinding[8];
        BindingCount = 0;
        Conditions = new ChartConditionalEncoding[4];
        ConditionCount = 0;
        Selection = null;
    }

    /// <summary>
    /// Sets the field configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetField(ChartEncodingChannel channel, string fieldName, ChartDataKind fieldKind)
    {
        ChartEncodingBinding binding = new ChartEncodingBinding(channel, fieldName, fieldKind);
        return SetBinding(binding);
    }

    /// <summary>
    /// Sets the x configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetX(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.X, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the y configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetY(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Y, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the x2 configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetX2(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.X2, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the y2 configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetY2(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Y2, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the color configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetColor(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Color, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the size configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetSize(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Size, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the shape configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetShape(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Shape, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the opacity configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetOpacity(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Opacity, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the text configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetText(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Text, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the tooltip configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetTooltip(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Tooltip, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the order configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetOrder(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Order, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the detail configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetDetail(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Detail, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the row configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetRow(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Row, fieldName, fieldKind);
    }

    /// <summary>
    /// Sets the column configuration.
    /// </summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetColumn(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Column, fieldName, fieldKind);
    }

    /// <summary>Sets the radial position field.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetRadius(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Radius, fieldName, fieldKind);
    }

    /// <summary>Sets the angular position field in radians.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetAngle(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Angle, fieldName, fieldKind);
    }

    /// <summary>Sets the latitude position field.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetLatitude(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Latitude, fieldName, fieldKind);
    }

    /// <summary>Sets the longitude position field.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetLongitude(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Longitude, fieldName, fieldKind);
    }

    /// <summary>Sets the stroke-color field.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetStrokeColor(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.StrokeColor, fieldName, fieldKind);
    }

    /// <summary>Sets the fill-color field.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetFillColor(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.FillColor, fieldName, fieldKind);
    }

    /// <summary>Sets the stroke-width field.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetStrokeWidth(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.StrokeWidth, fieldName, fieldKind);
    }

    /// <summary>Sets the dash-length field.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetDash(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Dash, fieldName, fieldKind);
    }

    /// <summary>Sets the value-label field.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetValueLabel(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.ValueLabel, fieldName, fieldKind);
    }

    /// <summary>Sets the z-index field.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetZIndex(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.ZIndex, fieldName, fieldKind);
    }

    /// <summary>Sets the wrap-facet field.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "fieldKind">The field kind.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetWrap(string fieldName, ChartDataKind fieldKind)
    {
        return SetField(ChartEncodingChannel.Wrap, fieldName, fieldKind);
    }

    /// <summary>Attaches live selection and highlight state to this encoding.</summary>
    /// <param name="selection">The live selection state.</param>
    public void SetSelection(ChartSelection selection)
    {
        if (selection == null)
        {
            throw new System.ArgumentNullException("selection");
        }

        Selection = selection;
    }

    /// <summary>Removes the attached selection state.</summary>
    public void ClearSelection()
    {
        Selection = null;
    }

    /// <summary>Adds or replaces a conditional encoding for one channel.</summary>
    /// <param name="condition">The conditional encoding.</param>
    /// <returns>The stored conditional encoding.</returns>
    public ChartConditionalEncoding SetConditionalEncoding(ChartConditionalEncoding condition)
    {
        if (condition == null)
        {
            throw new System.ArgumentNullException("condition");
        }

        int index = ConditionalEncodingIndex(condition.Channel);
        if (index >= 0)
        {
            Conditions[index] = condition.Copy();
            return Conditions[index];
        }

        EnsureConditionCapacity(ConditionCount + 1);
        Conditions[ConditionCount] = condition.Copy();
        ConditionCount = ConditionCount + 1;
        return Conditions[ConditionCount - 1];
    }

    /// <summary>Returns whether a channel has a conditional encoding.</summary>
    /// <param name="channel">The channel.</param>
    /// <returns><c>true</c> when a condition exists; otherwise, <c>false</c>.</returns>
    public bool HasConditionalEncoding(ChartEncodingChannel channel)
    {
        return ConditionalEncodingIndex(channel) >= 0;
    }

    /// <summary>Returns the conditional encoding for a channel.</summary>
    /// <param name="channel">The channel.</param>
    /// <returns>The conditional encoding.</returns>
    public ChartConditionalEncoding ConditionalEncodingFor(ChartEncodingChannel channel)
    {
        int index = ConditionalEncodingIndex(channel);
        if (index < 0)
        {
            throw new System.ArgumentException("Conditional encoding channel is not configured.");
        }

        return Conditions[index];
    }

    /// <summary>
    /// Sets the constant number configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetConstantNumber(ChartEncodingChannel channel, double value)
    {
        return SetBinding(ChartEncodingBinding.CreateNumberConstant(channel, value));
    }

    /// <summary>
    /// Sets the constant text configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetConstantText(ChartEncodingChannel channel, string value)
    {
        return SetBinding(ChartEncodingBinding.CreateTextConstant(channel, value));
    }

    /// <summary>
    /// Sets the constant color configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetConstantColor(ChartEncodingChannel channel, ChartColor value)
    {
        return SetBinding(ChartEncodingBinding.CreateColorConstant(channel, value));
    }

    /// <summary>
    /// Sets the constant shape configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting encoding binding.</returns>
    public ChartEncodingBinding SetConstantShape(ChartEncodingChannel channel, ChartMarkerShape value)
    {
        return SetBinding(ChartEncodingBinding.CreateShapeConstant(channel, value));
    }

    /// <summary>
    /// Sets the binding configuration.
    /// </summary>
    /// <param name = "binding">The binding.</param>
    /// <returns>The resulting encoding binding.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartEncodingBinding SetBinding(ChartEncodingBinding binding)
    {
        if (binding == null)
        {
            throw new System.ArgumentNullException("binding");
        }

        int index = BindingIndex(binding.Channel);
        if (index >= 0)
        {
            Bindings[index] = binding.Copy();
            return Bindings[index];
        }

        EnsureCapacity(BindingCount + 1);
        Bindings[BindingCount] = binding.Copy();
        BindingCount = BindingCount + 1;
        return Bindings[BindingCount - 1];
    }

    /// <summary>
    /// Determines whether channel.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool HasChannel(ChartEncodingChannel channel)
    {
        return BindingIndex(channel) >= 0;
    }

    /// <summary>
    /// Groups values into ding index using the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <returns>The resulting int.</returns>
    public int BindingIndex(ChartEncodingChannel channel)
    {
        ChartEncodingBinding.ValidateChannel(channel);
        int index = 0;
        while (index < BindingCount)
        {
            if (Bindings[index].Channel == channel)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Groups values into ding for using the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <returns>The resulting encoding binding.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartEncodingBinding BindingFor(ChartEncodingChannel channel)
    {
        int index = BindingIndex(channel);
        if (index < 0)
        {
            throw new System.ArgumentException("Encoding channel is not bound.");
        }

        return Bindings[index];
    }

    /// <summary>
    /// Groups values into ding at using the supplied data and configuration.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting encoding binding.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartEncodingBinding BindingAt(int index)
    {
        if (index < 0 || index >= BindingCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Bindings[index];
    }

    /// <summary>
    /// Creates an independent copy of the bindings.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartEncodingBinding[] CopyBindings()
    {
        ChartEncodingBinding[] copy = new ChartEncodingBinding[BindingCount];
        int index = 0;
        while (index < BindingCount)
        {
            copy[index] = Bindings[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartVisualEncoding Copy()
    {
        ChartVisualEncoding encoding = new ChartVisualEncoding();
        encoding.EnsureCapacity(BindingCount);
        int index = 0;
        while (index < BindingCount)
        {
            encoding.Bindings[index] = Bindings[index].Copy();
            index = index + 1;
        }

        encoding.BindingCount = BindingCount;
        encoding.EnsureConditionCapacity(ConditionCount);
        int conditionIndex = 0;
        while (conditionIndex < ConditionCount)
        {
            encoding.Conditions[conditionIndex] = Conditions[conditionIndex].Copy();
            conditionIndex = conditionIndex + 1;
        }

        encoding.ConditionCount = ConditionCount;
        if (Selection != null)
        {
            encoding.Selection = Selection.Copy();
        }

        return encoding;
    }

    internal int ConditionalEncodingIndex(ChartEncodingChannel channel)
    {
        ChartEncodingBinding.ValidateChannel(channel);
        int index = 0;
        while (index < ConditionCount)
        {
            if (Conditions[index].Channel == channel)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool HasEffectiveChannel(ChartEncodingChannel channel)
    {
        return HasChannel(channel) || (Selection != null && HasConditionalEncoding(channel));
    }

    internal ChartEncodingBinding BindingForRow(ChartEncodingChannel channel, int row)
    {
        if (Selection != null)
        {
            int index = ConditionalEncodingIndex(channel);
            if (index >= 0)
            {
                ChartEncodingBinding? conditional = Conditions[index].BindingForState(Selection.StateForRow(row));
                if (conditional != null)
                {
                    return conditional;
                }
            }
        }

        return BindingFor(channel);
    }

    internal void EnsureConditionCapacity(int capacity)
    {
        if (capacity <= Conditions.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Conditions.Length, capacity);
        ChartConditionalEncoding[] conditions = new ChartConditionalEncoding[newCapacity];
        System.Array.Copy(Conditions, conditions, ConditionCount);
        Conditions = conditions;
    }

    /// <summary>
    /// Validates against table and returns structured issues without rendering.
    /// </summary>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void ValidateAgainstTable(ChartDataTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        int index = 0;
        while (index < BindingCount)
        {
            ChartEncodingBinding binding = Bindings[index];
            if (!binding.IsConstant)
            {
                ChartDataColumn column = table.GetColumn(binding.FieldName);
                if (column.Kind != binding.FieldKind)
                {
                    throw new System.ArgumentException("Encoded field has a different data kind.");
                }
            }

            index = index + 1;
        }


        int conditionIndex = 0;
        while (conditionIndex < ConditionCount)
        {
            ChartConditionalEncoding condition = Conditions[conditionIndex];
            if (!HasChannel(condition.Channel) && (condition.UnselectedBinding == null || condition.SelectedBinding == null || condition.HighlightedBinding == null))
            {
                throw new System.ArgumentException("A conditional channel without a base binding requires unselected, selected, and highlighted bindings.");
            }

            ValidateConditionalBindingAgainstTable(table, condition.UnselectedBinding);
            ValidateConditionalBindingAgainstTable(table, condition.SelectedBinding);
            ValidateConditionalBindingAgainstTable(table, condition.HighlightedBinding);
            conditionIndex = conditionIndex + 1;
        }
    }

    internal static void ValidateConditionalBindingAgainstTable(ChartDataTable table, ChartEncodingBinding? binding)
    {
        if (binding == null || binding.IsConstant)
        {
            return;
        }

        ChartDataColumn column = table.GetColumn(binding.FieldName);
        if (column.Kind != binding.FieldKind)
        {
            throw new System.ArgumentException("Conditional encoded field has a different data kind.");
        }
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Bindings.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Bindings.Length, capacity);
        ChartEncodingBinding[] bindings = new ChartEncodingBinding[newCapacity];
        System.Array.Copy(Bindings, bindings, BindingCount);
        Bindings = bindings;
    }

    /// <summary>Returns the current bindings.</summary>
    /// <returns>The current field value.</returns>
    public ChartEncodingBinding[] GetBindings()
    {
        return Bindings;
    }

    /// <summary>Replaces the current bindings.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBindings(ChartEncodingBinding[] value)
    {
        Bindings = value;
    }

    /// <summary>Returns the current binding count.</summary>
    /// <returns>The current field value.</returns>
    public int GetBindingCount()
    {
        return BindingCount;
    }

    /// <summary>Replaces the current binding count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBindingCount(int value)
    {
        BindingCount = value;
    }
}
