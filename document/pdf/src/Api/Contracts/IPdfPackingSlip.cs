namespace pdf;

/// <summary>Defines operations exposed by <see cref="PdfPackingSlip"/>.</summary>
public interface IPdfPackingSlip
{
    /// <summary>Sets the sender block.</summary>
    /// <param name="sender">The sender text.</param>
    void SetSender(string sender);
    /// <summary>Sets the recipient block.</summary>
    /// <param name="recipient">The recipient text.</param>
    void SetRecipient(string recipient);
    /// <summary>Sets shipment metadata.</summary>
    /// <param name="dateText">The shipment date.</param>
    /// <param name="carrier">The carrier.</param>
    /// <param name="trackingNumber">The tracking number.</param>
    void SetShipmentDetails(string dateText, string carrier, string trackingNumber);
    /// <summary>Adds a shipped item.</summary>
    /// <param name="item">The item to add.</param>
    void AddItem(PdfPackingSlipItem item);
    /// <summary>Sets optional notes.</summary>
    /// <param name="notes">The notes text.</param>
    void SetNotes(string notes);
    /// <summary>Builds the packing slip.</summary>
    /// <returns>The built document.</returns>
    PdfDocument Build();
}
