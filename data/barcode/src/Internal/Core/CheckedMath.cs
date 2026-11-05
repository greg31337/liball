namespace barcode;

internal static class CheckedMath
{
    internal static int Add(int left, int right)
    {
        long result = (long)left + (long)right;
        if (result > int.MaxValue || result < int.MinValue)
        {
            throw new BarcodeValidationException("Integer addition exceeds the supported range.");
        }

        return (int)result;
    }

    internal static int Subtract(int left, int right)
    {
        long result = (long)left - (long)right;
        if (result > int.MaxValue || result < int.MinValue)
        {
            throw new BarcodeValidationException("Integer subtraction exceeds the supported range.");
        }

        return (int)result;
    }

    internal static int Multiply(int left, int right)
    {
        long result = (long)left * (long)right;
        if (result > int.MaxValue || result < int.MinValue)
        {
            throw new BarcodeValidationException("Integer multiplication exceeds the supported range.");
        }

        return (int)result;
    }

    internal static int ValidateDimension(int value)
    {
        if (value <= 0)
        {
            throw new BarcodeValidationException("Dimension must be greater than zero.");
        }

        return value;
    }

    internal static int ValidateCapacity(int capacity, int maximumCapacity)
    {
        if (capacity < 0)
        {
            throw new BarcodeValidationException("Capacity must not be negative.");
        }

        if (maximumCapacity < 0)
        {
            throw new BarcodeValidationException("Maximum capacity must not be negative.");
        }

        if (capacity > maximumCapacity)
        {
            throw new BarcodeValidationException("Capacity exceeds the configured maximum.");
        }

        return capacity;
    }

    internal static int ValidateIndex(int index, int length)
    {
        if (length < 0)
        {
            throw new BarcodeValidationException("Length must not be negative.");
        }

        if (index < 0 || index >= length)
        {
            throw new BarcodeValidationException("Index is outside the available range.");
        }

        return index;
    }

    internal static int ValidateRange(int offset, int count, int length)
    {
        if (length < 0)
        {
            throw new BarcodeValidationException("Length must not be negative.");
        }

        if (offset < 0)
        {
            throw new BarcodeValidationException("Offset must not be negative.");
        }

        if (count < 0)
        {
            throw new BarcodeValidationException("Count must not be negative.");
        }

        if (offset > length || count > length - offset)
        {
            throw new BarcodeValidationException("Range is outside the available length.");
        }

        return Add(offset, count);
    }

    internal static int CalculateAllocationLength(int width, int height, int bytesPerElement, int maximumLength)
    {
        ValidateDimension(width);
        ValidateDimension(height);
        ValidateDimension(bytesPerElement);
        int area = Multiply(width, height);
        int length = Multiply(area, bytesPerElement);
        return ValidateCapacity(length, maximumLength);
    }
}
