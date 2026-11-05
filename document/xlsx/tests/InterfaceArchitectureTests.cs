using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class InterfaceArchitectureTests
{
    [Test]
    public void ProductionClasses_DeclareAtMostOneInstanceConstructor()
    {
        System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.DeclaredOnly
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic;

        foreach (System.Type type in typeof(XlsxWorkbook).Assembly.GetTypes())
        {
            if (type.IsClass)
            {
                int constructorCount = type.GetConstructors(flags).Length;
                Assert.That(constructorCount, Is.LessThanOrEqualTo(1), type.FullName);
            }
        }
    }

    [Test]
    public void CellStoreContract_DenseAndSparseImplementationsBehaveConsistently()
    {
        AssertCellStoreContract(new XlsxSparseCellStore());
        AssertCellStoreContract(new XlsxDenseCellStore());
    }

    [Test]
    public void WorkbookValidation_CanUseAnInjectedImplementation()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorkbookValidationOptions options = new XlsxWorkbookValidationOptions();
        RecordingWorkbookValidator validator = new RecordingWorkbookValidator();

        XlsxValidationResult result = workbook.ValidateUsingWithOptions(validator, options);

        Assert.That(result, Is.SameAs(validator.Result));
        Assert.That(validator.Workbook, Is.SameAs(workbook));
        Assert.That(validator.Options, Is.SameAs(options));
        Assert.That(new XlsxWorkbookValidator(), Is.InstanceOf<IXlsxWorkbookValidator>());
    }

    [Test]
    public void WorkbookRepair_CanUseAnInjectedImplementation()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorkbookRepairOptions options = new XlsxWorkbookRepairOptions();
        RecordingWorkbookRepairer repairer = new RecordingWorkbookRepairer();

        XlsxWorkbookRepairResult result = workbook.RepairUsingWithOptions(repairer, options);

        Assert.That(result, Is.SameAs(repairer.Result));
        Assert.That(repairer.Workbook, Is.SameAs(workbook));
        Assert.That(repairer.Options, Is.SameAs(options));
        Assert.That(new XlsxWorkbookRepairer(), Is.InstanceOf<IXlsxWorkbookRepairer>());
    }

    [Test]
    public void WorkbookServiceDispatch_ReportsNullImplementations()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();

        XlsxValidationResult? validation = workbook.ValidateUsing(null!);
        XlsxWorkbookRepairResult? repair = workbook.RepairUsing(null!);

        Assert.That(validation.IsValid, Is.False);
        Assert.That(validation.Diagnostics[0].Code, Is.EqualTo("workbook.validatorNull"));
        Assert.That(repair.IsValid, Is.False);
        Assert.That(repair.Validation.Diagnostics[0].Code, Is.EqualTo("workbook.repairerNull"));
    }

    static void AssertCellStoreContract(IXlsxCellStore store)
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.Cells.Add(CreateCell("A1", "first"));
        row.Cells.Add(CreateCell("B2", "second"));
        worksheet.Rows.Add(row);

        XlsxCellStoreResult load = store.LoadWorksheetPart(worksheet);
        XlsxCellStoreResult remove = store.RemoveCellByReference("A1");
        XlsxCellStoreResult create = store.GetOrCreateCell(3, 3);

        Assert.That(load.IsValid, Is.True);
        Assert.That(remove.Removed, Is.True);
        Assert.That(create.IsValid, Is.True);
        Assert.That(store.GetCellByReference("B2")!.Value, Is.EqualTo("second"));
        Assert.That(store.ContainsCellByReference("C3"), Is.True);
        Assert.That(store.GetCellCount(), Is.EqualTo(2));
        Assert.That(store.GetMinRow(), Is.EqualTo(2));
        Assert.That(store.GetMaxRow(), Is.EqualTo(3));
        Assert.That(store.GetMinColumn(), Is.EqualTo(2));
        Assert.That(store.GetMaxColumn(), Is.EqualTo(3));
    }

    static XlsxWorksheetCell CreateCell(string reference, string value)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.Value = value;
        return cell;
    }
}

class RecordingWorkbookValidator : IXlsxWorkbookValidator
{
    public XlsxWorkbook? Workbook;
    public XlsxWorkbookValidationOptions? Options;
    public XlsxValidationResult Result;

    public RecordingWorkbookValidator()
    {
        Result = new XlsxValidationResult();
    }

        public XlsxValidationResult ValidateWorkbookWithOptions(XlsxWorkbook workbook, XlsxWorkbookValidationOptions options)
    {
        Workbook = workbook;
        Options = options;
        return Result;
    }
}

class RecordingWorkbookRepairer : IXlsxWorkbookRepairer
{
    public XlsxWorkbook? Workbook;
    public XlsxWorkbookRepairOptions? Options;
    public XlsxWorkbookRepairResult Result;

    public RecordingWorkbookRepairer()
    {
        Result = new XlsxWorkbookRepairResult();
    }

        public XlsxWorkbookRepairResult RepairWorkbookWithOptions(XlsxWorkbook workbook, XlsxWorkbookRepairOptions options)
    {
        Workbook = workbook;
        Options = options;
        Result.Workbook = workbook;
        return Result;
    }
}
