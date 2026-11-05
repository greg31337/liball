using System;
using System.Collections.Generic;

namespace diameter;

internal class ApplicationRegistryEntry
{
    internal IDiameterApplication Application;
    internal DiameterApplicationRegistration Registration;

    internal ApplicationRegistryEntry(
        IDiameterApplication application,
        DiameterApplicationRegistration registration)
    {
        Application = application;
        Registration = registration;
    }
}

internal class ApplicationRegistryLookup
{
    internal ApplicationRegistryEntry? Entry;
    internal bool ApplicationRegistered;
}

internal static class ApplicationRegistryCloner
{
    internal static DiameterApplicationRegistration CloneApplicationRegistration(
        DiameterApplicationRegistration source)
    {
        DiameterApplicationRegistration result =
            new DiameterApplicationRegistration();
        result.Name = source.Name.Trim();
        result.ApplicationId = source.ApplicationId;
        result.VendorId = source.VendorId;
        result.AdvertiseAuthentication = source.AdvertiseAuthentication;
        result.AdvertiseAccounting = source.AdvertiseAccounting;
        result.Roles = source.Roles;
        result.MaximumConcurrency = source.MaximumConcurrency;
        result.QueueCapacity = source.QueueCapacity;
        result.DrainTimeoutMilliseconds = source.DrainTimeoutMilliseconds;

        int commandIndex = 0;
        while (commandIndex < source.Commands.Count)
        {
            result.Commands.Add(CloneApplicationCommand(
                source.Commands[commandIndex]));
            commandIndex = commandIndex + 1;
        }

        DictionaryBuilder dictionaryValidator = new DictionaryBuilder();
        int dictionaryIndex = 0;
        while (dictionaryIndex < source.DictionaryModules.Count)
        {
            DiameterDictionaryModule module =
                source.DictionaryModules[dictionaryIndex];
            DictionaryRegistrationResult validation =
                dictionaryValidator.ValidateModule(module);
            if (validation.Status != DictionaryRegistrationStatus.Success)
            {
                throw new ArgumentException(validation.Message);
            }

            result.DictionaryModules.Add(
                DictionaryBuilder.CloneModule(module));
            dictionaryIndex = dictionaryIndex + 1;
        }

        return result;
    }

    internal static DiameterCommandRegistration CloneApplicationCommand(
        DiameterCommandRegistration source)
    {
        DiameterCommandRegistration result =
            new DiameterCommandRegistration();
        result.CommandCode = source.CommandCode;
        result.Name = source.Name.Trim();
        result.IsRequest = source.IsRequest;
        result.RequireValidation = source.RequireValidation;
        result.SessionAware = source.SessionAware;
        return result;
    }
}

internal class ApplicationRegistry
{
    internal object RegistryLock = new object();
    internal Dictionary<string, ApplicationRegistryEntry> EntriesByName =
        new Dictionary<string, ApplicationRegistryEntry>(
            StringComparer.OrdinalIgnoreCase);
    internal Dictionary<ulong, ApplicationRegistryEntry> EntriesByDispatchKey =
        new Dictionary<ulong, ApplicationRegistryEntry>();
    internal List<ApplicationRegistryEntry> EntryOrder =
        new List<ApplicationRegistryEntry>();
    internal long CapabilityRevision;

    internal DiameterApplicationRegistration RegisterApplicationModule(
        IDiameterApplication? application)
    {
        if (application == null)
        {
            throw new ArgumentException(
                "An application module is required.");
        }

        DiameterApplicationRegistration supplied =
            application.ReadDiameterApplicationRegistration();
        if (supplied == null)
        {
            throw new ArgumentException(
                "An application module must supply a registration.");
        }

        supplied.ValidateApplicationRegistration();
        DiameterApplicationRegistration candidate =
            ApplicationRegistryCloner.CloneApplicationRegistration(supplied);
        candidate.ValidateApplicationRegistration();

        lock (RegistryLock)
        {
            ValidateApplicationNameConflict(candidate);
            ValidateApplicationDispatchConflicts(candidate);
            DiameterPeerCapabilities activeCapabilities =
                CreateApplicationCapabilities(null);
            DiameterPeerCapabilities proposedCapabilities =
                CreateApplicationCapabilities(null);
            AppendApplicationCapabilities(proposedCapabilities, candidate);
            bool capabilitiesChanged = !AreApplicationCapabilitiesEqual(
                activeCapabilities,
                proposedCapabilities);
            if (capabilitiesChanged && CapabilityRevision == long.MaxValue)
            {
                throw new DiameterApplicationException(
                    "The application capability revision cannot be incremented.");
            }

            ApplicationRegistryEntry entry =
                new ApplicationRegistryEntry(application, candidate);
            EntriesByName.Add(candidate.Name, entry);
            int commandIndex = 0;
            while (commandIndex < candidate.Commands.Count)
            {
                DiameterCommandRegistration command =
                    candidate.Commands[commandIndex];
                ulong dispatchKey = CreateApplicationDispatchKey(
                    candidate.ApplicationId,
                    command.CommandCode,
                    command.IsRequest);
                EntriesByDispatchKey.Add(dispatchKey, entry);
                commandIndex = commandIndex + 1;
            }

            EntryOrder.Add(entry);
            if (capabilitiesChanged)
            {
                CapabilityRevision = CapabilityRevision + 1L;
            }

            return ApplicationRegistryCloner.CloneApplicationRegistration(
                candidate);
        }
    }

