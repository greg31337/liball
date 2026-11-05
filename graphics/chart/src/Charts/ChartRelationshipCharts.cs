namespace chart;
internal static class ChartRelationshipChartsOperations
{
    /// <summary>
    /// Computes the layout for sankey within the supplied bounds.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "links">The links.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "nodeWidth">The node width.</param>
    /// <param name = "nodePadding">The node padding.</param>
    /// <returns>The resulting graph layout.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartGraphLayout LayoutSankey(string[] labels, ChartGraphLink[] links, ChartRect bounds, double nodeWidth, double nodePadding)
    {
        ValidateLabelsLinksBounds(labels, links, bounds);
        ChartRenderStyle.ValidatePositive(nodeWidth, "nodeWidth");
        ChartStyle.ValidateNonNegative(nodePadding, "nodePadding");
        int[] layers = SankeyLayers(labels.Length, links);
        int maximumLayer = MaximumInt(layers);
        ChartGraphLayout layout = new ChartGraphLayout(labels.Length, links.Length);
        int nodeIndex = 0;
        while (nodeIndex < labels.Length)
        {
            int layer = layers[nodeIndex];
            int countInLayer = CountLayer(layers, layer);
            double nodeValue = SankeyNodeValue(nodeIndex, links);
            double layerTotal = SankeyLayerTotal(layers, layer, links);
            double availableHeight = bounds.Height - nodePadding * (countInLayer - 1);
            if (availableHeight <= 0.0)
            {
                throw new System.ArgumentException("Sankey node padding leaves no node height.");
            }

            double height = availableHeight / countInLayer;
            if (layerTotal > 0.0)
            {
                height = availableHeight * nodeValue / layerTotal;
            }

            double x = LayerX(bounds, nodeWidth, layer, maximumLayer);
            double y = bounds.Y + SankeyLayerOffset(layers, links, layer, nodeIndex, availableHeight, nodePadding);
            layout.AddNode(new ChartGraphNodeLayout(nodeIndex, labels[nodeIndex], x, y, nodeWidth, height, layer));
            nodeIndex = nodeIndex + 1;
        }

        int linkIndex = 0;
        while (linkIndex < links.Length)
        {
            ChartGraphNodeLayout source = layout.NodeAt(links[linkIndex].SourceIndex);
            ChartGraphNodeLayout target = layout.NodeAt(links[linkIndex].TargetIndex);
            layout.AddLink(new ChartGraphLinkLayout(links[linkIndex].SourceIndex, links[linkIndex].TargetIndex, source.X + source.Width, source.Y + source.Height * 0.5, target.X, target.Y + target.Height * 0.5, links[linkIndex].Value, links[linkIndex].Value));
            linkIndex = linkIndex + 1;
        }

        return layout;
    }

    /// <summary>
    /// Computes the layout for node link within the supplied bounds.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "links">The links.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <returns>The resulting graph layout.</returns>
    public static ChartGraphLayout LayoutNodeLink(string[] labels, ChartGraphLink[] links, ChartRect bounds)
    {
        ValidateLabelsLinksBounds(labels, links, bounds);
        ChartGraphLayout layout = new ChartGraphLayout(labels.Length, links.Length);
        ChartPoint center = bounds.Center();
        double radius = bounds.Width;
        if (bounds.Height < radius)
        {
            radius = bounds.Height;
        }

        radius = radius * 0.5;
        int index = 0;
        while (index < labels.Length)
        {
            double angle = 360.0 * index / labels.Length;
            ChartPoint point = ChartPieChartsOperations.PointAtAngle(center.X, center.Y, angle, radius);
            layout.AddNode(new ChartGraphNodeLayout(index, labels[index], point.X, point.Y, 0.0, 0.0, 0));
            index = index + 1;
        }

        AddGraphLinks(layout, links);
        return layout;
    }

