using System.Collections.Generic;

namespace jpeg;

internal class JpegMcuLayout
{
    internal int Width;
    internal int Height;
    internal int ComponentCount;
    internal int MaxHorizontalSamplingFactor;
    internal int MaxVerticalSamplingFactor;
    internal int McuWidth;
    internal int McuHeight;
    internal int McusPerRow;
    internal int McuRows;
    internal int McuCount;
    internal int BlocksPerMcu;
    internal List<JpegMcuComponentLayout> Components = new List<JpegMcuComponentLayout>();
}

internal class JpegMcuComponentLayout
{
    internal int ComponentIndex;
    internal int Identifier;
    internal int HorizontalSamplingFactor;
    internal int VerticalSamplingFactor;
    internal int SampleWidth;
    internal int SampleHeight;
    internal int PaddedSampleWidth;
    internal int PaddedSampleHeight;
    internal int BlocksPerRow;
    internal int BlockRows;
    internal int TotalBlockCount;
    internal int McuBlockOffset;
    internal int McuBlockCount;
}

internal class JpegMcuLayoutCalculator
{
    internal JpegMcuLayout Calculate(JpegFrameHeader frame)
    {
        ValidateFrame(frame);

        JpegMcuLayout layout = new JpegMcuLayout();
        layout.Width = frame.Width;
        layout.Height = frame.Height;
        layout.ComponentCount = GetComponentCount(frame);
        layout.MaxHorizontalSamplingFactor = FindMaxHorizontalSamplingFactor(frame);
        layout.MaxVerticalSamplingFactor = FindMaxVerticalSamplingFactor(frame);
        layout.McuWidth = layout.MaxHorizontalSamplingFactor * JpegConstants.BlockWidth;
        layout.McuHeight = layout.MaxVerticalSamplingFactor * JpegConstants.BlockHeight;
        layout.McusPerRow = DivideRoundUp(layout.Width, layout.McuWidth);
        layout.McuRows = DivideRoundUp(layout.Height, layout.McuHeight);
        layout.McuCount = layout.McusPerRow * layout.McuRows;

        int componentIndex = 0;
        int blockOffset = 0;
        while (componentIndex < layout.ComponentCount)
        {
            JpegFrameComponent component = frame.Components[componentIndex];
            JpegMcuComponentLayout componentLayout = CalculateComponentLayout(layout, component, componentIndex, blockOffset);
            layout.Components.Add(componentLayout);
            blockOffset = blockOffset + componentLayout.McuBlockCount;
            componentIndex = componentIndex + 1;
        }

        layout.BlocksPerMcu = blockOffset;
        return layout;
    }

    internal JpegMcuComponentLayout CalculateComponentLayout(JpegMcuLayout layout, JpegFrameComponent component, int componentIndex, int blockOffset)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        ValidateComponent(component);

