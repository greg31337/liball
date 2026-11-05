namespace icc;

internal class EmbeddedCarrierBuffer
{
    internal byte[] Bytes;
    internal long BaseOffset;
    internal bool LimitExceeded;

    internal EmbeddedCarrierBuffer()
    {
        Bytes = new byte[0];
        BaseOffset = 0;
        LimitExceeded = false;
    }
}