    /// <summary>
    /// Computes a deterministic seeded force-directed graph layout.
    /// </summary>
    /// <param name = "labels">The node labels.</param>
    /// <param name = "links">The graph links.</param>
    /// <param name = "bounds">The rectangle that constrains node positions.</param>
    /// <param name = "seed">The seed controlling initial node positions.</param>
    /// <param name = "iterations">The number of simulation iterations.</param>
    /// <param name = "linkDistance">The preferred distance between linked nodes.</param>
    /// <param name = "repulsion">The nonnegative pairwise repulsion strength.</param>
    /// <returns>The resulting graph layout.</returns>
    public static ChartGraphLayout LayoutForceDirected(string[] labels, ChartGraphLink[] links, ChartRect bounds, int seed, int iterations, double linkDistance, double repulsion)
    {
        ValidateLabelsLinksBounds(labels, links, bounds);
        if (iterations <= 0)
        {
            throw new System.ArgumentOutOfRangeException("iterations");
        }

        ChartRenderStyle.ValidatePositive(linkDistance, "linkDistance");
        ChartStyle.ValidateNonNegative(repulsion, "repulsion");
        double[] xValues = new double[labels.Length];
        double[] yValues = new double[labels.Length];
        double[] xVelocity = new double[labels.Length];
        double[] yVelocity = new double[labels.Length];
        double[] xForce = new double[labels.Length];
        double[] yForce = new double[labels.Length];
        int[] randomState = CreateForceRandomState(seed);
        InitializeForcePositions(xValues, yValues, bounds, randomState);
        int iteration = 0;
        while (iteration < iterations)
        {
            ResetForceValues(xForce, yForce);
            ApplyForceRepulsion(xValues, yValues, xForce, yForce, repulsion, iteration);
            ApplyForceLinks(xValues, yValues, xForce, yForce, links, linkDistance);
            ApplyForceCenter(xValues, yValues, xForce, yForce, bounds);
            IntegrateForcePositions(xValues, yValues, xVelocity, yVelocity, xForce, yForce, bounds);
            iteration = iteration + 1;
        }

        ChartGraphLayout layout = new ChartGraphLayout(labels.Length, links.Length);
        int node = 0;
        while (node < labels.Length)
        {
            layout.AddNode(new ChartGraphNodeLayout(node, labels[node], xValues[node], yValues[node], 0.0, 0.0, 0));
            node = node + 1;
        }

        AddGraphLinks(layout, links);
        return layout;
    }

    /// <summary>
    /// Computes the layout for tree within the supplied bounds.
    /// </summary>
    /// <param name = "data">The data.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "orientation">The orientation.</param>
    /// <returns>The resulting graph layout.</returns>
    public static ChartGraphLayout LayoutTree(ChartHierarchyData data, ChartRect bounds, ChartOrientation orientation)
    {
        ValidateHierarchyLayoutInputs(data, bounds, orientation);
        ChartGraphLayout layout = new ChartGraphLayout(data.NodeCount, data.NodeCount - 1);
        int maximumDepth = data.MaximumDepth();
        int index = 0;
        while (index < data.NodeCount)
        {
            int depth = data.NodeDepth(index);
            double depthAmount = 0.0;
            if (maximumDepth > 0)
            {
                depthAmount = depth / (double)maximumDepth;
            }

            double orderAmount = 0.0;
            if (data.NodeCount > 1)
            {
                orderAmount = index / (double)(data.NodeCount - 1);
            }

            ChartPoint point = TreePoint(bounds, depthAmount, orderAmount, orientation);
            layout.AddNode(new ChartGraphNodeLayout(index, data.NodeAt(index).Label, point.X, point.Y, 0.0, 0.0, depth));
            index = index + 1;
        }

        AddHierarchyLinks(layout, data);
        return layout;
    }

    /// <summary>
    /// Computes the layout for dendrogram within the supplied bounds.
    /// </summary>
    /// <param name = "data">The data.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "orientation">The orientation.</param>
    /// <returns>The resulting graph layout.</returns>
    public static ChartGraphLayout LayoutDendrogram(ChartHierarchyData data, ChartRect bounds, ChartOrientation orientation)
    {
        ValidateHierarchyLayoutInputs(data, bounds, orientation);
        ChartGraphLayout layout = new ChartGraphLayout(data.NodeCount, data.NodeCount - 1);
        int maximumDepth = data.MaximumDepth();
        int index = 0;
        while (index < data.NodeCount)
        {
            int depth = data.NodeDepth(index);
            double depthAmount = 0.0;
            if (maximumDepth > 0)
            {
                depthAmount = depth / (double)maximumDepth;
            }

            if (data.ChildCount(index) == 0)
            {
                depthAmount = 1.0;
            }

            double orderAmount = 0.0;
            if (data.NodeCount > 1)
            {
                orderAmount = index / (double)(data.NodeCount - 1);
            }

            ChartPoint point = TreePoint(bounds, depthAmount, orderAmount, orientation);
            layout.AddNode(new ChartGraphNodeLayout(index, data.NodeAt(index).Label, point.X, point.Y, 0.0, 0.0, depth));
            index = index + 1;
        }

        AddHierarchyLinks(layout, data);
        return layout;
    }

