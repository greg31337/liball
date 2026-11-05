using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class ApplicationInterceptorPipelineTests
{
    [Test]
    public void InboundPipelineOrdersByOrderThenOrdinalName()
    {
        List<string> log = new List<string>();
        PipelineInterceptor later =
            new PipelineInterceptor("zeta", 20, "continue", log);
        PipelineInterceptor beta =
            new PipelineInterceptor("beta", 10, "continue", log);
        PipelineInterceptor alpha =
            new PipelineInterceptor("alpha", 10, "continue", log);
        PipelineApplication application = new PipelineApplication();
        application.Interceptors.Add(later);
        application.Interceptors.Add(beta);
        application.Interceptors.Add(alpha);
        ApplicationInterceptorPipeline pipeline =
            new ApplicationInterceptorPipeline();

        DiameterApplicationResult result =
            pipeline.ProcessInboundApplicationInterceptors(
                application,
                CreateContext());

        Assert.Multiple(() =>
        {
            Assert.That(log,
                Is.EqualTo(new string[] { "alpha", "beta", "zeta" }));
            Assert.That(result.Continue, Is.True);
            Assert.That(result.Replace, Is.False);
            Assert.That(result.Code,
                Is.EqualTo("interceptor-pipeline-continued"));
            Assert.That(alpha.InboundCount, Is.EqualTo(1));
            Assert.That(alpha.OutboundCount, Is.Zero);
            Assert.That(alpha.LastContext!.Inbound, Is.True);
        });
    }

    [Test]
    public void ReplacementFlowsToLaterInterceptorsAndFinalCaller()
    {
        List<string> log = new List<string>();
        PipelineInterceptor replacement =
            new PipelineInterceptor("replace", 10, "replace", log);
        PipelineInterceptor observer =
            new PipelineInterceptor("observe", 20, "continue", log);
        PipelineApplication application = new PipelineApplication();
        application.Interceptors.Add(observer);
        application.Interceptors.Add(replacement);
        DiameterInterceptorContext input = CreateContext();
        ApplicationInterceptorPipeline pipeline =
            new ApplicationInterceptorPipeline();

        DiameterApplicationResult result =
            pipeline.ProcessInboundApplicationInterceptors(
                application,
                input);
        replacement.LastOutput!.ApplicationId = 99U;
        observer.LastContext!.Message.ApplicationId = 98U;

        Assert.Multiple(() =>
        {
            Assert.That(log,
                Is.EqualTo(new string[] { "replace", "observe" }));
            Assert.That(observer.SeenApplicationId, Is.EqualTo(11U));
            Assert.That(observer.SeenPreviousReplace, Is.True);
            Assert.That(result.Continue, Is.False);
            Assert.That(result.Replace, Is.True);
            Assert.That(result.OutputMessage!.ApplicationId,
                Is.EqualTo(11U));
            Assert.That(input.Message.ApplicationId, Is.EqualTo(10U));
        });
    }

    [Test]
    public void TerminalOutcomesShortCircuitRemainingInterceptors()
    {
        string[] behaviors = new string[]
        {
            "answer",
            "drop",
            "redirect",
            "fail"
        };
        int index = 0;
        while (index < behaviors.Length)
        {
            List<string> log = new List<string>();
            PipelineInterceptor terminal = new PipelineInterceptor(
                "terminal",
                10,
                behaviors[index],
                log);
            PipelineInterceptor skipped = new PipelineInterceptor(
                "skipped",
                20,
                "continue",
                log);
            PipelineApplication application = new PipelineApplication();
            application.Interceptors.Add(skipped);
            application.Interceptors.Add(terminal);
            ApplicationInterceptorPipeline pipeline =
                new ApplicationInterceptorPipeline();

            DiameterApplicationResult result =
                pipeline.ProcessInboundApplicationInterceptors(
                    application,
                    CreateContext());

            Assert.That(log, Is.EqualTo(new string[] { "terminal" }));
            Assert.That(skipped.InboundCount, Is.Zero);
            Assert.That(result.InterceptorContext, Is.Not.Null);
            if (behaviors[index] == "answer")
            {
                Assert.That(result.Answer, Is.True);
                Assert.That(result.OutputMessage!.Flags,
                    Is.EqualTo(DiameterMessageFlags.Proxyable));
            }
            else if (behaviors[index] == "drop")
            {
                Assert.That(result.Drop, Is.True);
            }
            else if (behaviors[index] == "redirect")
            {
                Assert.That(result.Redirect, Is.True);
                Assert.That(result.RedirectHosts, Has.Count.EqualTo(1));
                Assert.That(result.RedirectHosts[0].Host.Value,
                    Is.EqualTo("redirect.example"));
            }
            else
            {
                Assert.That(result.Fail, Is.True);
                Assert.That(result.ResultCode,
                    Is.EqualTo(DiameterResultCodes.TooBusy));
            }

            index = index + 1;
        }
    }

    [Test]
    public void OutboundPipelineCallsOnlyOutboundOperations()
    {
        List<string> log = new List<string>();
        PipelineInterceptor interceptor =
            new PipelineInterceptor("outbound", 10, "continue", log);
        PipelineApplication application = new PipelineApplication();
        application.Interceptors.Add(interceptor);
        ApplicationInterceptorPipeline pipeline =
            new ApplicationInterceptorPipeline();

        DiameterApplicationResult result =
            pipeline.ProcessOutboundApplicationInterceptors(
                application,
                CreateContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Continue, Is.True);
            Assert.That(interceptor.InboundCount, Is.Zero);
            Assert.That(interceptor.OutboundCount, Is.EqualTo(1));
            Assert.That(interceptor.LastContext!.Inbound, Is.False);
        });
    }

    [Test]
    public void InvalidInterceptorResultBecomesFailureAndStopsPipeline()
    {
        List<string> log = new List<string>();
        PipelineInterceptor invalid =
            new PipelineInterceptor("invalid", 10, "invalid", log);
        PipelineInterceptor skipped =
            new PipelineInterceptor("skipped", 20, "continue", log);
        PipelineApplication application = new PipelineApplication();
        application.Interceptors.Add(skipped);
        application.Interceptors.Add(invalid);
        ApplicationInterceptorPipeline pipeline =
            new ApplicationInterceptorPipeline();

        DiameterApplicationResult result =
            pipeline.ProcessInboundApplicationInterceptors(
                application,
                CreateContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Fail, Is.True);
            Assert.That(result.ResultCode,
                Is.EqualTo(DiameterResultCodes.UnableToComply));
            Assert.That(result.Code,
                Is.EqualTo("interceptor-result-invalid"));
            Assert.That(skipped.InboundCount, Is.Zero);
        });
    }

    [Test]
    public void ContinueCannotMutateMessageSeenByLaterInterceptors()
    {
        List<string> log = new List<string>();
        PipelineInterceptor mutating =
            new PipelineInterceptor("mutating", 10, "mutate", log);
        PipelineInterceptor observer =
            new PipelineInterceptor("observer", 20, "continue", log);
        PipelineApplication application = new PipelineApplication();
        application.Interceptors.Add(mutating);
        application.Interceptors.Add(observer);
        DiameterInterceptorContext input = CreateContext();
        ApplicationInterceptorPipeline pipeline =
            new ApplicationInterceptorPipeline();

        pipeline.ProcessInboundApplicationInterceptors(application, input);

        Assert.Multiple(() =>
        {
            Assert.That(mutating.LastContext!.Message.CommandCode,
                Is.EqualTo(999U));
            Assert.That(observer.SeenCommandCode, Is.EqualTo(300U));
            Assert.That(input.Message.CommandCode, Is.EqualTo(300U));
        });
    }

    [Test]
    public void InvalidOrDuplicateInterceptorMetadataIsRejectedBeforeCalls()
    {
        List<string> log = new List<string>();
        PipelineInterceptor first =
            new PipelineInterceptor("duplicate", 10, "continue", log);
        PipelineInterceptor duplicate =
            new PipelineInterceptor("DUPLICATE", 20, "continue", log);
        PipelineApplication duplicated = new PipelineApplication();
        duplicated.Interceptors.Add(first);
        duplicated.Interceptors.Add(duplicate);
        PipelineInterceptor invalidOrder =
            new PipelineInterceptor("invalid", -1, "continue", log);
        PipelineApplication invalid = new PipelineApplication();
        invalid.Interceptors.Add(invalidOrder);
        ApplicationInterceptorPipeline pipeline =
            new ApplicationInterceptorPipeline();

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterApplicationException>(() =>
                pipeline.ProcessInboundApplicationInterceptors(
                    duplicated,
                    CreateContext()));
            Assert.Throws<ArgumentException>(() =>
                pipeline.ProcessInboundApplicationInterceptors(
                    invalid,
                    CreateContext()));
            Assert.That(first.InboundCount, Is.Zero);
            Assert.That(duplicate.InboundCount, Is.Zero);
            Assert.That(invalidOrder.InboundCount, Is.Zero);
        });
    }

    private static DiameterInterceptorContext CreateContext()
    {
        DiameterInterceptorContext context =
            new DiameterInterceptorContext();
        context.Message.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        context.Message.CommandCode = 300U;
        context.Message.ApplicationId = 10U;
        context.Message.HopByHopIdentifier = 20U;
        context.Message.EndToEndIdentifier = 30U;
        context.PeerIdentity = new DiameterIdentity();
        context.PeerIdentity.Value = "peer.example";
        context.RouteIdentifier = "local-route";
        context.SessionId = "peer.example;1";
        context.InvokedAt = DateTimeOffset.FromUnixTimeSeconds(1000);
        return context;
    }

    private sealed class PipelineApplication : IDiameterApplication
    {
        public List<IMessageInterceptor> Interceptors =
            new List<IMessageInterceptor>();

        public DiameterApplicationRegistration
            ReadDiameterApplicationRegistration()
        {
            DiameterApplicationRegistration registration =
                new DiameterApplicationRegistration();
            registration.Name = "pipeline";
            DiameterCommandRegistration command =
                new DiameterCommandRegistration();
            command.CommandCode = 300U;
            command.Name = "Pipeline-Command";
            registration.Commands.Add(command);
            return registration;
        }

        public IDiameterRequestHandler?
            FindDiameterApplicationRequestHandler(uint commandCode)
        {
            return null;
        }

        public List<IMessageInterceptor>
            ReadDiameterApplicationInterceptors()
        {
            return Interceptors;
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

    private sealed class PipelineInterceptor : IMessageInterceptor
    {
        public string Name;
        public int Order;
        public string Behavior;
        public List<string> Log;
        public int InboundCount;
        public int OutboundCount;
        public DiameterInterceptorContext? LastContext;
        public DiameterMessage? LastOutput;
        public uint SeenApplicationId;
        public uint SeenCommandCode;
        public bool SeenPreviousReplace;

        public PipelineInterceptor(
            string name,
            int order,
            string behavior,
            List<string> log)
        {
            Name = name;
            Order = order;
            Behavior = behavior;
            Log = log;
        }

        public string ReadDiameterMessageInterceptorName()
        {
            return Name;
        }

        public int ReadDiameterMessageInterceptorOrder()
        {
            return Order;
        }

        public DiameterApplicationResult InterceptInboundDiameterMessage(
            DiameterInterceptorContext context)
        {
            InboundCount = InboundCount + 1;
            return Process(context);
        }

        public DiameterApplicationResult InterceptOutboundDiameterMessage(
            DiameterInterceptorContext context)
        {
            OutboundCount = OutboundCount + 1;
            return Process(context);
        }

        private DiameterApplicationResult Process(
            DiameterInterceptorContext context)
        {
            LastContext = context;
            SeenApplicationId = context.Message.ApplicationId;
            SeenCommandCode = context.Message.CommandCode;
            SeenPreviousReplace = context.PreviousResult != null &&
                context.PreviousResult.Replace;
            Log.Add(Name);
            if (Behavior == "replace")
            {
                DiameterMessage replacement = CloneMessage(context.Message);
                replacement.ApplicationId = 11U;
                LastOutput = replacement;
                DiameterApplicationResult result = CreateResult();
                result.Replace = true;
                result.OutputMessage = replacement;
                return result;
            }

            if (Behavior == "answer")
            {
                DiameterMessage answer = CloneMessage(context.Message);
                answer.Flags = answer.Flags &
                    DiameterMessageFlags.Proxyable;
                LastOutput = answer;
                DiameterApplicationResult result = CreateResult();
                result.Answer = true;
                result.OutputMessage = answer;
                return result;
            }

            if (Behavior == "drop")
            {
                DiameterApplicationResult result = CreateResult();
                result.Drop = true;
                return result;
            }

            if (Behavior == "redirect")
            {
                DiameterApplicationResult result = CreateResult();
                result.Redirect = true;
                DiameterUri redirect = new DiameterUri();
                redirect.Host.Value = "redirect.example";
                result.RedirectHosts.Add(redirect);
                return result;
            }

            if (Behavior == "fail")
            {
                DiameterApplicationResult result = CreateResult();
                result.Fail = true;
                result.ResultCode = DiameterResultCodes.TooBusy;
                return result;
            }

            if (Behavior == "invalid")
            {
                DiameterApplicationResult result =
                    new DiameterApplicationResult();
                result.Drop = true;
                return result;
            }

            if (Behavior == "mutate")
            {
                context.Message.CommandCode = 999U;
            }

            DiameterApplicationResult continued =
                new DiameterApplicationResult();
            continued.Code = Name + "-continued";
            return continued;
        }

        private static DiameterApplicationResult CreateResult()
        {
            DiameterApplicationResult result =
                new DiameterApplicationResult();
            result.Continue = false;
            return result;
        }

        private static DiameterMessage CloneMessage(DiameterMessage source)
        {
            DiameterMessage result = new DiameterMessage();
            result.Version = source.Version;
            result.Flags = source.Flags;
            result.CommandCode = source.CommandCode;
            result.ApplicationId = source.ApplicationId;
            result.HopByHopIdentifier = source.HopByHopIdentifier;
            result.EndToEndIdentifier = source.EndToEndIdentifier;
            return result;
        }
    }
}