    internal ApplicationRegistryEntry? RemoveApplicationModule(
        string? registrationName)
    {
        if (string.IsNullOrWhiteSpace(registrationName))
        {
            return null;
        }

        lock (RegistryLock)
        {
            if (!EntriesByName.ContainsKey(registrationName))
            {
                return null;
            }

            ApplicationRegistryEntry entry =
                EntriesByName[registrationName];
            DiameterPeerCapabilities activeCapabilities =
                CreateApplicationCapabilities(null);
            DiameterPeerCapabilities proposedCapabilities =
                CreateApplicationCapabilities(entry);
            bool capabilitiesChanged = !AreApplicationCapabilitiesEqual(
                activeCapabilities,
                proposedCapabilities);
            if (capabilitiesChanged && CapabilityRevision == long.MaxValue)
            {
                throw new DiameterApplicationException(
                    "The application capability revision cannot be incremented.");
            }

            EntriesByName.Remove(registrationName);
            int commandIndex = 0;
            while (commandIndex < entry.Registration.Commands.Count)
            {
                DiameterCommandRegistration command =
                    entry.Registration.Commands[commandIndex];
                ulong dispatchKey = CreateApplicationDispatchKey(
                    entry.Registration.ApplicationId,
                    command.CommandCode,
                    command.IsRequest);
                EntriesByDispatchKey.Remove(dispatchKey);
                commandIndex = commandIndex + 1;
            }

            EntryOrder.Remove(entry);
            if (capabilitiesChanged)
            {
                CapabilityRevision = CapabilityRevision + 1L;
            }

            return new ApplicationRegistryEntry(
                entry.Application,
                ApplicationRegistryCloner.CloneApplicationRegistration(
                    entry.Registration));
        }
    }

    internal ApplicationRegistryEntry? ReadApplicationEntry(
        uint applicationId,
        uint commandCode,
        bool request)
    {
        ApplicationRegistryLookup lookup = ReadApplicationLookup(
            applicationId,
            commandCode,
            request);
        return lookup.Entry;
    }

    internal ApplicationRegistryLookup ReadApplicationLookup(
        uint applicationId,
        uint commandCode,
        bool request)
    {
        ApplicationRegistryLookup result =
            new ApplicationRegistryLookup();
        if (commandCode == 0U ||
            commandCode > DiameterProtocolConstants.MaximumCommandCode)
        {
            return result;
        }

        ulong dispatchKey = CreateApplicationDispatchKey(
            applicationId,
            commandCode,
            request);
        lock (RegistryLock)
        {
            if (!EntriesByDispatchKey.ContainsKey(dispatchKey))
            {
                int index = 0;
                while (index < EntryOrder.Count)
                {
                    if (EntryOrder[index].Registration.ApplicationId ==
                        applicationId)
                    {
                        result.ApplicationRegistered = true;
                        return result;
                    }

                    index = index + 1;
                }

                return result;
            }

            ApplicationRegistryEntry entry =
                EntriesByDispatchKey[dispatchKey];
            result.ApplicationRegistered = true;
            result.Entry = new ApplicationRegistryEntry(
                entry.Application,
                ApplicationRegistryCloner.CloneApplicationRegistration(
                    entry.Registration));
            return result;
        }
    }

    internal List<DiameterApplicationRegistration> ReadApplicationRegistrations()
    {
        lock (RegistryLock)
        {
            List<DiameterApplicationRegistration> result =
                new List<DiameterApplicationRegistration>();
            int index = 0;
            while (index < EntryOrder.Count)
            {
                result.Add(
                    ApplicationRegistryCloner.CloneApplicationRegistration(
                        EntryOrder[index].Registration));
                index = index + 1;
            }

            return result;
        }
    }

    internal DiameterPeerCapabilities ReadApplicationCapabilities()
    {
        lock (RegistryLock)
        {
            return CreateApplicationCapabilities(null);
        }
    }

    internal long ReadApplicationCapabilityRevision()
    {
        lock (RegistryLock)
        {
            return CapabilityRevision;
        }
    }

    internal int ReadApplicationCount()
    {
        lock (RegistryLock)
        {
            return EntryOrder.Count;
        }
    }

    internal void ValidateApplicationNameConflict(
        DiameterApplicationRegistration candidate)
    {
        if (EntriesByName.ContainsKey(candidate.Name))
        {
            throw new DiameterApplicationException(
                "An active application uses registration name '" +
                candidate.Name + "'.");
        }
    }

