namespace raw;

/// <summary>Defines mutable profile registration operations for a profile catalogue.</summary>
public interface IRawProfileRegistry
{
    /// <summary>Returns the source corresponding to each registered read profile.</summary>
    /// <returns>An independently replaceable source array in profile order.</returns>
    RawProfileRegistrationSource[] GetReadProfileSources();

    /// <summary>Returns the source corresponding to each registered write profile.</summary>
    /// <returns>An independently replaceable source array in profile order.</returns>
    RawProfileRegistrationSource[] GetWriteProfileSources();

    /// <summary>Returns all registration diagnostics accumulated by the catalogue.</summary>
    /// <returns>An operation result containing an independent diagnostic snapshot.</returns>
    RawOperationResult GetRegistrationDiagnostics();

    /// <summary>Loads one validated built-in profile document.</summary>
    /// <param name="json">The complete profile JSON.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The atomic registration result.</returns>
    RawProfileRegistrationResult LoadBuiltIn(byte[] json, RawLimits limits);

    /// <summary>Registers caller-supplied custom profile JSON.</summary>
    /// <param name="json">The complete profile JSON.</param>
    /// <param name="replaceCustom">Whether an existing custom profile may be replaced.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The atomic registration result.</returns>
    RawProfileRegistrationResult RegisterCustom(byte[] json, bool replaceCustom, RawLimits limits);

    /// <summary>Registers a caller-supplied profile document.</summary>
    /// <param name="document">The profile document.</param>
    /// <param name="replaceCustom">Whether an existing custom profile may be replaced.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The atomic registration result.</returns>
    RawProfileRegistrationResult RegisterCustomDocument(RawProfileSchemaDocument? document, bool replaceCustom, RawLimits limits);
}
