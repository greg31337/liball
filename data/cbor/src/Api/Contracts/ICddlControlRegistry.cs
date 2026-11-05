namespace cbor;

/// <summary>Stores application CDDL control-operator registrations.</summary>
public interface ICddlControlRegistry
{
    /// <summary>Adds one custom control registration.</summary>
    /// <param name="registration">The registration to add.</param>
    void AddControl(CddlControlRegistration registration);

    /// <summary>Finds a custom control registration by name.</summary>
    /// <param name="name">The control name including its leading period.</param>
    /// <returns>The registration, or null when none is available.</returns>
    CddlControlRegistration? FindControl(string name);
}
