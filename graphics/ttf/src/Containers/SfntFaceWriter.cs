namespace ttf;

internal static class SfntFaceWriter
{
    internal static byte[] Write(FontFace face, FontLimits limits)
    {
        if (face == null)
        {
            throw new System.ArgumentNullException("face");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        if (!face.IsStructurallyValid())
        {
            throw new System.InvalidOperationException(
                "A structurally invalid inspect-only face cannot be reconstructed.");
        }

        return Write(
            face.GetScalerType(),
            face.GetTableStore(),
            limits);
    }

    internal static byte[] Write(
        uint scalerType,
        SfntTableStore tableStore,
        FontLimits limits)
    {
        if (tableStore == null)
        {
            throw new System.ArgumentNullException("tableStore");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        SfntWriteTable[] tables =
            new SfntWriteTable[tableStore.GetTableCount()];
        int index = 0;

        while (index < tables.Length)
        {
            SfntTableData table = tableStore.GetTable(index);
            tables[index] = new SfntWriteTable(
                table.GetTag(),
                table.GetDataSlice().CopyBytes());
            index += 1;
        }

        return SfntWriter.Write(scalerType, tables, limits);
    }
}
