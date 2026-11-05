using System.Globalization;
using System.Text.Json;
using xlsx;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// Keep monetary input as decimal until writing the spreadsheet's numeric cells.
string input = File.ReadAllText(SampleFiles.InputPath("expenses.json"));
var expenses = JsonSerializer.Deserialize<Expense[]>(
    input, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Missing expenses.");
if (expenses.Length == 0)
{
    throw new InvalidOperationException("At least one expense is required.");
}

var workbooks = new XlsxWorkbookOperations();
var cells = new XlsxWorksheetPartOperations();
var workbook = workbooks.Create();
workbook.WorkbookPart.Sheets[0].Name = "Expenses";
var sheet = new XlsxWorksheetPart
{
    PartPath = XlsxPackagePaths.Worksheet1
};

Require(cells.SetTextValue(sheet, "A1", "Expense").Validation);
Require(cells.SetTextValue(sheet, "B1", "Amount").Validation);
for (int i = 0; i < expenses.Length; i++)
{
    Require(cells.SetTextValue(sheet, $"A{i + 2}", expenses[i].Category).Validation);
    Require(cells.SetNumberValue(sheet, $"B{i + 2}", (double)expenses[i].Amount).Validation);
}

// Derive the formula range from the input rows, so adding an expense updates the total.
int totalRow = expenses.Length + 2;
string totalReference = $"B{totalRow}";
string formula = $"SUM(B2:B{totalRow - 1})";
double expectedTotal = (double)expenses.Sum(expense => expense.Amount);
Require(cells.SetTextValue(sheet, $"A{totalRow}", "Total").Validation);
Require(cells.SetFormulaCachedValue(sheet, totalReference, formula, "", "").Validation);
var calculation = cells.CalculateCellFormula(sheet, totalReference);
Require(calculation.Validation);
if (calculation.Value is null || Math.Abs(calculation.Value.Number - expectedTotal) > 0.000001)
{
    throw new InvalidOperationException("The expense formula returned an unexpected total.");
}

var worksheetBytes = new XlsxWorksheetPartWriter().WriteBytes(sheet);
Require(worksheetBytes.Validation);
new XlsxUnknownPartStoreOperations().AddPart(
    workbook.UnknownParts,
    XlsxPackagePaths.Worksheet1,
    XlsxContentTypes.Worksheet,
    worksheetBytes.Bytes);
var saved = workbooks.SaveBytes(workbook);
Require(saved.Validation);

// Print the cells read from the saved package, including the formula's cached value.
var options = new XlsxReaderOptions();
options.SelectedSheets.Add("Expenses");
var reopened = workbooks.OpenBytesWithOptions(saved.Bytes, options);
Require(reopened.Validation);
var loaded = (reopened.Workbook
    ?? throw new InvalidOperationException("No workbook was read.")).Worksheets.Single();
Console.WriteLine("Expenses workbook, read back from XLSX");
for (int row = 2; row <= totalRow; row++)
{
    var label = cells.GetCell(loaded, $"A{row}") ?? throw new InvalidOperationException("Missing label.");
    var amount = cells.GetCell(loaded, $"B{row}") ?? throw new InvalidOperationException("Missing amount.");
    Console.WriteLine($"{label.Value,-18} {double.Parse(amount.Value, CultureInfo.InvariantCulture),8:F2}");
}

var totalCell = cells.GetCell(loaded, totalReference)
    ?? throw new InvalidOperationException("Missing total.");
double storedTotal = double.Parse(totalCell.Value, CultureInfo.InvariantCulture);
if (totalCell.Formula != formula || Math.Abs(storedTotal - expectedTotal) > 0.000001)
{
    throw new InvalidOperationException("Formula or cached total changed during the XLSX round trip.");
}

Console.WriteLine($"Formula in {totalReference}: {totalCell.Formula}");
string directory = Path.GetFullPath(args.Length == 0 ? Path.Combine("artifacts", "Spreadsheet") : args[0]);
Directory.CreateDirectory(directory);
string path = Path.Combine(directory, "expenses.xlsx");
File.WriteAllBytes(path, saved.Bytes);
Console.WriteLine($"Saved: {path} ({saved.Bytes.Length:N0} bytes)");

static void Require(XlsxValidationResult result)
{
    if (!result.IsValid)
    {
        throw new InvalidOperationException(
            string.Join(Environment.NewLine, result.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));
    }
}

internal sealed record Expense(string Category, decimal Amount);