    /// <summary>
    /// Computes the layout for parallel coordinates within the supplied bounds.
    /// </summary>
    /// <param name = "axisLabels">The axis labels.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <returns>The resulting parallel coordinates layout.</returns>
    public static ChartParallelCoordinatesLayout LayoutParallelCoordinates(string[] axisLabels, ChartDataMatrix values, ChartRect bounds)
    {
        ValidateParallelInputs(axisLabels, values, bounds);
        ChartParallelCoordinatesLayout layout = new ChartParallelCoordinatesLayout(values.Columns, values.Rows);
        int axis = 0;
        while (axis < values.Columns)
        {
            double x = AxisX(bounds, axis, values.Columns);
            layout.AddAxis(new ChartParallelAxisLayout(axis, axisLabels[axis], x, ColumnMinimum(values, axis), ColumnMaximum(values, axis)));
            axis = axis + 1;
        }

        int row = 0;
        while (row < values.Rows)
        {
            ChartPoint[] points = new ChartPoint[values.Columns];
            axis = 0;
            while (axis < values.Columns)
            {
                ChartParallelAxisLayout axisLayout = layout.AxisAt(axis);
                double y = MapParallelY(values.GetValue(row, axis), axisLayout.Minimum, axisLayout.Maximum, bounds);
                points[axis] = new ChartPoint(axisLayout.X, y);
                axis = axis + 1;
            }

            layout.AddRecord(new ChartParallelRecordLayout(row, points));
            row = row + 1;
        }

        return layout;
    }

    /// <summary>
    /// Computes the layout for parallel categories within the supplied bounds.
    /// </summary>
    /// <param name = "axisLabels">The axis labels.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "rowCount">The number of logical rows represented by the input.</param>
    /// <param name = "axisCount">The requested number of axis entries.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <returns>The resulting parallel categories layout.</returns>
    public static ChartParallelCategoriesLayout LayoutParallelCategories(string[] axisLabels, string[] values, int rowCount, int axisCount, ChartRect bounds)
    {
        ValidateParallelCategoryInputs(axisLabels, values, rowCount, axisCount, bounds);
        ChartParallelCategoriesLayout layout = new ChartParallelCategoriesLayout(values.Length, rowCount * (axisCount - 1), axisCount, rowCount);
        int axis = 0;
        while (axis < axisCount)
        {
            AddCategoryBands(layout, values, rowCount, axisCount, axis, AxisX(bounds, axis, axisCount), bounds);
            axis = axis + 1;
        }

        int row = 0;
        while (row < rowCount)
        {
            axis = 0;
            while (axis < axisCount - 1)
            {
                int sourceBand = FindBand(layout, axis, CategoryValue(values, row, axis, axisCount));
                int targetBand = FindBand(layout, axis + 1, CategoryValue(values, row, axis + 1, axisCount));
                ChartParallelCategoryBand source = layout.BandAt(sourceBand);
                ChartParallelCategoryBand target = layout.BandAt(targetBand);
                layout.AddFlow(new ChartParallelCategoryFlow(row, sourceBand, targetBand, source.X, source.Y + source.Height * 0.5, target.X, target.Y + target.Height * 0.5));
                axis = axis + 1;
            }

            row = row + 1;
        }

        return layout;
    }

    /// <summary>
    /// Adds the supplied graph links to this object.
    /// </summary>
    /// <param name = "layout">The layout.</param>
    /// <param name = "links">The links.</param>
    public static void AddGraphLinks(ChartGraphLayout layout, ChartGraphLink[] links)
    {
        int index = 0;
        while (index < links.Length)
        {
            ChartGraphNodeLayout source = layout.NodeAt(links[index].SourceIndex);
            ChartGraphNodeLayout target = layout.NodeAt(links[index].TargetIndex);
            layout.AddLink(new ChartGraphLinkLayout(links[index].SourceIndex, links[index].TargetIndex, source.X, source.Y, target.X, target.Y, links[index].Value, links[index].Value));
            index = index + 1;
        }
    }

    /// <summary>
    /// Adds the supplied hierarchy links to this object.
    /// </summary>
    /// <param name = "layout">The layout.</param>
    /// <param name = "data">The data.</param>
    public static void AddHierarchyLinks(ChartGraphLayout layout, ChartHierarchyData data)
    {
        int index = 0;
        while (index < data.NodeCount)
        {
            int parent = data.NodeAt(index).ParentIndex;
            if (parent >= 0)
            {
                ChartGraphNodeLayout source = layout.NodeAt(parent);
                ChartGraphNodeLayout target = layout.NodeAt(index);
                layout.AddLink(new ChartGraphLinkLayout(parent, index, source.X, source.Y, target.X, target.Y, data.NodeTotal(index), 1.0));
            }

            index = index + 1;
        }
    }

