namespace avif.Tests;

public class Av1CdfContextTests
{
    const string JointTable = "Default_Mv_Joint_Cdf";
    const string CoefficientTable = "Default_Txb_Skip_Cdf";

    [Test]
    public void DefaultContextsContainEveryPersistentTableAndQuantizerSlice()
    {
        int[] baseQuantizers = new int[] { 0, 21, 61, 121 };
        ushort[] coefficientDefaults = Av1DefaultCdfTables.GetValues(CoefficientTable);
        int sliceLength = coefficientDefaults.Length / 4;
        int index = 0;
        while (index < baseQuantizers.Length)
        {
            Av1FrameCdfContext? context =
                Av1FrameCdfContext.CreateDefault(baseQuantizers[index]);
            Av1CdfTable? coefficient = (context ?? throw new InvalidOperationException("Invalid internal state.")).GetTable(CoefficientTable);
            ushort[] expected = coefficientDefaults
                .Skip(index * sliceLength)
                .Take(sliceLength)
                .ToArray();
            int tableIndex = 1;
            while (tableIndex < Av1DefaultCdfTables.GetTableCount())
            {
                string tableName = Av1DefaultCdfTables.GetTableName(tableIndex);
                Assert.That((context.GetTable(tableName) ?? throw new InvalidOperationException("Invalid internal state.")).IsValid(), Is.True, tableName);
                tableIndex++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(context.GetTableCount(), Is.EqualTo(95));
                Assert.That(context.CoefficientQuantizerContext, Is.EqualTo(index));
                Assert.That(context.GetTable(
                    Av1FrameCdfContext.IntraFrameYModeTableName), Is.Null);
                Assert.That((coefficient ?? throw new InvalidOperationException("Invalid internal state.")).Values, Is.EqualTo(expected));
                Assert.That(context.IsValid(), Is.True);
            });
            index++;
        }

