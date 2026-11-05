using System;
using System.Collections.Generic;

namespace avif;

// AV1 specification sections 5.9.2, 7.7, 7.20, 8.2.2, and 8.3.
internal class Av1CdfTable
{
    internal string Name;
    internal ushort[] Values;
    internal int[] RowOffsets;
    internal int[] RowSymbolCounts;

    internal static Av1CdfTable Create(string name, ushort[] values)
    {
        int rowCount = CountRows(values);
        int[] rowOffsets = new int[rowCount];
        int[] rowSymbolCounts = new int[rowCount];
        int row = 0;
        int start = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == 0 && index > start
                && values[index - 1] == Av1SyntaxConstants.CdfProbabilityTop)
            {
                rowOffsets[row] = start;
                rowSymbolCounts[row] = index - start;
                row++;
                start = index + 1;
            }

            index++;
        }
        return new Av1CdfTable(name, values, rowOffsets, rowSymbolCounts);
    }

    internal Av1CdfTable(
        string name,
        ushort[] values,
        int[] rowOffsets,
        int[] rowSymbolCounts)
    {
        Name = name;
        Values = values;
        RowOffsets = rowOffsets;
        RowSymbolCounts = rowSymbolCounts;
    }

    internal int GetRowCount()
    {
        return RowOffsets.Length;
    }

    internal Av1Cdf? GetRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= RowOffsets.Length)
        {
            return null;
        }

        return new Av1Cdf(
            Values,
            RowOffsets[rowIndex],
            RowSymbolCounts[rowIndex]);
    }

    internal Av1CdfTable Copy(bool resetCounts)
    {
        ushort[] values = (ushort[])Values.Clone();
        Av1CdfTable copy = new Av1CdfTable(
            Name,
            values,
            (int[])RowOffsets.Clone(),
            (int[])RowSymbolCounts.Clone());
        if (resetCounts)
        {
            copy.ResetCounts();
        }

        return copy;
    }

    internal void ResetCounts()
    {
        int row = 0;
        while (row < RowOffsets.Length)
        {
            Values[RowOffsets[row] + RowSymbolCounts[row]] = 0;
            row++;
        }
    }

    internal bool IsValid()
    {
        if (string.IsNullOrEmpty(Name) || Values == null
            || RowOffsets == null || RowSymbolCounts == null
            || RowOffsets.Length == 0
            || RowOffsets.Length != RowSymbolCounts.Length)
        {
            return false;
        }

        int row = 0;
        while (row < RowOffsets.Length)
        {
            Av1Cdf? cdf = GetRow(row);
            if (cdf == null || !cdf.IsValid())
            {
                return false;
            }

            row++;
        }

        int lastRow = RowOffsets.Length - 1;
        return RowOffsets[lastRow] + RowSymbolCounts[lastRow] + 1 == Values.Length;
    }

    static int CountRows(ushort[] values)
    {
        if (values == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == 0 && index > 0
                && values[index - 1] == Av1SyntaxConstants.CdfProbabilityTop)
            {
                count++;
            }

            index++;
        }

        return count;
    }
}

internal class Av1FrameCdfContext
{
    internal const string IntraFrameYModeTableName =
        "Default_Intra_Frame_Y_Mode_Cdf";
    internal const int FirstCoefficientTableIndex = 83;

    internal Dictionary<string, Av1CdfTable> Tables;
    internal int CoefficientQuantizerContext;

    internal Av1FrameCdfContext()
    {
        Tables = new Dictionary<string, Av1CdfTable>();
    }

    internal static Av1FrameCdfContext? CreateDefault(int baseQIndex)
    {
        if (baseQIndex < 0 || baseQIndex > Av1SyntaxConstants.MaxQuantizerIndex)
        {
            return null;
        }

        Av1FrameCdfContext context = new Av1FrameCdfContext();
        context.CoefficientQuantizerContext = GetCoefficientQuantizerContext(baseQIndex);
        int tableIndex = 1;
        while (tableIndex < Av1DefaultCdfTables.GetTableCount())
        {
            string name = Av1DefaultCdfTables.GetTableName(tableIndex);
            ushort[] values = Av1DefaultCdfTables.GetValues(name);
            if (tableIndex >= FirstCoefficientTableIndex)
            {
                int sliceLength = values.Length / 4;
                ushort[] slice = new ushort[sliceLength];
                System.Array.Copy(
                    values,
                    context.CoefficientQuantizerContext * sliceLength,
                    slice,
                    0,
                    sliceLength);
                values = slice;
            }

            context.Tables.Add(name, Av1CdfTable.Create(name, values));
            tableIndex++;
        }

        return context;
    }

