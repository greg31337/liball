namespace barcode;

internal class ResourceBudget
{
    internal int MaximumWork;
    internal int WorkUsed;
    internal int MaximumBytes;
    internal int BytesReserved;
    internal int MaximumModules;
    internal int ModulesReserved;

    internal ResourceBudget(int maximumWork, int maximumBytes, int maximumModules)
    {
        MaximumWork = CheckedMath.ValidateCapacity(maximumWork, int.MaxValue);
        MaximumBytes = CheckedMath.ValidateCapacity(maximumBytes, int.MaxValue);
        MaximumModules = CheckedMath.ValidateCapacity(maximumModules, int.MaxValue);
        WorkUsed = 0;
        BytesReserved = 0;
        ModulesReserved = 0;
    }

    internal int ConsumeWork(int amount)
    {
        if (amount < 0)
        {
            throw new BarcodeValidationException("Work amount must not be negative.");
        }

        int total = CheckedMath.Add(WorkUsed, amount);
        WorkUsed = CheckedMath.ValidateCapacity(total, MaximumWork);
        return WorkUsed;
    }

    internal int ReserveBytes(int amount)
    {
        if (amount < 0)
        {
            throw new BarcodeValidationException("Byte reservation must not be negative.");
        }

        int total = CheckedMath.Add(BytesReserved, amount);
        BytesReserved = CheckedMath.ValidateCapacity(total, MaximumBytes);
        return BytesReserved;
    }

    internal int ReserveModules(int amount)
    {
        if (amount < 0)
        {
            throw new BarcodeValidationException("Module reservation must not be negative.");
        }

        int total = CheckedMath.Add(ModulesReserved, amount);
        ModulesReserved = CheckedMath.ValidateCapacity(total, MaximumModules);
        return ModulesReserved;
    }
}
