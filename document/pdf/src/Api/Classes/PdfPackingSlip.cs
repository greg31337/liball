using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>Builds a packing slip with shipment details and item quantities.</summary>
public class PdfPackingSlip : IPdfPackingSlip
{
    internal string SlipNumber = string.Empty;
    internal string Sender = string.Empty;
    internal string Recipient = string.Empty;
    internal string DateText = string.Empty;
    internal string Carrier = string.Empty;
    internal string TrackingNumber = string.Empty;
    internal string Notes = string.Empty;
    internal List<PdfPackingSlipItem> Items = new List<PdfPackingSlipItem>();
    internal PdfDocument Document = new PdfDocument();
    internal bool Built;

    /// <summary>Initializes a packing slip.</summary>
    /// <param name="slipNumber">The packing-slip number.</param>
    public PdfPackingSlip(string slipNumber)
    {
        ValidateText(slipNumber, "slipNumber");
        SlipNumber = slipNumber;
        Items = new List<PdfPackingSlipItem>();
        Document = new PdfDocument();
        Built = false;
    }

    /// <summary>Sets the sender block.</summary>
    /// <param name="sender">The sender text.</param>
    public void SetSender(string sender)
    {
        EnsureNotBuilt();
        ValidateText(sender, "sender");
        Sender = sender;
    }

    /// <summary>Sets the recipient block.</summary>
    /// <param name="recipient">The recipient text.</param>
    public void SetRecipient(string recipient)
    {
        EnsureNotBuilt();
        ValidateText(recipient, "recipient");
        Recipient = recipient;
    }

    /// <summary>Sets shipment metadata.</summary>
    /// <param name="dateText">The shipment date.</param>
    /// <param name="carrier">The carrier.</param>
    /// <param name="trackingNumber">The tracking number.</param>
    public void SetShipmentDetails(string dateText, string carrier, string trackingNumber)
    {
        EnsureNotBuilt();
        ValidateText(dateText, "dateText");
        ValidateText(carrier, "carrier");
        ValidateText(trackingNumber, "trackingNumber");
        DateText = dateText;
        Carrier = carrier;
        TrackingNumber = trackingNumber;
    }

    /// <summary>Adds a shipped item.</summary>
    /// <param name="item">The item to add.</param>
    public void AddItem(PdfPackingSlipItem item)
    {
        EnsureNotBuilt();
        if (item == null) { throw new ArgumentNullException("item"); }
        Items.Add(item);
    }

    /// <summary>Sets optional notes.</summary>
    /// <param name="notes">The notes text.</param>
    public void SetNotes(string notes)
    {
        EnsureNotBuilt();
        ValidateText(notes, "notes");
        Notes = notes;
    }

    /// <summary>Builds the packing slip.</summary>
    /// <returns>The built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            PdfLayoutDocument layout = new PdfLayoutDocument(Document, new PdfPageSizeFactory().Letter(), new PdfPageMargins(72, 72, 72, 72));
            PdfParagraphStyle titleStyle = PdfParagraphStyle.Create(PdfTextStyle.WithFont(20, PdfFont.Bold), 24, 0, 0, 0, PdfTextAlignment.Right);
            PdfParagraphStyle bodyStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(10, PdfFont.Regular), 14, 0, 0, 0);
            PdfParagraphStyle boldStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(10, PdfFont.Bold), 14, 0, 0, 0);
            layout.AddTextBlock(new PdfTextBlock("PACKING SLIP " + SlipNumber, titleStyle));
            if (Sender.Length > 0)
            {
                layout.AddTextBlock(new PdfTextBlock("From: " + Sender, bodyStyle));
            }

            if (Recipient.Length > 0)
            {
                layout.AddTextBlock(new PdfTextBlock("Ship to: " + Recipient, bodyStyle));
            }

            if (DateText.Length > 0 || Carrier.Length > 0)
            {
                layout.AddTextBlock(new PdfTextBlock("Ship date: " + DateText + "   Carrier: " + Carrier, bodyStyle));
            }

            if (TrackingNumber.Length > 0)
            {
                layout.AddTextBlock(new PdfTextBlock("Tracking: " + TrackingNumber, bodyStyle));
            }

            layout.AddBlock(PdfBlock.WithSize(0, 14));
            PdfTableBlock table = new PdfTableBlock(new double[] { 90, 310, 68 });
            PdfTableRow header = table.AddHeaderRow();
            header.AddTextBlock(new PdfTextBlock("SKU", boldStyle));
            header.AddTextBlock(new PdfTextBlock("Description", boldStyle));
            header.AddTextBlock(new PdfTextBlock("Quantity", boldStyle));
            for (int index = 0; index < Items.Count; index++)
            {
                PdfTableRow row = table.AddRow();
                row.AddTextBlock(new PdfTextBlock(Items[index].Sku, bodyStyle));
                row.AddTextBlock(new PdfTextBlock(Items[index].Description, bodyStyle));
                row.AddTextBlock(new PdfTextBlock(Items[index].Quantity.ToString(), bodyStyle));
            }

            layout.AddTableBlock(table);
            if (Notes.Length > 0)
            {
                layout.AddBlock(PdfBlock.WithSize(0, 14));
                layout.AddTextBlock(new PdfTextBlock("Notes: " + Notes, bodyStyle));
            }

            layout.Render();
            Built = true;
        }

        return Document;
    }

    internal void ValidateText(string text, string name)
    {
        if (text == null) { throw new ArgumentNullException(name); }
    }

    internal void EnsureNotBuilt()
    {
        if (Built) { throw new InvalidOperationException("Packing slip has already been built."); }
    }
}
