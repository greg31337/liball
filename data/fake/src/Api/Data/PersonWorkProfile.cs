namespace fake;

/// <summary>Contains a correlated synthetic work profile selected from locale data.</summary>
/// <remarks>Salary bands are nonnumeric labels and the record makes no employment claim.</remarks>
public class PersonWorkProfile
{
    /// <summary>The occupational title.</summary>
    public string Title = string.Empty;

    /// <summary>The occupational descriptor.</summary>
    public string Descriptor = string.Empty;

    /// <summary>The broad occupational area.</summary>
    public string Area = string.Empty;

    /// <summary>The employment arrangement type.</summary>
    public string Type = string.Empty;

    /// <summary>The seniority level.</summary>
    public string Seniority = string.Empty;

    /// <summary>The organizational department.</summary>
    public string Department = string.Empty;

    /// <summary>The employment-status label.</summary>
    public string Status = string.Empty;

    /// <summary>The locale-provided salary-band label.</summary>
    public string SalaryBand = string.Empty;

    internal PersonWorkProfile()
    {
    }
}
