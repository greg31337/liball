namespace chart;

internal class ChartRasterCanvas
{
    internal RawBitmap Bitmap;
    internal int ClipX;
    internal int ClipY;
    internal int ClipWidth;
    internal int ClipHeight;
    internal const double TwoPi = 6.2831853071795862;

    internal ChartRasterCanvas(RawBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        Bitmap = bitmap;
        ResetClip();
    }

    internal void ResetClip()
    {
        ClipX = 0;
        ClipY = 0;
        ClipWidth = Bitmap.Width;
        ClipHeight = Bitmap.Height;
    }

    internal void SetClip(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            ClipX = 0;
            ClipY = 0;
            ClipWidth = 0;
            ClipHeight = 0;
            return;
        }

        int left = System.Math.Max(0, x);
        int top = System.Math.Max(0, y);
        long rightEdge = (long)x + width;
        long bottomEdge = (long)y + height;
        int right = Bitmap.Width;
        int bottom = Bitmap.Height;
        if (rightEdge < right)
        {
            right = rightEdge <= 0 ? 0 : (int)rightEdge;
        }

        if (bottomEdge < bottom)
        {
            bottom = bottomEdge <= 0 ? 0 : (int)bottomEdge;
        }

        if (right <= left || bottom <= top)
        {
            ClipX = 0;
            ClipY = 0;
            ClipWidth = 0;
            ClipHeight = 0;
            return;
        }

