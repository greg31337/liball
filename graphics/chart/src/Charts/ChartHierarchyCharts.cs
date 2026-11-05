namespace chart;
internal static class ChartHierarchyChartsOperations
{
    /// <summary>
    /// Computes the layout for treemap within the supplied bounds.
    /// </summary>
    /// <param name = "data">The data.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting hierarchy rect layout.</returns>
    public static ChartHierarchyRectLayout LayoutTreemap(ChartHierarchyData data, ChartRect bounds, ChartPalette palette)
    {
        return LayoutTreemapByKind(data, bounds, palette, ChartTreemapLayoutKind.SliceDice);
    }

    /// <summary>
    /// Computes a treemap with the requested sibling layout strategy.
    /// </summary>
    /// <param name = "data">The hierarchy data.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout.</param>
    /// <param name = "palette">The categorical colors to apply in stable node order.</param>
    /// <param name = "kind">The treemap sibling layout strategy.</param>
    /// <returns>The resulting hierarchy rectangle layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the layout kind is unsupported.</exception>
    public static ChartHierarchyRectLayout LayoutTreemapByKind(ChartHierarchyData data, ChartRect bounds, ChartPalette palette, ChartTreemapLayoutKind kind)
    {
        ValidateHierarchyBoundsPalette(data, bounds, palette);
        ValidateHierarchyTotal(data);
        ValidateTreemapKind(kind);
        ChartHierarchyRectLayout layout = new ChartHierarchyRectLayout(data.NodeCount);
        LayoutTreemapStrategyChildren(data, data.RootIndex, bounds, 0, palette, layout, kind);
        return layout;
    }

    /// <summary>
    /// Computes the layout for icicle within the supplied bounds.
    /// </summary>
    /// <param name = "data">The data.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting hierarchy rect layout.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartHierarchyRectLayout LayoutIcicle(ChartHierarchyData data, ChartRect bounds, ChartPalette palette)
    {
        ValidateHierarchyBoundsPalette(data, bounds, palette);
        ValidateHierarchyTotal(data);
        int maximumDepth = data.MaximumDepth();
        if (maximumDepth == 0)
        {
            throw new System.ArgumentException("Icicle layout requires child nodes.");
        }

        ChartHierarchyRectLayout layout = new ChartHierarchyRectLayout(data.NodeCount);
        double bandHeight = bounds.Height / maximumDepth;
        LayoutIcicleChildren(data, data.RootIndex, bounds.X, bounds.Width, bounds.Y, bandHeight, palette, layout);
        return layout;
    }

    /// <summary>
    /// Computes the layout for sunburst within the supplied bounds.
    /// </summary>
    /// <param name = "data">The data.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius in chart units; zero produces a solid shape.</param>
    /// <param name = "outerRadius">The outer radius in chart units, which must be greater than the inner radius.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "endAngle">The ending angle in degrees.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting sunburst layout.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartSunburstLayout LayoutSunburst(ChartHierarchyData data, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, double endAngle, ChartPalette palette)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        ChartPieChartsOperations.ValidateLayoutPoint(centerX, centerY);
        ChartPieSlice.ValidateRadii(innerRadius, outerRadius);
        ChartLinearScale.ValidateFinite(startAngle, "startAngle");
        ChartLinearScale.ValidateFinite(endAngle, "endAngle");
        ChartLineAreaChartsOperations.ValidatePalette(palette);
        ValidateHierarchyTotal(data);
        int maximumDepth = data.MaximumDepth();
        if (maximumDepth == 0)
        {
            throw new System.ArgumentException("Sunburst layout requires child nodes.");
        }