    internal int GetTableCount()
    {
        return Tables.Count;
    }

    internal Av1CdfTable? GetTable(string name)
    {
        if (name == null || !Tables.ContainsKey(name))
        {
            return null;
        }

        return Tables[name];
    }

    internal Av1Cdf? GetCdf(string name, int rowIndex)
    {
        Av1CdfTable? table = GetTable(name);
        return table == null ? null : table.GetRow(rowIndex);
    }

    internal Av1FrameCdfContext Copy(bool resetCounts)
    {
        Av1FrameCdfContext copy = new Av1FrameCdfContext();
        copy.CoefficientQuantizerContext = CoefficientQuantizerContext;
        foreach (KeyValuePair<string, Av1CdfTable> item in Tables)
        {
            copy.Tables.Add(item.Key, item.Value.Copy(resetCounts));
        }

        return copy;
    }

    internal Av1TileCdfContext CreateTileContext(int frameSerial, int tileIndex)
    {
        Av1TileCdfContext tile = new Av1TileCdfContext(frameSerial, tileIndex);
        string intraName = Av1DefaultCdfTables.GetTableName(0);
        tile.Tables.Add(
            intraName,
            Av1CdfTable.Create(intraName, Av1DefaultCdfTables.GetValues(intraName)));
        foreach (KeyValuePair<string, Av1CdfTable> item in Tables)
        {
            tile.Tables.Add(item.Key, item.Value.Copy(false));
        }

        return tile;
    }

    internal bool IsValid()
    {
        if (Tables == null
            || Tables.Count != Av1DefaultCdfTables.GetTableCount() - 1
            || CoefficientQuantizerContext < 0 || CoefficientQuantizerContext > 3)
        {
            return false;
        }

        int tableIndex = 1;
        while (tableIndex < Av1DefaultCdfTables.GetTableCount())
        {
            Av1CdfTable? table = GetTable(Av1DefaultCdfTables.GetTableName(tableIndex));
            if (table == null || !table.IsValid())
            {
                return false;
            }

            tableIndex++;
        }

        return true;
    }

    static int GetCoefficientQuantizerContext(int baseQIndex)
    {
        if (baseQIndex <= 20)
        {
            return 0;
        }

        if (baseQIndex <= 60)
        {
            return 1;
        }

        return baseQIndex <= 120 ? 2 : 3;
    }
}

internal class Av1TileCdfContext
{
    internal int FrameSerial;
    internal int TileIndex;
    internal Dictionary<string, Av1CdfTable> Tables;

    internal Av1TileCdfContext(int frameSerial, int tileIndex)
    {
        FrameSerial = frameSerial;
        TileIndex = tileIndex;
        Tables = new Dictionary<string, Av1CdfTable>();
    }

    internal int GetTableCount()
    {
        return Tables.Count;
    }

    internal Av1CdfTable? GetTable(string name)
    {
        if (name == null || !Tables.ContainsKey(name))
        {
            return null;
        }

        return Tables[name];
    }

    internal Av1Cdf? GetCdf(string name, int rowIndex)
    {
        Av1CdfTable? table = GetTable(name);
        return table == null ? null : table.GetRow(rowIndex);
    }

    internal Av1FrameCdfContext? CopyToFrameContext()
    {
        Av1FrameCdfContext frame = new Av1FrameCdfContext();
        int tableIndex = 1;
        while (tableIndex < Av1DefaultCdfTables.GetTableCount())
        {
            string name = Av1DefaultCdfTables.GetTableName(tableIndex);
            Av1CdfTable? table = GetTable(name);
            if (table == null)
            {
                return null;
            }

            frame.Tables.Add(name, table.Copy(false));
            tableIndex++;
        }

        return frame;
    }

