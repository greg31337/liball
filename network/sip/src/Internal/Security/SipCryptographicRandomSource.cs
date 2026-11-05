namespace sip;

internal class SipCryptographicRandomSource : ISipRandomSource
{
    public void FillBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
    }
}
