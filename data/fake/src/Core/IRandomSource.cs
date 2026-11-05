using System;

namespace fake;

internal interface IRandomSource
{
    int IntFromMaximumExclusive(int maximumExclusive);

    void FillFromDestination(Span<byte> destination);
}
