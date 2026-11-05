namespace barcode;

internal class RenderRectangleSet
{
    internal BarcodeRectangle[] ModuleRectangles =
        new BarcodeRectangle[0];
    internal BarcodeRectangle[] PixelRectangles =
        new BarcodeRectangle[0];
}

internal class RenderModuleRectangle
{
    internal int X;
    internal int Y;
    internal int Width;
    internal int Height;
    internal bool Continued;
}

internal static class RectangleCoalescer
{
    internal static RenderRectangleSet Coalesce(RenderPlan? plan)
    {
        if (plan == null)
        {
            throw new BarcodeRenderingException(
                "A render plan is required for rectangle coalescing.");
        }

        ValidatePlan(plan);
        RenderPlan validPlan = plan;
        BarcodeSymbol symbol = validPlan.Symbol;
        byte[] modules = symbol.Modules == null
            ? new byte[0]
            : symbol.Modules;
        global::System.Collections.Generic.List<RenderModuleRectangle> rectangles =
            CoalesceModuleRuns(
                modules,
                symbol.WidthModules,
                symbol.HeightModules);
        SortRectangles(rectangles);

        BarcodeRectangle[] moduleRectangles =
            new BarcodeRectangle[rectangles.Count];
        BarcodeRectangle[] pixelRectangles =
            new BarcodeRectangle[rectangles.Count];
        int index = 0;
        while (index < rectangles.Count)
        {
            RenderModuleRectangle rectangle = rectangles[index];
            BarcodeRectangle moduleRectangle = CreateModuleRectangle(
                validPlan,
                rectangle);
            BarcodeRectangle pixelRectangle = CreatePixelRectangle(
                validPlan,
                rectangle);
            moduleRectangles[index] = RotateRectangle(
                moduleRectangle,
                validPlan.UnrotatedWidthModules,
                validPlan.UnrotatedHeightModules,
                validPlan.Rotation);
            pixelRectangles[index] = RotateRectangle(
                pixelRectangle,
                validPlan.UnrotatedWidthPixels,
                validPlan.UnrotatedHeightPixels,
                validPlan.Rotation);
            index = index + 1;
        }

        SortPublicRectangles(moduleRectangles);
        SortPublicRectangles(pixelRectangles);
        RenderRectangleSet result = new RenderRectangleSet();
        result.ModuleRectangles = moduleRectangles;
        result.PixelRectangles = pixelRectangles;
        return result;
    }

    static global::System.Collections.Generic.List<RenderModuleRectangle> CoalesceModuleRuns(
        byte[] modules,
        int width,
        int height)
    {
        global::System.Collections.Generic.List<RenderModuleRectangle> completed =
            new global::System.Collections.Generic.List<RenderModuleRectangle>();
        global::System.Collections.Generic.List<RenderModuleRectangle> active =
            new global::System.Collections.Generic.List<RenderModuleRectangle>();
        int row = 0;
        while (row < height)
        {
            MarkNotContinued(active);
            global::System.Collections.Generic.List<RenderModuleRectangle> nextActive =
                new global::System.Collections.Generic.List<RenderModuleRectangle>();
            int column = 0;
            while (column < width)
            {
                while (column < width
                    && modules[(row * width) + column] == 0)
                {
                    column = column + 1;
                }

                if (column >= width)
                {
                    break;
                }

                int runStart = column;
                while (column < width
                    && modules[(row * width) + column] != 0)
                {
                    column = column + 1;
                }

                int runWidth = column - runStart;
                RenderModuleRectangle? continued = FindMatchingActive(
                    active,
                    runStart,
                    runWidth);
                if (continued == null)
                {
                    continued = new RenderModuleRectangle();
                    continued.X = runStart;
                    continued.Y = row;
                    continued.Width = runWidth;
                    continued.Height = 1;
                }
                else
                {
                    continued.Height = continued.Height + 1;
                    continued.Continued = true;
                }

                nextActive.Add(continued);
            }

            CompleteDiscontinued(active, completed);
            active = nextActive;
            row = row + 1;
        }

        AppendRectangles(completed, active);
        return completed;
    }

