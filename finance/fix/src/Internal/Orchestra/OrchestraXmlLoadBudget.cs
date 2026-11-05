namespace fix;

internal class OrchestraXmlLoadBudget
{
    internal int RemainingBytes;

    internal OrchestraXmlLoadBudget(int maximumBytes)
    {
        RemainingBytes = maximumBytes;
    }
}