    internal bool IsValid()
    {
        if (Tables == null || Tables.Count != Av1DefaultCdfTables.GetTableCount())
        {
            return false;
        }

        int tableIndex = 0;
        while (tableIndex < Av1DefaultCdfTables.GetTableCount())
        {
            Av1CdfTable? table = GetTable(Av1DefaultCdfTables.GetTableName(tableIndex));
            if (table == null || !table.IsValid())
            {
                return false;
            }

            tableIndex++;
        }

        return true;
    }
}

internal class Av1CdfContextManager
{
    internal Av1FrameCdfContext[] ReferenceContexts;
    internal Av1FrameCdfContext? CurrentFrameContext;
    internal Av1FrameCdfContext? SavedTileContext;
    internal ParsedAv1FrameHeader? CurrentHeader;
    internal bool[]? FinishedTiles;
    internal int FinishedTileCount;
    internal int FrameSerial;
    internal bool FrameActive;
    internal AvifResultCode Code;

    internal Av1CdfContextManager()
    {
        ReferenceContexts =
            new Av1FrameCdfContext[Av1SyntaxConstants.StoredReferenceFrames];
        Code = AvifResultCode.Success;
    }

    internal AvifResultCode BeginFrame(ParsedAv1FrameHeader header)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (FrameActive)
        {
            SetCode(AvifResultCode.InvalidState);
            return Code;
        }

        if (!HasValidHeader(header))
        {
            SetCode(AvifResultCode.InvalidArgument);
            return Code;
        }

        Av1FrameCdfContext? context;
        if (header.ErrorResilientMode || header.PrimaryReferenceFrame == 7)
        {
            context = Av1FrameCdfContext.CreateDefault((header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).BaseQIndex);
        }
        else
        {
            int slot = header.ReferenceFrameIndexes[header.PrimaryReferenceFrame];
            if (slot < 0 || slot >= ReferenceContexts.Length
                || ReferenceContexts[slot] == null
                || !ReferenceContexts[slot].IsValid())
            {
                SetCode(AvifResultCode.InvalidAv1Bitstream);
                return Code;
            }

            context = ReferenceContexts[slot].Copy(true);
        }

        if (context == null || !context.IsValid())
        {
            SetCode(AvifResultCode.InvalidAv1Bitstream);
            return Code;
        }

        FrameSerial++;
        CurrentHeader = header;
        CurrentFrameContext = context;
        SavedTileContext = null;
        FinishedTiles = new bool[(header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns * header.Tiles.TileRows];
        FinishedTileCount = 0;
        FrameActive = true;
        return Code;
    }

