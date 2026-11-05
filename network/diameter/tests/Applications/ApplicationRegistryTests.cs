using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class ApplicationRegistryTests
{
    [Test]
    public void RegistrationAtomicallyIndexesDirectionsAndOwnsMetadata()
    {
        ApplicationRegistry registry = new ApplicationRegistry();
        RegistryApplication application = CreateApplication(
            "sample",
            16777216U,
            10415U,
            300U,
            true);
        application.Registration.Commands.Add(CreateCommand(300U, false));
        application.Registration.Commands.Add(CreateCommand(301U, true));
        DiameterDictionaryModule dictionary = new DiameterDictionaryModule();
        dictionary.Name = "sample-dictionary";
        DiameterVendorDefinition vendor = new DiameterVendorDefinition();
        vendor.Id = 10415U;
        vendor.Name = "Sample Vendor";
        dictionary.Vendors.Add(vendor);
        application.Registration.DictionaryModules.Add(dictionary);

        DiameterApplicationRegistration registered =
            registry.RegisterApplicationModule(application);
        application.Registration.Name = "changed";
        application.Registration.Commands[0].CommandCode = 999U;
        application.Registration.DictionaryModules[0].Vendors[0].Name =
            "Changed Vendor";
        registered.Name = "changed-result";
        registered.Commands[0].Name = "Changed-Command";

        ApplicationRegistryEntry? request = registry.ReadApplicationEntry(
            16777216U,
            300U,
            true);
        ApplicationRegistryEntry? answer = registry.ReadApplicationEntry(
            16777216U,
            300U,
            false);
        ApplicationRegistryEntry? second = registry.ReadApplicationEntry(
            16777216U,
            301U,
            true);
        List<DiameterApplicationRegistration> registrations =
            registry.ReadApplicationRegistrations();
        registrations[0].Commands[0].CommandCode = 998U;
        List<DiameterApplicationRegistration> retained =
            registry.ReadApplicationRegistrations();

        Assert.Multiple(() =>
        {
            Assert.That(request, Is.Not.Null);
            Assert.That(answer, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(request!.Application, Is.SameAs(application));
            Assert.That(answer!.Application, Is.SameAs(application));
            Assert.That(second!.Application, Is.SameAs(application));
            Assert.That(registry.ReadApplicationEntry(
                16777216U,
                301U,
                false), Is.Null);
            Assert.That(retained[0].Name, Is.EqualTo("sample"));
            Assert.That(retained[0].Commands[0].CommandCode,
                Is.EqualTo(300U));
            Assert.That(retained[0].Commands[0].Name,
                Is.EqualTo("Sample-Command"));
            Assert.That(retained[0].DictionaryModules[0].Vendors[0].Name,
                Is.EqualTo("Sample Vendor"));
            Assert.That(registry.ReadApplicationCapabilityRevision(),
                Is.EqualTo(1L));
        });
    }

    [Test]
    public void ConflictsAreDeterministicAndLeaveRegistryUnchanged()
    {
        ApplicationRegistry registry = new ApplicationRegistry();
        registry.RegisterApplicationModule(CreateApplication(
            "first",
            10U,
            0U,
            300U,
            true));
        RegistryApplication nameConflict = CreateApplication(
            "FIRST",
            11U,
            0U,
            301U,
            true);
        RegistryApplication dispatchConflict = CreateApplication(
            "dispatch-conflict",
            10U,
            0U,
            300U,
            true);
        dispatchConflict.Registration.Commands.Add(CreateCommand(302U, true));

        DiameterApplicationException nameException =
            Assert.Throws<DiameterApplicationException>(() =>
                registry.RegisterApplicationModule(nameConflict))!;
        DiameterApplicationException dispatchException =
            Assert.Throws<DiameterApplicationException>(() =>
                registry.RegisterApplicationModule(dispatchConflict))!;

        Assert.Multiple(() =>
        {
            Assert.That(nameException.Message,
                Is.EqualTo(
                    "An active application uses registration name 'FIRST'."));
            Assert.That(dispatchException.Message,
                Is.EqualTo(
                    "Application-Id 10, Command-Code 300, and request direction are already registered by 'first'."));
            Assert.That(registry.ReadApplicationCount(), Is.EqualTo(1));
            Assert.That(registry.ReadApplicationCapabilityRevision(),
                Is.Zero);
            Assert.That(registry.ReadApplicationEntry(10U, 302U, true),
                Is.Null);
        });
    }

    [Test]
    public void CapabilitySnapshotsTrackDeduplicationAndRemoval()
    {
        ApplicationRegistry registry = new ApplicationRegistry();
        RegistryApplication standardAuth = CreateApplication(
            "standard-auth",
            4U,
            0U,
            272U,
            true);
        standardAuth.Registration.AdvertiseAuthentication = true;
        RegistryApplication splitAuth = CreateApplication(
            "split-auth",
            4U,
            0U,
            273U,
            true);
        splitAuth.Registration.AdvertiseAuthentication = true;
        RegistryApplication standardAccounting = CreateApplication(
            "standard-accounting",
            3U,
            0U,
            271U,
            true);
        standardAccounting.Registration.AdvertiseAccounting = true;
        RegistryApplication vendor = CreateApplication(
            "vendor",
            16777216U,
            10415U,
            300U,
            true);
        vendor.Registration.AdvertiseAuthentication = true;
        vendor.Registration.AdvertiseAccounting = true;
        RegistryApplication vendorOnly = CreateApplication(
            "vendor-only",
            16777217U,
            999U,
            301U,
            true);
        RegistryApplication otherVendor = CreateApplication(
            "other-vendor",
            16777216U,
            222U,
            302U,
            true);
        otherVendor.Registration.AdvertiseAuthentication = true;

        registry.RegisterApplicationModule(standardAuth);
        registry.RegisterApplicationModule(splitAuth);
        registry.RegisterApplicationModule(standardAccounting);
        registry.RegisterApplicationModule(vendor);
        registry.RegisterApplicationModule(vendorOnly);
        registry.RegisterApplicationModule(otherVendor);
        DiameterPeerCapabilities capabilities =
            registry.ReadApplicationCapabilities();
        capabilities.AuthApplicationIds.Add(99U);
        capabilities.VendorSpecificApplications[0].Id = 99U;
        DiameterPeerCapabilities retained =
            registry.ReadApplicationCapabilities();

        registry.RemoveApplicationModule("standard-auth");
        DiameterPeerCapabilities afterFirstRemoval =
            registry.ReadApplicationCapabilities();
        registry.RemoveApplicationModule("split-auth");
        registry.RemoveApplicationModule("vendor");
        DiameterPeerCapabilities afterLastRemoval =
            registry.ReadApplicationCapabilities();

        Assert.Multiple(() =>
        {
            Assert.That(retained.AuthApplicationIds,
                Is.EqualTo(new uint[] { 4U }));
            Assert.That(retained.AcctApplicationIds,
                Is.EqualTo(new uint[] { 3U }));
            Assert.That(retained.SupportedVendorIds,
                Is.EqualTo(new uint[] { 10415U, 999U, 222U }));
            Assert.That(retained.VendorSpecificApplications,
                Has.Count.EqualTo(3));
            Assert.That(retained.VendorSpecificApplications[0].Id,
                Is.EqualTo(16777216U));
            Assert.That(retained.VendorSpecificApplications[0].IsAccounting,
                Is.False);
            Assert.That(retained.VendorSpecificApplications[1].IsAccounting,
                Is.True);
            Assert.That(retained.VendorSpecificApplications[2].VendorId,
                Is.EqualTo(222U));
            Assert.That(afterFirstRemoval.AuthApplicationIds,
                Is.EqualTo(new uint[] { 4U }));
            Assert.That(afterLastRemoval.AuthApplicationIds, Is.Empty);
            Assert.That(afterLastRemoval.AcctApplicationIds,
                Is.EqualTo(new uint[] { 3U }));
            Assert.That(afterLastRemoval.SupportedVendorIds,
                Is.EqualTo(new uint[] { 999U, 222U }));
            Assert.That(afterLastRemoval.VendorSpecificApplications,
                Has.Count.EqualTo(1));
            Assert.That(registry.ReadApplicationCapabilityRevision(),
                Is.EqualTo(7L));
        });
    }

    [Test]
    public void RemovalRetractsEveryDispatchKeyAndReturnsModule()
    {
        ApplicationRegistry registry = new ApplicationRegistry();
        RegistryApplication application = CreateApplication(
            "removable",
            20U,
            0U,
            300U,
            true);
        application.Registration.Commands.Add(CreateCommand(300U, false));
        registry.RegisterApplicationModule(application);

        ApplicationRegistryEntry? removed =
            registry.RemoveApplicationModule("REMOVABLE");
        removed!.Registration.Name = "changed-result";
        ApplicationRegistryEntry? missing =
            registry.RemoveApplicationModule("removable");

        Assert.Multiple(() =>
        {
            Assert.That(removed.Application, Is.SameAs(application));
            Assert.That(registry.ReadApplicationEntry(20U, 300U, true),
                Is.Null);
            Assert.That(registry.ReadApplicationEntry(20U, 300U, false),
                Is.Null);
            Assert.That(registry.ReadApplicationRegistrations(), Is.Empty);
            Assert.That(missing, Is.Null);
            Assert.That(registry.RemoveApplicationModule(null), Is.Null);
            Assert.That(registry.ReadApplicationCapabilityRevision(),
                Is.Zero);
        });
    }

    [Test]
    public void ConcurrentRegistrationAllowsOnlyOneDispatchOwner()
    {
        ApplicationRegistry registry = new ApplicationRegistry();
        int successes = 0;
        int conflicts = 0;
        Task[] tasks = new Task[16];
        int index = 0;
        while (index < tasks.Length)
        {
            int applicationNumber = index;
            tasks[index] = Task.Run(() =>
            {
                RegistryApplication application = CreateApplication(
                    "concurrent-" + applicationNumber,
                    30U,
                    0U,
                    400U,
                    true);
                try
                {
                    registry.RegisterApplicationModule(application);
                    Interlocked.Increment(ref successes);
                }
                catch (DiameterApplicationException)
                {
                    Interlocked.Increment(ref conflicts);
                }
            });
            index = index + 1;
        }

        Task.WaitAll(tasks);

        Assert.Multiple(() =>
        {
            Assert.That(successes, Is.EqualTo(1));
            Assert.That(conflicts, Is.EqualTo(15));
            Assert.That(registry.ReadApplicationCount(), Is.EqualTo(1));
            Assert.That(registry.ReadApplicationEntry(30U, 400U, true),
                Is.Not.Null);
            Assert.That(registry.ReadApplicationCapabilityRevision(),
                Is.Zero);
        });
    }

    [Test]
    public void InvalidDictionaryMetadataIsRejectedBeforeMutation()
    {
        ApplicationRegistry registry = new ApplicationRegistry();
        RegistryApplication application = CreateApplication(
            "invalid-dictionary",
            40U,
            0U,
            500U,
            true);
        DiameterDictionaryModule dictionary = new DiameterDictionaryModule();
        dictionary.Name = "invalid";
        dictionary.Commands = null!;
        application.Registration.DictionaryModules.Add(dictionary);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                registry.RegisterApplicationModule(application));
            Assert.That(registry.ReadApplicationCount(), Is.Zero);
            Assert.That(registry.ReadApplicationCapabilityRevision(),
                Is.Zero);
        });
    }

    private static RegistryApplication CreateApplication(
        string name,
        uint applicationId,
        uint vendorId,
        uint commandCode,
        bool request)
    {
        DiameterApplicationRegistration registration =
            new DiameterApplicationRegistration();
        registration.Name = name;
        registration.ApplicationId = applicationId;
        registration.VendorId = vendorId;
        registration.Commands.Add(CreateCommand(commandCode, request));
        return new RegistryApplication(registration);
    }

    private static DiameterCommandRegistration CreateCommand(
        uint commandCode,
        bool request)
    {
        DiameterCommandRegistration command =
            new DiameterCommandRegistration();
        command.CommandCode = commandCode;
        command.Name = "Sample-Command";
        command.IsRequest = request;
        return command;
    }

    private sealed class RegistryApplication : IDiameterApplication
    {
        public DiameterApplicationRegistration Registration;

        public RegistryApplication(
            DiameterApplicationRegistration registration)
        {
            Registration = registration;
        }

        public DiameterApplicationRegistration
            ReadDiameterApplicationRegistration()
        {
            return Registration;
        }

        public IDiameterRequestHandler?
            FindDiameterApplicationRequestHandler(uint commandCode)
        {
            return null;
        }

        public List<IMessageInterceptor>
            ReadDiameterApplicationInterceptors()
        {
            return new List<IMessageInterceptor>();
        }

        public void StartDiameterApplication()
        {
        }

        public void DrainDiameterApplication()
        {
        }

        public void StopDiameterApplication()
        {
        }
    }
}