    internal void ValidateApplicationDispatchConflicts(
        DiameterApplicationRegistration candidate)
    {
        int index = 0;
        while (index < candidate.Commands.Count)
        {
            DiameterCommandRegistration command = candidate.Commands[index];
            ulong dispatchKey = CreateApplicationDispatchKey(
                candidate.ApplicationId,
                command.CommandCode,
                command.IsRequest);
            if (EntriesByDispatchKey.ContainsKey(dispatchKey))
            {
                ApplicationRegistryEntry active =
                    EntriesByDispatchKey[dispatchKey];
                string direction = command.IsRequest ? "request" : "answer";
                throw new DiameterApplicationException(
                    "Application-Id " + candidate.ApplicationId +
                    ", Command-Code " + command.CommandCode +
                    ", and " + direction + " direction are already registered by '" +
                    active.Registration.Name + "'.");
            }

            index = index + 1;
        }
    }

    internal void AppendApplicationCapabilities(
        DiameterPeerCapabilities capabilities,
        DiameterApplicationRegistration registration)
    {
        if (registration.VendorId != 0U)
        {
            AppendUniqueApplicationIdentifier(
                capabilities.SupportedVendorIds,
                registration.VendorId);
            if (registration.AdvertiseAuthentication)
            {
                AppendUniqueVendorApplication(
                    capabilities.VendorSpecificApplications,
                    registration.ApplicationId,
                    registration.VendorId,
                    false);
            }

            if (registration.AdvertiseAccounting)
            {
                AppendUniqueVendorApplication(
                    capabilities.VendorSpecificApplications,
                    registration.ApplicationId,
                    registration.VendorId,
                    true);
            }

            return;
        }

        if (registration.AdvertiseAuthentication)
        {
            AppendUniqueApplicationIdentifier(
                capabilities.AuthApplicationIds,
                registration.ApplicationId);
        }

        if (registration.AdvertiseAccounting)
        {
            AppendUniqueApplicationIdentifier(
                capabilities.AcctApplicationIds,
                registration.ApplicationId);
        }
    }

    internal void AppendUniqueApplicationIdentifier(
        List<uint> identifiers,
        uint identifier)
    {
        int index = 0;
        while (index < identifiers.Count)
        {
            if (identifiers[index] == identifier)
            {
                return;
            }

            index = index + 1;
        }

        identifiers.Add(identifier);
    }

    internal void AppendUniqueVendorApplication(
        List<DiameterApplicationId> applications,
        uint applicationId,
        uint vendorId,
        bool accounting)
    {
        int index = 0;
        while (index < applications.Count)
        {
            DiameterApplicationId existing = applications[index];
            if (existing.Id == applicationId &&
                existing.VendorId == vendorId &&
                existing.IsAccounting == accounting)
            {
                return;
            }

            index = index + 1;
        }

        DiameterApplicationId application = new DiameterApplicationId();
        application.Id = applicationId;
        application.VendorId = vendorId;
        application.IsAccounting = accounting;
        applications.Add(application);
    }

    internal DiameterPeerCapabilities CreateApplicationCapabilities(
        ApplicationRegistryEntry? excludedEntry)
    {
        DiameterPeerCapabilities result =
            new DiameterPeerCapabilities();
        int index = 0;
        while (index < EntryOrder.Count)
        {
            ApplicationRegistryEntry entry = EntryOrder[index];
            if (entry != excludedEntry)
            {
                AppendApplicationCapabilities(
                    result,
                    entry.Registration);
            }

            index = index + 1;
        }

        return result;
    }

    internal bool AreApplicationCapabilitiesEqual(
        DiameterPeerCapabilities first,
        DiameterPeerCapabilities second)
    {
        if (!AreApplicationIdentifiersEqual(
            first.SupportedVendorIds,
            second.SupportedVendorIds) ||
            !AreApplicationIdentifiersEqual(
                first.AuthApplicationIds,
                second.AuthApplicationIds) ||
            !AreApplicationIdentifiersEqual(
                first.AcctApplicationIds,
                second.AcctApplicationIds) ||
            first.VendorSpecificApplications.Count !=
                second.VendorSpecificApplications.Count)
        {
            return false;
        }

        int index = 0;
        while (index < first.VendorSpecificApplications.Count)
        {
            DiameterApplicationId firstApplication =
                first.VendorSpecificApplications[index];
            DiameterApplicationId secondApplication =
                second.VendorSpecificApplications[index];
            if (firstApplication.Id != secondApplication.Id ||
                firstApplication.VendorId != secondApplication.VendorId ||
                firstApplication.IsAccounting !=
                    secondApplication.IsAccounting)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool AreApplicationIdentifiersEqual(
        List<uint> first,
        List<uint> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        int index = 0;
        while (index < first.Count)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal ulong CreateApplicationDispatchKey(
        uint applicationId,
        uint commandCode,
        bool request)
    {
        ulong direction = request ? 1UL : 0UL;
        return ((ulong)applicationId << 25) |
            ((ulong)commandCode << 1) | direction;
    }
}
