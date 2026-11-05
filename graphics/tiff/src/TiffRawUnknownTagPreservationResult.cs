namespace tiff;

internal class TiffRawUnknownTagPreservationResult
{
    internal bool Success;
    internal TiffTagValue[] Values;
    internal TiffDiagnostics Diagnostics;

    internal TiffRawUnknownTagPreservationResult()
    {
        Success = true;
        Values = new TiffTagValue[0];
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffRawUnknownTagPreservationResult Create(TiffTagValue[] values, TiffDiagnostics diagnostics)
    {
        TiffRawUnknownTagPreservationResult value = new TiffRawUnknownTagPreservationResult();
        value.Initialize(values, diagnostics);
        return value;
    }

    internal void Initialize(TiffTagValue[] values, TiffDiagnostics diagnostics)
    {
        Values = CopyValues(values);
        Diagnostics = diagnostics;
        Success = !Diagnostics.HasErrors();
    }

    internal int ValueCount()
    {
        return Values.Length;
    }

    internal static TiffTagValue[] CopyValues(TiffTagValue[] values)
    {
        if (values == null)
        {
            return new TiffTagValue[0];
        }

        TiffTagValue[] copy = new TiffTagValue[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                copy[index] = new TiffTagValue();
            }
            else
            {
                copy[index] = values[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
