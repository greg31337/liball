using System.Collections.Generic;

namespace diameter.Tests;

public class ApplicationContractTests
{
    [Test]
    public void ApplicationProvidesRegistrationHandlersAndLifecycle()
    {
        RecordingRequestHandler handler = new RecordingRequestHandler();
        RecordingInterceptor interceptor = new RecordingInterceptor();
        TestApplication implementation =
            new TestApplication(handler, interceptor);
        IDiameterApplication application = implementation;

        DiameterApplicationRegistration registration =
            application.ReadDiameterApplicationRegistration();
        IDiameterRequestHandler? found =
            application.FindDiameterApplicationRequestHandler(300U);
        IDiameterRequestHandler? missing =
            application.FindDiameterApplicationRequestHandler(301U);
        List<IMessageInterceptor> interceptors =
            application.ReadDiameterApplicationInterceptors();
        application.StartDiameterApplication();
        application.DrainDiameterApplication();
        application.StopDiameterApplication();

        Assert.Multiple(() =>
        {
            Assert.That(registration.Name, Is.EqualTo("sample-application"));
            Assert.That(registration.ApplicationId, Is.EqualTo(16777216U));
            Assert.That(found, Is.SameAs(handler));
            Assert.That(missing, Is.Null);
            Assert.That(interceptors, Has.Count.EqualTo(1));
            Assert.That(interceptors[0], Is.SameAs(interceptor));
            Assert.That(implementation.StartCount, Is.EqualTo(1));
            Assert.That(implementation.DrainCount, Is.EqualTo(1));
            Assert.That(implementation.StopCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RequestHandlerReceivesContextAndReturnsDecision()
    {
        RecordingRequestHandler implementation =
            new RecordingRequestHandler();
        IDiameterRequestHandler handler = implementation;
        DiameterRequestContext context = new DiameterRequestContext();
        context.SessionId = "peer.example;1";

        DiameterApplicationResult result =
            handler.HandleDiameterApplicationRequest(context);

        Assert.Multiple(() =>
        {
            Assert.That(implementation.LastContext, Is.SameAs(context));
            Assert.That(result.RequestContext, Is.SameAs(context));
            Assert.That(result.Continue, Is.False);
            Assert.That(result.Drop, Is.True);
            Assert.That(result.Code, Is.EqualTo("handled"));
        });
    }

    [Test]
    public void InterceptorExposesOrderAndProcessesBothDirections()
    {
        RecordingInterceptor implementation = new RecordingInterceptor();
        IMessageInterceptor interceptor = implementation;
        DiameterInterceptorContext inbound =
            new DiameterInterceptorContext();
        inbound.Inbound = true;
        DiameterInterceptorContext outbound =
            new DiameterInterceptorContext();
        outbound.Inbound = false;

        DiameterApplicationResult inboundResult =
            interceptor.InterceptInboundDiameterMessage(inbound);
        DiameterApplicationResult outboundResult =
            interceptor.InterceptOutboundDiameterMessage(outbound);

        Assert.Multiple(() =>
        {
            Assert.That(interceptor.ReadDiameterMessageInterceptorName(),
                Is.EqualTo("recording"));
            Assert.That(interceptor.ReadDiameterMessageInterceptorOrder(),
                Is.EqualTo(20));
            Assert.That(implementation.InboundContext, Is.SameAs(inbound));
            Assert.That(implementation.OutboundContext, Is.SameAs(outbound));
            Assert.That(inboundResult.InterceptorContext, Is.SameAs(inbound));
            Assert.That(outboundResult.InterceptorContext, Is.SameAs(outbound));
            Assert.That(implementation.InboundCount, Is.EqualTo(1));
            Assert.That(implementation.OutboundCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ApplicationHostRegistersFindsReadsAndRemovesModules()
    {
        RecordingRequestHandler handler = new RecordingRequestHandler();
        RecordingInterceptor interceptor = new RecordingInterceptor();
        IDiameterApplication application =
            new TestApplication(handler, interceptor);
        TestApplicationHost implementation = new TestApplicationHost();
        IDiameterApplicationHost host = implementation;

        DiameterApplicationRegistration registered =
            host.RegisterDiameterApplication(application);
        DiameterApplicationRegistration? found =
            host.FindDiameterApplicationRegistration(
                16777216U,
                300U,
                true);
        DiameterApplicationRegistration? missing =
            host.FindDiameterApplicationRegistration(
                16777216U,
                300U,
                false);
        List<DiameterApplicationRegistration> registrations =
            host.ReadDiameterApplicationRegistrations();
        bool removed = host.UnregisterDiameterApplication(
            "sample-application",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(registered.Name, Is.EqualTo("sample-application"));
            Assert.That(found, Is.SameAs(registered));
            Assert.That(missing, Is.Null);
            Assert.That(registrations, Has.Count.EqualTo(1));
            Assert.That(registrations[0], Is.SameAs(registered));
            Assert.That(removed, Is.True);
            Assert.That(implementation.DrainRequested, Is.True);
            Assert.That(host.ReadDiameterApplicationRegistrations(), Is.Empty);
        });
    }

    private sealed class RecordingRequestHandler : IDiameterRequestHandler
    {
        public DiameterRequestContext? LastContext;

        public DiameterApplicationResult HandleDiameterApplicationRequest(
            DiameterRequestContext context)
        {
            LastContext = context;
            DiameterApplicationResult result =
                new DiameterApplicationResult();
            result.Continue = false;
            result.Drop = true;
            result.RequestContext = context;
            result.Code = "handled";
            return result;
        }
    }

    private sealed class RecordingInterceptor : IMessageInterceptor
    {
        public DiameterInterceptorContext? InboundContext;

        public DiameterInterceptorContext? OutboundContext;

        public int InboundCount;

        public int OutboundCount;

        public string ReadDiameterMessageInterceptorName()
        {
            return "recording";
        }

        public int ReadDiameterMessageInterceptorOrder()
        {
            return 20;
        }

        public DiameterApplicationResult InterceptInboundDiameterMessage(
            DiameterInterceptorContext context)
        {
            InboundContext = context;
            InboundCount = InboundCount + 1;
            DiameterApplicationResult result =
                new DiameterApplicationResult();
            result.InterceptorContext = context;
            return result;
        }

        public DiameterApplicationResult InterceptOutboundDiameterMessage(
            DiameterInterceptorContext context)
        {
            OutboundContext = context;
            OutboundCount = OutboundCount + 1;
            DiameterApplicationResult result =
                new DiameterApplicationResult();
            result.InterceptorContext = context;
            return result;
        }
    }

    private sealed class TestApplication : IDiameterApplication
    {
        private readonly RecordingRequestHandler Handler;

        private readonly RecordingInterceptor Interceptor;

        public int StartCount;

        public int DrainCount;

        public int StopCount;

        public TestApplication(
            RecordingRequestHandler handler,
            RecordingInterceptor interceptor)
        {
            Handler = handler;
            Interceptor = interceptor;
        }

        public DiameterApplicationRegistration
            ReadDiameterApplicationRegistration()
        {
            DiameterApplicationRegistration registration =
                new DiameterApplicationRegistration();
            registration.Name = "sample-application";
            registration.ApplicationId = 16777216U;
            registration.Commands.Add(CreateCommand());
            return registration;
        }

        public IDiameterRequestHandler?
            FindDiameterApplicationRequestHandler(uint commandCode)
        {
            if (commandCode == 300U)
            {
                return Handler;
            }

            return null;
        }

        public List<IMessageInterceptor>
            ReadDiameterApplicationInterceptors()
        {
            return new List<IMessageInterceptor> { Interceptor };
        }

        public void StartDiameterApplication()
        {
            StartCount = StartCount + 1;
        }

        public void DrainDiameterApplication()
        {
            DrainCount = DrainCount + 1;
        }

        public void StopDiameterApplication()
        {
            StopCount = StopCount + 1;
        }

        private static DiameterCommandRegistration CreateCommand()
        {
            DiameterCommandRegistration command =
                new DiameterCommandRegistration();
            command.CommandCode = 300U;
            command.Name = "Sample-Command";
            return command;
        }
    }

    private sealed class TestApplicationHost : IDiameterApplicationHost
    {
        private IDiameterApplication? Application;

        private DiameterApplicationRegistration? Registration;

        public bool DrainRequested;

        public DiameterApplicationRegistration RegisterDiameterApplication(
            IDiameterApplication? application)
        {
            if (application == null)
            {
                throw new DiameterApplicationException("missing application");
            }

            Application = application;
            Registration =
                application.ReadDiameterApplicationRegistration();
            application.StartDiameterApplication();
            return Registration;
        }

        public bool UnregisterDiameterApplication(
            string? registrationName,
            bool drain)
        {
            if (Registration == null || Application == null ||
                Registration.Name != registrationName)
            {
                return false;
            }

            DrainRequested = drain;
            if (drain)
            {
                Application.DrainDiameterApplication();
            }

            Application.StopDiameterApplication();
            Application = null;
            Registration = null;
            return true;
        }

        public DiameterApplicationRegistration?
            FindDiameterApplicationRegistration(
                uint applicationId,
                uint commandCode,
                bool request)
        {
            if (Registration == null ||
                Registration.ApplicationId != applicationId)
            {
                return null;
            }

            int index = 0;
            while (index < Registration.Commands.Count)
            {
                DiameterCommandRegistration command =
                    Registration.Commands[index];
                if (command.CommandCode == commandCode &&
                    command.IsRequest == request)
                {
                    return Registration;
                }

                index = index + 1;
            }

            return null;
        }

        public List<DiameterApplicationRegistration>
            ReadDiameterApplicationRegistrations()
        {
            if (Registration == null)
            {
                return new List<DiameterApplicationRegistration>();
            }

            return new List<DiameterApplicationRegistration>
            {
                Registration
            };
        }
    }
}
