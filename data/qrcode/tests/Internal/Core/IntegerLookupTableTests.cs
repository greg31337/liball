using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class IntegerLookupTableTests
{
    [Test]
    public void InitializationDefensivelyCopiesEverySourceRow()
    {
        int[][] source = new int[][]
        {
            new int[] { 1, 2 },
            new int[] { 3, 4, 5 }
        };
        IntegerLookupTable table = new IntegerLookupTable(source);

        source[0][0] = 99;
        source[1] = new int[] { 88 };

        Assert.Multiple(() =>
        {
            Assert.That(table.GetRowCount(), Is.EqualTo(2));
            Assert.That(table.GetColumnCount(0), Is.EqualTo(2));
            Assert.That(table.GetColumnCount(1), Is.EqualTo(3));
            Assert.That(table.GetValue(0, 0), Is.EqualTo(1));
            Assert.That(table.CreateRowCopy(1), Is.EqualTo(new int[] { 3, 4, 5 }));
        });
    }

    [Test]
    public void ReturnedRowsAndSpanCopiesCannotMutateStoredValues()
    {
        IntegerLookupTable table = new IntegerLookupTable(new int[][]
        {
            new int[] { 6, 30, 58 }
        });
        int[] returned = table.CreateRowCopy(0);
        int[] destination = new int[] { 0, 0, 0, 77 };

        returned[0] = 99;
        table.CopyRow(0, destination);
        destination[1] = 88;

        Assert.Multiple(() =>
        {
            Assert.That(table.GetValue(0, 0), Is.EqualTo(6));
            Assert.That(table.GetValue(0, 1), Is.EqualTo(30));
            Assert.That(table.CreateRowCopy(0), Is.EqualTo(new int[] { 6, 30, 58 }));
            Assert.That(destination[3], Is.EqualTo(77));
        });
    }

    [Test]
    public void InvalidRowsColumnsAndDestinationsAreRejected()
    {
        IntegerLookupTable table = new IntegerLookupTable(new int[][]
        {
            new int[] { 1, 2 }
        });

        QrInputException row = Assert.Throws<QrInputException>(() => table.GetColumnCount(1))!;
        QrInputException column = Assert.Throws<QrInputException>(() => table.GetValue(0, 2))!;
        QrInputException destination = Assert.Throws<QrInputException>(() =>
            table.CopyRow(0, new int[1]))!;

        Assert.Multiple(() =>
        {
            Assert.That(row.InputName, Is.EqualTo("rowIndex"));
            Assert.That(column.InputName, Is.EqualTo("columnIndex"));
            Assert.That(destination.InputName, Is.EqualTo("destination"));
        });
    }

    [Test]
    public void AlignmentLookupsRemainStableDuringConcurrentCallerMutation()
    {
        int failureCount = 0;

        Parallel.For(0, 512, iteration =>
        {
            int[] positions = AlignmentPositionTable.GetPositions(40);
            int[] columns = RectangularMicroAlignmentTable.GetCenterColumns(139);
            if (positions[0] != 6 || positions[6] != 170 || columns[0] != 27 || columns[3] != 111)
            {
                Interlocked.Increment(ref failureCount);
            }

            positions[0] = iteration;
            columns[0] = iteration;
        });

        Assert.Multiple(() =>
        {
            Assert.That(failureCount, Is.EqualTo(0));
            Assert.That(AlignmentPositionTable.GetPositions(40)[0], Is.EqualTo(6));
            Assert.That(RectangularMicroAlignmentTable.GetCenterColumns(139)[0], Is.EqualTo(27));
        });
    }
}
