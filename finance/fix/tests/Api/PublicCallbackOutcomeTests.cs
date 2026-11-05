using System.Collections.Generic;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class PublicCallbackOutcomeTests
{
    [Test]
    public void CrackHandlerMethodsExposeIndependentSuccessAndFailureDecisions()
    {
        RecordingCrackHandler implementation = new RecordingCrackHandler();
        IMessageCrackHandler handler = implementation;
        MessageCrackContext administrativeContext = new MessageCrackContext { Name = "Logon", IsAdministrative = true };
        MessageCrackContext applicationContext = new MessageCrackContext { Name = "Order", IsAdministrative = false };

        ApplicationDecision administrative = handler.HandleAdministrativeMessage(administrativeContext);
        ApplicationDecision application = handler.HandleApplicationMessage(applicationContext);

        Assert.Multiple(() =>
        {
            Assert.That(administrative.Disposition, Is.EqualTo(MessageDisposition.Accept));
            Assert.That(administrative.Code, Is.EqualTo("administrative-accepted"));
            Assert.That(application.Disposition, Is.EqualTo(MessageDisposition.Reject));
            Assert.That(application.Code, Is.EqualTo("application-rejected"));
            Assert.That(implementation.Contexts, Is.EqualTo(new[] { administrativeContext, applicationContext }));
        });
    }

    [Test]
    public void SessionApplicationMethodsExposeEveryCallbackAndDecisionOutcome()
    {
        RecordingSessionCallbacks implementation = new RecordingSessionCallbacks();
        ISessionApplication application = implementation;
        SessionId id = new SessionId { BeginString = FixBeginStrings.Fix44, SenderCompID = "LOCAL", TargetCompID = "REMOTE" };
        ApplicationMessageContext context = new ApplicationMessageContext { Id = id, Message = new FixMessage { MsgType = "D" } };

        ApplicationDecision created = application.OnSessionCreated(id);
        ApplicationDecision logon = application.OnSessionLogon(context);
        ApplicationDecision administrative = application.OnAdministrativeMessage(context);
        ApplicationDecision applicationMessage = application.OnApplicationMessage(context);
        ApplicationDecision replay = application.OnReplayMessage(context);
        ApplicationDecision logout = application.OnSessionLogout(context);

        Assert.Multiple(() =>
        {
            Assert.That(created.Disposition, Is.EqualTo(MessageDisposition.Accept));
            Assert.That(logon.Disposition, Is.EqualTo(MessageDisposition.Accept));
            Assert.That(administrative.Disposition, Is.EqualTo(MessageDisposition.Accept));
            Assert.That(applicationMessage.Disposition, Is.EqualTo(MessageDisposition.Reject));
            Assert.That(applicationMessage.Code, Is.EqualTo("application-rejected"));
            Assert.That(replay.Disposition, Is.EqualTo(MessageDisposition.Drop));
            Assert.That(replay.Code, Is.EqualTo("replay-dropped"));
            Assert.That(logout.Disposition, Is.EqualTo(MessageDisposition.Accept));
            Assert.That(implementation.Callbacks, Is.EqualTo(new[] { "created", "logon", "administrative", "application", "replay", "logout" }));
            Assert.That(id.SenderCompID, Is.EqualTo("LOCAL"));
            Assert.That(context.Message!.MsgType, Is.EqualTo("D"));
        });
    }

    private class RecordingCrackHandler : IMessageCrackHandler
    {
        internal List<MessageCrackContext> Contexts = [];

        public ApplicationDecision HandleAdministrativeMessage(MessageCrackContext context)
        {
            Contexts.Add(context);
            return Decision(MessageDisposition.Accept, "administrative-accepted");
        }

        public ApplicationDecision HandleApplicationMessage(MessageCrackContext context)
        {
            Contexts.Add(context);
            return Decision(MessageDisposition.Reject, "application-rejected");
        }
    }

    private class RecordingSessionCallbacks : ISessionApplication
    {
        internal List<string> Callbacks = [];

        public ApplicationDecision OnSessionCreated(SessionId id)
        {
            Callbacks.Add("created");
            return Decision(MessageDisposition.Accept, "created");
        }

        public ApplicationDecision OnSessionLogon(ApplicationMessageContext context)
        {
            Callbacks.Add("logon");
            return Decision(MessageDisposition.Accept, "logon");
        }

        public ApplicationDecision OnSessionLogout(ApplicationMessageContext context)
        {
            Callbacks.Add("logout");
            return Decision(MessageDisposition.Accept, "logout");
        }

        public ApplicationDecision OnAdministrativeMessage(ApplicationMessageContext context)
        {
            Callbacks.Add("administrative");
            return Decision(MessageDisposition.Accept, "administrative");
        }

        public ApplicationDecision OnApplicationMessage(ApplicationMessageContext context)
        {
            Callbacks.Add("application");
            return Decision(MessageDisposition.Reject, "application-rejected");
        }

        public ApplicationDecision OnReplayMessage(ApplicationMessageContext context)
        {
            Callbacks.Add("replay");
            return Decision(MessageDisposition.Drop, "replay-dropped");
        }
    }

    private static ApplicationDecision Decision(MessageDisposition disposition, string code)
    {
        return new ApplicationDecision { Disposition = disposition, Code = code };
    }
}
