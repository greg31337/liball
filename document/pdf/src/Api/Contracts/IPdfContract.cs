namespace pdf;

/// <summary>Defines operations exposed by <see cref="PdfContract"/>.</summary>
public interface IPdfContract
{
    /// <summary>Sets the two contracting parties.</summary>
    /// <param name="firstParty">The first party.</param>
    /// <param name="secondParty">The second party.</param>
    void SetParties(string firstParty, string secondParty);
    /// <summary>Sets the displayed effective date.</summary>
    /// <param name="dateText">The effective date.</param>
    void SetEffectiveDate(string dateText);
    /// <summary>Adds a numbered clause.</summary>
    /// <param name="clause">The clause to add.</param>
    void AddClause(PdfContractClause clause);
    /// <summary>Adds a signature line.</summary>
    /// <param name="signature">The signature line to add.</param>
    void AddSignature(PdfContractSignature signature);
    /// <summary>Builds the contract document.</summary>
    /// <returns>The built document.</returns>
    PdfDocument Build();
}
