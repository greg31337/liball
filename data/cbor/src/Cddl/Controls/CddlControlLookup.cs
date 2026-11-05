using System.Collections.Generic;

namespace cbor;

internal class CddlControlLookup
{
    internal List<CddlControlRegistration> Registrations =
        new List<CddlControlRegistration>();
    internal string? ErrorMessage;

    internal CddlControlLookup(CddlControlRegistration[]? registrations)
    {
        if (registrations == null)
        {
            ErrorMessage = "The schema control-registration collection is required.";
            return;
        }
        int index = 0;
        while (index < registrations.Length)
        {
            CddlControlRegistration? source = registrations[index];
            if (source == null)
            {
                ErrorMessage = "A schema control registration is required.";
                return;
            }
            if (!IsValidName(source.Name))
            {
                ErrorMessage =
                    "A control registration name must start with a period and contain a name.";
                return;
            }
            if (FindAnyControl(source.Name) != null)
            {
                ErrorMessage = $"Control {source.Name} is registered more than once.";
                return;
            }
            if (source.IsEnabled && source.Handler == null)
            {
                ErrorMessage =
                    $"Enabled control {source.Name} does not have a handler.";
                return;
            }
            CddlControlRegistration registration =
                new CddlControlRegistration();
            registration.Name = source.Name;
            registration.Handler = source.Handler;
            registration.IsEnabled = source.IsEnabled;
            Registrations.Add(registration);
            index = index + 1;
        }
    }

    internal CddlControlRegistration? FindControl(string name)
    {
        CddlControlRegistration? registration = FindAnyControl(name);
        if (registration == null
            || !registration.IsEnabled
            || registration.Handler == null)
        {
            return null;
        }
        return registration;
    }

    internal CddlControlRegistration? FindAnyControl(string name)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            CddlControlRegistration registration = Registrations[index];
            if (registration.Name == name)
            {
                return registration;
            }
            index = index + 1;
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
}
