namespace cbor;

/// <summary>Stores application CDDL control-operator registrations.</summary>
public class CddlControlRegistry : ICddlControlRegistry
{
    internal System.Collections.Generic.List<CddlControlRegistration>
        Registrations = new System.Collections.Generic.List<CddlControlRegistration>();
    internal object Sync = new object();

    /// <summary>Creates an empty CDDL control registry.</summary>
    public CddlControlRegistry()
    {
    }

    /// <summary>Adds one custom control registration.</summary>
    /// <param name="registration">The registration to add.</param>
    public void AddControl(CddlControlRegistration registration)
    {
        if (registration == null)
        {
            throw new System.ArgumentNullException("registration");
        }
        if (!IsValidName(registration.Name))
        {
            throw new System.ArgumentException(
                "A control registration name must start with a period and contain a name.",
                "registration"
            );
        }
        if (registration.IsEnabled && registration.Handler == null)
        {
            throw new System.ArgumentException(
                "An enabled control registration requires a handler.",
                "registration"
            );
        }
        lock (Sync)
        {
            int index = 0;
            while (index < Registrations.Count)
            {
                if (Registrations[index].Name == registration.Name)
                {
                    throw new System.ArgumentException(
                        $"Control {registration.Name} is already registered.",
                        "registration"
                    );
                }
                index = index + 1;
            }
            Registrations.Add(CopyRegistration(registration));
        }
    }

    /// <summary>Finds a custom control registration by name.</summary>
    /// <param name="name">The control name including its leading period.</param>
    /// <returns>The registration, or null when none is available.</returns>
    public CddlControlRegistration? FindControl(string name)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }
        lock (Sync)
        {
            int index = 0;
            while (index < Registrations.Count)
            {
                CddlControlRegistration registration = Registrations[index];
                if (registration.Name == name)
                {
                    return CopyRegistration(registration);
                }
                index = index + 1;
            }
        }
        return null;
    }

    internal bool IsValidName(string name)
    {
        if (name == null || name.Length < 2 || name[0] != '.'
            || !IsNameStart(name[1]))
        {
            return false;
        }
        int index = 2;
        while (index < name.Length)
        {
            char value = name[index];
            if (!IsNameStart(value)
                && !(value >= '0' && value <= '9')
                && value != '-'
                && value != '.')
            {
                return false;
            }
            index = index + 1;
        }
        char last = name[name.Length - 1];
        return last != '-' && last != '.';
    }

    internal bool IsNameStart(char value)
    {
        return (value >= 'A' && value <= 'Z')
            || (value >= 'a' && value <= 'z')
            || value == '@'
            || value == '_'
            || value == '$';
    }

    internal CddlControlRegistration CopyRegistration(
        CddlControlRegistration registration)
    {
        CddlControlRegistration copy = new CddlControlRegistration();
        copy.Name = registration.Name;
        copy.Handler = registration.Handler;
        copy.IsEnabled = registration.IsEnabled;
        return copy;
    }
}