        JpegMcuComponentLayout componentLayout = new JpegMcuComponentLayout();
        componentLayout.ComponentIndex = componentIndex;
        componentLayout.Identifier = component.Identifier;
        componentLayout.HorizontalSamplingFactor = component.HorizontalSamplingFactor;
        componentLayout.VerticalSamplingFactor = component.VerticalSamplingFactor;
        componentLayout.SampleWidth = ScaleDimension(layout.Width, component.HorizontalSamplingFactor, layout.MaxHorizontalSamplingFactor);
        componentLayout.SampleHeight = ScaleDimension(layout.Height, component.VerticalSamplingFactor, layout.MaxVerticalSamplingFactor);
        componentLayout.BlocksPerRow = DivideRoundUp(componentLayout.SampleWidth, JpegConstants.BlockWidth);
        componentLayout.BlockRows = DivideRoundUp(componentLayout.SampleHeight, JpegConstants.BlockHeight);
        componentLayout.PaddedSampleWidth = componentLayout.BlocksPerRow * JpegConstants.BlockWidth;
        componentLayout.PaddedSampleHeight = componentLayout.BlockRows * JpegConstants.BlockHeight;
        componentLayout.McuBlockOffset = blockOffset;
        componentLayout.McuBlockCount = component.HorizontalSamplingFactor * component.VerticalSamplingFactor;
        componentLayout.TotalBlockCount = componentLayout.BlocksPerRow * componentLayout.BlockRows;
        return componentLayout;
    }

    internal JpegMcuComponentLayout FindComponentLayout(JpegMcuLayout layout, int identifier)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        foreach (JpegMcuComponentLayout component in layout.Components)
        {
            if (component.Identifier == identifier)
            {
                return component;
            }
        }

        throw JpegInvalidDataException.Create("MCU layout component was not found.");
    }

    internal int FindMaxHorizontalSamplingFactor(JpegFrameHeader frame)
    {
        ValidateFrame(frame);

        int max = 1;
        foreach (JpegFrameComponent component in frame.Components)
        {
            if (component.HorizontalSamplingFactor > max)
            {
                max = component.HorizontalSamplingFactor;
            }
        }

        return max;
    }

    internal int FindMaxVerticalSamplingFactor(JpegFrameHeader frame)
    {
        ValidateFrame(frame);

        int max = 1;
        foreach (JpegFrameComponent component in frame.Components)
        {
            if (component.VerticalSamplingFactor > max)
            {
                max = component.VerticalSamplingFactor;
            }
        }

        return max;
    }

    internal int GetComponentCount(JpegFrameHeader frame)
    {
        if (frame.Components == null)
        {
            return 0;
        }

        return frame.Components.Count;
    }

    internal int ScaleDimension(int dimension, int samplingFactor, int maxSamplingFactor)
    {
        if (dimension < 0)
        {
            throw new System.ArgumentOutOfRangeException("dimension");
        }

        if (samplingFactor < 1 || samplingFactor > 4)
        {
            throw new System.ArgumentOutOfRangeException("samplingFactor");
        }

        if (maxSamplingFactor < samplingFactor || maxSamplingFactor > 4)
        {
            throw new System.ArgumentOutOfRangeException("maxSamplingFactor");
        }

        return DivideRoundUpLong((long)dimension * (long)samplingFactor, maxSamplingFactor);
    }

    internal int DivideRoundUp(int value, int divisor)
    {
        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        return DivideRoundUpLong(value, divisor);
    }

    internal int DivideRoundUpLong(long value, int divisor)
    {
        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        if (divisor <= 0)
        {
            throw new System.ArgumentOutOfRangeException("divisor");
        }

        long result = (value + divisor - 1) / divisor;
        if (result > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("Rounded-up value is too large.");
        }

        return (int)result;
    }

    internal void ValidateFrame(JpegFrameHeader frame)
    {
        if (frame == null)
        {
            throw new System.ArgumentNullException("frame");
        }

        if (frame.Width <= 0)
        {
            throw JpegInvalidDataException.Create("Frame width must be greater than zero.");
        }

        if (frame.Height < 0)
        {
            throw JpegInvalidDataException.Create("Frame height cannot be negative.");
        }

        if (frame.Components == null)
        {
            throw JpegInvalidDataException.Create("Frame component list cannot be null.");
        }

        int componentCount = GetComponentCount(frame);
        if (componentCount <= 0)
        {
            throw JpegInvalidDataException.Create("Frame must contain at least one component.");
        }

        if (frame.ComponentCount != 0 && frame.ComponentCount != componentCount)
        {
            throw JpegInvalidDataException.Create("Frame component count must match the component list.");
        }

        int index = 0;
        while (index < componentCount)
        {
            ValidateComponent(frame.Components[index]);
            index = index + 1;
        }
    }

    internal void ValidateComponent(JpegFrameComponent component)
    {
        if (component == null)
        {
            throw JpegInvalidDataException.Create("Frame component cannot be null.");
        }

        if (component.HorizontalSamplingFactor < 1 || component.HorizontalSamplingFactor > 4)
        {
            throw JpegInvalidDataException.Create("Horizontal sampling factor must be between 1 and 4.");
        }

        if (component.VerticalSamplingFactor < 1 || component.VerticalSamplingFactor > 4)
        {
            throw JpegInvalidDataException.Create("Vertical sampling factor must be between 1 and 4.");
        }
    }
}
