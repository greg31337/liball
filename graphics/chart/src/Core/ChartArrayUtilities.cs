namespace chart;

internal static class ChartArrayUtilities
{
    internal static int GrowthCapacity(int currentLength, int requiredCapacity)
    {
        if (currentLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("currentLength");
        }

        if (requiredCapacity < 0)
        {
            throw new System.ArgumentOutOfRangeException("requiredCapacity");
        }

        if (requiredCapacity <= currentLength)
        {
            return currentLength;
        }

        if (currentLength == 0)
        {
            return requiredCapacity;
        }

        int newCapacity = currentLength;
        while (newCapacity < requiredCapacity)
        {
            if (newCapacity > int.MaxValue / 2)
            {
                return requiredCapacity;
            }

            newCapacity = newCapacity * 2;
        }

        return newCapacity;
    }
}