    internal Av1TileCdfContext? CreateTileContext(int tileIndex)
    {
        if (Code != AvifResultCode.Success || !FrameActive)
        {
            SetCode(AvifResultCode.InvalidState);
            return null;
        }

        int tileCount = ((CurrentHeader ?? throw new InvalidOperationException("Invalid internal state.")).Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns * (CurrentHeader.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileRows;
        if (tileIndex < 0 || tileIndex >= tileCount)
        {
            SetCode(AvifResultCode.InvalidArgument);
            return null;
        }

        return (CurrentFrameContext ?? throw new InvalidOperationException("Invalid internal state.")).CreateTileContext(FrameSerial, tileIndex);
    }

    internal AvifResultCode FinishTile(Av1TileCdfContext? tile)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (!FrameActive || tile == null || tile.FrameSerial != FrameSerial
            || tile.TileIndex < 0
            || tile.TileIndex >= ((CurrentHeader ?? throw new InvalidOperationException("Invalid internal state.")).Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns
                * (CurrentHeader.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileRows
            || (FinishedTiles ?? throw new InvalidOperationException("Invalid internal state."))[tile.TileIndex]
            || !tile.IsValid())
        {
            SetCode(AvifResultCode.InvalidArgument);
            return Code;
        }

        if (!CurrentHeader.DisableFrameEndUpdateCdf
            && tile.TileIndex == CurrentHeader.Tiles.ContextUpdateTileId)
        {
            SavedTileContext = tile.CopyToFrameContext();
            if (SavedTileContext == null)
            {
                SetCode(AvifResultCode.InvalidAv1Bitstream);
            }
            else
            {
                SavedTileContext.CoefficientQuantizerContext =
                    (CurrentFrameContext ?? throw new InvalidOperationException("Invalid internal state.")).CoefficientQuantizerContext;
            }
        }

        if (Code == AvifResultCode.Success)
        {
            FinishedTiles[tile.TileIndex] = true;
            FinishedTileCount++;
        }

        return Code;
    }

    internal AvifResultCode CompleteFrame()
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (!FrameActive)
        {
            SetCode(AvifResultCode.InvalidState);
            return Code;
        }

        if (FinishedTileCount != (FinishedTiles ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            SetCode(AvifResultCode.InvalidAv1Bitstream);
            return Code;
        }

        if (!(CurrentHeader ?? throw new InvalidOperationException("Invalid internal state.")).DisableFrameEndUpdateCdf)
        {
            if (SavedTileContext == null || !SavedTileContext.IsValid())
            {
                SetCode(AvifResultCode.InvalidAv1Bitstream);
                return Code;
            }

            CurrentFrameContext = SavedTileContext.Copy(false);
        }

        int slot = 0;
        while (slot < ReferenceContexts.Length)
        {
            if ((CurrentHeader.RefreshFrameFlags & (1 << slot)) != 0)
            {
                ReferenceContexts[slot] = (CurrentFrameContext ?? throw new InvalidOperationException("Invalid internal state.")).Copy(false);
            }

            slot++;
        }

        FrameActive = false;
        CurrentHeader = null;
        SavedTileContext = null;
        FinishedTiles = null;
        FinishedTileCount = 0;
        return Code;
    }

    internal Av1FrameCdfContext? GetReferenceContext(int slot)
    {
        if (slot < 0 || slot >= ReferenceContexts.Length)
        {
            return null;
        }

        return ReferenceContexts[slot];
    }

    internal AvifResultCode AbortFrame()
    {
        if (!FrameActive)
        {
            return AvifResultCode.InvalidState;
        }

        CurrentFrameContext = null;
        SavedTileContext = null;
        CurrentHeader = null;
        FinishedTiles = null;
        FinishedTileCount = 0;
        FrameActive = false;
        return AvifResultCode.Success;
    }

    internal void Reset()
    {
        System.Array.Clear(ReferenceContexts);
        CurrentFrameContext = null;
        SavedTileContext = null;
        CurrentHeader = null;
        FinishedTiles = null;
        FinishedTileCount = 0;
        FrameSerial = 0;
        FrameActive = false;
        Code = AvifResultCode.Success;
    }

    static bool HasValidHeader(ParsedAv1FrameHeader header)
    {
        return header != null && !header.ShowExistingFrame
            && header.ReferenceFrameIndexes != null
            && header.ReferenceFrameIndexes.Length == Av1SyntaxConstants.RefsPerFrame
            && header.Quantization != null
            && header.Quantization.BaseQIndex >= 0
            && header.Quantization.BaseQIndex <= Av1SyntaxConstants.MaxQuantizerIndex
            && header.Tiles != null && header.Tiles.TileColumns > 0
            && header.Tiles.TileColumns <= Av1SyntaxConstants.MaxTileColumns
            && header.Tiles.TileRows > 0
            && header.Tiles.TileRows <= Av1SyntaxConstants.MaxTileRows
            && header.Tiles.ContextUpdateTileId >= 0
            && header.Tiles.ContextUpdateTileId
                < header.Tiles.TileColumns * header.Tiles.TileRows
            && header.RefreshFrameFlags >= 0 && header.RefreshFrameFlags <= 255
            && header.PrimaryReferenceFrame >= 0 && header.PrimaryReferenceFrame <= 7
            && (!header.DisableCdfUpdate || header.DisableFrameEndUpdateCdf);
    }

    void SetCode(AvifResultCode code)
    {
        if (Code == AvifResultCode.Success)
        {
            Code = code;
        }
    }
}