        ClipX = left;
        ClipY = top;
        ClipWidth = right - left;
        ClipHeight = bottom - top;
    }

    internal void SetClipRect(ChartRect rect)
    {
        if (rect == null)
        {
            throw new System.ArgumentNullException("rect");
        }

        int left = FloorToRange(rect.Left(), 0, Bitmap.Width);
        int top = FloorToRange(rect.Top(), 0, Bitmap.Height);
        int right = CeilingToRange(rect.Right(), 0, Bitmap.Width);
        int bottom = CeilingToRange(rect.Bottom(), 0, Bitmap.Height);
        SetClip(left, top, right - left, bottom - top);
    }

    internal bool ContainsClipPixel(int x, int y)
    {
        return x >= ClipX && y >= ClipY && x < ClipX + ClipWidth && y < ClipY + ClipHeight;
    }

    internal void Clear(ChartColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        int y = 0;
        while (y < Bitmap.Height)
        {
            int x = 0;
            while (x < Bitmap.Width)
            {
                Bitmap.SetPixel(x, y, color);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void ClearClip(ChartColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        int y = ClipY;
        while (y < ClipY + ClipHeight)
        {
            int x = ClipX;
            while (x < ClipX + ClipWidth)
            {
                Bitmap.SetPixel(x, y, color);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal bool WritePixel(int x, int y, ChartColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        if (!Bitmap.ContainsPixel(x, y))
        {
            return false;
        }

        if (!ContainsClipPixel(x, y))
        {
            return false;
        }

        Bitmap.SetPixel(x, y, color);
        return true;
    }

    internal bool BlendPixel(int x, int y, ChartColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        if (!Bitmap.ContainsPixel(x, y))
        {
            return false;
        }

        if (!ContainsClipPixel(x, y))
        {
            return false;
        }

        ChartColor destination = Bitmap.GetPixel(x, y);
        Bitmap.SetPixel(x, y, SourceOver(color, destination));
        return true;
    }

    internal bool BlendPixelWithMode(int x, int y, ChartColor color, ChartBlendMode blendMode)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        if (!Bitmap.ContainsPixel(x, y) || !ContainsClipPixel(x, y))
        {
            return false;
        }

        ChartColor destination = Bitmap.GetPixel(x, y);
        Bitmap.SetPixel(x, y, Composite(color, destination, blendMode));
        return true;
    }

    internal bool DrawPixel(int x, int y, ChartColor color, ChartBlendMode blendMode)
    {
        if (blendMode == ChartBlendMode.Source)
        {
            return WritePixel(x, y, color);
        }

        if (blendMode == ChartBlendMode.Normal || blendMode == ChartBlendMode.SourceOver)
        {
            return BlendPixel(x, y, color);
        }

        return BlendPixelWithMode(x, y, color, blendMode);
    }

    internal void DrawImage(RawBitmap image, double x, double y, double width, double height, double opacity, ChartBlendMode blendMode)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        ChartRenderStyle.ValidatePositive(width, "width");
        ChartRenderStyle.ValidatePositive(height, "height");
        ChartStyle.ValidateOpacity(opacity, "opacity");
        int left = (int)System.Math.Floor(x);
        int top = (int)System.Math.Floor(y);
        int right = (int)System.Math.Ceiling(x + width);
        int bottom = (int)System.Math.Ceiling(y + height);
        int destinationY = top;
        while (destinationY < bottom)
        {
            double unitY = (destinationY + 0.5 - y) / height;
            int sourceY = (int)System.Math.Floor(unitY * image.Height);
            if (sourceY < 0)
            {
                sourceY = 0;
            }
            if (sourceY >= image.Height)
            {
                sourceY = image.Height - 1;
            }

            int destinationX = left;
            while (destinationX < right)
            {
                double unitX = (destinationX + 0.5 - x) / width;
                int sourceX = (int)System.Math.Floor(unitX * image.Width);
                if (sourceX < 0)
                {
                    sourceX = 0;
                }
                if (sourceX >= image.Width)
                {
                    sourceX = image.Width - 1;
                }

                ChartColor color = ChartRenderStyle.ApplyOpacity(image.GetPixel(sourceX, sourceY), opacity);
                DrawPixel(destinationX, destinationY, color, blendMode);
                destinationX = destinationX + 1;
            }

            destinationY = destinationY + 1;
        }
    }

    internal void DrawTransformedImage(RawBitmap image, double x, double y, double width, double height, ChartMatrix transform, double opacity, ChartRenderQuality quality, ChartBlendMode blendMode)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }
        if (transform == null)
        {
            throw new System.ArgumentNullException("transform");
        }

        ChartRenderStyle.ValidatePositive(width, "width");
        ChartRenderStyle.ValidatePositive(height, "height");
        ChartStyle.ValidateOpacity(opacity, "opacity");
        ChartMatrix? inverse = transform.Invert();
        if (inverse == null)
        {
            return;
        }

        ChartRect transformed = ChartRenderScene.TransformBounds(new ChartRect(x, y, width, height), transform);
        int left = FloorToRange(transformed.Left(), ClipX, ClipX + ClipWidth);
        int top = FloorToRange(transformed.Top(), ClipY, ClipY + ClipHeight);
        int right = CeilingToRange(transformed.Right(), ClipX, ClipX + ClipWidth);
        int bottom = CeilingToRange(transformed.Bottom(), ClipY, ClipY + ClipHeight);
        int destinationY = top;
        while (destinationY < bottom)
        {
            int destinationX = left;
            while (destinationX < right)
            {
                int sampleCount = 1;
                if (quality == ChartRenderQuality.Antialiased)
                {
                    sampleCount = 4;
                }

                double alphaTotal = 0.0;
                double redTotal = 0.0;
                double greenTotal = 0.0;
                double blueTotal = 0.0;
                int sampleY = 0;
                while (sampleY < sampleCount)
                {
                    int sampleX = 0;
                    while (sampleX < sampleCount)
                    {
                        double sampleOffsetX = (sampleX + 0.5) / sampleCount;
                        double sampleOffsetY = (sampleY + 0.5) / sampleCount;
                        ChartPoint local = inverse.TransformPoint(new ChartPoint(destinationX + sampleOffsetX, destinationY + sampleOffsetY));
                        if (local.X >= x && local.X < x + width && local.Y >= y && local.Y < y + height)
                        {
                            int sourceX = (int)System.Math.Floor((local.X - x) / width * image.Width);
                            int sourceY = (int)System.Math.Floor((local.Y - y) / height * image.Height);
                            sourceX = System.Math.Max(0, System.Math.Min(image.Width - 1, sourceX));
                            sourceY = System.Math.Max(0, System.Math.Min(image.Height - 1, sourceY));
                            ChartColor sample = image.GetPixel(sourceX, sourceY);
                            double sampleAlpha = sample.A / 255.0;
                            alphaTotal = alphaTotal + sampleAlpha;
                            redTotal = redTotal + sample.R * sampleAlpha;
                            greenTotal = greenTotal + sample.G * sampleAlpha;
                            blueTotal = blueTotal + sample.B * sampleAlpha;
                        }

                        sampleX = sampleX + 1;
                    }

                    sampleY = sampleY + 1;
                }

                if (alphaTotal > 0.0)
                {
                    double divisor = sampleCount * sampleCount;
                    ChartColor color = new ChartColor(ChartColor.RoundToByte(redTotal / alphaTotal), ChartColor.RoundToByte(greenTotal / alphaTotal), ChartColor.RoundToByte(blueTotal / alphaTotal), ChartColor.RoundToByte(alphaTotal / divisor * 255.0));
                    DrawPixel(destinationX, destinationY, ChartRenderStyle.ApplyOpacity(color, opacity), blendMode);
                }

                destinationX = destinationX + 1;
            }

            destinationY = destinationY + 1;
        }
    }

    internal void DrawLineAliased(int x0, int y0, int x1, int y1, ChartColor color, ChartBlendMode blendMode)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        long dx = System.Math.Abs((long)x1 - x0);
        int sx = -1;
        if (x0 < x1)
        {
            sx = 1;
        }

        long dy = 0 - System.Math.Abs((long)y1 - y0);
        int sy = -1;
        if (y0 < y1)
        {
            sy = 1;
        }

        long error = dx + dy;

        while (true)
        {
            DrawPixel(x0, y0, color, blendMode);

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            long doubledError = error * 2;
            if (doubledError >= dy)
            {
                error = error + dy;
                x0 = x0 + sx;
            }

            if (doubledError <= dx)
            {
                error = error + dx;
                y0 = y0 + sy;
            }
        }
    }

    internal void DrawLineAntialiased(double x0, double y0, double x1, double y1, ChartColor color, ChartBlendMode blendMode)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        if (x0 == x1 && y0 == y1)
        {
            DrawPixel(RoundToInt(x0), RoundToInt(y0), color, blendMode);
            return;
        }

        bool steep = System.Math.Abs(y1 - y0) > System.Math.Abs(x1 - x0);
        if (steep)
        {
            double swap = x0;
            x0 = y0;
            y0 = swap;
            swap = x1;
            x1 = y1;
            y1 = swap;
        }

        if (x0 > x1)
        {
            double swap = x0;
            x0 = x1;
            x1 = swap;
            swap = y0;
            y0 = y1;
            y1 = swap;
        }

        double dx = x1 - x0;
        if (dx == 0.0)
        {
            DrawLineAliased(RoundToInt(x0), RoundToInt(y0), RoundToInt(x1), RoundToInt(y1), color, blendMode);
            return;
        }

        double gradient = (y1 - y0) / dx;

        double firstEndX = RoundDouble(x0);
        double firstEndY = y0 + gradient * (firstEndX - x0);
        double firstGap = ReverseFractionalPart(x0 + 0.5);
        int firstPixelX = (int)firstEndX;
        int firstPixelY = (int)System.Math.Floor(firstEndY);
        PlotCoverage(steep, firstPixelX, firstPixelY, ReverseFractionalPart(firstEndY) * firstGap, color, blendMode);
        PlotCoverage(steep, firstPixelX, firstPixelY + 1, FractionalPart(firstEndY) * firstGap, color, blendMode);

        double intery = firstEndY + gradient;

        double secondEndX = RoundDouble(x1);
        double secondEndY = y1 + gradient * (secondEndX - x1);
        double secondGap = FractionalPart(x1 + 0.5);
        int secondPixelX = (int)secondEndX;
        int secondPixelY = (int)System.Math.Floor(secondEndY);
        PlotCoverage(steep, secondPixelX, secondPixelY, ReverseFractionalPart(secondEndY) * secondGap, color, blendMode);
        PlotCoverage(steep, secondPixelX, secondPixelY + 1, FractionalPart(secondEndY) * secondGap, color, blendMode);

        int x = firstPixelX + 1;
        while (x < secondPixelX)
        {
            int y = (int)System.Math.Floor(intery);
            PlotCoverage(steep, x, y, ReverseFractionalPart(intery), color, blendMode);
            PlotCoverage(steep, x, y + 1, FractionalPart(intery), color, blendMode);
            intery = intery + gradient;
            x = x + 1;
        }
    }

    internal void DrawLine(double x0, double y0, double x1, double y1, ChartColor color, ChartRenderQuality quality, ChartBlendMode blendMode)
    {
        if (quality == ChartRenderQuality.Antialiased)
        {
            DrawLineAntialiased(x0, y0, x1, y1, color, blendMode);
            return;
        }

        DrawLineAliased(RoundToInt(x0), RoundToInt(y0), RoundToInt(x1), RoundToInt(y1), color, blendMode);
    }

    internal void PlotCoverage(bool steep, int x, int y, double coverage, ChartColor color, ChartBlendMode blendMode)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        double clamped = ChartColor.ClampDouble(coverage, 0.0, 1.0);
        byte alpha = ChartColor.RoundToByte(color.A * clamped);
        if (alpha == 0)
        {
            return;
        }

        ChartColor covered = color.WithAlpha(alpha);
        if (steep)
        {
            DrawPixel(y, x, covered, blendMode);
            return;
        }

        DrawPixel(x, y, covered, blendMode);
    }

    internal void DrawPolyline(ChartPoint[] points, double strokeWidth, ChartColor color, ChartLineCap lineCap, ChartLineJoin lineJoin, ChartBlendMode blendMode)
    {
        DrawPolylineWithStyle(points, strokeWidth, color, lineCap, lineJoin, new double[0], 4.0, blendMode);
    }

    internal void DrawPolylineWithStyle(
        ChartPoint[] points,
        double strokeWidth,
        ChartColor color,
        ChartLineCap lineCap,
        ChartLineJoin lineJoin,
        double[] dashPattern,
        double miterLimit,
        ChartBlendMode blendMode)
    {
        ValidatePolyline(points, strokeWidth, color, dashPattern, miterLimit);

        double radius = strokeWidth * 0.5;
        double pad = System.Math.Ceiling(radius * System.Math.Max(1.0, miterLimit)) + 1.0;
        int left = FloorToRange(PolylineMinimumX(points) - pad, ClipX, ClipX + ClipWidth);
        int top = FloorToRange(PolylineMinimumY(points) - pad, ClipY, ClipY + ClipHeight);
        int right = CeilingToRange(PolylineMaximumX(points) + pad, ClipX, ClipX + ClipWidth);
        int bottom = CeilingToRange(PolylineMaximumY(points) + pad, ClipY, ClipY + ClipHeight);

        double totalLength = PolylineLength(points);
        int y = top;
        while (y < bottom)
        {
            int x = left;
            while (x < right)
            {
                double px = x + 0.5;
                double py = y + 0.5;
                if (StrokeContainsPoint(points, px, py, radius, lineCap, lineJoin, dashPattern, miterLimit, totalLength))
                {
                    DrawPixel(x, y, color, blendMode);
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal static void ValidatePolyline(ChartPoint[] points, double strokeWidth, ChartColor color, double[] dashPattern, double miterLimit)
    {
        if (points == null)
        {
            throw new System.ArgumentNullException("points");
        }

        if (points.Length < 2)
        {
            throw new System.ArgumentException("A polyline requires at least two points.");
        }

        if (strokeWidth <= 0.0 || double.IsNaN(strokeWidth) || double.IsInfinity(strokeWidth))
        {
            throw new System.ArgumentOutOfRangeException("strokeWidth");
        }

        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        if (miterLimit <= 0.0 || double.IsNaN(miterLimit) || double.IsInfinity(miterLimit))
        {
            throw new System.ArgumentOutOfRangeException("miterLimit");
        }

        int index = 0;
        while (index < points.Length)
        {
            if (points[index] == null)
            {
                throw new System.ArgumentException("Polyline points cannot contain null values.");
            }

            index = index + 1;
        }

        ValidateDashPattern(dashPattern);
    }

    internal static void ValidateDashPattern(double[] dashPattern)
    {
        if (dashPattern == null)
        {
            return;
        }

        int index = 0;
        while (index < dashPattern.Length)
        {
            if (dashPattern[index] <= 0.0 || double.IsNaN(dashPattern[index]) || double.IsInfinity(dashPattern[index]))
            {
                throw new System.ArgumentOutOfRangeException("dashPattern");
            }

            index = index + 1;
        }
    }

    internal static bool StrokeContainsPoint(
        ChartPoint[] points,
        double px,
        double py,
        double radius,
        ChartLineCap lineCap,
        ChartLineJoin lineJoin,
        double[] dashPattern,
        double miterLimit,
        double totalLength)
    {
        double radiusSquared = radius * radius;
        double distanceAlong = 0.0;
        int segmentIndex = 0;
        while (segmentIndex < points.Length - 1)
        {
            ChartPoint start = points[segmentIndex];
            ChartPoint end = points[segmentIndex + 1];
            double length = start.DistanceTo(end);

            if (length > 0.0)
            {
                bool firstSegment = segmentIndex == 0;
                bool lastSegment = segmentIndex == points.Length - 2;
                if (SegmentStrokeContainsPoint(start, end, px, py, radius, radiusSquared, lineCap, firstSegment, lastSegment, dashPattern, distanceAlong, length, totalLength))
                {
                    return true;
                }
            }

            distanceAlong = distanceAlong + length;
            segmentIndex = segmentIndex + 1;
        }

        return JoinStrokeContainsPoint(points, px, py, radius, radiusSquared, lineJoin, dashPattern, miterLimit);
    }

    internal static bool SegmentStrokeContainsPoint(
        ChartPoint start,
        ChartPoint end,
        double px,
        double py,
        double radius,
        double radiusSquared,
        ChartLineCap lineCap,
        bool firstSegment,
        bool lastSegment,
        double[] dashPattern,
        double distanceAlong,
        double length,
        double totalLength)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double projectionDistance = ((px - start.X) * dx + (py - start.Y) * dy) / length;
        double projection = projectionDistance / length;
        double minimumProjection = 0.0;
        double maximumProjection = 1.0;

        if (firstSegment && lineCap == ChartLineCap.Square)
        {
            minimumProjection = 0.0 - radius / length;
        }

        if (lastSegment && lineCap == ChartLineCap.Square)
        {
            maximumProjection = 1.0 + radius / length;
        }

        if (projection >= minimumProjection && projection <= maximumProjection)
        {
            double nearestX = start.X + dx * projection;
            double nearestY = start.Y + dy * projection;
            double distanceSquared = DistanceSquared(px, py, nearestX, nearestY);
            if (distanceSquared <= radiusSquared)
            {
                double dashDistance = distanceAlong + ClampDouble(projectionDistance, 0.0, length);
                if (IsDashOn(dashDistance, dashPattern))
                {
                    return true;
                }
            }
        }

        if (firstSegment && lineCap == ChartLineCap.Round && DistanceSquared(px, py, start.X, start.Y) <= radiusSquared && IsDashOn(0.0, dashPattern))
        {
            return true;
        }

        if (lastSegment && lineCap == ChartLineCap.Round && DistanceSquared(px, py, end.X, end.Y) <= radiusSquared && IsDashOn(totalLength, dashPattern))
        {
            return true;
        }

        return false;
    }

    internal static bool JoinStrokeContainsPoint(
        ChartPoint[] points,
        double px,
        double py,
        double radius,
        double radiusSquared,
        ChartLineJoin lineJoin,
        double[] dashPattern,
        double miterLimit)
    {
        if (lineJoin == ChartLineJoin.Bevel)
        {
            return false;
        }

        double distanceAlong = 0.0;
        int index = 1;
        while (index < points.Length - 1)
        {
            ChartPoint previous = points[index - 1];
            ChartPoint current = points[index];
            ChartPoint next = points[index + 1];
            double previousLength = previous.DistanceTo(current);
            double nextLength = current.DistanceTo(next);
            distanceAlong = distanceAlong + previousLength;

            if (previousLength > 0.0 && nextLength > 0.0 && IsDashOn(distanceAlong, dashPattern))
            {
                double distanceSquared = DistanceSquared(px, py, current.X, current.Y);
                if (lineJoin == ChartLineJoin.Round && distanceSquared <= radiusSquared)
                {
                    return true;
                }

                if (lineJoin == ChartLineJoin.Miter && IsMiterWithinLimit(previous, current, next, miterLimit))
                {
                    double miterRadius = radius * MiterScale(previous, current, next);
                    if (distanceSquared <= miterRadius * miterRadius)
                    {
                        return true;
                    }
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsDashOn(double distance, double[] dashPattern)
    {
        if (dashPattern == null)
        {
            return true;
        }

        if (dashPattern.Length == 0)
        {
            return true;
        }

        double total = 0.0;
        int index = 0;
        while (index < dashPattern.Length)
        {
            total = total + dashPattern[index];
            index = index + 1;
        }

        int effectiveCount = dashPattern.Length;
        if (dashPattern.Length % 2 == 1)
        {
            total = total * 2.0;
            effectiveCount = dashPattern.Length * 2;
        }

        if (total <= 0.0)
        {
            return true;
        }

        double position = distance % total;
        index = 0;
        while (index < effectiveCount)
        {
            double length = dashPattern[index % dashPattern.Length];
            if (position < length)
            {
                return index % 2 == 0;
            }

            position = position - length;
            index = index + 1;
        }

        return true;
    }

    internal static bool IsMiterWithinLimit(ChartPoint previous, ChartPoint current, ChartPoint next, double miterLimit)
    {
        return MiterScale(previous, current, next) <= miterLimit;
    }

    internal static double MiterScale(ChartPoint previous, ChartPoint current, ChartPoint next)
    {
        double ax = previous.X - current.X;
        double ay = previous.Y - current.Y;
        double bx = next.X - current.X;
        double by = next.Y - current.Y;
        double alength = System.Math.Sqrt(ax * ax + ay * ay);
        double blength = System.Math.Sqrt(bx * bx + by * by);

        if (alength == 0.0 || blength == 0.0)
        {
            return 1.0;
        }

        ax = ax / alength;
        ay = ay / alength;
        bx = bx / blength;
        by = by / blength;

        double dot = ClampDouble(ax * bx + ay * by, -1.0, 1.0);
        double sineHalf = System.Math.Sqrt((1.0 - dot) * 0.5);
        if (sineHalf <= 0.000001)
        {
            return 1000000.0;
        }

        return 1.0 / sineHalf;
    }

    internal static double PolylineLength(ChartPoint[] points)
    {
        double length = 0.0;
        int index = 0;
        while (index < points.Length - 1)
        {
            length = length + points[index].DistanceTo(points[index + 1]);
            index = index + 1;
        }

        return length;
    }

    internal static double PolylineMinimumX(ChartPoint[] points)
    {
        double value = points[0].X;
        int index = 1;
        while (index < points.Length)
        {
            value = System.Math.Min(value, points[index].X);
            index = index + 1;
        }

        return value;
    }

    internal static double PolylineMinimumY(ChartPoint[] points)
    {
        double value = points[0].Y;
        int index = 1;
        while (index < points.Length)
        {
            value = System.Math.Min(value, points[index].Y);
            index = index + 1;
        }

        return value;
    }

    internal static double PolylineMaximumX(ChartPoint[] points)
    {
        double value = points[0].X;
        int index = 1;
        while (index < points.Length)
        {
            value = System.Math.Max(value, points[index].X);
            index = index + 1;
        }

        return value;
    }

    internal static double PolylineMaximumY(ChartPoint[] points)
    {
        double value = points[0].Y;
        int index = 1;
        while (index < points.Length)
        {
            value = System.Math.Max(value, points[index].Y);
            index = index + 1;
        }

        return value;
    }

    internal void FillRectangle(ChartRect rect, ChartColor color, ChartBlendMode blendMode)
    {
        if (rect == null)
        {
            throw new System.ArgumentNullException("rect");
        }

        FillAxisAlignedRectangle(rect.Left(), rect.Top(), rect.Right(), rect.Bottom(), color, blendMode);
    }

    internal void FillRectangleGradient(ChartRect rect, ChartPoint start, ChartPoint end, ChartColor[] colors, double[] offsets, double opacity, ChartBlendMode blendMode)
    {
        ChartRenderStyle.ValidateGradient(start, end, colors, offsets);
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double lengthSquared = dx * dx + dy * dy;
        int left = FloorToRange(rect.Left(), ClipX, ClipX + ClipWidth);
        int top = FloorToRange(rect.Top(), ClipY, ClipY + ClipHeight);
        int right = CeilingToRange(rect.Right(), ClipX, ClipX + ClipWidth);
        int bottom = CeilingToRange(rect.Bottom(), ClipY, ClipY + ClipHeight);
        int y = top;
        while (y < bottom)
        {
            int x = left;
            while (x < right)
            {
                double offset = 0.0;
                if (lengthSquared > 0.0)
                {
                    offset = ((x + 0.5 - start.X) * dx + (y + 0.5 - start.Y) * dy) / lengthSquared;
                }

                ChartColor color = GradientColor(colors, offsets, offset);
                DrawPixel(x, y, ChartRenderStyle.ApplyOpacity(color, opacity), blendMode);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void FillRectanglePattern(ChartRect rect, RawBitmap pattern, double opacity, ChartBlendMode blendMode)
    {
        if (pattern == null)
        {
            throw new System.ArgumentNullException("pattern");
        }

        int left = FloorToRange(rect.Left(), ClipX, ClipX + ClipWidth);
        int top = FloorToRange(rect.Top(), ClipY, ClipY + ClipHeight);
        int right = CeilingToRange(rect.Right(), ClipX, ClipX + ClipWidth);
        int bottom = CeilingToRange(rect.Bottom(), ClipY, ClipY + ClipHeight);
        int y = top;
        while (y < bottom)
        {
            int patternY = y % pattern.Height;
            if (patternY < 0)
            {
                patternY = patternY + pattern.Height;
            }

            int x = left;
            while (x < right)
            {
                int patternX = x % pattern.Width;
                if (patternX < 0)
                {
                    patternX = patternX + pattern.Width;
                }

                ChartColor color = ChartRenderStyle.ApplyOpacity(pattern.GetPixel(patternX, patternY), opacity);
                DrawPixel(x, y, color, blendMode);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal static ChartColor GradientColor(ChartColor[] colors, double[] offsets, double offset)
    {
        if (offset <= offsets[0])
        {
            return colors[0];
        }

        int last = colors.Length - 1;
        if (offset >= offsets[last])
        {
            return colors[last];
        }

        int index = 1;
        while (index < colors.Length)
        {
            if (offset <= offsets[index])
            {
                double range = offsets[index] - offsets[index - 1];
                double amount = 0.0;
                if (range > 0.0)
                {
                    amount = (offset - offsets[index - 1]) / range;
                }

                byte red = ChartColor.RoundToByte(colors[index - 1].R + (colors[index].R - colors[index - 1].R) * amount);
                byte green = ChartColor.RoundToByte(colors[index - 1].G + (colors[index].G - colors[index - 1].G) * amount);
                byte blue = ChartColor.RoundToByte(colors[index - 1].B + (colors[index].B - colors[index - 1].B) * amount);
                byte alpha = ChartColor.RoundToByte(colors[index - 1].A + (colors[index].A - colors[index - 1].A) * amount);
                return new ChartColor(red, green, blue, alpha);
            }

            index = index + 1;
        }

        return colors[last];
    }

    internal void StrokeRectangle(ChartRect rect, double strokeWidth, ChartColor color, ChartLineJoin lineJoin, ChartBlendMode blendMode)
    {
        if (rect == null)
        {
            throw new System.ArgumentNullException("rect");
        }

        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(rect.Left(), rect.Top()),
            new ChartPoint(rect.Right(), rect.Top()),
            new ChartPoint(rect.Right(), rect.Bottom()),
            new ChartPoint(rect.Left(), rect.Bottom()),
            new ChartPoint(rect.Left(), rect.Top())
        };

        DrawPolylineWithStyle(points, strokeWidth, color, ChartLineCap.Butt, lineJoin, new double[0], 4.0, blendMode);
    }

    internal void StrokeRectangleWithStyle(ChartRect rect, double strokeWidth, ChartColor color, ChartLineJoin lineJoin, double[] dashPattern, double miterLimit, ChartBlendMode blendMode)
    {
        if (rect == null)
        {
            throw new System.ArgumentNullException("rect");
        }

        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(rect.Left(), rect.Top()),
            new ChartPoint(rect.Right(), rect.Top()),
            new ChartPoint(rect.Right(), rect.Bottom()),
            new ChartPoint(rect.Left(), rect.Bottom()),
            new ChartPoint(rect.Left(), rect.Top())
        };
        DrawPolylineWithStyle(points, strokeWidth, color, ChartLineCap.Butt, lineJoin, dashPattern, miterLimit, blendMode);
    }

    internal void FillAxisAlignedRectangle(double leftValue, double topValue, double rightValue, double bottomValue, ChartColor color, ChartBlendMode blendMode)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        double leftEdge = System.Math.Min(leftValue, rightValue);
        double rightEdge = System.Math.Max(leftValue, rightValue);
        double topEdge = System.Math.Min(topValue, bottomValue);
        double bottomEdge = System.Math.Max(topValue, bottomValue);
        int left = FloorToRange(leftEdge, ClipX, ClipX + ClipWidth);
        int top = FloorToRange(topEdge, ClipY, ClipY + ClipHeight);
        int right = CeilingToRange(rightEdge, ClipX, ClipX + ClipWidth);
        int bottom = CeilingToRange(bottomEdge, ClipY, ClipY + ClipHeight);

        int y = top;
        while (y < bottom)
        {
            int x = left;
            while (x < right)
            {
                double px = x + 0.5;
                double py = y + 0.5;
                if (px >= leftEdge && px < rightEdge && py >= topEdge && py < bottomEdge)
                {
                    DrawPixel(x, y, color, blendMode);
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void FillPolygon(ChartPoint[] points, ChartColor color, ChartFillRule fillRule, ChartBlendMode blendMode)
    {
        ValidatePolygon(points, color);

        int left = FloorToRange(PolylineMinimumX(points), ClipX, ClipX + ClipWidth);
        int top = FloorToRange(PolylineMinimumY(points), ClipY, ClipY + ClipHeight);
        int right = CeilingToRange(PolylineMaximumX(points), ClipX, ClipX + ClipWidth);
        int bottom = CeilingToRange(PolylineMaximumY(points), ClipY, ClipY + ClipHeight);

        int y = top;
        while (y < bottom)
        {
            int x = left;
            while (x < right)
            {
                if (PolygonContainsPoint(points, x + 0.5, y + 0.5, fillRule))
                {
                    DrawPixel(x, y, color, blendMode);
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void StrokePolygon(ChartPoint[] points, double strokeWidth, ChartColor color, ChartLineJoin lineJoin, ChartBlendMode blendMode)
    {
        ValidatePolygon(points, color);
        ChartPoint[] closed = ClosePoints(points);
        DrawPolylineWithStyle(closed, strokeWidth, color, ChartLineCap.Butt, lineJoin, new double[0], 4.0, blendMode);
    }

    internal void StrokePolygonWithStyle(ChartPoint[] points, double strokeWidth, ChartColor color, ChartLineJoin lineJoin, double[] dashPattern, double miterLimit, ChartBlendMode blendMode)
    {
        ValidatePolygon(points, color);
        ChartPoint[] closed = ClosePoints(points);
        DrawPolylineWithStyle(closed, strokeWidth, color, ChartLineCap.Butt, lineJoin, dashPattern, miterLimit, blendMode);
    }

    internal void FillCircle(double centerX, double centerY, double radius, ChartColor color, ChartBlendMode blendMode)
    {
        FillEllipse(centerX, centerY, radius, radius, color, blendMode);
    }

    internal void StrokeCircle(double centerX, double centerY, double radius, double strokeWidth, ChartColor color, ChartBlendMode blendMode)
    {
        StrokeEllipse(centerX, centerY, radius, radius, strokeWidth, color, blendMode);
    }

    internal void FillEllipse(double centerX, double centerY, double radiusX, double radiusY, ChartColor color, ChartBlendMode blendMode)
    {
        ValidateEllipse(radiusX, radiusY, color);
        int left = FloorToRange(centerX - radiusX, ClipX, ClipX + ClipWidth);
        int top = FloorToRange(centerY - radiusY, ClipY, ClipY + ClipHeight);
        int right = CeilingToRange(centerX + radiusX, ClipX, ClipX + ClipWidth);
        int bottom = CeilingToRange(centerY + radiusY, ClipY, ClipY + ClipHeight);

        int y = top;
        while (y < bottom)
        {
            int x = left;
            while (x < right)
            {
                if (EllipseValue(x + 0.5, y + 0.5, centerX, centerY, radiusX, radiusY) <= 1.0)
                {
                    DrawPixel(x, y, color, blendMode);
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void StrokeEllipse(double centerX, double centerY, double radiusX, double radiusY, double strokeWidth, ChartColor color, ChartBlendMode blendMode)
    {
        ValidateEllipse(radiusX, radiusY, color);
        ValidateStrokeWidth(strokeWidth);

        double pad = strokeWidth * 0.5 + 1.0;
        int left = FloorToRange(centerX - radiusX - pad, ClipX, ClipX + ClipWidth);
        int top = FloorToRange(centerY - radiusY - pad, ClipY, ClipY + ClipHeight);
        int right = CeilingToRange(centerX + radiusX + pad, ClipX, ClipX + ClipWidth);
        int bottom = CeilingToRange(centerY + radiusY + pad, ClipY, ClipY + ClipHeight);

        int y = top;
        while (y < bottom)
        {
            int x = left;
            while (x < right)
            {
                if (EllipseStrokeContainsPoint(x + 0.5, y + 0.5, centerX, centerY, radiusX, radiusY, strokeWidth))
                {
                    DrawPixel(x, y, color, blendMode);
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void StrokeEllipseWithStyle(double centerX, double centerY, double radiusX, double radiusY, double strokeWidth, ChartColor color, ChartLineJoin lineJoin, double[] dashPattern, double miterLimit, ChartBlendMode blendMode)
    {
        ValidateEllipse(radiusX, radiusY, color);
        ChartPoint[] points = CreateEllipsePoints(centerX, centerY, radiusX, radiusY);
        DrawPolylineWithStyle(points, strokeWidth, color, ChartLineCap.Butt, lineJoin, dashPattern, miterLimit, blendMode);
    }

    internal static ChartPoint[] CreateEllipsePoints(double centerX, double centerY, double radiusX, double radiusY)
    {
        int segments = 96;
        ChartPoint[] points = new ChartPoint[segments + 1];
        int index = 0;
        while (index <= segments)
        {
            double angle = TwoPi * index / segments;
            points[index] = new ChartPoint(centerX + System.Math.Cos(angle) * radiusX, centerY + System.Math.Sin(angle) * radiusY);
            index = index + 1;
        }

        return points;
    }

    internal void StrokeArc(double centerX, double centerY, double radiusX, double radiusY, double startAngle, double endAngle, double strokeWidth, ChartColor color, ChartBlendMode blendMode)
    {
        ValidateEllipse(radiusX, radiusY, color);
        ValidateStrokeWidth(strokeWidth);

        double pad = strokeWidth * 0.5 + 1.0;
        int left = FloorToRange(centerX - radiusX - pad, ClipX, ClipX + ClipWidth);
        int top = FloorToRange(centerY - radiusY - pad, ClipY, ClipY + ClipHeight);
        int right = CeilingToRange(centerX + radiusX + pad, ClipX, ClipX + ClipWidth);
        int bottom = CeilingToRange(centerY + radiusY + pad, ClipY, ClipY + ClipHeight);

        int y = top;
        while (y < bottom)
        {
            int x = left;
            while (x < right)
            {
                double px = x + 0.5;
                double py = y + 0.5;
                double angle = System.Math.Atan2(py - centerY, px - centerX);
                if (AngleInSweep(angle, startAngle, endAngle) && EllipseStrokeContainsPoint(px, py, centerX, centerY, radiusX, radiusY, strokeWidth))
                {
                    DrawPixel(x, y, color, blendMode);
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void FillWedge(double centerX, double centerY, double radiusX, double radiusY, double startAngle, double endAngle, ChartColor color, ChartBlendMode blendMode)
    {
        ValidateEllipse(radiusX, radiusY, color);
        int left = FloorToRange(centerX - radiusX, ClipX, ClipX + ClipWidth);
        int top = FloorToRange(centerY - radiusY, ClipY, ClipY + ClipHeight);
        int right = CeilingToRange(centerX + radiusX, ClipX, ClipX + ClipWidth);
        int bottom = CeilingToRange(centerY + radiusY, ClipY, ClipY + ClipHeight);

        int y = top;
        while (y < bottom)
        {
            int x = left;
            while (x < right)
            {
                double px = x + 0.5;
                double py = y + 0.5;
                double angle = System.Math.Atan2(py - centerY, px - centerX);
                if (EllipseValue(px, py, centerX, centerY, radiusX, radiusY) <= 1.0 && AngleInSweep(angle, startAngle, endAngle))
                {
                    DrawPixel(x, y, color, blendMode);
                }

                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void StrokeWedge(double centerX, double centerY, double radiusX, double radiusY, double startAngle, double endAngle, double strokeWidth, ChartColor color, ChartBlendMode blendMode)
    {
        ValidateEllipse(radiusX, radiusY, color);
        ValidateStrokeWidth(strokeWidth);

        StrokeArc(centerX, centerY, radiusX, radiusY, startAngle, endAngle, strokeWidth, color, blendMode);

        ChartPoint center = new ChartPoint(centerX, centerY);
        ChartPoint start = EllipsePointAtAngle(centerX, centerY, radiusX, radiusY, startAngle);
        ChartPoint end = EllipsePointAtAngle(centerX, centerY, radiusX, radiusY, endAngle);
        DrawPolyline(new ChartPoint[] { center, start }, strokeWidth, color, ChartLineCap.Butt, ChartLineJoin.Bevel, blendMode);
        DrawPolyline(new ChartPoint[] { center, end }, strokeWidth, color, ChartLineCap.Butt, ChartLineJoin.Bevel, blendMode);
    }

    internal static void ValidatePolygon(ChartPoint[] points, ChartColor color)
    {
        if (points == null)
        {
            throw new System.ArgumentNullException("points");
        }

        if (points.Length < 3)
        {
            throw new System.ArgumentException("A polygon requires at least three points.");
        }

        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        int index = 0;
        while (index < points.Length)
        {
            if (points[index] == null)
            {
                throw new System.ArgumentException("Polygon points cannot contain null values.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateEllipse(double radiusX, double radiusY, ChartColor color)
    {
        if (radiusX <= 0.0 || double.IsNaN(radiusX) || double.IsInfinity(radiusX))
        {
            throw new System.ArgumentOutOfRangeException("radiusX");
        }

        if (radiusY <= 0.0 || double.IsNaN(radiusY) || double.IsInfinity(radiusY))
        {
            throw new System.ArgumentOutOfRangeException("radiusY");
        }

        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }
    }

    internal static void ValidateStrokeWidth(double strokeWidth)
    {
        if (strokeWidth <= 0.0 || double.IsNaN(strokeWidth) || double.IsInfinity(strokeWidth))
        {
            throw new System.ArgumentOutOfRangeException("strokeWidth");
        }
    }

    internal static ChartPoint[] ClosePoints(ChartPoint[] points)
    {
        ChartPoint[] closed = new ChartPoint[points.Length + 1];
        int index = 0;
        while (index < points.Length)
        {
            closed[index] = points[index].Copy();
            index = index + 1;
        }

        closed[points.Length] = points[0].Copy();
        return closed;
    }

    internal static bool PolygonContainsPoint(ChartPoint[] points, double px, double py, ChartFillRule fillRule)
    {
        if (fillRule == ChartFillRule.Nonzero)
        {
            return PolygonContainsPointNonzero(points, px, py);
        }

        return PolygonContainsPointEvenOdd(points, px, py);
    }

    internal static bool PolygonContainsPointEvenOdd(ChartPoint[] points, double px, double py)
    {
        bool inside = false;
        int index = 0;
        int previousIndex = points.Length - 1;

        while (index < points.Length)
        {
            ChartPoint start = points[previousIndex];
            ChartPoint end = points[index];
            bool crosses = (start.Y > py && end.Y <= py) || (end.Y > py && start.Y <= py);

            if (crosses)
            {
                double xAtY = start.X + (py - start.Y) * (end.X - start.X) / (end.Y - start.Y);
                if (px < xAtY)
                {
                    inside = !inside;
                }
            }

            previousIndex = index;
            index = index + 1;
        }

        return inside;
    }

    internal static bool PolygonContainsPointNonzero(ChartPoint[] points, double px, double py)
    {
        int winding = 0;
        int index = 0;
        while (index < points.Length)
        {
            ChartPoint start = points[index];
            ChartPoint end = points[(index + 1) % points.Length];

            if (start.Y <= py)
            {
                if (end.Y > py && IsLeft(start, end, px, py) > 0.0)
                {
                    winding = winding + 1;
                }
            }
            else
            {
                if (end.Y <= py && IsLeft(start, end, px, py) < 0.0)
                {
                    winding = winding - 1;
                }
            }

            index = index + 1;
        }

        return winding != 0;
    }

    internal static double IsLeft(ChartPoint start, ChartPoint end, double px, double py)
    {
        return (end.X - start.X) * (py - start.Y) - (px - start.X) * (end.Y - start.Y);
    }

    internal static double EllipseValue(double px, double py, double centerX, double centerY, double radiusX, double radiusY)
    {
        double nx = (px - centerX) / radiusX;
        double ny = (py - centerY) / radiusY;
        return nx * nx + ny * ny;
    }

    internal static bool EllipseStrokeContainsPoint(double px, double py, double centerX, double centerY, double radiusX, double radiusY, double strokeWidth)
    {
        double nx = (px - centerX) / radiusX;
        double ny = (py - centerY) / radiusY;
        double normalizedRadius = System.Math.Sqrt(nx * nx + ny * ny);
        double scale = System.Math.Min(radiusX, radiusY);
        double distance = System.Math.Abs(normalizedRadius - 1.0) * scale;
        return distance <= strokeWidth * 0.5;
    }

    internal static ChartPoint EllipsePointAtAngle(double centerX, double centerY, double radiusX, double radiusY, double angle)
    {
        return new ChartPoint(centerX + System.Math.Cos(angle) * radiusX, centerY + System.Math.Sin(angle) * radiusY);
    }

    internal static double NormalizeAngle(double angle)
    {
        double result = angle % TwoPi;
        if (result < 0.0)
        {
            result = result + TwoPi;
        }

        return result;
    }

    internal static bool AngleInSweep(double angle, double startAngle, double endAngle)
    {
        double sweep = endAngle - startAngle;
        while (sweep < 0.0)
        {
            sweep = sweep + TwoPi;
        }

        if (sweep >= TwoPi)
        {
            return true;
        }

        double normalized = NormalizeAngle(angle - startAngle);
        return normalized <= sweep + 0.0000001;
    }

    internal void FillPath(ChartPath path, ChartColor color, ChartFillRule fillRule, ChartBlendMode blendMode)
    {
        FillPathWithTolerance(path, color, fillRule, 0.25, blendMode);
    }

    internal void FillPathWithTolerance(ChartPath path, ChartColor color, ChartFillRule fillRule, double tolerance, ChartBlendMode blendMode)
    {
        ValidatePathRenderInputs(path, color, tolerance);

        ChartPathPolyline[] polylines = path.Flatten(tolerance);
        int index = 0;
        while (index < polylines.Length)
        {
            if (polylines[index].Closed && polylines[index].Points.Length >= 3)
            {
                FillPolygon(polylines[index].Points, color, fillRule, blendMode);
            }

            index = index + 1;
        }
    }

    internal void StrokePath(ChartPath path, double strokeWidth, ChartColor color, ChartLineCap lineCap, ChartLineJoin lineJoin, ChartBlendMode blendMode)
    {
        StrokePathWithTolerance(path, strokeWidth, color, lineCap, lineJoin, 0.25, blendMode);
    }

    internal void StrokePathWithTolerance(ChartPath path, double strokeWidth, ChartColor color, ChartLineCap lineCap, ChartLineJoin lineJoin, double tolerance, ChartBlendMode blendMode)
    {
        ValidatePathRenderInputs(path, color, tolerance);
        ValidateStrokeWidth(strokeWidth);

        ChartPathPolyline[] polylines = path.Flatten(tolerance);
        int index = 0;
        while (index < polylines.Length)
        {
            if (polylines[index].Points.Length >= 2)
            {
                if (polylines[index].Closed)
                {
                    DrawPolyline(polylines[index].Points, strokeWidth, color, ChartLineCap.Butt, lineJoin, blendMode);
                }
                else
                {
                    DrawPolyline(polylines[index].Points, strokeWidth, color, lineCap, lineJoin, blendMode);
                }
            }

            index = index + 1;
        }
    }

    internal void StrokePathWithStyle(ChartPath path, double strokeWidth, ChartColor color, ChartLineCap lineCap, ChartLineJoin lineJoin, double[] dashPattern, double miterLimit, double tolerance, ChartBlendMode blendMode)
    {
        ValidatePathRenderInputs(path, color, tolerance);
        ValidateStrokeWidth(strokeWidth);
        ValidateDashPattern(dashPattern);
        ChartRenderStyle.ValidatePositive(miterLimit, "miterLimit");
        ChartPathPolyline[] polylines = path.Flatten(tolerance);
        int index = 0;
        while (index < polylines.Length)
        {
            if (polylines[index].Points.Length >= 2)
            {
                ChartLineCap cap = lineCap;
                if (polylines[index].Closed)
                {
                    cap = ChartLineCap.Butt;
                }

                DrawPolylineWithStyle(polylines[index].Points, strokeWidth, color, cap, lineJoin, dashPattern, miterLimit, blendMode);
            }

            index = index + 1;
        }
    }

    internal static void ValidatePathRenderInputs(ChartPath path, ChartColor color, double tolerance)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        if (tolerance <= 0.0 || double.IsNaN(tolerance) || double.IsInfinity(tolerance))
        {
            throw new System.ArgumentOutOfRangeException("tolerance");
        }
    }

    internal void DrawMarker(double centerX, double centerY, double size, ChartMarkerShape shape, ChartColor color, ChartBlendMode blendMode)
    {
        ValidateMarker(size, color);

        double half = size * 0.5;
        if (shape == ChartMarkerShape.Circle)
        {
            FillCircle(centerX, centerY, half, color, blendMode);
            return;
        }

        if (shape == ChartMarkerShape.Square)
        {
            FillAxisAlignedRectangle(centerX - half, centerY - half, centerX + half, centerY + half, color, blendMode);
            return;
        }

        if (shape == ChartMarkerShape.Diamond)
        {
            FillPolygon(new ChartPoint[]
            {
                new ChartPoint(centerX, centerY - half),
                new ChartPoint(centerX + half, centerY),
                new ChartPoint(centerX, centerY + half),
                new ChartPoint(centerX - half, centerY)
            }, color, ChartFillRule.Nonzero, blendMode);
            return;
        }

        if (shape == ChartMarkerShape.Triangle)
        {
            FillPolygon(new ChartPoint[]
            {
                new ChartPoint(centerX, centerY - half),
                new ChartPoint(centerX + half, centerY + half),
                new ChartPoint(centerX - half, centerY + half)
            }, color, ChartFillRule.Nonzero, blendMode);
            return;
        }

        if (shape == ChartMarkerShape.Cross)
        {
            double width = MarkerStrokeWidth(size);
            DrawPolyline(new ChartPoint[] { new ChartPoint(centerX - half, centerY - half), new ChartPoint(centerX + half, centerY + half) }, width, color, ChartLineCap.Round, ChartLineJoin.Round, blendMode);
            DrawPolyline(new ChartPoint[] { new ChartPoint(centerX - half, centerY + half), new ChartPoint(centerX + half, centerY - half) }, width, color, ChartLineCap.Round, ChartLineJoin.Round, blendMode);
            return;
        }

        if (shape == ChartMarkerShape.Plus)
        {
            double width = MarkerStrokeWidth(size);
            DrawPolyline(new ChartPoint[] { new ChartPoint(centerX - half, centerY), new ChartPoint(centerX + half, centerY) }, width, color, ChartLineCap.Round, ChartLineJoin.Round, blendMode);
            DrawPolyline(new ChartPoint[] { new ChartPoint(centerX, centerY - half), new ChartPoint(centerX, centerY + half) }, width, color, ChartLineCap.Round, ChartLineJoin.Round, blendMode);
            return;
        }

        if (shape == ChartMarkerShape.Star)
        {
            FillPolygon(CreateStarPoints(centerX, centerY, half, half * 0.45), color, ChartFillRule.Nonzero, blendMode);
            return;
        }

        if (shape == ChartMarkerShape.Line)
        {
            DrawPolyline(new ChartPoint[] { new ChartPoint(centerX - half, centerY), new ChartPoint(centerX + half, centerY) }, MarkerStrokeWidth(size), color, ChartLineCap.Round, ChartLineJoin.Round, blendMode);
            return;
        }

        throw new System.ArgumentException("CustomPath marker requires a marker path.");
    }

    internal void DrawCustomMarker(double centerX, double centerY, double size, ChartPath markerPath, ChartColor color, ChartFillRule fillRule, ChartBlendMode blendMode)
    {
        ValidateMarker(size, color);
        if (markerPath == null)
        {
            throw new System.ArgumentNullException("markerPath");
        }

        ChartPath transformed = TransformMarkerPath(markerPath, centerX, centerY, size);
        FillPathWithTolerance(transformed, color, fillRule, 0.25, blendMode);
    }

    internal void StrokeCustomMarker(double centerX, double centerY, double size, ChartPath markerPath, double strokeWidth, ChartColor color, ChartLineCap lineCap, ChartLineJoin lineJoin, ChartBlendMode blendMode)
    {
        ValidateMarker(size, color);
        ValidateStrokeWidth(strokeWidth);
        if (markerPath == null)
        {
            throw new System.ArgumentNullException("markerPath");
        }

        ChartPath transformed = TransformMarkerPath(markerPath, centerX, centerY, size);
        StrokePathWithTolerance(transformed, strokeWidth, color, lineCap, lineJoin, 0.25, blendMode);
    }

    internal void StrokeCustomMarkerWithStyle(double centerX, double centerY, double size, ChartPath markerPath, double strokeWidth, ChartColor color, ChartLineCap lineCap, ChartLineJoin lineJoin, double[] dashPattern, double miterLimit, ChartBlendMode blendMode)
    {
        ValidateMarker(size, color);
        ValidateStrokeWidth(strokeWidth);
        if (markerPath == null)
        {
            throw new System.ArgumentNullException("markerPath");
        }

        ChartPath transformed = TransformMarkerPath(markerPath, centerX, centerY, size);
        StrokePathWithStyle(transformed, strokeWidth, color, lineCap, lineJoin, dashPattern, miterLimit, 0.25, blendMode);
    }

    internal static void ValidateMarker(double size, ChartColor color)
    {
        if (size <= 0.0 || double.IsNaN(size) || double.IsInfinity(size))
        {
            throw new System.ArgumentOutOfRangeException("size");
        }

        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }
    }

    internal static double MarkerStrokeWidth(double size)
    {
        return System.Math.Max(1.0, size * 0.2);
    }

    internal static ChartPoint[] CreateStarPoints(double centerX, double centerY, double outerRadius, double innerRadius)
    {
        ChartPoint[] points = new ChartPoint[10];
        int index = 0;
        while (index < 10)
        {
            double radius = outerRadius;
            if (index % 2 == 1)
            {
                radius = innerRadius;
            }

            double angle = 0.0 - System.Math.PI * 0.5 + index * System.Math.PI / 5.0;
            points[index] = new ChartPoint(centerX + System.Math.Cos(angle) * radius, centerY + System.Math.Sin(angle) * radius);
            index = index + 1;
        }

        return points;
    }

    internal static ChartPath TransformMarkerPath(ChartPath markerPath, double centerX, double centerY, double size)
    {
        if (markerPath == null)
        {
            throw new System.ArgumentNullException("markerPath");
        }

        ChartPath transformed = new ChartPath();
        int index = 0;
        while (index < markerPath.Commands.Count)
        {
            ChartPathCommand command = markerPath.Commands[index];
            if (command.Kind == ChartPathCommandKind.MoveTo)
            {
                transformed.MoveTo(TransformMarkerX(command.X1, centerX, size), TransformMarkerY(command.Y1, centerY, size));
            }
            else if (command.Kind == ChartPathCommandKind.LineTo)
            {
                transformed.LineTo(TransformMarkerX(command.X1, centerX, size), TransformMarkerY(command.Y1, centerY, size));
            }
            else if (command.Kind == ChartPathCommandKind.QuadraticBezierTo)
            {
                transformed.QuadraticBezierTo(
                    TransformMarkerX(command.X1, centerX, size),
                    TransformMarkerY(command.Y1, centerY, size),
                    TransformMarkerX(command.X2, centerX, size),
                    TransformMarkerY(command.Y2, centerY, size));
            }
            else if (command.Kind == ChartPathCommandKind.CubicBezierTo)
            {
                transformed.CubicBezierTo(
                    TransformMarkerX(command.X1, centerX, size),
                    TransformMarkerY(command.Y1, centerY, size),
                    TransformMarkerX(command.X2, centerX, size),
                    TransformMarkerY(command.Y2, centerY, size),
                    TransformMarkerX(command.X3, centerX, size),
                    TransformMarkerY(command.Y3, centerY, size));
            }
            else if (command.Kind == ChartPathCommandKind.Close)
            {
                transformed.Close();
            }

            index = index + 1;
        }

        return transformed;
    }

    internal static double TransformMarkerX(double x, double centerX, double size)
    {
        return centerX + x * size;
    }

    internal static double TransformMarkerY(double y, double centerY, double size)
    {
        return centerY + y * size;
    }

    internal void DrawText(string text, int x, int y, ChartBitmapFont font, int scale, ChartColor color, ChartBlendMode blendMode)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        if (font == null)
        {
            throw new System.ArgumentNullException("font");
        }

        ChartBitmapFont.ValidateScale(scale);
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        double originX = x;
        double cursorX = x;
        double cursorY = y;
        int index = 0;
        while (index < text.Length)
        {
            char c = text[index];
            if (c == '\n')
            {
                cursorX = originX;
                cursorY = cursorY + (font.GlyphHeight + font.Spacing) * (double)scale;
            }
            else
            {
                DrawGlyph(c, cursorX, cursorY, font, scale, color, blendMode);
                cursorX = cursorX + (font.GlyphWidth + font.Spacing) * (double)scale;
            }

            index = index + 1;
        }
    }

    internal void DrawGlyph(char value, double x, double y, ChartBitmapFont font, int scale, ChartColor color, ChartBlendMode blendMode)
    {
        if (font == null)
        {
            throw new System.ArgumentNullException("font");
        }

        ChartBitmapFont.ValidateScale(scale);
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ChartBitmapGlyph glyph = font.GetGlyph(value);
        int row = 0;
        while (row < glyph.Height)
        {
            int column = 0;
            while (column < glyph.Width)
            {
                if (glyph.IsPixelSet(column, row))
                {
                    double left = x + column * (double)scale;
                    double top = y + row * (double)scale;
                    FillAxisAlignedRectangle(left, top, left + scale, top + scale, color, blendMode);
                }

                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal void DrawTextLayout(ChartTextLayout layout, ChartBitmapFont font, int scale, ChartColor color, ChartBlendMode blendMode)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        if (font == null)
        {
            throw new System.ArgumentNullException("font");
        }

        ChartBitmapFont.ValidateScale(scale);
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        int index = 0;
        while (index < layout.Lines.Length)
        {
            DrawText(layout.Lines[index].Text, layout.Lines[index].X, layout.Lines[index].Y, font, scale, color, blendMode);
            index = index + 1;
        }
    }

    internal void DrawRotatedTextLayout(ChartTextLayout layout, ChartBitmapFont font, int scale, ChartColor color, ChartBlendMode blendMode, double angleRadians, double pivotX, double pivotY)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        if (font == null)
        {
            throw new System.ArgumentNullException("font");
        }

        ChartBitmapFont.ValidateScale(scale);
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ChartLinearScale.ValidateFinite(angleRadians, "angleRadians");
        int lineIndex = 0;
        while (lineIndex < layout.Lines.Length)
        {
            ChartTextLayoutLine line = layout.Lines[lineIndex];
            int characterIndex = 0;
            while (characterIndex < line.Text.Length)
            {
                ChartBitmapGlyph glyph = font.GetGlyph(line.Text[characterIndex]);
                int row = 0;
                while (row < glyph.Height)
                {
                    int column = 0;
                    while (column < glyph.Width)
                    {
                        if (glyph.IsPixelSet(column, row))
                        {
                            int scaledY = 0;
                            while (scaledY < scale)
                            {
                                int scaledX = 0;
                                while (scaledX < scale)
                                {
                                    double sourceX = line.X + characterIndex * font.GlyphAdvance(scale) + column * scale + scaledX;
                                    double sourceY = line.Y + row * scale + scaledY;
                                    ChartPoint rotated = ChartBitmapFont.RotatePoint(sourceX, sourceY, pivotX, pivotY, angleRadians);
                                    DrawPixel(ChartRenderScene.RoundToInt(rotated.X), ChartRenderScene.RoundToInt(rotated.Y), color, blendMode);
                                    scaledX = scaledX + 1;
                                }

                                scaledY = scaledY + 1;
                            }
                        }

                        column = column + 1;
                    }

                    row = row + 1;
                }

                characterIndex = characterIndex + 1;
            }

            lineIndex = lineIndex + 1;
        }
    }

    internal ChartTextLayout DrawAlignedText(
        string text,
        int anchorX,
        int anchorY,
        ChartBitmapFont font,
        int scale,
        ChartTextAlignment textAlignment,
        ChartVerticalTextAlignment verticalAlignment,
        ChartColor color,
        ChartBlendMode blendMode)
    {
        if (font == null)
        {
            throw new System.ArgumentNullException("font");
        }

        ChartTextLayout layout = font.CreateTextLayout(text, anchorX, anchorY, scale, textAlignment, verticalAlignment);
        DrawTextLayout(layout, font, scale, color, blendMode);
        return layout;
    }

    internal static ChartColor SourceOver(ChartColor source, ChartColor destination)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (source.A == 0)
        {
            return destination.Copy();
        }

        if (source.A == 255)
        {
            return source.Copy();
        }

        double sourceAlpha = source.A / 255.0;
        double destinationAlpha = destination.A / 255.0;
        double inverseSourceAlpha = 1.0 - sourceAlpha;
        double outputAlpha = sourceAlpha + destinationAlpha * inverseSourceAlpha;

        if (outputAlpha <= 0.0)
        {
            return ChartColor.Transparent();
        }

        double r = (source.R * sourceAlpha + destination.R * destinationAlpha * inverseSourceAlpha) / outputAlpha;
        double g = (source.G * sourceAlpha + destination.G * destinationAlpha * inverseSourceAlpha) / outputAlpha;
        double b = (source.B * sourceAlpha + destination.B * destinationAlpha * inverseSourceAlpha) / outputAlpha;
        byte a = ChartColor.RoundToByte(outputAlpha * 255.0);

        return new ChartColor(ChartColor.RoundToByte(r), ChartColor.RoundToByte(g), ChartColor.RoundToByte(b), a);
    }

    internal static ChartColor Composite(ChartColor source, ChartColor destination, ChartBlendMode blendMode)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (blendMode == ChartBlendMode.Normal || blendMode == ChartBlendMode.SourceOver)
        {
            return SourceOver(source, destination);
        }

        if (blendMode == ChartBlendMode.Source)
        {
            return source.Copy();
        }

        double sourceAlpha = source.A / 255.0;
        double destinationAlpha = destination.A / 255.0;
        double outputAlpha = sourceAlpha + destinationAlpha - sourceAlpha * destinationAlpha;
        if (outputAlpha <= 0.0)
        {
            return ChartColor.Transparent();
        }

        double red = CompositeChannel(source.R / 255.0, destination.R / 255.0, sourceAlpha, destinationAlpha, outputAlpha, blendMode);
        double green = CompositeChannel(source.G / 255.0, destination.G / 255.0, sourceAlpha, destinationAlpha, outputAlpha, blendMode);
        double blue = CompositeChannel(source.B / 255.0, destination.B / 255.0, sourceAlpha, destinationAlpha, outputAlpha, blendMode);
        return new ChartColor(ChartColor.RoundToByte(red * 255.0), ChartColor.RoundToByte(green * 255.0), ChartColor.RoundToByte(blue * 255.0), ChartColor.RoundToByte(outputAlpha * 255.0));
    }

    internal static double CompositeChannel(double source, double destination, double sourceAlpha, double destinationAlpha, double outputAlpha, ChartBlendMode blendMode)
    {
        double blended = source;
        if (blendMode == ChartBlendMode.Multiply)
        {
            blended = source * destination;
        }
        else if (blendMode == ChartBlendMode.Screen)
        {
            blended = source + destination - source * destination;
        }
        else if (blendMode == ChartBlendMode.Darken)
        {
            blended = System.Math.Min(source, destination);
        }
        else if (blendMode == ChartBlendMode.Lighten)
        {
            blended = System.Math.Max(source, destination);
        }

        double premultiplied = sourceAlpha * (1.0 - destinationAlpha) * source + sourceAlpha * destinationAlpha * blended + (1.0 - sourceAlpha) * destinationAlpha * destination;
        return premultiplied / outputAlpha;
    }

    internal static int RoundToInt(double value)
    {
        double rounded = System.Math.Floor(value + 0.5);
        if (double.IsNaN(rounded) || rounded < int.MinValue || rounded > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        return (int)rounded;
    }

    internal static double RoundDouble(double value)
    {
        return System.Math.Floor(value + 0.5);
    }

    internal static double FractionalPart(double value)
    {
        return value - System.Math.Floor(value);
    }

    internal static double ReverseFractionalPart(double value)
    {
        return 1.0 - FractionalPart(value);
    }

    internal static double ClampDouble(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }

    internal static int FloorToRange(double value, int minimum, int maximum)
    {
        if (double.IsNaN(value))
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        if (value <= minimum)
        {
            return minimum;
        }

        if (value >= maximum)
        {
            return maximum;
        }

        return (int)System.Math.Floor(value);
    }

    internal static int CeilingToRange(double value, int minimum, int maximum)
    {
        if (double.IsNaN(value))
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        if (value <= minimum)
        {
            return minimum;
        }

        if (value >= maximum)
        {
            return maximum;
        }

        return (int)System.Math.Ceiling(value);
    }

    internal static double DistanceSquared(double x0, double y0, double x1, double y1)
    {
        double dx = x1 - x0;
        double dy = y1 - y0;
        return dx * dx + dy * dy;
    }
}
