using System;

namespace pdf;

internal static class SampleInvoice
{
    internal static void Generate(string outputPath)
    {
        if (outputPath == null)
        {
            throw new ArgumentNullException("outputPath");
        }

        PdfDocument document = BuildDocument();
        document.Save(outputPath);
    }

    internal static PdfDocument BuildDocument()
    {
        PdfInvoice invoice = new PdfInvoice();
        PdfImage logo = invoice.Document.AddImage(24, 24, CreateLogoImage(24, 24));
        PdfParty seller = CreateSeller();
        PdfParty buyer = CreateBuyer();
        PdfParty shipping = CreateShipping();

        invoice.SetLogo(logo, 48, 24);
        invoice.SetSeller(seller);
        invoice.SetBuyer(buyer);
        invoice.SetShipping(shipping);
        invoice.SetInvoiceDetails("INV-2026-001", "2026-07-31", "2026-08-30");
        invoice.AddLine(CreateLine("Implementation package", 1, 1200, 0, 0.08));
        invoice.AddLine(CreateLine("Support retainer", 3, 150, 50, 0.08));
        invoice.AddLine(CreateLine("Training session", 2, 275, 0, 0.08));
        invoice.SetPaymentTerms("Payment due within 30 days by ACH or wire transfer.");
        invoice.SetRemittanceInstructions("Remit to Acme Supply Co., account ending 4242, routing ending 0101.");
        invoice.SetNotes("Thank you for your business. Please reference the invoice number with payment.");

        return invoice.Build();
    }

    internal static PdfParty CreateSeller()
    {
        PdfParty party = new PdfParty("Acme Supply Co.");
        party.AddLine("100 Market Street");
        party.AddLine("San Francisco, CA 94105");
        party.SetTaxIdentifier("US-TAX-12345");
        party.SetEmail("billing@acme.example");
        return party;
    }

    internal static PdfParty CreateBuyer()
    {
        PdfParty party = new PdfParty("Contoso Retail");
        party.AddLine("25 Commerce Avenue");
        party.AddLine("Seattle, WA 98101");
        party.SetTaxIdentifier("US-TAX-77889");
        party.SetEmail("ap@contoso.example");
        return party;
    }

    internal static PdfParty CreateShipping()
    {
        PdfParty party = new PdfParty("Contoso Retail Warehouse");
        party.AddLine("Dock 4");
        party.AddLine("500 Harbor Way");
        party.AddLine("Seattle, WA 98134");
        return party;
    }

    internal static PdfInvoiceLine CreateLine(string description, double quantity, double unitPrice, double discount, double taxRate)
    {
        PdfInvoiceLine line = new PdfInvoiceLine(description, quantity, unitPrice);
        line.SetDiscount(discount);
        line.SetTaxRate(taxRate);
        return line;
    }

    internal static byte[] CreateLogoImage(int width, int height)
    {
        byte[] pixels = new byte[width * height * 3];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = (y * width + x) * 3;
                bool inBand = x > y - 4 && x < y + 8;
                bool inCorner = x < width / 3 && y < height / 3;

                if (inBand || inCorner)
                {
                    pixels[offset] = 34;
                    pixels[offset + 1] = 92;
                    pixels[offset + 2] = 145;
                }
                else
                {
                    pixels[offset] = 232;
                    pixels[offset + 1] = 239;
                    pixels[offset + 2] = 244;
                }
            }
        }

        return pixels;
    }
}
