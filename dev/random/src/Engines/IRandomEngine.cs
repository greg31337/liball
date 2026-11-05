using System;

namespace random;

internal interface IRandomEngine
{
    RandomAlgorithmInfo EngineInfo();

    ulong NextRawWord();

    IRandomEngine CloneEngine();

    RandomState ExportEngineState();
}

internal interface IAdvanceableRandomEngine
{
    void AdvanceEngine(UInt128 distance);
}

internal interface IJumpableRandomEngine
{
    void JumpEngine();

    void LongJumpEngine();
}

internal interface ISeekableRandomEngine
{
    void SeekEngine(UInt128 blockIndex);

    void SeekEngineCounter(ulong[] absoluteCounter);
}

internal interface IBlockReservableRandomEngine
{
    ulong[] ReserveBlocksEngine(UInt128 blockCount);
}