        Av1TileCdfContext tile =
            (Av1FrameCdfContext.CreateDefault(0) ?? throw new InvalidOperationException("Invalid internal state.")).CreateTileContext(1, 0);
        Assert.Multiple(() =>
        {
            Assert.That(tile.GetTableCount(), Is.EqualTo(96));
            Assert.That(tile.GetTable(
                Av1FrameCdfContext.IntraFrameYModeTableName), Is.Not.Null);
            Assert.That(tile.IsValid(), Is.True);
            Assert.That(Av1FrameCdfContext.CreateDefault(-1), Is.Null);
            Assert.That(Av1FrameCdfContext.CreateDefault(256), Is.Null);
        });
    }

    [Test]
    public void TileContextsAreIndependentAndDesignatedTileUpdatesFrameReferences()
    {
        Av1CdfContextManager manager = new Av1CdfContextManager();
        ParsedAv1FrameHeader header = CreateHeader(20, 2, 1, false, 1 << 2 | 1 << 5);

        AvifResultCode begin = manager.BeginFrame(header);
        Av1TileCdfContext? firstTile = manager.CreateTileContext(0);
        Av1TileCdfContext? updateTile = manager.CreateTileContext(1);
        DecodeJointSymbol(firstTile, 2);
        AvifResultCode firstFinish = manager.FinishTile(firstTile);
        ushort[] untouchedUpdateTile = GetJointValues(updateTile);
        DecodeJointSymbol(updateTile, 2);
        AvifResultCode updateFinish = manager.FinishTile(updateTile);
        AvifResultCode complete = manager.CompleteFrame();

        Assert.Multiple(() =>
        {
            Assert.That(begin, Is.EqualTo(AvifResultCode.Success));
            Assert.That(firstFinish, Is.EqualTo(AvifResultCode.Success));
            Assert.That(untouchedUpdateTile,
                Is.EqualTo(new ushort[] { 4096, 11264, 19328, 32768, 0 }));
            Assert.That(updateFinish, Is.EqualTo(AvifResultCode.Success));
            Assert.That(complete, Is.EqualTo(AvifResultCode.Success));
            Assert.That(GetJointValues(manager.CurrentFrameContext),
                Is.EqualTo(new ushort[] { 3968, 10912, 19748, 32768, 1 }));
            Assert.That(GetJointValues(manager.GetReferenceContext(2)),
                Is.EqualTo(GetJointValues(manager.CurrentFrameContext)));
            Assert.That(GetJointValues(manager.GetReferenceContext(5)),
                Is.EqualTo(GetJointValues(manager.CurrentFrameContext)));
            Assert.That(manager.GetReferenceContext(0), Is.Null);
        });
    }

    [Test]
    public void LoadingPrimaryReferenceKeepsProbabilitiesAndResetsCounts()
    {
        Av1CdfContextManager manager = new Av1CdfContextManager();
        StoreAdaptedContext(manager, 3);
        ushort[] stored = GetJointValues(manager.GetReferenceContext(3));
        ParsedAv1FrameHeader next = CreateHeader(200, 1, 0, true, 0);
        next.PrimaryReferenceFrame = 0;
        next.ReferenceFrameIndexes[0] = 3;

        AvifResultCode begin = manager.BeginFrame(next);
        ushort[] loaded = GetJointValues(manager.CurrentFrameContext);

        Assert.Multiple(() =>
        {
            Assert.That(begin, Is.EqualTo(AvifResultCode.Success));
            Assert.That(stored,
                Is.EqualTo(new ushort[] { 3968, 10912, 19748, 32768, 1 }));
            Assert.That(loaded,
                Is.EqualTo(new ushort[] { 3968, 10912, 19748, 32768, 0 }));
            Assert.That((manager.CurrentFrameContext ?? throw new InvalidOperationException("Invalid internal state.")).CoefficientQuantizerContext,
                Is.EqualTo(0));
            Assert.That(GetJointValues(manager.GetReferenceContext(3)), Is.EqualTo(stored));
        });
    }

    [Test]
    public void DisabledFrameEndUpdateStoresTheUnmodifiedFrameContext()
    {
        Av1CdfContextManager manager = new Av1CdfContextManager();
        ParsedAv1FrameHeader header = CreateHeader(20, 1, 0, true, 1 << 4);

        manager.BeginFrame(header);
        Av1TileCdfContext? tile = manager.CreateTileContext(0);
        DecodeJointSymbol(tile, 2);
        manager.FinishTile(tile);
        AvifResultCode complete = manager.CompleteFrame();

        Assert.Multiple(() =>
        {
            Assert.That(complete, Is.EqualTo(AvifResultCode.Success));
            Assert.That(GetJointValues(manager.GetReferenceContext(4)),
                Is.EqualTo(new ushort[] { 4096, 11264, 19328, 32768, 0 }));
        });
    }

    [Test]
    public void ErrorResilientFrameUsesDefaultsInsteadOfStoredPrimaryContext()
    {
        Av1CdfContextManager manager = new Av1CdfContextManager();
        StoreAdaptedContext(manager, 6);
        ParsedAv1FrameHeader resilient = CreateHeader(61, 1, 0, true, 0);
        resilient.ErrorResilientMode = true;
        resilient.PrimaryReferenceFrame = 0;
        resilient.ReferenceFrameIndexes[0] = 6;

        AvifResultCode begin = manager.BeginFrame(resilient);

        Assert.Multiple(() =>
        {
            Assert.That(begin, Is.EqualTo(AvifResultCode.Success));
            Assert.That(GetJointValues(manager.CurrentFrameContext),
                Is.EqualTo(new ushort[] { 4096, 11264, 19328, 32768, 0 }));
            Assert.That((manager.CurrentFrameContext ?? throw new InvalidOperationException("Invalid internal state.")).CoefficientQuantizerContext,
                Is.EqualTo(2));
            Assert.That(GetJointValues(manager.GetReferenceContext(6)),
                Is.EqualTo(new ushort[] { 3968, 10912, 19748, 32768, 1 }));
        });
    }

    [Test]
    public void IncompleteFramesAndInvalidLifecycleCallsAreRejectedAndResettable()
    {
        Av1CdfContextManager manager = new Av1CdfContextManager();
        ParsedAv1FrameHeader header = CreateHeader(20, 2, 1, false, 0);
        manager.BeginFrame(header);
        Av1TileCdfContext? first = manager.CreateTileContext(0);
        manager.FinishTile(first);

        AvifResultCode incomplete = manager.CompleteFrame();
        manager.Reset();
        Av1TileCdfContext? inactive = manager.CreateTileContext(0);

        Assert.Multiple(() =>
        {
            Assert.That(incomplete, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(inactive, Is.Null);
            Assert.That(manager.Code, Is.EqualTo(AvifResultCode.InvalidState));
        });

        manager.Reset();
        Assert.Multiple(() =>
        {
            Assert.That(manager.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(manager.FrameActive, Is.False);
            Assert.That(manager.GetReferenceContext(0), Is.Null);
        });
    }

    static ParsedAv1FrameHeader CreateHeader(
        int baseQIndex,
        int tileColumns,
        int contextUpdateTile,
        bool disableFrameEndUpdate,
        int refreshFlags)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.PrimaryReferenceFrame = 7;
        header.DisableFrameEndUpdateCdf = disableFrameEndUpdate;
        header.RefreshFrameFlags = refreshFlags;
        header.Quantization = new Av1QuantizationParameters();
        header.Quantization.BaseQIndex = baseQIndex;
        header.Tiles = new Av1TileInfo();
        header.Tiles.TileColumns = tileColumns;
        header.Tiles.TileRows = 1;
        header.Tiles.ContextUpdateTileId = contextUpdateTile;
        return header;
    }

    static void StoreAdaptedContext(Av1CdfContextManager manager, int slot)
    {
        ParsedAv1FrameHeader header = CreateHeader(20, 1, 0, false, 1 << slot);
        manager.BeginFrame(header);
        Av1TileCdfContext? tile = manager.CreateTileContext(0);
        DecodeJointSymbol(tile, 2);
        manager.FinishTile(tile);
        Assert.That(manager.CompleteFrame(), Is.EqualTo(AvifResultCode.Success));
    }

    static void DecodeJointSymbol(Av1TileCdfContext? tile, int expectedSymbol)
    {
        Av1SymbolDecoder decoder = new Av1SymbolDecoder(
            new byte[] { 0x70, 0x00 }, 0, 2, false);
        int symbol = decoder.ReadCdfSymbol((tile ?? throw new InvalidOperationException("Invalid internal state.")).GetCdf(JointTable, 0));
        Assert.Multiple(() =>
        {
            Assert.That(symbol, Is.EqualTo(expectedSymbol));
            Assert.That(decoder.Exit(), Is.EqualTo(AvifResultCode.Success));
        });
    }

    static ushort[] GetJointValues(Av1TileCdfContext? context)
    {
        return (ushort[])((context ?? throw new InvalidOperationException("Invalid internal state.")).GetTable(JointTable) ?? throw new InvalidOperationException("Invalid internal state.")).Values.Clone();
    }

    static ushort[] GetJointValues(Av1FrameCdfContext? context)
    {
        return (ushort[])((context ?? throw new InvalidOperationException("Invalid internal state.")).GetTable(JointTable) ?? throw new InvalidOperationException("Invalid internal state.")).Values.Clone();
    }
}