    static RenderModuleRectangle? FindMatchingActive(
        global::System.Collections.Generic.List<RenderModuleRectangle> active,
        int x,
        int width)
    {
        int index = 0;
        while (index < active.Count)
        {
            RenderModuleRectangle rectangle = active[index];
            if (!rectangle.Continued
                && rectangle.X == x
                && rectangle.Width == width)
            {
                return rectangle;
            }

            index = index + 1;
        }

        return null;
    }

    static void MarkNotContinued(
        global::System.Collections.Generic.List<RenderModuleRectangle> active)
    {
        int index = 0;
        while (index < active.Count)
        {
            active[index].Continued = false;
            index = index + 1;
        }
    }

    static void CompleteDiscontinued(
        global::System.Collections.Generic.List<RenderModuleRectangle> active,
        global::System.Collections.Generic.List<RenderModuleRectangle> completed)
    {
        int index = 0;
        while (index < active.Count)
        {
            if (!active[index].Continued)
            {
                completed.Add(active[index]);
            }

            index = index + 1;
        }
    }

    static void AppendRectangles(
        global::System.Collections.Generic.List<RenderModuleRectangle> target,
        global::System.Collections.Generic.List<RenderModuleRectangle> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    static BarcodeRectangle CreateModuleRectangle(
        RenderPlan plan,
        RenderModuleRectangle source)
    {
        BarcodeRectangle rectangle = new BarcodeRectangle();
        rectangle.X = plan.ContentXModules + source.X;
        rectangle.Y = plan.ContentYModules + source.Y;
        rectangle.Width = source.Width;
        rectangle.Height = source.Height;
        return rectangle;
    }

    static BarcodeRectangle CreatePixelRectangle(
        RenderPlan plan,
        RenderModuleRectangle source)
    {
        BarcodeRectangle rectangle = new BarcodeRectangle();
        rectangle.X = plan.ContentXPixels
            + (source.X * plan.ModuleScale);
        rectangle.Width = source.Width * plan.ModuleScale;
        if (plan.Symbol.Dimension == BarcodeDimension.Linear)
        {
            rectangle.Y = plan.ContentYPixels;
            rectangle.Height = plan.BarHeightPixels;
        }
        else
        {
            rectangle.Y = plan.ContentYPixels
                + (source.Y * plan.ModuleScale);
            rectangle.Height = source.Height * plan.ModuleScale;
        }

        return rectangle;
    }

    static BarcodeRectangle RotateRectangle(
        BarcodeRectangle source,
        int unrotatedWidth,
        int unrotatedHeight,
        BarcodeRotation rotation)
    {
        BarcodeRectangle result = new BarcodeRectangle();
        switch (rotation)
        {
            case BarcodeRotation.None:
                result.X = source.X;
                result.Y = source.Y;
                result.Width = source.Width;
                result.Height = source.Height;
                break;
            case BarcodeRotation.Clockwise90:
                result.X = unrotatedHeight
                    - source.Y
                    - source.Height;
                result.Y = source.X;
                result.Width = source.Height;
                result.Height = source.Width;
                break;
            case BarcodeRotation.Clockwise180:
                result.X = unrotatedWidth
                    - source.X
                    - source.Width;
                result.Y = unrotatedHeight
                    - source.Y
                    - source.Height;
                result.Width = source.Width;
                result.Height = source.Height;
                break;
            case BarcodeRotation.Clockwise270:
                result.X = source.Y;
                result.Y = unrotatedWidth
                    - source.X
                    - source.Width;
                result.Width = source.Height;
                result.Height = source.Width;
                break;
            default:
                break;
        }

        return result;
    }

    static void SortRectangles(
        global::System.Collections.Generic.List<RenderModuleRectangle> rectangles)
    {
        int index = 1;
        while (index < rectangles.Count)
        {
            RenderModuleRectangle current = rectangles[index];
            int previous = index - 1;
            while (previous >= 0
                && CompareModuleRectangles(
                    rectangles[previous],
                    current) > 0)
            {
                rectangles[previous + 1] = rectangles[previous];
                previous = previous - 1;
            }

            rectangles[previous + 1] = current;
            index = index + 1;
        }
    }

    static int CompareModuleRectangles(
        RenderModuleRectangle first,
        RenderModuleRectangle second)
    {
        if (first.Y != second.Y)
        {
            return first.Y < second.Y ? -1 : 1;
        }

        if (first.X != second.X)
        {
            return first.X < second.X ? -1 : 1;
        }

        if (first.Height != second.Height)
        {
            return first.Height < second.Height ? -1 : 1;
        }

        if (first.Width != second.Width)
        {
            return first.Width < second.Width ? -1 : 1;
        }

        return 0;
    }

    static void SortPublicRectangles(BarcodeRectangle[] rectangles)
    {
        int index = 1;
        while (index < rectangles.Length)
        {
            BarcodeRectangle current = rectangles[index];
            int previous = index - 1;
            while (previous >= 0
                && ComparePublicRectangles(
                    rectangles[previous],
                    current) > 0)
            {
                rectangles[previous + 1] = rectangles[previous];
                previous = previous - 1;
            }

            rectangles[previous + 1] = current;
            index = index + 1;
        }
    }

    static int ComparePublicRectangles(
        BarcodeRectangle first,
        BarcodeRectangle second)
    {
        if (first.Y != second.Y)
        {
            return first.Y < second.Y ? -1 : 1;
        }

        if (first.X != second.X)
        {
            return first.X < second.X ? -1 : 1;
        }

        if (first.Height != second.Height)
        {
            return first.Height < second.Height ? -1 : 1;
        }

        if (first.Width != second.Width)
        {
            return first.Width < second.Width ? -1 : 1;
        }

        return 0;
    }

    static void ValidatePlan(RenderPlan plan)
    {
        if (plan.Symbol == null)
        {
            throw new BarcodeRenderingException(
                "A render plan is required for rectangle coalescing.");
        }

        BarcodeSymbol symbol = plan.Symbol;
        if (symbol.WidthModules <= 0
            || symbol.HeightModules <= 0
            || symbol.Modules == null
            || (long)symbol.WidthModules * (long)symbol.HeightModules
                != symbol.Modules.Length)
        {
            throw new BarcodeRenderingException(
                "The render plan contains invalid symbol modules.");
        }

        if (symbol.Dimension == BarcodeDimension.Linear
            && symbol.HeightModules != 1)
        {
            throw new BarcodeRenderingException(
                "A linear render plan must contain one logical module row.");
        }

        int dimensionValue = (int)symbol.Dimension;
        if (dimensionValue < (int)BarcodeDimension.Linear
            || dimensionValue > (int)BarcodeDimension.StackedLinear)
        {
            throw new BarcodeRenderingException(
                "The render plan contains an invalid barcode dimension.");
        }

        int rotationValue = (int)plan.Rotation;
        if (rotationValue < (int)BarcodeRotation.None
            || rotationValue > (int)BarcodeRotation.Clockwise270)
        {
            throw new BarcodeRenderingException(
                "The render plan contains an invalid rotation.");
        }

        if (plan.ModuleScale <= 0
            || plan.BarHeightPixels <= 0
            || plan.ContentXModules < 0
            || plan.ContentYModules < 0
            || plan.ContentXPixels < 0
            || plan.ContentYPixels < 0
            || plan.UnrotatedWidthModules <= 0
            || plan.UnrotatedHeightModules <= 0
            || plan.UnrotatedWidthPixels <= 0
            || plan.UnrotatedHeightPixels <= 0)
        {
            throw new BarcodeRenderingException(
                "The render plan dimensions are invalid.");
        }

        long moduleRight = (long)plan.ContentXModules
            + (long)symbol.WidthModules;
        long moduleBottom = (long)plan.ContentYModules
            + (long)symbol.HeightModules;
        long pixelRight = (long)plan.ContentXPixels
            + ((long)symbol.WidthModules * (long)plan.ModuleScale);
        long pixelContentHeight = symbol.Dimension
                == BarcodeDimension.Linear
            ? plan.BarHeightPixels
            : (long)symbol.HeightModules * (long)plan.ModuleScale;
        long pixelBottom = (long)plan.ContentYPixels
            + pixelContentHeight;
        if (moduleRight > plan.UnrotatedWidthModules
            || moduleBottom > plan.UnrotatedHeightModules
            || pixelRight > plan.UnrotatedWidthPixels
            || pixelBottom > plan.UnrotatedHeightPixels)
        {
            throw new BarcodeRenderingException(
                "The symbol content extends outside the render plan.");
        }
    }
}