    internal static ChartRenderScene CreateGraphScene(string id, int width, int height, ChartGraphLayout layout, double nodeSize, ChartRenderStyle nodeStyle, ChartRenderStyle linkStyle, ChartRenderStyle labelStyle)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddGraphLayoutToScene(scene, id, layout, nodeSize, nodeStyle, linkStyle, labelStyle);
        return scene;
    }

    internal static void AddGraphLayoutToScene(IChartRenderTarget scene, string id, ChartGraphLayout layout, double nodeSize, ChartRenderStyle nodeStyle, ChartRenderStyle linkStyle, ChartRenderStyle labelStyle)
    {
        ValidateRenderTargetId(scene, id);
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        ChartRenderStyle.ValidatePositive(nodeSize, "nodeSize");
        ValidateRenderStyles(nodeStyle, linkStyle, labelStyle);
        int index = 0;
        while (index < layout.LinkCount)
        {
            ChartGraphLinkLayout link = layout.LinkAt(index);
            ChartRenderStyle renderedLinkStyle = GraphLinkStyle(linkStyle, link.Width);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddLine(id + "-link-" + suffix, link.X1, link.Y1, link.X2, link.Y2, renderedLinkStyle);
            index = index + 1;
        }

        index = 0;
        while (index < layout.NodeCount)
        {
            ChartGraphNodeLayout node = layout.NodeAt(index);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            double labelX = node.X;
            double labelY = node.Y - nodeSize * 0.75;
            if (node.Width > 0.0 && node.Height > 0.0)
            {
                scene.AddRect(id + "-node-" + suffix, node.X, node.Y, node.Width, node.Height, 0.0, 0.0, nodeStyle);
                labelX = node.X + node.Width * 0.5;
                labelY = node.Y + node.Height * 0.5;
            }
            else
            {
                scene.AddMarker(id + "-node-" + suffix, ChartMarkerShape.Circle, node.X, node.Y, nodeSize, nodeStyle);
            }

            scene.AddText(id + "-label-" + suffix, node.Label, labelX, labelY, labelStyle);
            index = index + 1;
        }
    }

    internal static ChartRenderScene CreateParallelCoordinatesScene(string id, int width, int height, ChartParallelCoordinatesLayout layout, ChartRect bounds, ChartRenderStyle axisStyle, ChartRenderStyle recordStyle, ChartRenderStyle labelStyle)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddParallelCoordinatesToScene(scene, id, layout, bounds, axisStyle, recordStyle, labelStyle);
        return scene;
    }

    internal static void AddParallelCoordinatesToScene(IChartRenderTarget scene, string id, ChartParallelCoordinatesLayout layout, ChartRect bounds, ChartRenderStyle axisStyle, ChartRenderStyle recordStyle, ChartRenderStyle labelStyle)
    {
        ValidateRenderTargetId(scene, id);
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        ChartAxis.ValidatePlotArea(bounds);
        ValidateRenderStyles(axisStyle, recordStyle, labelStyle);
        int index = 0;
        while (index < layout.RecordCount)
        {
            ChartParallelRecordLayout record = layout.RecordAt(index);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddPolyline(id + "-record-" + suffix, record.Points, recordStyle);
            index = index + 1;
        }

        index = 0;
        while (index < layout.AxisCount)
        {
            ChartParallelAxisLayout axis = layout.AxisAt(index);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddLine(id + "-axis-" + suffix, axis.X, bounds.Y, axis.X, bounds.Y + bounds.Height, axisStyle);
            scene.AddText(id + "-label-" + suffix, axis.Label, axis.X, bounds.Y, labelStyle);
            index = index + 1;
        }
    }

    internal static ChartRenderScene CreateParallelCategoriesScene(string id, int width, int height, ChartParallelCategoriesLayout layout, double bandWidth, ChartRenderStyle bandStyle, ChartRenderStyle flowStyle, ChartRenderStyle labelStyle)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddParallelCategoriesToScene(scene, id, layout, bandWidth, bandStyle, flowStyle, labelStyle);
        return scene;
    }

    internal static void AddParallelCategoriesToScene(IChartRenderTarget scene, string id, ChartParallelCategoriesLayout layout, double bandWidth, ChartRenderStyle bandStyle, ChartRenderStyle flowStyle, ChartRenderStyle labelStyle)
    {
        ValidateRenderTargetId(scene, id);
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        ChartRenderStyle.ValidatePositive(bandWidth, "bandWidth");
        ValidateRenderStyles(bandStyle, flowStyle, labelStyle);
        int index = 0;
        while (index < layout.FlowCount)
        {
            ChartParallelCategoryFlow flow = layout.FlowAt(index);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddLine(id + "-flow-" + suffix, flow.X1, flow.Y1, flow.X2, flow.Y2, flowStyle);
            index = index + 1;
        }

        index = 0;
        while (index < layout.BandCount)
        {
            ChartParallelCategoryBand band = layout.BandAt(index);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddRect(id + "-band-" + suffix, band.X - bandWidth * 0.5, band.Y, bandWidth, band.Height, 0.0, 0.0, bandStyle);
            scene.AddText(id + "-label-" + suffix, band.Category, band.X, band.Y + band.Height * 0.5, labelStyle);
            index = index + 1;
        }
    }

    internal static ChartRenderStyle GraphLinkStyle(ChartRenderStyle linkStyle, double layoutWidth)
    {
        ChartLineAreaChartsOperations.ValidateStyle(linkStyle);
        ChartRenderStyle style = linkStyle.Copy();
        if (style.HasStroke && !style.StrokeNone && layoutWidth > 0.0)
        {
            style.SetStrokeWidth(style.StrokeWidth * layoutWidth);
        }

        return style;
    }

    internal static void ValidateRenderTargetId(IChartRenderTarget scene, string id)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ChartPlot.ValidateId(id);
    }

    internal static void ValidateRenderStyles(ChartRenderStyle first, ChartRenderStyle second, ChartRenderStyle third)
    {
        ChartLineAreaChartsOperations.ValidateStyle(first);
        ChartLineAreaChartsOperations.ValidateStyle(second);
        ChartLineAreaChartsOperations.ValidateStyle(third);
    }

    internal static int[] CreateForceRandomState(int seed)
    {
        long normalized = seed;
        if (normalized < 0)
        {
            normalized = -normalized;
        }

        normalized = normalized % 2147483646L + 1L;
        return new int[] { (int)normalized };
    }

    internal static double NextForceRandom(int[] state)
    {
        long next = (long)state[0] * 48271L % 2147483647L;
        state[0] = (int)next;
        return next / 2147483647.0;
    }

    internal static void InitializeForcePositions(double[] xValues, double[] yValues, ChartRect bounds, int[] randomState)
    {
        int index = 0;
        while (index < xValues.Length)
        {
            xValues[index] = bounds.X + bounds.Width * NextForceRandom(randomState);
            yValues[index] = bounds.Y + bounds.Height * NextForceRandom(randomState);
            index = index + 1;
        }
    }

    internal static void ResetForceValues(double[] xForce, double[] yForce)
    {
        int index = 0;
        while (index < xForce.Length)
        {
            xForce[index] = 0.0;
            yForce[index] = 0.0;
            index = index + 1;
        }
    }

    internal static void ApplyForceRepulsion(double[] xValues, double[] yValues, double[] xForce, double[] yForce, double repulsion, int iteration)
    {
        int first = 0;
        while (first < xValues.Length)
        {
            int second = first + 1;
            while (second < xValues.Length)
            {
                double dx = xValues[second] - xValues[first];
                double dy = yValues[second] - yValues[first];
                double distanceSquared = dx * dx + dy * dy;
                if (distanceSquared < 0.000000000001)
                {
                    dx = 0.000001 * (second - first);
                    dy = 0.000001 * (iteration + 1);
                    distanceSquared = dx * dx + dy * dy;
                }

                double distance = System.Math.Sqrt(distanceSquared);
                double magnitude = repulsion / distanceSquared;
                double forceX = magnitude * dx / distance;
                double forceY = magnitude * dy / distance;
                xForce[first] = xForce[first] - forceX;
                yForce[first] = yForce[first] - forceY;
                xForce[second] = xForce[second] + forceX;
                yForce[second] = yForce[second] + forceY;
                second = second + 1;
            }

            first = first + 1;
        }
    }

    internal static void ApplyForceLinks(double[] xValues, double[] yValues, double[] xForce, double[] yForce, ChartGraphLink[] links, double linkDistance)
    {
        int index = 0;
        while (index < links.Length)
        {
            int source = links[index].SourceIndex;
            int target = links[index].TargetIndex;
            double dx = xValues[target] - xValues[source];
            double dy = yValues[target] - yValues[source];
            double distanceSquared = dx * dx + dy * dy;
            if (distanceSquared > 0.000000000001)
            {
                double distance = System.Math.Sqrt(distanceSquared);
                double strength = 0.05;
                if (links[index].Value > 0.0)
                {
                    strength = strength * System.Math.Sqrt(links[index].Value);
                }

                double magnitude = (distance - linkDistance) * strength;
                double forceX = magnitude * dx / distance;
                double forceY = magnitude * dy / distance;
                xForce[source] = xForce[source] + forceX;
                yForce[source] = yForce[source] + forceY;
                xForce[target] = xForce[target] - forceX;
                yForce[target] = yForce[target] - forceY;
            }

            index = index + 1;
        }
    }

    internal static void ApplyForceCenter(double[] xValues, double[] yValues, double[] xForce, double[] yForce, ChartRect bounds)
    {
        double centerX = bounds.X + bounds.Width * 0.5;
        double centerY = bounds.Y + bounds.Height * 0.5;
        int index = 0;
        while (index < xValues.Length)
        {
            xForce[index] = xForce[index] + (centerX - xValues[index]) * 0.01;
            yForce[index] = yForce[index] + (centerY - yValues[index]) * 0.01;
            index = index + 1;
        }
    }

    internal static void IntegrateForcePositions(double[] xValues, double[] yValues, double[] xVelocity, double[] yVelocity, double[] xForce, double[] yForce, ChartRect bounds)
    {
        double right = bounds.X + bounds.Width;
        double bottom = bounds.Y + bounds.Height;
        int index = 0;
        while (index < xValues.Length)
        {
            xVelocity[index] = (xVelocity[index] + xForce[index] * 0.1) * 0.85;
            yVelocity[index] = (yVelocity[index] + yForce[index] * 0.1) * 0.85;
            xValues[index] = xValues[index] + xVelocity[index];
            yValues[index] = yValues[index] + yVelocity[index];
            if (xValues[index] < bounds.X)
            {
                xValues[index] = bounds.X;
                xVelocity[index] = 0.0;
            }
            else if (xValues[index] > right)
            {
                xValues[index] = right;
                xVelocity[index] = 0.0;
            }

            if (yValues[index] < bounds.Y)
            {
                yValues[index] = bounds.Y;
                yVelocity[index] = 0.0;
            }
            else if (yValues[index] > bottom)
            {
                yValues[index] = bottom;
                yVelocity[index] = 0.0;
            }

            index = index + 1;
        }
    }

    internal static int[] SankeyLayers(int nodeCount, ChartGraphLink[] links)
    {
        if (nodeCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("nodeCount");
        }

        ValidateLinks(links, nodeCount);
        int[] layers = new int[nodeCount];
        int pass = 0;
        while (pass < nodeCount)
        {
            int index = 0;
            while (index < links.Length)
            {
                int nextLayer = layers[links[index].SourceIndex] + 1;
                if (nextLayer > layers[links[index].TargetIndex])
                {
                    layers[links[index].TargetIndex] = nextLayer;
                }

                index = index + 1;
            }

            pass = pass + 1;
        }

        return layers;
    }

    internal static double SankeyNodeValue(int nodeIndex, ChartGraphLink[] links)
    {
        double input = 0.0;
        double output = 0.0;
        int index = 0;
        while (index < links.Length)
        {
            if (links[index].SourceIndex == nodeIndex)
            {
                output = output + links[index].Value;
            }

            if (links[index].TargetIndex == nodeIndex)
            {
                input = input + links[index].Value;
            }

            index = index + 1;
        }

        if (input > output)
        {
            return input;
        }

        return output;
    }

    internal static double SankeyLayerTotal(int[] layers, int layer, ChartGraphLink[] links)
    {
        double total = 0.0;
        int index = 0;
        while (index < layers.Length)
        {
            if (layers[index] == layer)
            {
                total = total + SankeyNodeValue(index, links);
            }

            index = index + 1;
        }

        return total;
    }

    internal static double LayerX(ChartRect bounds, double nodeWidth, int layer, int maximumLayer)
    {
        if (maximumLayer <= 0)
        {
            return bounds.X + bounds.Width * 0.5 - nodeWidth * 0.5;
        }

        return bounds.X + (bounds.Width - nodeWidth) * layer / maximumLayer;
    }

    internal static int CountLayer(int[] layers, int layer)
    {
        int count = 0;
        int index = 0;
        while (index < layers.Length)
        {
            if (layers[index] == layer)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static int LayerOrdinal(int[] layers, int nodeIndex)
    {
        int ordinal = 0;
        int index = 0;
        while (index < nodeIndex)
        {
            if (layers[index] == layers[nodeIndex])
            {
                ordinal = ordinal + 1;
            }

            index = index + 1;
        }

        return ordinal;
    }

    internal static double SankeyLayerOffset(int[] layers, ChartGraphLink[] links, int layer, int nodeIndex, double availableHeight, double nodePadding)
    {
        double offset = 0.0;
        double layerTotal = SankeyLayerTotal(layers, layer, links);
        int index = 0;
        while (index < nodeIndex)
        {
            if (layers[index] == layer)
            {
                double height = availableHeight / CountLayer(layers, layer);
                if (layerTotal > 0.0)
                {
                    height = availableHeight * SankeyNodeValue(index, links) / layerTotal;
                }

                offset = offset + height + nodePadding;
            }

            index = index + 1;
        }

        return offset;
    }

    internal static int MaximumInt(int[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (values.Length == 0)
        {
            throw new System.ArgumentException("Values cannot be empty.");
        }

        int maximum = values[0];
        int index = 1;
        while (index < values.Length)
        {
            if (values[index] > maximum)
            {
                maximum = values[index];
            }

            index = index + 1;
        }

        return maximum;
    }

    internal static ChartPoint TreePoint(ChartRect bounds, double depthAmount, double orderAmount, ChartOrientation orientation)
    {
        if (orientation == ChartOrientation.Horizontal)
        {
            return new ChartPoint(bounds.X + bounds.Width * depthAmount, bounds.Y + bounds.Height * orderAmount);
        }

        return new ChartPoint(bounds.X + bounds.Width * orderAmount, bounds.Y + bounds.Height * depthAmount);
    }

    internal static double AxisX(ChartRect bounds, int axis, int axisCount)
    {
        if (axisCount <= 1)
        {
            return bounds.X + bounds.Width * 0.5;
        }

        return bounds.X + bounds.Width * axis / (double)(axisCount - 1);
    }

    internal static double MapParallelY(double value, double minimum, double maximum, ChartRect bounds)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        if (minimum == maximum)
        {
            return bounds.Y + bounds.Height * 0.5;
        }

        double amount = (value - minimum) / (maximum - minimum);
        return bounds.Bottom() - bounds.Height * amount;
    }

    internal static double ColumnMinimum(ChartDataMatrix matrix, int column)
    {
        matrix.ValidateCoordinates(0, column);
        double minimum = matrix.GetValue(0, column);
        int row = 1;
        while (row < matrix.Rows)
        {
            double value = matrix.GetValue(row, column);
            if (value < minimum)
            {
                minimum = value;
            }

            row = row + 1;
        }

        return minimum;
    }

    internal static double ColumnMaximum(ChartDataMatrix matrix, int column)
    {
        matrix.ValidateCoordinates(0, column);
        double maximum = matrix.GetValue(0, column);
        int row = 1;
        while (row < matrix.Rows)
        {
            double value = matrix.GetValue(row, column);
            if (value > maximum)
            {
                maximum = value;
            }

            row = row + 1;
        }

        return maximum;
    }

    internal static void AddCategoryBands(ChartParallelCategoriesLayout layout, string[] values, int rowCount, int axisCount, int axis, double x, ChartRect bounds)
    {
        int categoryCount = UniqueCategoryCount(values, rowCount, axisCount, axis);
        double cursorY = bounds.Y;
        int categoryIndex = 0;
        while (categoryIndex < categoryCount)
        {
            string category = UniqueCategoryAt(values, rowCount, axisCount, axis, categoryIndex);
            int count = CountCategory(values, rowCount, axisCount, axis, category);
            double height = bounds.Height * count / rowCount;
            layout.AddBand(new ChartParallelCategoryBand(axis, categoryIndex, category, x, cursorY, height, count));
            cursorY = cursorY + height;
            categoryIndex = categoryIndex + 1;
        }
    }

    internal static string CategoryValue(string[] values, int row, int axis, int axisCount)
    {
        return values[row * axisCount + axis];
    }

    internal static int UniqueCategoryCount(string[] values, int rowCount, int axisCount, int axis)
    {
        int count = 0;
        int row = 0;
        while (row < rowCount)
        {
            string category = CategoryValue(values, row, axis, axisCount);
            if (FirstCategoryRow(values, rowCount, axisCount, axis, category) == row)
            {
                count = count + 1;
            }

            row = row + 1;
        }

        return count;
    }

    internal static string UniqueCategoryAt(string[] values, int rowCount, int axisCount, int axis, int categoryIndex)
    {
        int ordinal = 0;
        int row = 0;
        while (row < rowCount)
        {
            string category = CategoryValue(values, row, axis, axisCount);
            if (FirstCategoryRow(values, rowCount, axisCount, axis, category) == row)
            {
                if (ordinal == categoryIndex)
                {
                    return category;
                }

                ordinal = ordinal + 1;
            }

            row = row + 1;
        }

        throw new System.ArgumentOutOfRangeException("categoryIndex");
    }

    internal static int FirstCategoryRow(string[] values, int rowCount, int axisCount, int axis, string category)
    {
        int row = 0;
        while (row < rowCount)
        {
            if (CategoryValue(values, row, axis, axisCount) == category)
            {
                return row;
            }

            row = row + 1;
        }

        return -1;
    }

    internal static int CountCategory(string[] values, int rowCount, int axisCount, int axis, string category)
    {
        int count = 0;
        int row = 0;
        while (row < rowCount)
        {
            if (CategoryValue(values, row, axis, axisCount) == category)
            {
                count = count + 1;
            }

            row = row + 1;
        }

        return count;
    }

    internal static int FindBand(ChartParallelCategoriesLayout layout, int axis, string category)
    {
        int index = 0;
        while (index < layout.BandCount)
        {
            if (layout.Bands[index].AxisIndex == axis && layout.Bands[index].Category == category)
            {
                return index;
            }

            index = index + 1;
        }

        throw new System.ArgumentException("Parallel category band was not found.");
    }

    internal static void ValidateLabelsLinksBounds(string[] labels, ChartGraphLink[] links, ChartRect bounds)
    {
        ValidateLabels(labels);
        ValidateLinks(links, labels.Length);
        ChartAxis.ValidatePlotArea(bounds);
    }

    internal static void ValidateLabels(string[] labels)
    {
        if (labels == null)
        {
            throw new System.ArgumentNullException("labels");
        }

        if (labels.Length == 0)
        {
            throw new System.ArgumentException("Labels cannot be empty.");
        }

        int index = 0;
        while (index < labels.Length)
        {
            ChartTextBlock.ValidateText(labels[index], "labels");
            index = index + 1;
        }
    }

    internal static void ValidateLinks(ChartGraphLink[] links, int nodeCount)
    {
        if (links == null)
        {
            throw new System.ArgumentNullException("links");
        }

        int index = 0;
        while (index < links.Length)
        {
            if (links[index] == null)
            {
                throw new System.ArgumentException("Graph links cannot contain null values.");
            }

            if (links[index].SourceIndex >= nodeCount || links[index].TargetIndex >= nodeCount)
            {
                throw new System.ArgumentOutOfRangeException("links");
            }

            index = index + 1;
        }
    }

    internal static void ValidateHierarchyLayoutInputs(ChartHierarchyData data, ChartRect bounds, ChartOrientation orientation)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        ChartAxis.ValidatePlotArea(bounds);
        if (orientation != ChartOrientation.Horizontal && orientation != ChartOrientation.Vertical)
        {
            throw new System.ArgumentOutOfRangeException("orientation");
        }
    }

    internal static void ValidateParallelInputs(string[] axisLabels, ChartDataMatrix values, ChartRect bounds)
    {
        ValidateLabels(axisLabels);
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (axisLabels.Length != values.Columns)
        {
            throw new System.ArgumentException("Axis label count must match matrix columns.");
        }

        if (values.Columns < 2)
        {
            throw new System.ArgumentException("Parallel coordinates require at least two axes.");
        }

        ChartAxis.ValidatePlotArea(bounds);
        int index = 0;
        while (index < values.Values.Length)
        {
            ChartLinearScale.ValidateFinite(values.Values[index], "values");
            index = index + 1;
        }
    }

    internal static void ValidateParallelCategoryInputs(string[] axisLabels, string[] values, int rowCount, int axisCount, ChartRect bounds)
    {
        ValidateLabels(axisLabels);
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (rowCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("rowCount");
        }

        if (axisCount < 2)
        {
            throw new System.ArgumentOutOfRangeException("axisCount");
        }

        if (axisLabels.Length != axisCount)
        {
            throw new System.ArgumentException("Axis label count must match axis count.");
        }

        if (values.Length != (long)rowCount * axisCount)
        {
            throw new System.ArgumentException("Category value count must match rows times axes.");
        }

        ChartAxis.ValidatePlotArea(bounds);
        int index = 0;
        while (index < values.Length)
        {
            ChartTextBlock.ValidateText(values[index], "values");
            index = index + 1;
        }
    }
}
