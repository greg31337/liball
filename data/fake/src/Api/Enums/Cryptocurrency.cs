namespace fake;

/// <summary>Selects a supported cryptocurrency address shape.</summary>
public enum Cryptocurrency
{
    /// <summary>A Bitcoin P2PKH Base58Check address shape.</summary>
    Bitcoin = 0,

    /// <summary>An Ethereum EIP-55 hexadecimal address shape.</summary>
    Ethereum = 1,

    /// <summary>A Litecoin P2PKH Base58Check address shape.</summary>
    Litecoin = 2
}
