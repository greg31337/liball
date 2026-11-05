namespace svg;

internal class SvgFilterRenderState
{
    SvgFilterGraphPlan plan;
    SvgFilterRenderOptions options;
    SvgFilterRenderResult result;
    SvgFilterRenderError error;
    byte[] first;
    byte[] second;
    byte[] temporary;

    internal SvgFilterRenderState(SvgFilterGraphPlan graphPlan, SvgFilterRenderOptions renderOptions)
    {
        plan = graphPlan;
        options = renderOptions;
        result = new SvgFilterRenderResult();
        result.Plan = graphPlan;
        error = SvgFilterRenderError.None;
        first = new byte[4];
        second = new byte[4];
        temporary = new byte[4];
    }

    internal SvgFilterRenderResult Run()
    {
        if (!Validate()) return Failure();
        int index = 0;
        while (index < plan.Steps.Count && error == SvgFilterRenderError.None)
        {
            EvaluateStep(plan.Steps[index]);
            if (error == SvgFilterRenderError.None) result.PrimitivesEvaluated = result.PrimitivesEvaluated + 1;
            index = index + 1;
        }
        if (error != SvgFilterRenderError.None) return Failure();
        if (!CreateOutput()) return Failure();
        result.Success = true;
        result.Error = SvgFilterRenderError.None;
        result.Bounds = CopyRect(plan.FinalBounds);
        return result;
    }

    bool Validate()
    {
        if (plan == null || options == null || options.GraphOptions == null || !plan.Success || options.MaximumPixels < 1 || options.MaximumTableValues < 1 || plan.Steps == null || plan.Surfaces == null || plan.FinalBounds == null || plan.PrimitiveTransform == null) return SetError(SvgFilterRenderError.InvalidArgument);
        int index = 0;
        while (index < plan.Surfaces.Count)
        {
            SvgFilterSurface surface = plan.Surfaces[index];
            if (surface == null || surface.Index != index || surface.Buffer == null || surface.Buffer.Width != surface.Width || surface.Buffer.Height != surface.Height || surface.Buffer.ColorSpace != surface.ColorSpace) return SetError(SvgFilterRenderError.InvalidArgument);
            index = index + 1;
        }
        index = 0;
        while (index < plan.Steps.Count)
        {
            SvgFilterGraphStep step = plan.Steps[index];
            if (step == null || step.Index != index || step.Primitive == null || step.Bounds == null || step.Inputs == null || !Supported(step.Primitive.ElementKind)) return SetError(step != null && step.Primitive != null ? SvgFilterRenderError.UnsupportedPrimitive : SvgFilterRenderError.InvalidArgument);
            if (step.Primitive.Attributes == null || step.Primitive.Children == null) return SetError(SvgFilterRenderError.InvalidArgument);
            if (!ValidInputs(step)) return SetError(SvgFilterRenderError.InvalidArgument);
            if (!step.Bounds.IsEmpty)
            {
                if (step.OutputSurfaceIndex < 0 || step.OutputSurfaceIndex >= plan.Surfaces.Count) return SetError(SvgFilterRenderError.InvalidArgument);
                SvgFilterSurface output = plan.Surfaces[step.OutputSurfaceIndex];
                if (output.Width < step.Bounds.Width || output.Height < step.Bounds.Height || output.ColorSpace != step.ColorSpace) return SetError(SvgFilterRenderError.InvalidArgument);
            }
            index = index + 1;
        }
        return true;
    }

    bool ValidInputs(SvgFilterGraphStep step)
    {
        int count = step.Inputs.Count;
        if (step.Primitive.ElementKind == SvgElementKind.FeBlend || step.Primitive.ElementKind == SvgElementKind.FeComposite || step.Primitive.ElementKind == SvgElementKind.FeDisplacementMap) return count == 2;
        if (step.Primitive.ElementKind == SvgElementKind.FeFlood || step.Primitive.ElementKind == SvgElementKind.FeImage || step.Primitive.ElementKind == SvgElementKind.FeTurbulence) return count == 0;
        if (step.Primitive.ElementKind == SvgElementKind.FeColorMatrix || step.Primitive.ElementKind == SvgElementKind.FeComponentTransfer || step.Primitive.ElementKind == SvgElementKind.FeConvolveMatrix || step.Primitive.ElementKind == SvgElementKind.FeDiffuseLighting || step.Primitive.ElementKind == SvgElementKind.FeDropShadow || step.Primitive.ElementKind == SvgElementKind.FeGaussianBlur || step.Primitive.ElementKind == SvgElementKind.FeMorphology || step.Primitive.ElementKind == SvgElementKind.FeOffset || step.Primitive.ElementKind == SvgElementKind.FeSpecularLighting || step.Primitive.ElementKind == SvgElementKind.FeTile) return count == 1;
        return step.Primitive.ElementKind == SvgElementKind.FeMerge;
    }