        ChartSunburstLayout layout = new ChartSunburstLayout(centerX, centerY, data.NodeCount);
        double ringWidth = (outerRadius - innerRadius) / maximumDepth;
        LayoutSunburstChildren(data, data.RootIndex, startAngle, endAngle, innerRadius, ringWidth, palette, layout);
        return layout;
    }

    internal static ChartRenderScene CreateTreemapScene(string id, int width, int height, ChartHierarchyData data, ChartRect bounds, ChartPalette palette, ChartTreemapLayoutKind kind)
    {
        ChartHierarchyRectLayout layout = LayoutTreemapByKind(data, bounds, palette, kind);
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddRectLayoutToScene(scene, id, layout);
        return scene;
    }

    internal static ChartRenderScene CreateIcicleScene(string id, int width, int height, ChartHierarchyData data, ChartRect bounds, ChartPalette palette)
    {
        ChartHierarchyRectLayout layout = LayoutIcicle(data, bounds, palette);
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddRectLayoutToScene(scene, id, layout);
        return scene;
    }

    internal static ChartRenderScene CreateSunburstScene(string id, int width, int height, ChartHierarchyData data, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, double endAngle, ChartPalette palette)
    {
        ChartSunburstLayout layout = LayoutSunburst(data, centerX, centerY, innerRadius, outerRadius, startAngle, endAngle, palette);
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddSunburstToScene(scene, id, layout);
        return scene;
    }

    /// <summary>
    /// Adds the supplied rect layout to scene to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void AddRectLayoutToScene(IChartRenderTarget scene, string id, ChartHierarchyRectLayout layout)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ChartPlot.ValidateId(id);
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        int index = 0;
        while (index < layout.ItemCount)
        {
            ChartHierarchyRectItem item = layout.ItemAt(index);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddRect(id + "-item-" + suffix, item.Bounds.X, item.Bounds.Y, item.Bounds.Width, item.Bounds.Height, 0.0, 0.0, ChartRenderStyle.CreateFill(item.Color));
            index = index + 1;
        }
    }

    /// <summary>
    /// Adds the supplied sunburst to scene to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void AddSunburstToScene(IChartRenderTarget scene, string id, ChartSunburstLayout layout)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ChartPlot.ValidateId(id);
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        int index = 0;
        while (index < layout.ItemCount)
        {
            ChartHierarchyArcItem item = layout.ItemAt(index);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ChartPath path = ChartPolarChartsOperations.CreateAnnularSectorPath(layout.CenterX, layout.CenterY, item.InnerRadius, item.OuterRadius, item.StartAngle, item.EndAngle);
            scene.AddPath(id + "-item-" + suffix, path, ChartRenderStyle.CreateFill(item.Color));
            index = index + 1;
        }
    }

    internal static void LayoutTreemapChildren(ChartHierarchyData data, int parentIndex, ChartRect bounds, int depth, ChartPalette palette, ChartHierarchyRectLayout layout)
    {
        int childCount = data.ChildCount(parentIndex);
        if (childCount == 0)
        {
            return;
        }

        double total = ChildrenTotal(data, parentIndex);
        if (total <= 0.0)
        {
            return;
        }

        double cursorX = bounds.X;
        double cursorY = bounds.Y;
        int childOrdinal = 0;
        while (childOrdinal < childCount)
        {
            int childIndex = data.ChildIndex(parentIndex, childOrdinal);
            double value = data.NodeTotal(childIndex);
            double amount = value / total;
            ChartRect childBounds = TreemapChildBounds(bounds, depth, amount, cursorX, cursorY);
            if (value > 0.0)
            {
                layout.AddItem(new ChartHierarchyRectItem(childIndex, data.NodeAt(childIndex).Label, value, depth + 1, childBounds, palette.GetColor(childIndex)));
                LayoutTreemapChildren(data, childIndex, childBounds, depth + 1, palette, layout);
            }

            if (depth % 2 == 0)
            {
                cursorX = cursorX + childBounds.Width;
            }
            else
            {
                cursorY = cursorY + childBounds.Height;
            }

            childOrdinal = childOrdinal + 1;
        }
    }

    internal static ChartRect TreemapChildBounds(ChartRect bounds, int depth, double amount, double cursorX, double cursorY)
    {
        if (depth % 2 == 0)
        {
            return new ChartRect(cursorX, bounds.Y, bounds.Width * amount, bounds.Height);
        }

        return new ChartRect(bounds.X, cursorY, bounds.Width, bounds.Height * amount);
    }

    internal static void LayoutTreemapStrategyChildren(ChartHierarchyData data, int parentIndex, ChartRect bounds, int depth, ChartPalette palette, ChartHierarchyRectLayout layout, ChartTreemapLayoutKind kind)
    {
        if (kind == ChartTreemapLayoutKind.Squarified)
        {
            LayoutSquarifiedChildren(data, parentIndex, bounds, depth, palette, layout);
            return;
        }

        int childCount = data.ChildCount(parentIndex);
        if (childCount == 0)
        {
            return;
        }

        double total = ChildrenTotal(data, parentIndex);
        if (total <= 0.0)
        {
            return;
        }

        bool dice = kind == ChartTreemapLayoutKind.Dice;
        if (kind == ChartTreemapLayoutKind.SliceDice)
        {
            dice = depth % 2 == 0;
        }

        double cursorX = bounds.X;
        double cursorY = bounds.Y;
        int childOrdinal = 0;
        while (childOrdinal < childCount)
        {
            int childIndex = data.ChildIndex(parentIndex, childOrdinal);
            double value = data.NodeTotal(childIndex);
            double amount = value / total;
            ChartRect childBounds;
            if (dice)
            {
                childBounds = new ChartRect(cursorX, bounds.Y, bounds.Width * amount, bounds.Height);
                cursorX = cursorX + childBounds.Width;
            }
            else
            {
                childBounds = new ChartRect(bounds.X, cursorY, bounds.Width, bounds.Height * amount);
                cursorY = cursorY + childBounds.Height;
            }

            if (value > 0.0)
            {
                layout.AddItem(new ChartHierarchyRectItem(childIndex, data.NodeAt(childIndex).Label, value, depth + 1, childBounds, palette.GetColor(childIndex)));
                LayoutTreemapStrategyChildren(data, childIndex, childBounds, depth + 1, palette, layout, kind);
            }

            childOrdinal = childOrdinal + 1;
        }
    }

    internal static void LayoutSquarifiedChildren(ChartHierarchyData data, int parentIndex, ChartRect bounds, int depth, ChartPalette palette, ChartHierarchyRectLayout layout)
    {
        int childCount = data.ChildCount(parentIndex);
        if (childCount == 0)
        {
            return;
        }

        double total = ChildrenTotal(data, parentIndex);
        if (total <= 0.0)
        {
            return;
        }

        int[] childIndices = new int[childCount];
        double[] childAreas = new double[childCount];
        int positiveCount = 0;
        int childOrdinal = 0;
        while (childOrdinal < childCount)
        {
            int childIndex = data.ChildIndex(parentIndex, childOrdinal);
            double value = data.NodeTotal(childIndex);
            if (value > 0.0)
            {
                childIndices[positiveCount] = childIndex;
                childAreas[positiveCount] = bounds.Width * bounds.Height * value / total;
                positiveCount = positiveCount + 1;
            }

            childOrdinal = childOrdinal + 1;
        }

        SortSquarifiedItems(childIndices, childAreas, positiveCount);
        double[] remaining = new double[] { bounds.X, bounds.Y, bounds.Width, bounds.Height };
        int rowStart = 0;
        int rowCount = 0;
        double rowArea = 0.0;
        int next = 0;
        while (next < positiveCount)
        {
            double side = remaining[2];
            if (remaining[3] < side)
            {
                side = remaining[3];
            }

            double currentWorst = double.PositiveInfinity;
            if (rowCount > 0)
            {
                currentWorst = SquarifiedWorstAspect(childAreas, rowStart, rowCount, rowArea, side);
            }

            double candidateWorst = SquarifiedWorstAspect(childAreas, rowStart, rowCount + 1, rowArea + childAreas[next], side);
            if (rowCount == 0 || candidateWorst <= currentWorst)
            {
                rowCount = rowCount + 1;
                rowArea = rowArea + childAreas[next];
                next = next + 1;
            }
            else
            {
                LayoutSquarifiedRow(data, childIndices, childAreas, rowStart, rowCount, rowArea, remaining, depth, palette, layout);
                rowStart = next;
                rowCount = 0;
                rowArea = 0.0;
            }
        }

        if (rowCount > 0)
        {
            LayoutSquarifiedRow(data, childIndices, childAreas, rowStart, rowCount, rowArea, remaining, depth, palette, layout);
        }
    }

    internal static void SortSquarifiedItems(int[] childIndices, double[] childAreas, int count)
    {
        int index = 1;
        while (index < count)
        {
            int savedIndex = childIndices[index];
            double savedArea = childAreas[index];
            int position = index;
            while (position > 0 && childAreas[position - 1] < savedArea)
            {
                childIndices[position] = childIndices[position - 1];
                childAreas[position] = childAreas[position - 1];
                position = position - 1;
            }

            childIndices[position] = savedIndex;
            childAreas[position] = savedArea;
            index = index + 1;
        }
    }

    internal static double SquarifiedWorstAspect(double[] areas, int start, int count, double totalArea, double side)
    {
        double minimum = areas[start];
        double maximum = areas[start];
        int index = start + 1;
        while (index < start + count)
        {
            if (areas[index] < minimum)
            {
                minimum = areas[index];
            }

            if (areas[index] > maximum)
            {
                maximum = areas[index];
            }

            index = index + 1;
        }

        double sideSquared = side * side;
        double totalSquared = totalArea * totalArea;
        double first = sideSquared * maximum / totalSquared;
        double second = totalSquared / (sideSquared * minimum);
        if (first > second)
        {
            return first;
        }

        return second;
    }

    internal static void LayoutSquarifiedRow(ChartHierarchyData data, int[] childIndices, double[] childAreas, int start, int count, double rowArea, double[] remaining, int depth, ChartPalette palette, ChartHierarchyRectLayout layout)
    {
        bool vertical = remaining[2] >= remaining[3];
        double cursor = vertical ? remaining[1] : remaining[0];
        double thickness = vertical ? rowArea / remaining[3] : rowArea / remaining[2];
        int ordinal = 0;
        while (ordinal < count)
        {
            int item = start + ordinal;
            double length = childAreas[item] / thickness;
            if (ordinal == count - 1)
            {
                if (vertical)
                {
                    length = remaining[1] + remaining[3] - cursor;
                }
                else
                {
                    length = remaining[0] + remaining[2] - cursor;
                }
            }

            ChartRect childBounds;
            if (vertical)
            {
                childBounds = new ChartRect(remaining[0], cursor, thickness, length);
            }
            else
            {
                childBounds = new ChartRect(cursor, remaining[1], length, thickness);
            }

            int childIndex = childIndices[item];
            double value = data.NodeTotal(childIndex);
            layout.AddItem(new ChartHierarchyRectItem(childIndex, data.NodeAt(childIndex).Label, value, depth + 1, childBounds, palette.GetColor(childIndex)));
            LayoutSquarifiedChildren(data, childIndex, childBounds, depth + 1, palette, layout);
            cursor = cursor + length;
            ordinal = ordinal + 1;
        }

        if (vertical)
        {
            remaining[0] = remaining[0] + thickness;
            remaining[2] = remaining[2] - thickness;
        }
        else
        {
            remaining[1] = remaining[1] + thickness;
            remaining[3] = remaining[3] - thickness;
        }
    }

    internal static void ValidateTreemapKind(ChartTreemapLayoutKind kind)
    {
        if (kind != ChartTreemapLayoutKind.Slice && kind != ChartTreemapLayoutKind.Dice && kind != ChartTreemapLayoutKind.SliceDice && kind != ChartTreemapLayoutKind.Squarified)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }

    internal static void LayoutIcicleChildren(ChartHierarchyData data, int parentIndex, double x, double width, double y, double bandHeight, ChartPalette palette, ChartHierarchyRectLayout layout)
    {
        int childCount = data.ChildCount(parentIndex);
        if (childCount == 0)
        {
            return;
        }

        double total = ChildrenTotal(data, parentIndex);
        if (total <= 0.0)
        {
            return;
        }

        double cursorX = x;
        int childOrdinal = 0;
        while (childOrdinal < childCount)
        {
            int childIndex = data.ChildIndex(parentIndex, childOrdinal);
            double value = data.NodeTotal(childIndex);
            double childWidth = width * value / total;
            ChartRect childBounds = new ChartRect(cursorX, y, childWidth, bandHeight);
            if (value > 0.0)
            {
                int depth = data.NodeDepth(childIndex);
                layout.AddItem(new ChartHierarchyRectItem(childIndex, data.NodeAt(childIndex).Label, value, depth, childBounds, palette.GetColor(childIndex)));
                LayoutIcicleChildren(data, childIndex, cursorX, childWidth, y + bandHeight, bandHeight, palette, layout);
            }

            cursorX = cursorX + childWidth;
            childOrdinal = childOrdinal + 1;
        }
    }

    internal static void LayoutSunburstChildren(ChartHierarchyData data, int parentIndex, double startAngle, double endAngle, double innerRadius, double ringWidth, ChartPalette palette, ChartSunburstLayout layout)
    {
        int childCount = data.ChildCount(parentIndex);
        if (childCount == 0)
        {
            return;
        }

        double total = ChildrenTotal(data, parentIndex);
        if (total <= 0.0)
        {
            return;
        }

        double cursorAngle = startAngle;
        int childOrdinal = 0;
        while (childOrdinal < childCount)
        {
            int childIndex = data.ChildIndex(parentIndex, childOrdinal);
            double value = data.NodeTotal(childIndex);
            double childSweep = (endAngle - startAngle) * value / total;
            if (value > 0.0)
            {
                int depth = data.NodeDepth(childIndex);
                double childInner = innerRadius + (depth - 1) * ringWidth;
                double childOuter = childInner + ringWidth;
                layout.AddItem(new ChartHierarchyArcItem(childIndex, data.NodeAt(childIndex).Label, value, depth, cursorAngle, cursorAngle + childSweep, childInner, childOuter, palette.GetColor(childIndex)));
                LayoutSunburstChildren(data, childIndex, cursorAngle, cursorAngle + childSweep, innerRadius, ringWidth, palette, layout);
            }

            cursorAngle = cursorAngle + childSweep;
            childOrdinal = childOrdinal + 1;
        }
    }

    internal static double ChildrenTotal(ChartHierarchyData data, int parentIndex)
    {
        double total = 0.0;
        int childCount = data.ChildCount(parentIndex);
        int childOrdinal = 0;
        while (childOrdinal < childCount)
        {
            total = total + data.NodeTotal(data.ChildIndex(parentIndex, childOrdinal));
            childOrdinal = childOrdinal + 1;
        }

        return total;
    }

    internal static void ValidateHierarchyBoundsPalette(ChartHierarchyData data, ChartRect bounds, ChartPalette palette)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        ChartAxis.ValidatePlotArea(bounds);
        ChartLineAreaChartsOperations.ValidatePalette(palette);
    }

    internal static void ValidateHierarchyTotal(ChartHierarchyData data)
    {
        if (data.NodeTotal(data.RootIndex) <= 0.0)
        {
            throw new System.ArgumentException("Hierarchy total value must be positive.");
        }
    }
}
