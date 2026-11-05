namespace barcode;

internal class ModuleMatrix
{
    internal int Width;
    internal int Height;
    internal BitRow[] Rows;

    internal ModuleMatrix(int width, int height)
    {
        Width = CheckedMath.ValidateDimension(width);
        Height = CheckedMath.ValidateDimension(height);
        CheckedMath.CalculateAllocationLength(width, height, 1, int.MaxValue);
        Rows = new BitRow[height];
        int row = 0;
        while (row < height)
        {
            Rows[row] = new BitRow(width);
            row = row + 1;
        }
    }

    internal bool GetModule(int x, int y)
    {
        CheckedMath.ValidateIndex(x, Width);
        CheckedMath.ValidateIndex(y, Height);
        return Rows[y].ReadBitUnchecked(x);
    }

    internal void SetModule(int x, int y, bool value)
    {
        CheckedMath.ValidateIndex(x, Width);
        CheckedMath.ValidateIndex(y, Height);
        Rows[y].WriteBitUnchecked(x, value);
    }

    internal void ClearModules()
    {
        int row = 0;
        while (row < Height)
        {
            Rows[row].ClearBits();
            row = row + 1;
        }
    }

    internal void SetAllModules(bool value)
    {
        int row = 0;
        while (row < Height)
        {
            Rows[row].SetAllBits(value);
            row = row + 1;
        }
    }

    internal void LoadModules(bool[] modules)
    {
        if (modules == null)
        {
            throw new BarcodeValidationException("Module source must not be null.");
        }

        int expectedLength = CheckedMath.Multiply(Width, Height);
        if (modules.Length != expectedLength)
        {
            throw new BarcodeValidationException("Module source length does not match the matrix dimensions.");
        }

        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                int index = CheckedMath.Add(CheckedMath.Multiply(y, Width), x);
                Rows[y].WriteBitUnchecked(x, modules[index]);
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal bool[] CopyModules()
    {
        int length = CheckedMath.Multiply(Width, Height);
        bool[] modules = new bool[length];
        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                int index = CheckedMath.Add(CheckedMath.Multiply(y, Width), x);
                modules[index] = Rows[y].ReadBitUnchecked(x);
                x = x + 1;
            }

            y = y + 1;
        }

        return modules;
    }

    internal bool[] CopyRow(int y)
    {
        CheckedMath.ValidateIndex(y, Height);
        return Rows[y].CopyBits();
    }

    internal void SetRow(int y, bool[] modules)
    {
        CheckedMath.ValidateIndex(y, Height);
        Rows[y].LoadBits(modules);
    }

    internal bool[] CopyColumn(int x)
    {
        CheckedMath.ValidateIndex(x, Width);
        bool[] modules = new bool[Height];
        int y = 0;
        while (y < Height)
        {
            modules[y] = Rows[y].ReadBitUnchecked(x);
            y = y + 1;
        }

        return modules;
    }

    internal void SetColumn(int x, bool[] modules)
    {
        CheckedMath.ValidateIndex(x, Width);
        if (modules == null)
        {
            throw new BarcodeValidationException("Column source must not be null.");
        }

        if (modules.Length != Height)
        {
            throw new BarcodeValidationException("Column source length does not match the matrix height.");
        }

        int y = 0;
        while (y < Height)
        {
            Rows[y].WriteBitUnchecked(x, modules[y]);
            y = y + 1;
        }
    }

    internal ModuleMatrix Crop(int left, int top, int width, int height)
    {
        CheckedMath.ValidateDimension(width);
        CheckedMath.ValidateDimension(height);
        CheckedMath.ValidateRange(left, width, Width);
        CheckedMath.ValidateRange(top, height, Height);
        ModuleMatrix result = new ModuleMatrix(width, height);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                bool value = Rows[top + y].ReadBitUnchecked(left + x);
                result.Rows[y].WriteBitUnchecked(x, value);
                x = x + 1;
            }

            y = y + 1;
        }

        return result;
    }

    internal ModuleMatrix RotateClockwise90()
    {
        ModuleMatrix result = new ModuleMatrix(Height, Width);
        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                bool value = Rows[y].ReadBitUnchecked(x);
                result.Rows[x].WriteBitUnchecked(Height - 1 - y, value);
                x = x + 1;
            }

            y = y + 1;
        }

        return result;
    }

    internal ModuleMatrix Rotate180()
    {
        ModuleMatrix result = new ModuleMatrix(Width, Height);
        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                bool value = Rows[y].ReadBitUnchecked(x);
                result.Rows[Height - 1 - y].WriteBitUnchecked(Width - 1 - x, value);
                x = x + 1;
            }

            y = y + 1;
        }

        return result;
    }

    internal ModuleMatrix RotateClockwise270()
    {
        ModuleMatrix result = new ModuleMatrix(Height, Width);
        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                bool value = Rows[y].ReadBitUnchecked(x);
                result.Rows[Width - 1 - x].WriteBitUnchecked(y, value);
                x = x + 1;
            }

            y = y + 1;
        }

        return result;
    }

    internal ModuleMatrix ReflectHorizontal()
    {
        ModuleMatrix result = new ModuleMatrix(Width, Height);
        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                bool value = Rows[y].ReadBitUnchecked(x);
                result.Rows[y].WriteBitUnchecked(Width - 1 - x, value);
                x = x + 1;
            }

            y = y + 1;
        }

        return result;
    }

    internal ModuleMatrix ReflectVertical()
    {
        ModuleMatrix result = new ModuleMatrix(Width, Height);
        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                bool value = Rows[y].ReadBitUnchecked(x);
                result.Rows[Height - 1 - y].WriteBitUnchecked(x, value);
                x = x + 1;
            }

            y = y + 1;
        }

        return result;
    }

    internal ModuleMatrix AddBorder(int left, int top, int right, int bottom, bool dark)
    {
        if (left < 0 || top < 0 || right < 0 || bottom < 0)
        {
            throw new BarcodeValidationException("Border dimensions must not be negative.");
        }

        int outputWidth = CheckedMath.Add(CheckedMath.Add(Width, left), right);
        int outputHeight = CheckedMath.Add(CheckedMath.Add(Height, top), bottom);
        ModuleMatrix result = new ModuleMatrix(outputWidth, outputHeight);
        if (dark)
        {
            result.SetAllModules(true);
        }

        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                bool value = Rows[y].ReadBitUnchecked(x);
                result.Rows[top + y].WriteBitUnchecked(left + x, value);
                x = x + 1;
            }

            y = y + 1;
        }

        return result;
    }

    internal int[]? GetDarkBounds()
    {
        int minimumX = Width;
        int minimumY = Height;
        int maximumX = -1;
        int maximumY = -1;
        int y = 0;
        while (y < Height)
        {
            int x = 0;
            while (x < Width)
            {
                if (Rows[y].ReadBitUnchecked(x))
                {
                    if (x < minimumX)
                    {
                        minimumX = x;
                    }

                    if (x > maximumX)
                    {
                        maximumX = x;
                    }

                    if (y < minimumY)
                    {
                        minimumY = y;
                    }

                    if (y > maximumY)
                    {
                        maximumY = y;
                    }
                }

                x = x + 1;
            }

            y = y + 1;
        }

        if (maximumX < 0)
        {
            return null;
        }

        int boundsWidth = CheckedMath.Add(CheckedMath.Subtract(maximumX, minimumX), 1);
        int boundsHeight = CheckedMath.Add(CheckedMath.Subtract(maximumY, minimumY), 1);
        return new int[] { minimumX, minimumY, boundsWidth, boundsHeight };
    }
}