    void EvaluateStep(SvgFilterGraphStep step)
    {
        if (step.Bounds.IsEmpty) return;
        SvgFilterSurface surface = plan.Surfaces[step.OutputSurfaceIndex];
        SvgPixelOperationResult cleared = (surface.Buffer ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Clear();
        if (!cleared.Success)
        {
            SetError(SvgFilterRenderError.BufferOperationFailed);
            return;
        }
        if (step.Primitive.ElementKind == SvgElementKind.FeBlend) Blend(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeComposite) Composite(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeMerge) Merge(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeFlood) Flood(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeOffset) Offset(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeTile) Tile(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeColorMatrix) ColorMatrix(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeComponentTransfer) ComponentTransfer(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeGaussianBlur) GaussianBlur(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeDropShadow) DropShadow(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeConvolveMatrix) ConvolveMatrix(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeMorphology) Morphology(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeDisplacementMap) DisplacementMap(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeImage) Image(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeTurbulence) Turbulence(step, surface.Buffer);
        else if (step.Primitive.ElementKind == SvgElementKind.FeDiffuseLighting || step.Primitive.ElementKind == SvgElementKind.FeSpecularLighting) Lighting(step, surface.Buffer);
        else SetError(SvgFilterRenderError.UnsupportedPrimitive);
    }

    void Blend(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        if (!TakeWork((long)step.Bounds.Width * step.Bounds.Height)) return;
        SvgBlendMode mode = SvgBlendModeMath.Parse(Text(step.Primitive, SvgAttributeNames.Mode, "normal"));
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                int globalX = step.Bounds.X + x;
                int globalY = step.Bounds.Y + y;
                Read(step.Inputs[0], globalX, globalY, step.ColorSpace, first);
                Read(step.Inputs[1], globalX, globalY, step.ColorSpace, second);
                if (error == SvgFilterRenderError.None)
                {
                    SvgBlendModeMath.Composite(first, 0, step.ColorSpace, second, 0, step.ColorSpace, SvgCompositeOperator.SourceOver, 1, mode, SvgBlendColorSpace.Auto, false);
                    Store(output, x, y, second);
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void Composite(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        if (!TakeWork((long)step.Bounds.Width * step.Bounds.Height)) return;
        string operationText = Text(step.Primitive, SvgAttributeNames.Operator, "over");
        bool arithmetic = operationText == "arithmetic";
        SvgCompositeOperator operation = CompositeOperator(operationText);
        double k1 = Number(step.Primitive, "k1", 0);
        double k2 = Number(step.Primitive, "k2", 0);
        double k3 = Number(step.Primitive, "k3", 0);
        double k4 = Number(step.Primitive, "k4", 0);
        if (!SvgFiniteMath.IsFinite(k1) || !SvgFiniteMath.IsFinite(k2) || !SvgFiniteMath.IsFinite(k3) || !SvgFiniteMath.IsFinite(k4))
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                int globalX = step.Bounds.X + x;
                int globalY = step.Bounds.Y + y;
                Read(step.Inputs[0], globalX, globalY, step.ColorSpace, first);
                Read(step.Inputs[1], globalX, globalY, step.ColorSpace, second);
                if (error == SvgFilterRenderError.None)
                {
                    if (arithmetic) Arithmetic(first, second, temporary, k1, k2, k3, k4);
                    else
                    {
                        CopyPixel(first, temporary);
                        SvgBlendModeMath.Composite(temporary, 0, step.ColorSpace, second, 0, step.ColorSpace, operation, 1, SvgBlendMode.Normal, SvgBlendColorSpace.Auto, false);
                        CopyPixel(second, temporary);
                    }
                    Store(output, x, y, temporary);
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void Merge(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        long multiplier = step.Inputs.Count == 0 ? 1 : step.Inputs.Count;
        if (!TakeWork((long)step.Bounds.Width * step.Bounds.Height * multiplier)) return;
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                ClearPixel(temporary);
                int inputIndex = 0;
                while (inputIndex < step.Inputs.Count && error == SvgFilterRenderError.None)
                {
                    Read(step.Inputs[inputIndex], step.Bounds.X + x, step.Bounds.Y + y, step.ColorSpace, first);
                    if (error == SvgFilterRenderError.None) SvgBlendModeMath.Composite(first, 0, step.ColorSpace, temporary, 0, step.ColorSpace, SvgCompositeOperator.SourceOver, 1, SvgBlendMode.Normal, SvgBlendColorSpace.Auto, false);
                    inputIndex = inputIndex + 1;
                }
                if (error == SvgFilterRenderError.None) Store(output, x, y, temporary);
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void Flood(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        if (!TakeWork((long)step.Bounds.Width * step.Bounds.Height)) return;
        SvgColor? color = step.Primitive.FloodColor;
        if (color == null) color = new SvgColor(SvgColorSpace.Srgb, 0, 0, 0, 1);
        double opacity = step.Primitive.FloodOpacity;
        if (!SvgFiniteMath.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        SvgPaintSampleOptions paintOptions = new SvgPaintSampleOptions();
        paintOptions.OutputColorSpace = step.ColorSpace;
        paintOptions.Opacity = opacity;
        SvgPaintSampleResult sampled = SvgPaintSampleMath.Pixel(color, paintOptions);
        if (!sampled.Success)
        {
            SetError(SvgFilterRenderError.ColorConversionFailed);
            return;
        }
        SvgPixelOperationResult cleared = output.ClearRectangle(new SvgPixelRect(0, 0, step.Bounds.Width, step.Bounds.Height), sampled.Pixel);
        if (!cleared.Success) SetError(SvgFilterRenderError.BufferOperationFailed);
    }

    void Offset(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        if (!TakeWork((long)step.Bounds.Width * step.Bounds.Height)) return;
        SvgVectorTransformResult transformed = plan.PrimitiveTransform.TransformVector(new SvgVector(step.Primitive.OffsetX, step.Primitive.OffsetY));
        if (!transformed.Success)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                double sampleX = step.Bounds.X + x - (transformed.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X;
                double sampleY = step.Bounds.Y + y - transformed.Vector.Y;
                Bilinear(step.Inputs[0], sampleX, sampleY, step.ColorSpace, temporary);
                if (error == SvgFilterRenderError.None) Store(output, x, y, temporary);
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void Tile(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        if (!TakeWork((long)step.Bounds.Width * step.Bounds.Height)) return;
        SvgFilterGraphInput input = step.Inputs[0];
        if (input.Bounds.IsEmpty) return;
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                int sourceX = input.Bounds.X + Mod(step.Bounds.X + x - input.Bounds.X, input.Bounds.Width);
                int sourceY = input.Bounds.Y + Mod(step.Bounds.Y + y - input.Bounds.Y, input.Bounds.Height);
                Read(input, sourceX, sourceY, step.ColorSpace, temporary);
                if (error == SvgFilterRenderError.None) Store(output, x, y, temporary);
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void ConvolveMatrix(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        int[] order = IntegerList(step.Primitive, SvgAttributeNames.Order);
        int width = order.Length == 0 ? 3 : order[0];
        int height = order.Length < 2 ? width : order[1];
        double[] kernel = NumberList(step.Primitive, SvgAttributeNames.KernelMatrix);
        if (order.Length > 2 || width < 1 || height < 1)
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        long kernelCount = (long)width * height;
        if (kernelCount > options.GraphOptions.FilterLimits.MaxKernelElements)
        {
            SetError(SvgFilterRenderError.WorkLimitExceeded);
            return;
        }
        if (kernel.Length != kernelCount || !ValidNumbers(kernel))
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        double sum = 0;
        int index = 0;
        while (index < kernel.Length)
        {
            sum = sum + kernel[index];
            index = index + 1;
        }
        double defaultDivisor = sum == 0 ? 1 : sum;
        double divisor = Number(step.Primitive, SvgAttributeNames.Divisor, defaultDivisor);
        if (divisor == 0) divisor = defaultDivisor;
        double bias = Number(step.Primitive, SvgAttributeNames.Bias, 0);
        int targetX = Integer(step.Primitive, SvgAttributeNames.TargetX, width / 2);
        int targetY = Integer(step.Primitive, SvgAttributeNames.TargetY, height / 2);
        string edgeMode = Text(step.Primitive, SvgAttributeNames.EdgeMode, "duplicate");
        bool preserveAlpha = Text(step.Primitive, SvgAttributeNames.PreserveAlpha, "false") == "true";
        if (!SvgFiniteMath.IsFinite(divisor) || !SvgFiniteMath.IsFinite(bias) || divisor == 0 || targetX < 0 || targetX >= width || targetY < 0 || targetY >= height || !ValidEdgeMode(edgeMode))
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        SvgConvolutionStep? sampleStep = ConvolutionStep(step.Primitive);
        if (sampleStep == null) return;
        if (!TakeBlurWork(step.Bounds.Width, step.Bounds.Height, kernel.Length)) return;
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                ConvolvePixel(step, step.Bounds.X + x, step.Bounds.Y + y, width, height, targetX, targetY, kernel, divisor, bias, edgeMode, preserveAlpha, sampleStep, temporary);
                if (error == SvgFilterRenderError.None) Store(output, x, y, temporary);
                x = x + 1;
            }
            y = y + 1;
        }
    }

    SvgConvolutionStep? ConvolutionStep(SvgSceneFilterPrimitiveData primitive)
    {
        double[] units = NumberList(primitive, SvgAttributeNames.KernelUnitLength);
        if (units.Length > 2 || !ValidNumbers(units)) return ConvolutionFailure(SvgFilterRenderError.InvalidArgument);
        SvgConvolutionStep result = new SvgConvolutionStep();
        if (units.Length == 0) return result;
        double x = units[0];
        double y = units.Length == 1 ? x : units[1];
        if (x <= 0 || y <= 0) return result;
        SvgVectorTransformResult horizontal = plan.PrimitiveTransform.TransformVector(new SvgVector(x, 0));
        SvgVectorTransformResult vertical = plan.PrimitiveTransform.TransformVector(new SvgVector(0, y));
        if (!horizontal.Success || !vertical.Success) return ConvolutionFailure(SvgFilterRenderError.NonFiniteValue);
        result.HorizontalX = (horizontal.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X;
        result.HorizontalY = horizontal.Vector.Y;
        result.VerticalX = (vertical.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X;
        result.VerticalY = vertical.Vector.Y;
        result.Interpolated = true;
        return result;
    }

    SvgConvolutionStep? ConvolutionFailure(SvgFilterRenderError value)
    {
        SetError(value);
        return null;
    }

    void ConvolvePixel(SvgFilterGraphStep step, int x, int y, int width, int height, int targetX, int targetY, double[] kernel, double divisor, double bias, string edgeMode, bool preserveAlpha, SvgConvolutionStep sampleStep, byte[] pixel)
    {
        double red = 0;
        double green = 0;
        double blue = 0;
        double alpha = 0;
        int row = 0;
        while (row < height && error == SvgFilterRenderError.None)
        {
            int column = 0;
            while (column < width && error == SvgFilterRenderError.None)
            {
                double sampleX = x + (column - targetX) * sampleStep.HorizontalX + (row - targetY) * sampleStep.VerticalX;
                double sampleY = y + (column - targetX) * sampleStep.HorizontalY + (row - targetY) * sampleStep.VerticalY;
                if (sampleStep.Interpolated) BilinearEdge(step.Inputs[0], sampleX, sampleY, step.ColorSpace, edgeMode, first);
                else ReadEdge(step.Inputs[0], x + column - targetX, y + row - targetY, step.ColorSpace, edgeMode, first);
                double weight = kernel[(height - row - 1) * width + width - column - 1];
                if (preserveAlpha)
                {
                    red = red + Straight(first[0], first[3]) * weight;
                    green = green + Straight(first[1], first[3]) * weight;
                    blue = blue + Straight(first[2], first[3]) * weight;
                }
                else
                {
                    red = red + first[0] * weight;
                    green = green + first[1] * weight;
                    blue = blue + first[2] * weight;
                    alpha = alpha + first[3] * weight;
                }
                column = column + 1;
            }
            row = row + 1;
        }
        Read(step.Inputs[0], x, y, step.ColorSpace, second);
        if (preserveAlpha)
        {
            pixel[3] = second[3];
            double scale = second[3] / 255.0;
            pixel[0] = Byte((red / divisor + bias * 255) * scale);
            pixel[1] = Byte((green / divisor + bias * 255) * scale);
            pixel[2] = Byte((blue / divisor + bias * 255) * scale);
        }
        else
        {
            pixel[3] = Byte(alpha / divisor + bias * 255);
            pixel[0] = Byte(red / divisor + bias * second[3]);
            pixel[1] = Byte(green / divisor + bias * second[3]);
            pixel[2] = Byte(blue / divisor + bias * second[3]);
        }
        ClampPremultiplied(pixel);
    }

    void Morphology(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        double[] radii = NumberList(step.Primitive, SvgAttributeNames.Radius);
        if (radii.Length == 0) radii = new double[1];
        if (radii.Length > 2 || !ValidNumbers(radii) || radii[0] < 0 || radii.Length == 2 && radii[1] < 0)
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        double radiusXValue = radii[0];
        double radiusYValue = radii.Length == 1 ? radiusXValue : radii[1];
        if (radiusXValue == 0 || radiusYValue == 0)
        {
            radiusXValue = 0;
            radiusYValue = 0;
        }
        SvgVectorTransformResult horizontal = plan.PrimitiveTransform.TransformVector(new SvgVector(radiusXValue, 0));
        SvgVectorTransformResult vertical = plan.PrimitiveTransform.TransformVector(new SvgVector(0, radiusYValue));
        if (!horizontal.Success || !vertical.Success)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        double transformedX = Hypot((horizontal.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, horizontal.Vector.Y);
        double transformedY = Hypot((vertical.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, vertical.Vector.Y);
        if (!SvgFiniteMath.IsFinite(transformedX) || !SvgFiniteMath.IsFinite(transformedY) || transformedX > int.MaxValue || transformedY > int.MaxValue)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        int radiusX = (int)System.Math.Floor(transformedX);
        int radiusY = (int)System.Math.Floor(transformedY);
        long kernelWidth = radiusX * 2L + 1;
        long kernelHeight = radiusY * 2L + 1;
        long kernelCount = kernelWidth > long.MaxValue / kernelHeight ? long.MaxValue : kernelWidth * kernelHeight;
        if (kernelCount > options.GraphOptions.FilterLimits.MaxKernelElements || kernelCount > int.MaxValue)
        {
            SetError(SvgFilterRenderError.WorkLimitExceeded);
            return;
        }
        string operation = Text(step.Primitive, SvgAttributeNames.Operator, "erode");
        if (operation != "erode" && operation != "dilate")
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        if (!TakeBlurWork(step.Bounds.Width, step.Bounds.Height, (int)kernelCount)) return;
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                MorphologyPixel(step, step.Bounds.X + x, step.Bounds.Y + y, radiusX, radiusY, operation == "erode", temporary);
                if (error == SvgFilterRenderError.None) Store(output, x, y, temporary);
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void MorphologyPixel(SvgFilterGraphStep step, int x, int y, int radiusX, int radiusY, bool erode, byte[] pixel)
    {
        int initial = erode ? 255 : 0;
        pixel[0] = (byte)initial;
        pixel[1] = (byte)initial;
        pixel[2] = (byte)initial;
        pixel[3] = (byte)initial;
        int offsetY = -radiusY;
        while (offsetY <= radiusY && error == SvgFilterRenderError.None)
        {
            int offsetX = -radiusX;
            while (offsetX <= radiusX && error == SvgFilterRenderError.None)
            {
                ReadEdge(step.Inputs[0], (long)x + offsetX, (long)y + offsetY, step.ColorSpace, "none", first);
                int channel = 0;
                while (channel < 4)
                {
                    if (erode && first[channel] < pixel[channel] || !erode && first[channel] > pixel[channel]) pixel[channel] = first[channel];
                    channel = channel + 1;
                }
                offsetX = offsetX + 1;
            }
            offsetY = offsetY + 1;
        }
        ClampPremultiplied(pixel);
    }

    void DisplacementMap(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        double scale = Number(step.Primitive, SvgAttributeNames.Scale, 0);
        string xChannel = Text(step.Primitive, SvgAttributeNames.XChannelSelector, "a");
        string yChannel = Text(step.Primitive, SvgAttributeNames.YChannelSelector, "a");
        if (!SvgFiniteMath.IsFinite(scale) || !ValidChannel(xChannel) || !ValidChannel(yChannel))
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        if (!TakeWork((long)step.Bounds.Width * step.Bounds.Height)) return;
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                int globalX = step.Bounds.X + x;
                int globalY = step.Bounds.Y + y;
                Read(step.Inputs[1], globalX, globalY, step.ColorSpace, first);
                double displacementX = scale * (Channel(first, xChannel) - 0.5);
                double displacementY = scale * (Channel(first, yChannel) - 0.5);
                SvgVectorTransformResult transformed = plan.PrimitiveTransform.TransformVector(new SvgVector(displacementX, displacementY));
                if (!transformed.Success) SetError(SvgFilterRenderError.NonFiniteValue);
                else
                {
                    Bilinear(step.Inputs[0], globalX + (transformed.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, globalY + transformed.Vector.Y, step.ColorSpace, temporary);
                    if (error == SvgFilterRenderError.None) Store(output, x, y, temporary);
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void Image(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        if (!TakeWork((long)step.Bounds.Width * step.Bounds.Height)) return;
        SvgSceneResource? resource = step.Primitive.ImageResource;
        if (resource == null) return;
        if (resource.Kind != SvgSceneResourceKind.Image || resource.Image == null || !resource.HasValidTag())
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        SvgPixelBuffer? source = resource.Image.DecodedPixels;
        if (source == null) return;
        SvgAspectRatio aspect = step.Primitive.ImageAspectRatio;
        if (!SvgImageSampleMath.ValidBuffer(source) || aspect == null || !SvgAspectRatioNames.IsAlignment(aspect.Alignment) || !SvgAspectRatioNames.IsScale(aspect.Scale))
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        SvgViewBoxFitResult fit = aspect.Fit(new SvgViewBox(0, 0, source.Width, source.Height), step.Bounds.X, step.Bounds.Y, step.Bounds.Width, step.Bounds.Height);
        if (!fit.Success)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        SvgMatrixOperationResult inverse = (fit.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Invert();
        if (!inverse.Success)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                SvgPointTransformResult mapped = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(new SvgPoint(step.Bounds.X + x + 0.5, step.Bounds.Y + y + 0.5));
                if (!mapped.Success) SetError(SvgFilterRenderError.NonFiniteValue);
                else
                {
                    SvgImageSampleResult sampled = SvgImageSampleMath.Source(source, (mapped.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, mapped.Point.Y, SvgImageSamplingMode.Bilinear, 1);
                    if (!sampled.Success) SetError(SvgFilterRenderError.BufferOperationFailed);
                    else if (sampled.HasPixel)
                    {
                        SvgPremultipliedRgba8 value = sampled.Pixel;
                        if (sampled.ColorSpace != step.ColorSpace) value = (Convert(value, sampled.ColorSpace, step.ColorSpace) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                        if (value == null) SetError(SvgFilterRenderError.ColorConversionFailed);
                        else
                        {
                            temporary[0] = value.Red;
                            temporary[1] = value.Green;
                            temporary[2] = value.Blue;
                            temporary[3] = value.Alpha;
                            Store(output, x, y, temporary);
                        }
                    }
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void Turbulence(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        double[] frequencies = NumberList(step.Primitive, SvgAttributeNames.BaseFrequency);
        double frequencyX = frequencies.Length == 0 ? 0 : frequencies[0];
        double frequencyY = frequencies.Length < 2 ? frequencyX : frequencies[1];
        int octaves = Integer(step.Primitive, SvgAttributeNames.NumOctaves, 1);
        double seed = Number(step.Primitive, SvgAttributeNames.Seed, 0);
        string type = Text(step.Primitive, SvgAttributeNames.Type, "turbulence");
        string stitchTiles = Text(step.Primitive, SvgAttributeNames.StitchTiles, "nostitch");
        bool fractalNoise = type == "fractalnoise";
        bool stitch = stitchTiles == "stitch";
        if (frequencies.Length > 2 || !SvgFiniteMath.IsFinite(frequencyX) || !SvgFiniteMath.IsFinite(frequencyY) || frequencyX < 0 || frequencyY < 0 || octaves < 1 || octaves > options.GraphOptions.FilterLimits.MaxTurbulenceOctaves || !SvgFiniteMath.IsFinite(seed) || type != "turbulence" && !fractalNoise || stitchTiles != "nostitch" && !stitch)
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        SvgRect? tile = step.Primitive.PrimitiveRegion;
        if (stitch && (tile == null || !SvgFiniteMath.IsFinite(tile.X) || !SvgFiniteMath.IsFinite(tile.Y) || !SvgFiniteMath.IsFinite(tile.Width) || !SvgFiniteMath.IsFinite(tile.Height) || tile.Width <= 0 || tile.Height <= 0))
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        SvgMatrixOperationResult inverse = plan.PrimitiveTransform.Invert();
        if (!inverse.Success)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        if (!TakeBlurWork(step.Bounds.Width, step.Bounds.Height, octaves * 4)) return;
        SvgTurbulenceGenerator generator = new SvgTurbulenceGenerator(seed, frequencyX, frequencyY, octaves, fractalNoise, stitch, tile);
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                SvgPoint devicePoint = new SvgPoint(step.Bounds.X + x + 0.5, step.Bounds.Y + y + 0.5);
                SvgPointTransformResult point = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(devicePoint);
                if (!point.Success) SetError(SvgFilterRenderError.NonFiniteValue);
                else
                {
                    double alpha = generator.Sample(3, (point.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, point.Point.Y);
                    double red = generator.Sample(0, point.Point.X, point.Point.Y);
                    double green = generator.Sample(1, point.Point.X, point.Point.Y);
                    double blue = generator.Sample(2, point.Point.X, point.Point.Y);
                    if (!generator.Success) SetError(SvgFilterRenderError.NonFiniteValue);
                    else
                    {
                        temporary[0] = UnitByte(red * alpha);
                        temporary[1] = UnitByte(green * alpha);
                        temporary[2] = UnitByte(blue * alpha);
                        temporary[3] = UnitByte(alpha);
                        Store(output, x, y, temporary);
                    }
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void Lighting(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        bool specular = step.Primitive.ElementKind == SvgElementKind.FeSpecularLighting;
        double surfaceScale = Number(step.Primitive, SvgAttributeNames.SurfaceScale, 1);
        double lightingConstant = Number(step.Primitive, specular ? SvgAttributeNames.SpecularConstant : SvgAttributeNames.DiffuseConstant, 1);
        double exponent = Number(step.Primitive, SvgAttributeNames.SpecularExponent, 1);
        if (!SvgFiniteMath.IsFinite(surfaceScale) || !SvgFiniteMath.IsFinite(lightingConstant) || lightingConstant < 0 || !SvgFiniteMath.IsFinite(exponent) || specular && (exponent < 1 || exponent > 128) || step.Primitive.Children.Count != 1)
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        SvgLightingKernel? kernel = LightingKernel(step.Primitive);
        SvgLightingSource? source = LightingSource(step.Primitive.Children[0]);
        double[]? color = LightingColor(step);
        SvgMatrixOperationResult inverse = plan.PrimitiveTransform.Invert();
        if (kernel == null || source == null || color == null) return;
        if (!inverse.Success)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        if (!TakeBlurWork(step.Bounds.Width, step.Bounds.Height, 9)) return;
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                int globalX = step.Bounds.X + x;
                int globalY = step.Bounds.Y + y;
                SvgPointTransformResult point = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(new SvgPoint(globalX + 0.5, globalY + 0.5));
                SvgLightingNormal? normal = SurfaceNormal(step, globalX, globalY, x, y, kernel, surfaceScale);
                if (!point.Success || normal == null) SetError(SvgFilterRenderError.NonFiniteValue);
                else
                {
                    SvgLightingVector? light = LightVector(source, (point.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, point.Point.Y, normal.Height);
                    if (light == null) SetError(SvgFilterRenderError.NonFiniteValue);
                    else LightingPixel(specular, lightingConstant, exponent, color, normal, light, temporary);
                    if (error == SvgFilterRenderError.None) Store(output, x, y, temporary);
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    SvgLightingKernel? LightingKernel(SvgSceneFilterPrimitiveData primitive)
    {
        double[] units = NumberList(primitive, SvgAttributeNames.KernelUnitLength);
        if (units.Length > 2 || !ValidNumbers(units)) return LightingKernelFailure(SvgFilterRenderError.InvalidArgument);
        double unitX = units.Length == 0 ? 0 : units[0];
        double unitY = units.Length < 2 ? unitX : units[1];
        SvgVectorTransformResult horizontal;
        SvgVectorTransformResult vertical;
        if (unitX <= 0 || unitY <= 0)
        {
            horizontal = plan.PrimitiveTransform.TransformVector(new SvgVector(1, 0));
            vertical = plan.PrimitiveTransform.TransformVector(new SvgVector(0, 1));
            if (!horizontal.Success || !vertical.Success) return LightingKernelFailure(SvgFilterRenderError.NonFiniteValue);
            double horizontalLength = VectorLength((horizontal.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, horizontal.Vector.Y, 0);
            double verticalLength = VectorLength((vertical.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, vertical.Vector.Y, 0);
            if (horizontalLength == 0 || verticalLength == 0) return LightingKernelFailure(SvgFilterRenderError.NonFiniteValue);
            unitX = 1 / horizontalLength;
            unitY = 1 / verticalLength;
            horizontal.Vector.X = horizontal.Vector.X / horizontalLength;
            horizontal.Vector.Y = horizontal.Vector.Y / horizontalLength;
            vertical.Vector.X = vertical.Vector.X / verticalLength;
            vertical.Vector.Y = vertical.Vector.Y / verticalLength;
        }
        else
        {
            horizontal = plan.PrimitiveTransform.TransformVector(new SvgVector(unitX, 0));
            vertical = plan.PrimitiveTransform.TransformVector(new SvgVector(0, unitY));
            if (!horizontal.Success || !vertical.Success) return LightingKernelFailure(SvgFilterRenderError.NonFiniteValue);
        }
        SvgLightingKernel result = new SvgLightingKernel();
        result.UnitX = unitX;
        result.UnitY = unitY;
        result.HorizontalX = (horizontal.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X;
        result.HorizontalY = horizontal.Vector.Y;
        result.VerticalX = (vertical.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X;
        result.VerticalY = vertical.Vector.Y;
        return result;
    }

    SvgLightingKernel? LightingKernelFailure(SvgFilterRenderError value)
    {
        SetError(value);
        return null;
    }

    SvgLightingNormal? SurfaceNormal(SvgFilterGraphStep step, int x, int y, int localX, int localY, SvgLightingKernel kernel, double surfaceScale)
    {
        double topLeft = LightingAlpha(step, x, y, -1, -1, kernel);
        double topCenter = LightingAlpha(step, x, y, 0, -1, kernel);
        double topRight = LightingAlpha(step, x, y, 1, -1, kernel);
        double middleLeft = LightingAlpha(step, x, y, -1, 0, kernel);
        double middleCenter = LightingAlpha(step, x, y, 0, 0, kernel);
        double middleRight = LightingAlpha(step, x, y, 1, 0, kernel);
        double bottomLeft = LightingAlpha(step, x, y, -1, 1, kernel);
        double bottomCenter = LightingAlpha(step, x, y, 0, 1, kernel);
        double bottomRight = LightingAlpha(step, x, y, 1, 1, kernel);
        if (error != SvgFilterRenderError.None) return null;
        bool left = localX == 0;
        bool right = localX == step.Bounds.Width - 1;
        bool top = localY == 0;
        bool bottom = localY == step.Bounds.Height - 1;
        double gradientX = 0;
        double gradientY = 0;
        double factorX = 0;
        double factorY = 0;
        if (top && left)
        {
            gradientX = -2 * middleCenter + 2 * middleRight - bottomCenter + bottomRight;
            gradientY = -2 * middleCenter - middleRight + 2 * bottomCenter + bottomRight;
            factorX = 2 / (3 * kernel.UnitX);
            factorY = 2 / (3 * kernel.UnitY);
        }
        else if (top && right)
        {
            gradientX = -2 * middleLeft + 2 * middleCenter - bottomLeft + bottomCenter;
            gradientY = -middleLeft - 2 * middleCenter + bottomLeft + 2 * bottomCenter;
            factorX = 2 / (3 * kernel.UnitX);
            factorY = 2 / (3 * kernel.UnitY);
        }
        else if (bottom && left)
        {
            gradientX = -topCenter + topRight - 2 * middleCenter + 2 * middleRight;
            gradientY = -2 * topCenter - topRight + 2 * middleCenter + middleRight;
            factorX = 2 / (3 * kernel.UnitX);
            factorY = 2 / (3 * kernel.UnitY);
        }
        else if (bottom && right)
        {
            gradientX = -topLeft + topCenter - 2 * middleLeft + 2 * middleCenter;
            gradientY = -topLeft - 2 * topCenter + middleLeft + 2 * middleCenter;
            factorX = 2 / (3 * kernel.UnitX);
            factorY = 2 / (3 * kernel.UnitY);
        }
        else if (top)
        {
            gradientX = -2 * middleLeft + 2 * middleRight - bottomLeft + bottomRight;
            gradientY = -middleLeft - 2 * middleCenter - middleRight + bottomLeft + 2 * bottomCenter + bottomRight;
            factorX = 1 / (3 * kernel.UnitX);
            factorY = 1 / (2 * kernel.UnitY);
        }
        else if (bottom)
        {
            gradientX = -topLeft + topRight - 2 * middleLeft + 2 * middleRight;
            gradientY = -topLeft - 2 * topCenter - topRight + middleLeft + 2 * middleCenter + middleRight;
            factorX = 1 / (3 * kernel.UnitX);
            factorY = 1 / (2 * kernel.UnitY);
        }
        else if (left)
        {
            gradientX = -topCenter + topRight - 2 * middleCenter + 2 * middleRight - bottomCenter + bottomRight;
            gradientY = -2 * topCenter - topRight + 2 * bottomCenter + bottomRight;
            factorX = 1 / (2 * kernel.UnitX);
            factorY = 1 / (3 * kernel.UnitY);
        }
        else if (right)
        {
            gradientX = -topLeft + topCenter - 2 * middleLeft + 2 * middleCenter - bottomLeft + bottomCenter;
            gradientY = -topLeft - 2 * topCenter + bottomLeft + 2 * bottomCenter;
            factorX = 1 / (2 * kernel.UnitX);
            factorY = 1 / (3 * kernel.UnitY);
        }
        else
        {
            gradientX = -topLeft + topRight - 2 * middleLeft + 2 * middleRight - bottomLeft + bottomRight;
            gradientY = -topLeft - 2 * topCenter - topRight + bottomLeft + 2 * bottomCenter + bottomRight;
            factorX = 1 / (4 * kernel.UnitX);
            factorY = 1 / (4 * kernel.UnitY);
        }
        double normalX = step.Bounds.Width == 1 ? 0 : -surfaceScale * factorX * gradientX;
        double normalY = step.Bounds.Height == 1 ? 0 : -surfaceScale * factorY * gradientY;
        double length = VectorLength(normalX, normalY, 1);
        if (!SvgFiniteMath.IsFinite(normalX) || !SvgFiniteMath.IsFinite(normalY) || length == 0) return null;
        SvgLightingNormal result = new SvgLightingNormal();
        result.X = normalX / length;
        result.Y = normalY / length;
        result.Z = 1 / length;
        result.Height = surfaceScale * middleCenter;
        return result;
    }

    double LightingAlpha(SvgFilterGraphStep step, int x, int y, int column, int row, SvgLightingKernel kernel)
    {
        double sampleX = x + column * kernel.HorizontalX + row * kernel.VerticalX;
        double sampleY = y + column * kernel.HorizontalY + row * kernel.VerticalY;
        BilinearEdge(step.Inputs[0], sampleX, sampleY, step.ColorSpace, "none", temporary);
        return temporary[3] / 255.0;
    }

    SvgLightingSource? LightingSource(SvgFilterPrimitiveChild child)
    {
        if (child == null) return LightingSourceFailure();
        SvgLightingSource result = new SvgLightingSource();
        result.Kind = child.ElementKind;
        if (child.ElementKind == SvgElementKind.FeDistantLight)
        {
            double azimuth = ChildAngleDegrees(child, SvgAttributeNames.Azimuth, 0) * System.Math.PI / 180;
            double elevation = ChildAngleDegrees(child, SvgAttributeNames.Elevation, 0) * System.Math.PI / 180;
            result.DirectionX = System.Math.Cos(azimuth) * System.Math.Cos(elevation);
            result.DirectionY = System.Math.Sin(azimuth) * System.Math.Cos(elevation);
            result.DirectionZ = System.Math.Sin(elevation);
        }
        else if (child.ElementKind == SvgElementKind.FePointLight || child.ElementKind == SvgElementKind.FeSpotLight)
        {
            result.X = ChildNumber(child, SvgAttributeNames.X, 0);
            result.Y = ChildNumber(child, SvgAttributeNames.Y, 0);
            result.Z = ChildNumber(child, "z", 0);
            if (child.ElementKind == SvgElementKind.FeSpotLight)
            {
                double targetX = ChildNumber(child, "pointsAtX", 0);
                double targetY = ChildNumber(child, "pointsAtY", 0);
                double targetZ = ChildNumber(child, "pointsAtZ", 0);
                result.SpotExponent = ChildNumber(child, SvgAttributeNames.SpecularExponent, 1);
                double coneAngle = ChildAngleDegrees(child, "limitingConeAngle", 90);
                double directionLength = VectorLength(targetX - result.X, targetY - result.Y, targetZ - result.Z);
                if (directionLength > 0)
                {
                    result.DirectionX = (targetX - result.X) / directionLength;
                    result.DirectionY = (targetY - result.Y) / directionLength;
                    result.DirectionZ = (targetZ - result.Z) / directionLength;
                    result.HasDirection = true;
                }
                result.ConeCosine = System.Math.Cos(coneAngle * System.Math.PI / 180);
                if (!SvgFiniteMath.IsFinite(result.SpotExponent) || result.SpotExponent < 1 || result.SpotExponent > 128 || !SvgFiniteMath.IsFinite(coneAngle) || coneAngle < 0 || coneAngle > 90) return LightingSourceFailure();
            }
        }
        else return LightingSourceFailure();
        if (!SvgFiniteMath.IsFinite(result.X) || !SvgFiniteMath.IsFinite(result.Y) || !SvgFiniteMath.IsFinite(result.Z) || !SvgFiniteMath.IsFinite(result.DirectionX) || !SvgFiniteMath.IsFinite(result.DirectionY) || !SvgFiniteMath.IsFinite(result.DirectionZ)) return LightingSourceFailure();
        return result;
    }

    SvgLightingSource? LightingSourceFailure()
    {
        SetError(SvgFilterRenderError.InvalidArgument);
        return null;
    }

    double[]? LightingColor(SvgFilterGraphStep step)
    {
        SvgColor? color = step.Primitive.LightingColor;
        if (color == null) color = new SvgColor(SvgColorSpace.Srgb, 1, 1, 1, 1);
        SvgColorSpace colorSpace = step.ColorSpace == SvgPixelColorSpace.Srgb ? SvgColorSpace.Srgb : SvgColorSpace.SrgbLinear;
        SvgColorOperationResult converted = color.ConvertTo(colorSpace);
        if (!converted.Success)
        {
            SetError(SvgFilterRenderError.ColorConversionFailed);
            return null;
        }
        return [Clamp((converted.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red), Clamp(converted.Color.Green), Clamp(converted.Color.Blue)];
    }

    SvgLightingVector? LightVector(SvgLightingSource source, double x, double y, double height)
    {
        SvgLightingVector result = new SvgLightingVector();
        if (source.Kind == SvgElementKind.FeDistantLight)
        {
            result.X = source.DirectionX;
            result.Y = source.DirectionY;
            result.Z = source.DirectionZ;
            result.Intensity = 1;
            return result;
        }
        double deltaX = source.X - x;
        double deltaY = source.Y - y;
        double deltaZ = source.Z - height;
        double length = VectorLength(deltaX, deltaY, deltaZ);
        if (!SvgFiniteMath.IsFinite(length)) return null;
        if (length == 0) return result;
        result.X = deltaX / length;
        result.Y = deltaY / length;
        result.Z = deltaZ / length;
        result.Intensity = 1;
        if (source.Kind == SvgElementKind.FeSpotLight)
        {
            if (!source.HasDirection)
            {
                result.Intensity = 0;
                return result;
            }
            double focus = -(result.X * source.DirectionX + result.Y * source.DirectionY + result.Z * source.DirectionZ);
            if (focus <= 0 || focus < source.ConeCosine) result.Intensity = 0;
            else result.Intensity = System.Math.Pow(focus, source.SpotExponent);
        }
        return result;
    }

    void LightingPixel(bool specular, double lightingConstant, double exponent, double[] color, SvgLightingNormal normal, SvgLightingVector light, byte[] pixel)
    {
        double product;
        if (specular)
        {
            double halfLength = VectorLength(light.X, light.Y, light.Z + 1);
            product = halfLength == 0 ? 0 : normal.X * light.X / halfLength + normal.Y * light.Y / halfLength + normal.Z * (light.Z + 1) / halfLength;
            product = product <= 0 ? 0 : System.Math.Pow(product, exponent);
        }
        else
        {
            product = normal.X * light.X + normal.Y * light.Y + normal.Z * light.Z;
            if (product < 0) product = 0;
        }
        double intensity = lightingConstant * product * light.Intensity;
        if (!SvgFiniteMath.IsFinite(intensity))
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        double red = Clamp(color[0] * intensity);
        double green = Clamp(color[1] * intensity);
        double blue = Clamp(color[2] * intensity);
        pixel[0] = UnitByte(red);
        pixel[1] = UnitByte(green);
        pixel[2] = UnitByte(blue);
        pixel[3] = specular ? UnitByte(System.Math.Max(red, System.Math.Max(green, blue))) : (byte)255;
    }

    static double VectorLength(double x, double y, double z)
    {
        return System.Math.Sqrt(x * x + y * y + z * z);
    }

    void GaussianBlur(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        double[]? deviations = Deviations(step.Primitive);
        if (deviations == null) return;
        if (!BlurInto(step.Inputs[0], step.Bounds, output, deviations[0], deviations[1], Text(step.Primitive, SvgAttributeNames.EdgeMode, "none"), false)) return;
    }

    void DropShadow(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        double[]? deviations = Deviations(step.Primitive);
        if (deviations == null) return;
        SvgVectorTransformResult offset = plan.PrimitiveTransform.TransformVector(new SvgVector(step.Primitive.OffsetX, step.Primitive.OffsetY));
        if (!offset.Success)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        SvgPixelRect? blurBounds = ShiftedSampleBounds(step.Bounds, (offset.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, offset.Vector.Y);
        if (blurBounds == null) return;
        SvgPixelBufferResult created = SvgPixelBuffer.Create(blurBounds.Width, blurBounds.Height, step.ColorSpace, options.GraphOptions.RasterLimits);
        if (!created.Success)
        {
            SetError(SvgFilterRenderError.BufferOperationFailed);
            return;
        }
        if (!BlurInto(step.Inputs[0], blurBounds, created.Buffer, deviations[0], deviations[1], "none", true)) return;
        SvgPremultipliedRgba8? flood = FloodPixel(step);
        if (flood == null) return;
        if (!TakeWork((long)step.Bounds.Width * step.Bounds.Height)) return;
        SvgFilterImage blurred = new SvgFilterImage(created.Buffer, blurBounds);
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                BilinearImage(blurred, step.Bounds.X + x - offset.Vector.X, step.Bounds.Y + y - offset.Vector.Y, temporary);
                int coverage = temporary[3];
                second[0] = MultiplyByte(flood.Red, coverage);
                second[1] = MultiplyByte(flood.Green, coverage);
                second[2] = MultiplyByte(flood.Blue, coverage);
                second[3] = MultiplyByte(flood.Alpha, coverage);
                Read(step.Inputs[0], step.Bounds.X + x, step.Bounds.Y + y, step.ColorSpace, first);
                if (error == SvgFilterRenderError.None)
                {
                    SvgBlendModeMath.Composite(first, 0, step.ColorSpace, second, 0, step.ColorSpace, SvgCompositeOperator.SourceOver, 1, SvgBlendMode.Normal, SvgBlendColorSpace.Auto, false);
                    Store(output, x, y, second);
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    bool BlurInto(SvgFilterGraphInput input, SvgPixelRect bounds, SvgPixelBuffer? output, double deviationX, double deviationY, string edgeMode, bool alphaOnly)
    {
        double[]? horizontal = Kernel(deviationX);
        double[]? vertical = Kernel(deviationY);
        if (horizontal == null || vertical == null) return false;
        int radiusX = horizontal.Length / 2;
        int radiusY = vertical.Length / 2;
        if (!ValidEdgeMode(edgeMode)) return SetError(SvgFilterRenderError.InvalidArgument);
        long topValue = (long)bounds.Y - radiusY;
        long bottomValue = (long)bounds.Bottom + radiusY;
        long heightValue = (long)bounds.Height + radiusY * 2L;
        if (topValue < int.MinValue || topValue > int.MaxValue || bottomValue < int.MinValue || bottomValue > int.MaxValue || heightValue < 0 || heightValue > int.MaxValue) return SetError(SvgFilterRenderError.NonFiniteValue);
        SvgPixelRect intermediateBounds = new SvgPixelRect(bounds.X, (int)topValue, bounds.Width, (int)heightValue);
        SvgPixelBufferResult created = SvgPixelBuffer.Create(intermediateBounds.Width, intermediateBounds.Height, (output ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ColorSpace, options.GraphOptions.RasterLimits);
        if (!created.Success) return SetError(SvgFilterRenderError.BufferOperationFailed);
        if (!TakeBlurWork(intermediateBounds.Width, intermediateBounds.Height, horizontal.Length)) return false;
        int y = 0;
        while (y < intermediateBounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < intermediateBounds.Width && error == SvgFilterRenderError.None)
            {
                WeightedHorizontal(input, intermediateBounds.X + x, intermediateBounds.Y + y, output.ColorSpace, edgeMode, alphaOnly, horizontal, radiusX, temporary);
                Store(created.Buffer, x, y, temporary);
                x = x + 1;
            }
            y = y + 1;
        }
        if (!TakeBlurWork(bounds.Width, bounds.Height, vertical.Length)) return false;
        y = 0;
        while (y < bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < bounds.Width && error == SvgFilterRenderError.None)
            {
                WeightedVertical(created.Buffer, x, y + radiusY, vertical, radiusY, temporary);
                Store(output, x, y, temporary);
                x = x + 1;
            }
            y = y + 1;
        }
        return error == SvgFilterRenderError.None;
    }

    void WeightedHorizontal(SvgFilterGraphInput input, int x, int y, SvgPixelColorSpace colorSpace, string edgeMode, bool alphaOnly, double[] kernel, int radius, byte[] pixel)
    {
        double red = 0;
        double green = 0;
        double blue = 0;
        double alpha = 0;
        int index = 0;
        while (index < kernel.Length && error == SvgFilterRenderError.None)
        {
            ReadEdge(input, (long)x + index - radius, y, colorSpace, edgeMode, first);
            double weight = kernel[index];
            if (!alphaOnly)
            {
                red = red + first[0] * weight;
                green = green + first[1] * weight;
                blue = blue + first[2] * weight;
            }
            alpha = alpha + first[3] * weight;
            index = index + 1;
        }
        pixel[0] = alphaOnly ? (byte)0 : Byte(red);
        pixel[1] = alphaOnly ? (byte)0 : Byte(green);
        pixel[2] = alphaOnly ? (byte)0 : Byte(blue);
        pixel[3] = Byte(alpha);
        ClampPremultiplied(pixel);
    }

    static void WeightedVertical(SvgPixelBuffer? input, int x, int centerY, double[] kernel, int radius, byte[] pixel)
    {
        double red = 0;
        double green = 0;
        double blue = 0;
        double alpha = 0;
        int index = 0;
        while (index < kernel.Length)
        {
            int sourceIndex = (input ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + (centerY + index - radius) * input.Stride + x * 4;
            double weight = kernel[index];
            red = red + input.Data[sourceIndex] * weight;
            green = green + input.Data[sourceIndex + 1] * weight;
            blue = blue + input.Data[sourceIndex + 2] * weight;
            alpha = alpha + input.Data[sourceIndex + 3] * weight;
            index = index + 1;
        }
        pixel[0] = Byte(red);
        pixel[1] = Byte(green);
        pixel[2] = Byte(blue);
        pixel[3] = Byte(alpha);
        ClampPremultiplied(pixel);
    }

    void ReadEdge(SvgFilterGraphInput input, long x, long y, SvgPixelColorSpace colorSpace, string edgeMode, byte[] pixel)
    {
        if (edgeMode == "duplicate" && !input.Bounds.IsEmpty)
        {
            if (x < input.Bounds.X) x = input.Bounds.X;
            if (x >= input.Bounds.Right) x = input.Bounds.Right - 1;
            if (y < input.Bounds.Y) y = input.Bounds.Y;
            if (y >= input.Bounds.Bottom) y = input.Bounds.Bottom - 1;
        }
        else if (edgeMode == "wrap" && !input.Bounds.IsEmpty)
        {
            x = input.Bounds.X + Mod(x - input.Bounds.X, input.Bounds.Width);
            y = input.Bounds.Y + Mod(y - input.Bounds.Y, input.Bounds.Height);
        }
        if (x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue)
        {
            ClearPixel(pixel);
            return;
        }
        Read(input, (int)x, (int)y, colorSpace, pixel);
    }

    double[]? Deviations(SvgSceneFilterPrimitiveData primitive)
    {
        double[] values = NumberList(primitive, SvgAttributeNames.StdDeviation);
        if (values.Length == 0) values = new double[1];
        if (values.Length > 2 || !ValidNumbers(values))
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return null;
        }
        double x = values[0];
        double y = values.Length == 1 ? x : values[1];
        if (x < 0 || y < 0)
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return null;
        }
        if (x > options.GraphOptions.FilterLimits.MaxBlurStandardDeviation || y > options.GraphOptions.FilterLimits.MaxBlurStandardDeviation)
        {
            SetError(SvgFilterRenderError.WorkLimitExceeded);
            return null;
        }
        SvgVectorTransformResult horizontal = plan.PrimitiveTransform.TransformVector(new SvgVector(x, 0));
        SvgVectorTransformResult vertical = plan.PrimitiveTransform.TransformVector(new SvgVector(0, y));
        if (!horizontal.Success || !vertical.Success)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return null;
        }
        double pixelX = Hypot((horizontal.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, horizontal.Vector.Y);
        double pixelY = Hypot((vertical.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, vertical.Vector.Y);
        if (!SvgFiniteMath.IsFinite(pixelX) || !SvgFiniteMath.IsFinite(pixelY))
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return null;
        }
        double[] result = new double[2];
        result[0] = pixelX;
        result[1] = pixelY;
        return result;
    }

    double[]? Kernel(double deviation)
    {
        if (deviation <= 0)
        {
            double[] identity = new double[1];
            identity[0] = 1;
            return identity;
        }
        if (deviation > options.GraphOptions.FilterLimits.MaxBlurStandardDeviation)
        {
            SetError(SvgFilterRenderError.WorkLimitExceeded);
            return null;
        }
        double radiusValue = System.Math.Ceiling(deviation * 3);
        if (!SvgFiniteMath.IsFinite(radiusValue) || radiusValue > (int.MaxValue - 1) / 2) return KernelFailure(SvgFilterRenderError.NonFiniteValue);
        int radius = (int)radiusValue;
        int count = radius * 2 + 1;
        if (count > options.GraphOptions.FilterLimits.MaxKernelElements) return KernelFailure(SvgFilterRenderError.WorkLimitExceeded);
        double[] kernel = new double[count];
        double denominator = 2 * deviation * deviation;
        double sum = 0;
        int index = 0;
        while (index < count)
        {
            double distance = index - radius;
            double weight = System.Math.Exp(-(distance * distance) / denominator);
            kernel[index] = weight;
            sum = sum + weight;
            index = index + 1;
        }
        if (!SvgFiniteMath.IsFinite(sum) || sum <= 0) return KernelFailure(SvgFilterRenderError.NonFiniteValue);
        index = 0;
        while (index < count)
        {
            kernel[index] = kernel[index] / sum;
            index = index + 1;
        }
        return kernel;
    }

    double[]? KernelFailure(SvgFilterRenderError value)
    {
        SetError(value);
        return null;
    }

    bool TakeBlurWork(int width, int height, int kernel)
    {
        long area = (long)width * height;
        if (kernel < 0 || area > long.MaxValue / kernel) return SetError(SvgFilterRenderError.WorkLimitExceeded);
        return TakeWork(area * kernel);
    }

    SvgPremultipliedRgba8? FloodPixel(SvgFilterGraphStep step)
    {
        SvgColor? color = step.Primitive.FloodColor;
        if (color == null) color = new SvgColor(SvgColorSpace.Srgb, 0, 0, 0, 1);
        double opacity = step.Primitive.FloodOpacity;
        if (!SvgFiniteMath.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return null;
        }
        SvgPaintSampleOptions paintOptions = new SvgPaintSampleOptions();
        paintOptions.OutputColorSpace = step.ColorSpace;
        paintOptions.Opacity = opacity;
        SvgPaintSampleResult sampled = SvgPaintSampleMath.Pixel(color, paintOptions);
        if (!sampled.Success)
        {
            SetError(SvgFilterRenderError.ColorConversionFailed);
            return null;
        }
        return sampled.Pixel;
    }

    SvgPixelRect? ShiftedSampleBounds(SvgPixelRect output, double offsetX, double offsetY)
    {
        if (!SvgFiniteMath.IsFinite(offsetX) || !SvgFiniteMath.IsFinite(offsetY)) return RectFailure(SvgFilterRenderError.NonFiniteValue);
        double leftValue = System.Math.Floor(output.X - offsetX);
        double topValue = System.Math.Floor(output.Y - offsetY);
        double rightValue = System.Math.Floor(output.Right - 1 - offsetX) + 2;
        double bottomValue = System.Math.Floor(output.Bottom - 1 - offsetY) + 2;
        if (leftValue < int.MinValue || leftValue > int.MaxValue || topValue < int.MinValue || topValue > int.MaxValue || rightValue < int.MinValue || rightValue > int.MaxValue || bottomValue < int.MinValue || bottomValue > int.MaxValue) return RectFailure(SvgFilterRenderError.NonFiniteValue);
        long width = (long)rightValue - (long)leftValue;
        long height = (long)bottomValue - (long)topValue;
        if (width < 0 || height < 0 || width > int.MaxValue || height > int.MaxValue) return RectFailure(SvgFilterRenderError.NonFiniteValue);
        return new SvgPixelRect((int)leftValue, (int)topValue, (int)width, (int)height);
    }

    SvgPixelRect? RectFailure(SvgFilterRenderError value)
    {
        SetError(value);
        return null;
    }

    void BilinearImage(SvgFilterImage image, double x, double y, byte[] pixel)
    {
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y) || x < int.MinValue || x >= int.MaxValue || y < int.MinValue || y >= int.MaxValue)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        int left = (int)System.Math.Floor(x);
        int top = (int)System.Math.Floor(y);
        double horizontal = x - left;
        double vertical = y - top;
        ReadImage(image, left, top, first);
        ReadImage(image, left + 1, top, second);
        double redTop = first[0] * (1 - horizontal) + second[0] * horizontal;
        double greenTop = first[1] * (1 - horizontal) + second[1] * horizontal;
        double blueTop = first[2] * (1 - horizontal) + second[2] * horizontal;
        double alphaTop = first[3] * (1 - horizontal) + second[3] * horizontal;
        ReadImage(image, left, top + 1, first);
        ReadImage(image, left + 1, top + 1, second);
        double redBottom = first[0] * (1 - horizontal) + second[0] * horizontal;
        double greenBottom = first[1] * (1 - horizontal) + second[1] * horizontal;
        double blueBottom = first[2] * (1 - horizontal) + second[2] * horizontal;
        double alphaBottom = first[3] * (1 - horizontal) + second[3] * horizontal;
        pixel[0] = Byte(redTop * (1 - vertical) + redBottom * vertical);
        pixel[1] = Byte(greenTop * (1 - vertical) + greenBottom * vertical);
        pixel[2] = Byte(blueTop * (1 - vertical) + blueBottom * vertical);
        pixel[3] = Byte(alphaTop * (1 - vertical) + alphaBottom * vertical);
        ClampPremultiplied(pixel);
    }

    static void ReadImage(SvgFilterImage image, int x, int y, byte[] pixel)
    {
        ClearPixel(pixel);
        if (x < image.Bounds.X || y < image.Bounds.Y || x >= image.Bounds.Right || y >= image.Bounds.Bottom) return;
        int index = (image.Buffer ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + (y - image.Bounds.Y) * image.Buffer.Stride + (x - image.Bounds.X) * 4;
        pixel[0] = image.Buffer.Data[index];
        pixel[1] = image.Buffer.Data[index + 1];
        pixel[2] = image.Buffer.Data[index + 2];
        pixel[3] = image.Buffer.Data[index + 3];
    }

    void ColorMatrix(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        double[]? matrix = Matrix(step.Primitive);
        if (matrix == null) return;
        if (!TakeWork((long)step.Bounds.Width * step.Bounds.Height)) return;
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                Read(step.Inputs[0], step.Bounds.X + x, step.Bounds.Y + y, step.ColorSpace, first);
                if (error == SvgFilterRenderError.None)
                {
                    ApplyMatrix(first, temporary, matrix);
                    Store(output, x, y, temporary);
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    void ComponentTransfer(SvgFilterGraphStep step, SvgPixelBuffer output)
    {
        SvgFilterTransferFunction red = Transfer(step.Primitive, SvgElementKind.FeFuncR);
        SvgFilterTransferFunction green = Transfer(step.Primitive, SvgElementKind.FeFuncG);
        SvgFilterTransferFunction blue = Transfer(step.Primitive, SvgElementKind.FeFuncB);
        SvgFilterTransferFunction alpha = Transfer(step.Primitive, SvgElementKind.FeFuncA);
        if (error != SvgFilterRenderError.None) return;
        if (!TakeWork((long)step.Bounds.Width * step.Bounds.Height)) return;
        int y = 0;
        while (y < step.Bounds.Height && error == SvgFilterRenderError.None)
        {
            int x = 0;
            while (x < step.Bounds.Width && error == SvgFilterRenderError.None)
            {
                Read(step.Inputs[0], step.Bounds.X + x, step.Bounds.Y + y, step.ColorSpace, first);
                if (error == SvgFilterRenderError.None)
                {
                    ApplyTransfer(first, temporary, red, green, blue, alpha);
                    Store(output, x, y, temporary);
                }
                x = x + 1;
            }
            y = y + 1;
        }
    }

    double[]? Matrix(SvgSceneFilterPrimitiveData primitive)
    {
        string type = Text(primitive, SvgAttributeNames.Type, "matrix");
        double[] values = NumberList(primitive, SvgAttributeNames.Values);
        if (!ValidNumbers(values))
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return null;
        }
        if (type == "matrix")
        {
            if (values.Length == 0) return IdentityMatrix();
            if (values.Length != 20)
            {
                SetError(SvgFilterRenderError.InvalidArgument);
                return null;
            }
            return CopyNumbers(values);
        }
        if (type == "saturate")
        {
            if (values.Length > 1)
            {
                SetError(SvgFilterRenderError.InvalidArgument);
                return null;
            }
            double saturation = values.Length == 0 ? 1 : values[0];
            if (!SvgFiniteMath.IsFinite(saturation))
            {
                SetError(SvgFilterRenderError.NonFiniteValue);
                return null;
            }
            return SaturationMatrix(saturation);
        }
        if (type == "huerotate")
        {
            if (values.Length > 1)
            {
                SetError(SvgFilterRenderError.InvalidArgument);
                return null;
            }
            double degrees = values.Length == 0 ? 0 : values[0];
            if (!SvgFiniteMath.IsFinite(degrees))
            {
                SetError(SvgFilterRenderError.NonFiniteValue);
                return null;
            }
            return HueMatrix(degrees);
        }
        if (type == "luminancetoalpha")
        {
            if (values.Length != 0)
            {
                SetError(SvgFilterRenderError.InvalidArgument);
                return null;
            }
            return LuminanceMatrix();
        }
        SetError(SvgFilterRenderError.InvalidArgument);
        return null;
    }

    SvgFilterTransferFunction Transfer(SvgSceneFilterPrimitiveData primitive, SvgElementKind kind)
    {
        SvgFilterPrimitiveChild? selected = null;
        int index = 0;
        while (index < primitive.Children.Count)
        {
            SvgFilterPrimitiveChild child = primitive.Children[index];
            if (child != null && child.ElementKind == kind) selected = child;
            index = index + 1;
        }
        SvgFilterTransferFunction function = new SvgFilterTransferFunction();
        if (selected == null) return function;
        function.Type = ChildText(selected, SvgAttributeNames.Type, "identity");
        function.Table = ChildNumberList(selected, SvgAttributeNames.TableValues);
        if (function.Table.Length > options.MaximumTableValues)
        {
            SetError(SvgFilterRenderError.WorkLimitExceeded);
            return function;
        }
        function.Slope = ChildNumber(selected, SvgAttributeNames.Slope, 1);
        function.Intercept = ChildNumber(selected, SvgAttributeNames.Intercept, 0);
        function.Amplitude = ChildNumber(selected, SvgAttributeNames.Amplitude, 1);
        function.Exponent = ChildNumber(selected, SvgAttributeNames.Exponent, 1);
        function.Offset = ChildNumber(selected, SvgAttributeNames.Offset, 0);
        if (!ValidTransfer(function)) SetError(SvgFilterRenderError.NonFiniteValue);
        return function;
    }

    static void ApplyMatrix(byte[] input, byte[] output, double[] matrix)
    {
        double alpha = input[3] / 255.0;
        double red = alpha == 0 ? 0 : input[0] / 255.0 / alpha;
        double green = alpha == 0 ? 0 : input[1] / 255.0 / alpha;
        double blue = alpha == 0 ? 0 : input[2] / 255.0 / alpha;
        double outputRed = Channel(matrix, 0, red, green, blue, alpha);
        double outputGreen = Channel(matrix, 5, red, green, blue, alpha);
        double outputBlue = Channel(matrix, 10, red, green, blue, alpha);
        double outputAlpha = Channel(matrix, 15, red, green, blue, alpha);
        output[3] = UnitByte(outputAlpha);
        output[0] = UnitByte(outputRed * outputAlpha);
        output[1] = UnitByte(outputGreen * outputAlpha);
        output[2] = UnitByte(outputBlue * outputAlpha);
        ClampPremultiplied(output);
    }

    static void ApplyTransfer(byte[] input, byte[] output, SvgFilterTransferFunction redFunction, SvgFilterTransferFunction greenFunction, SvgFilterTransferFunction blueFunction, SvgFilterTransferFunction alphaFunction)
    {
        double alpha = input[3] / 255.0;
        double red = alpha == 0 ? 0 : input[0] / 255.0 / alpha;
        double green = alpha == 0 ? 0 : input[1] / 255.0 / alpha;
        double blue = alpha == 0 ? 0 : input[2] / 255.0 / alpha;
        double outputAlpha = TransferChannel(alpha, alphaFunction);
        double outputRed = TransferChannel(red, redFunction);
        double outputGreen = TransferChannel(green, greenFunction);
        double outputBlue = TransferChannel(blue, blueFunction);
        output[3] = UnitByte(outputAlpha);
        output[0] = UnitByte(outputRed * outputAlpha);
        output[1] = UnitByte(outputGreen * outputAlpha);
        output[2] = UnitByte(outputBlue * outputAlpha);
        ClampPremultiplied(output);
    }

    static double TransferChannel(double value, SvgFilterTransferFunction function)
    {
        if (function.Type == "table")
        {
            if (function.Table.Length == 0) return Clamp(value);
            if (function.Table.Length == 1) return ClampFinite(function.Table[0]);
            if (value >= 1) return ClampFinite(function.Table[function.Table.Length - 1]);
            double position = Clamp(value) * (function.Table.Length - 1);
            int index = (int)System.Math.Floor(position);
            double amount = position - index;
            return ClampFinite(function.Table[index] * (1 - amount) + function.Table[index + 1] * amount);
        }
        if (function.Type == "discrete")
        {
            if (function.Table.Length == 0) return Clamp(value);
            int index = value >= 1 ? function.Table.Length - 1 : (int)System.Math.Floor(Clamp(value) * function.Table.Length);
            return ClampFinite(function.Table[index]);
        }
        if (function.Type == "linear") return ClampFinite(function.Slope * value + function.Intercept);
        if (function.Type == "gamma") return ClampFinite(function.Amplitude * System.Math.Pow(value, function.Exponent) + function.Offset);
        return Clamp(value);
    }

    static double Channel(double[] matrix, int offset, double red, double green, double blue, double alpha)
    {
        return ClampFinite(matrix[offset] * red + matrix[offset + 1] * green + matrix[offset + 2] * blue + matrix[offset + 3] * alpha + matrix[offset + 4]);
    }

    static double[] IdentityMatrix()
    {
        double[] matrix = new double[20];
        matrix[0] = 1;
        matrix[6] = 1;
        matrix[12] = 1;
        matrix[18] = 1;
        return matrix;
    }

    static double[] SaturationMatrix(double saturation)
    {
        double[] matrix = IdentityMatrix();
        matrix[0] = 0.213 + 0.787 * saturation;
        matrix[1] = 0.715 - 0.715 * saturation;
        matrix[2] = 0.072 - 0.072 * saturation;
        matrix[5] = 0.213 - 0.213 * saturation;
        matrix[6] = 0.715 + 0.285 * saturation;
        matrix[7] = 0.072 - 0.072 * saturation;
        matrix[10] = 0.213 - 0.213 * saturation;
        matrix[11] = 0.715 - 0.715 * saturation;
        matrix[12] = 0.072 + 0.928 * saturation;
        return matrix;
    }

    static double[] HueMatrix(double degrees)
    {
        double radians = degrees * System.Math.PI / 180;
        double cosine = System.Math.Cos(radians);
        double sine = System.Math.Sin(radians);
        double[] matrix = IdentityMatrix();
        matrix[0] = 0.213 + 0.787 * cosine - 0.213 * sine;
        matrix[1] = 0.715 - 0.715 * cosine - 0.715 * sine;
        matrix[2] = 0.072 - 0.072 * cosine + 0.928 * sine;
        matrix[5] = 0.213 - 0.213 * cosine + 0.143 * sine;
        matrix[6] = 0.715 + 0.285 * cosine + 0.140 * sine;
        matrix[7] = 0.072 - 0.072 * cosine - 0.283 * sine;
        matrix[10] = 0.213 - 0.213 * cosine - 0.787 * sine;
        matrix[11] = 0.715 - 0.715 * cosine + 0.715 * sine;
        matrix[12] = 0.072 + 0.928 * cosine + 0.072 * sine;
        return matrix;
    }

    static double[] LuminanceMatrix()
    {
        double[] matrix = new double[20];
        matrix[15] = 0.2125;
        matrix[16] = 0.7154;
        matrix[17] = 0.0721;
        return matrix;
    }

    static double[] CopyNumbers(double[] source)
    {
        double[] copy = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }
        return copy;
    }

    void Bilinear(SvgFilterGraphInput input, double x, double y, SvgPixelColorSpace colorSpace, byte[] pixel)
    {
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y) || x < int.MinValue || x >= int.MaxValue || y < int.MinValue || y >= int.MaxValue)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        int left = (int)System.Math.Floor(x);
        int top = (int)System.Math.Floor(y);
        double horizontal = x - left;
        double vertical = y - top;
        Read(input, left, top, colorSpace, first);
        Read(input, left + 1, top, colorSpace, second);
        double redTop = first[0] * (1 - horizontal) + second[0] * horizontal;
        double greenTop = first[1] * (1 - horizontal) + second[1] * horizontal;
        double blueTop = first[2] * (1 - horizontal) + second[2] * horizontal;
        double alphaTop = first[3] * (1 - horizontal) + second[3] * horizontal;
        Read(input, left, top + 1, colorSpace, first);
        Read(input, left + 1, top + 1, colorSpace, second);
        double redBottom = first[0] * (1 - horizontal) + second[0] * horizontal;
        double greenBottom = first[1] * (1 - horizontal) + second[1] * horizontal;
        double blueBottom = first[2] * (1 - horizontal) + second[2] * horizontal;
        double alphaBottom = first[3] * (1 - horizontal) + second[3] * horizontal;
        pixel[0] = Byte(redTop * (1 - vertical) + redBottom * vertical);
        pixel[1] = Byte(greenTop * (1 - vertical) + greenBottom * vertical);
        pixel[2] = Byte(blueTop * (1 - vertical) + blueBottom * vertical);
        pixel[3] = Byte(alphaTop * (1 - vertical) + alphaBottom * vertical);
        ClampPremultiplied(pixel);
    }

    void BilinearEdge(SvgFilterGraphInput input, double x, double y, SvgPixelColorSpace colorSpace, string edgeMode, byte[] pixel)
    {
        if (!SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y) || x < long.MinValue || x >= long.MaxValue || y < long.MinValue || y >= long.MaxValue)
        {
            SetError(SvgFilterRenderError.NonFiniteValue);
            return;
        }
        long left = (long)System.Math.Floor(x);
        long top = (long)System.Math.Floor(y);
        double horizontal = x - left;
        double vertical = y - top;
        ReadEdge(input, left, top, colorSpace, edgeMode, first);
        ReadEdge(input, left + 1, top, colorSpace, edgeMode, second);
        double redTop = first[0] * (1 - horizontal) + second[0] * horizontal;
        double greenTop = first[1] * (1 - horizontal) + second[1] * horizontal;
        double blueTop = first[2] * (1 - horizontal) + second[2] * horizontal;
        double alphaTop = first[3] * (1 - horizontal) + second[3] * horizontal;
        ReadEdge(input, left, top + 1, colorSpace, edgeMode, first);
        ReadEdge(input, left + 1, top + 1, colorSpace, edgeMode, second);
        double redBottom = first[0] * (1 - horizontal) + second[0] * horizontal;
        double greenBottom = first[1] * (1 - horizontal) + second[1] * horizontal;
        double blueBottom = first[2] * (1 - horizontal) + second[2] * horizontal;
        double alphaBottom = first[3] * (1 - horizontal) + second[3] * horizontal;
        pixel[0] = Byte(redTop * (1 - vertical) + redBottom * vertical);
        pixel[1] = Byte(greenTop * (1 - vertical) + greenBottom * vertical);
        pixel[2] = Byte(blueTop * (1 - vertical) + blueBottom * vertical);
        pixel[3] = Byte(alphaTop * (1 - vertical) + alphaBottom * vertical);
        ClampPremultiplied(pixel);
    }

    void Read(SvgFilterGraphInput input, int x, int y, SvgPixelColorSpace colorSpace, byte[] pixel)
    {
        ClearPixel(pixel);
        if (input.SurfaceIndex < 0 || x < input.Bounds.X || y < input.Bounds.Y || x >= input.Bounds.Right || y >= input.Bounds.Bottom) return;
        if (input.SurfaceIndex >= plan.Surfaces.Count)
        {
            SetError(SvgFilterRenderError.InvalidArgument);
            return;
        }
        SvgFilterSurface surface = plan.Surfaces[input.SurfaceIndex];
        int localX = x - input.Bounds.X;
        int localY = y - input.Bounds.Y;
        SvgPixelReadResult read = (surface.Buffer ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Read(localX, localY);
        if (!read.Success)
        {
            SetError(SvgFilterRenderError.BufferOperationFailed);
            return;
        }
        SvgPremultipliedRgba8? value = read.Pixel;
        if (surface.ColorSpace != colorSpace)
        {
            value = Convert(value, surface.ColorSpace, colorSpace);
            if (value == null)
            {
                SetError(SvgFilterRenderError.ColorConversionFailed);
                return;
            }
        }
        pixel[0] = (value ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red;
        pixel[1] = value.Green;
        pixel[2] = value.Blue;
        pixel[3] = value.Alpha;
    }

    bool CreateOutput()
    {
        if (plan.FinalSurfaceIndex < 0)
        {
            SvgPixelBufferResult empty = SvgPixelBuffer.Create(0, 0, options.GraphOptions.DefaultColorSpace, options.GraphOptions.RasterLimits);
            if (!empty.Success) return SetError(SvgFilterRenderError.BufferOperationFailed);
            result.Output = empty.Buffer;
            return true;
        }
        if (plan.FinalSurfaceIndex >= plan.Surfaces.Count) return SetError(SvgFilterRenderError.InvalidArgument);
        SvgFilterSurface surface = plan.Surfaces[plan.FinalSurfaceIndex];
        SvgPixelBufferResult view = (surface.Buffer ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Subsurface(new SvgPixelRect(0, 0, plan.FinalBounds.Width, plan.FinalBounds.Height));
        if (!view.Success) return SetError(SvgFilterRenderError.BufferOperationFailed);
        result.Output = view.Buffer;
        return true;
    }

    bool TakeWork(long amount)
    {
        if (amount < 0 || amount > options.MaximumPixels - result.PixelsProcessed) return SetError(SvgFilterRenderError.WorkLimitExceeded);
        result.PixelsProcessed = result.PixelsProcessed + amount;
        return true;
    }

    bool SetError(SvgFilterRenderError value)
    {
        if (error == SvgFilterRenderError.None) error = value;
        return false;
    }

    SvgFilterRenderResult Failure()
    {
        result.Success = false;
        result.Error = error == SvgFilterRenderError.None ? SvgFilterRenderError.InvalidArgument : error;
        result.Output = null;
        return result;
    }

    static bool Supported(SvgElementKind kind)
    {
        return kind == SvgElementKind.FeBlend || kind == SvgElementKind.FeColorMatrix || kind == SvgElementKind.FeComponentTransfer || kind == SvgElementKind.FeComposite || kind == SvgElementKind.FeConvolveMatrix || kind == SvgElementKind.FeDiffuseLighting || kind == SvgElementKind.FeDisplacementMap || kind == SvgElementKind.FeDropShadow || kind == SvgElementKind.FeGaussianBlur || kind == SvgElementKind.FeImage || kind == SvgElementKind.FeMerge || kind == SvgElementKind.FeFlood || kind == SvgElementKind.FeMorphology || kind == SvgElementKind.FeOffset || kind == SvgElementKind.FeSpecularLighting || kind == SvgElementKind.FeTile || kind == SvgElementKind.FeTurbulence;
    }

    static string Text(SvgSceneFilterPrimitiveData primitive, string name, string fallback)
    {
        SvgFilterPrimitiveAttribute? attribute = Attribute(primitive, name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || !attribute.TypedValue.HasValue) return fallback;
        string text = attribute.TypedValue.EffectiveText;
        return text == null || text.Length == 0 ? fallback : text.Trim().ToLowerInvariant();
    }

    static double Number(SvgSceneFilterPrimitiveData primitive, string name, double fallback)
    {
        SvgFilterPrimitiveAttribute? attribute = Attribute(primitive, name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.Number || attribute.TypedValue.Value == null) return fallback;
        return (double)attribute.TypedValue.Value;
    }

    static double[] NumberList(SvgSceneFilterPrimitiveData primitive, string name)
    {
        SvgFilterPrimitiveAttribute? attribute = Attribute(primitive, name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.NumberList || attribute.TypedValue.Value == null) return new double[0];
        return (double[])attribute.TypedValue.Value;
    }

    static int Integer(SvgSceneFilterPrimitiveData primitive, string name, int fallback)
    {
        SvgFilterPrimitiveAttribute? attribute = Attribute(primitive, name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.Integer || attribute.TypedValue.Value == null) return fallback;
        return (int)attribute.TypedValue.Value;
    }

    static int[] IntegerList(SvgSceneFilterPrimitiveData primitive, string name)
    {
        SvgFilterPrimitiveAttribute? attribute = Attribute(primitive, name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.IntegerList || attribute.TypedValue.Value == null) return new int[0];
        return (int[])attribute.TypedValue.Value;
    }

    static string ChildText(SvgFilterPrimitiveChild child, string name, string fallback)
    {
        SvgFilterPrimitiveAttribute? attribute = child.FindAttribute(name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || !attribute.TypedValue.HasValue) return fallback;
        string text = attribute.TypedValue.EffectiveText;
        return text == null || text.Length == 0 ? fallback : text.Trim().ToLowerInvariant();
    }

    static double ChildNumber(SvgFilterPrimitiveChild child, string name, double fallback)
    {
        SvgFilterPrimitiveAttribute? attribute = child.FindAttribute(name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.Number || attribute.TypedValue.Value == null) return fallback;
        return (double)attribute.TypedValue.Value;
    }

    static double ChildAngleDegrees(SvgFilterPrimitiveChild child, string name, double fallback)
    {
        SvgFilterPrimitiveAttribute? attribute = child.FindAttribute(name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.Angle || attribute.TypedValue.Value == null) return fallback;
        SvgUnitConversionResult converted = ((SvgAngle)attribute.TypedValue.Value).ConvertTo(SvgAngleUnit.Deg);
        return converted.Success ? converted.Value : fallback;
    }

    static double[] ChildNumberList(SvgFilterPrimitiveChild child, string name)
    {
        SvgFilterPrimitiveAttribute? attribute = child.FindAttribute(name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.NumberList || attribute.TypedValue.Value == null) return new double[0];
        return (double[])attribute.TypedValue.Value;
    }

    static SvgFilterPrimitiveAttribute? Attribute(SvgSceneFilterPrimitiveData primitive, string name)
    {
        int index = 0;
        while (index < primitive.Attributes.Count)
        {
            SvgFilterPrimitiveAttribute attribute = primitive.Attributes[index];
            if (attribute != null && attribute.Name == name) return attribute;
            index = index + 1;
        }
        return null;
    }

    static SvgCompositeOperator CompositeOperator(string text)
    {
        if (text == "in") return SvgCompositeOperator.SourceIn;
        if (text == "out") return SvgCompositeOperator.SourceOut;
        if (text == "atop") return SvgCompositeOperator.SourceAtop;
        if (text == "xor") return SvgCompositeOperator.Xor;
        if (text == "lighter") return SvgCompositeOperator.Lighter;
        return SvgCompositeOperator.SourceOver;
    }

    static void Arithmetic(byte[] input1, byte[] input2, byte[] output, double k1, double k2, double k3, double k4)
    {
        double alpha = ArithmeticChannel(input1[3], input2[3], k1, k2, k3, k4);
        double red = ArithmeticChannel(input1[0], input2[0], k1, k2, k3, k4);
        double green = ArithmeticChannel(input1[1], input2[1], k1, k2, k3, k4);
        double blue = ArithmeticChannel(input1[2], input2[2], k1, k2, k3, k4);
        output[3] = UnitByte(alpha);
        output[0] = UnitByte(System.Math.Min(red, alpha));
        output[1] = UnitByte(System.Math.Min(green, alpha));
        output[2] = UnitByte(System.Math.Min(blue, alpha));
    }

    static double ArithmeticChannel(byte first, byte second, double k1, double k2, double k3, double k4)
    {
        double left = first / 255.0;
        double right = second / 255.0;
        return Clamp(k1 * left * right + k2 * left + k3 * right + k4);
    }

    static SvgPremultipliedRgba8? Convert(SvgPremultipliedRgba8? pixel, SvgPixelColorSpace source, SvgPixelColorSpace destination)
    {
        if ((pixel ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Alpha == 0) return new SvgPremultipliedRgba8(0, 0, 0, 0);
        double alpha = pixel.Alpha / 255.0;
        SvgColorSpace sourceSpace = source == SvgPixelColorSpace.Srgb ? SvgColorSpace.Srgb : SvgColorSpace.SrgbLinear;
        SvgColorSpace destinationSpace = destination == SvgPixelColorSpace.Srgb ? SvgColorSpace.Srgb : SvgColorSpace.SrgbLinear;
        SvgColor color = new SvgColor(sourceSpace, pixel.Red / 255.0 / alpha, pixel.Green / 255.0 / alpha, pixel.Blue / 255.0 / alpha, alpha);
        SvgColorOperationResult converted = color.ConvertTo(destinationSpace);
        if (!converted.Success) return null;
        return new SvgPremultipliedRgba8(UnitByte((converted.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Red * alpha), UnitByte(converted.Color.Green * alpha), UnitByte(converted.Color.Blue * alpha), pixel.Alpha);
    }

    static void Store(SvgPixelBuffer? output, int x, int y, byte[] pixel)
    {
        int index = (output ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Offset + y * output.Stride + x * 4;
        output.Data[index] = pixel[0];
        output.Data[index + 1] = pixel[1];
        output.Data[index + 2] = pixel[2];
        output.Data[index + 3] = pixel[3];
    }

    static void CopyPixel(byte[] source, byte[] destination)
    {
        destination[0] = source[0];
        destination[1] = source[1];
        destination[2] = source[2];
        destination[3] = source[3];
    }

    static void ClearPixel(byte[] pixel)
    {
        pixel[0] = 0;
        pixel[1] = 0;
        pixel[2] = 0;
        pixel[3] = 0;
    }

    static void ClampPremultiplied(byte[] pixel)
    {
        if (pixel[0] > pixel[3]) pixel[0] = pixel[3];
        if (pixel[1] > pixel[3]) pixel[1] = pixel[3];
        if (pixel[2] > pixel[3]) pixel[2] = pixel[3];
    }

    static int Mod(long value, int divisor)
    {
        long result = value % divisor;
        return (int)(result < 0 ? result + divisor : result);
    }

    static bool ValidEdgeMode(string value)
    {
        return value == "none" || value == "duplicate" || value == "wrap";
    }

    static bool ValidChannel(string value)
    {
        return value == "r" || value == "g" || value == "b" || value == "a";
    }

    static double Channel(byte[] pixel, string selector)
    {
        if (selector == "a") return pixel[3] / 255.0;
        int index = selector == "r" ? 0 : selector == "g" ? 1 : 2;
        return pixel[3] == 0 ? 0 : System.Math.Min(1, pixel[index] / (double)pixel[3]);
    }

    static double Straight(byte value, byte alpha)
    {
        return alpha == 0 ? 0 : System.Math.Min(255, value * 255.0 / alpha);
    }

    static double Hypot(double x, double y)
    {
        double horizontal = System.Math.Abs(x);
        double vertical = System.Math.Abs(y);
        double maximum = System.Math.Max(horizontal, vertical);
        if (maximum == 0) return 0;
        horizontal = horizontal / maximum;
        vertical = vertical / maximum;
        return maximum * System.Math.Sqrt(horizontal * horizontal + vertical * vertical);
    }

    static byte MultiplyByte(byte value, int factor)
    {
        return (byte)((value * factor + 127) / 255);
    }

    static double Clamp(double value)
    {
        if (value <= 0) return 0;
        if (value >= 1) return 1;
        return value;
    }

    static double ClampFinite(double value)
    {
        if (double.IsNaN(value) || value <= 0) return 0;
        if (value >= 1) return 1;
        return value;
    }

    static bool ValidTransfer(SvgFilterTransferFunction function)
    {
        if (!SvgFiniteMath.IsFinite(function.Slope) || !SvgFiniteMath.IsFinite(function.Intercept) || !SvgFiniteMath.IsFinite(function.Amplitude) || !SvgFiniteMath.IsFinite(function.Exponent) || !SvgFiniteMath.IsFinite(function.Offset)) return false;
        int index = 0;
        while (index < function.Table.Length)
        {
            if (!SvgFiniteMath.IsFinite(function.Table[index])) return false;
            index = index + 1;
        }
        return true;
    }

    static bool ValidNumbers(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!SvgFiniteMath.IsFinite(values[index])) return false;
            index = index + 1;
        }
        return true;
    }

    static byte UnitByte(double value)
    {
        return (byte)System.Math.Round(Clamp(value) * 255, System.MidpointRounding.AwayFromZero);
    }

    static byte Byte(double value)
    {
        if (value <= 0) return 0;
        if (value >= 255) return 255;
        return (byte)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }

    static SvgPixelRect CopyRect(SvgPixelRect source)
    {
        return new SvgPixelRect(source.X, source.Y, source.Width, source.Height);
    }
}

internal class SvgFilterTransferFunction
{
    internal string Type;
    internal double[] Table;
    internal double Slope;
    internal double Intercept;
    internal double Amplitude;
    internal double Exponent;
    internal double Offset;

    internal SvgFilterTransferFunction()
    {
        Type = "identity";
        Table = new double[0];
        Slope = 1;
        Intercept = 0;
        Amplitude = 1;
        Exponent = 1;
        Offset = 0;
    }
}

internal class SvgFilterImage
{
    internal SvgPixelBuffer? Buffer;
    internal SvgPixelRect Bounds;

    internal SvgFilterImage(SvgPixelBuffer? buffer, SvgPixelRect bounds)
    {
        Buffer = buffer;
        Bounds = bounds;
    }
}

internal class SvgConvolutionStep
{
    internal double HorizontalX;
    internal double HorizontalY;
    internal double VerticalX;
    internal double VerticalY;
    internal bool Interpolated;

    internal SvgConvolutionStep()
    {
        HorizontalX = 1;
        HorizontalY = 0;
        VerticalX = 0;
        VerticalY = 1;
        Interpolated = false;
    }
}

internal class SvgLightingKernel
{
    internal double UnitX;
    internal double UnitY;
    internal double HorizontalX;
    internal double HorizontalY;
    internal double VerticalX;
    internal double VerticalY;
}

internal class SvgLightingNormal
{
    internal double X;
    internal double Y;
    internal double Z;
    internal double Height;
}

internal class SvgLightingSource
{
    internal SvgElementKind Kind;
    internal double X;
    internal double Y;
    internal double Z;
    internal double DirectionX;
    internal double DirectionY;
    internal double DirectionZ;
    internal double SpotExponent;
    internal double ConeCosine;
    internal bool HasDirection;
}

internal class SvgLightingVector
{
    internal double X;
    internal double Y;
    internal double Z;
    internal double Intensity;
}

internal class SvgTurbulenceGenerator
{
    const int TableSize = 256;
    const int TableMask = 255;
    const int PerlinOffset = 4096;
    const long RandomModulus = 2147483647;
    const long RandomMultiplier = 16807;
    const long RandomQuotient = 127773;
    const long RandomRemainder = 2836;

    int[] selectors;
    double[,,] gradients;
    long randomSeed;
    double frequencyX;
    double frequencyY;
    int octaves;
    bool fractalNoise;
    bool stitch;
    SvgRect? tile;

    internal bool Success;

    internal SvgTurbulenceGenerator(double seed, double horizontalFrequency, double verticalFrequency, int octaveCount, bool useFractalNoise, bool stitchTiles, SvgRect? tileRegion)
    {
        selectors = new int[TableSize * 2 + 2];
        gradients = new double[4, TableSize * 2 + 2, 2];
        randomSeed = NormalizeSeed(seed);
        frequencyX = horizontalFrequency;
        frequencyY = verticalFrequency;
        octaves = octaveCount;
        fractalNoise = useFractalNoise;
        stitch = stitchTiles;
        tile = tileRegion;
        Success = true;
        Initialize();
        if (stitch)
        {
            frequencyX = StitchFrequency(frequencyX, (tile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width);
            frequencyY = StitchFrequency(frequencyY, tile.Height);
        }
    }

    internal double Sample(int channel, double x, double y)
    {
        if (!Success || channel < 0 || channel > 3 || !SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y))
        {
            Success = false;
            return 0;
        }
        SvgTurbulenceStitchInfo? stitchInfo = stitch ? CreateStitchInfo() : null;
        if (!Success) return 0;
        double pointX = x * frequencyX;
        double pointY = y * frequencyY;
        if (!SvgFiniteMath.IsFinite(pointX) || !SvgFiniteMath.IsFinite(pointY))
        {
            Success = false;
            return 0;
        }
        double sum = 0;
        double ratio = 1;
        int octave = 0;
        while (octave < octaves && Success)
        {
            double noise = Noise(channel, pointX, pointY, stitchInfo);
            sum = sum + (fractalNoise ? noise : System.Math.Abs(noise)) / ratio;
            pointX = pointX * 2;
            pointY = pointY * 2;
            ratio = ratio * 2;
            if (stitchInfo != null) DoubleStitch(stitchInfo);
            octave = octave + 1;
        }
        if (!Success || !SvgFiniteMath.IsFinite(sum))
        {
            Success = false;
            return 0;
        }
        double value = fractalNoise ? (sum + 1) / 2 : sum;
        if (value <= 0) return 0;
        if (value >= 1) return 1;
        return value;
    }

    void Initialize()
    {
        int channel = 0;
        int index = 0;
        while (channel < 4)
        {
            index = 0;
            while (index < TableSize)
            {
                selectors[index] = index;
                double horizontal = 0;
                double vertical = 0;
                double length = 0;
                do
                {
                    horizontal = (NextRandom() % (TableSize * 2) - TableSize) / (double)TableSize;
                    vertical = (NextRandom() % (TableSize * 2) - TableSize) / (double)TableSize;
                    length = horizontal * horizontal + vertical * vertical;
                }
                while (length > 1 || length == 0);
                double scale = System.Math.Sqrt(length);
                gradients[channel, index, 0] = horizontal / scale;
                gradients[channel, index, 1] = vertical / scale;
                index = index + 1;
            }
            channel = channel + 1;
        }
        index = TableSize;
        while (index > 1)
        {
            index = index - 1;
            int selected = (int)(NextRandom() % TableSize);
            int swap = selectors[index];
            selectors[index] = selectors[selected];
            selectors[selected] = swap;
        }
        index = 0;
        while (index < TableSize + 2)
        {
            selectors[TableSize + index] = selectors[index];
            channel = 0;
            while (channel < 4)
            {
                gradients[channel, TableSize + index, 0] = gradients[channel, index, 0];
                gradients[channel, TableSize + index, 1] = gradients[channel, index, 1];
                channel = channel + 1;
            }
            index = index + 1;
        }
    }

    double Noise(int channel, double x, double y, SvgTurbulenceStitchInfo? stitchInfo)
    {
        double shiftedX = x + PerlinOffset;
        double shiftedY = y + PerlinOffset;
        if (!SvgFiniteMath.IsFinite(shiftedX) || !SvgFiniteMath.IsFinite(shiftedY) || shiftedX < int.MinValue || shiftedX >= int.MaxValue || shiftedY < int.MinValue || shiftedY >= int.MaxValue)
        {
            Success = false;
            return 0;
        }
        int baseX = (int)shiftedX;
        int baseY = (int)shiftedY;
        int nextX = baseX + 1;
        int nextY = baseY + 1;
        double remainderX = shiftedX - baseX;
        double remainderY = shiftedY - baseY;
        if (stitchInfo != null)
        {
            if (baseX >= stitchInfo.WrapX) baseX = baseX - stitchInfo.Width;
            if (nextX >= stitchInfo.WrapX) nextX = nextX - stitchInfo.Width;
            if (baseY >= stitchInfo.WrapY) baseY = baseY - stitchInfo.Height;
            if (nextY >= stitchInfo.WrapY) nextY = nextY - stitchInfo.Height;
        }
        baseX = baseX & TableMask;
        nextX = nextX & TableMask;
        baseY = baseY & TableMask;
        nextY = nextY & TableMask;
        int firstBase = selectors[baseX];
        int nextBase = selectors[nextX];
        int bottomLeft = selectors[firstBase + baseY];
        int topLeft = selectors[firstBase + nextY];
        int bottomRight = selectors[nextBase + baseY];
        int topRight = selectors[nextBase + nextY];
        double smoothX = Smooth(remainderX);
        double smoothY = Smooth(remainderY);
        double bottom = Interpolate(smoothX, Dot(channel, bottomLeft, remainderX, remainderY), Dot(channel, bottomRight, remainderX - 1, remainderY));
        double top = Interpolate(smoothX, Dot(channel, topLeft, remainderX, remainderY - 1), Dot(channel, topRight, remainderX - 1, remainderY - 1));
        return Interpolate(smoothY, bottom, top);
    }

    SvgTurbulenceStitchInfo? CreateStitchInfo()
    {
        double widthValue = (tile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width * frequencyX + 0.5;
        double heightValue = tile.Height * frequencyY + 0.5;
        if (!SvgFiniteMath.IsFinite(widthValue) || !SvgFiniteMath.IsFinite(heightValue) || widthValue < 0 || widthValue > int.MaxValue || heightValue < 0 || heightValue > int.MaxValue)
        {
            Success = false;
            return null;
        }
        SvgTurbulenceStitchInfo value = new SvgTurbulenceStitchInfo();
        value.Width = (int)widthValue;
        value.Height = (int)heightValue;
        double wrapX = tile.X * frequencyX + PerlinOffset + value.Width;
        double wrapY = tile.Y * frequencyY + PerlinOffset + value.Height;
        if (!SvgFiniteMath.IsFinite(wrapX) || !SvgFiniteMath.IsFinite(wrapY) || wrapX < int.MinValue || wrapX > int.MaxValue || wrapY < int.MinValue || wrapY > int.MaxValue)
        {
            Success = false;
            return null;
        }
        value.WrapX = (int)wrapX;
        value.WrapY = (int)wrapY;
        return value;
    }

    void DoubleStitch(SvgTurbulenceStitchInfo value)
    {
        long width = (long)value.Width * 2;
        long height = (long)value.Height * 2;
        long wrapX = (long)value.WrapX * 2 - PerlinOffset;
        long wrapY = (long)value.WrapY * 2 - PerlinOffset;
        if (width > int.MaxValue || height > int.MaxValue || wrapX < int.MinValue || wrapX > int.MaxValue || wrapY < int.MinValue || wrapY > int.MaxValue)
        {
            Success = false;
            return;
        }
        value.Width = (int)width;
        value.Height = (int)height;
        value.WrapX = (int)wrapX;
        value.WrapY = (int)wrapY;
    }

    long NextRandom()
    {
        long value = RandomMultiplier * (randomSeed % RandomQuotient) - RandomRemainder * (randomSeed / RandomQuotient);
        if (value <= 0) value = value + RandomModulus;
        randomSeed = value;
        return value;
    }

    double Dot(int channel, int index, double x, double y)
    {
        return x * gradients[channel, index, 0] + y * gradients[channel, index, 1];
    }

    static long NormalizeSeed(double value)
    {
        double integer = System.Math.Truncate(value);
        if (integer > RandomModulus - 1) return RandomModulus - 1;
        if (integer <= 0)
        {
            double remainder = integer % (RandomModulus - 1);
            return -(long)remainder + 1;
        }
        return (long)integer;
    }

    static double StitchFrequency(double frequency, double size)
    {
        if (frequency == 0) return 0;
        double lower = System.Math.Floor(size * frequency) / size;
        double upper = System.Math.Ceiling(size * frequency) / size;
        if (lower == 0) return upper;
        return frequency / lower < upper / frequency ? lower : upper;
    }

    static double Smooth(double value)
    {
        return value * value * (3 - 2 * value);
    }

    static double Interpolate(double amount, double start, double end)
    {
        return start + amount * (end - start);
    }
}

internal class SvgTurbulenceStitchInfo
{
    internal int Width;
    internal int Height;
    internal int WrapX;
    internal int WrapY;
}
