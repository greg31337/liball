namespace json;

/// <summary>Selects how duplicate decoded object names are handled.</summary>
public enum JsonDuplicateKeyPolicy
{
    /// <summary>Retains every object member in source order.</summary>
    Preserve = 0,

    /// <summary>Rejects the object when a duplicate name is encountered.</summary>
    Reject = 1,

    /// <summary>Retains the first member with each decoded name.</summary>
    KeepFirst = 2,

    /// <summary>Retains the last member with each decoded name.</summary>
    KeepLast = 3
}
