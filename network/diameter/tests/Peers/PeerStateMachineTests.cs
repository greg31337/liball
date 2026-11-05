using System.Threading;

namespace diameter.Tests;

internal class PeerStateMachineTests
{
    [TestCaseSource(nameof(ReadDefinedPeerTransitions))]
    public void DefinedEventsAdvanceStateAndReturnOrderedRfcActions(
        DiameterPeerState initialState,
        PeerStateMachineEvent peerEvent,
        DiameterPeerState expectedState,
        PeerStateMachineAction[] expectedActions)
    {
        PeerStateMachine machine = new PeerStateMachine(initialState);

        PeerStateTransition result = machine.HandlePeerStateEvent(peerEvent);

        Assert.Multiple(() =>
        {
            Assert.That(result.Defined, Is.True);
            Assert.That(result.PreviousState, Is.EqualTo(initialState));
            Assert.That(result.NextState, Is.EqualTo(expectedState));
            Assert.That(result.Event, Is.EqualTo(peerEvent));
            Assert.That(result.Actions, Is.EqualTo(expectedActions));
            Assert.That(result.Code, Is.EqualTo("transition-defined"));
            Assert.That(machine.ReadPeerState(), Is.EqualTo(expectedState));
        });
    }

    [TestCase(DiameterPeerState.Closed, PeerStateMachineEvent.Stop)]
    [TestCase(
        DiameterPeerState.WaitConnectionAcknowledgement,
        PeerStateMachineEvent.SendMessage)]
    [TestCase(
        DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
        PeerStateMachineEvent.ElectionWon)]
    [TestCase(
        DiameterPeerState.WaitConnectionAcknowledgementElection,
        PeerStateMachineEvent.InitiatorCapabilitiesAnswerReceived)]
    [TestCase(DiameterPeerState.WaitReturns, PeerStateMachineEvent.Stop)]
    [TestCase(
        DiameterPeerState.ResponderOpen,
        PeerStateMachineEvent.InitiatorMessageReceived)]
    [TestCase(
        DiameterPeerState.InitiatorOpen,
        PeerStateMachineEvent.ResponderMessageReceived)]
    [TestCase(DiameterPeerState.Closing, PeerStateMachineEvent.SendMessage)]
    public void UndefinedEventsLeaveStateUnchanged(
        DiameterPeerState initialState,
        PeerStateMachineEvent peerEvent)
    {
        PeerStateMachine machine = new PeerStateMachine(initialState);

        PeerStateTransition result = machine.HandlePeerStateEvent(peerEvent);

        Assert.Multiple(() =>
        {
            Assert.That(result.Defined, Is.False);
            Assert.That(result.PreviousState, Is.EqualTo(initialState));
            Assert.That(result.NextState, Is.EqualTo(initialState));
            Assert.That(result.Event, Is.EqualTo(peerEvent));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Code, Is.EqualTo("event-not-defined"));
            Assert.That(machine.ReadPeerState(), Is.EqualTo(initialState));
        });
    }

    [Test]
    public void EveryRfcStateEventCellIsClassifiedExactlyOnce()
    {
        int cellCount = 0;
        int definedCount = 0;
        foreach (DiameterPeerState state in Enum.GetValues<DiameterPeerState>())
        {
            foreach (PeerStateMachineEvent peerEvent in
                Enum.GetValues<PeerStateMachineEvent>())
            {
                PeerStateMachine machine = new PeerStateMachine(state);
                PeerStateTransition result =
                    machine.HandlePeerStateEvent(peerEvent);
                bool expectedDefined = IsDefinedPeerTransitionCell(
                    state,
                    peerEvent);
                string cell = state + "/" + peerEvent;

                Assert.That(result.Defined, Is.EqualTo(expectedDefined), cell);
                Assert.That(result.PreviousState, Is.EqualTo(state), cell);
                Assert.That(result.Event, Is.EqualTo(peerEvent), cell);
                if (expectedDefined)
                {
                    Assert.That(result.Actions, Is.Not.Empty, cell);
                    Assert.That(result.Code, Is.EqualTo("transition-defined"), cell);
                    definedCount = definedCount + 1;
                }
                else
                {
                    Assert.That(result.NextState, Is.EqualTo(state), cell);
                    Assert.That(result.Actions, Is.Empty, cell);
                    Assert.That(result.Code, Is.EqualTo("event-not-defined"), cell);
                }

                cellCount = cellCount + 1;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(cellCount, Is.EqualTo(176));
            Assert.That(definedCount, Is.EqualTo(43));
        });
    }

    [Test]
    public void CapabilitiesAnswerAndTimeoutRaceIsAtomic()
    {
        int iteration = 0;
        while (iteration < 64)
        {
            PeerStateMachine machine = new PeerStateMachine(
                DiameterPeerState.WaitInitiatorCapabilitiesAnswer);
            using Barrier barrier = new Barrier(3);
            Task<PeerStateTransition> answerTask = Task.Run(() =>
            {
                barrier.SignalAndWait();
                return machine.HandlePeerStateEvent(
                    PeerStateMachineEvent.InitiatorCapabilitiesAnswerReceived);
            });
            Task<PeerStateTransition> timeoutTask = Task.Run(() =>
            {
                barrier.SignalAndWait();
                return machine.HandlePeerStateEvent(
                    PeerStateMachineEvent.Timeout);
            });

            barrier.SignalAndWait();
            Task.WaitAll(answerTask, timeoutTask);
            PeerStateTransition answer = answerTask.Result;
            PeerStateTransition timeout = timeoutTask.Result;

            Assert.That(
                (answer.Defined ? 1 : 0) + (timeout.Defined ? 1 : 0),
                Is.EqualTo(1),
                "iteration " + iteration);
            if (answer.Defined)
            {
                Assert.That(answer.NextState,
                    Is.EqualTo(DiameterPeerState.InitiatorOpen));
                Assert.That(answer.Actions,
                    Is.EqualTo(new[]
                    {
                        PeerStateMachineAction.ProcessCapabilitiesAnswer
                    }));
                Assert.That(machine.ReadPeerState(),
                    Is.EqualTo(DiameterPeerState.InitiatorOpen));
            }
            else
            {
                Assert.That(timeout.NextState,
                    Is.EqualTo(DiameterPeerState.Closed));
                Assert.That(timeout.Actions,
                    Is.EqualTo(new[] { PeerStateMachineAction.Error }));
                Assert.That(machine.ReadPeerState(),
                    Is.EqualTo(DiameterPeerState.Closed));
            }

            iteration = iteration + 1;
        }
    }

    [Test]
    public void InvalidStateAndEventValuesAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new PeerStateMachine((DiameterPeerState)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            PeerStateMachine machine = new PeerStateMachine(
                DiameterPeerState.Closed);
            Assert.That(
                () => machine.HandlePeerStateEvent((PeerStateMachineEvent)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => machine.HandlePeerStateEvent((PeerStateMachineEvent)(-1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => machine.HandlePeerStateEvent((PeerStateMachineEvent)22),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(machine.ReadPeerState(), Is.EqualTo(DiameterPeerState.Closed));
        });
    }

    [Test]
    public void ReturnedActionsAreOwnedByEachTransition()
    {
        PeerStateMachine machine = new PeerStateMachine(
            DiameterPeerState.ResponderOpen);
        PeerStateTransition first = machine.HandlePeerStateEvent(
            PeerStateMachineEvent.SendMessage);
        first.Actions.Clear();

        PeerStateTransition second = machine.HandlePeerStateEvent(
            PeerStateMachineEvent.SendMessage);

        Assert.That(
            second.Actions,
            Is.EqualTo(new[] { PeerStateMachineAction.ResponderSendMessage }));
    }

    private static IEnumerable<TestCaseData> ReadDefinedPeerTransitions()
    {
        yield return CreatePeerTransitionCase(
            DiameterPeerState.Closed,
            PeerStateMachineEvent.Start,
            DiameterPeerState.WaitConnectionAcknowledgement,
            [PeerStateMachineAction.InitiatorSendConnectionRequest]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.Closed,
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived,
            DiameterPeerState.ResponderOpen,
            [
                PeerStateMachineAction.ResponderAccept,
                PeerStateMachineAction.ProcessCapabilitiesRequest,
                PeerStateMachineAction.ResponderSendCapabilitiesAnswer
            ]);

        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitConnectionAcknowledgement,
            PeerStateMachineEvent.InitiatorConnectionAcknowledged,
            DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
            [PeerStateMachineAction.InitiatorSendCapabilitiesRequest]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitConnectionAcknowledgement,
            PeerStateMachineEvent.InitiatorConnectionRejected,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.Cleanup]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitConnectionAcknowledgement,
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived,
            DiameterPeerState.WaitConnectionAcknowledgementElection,
            [
                PeerStateMachineAction.ResponderAccept,
                PeerStateMachineAction.ProcessCapabilitiesRequest
            ]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitConnectionAcknowledgement,
            PeerStateMachineEvent.Timeout,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.Error]);

        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
            PeerStateMachineEvent.InitiatorCapabilitiesAnswerReceived,
            DiameterPeerState.InitiatorOpen,
            [PeerStateMachineAction.ProcessCapabilitiesAnswer]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived,
            DiameterPeerState.WaitReturns,
            [
                PeerStateMachineAction.ResponderAccept,
                PeerStateMachineAction.ProcessCapabilitiesRequest,
                PeerStateMachineAction.Elect
            ]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
            PeerStateMachineEvent.InitiatorPeerDisconnected,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.InitiatorDisconnect]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
            PeerStateMachineEvent.InitiatorNonCapabilitiesAnswerReceived,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.Error]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
            PeerStateMachineEvent.Timeout,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.Error]);

        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitConnectionAcknowledgementElection,
            PeerStateMachineEvent.InitiatorConnectionAcknowledged,
            DiameterPeerState.WaitReturns,
            [
                PeerStateMachineAction.InitiatorSendCapabilitiesRequest,
                PeerStateMachineAction.Elect
            ]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitConnectionAcknowledgementElection,
            PeerStateMachineEvent.InitiatorConnectionRejected,
            DiameterPeerState.ResponderOpen,
            [PeerStateMachineAction.ResponderSendCapabilitiesAnswer]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitConnectionAcknowledgementElection,
            PeerStateMachineEvent.ResponderPeerDisconnected,
            DiameterPeerState.WaitConnectionAcknowledgement,
            [PeerStateMachineAction.ResponderDisconnect]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitConnectionAcknowledgementElection,
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived,
            DiameterPeerState.WaitConnectionAcknowledgementElection,
            [PeerStateMachineAction.ResponderReject]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitConnectionAcknowledgementElection,
            PeerStateMachineEvent.Timeout,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.Error]);

        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitReturns,
            PeerStateMachineEvent.ElectionWon,
            DiameterPeerState.ResponderOpen,
            [
                PeerStateMachineAction.InitiatorDisconnect,
                PeerStateMachineAction.ResponderSendCapabilitiesAnswer
            ]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitReturns,
            PeerStateMachineEvent.InitiatorPeerDisconnected,
            DiameterPeerState.ResponderOpen,
            [
                PeerStateMachineAction.InitiatorDisconnect,
                PeerStateMachineAction.ResponderSendCapabilitiesAnswer
            ]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitReturns,
            PeerStateMachineEvent.InitiatorCapabilitiesAnswerReceived,
            DiameterPeerState.InitiatorOpen,
            [PeerStateMachineAction.ResponderDisconnect]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitReturns,
            PeerStateMachineEvent.ResponderPeerDisconnected,
            DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
            [PeerStateMachineAction.ResponderDisconnect]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitReturns,
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived,
            DiameterPeerState.WaitReturns,
            [PeerStateMachineAction.ResponderReject]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.WaitReturns,
            PeerStateMachineEvent.Timeout,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.Error]);

        yield return CreatePeerTransitionCase(
            DiameterPeerState.ResponderOpen,
            PeerStateMachineEvent.SendMessage,
            DiameterPeerState.ResponderOpen,
            [PeerStateMachineAction.ResponderSendMessage]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.ResponderOpen,
            PeerStateMachineEvent.ResponderMessageReceived,
            DiameterPeerState.ResponderOpen,
            [PeerStateMachineAction.ProcessMessage]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.ResponderOpen,
            PeerStateMachineEvent.ResponderWatchdogRequestReceived,
            DiameterPeerState.ResponderOpen,
            [
                PeerStateMachineAction.ProcessWatchdogRequest,
                PeerStateMachineAction.ResponderSendWatchdogAnswer
            ]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.ResponderOpen,
            PeerStateMachineEvent.ResponderWatchdogAnswerReceived,
            DiameterPeerState.ResponderOpen,
            [PeerStateMachineAction.ProcessWatchdogAnswer]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.ResponderOpen,
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived,
            DiameterPeerState.ResponderOpen,
            [PeerStateMachineAction.ResponderReject]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.ResponderOpen,
            PeerStateMachineEvent.Stop,
            DiameterPeerState.Closing,
            [PeerStateMachineAction.ResponderSendDisconnectRequest]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.ResponderOpen,
            PeerStateMachineEvent.ResponderDisconnectRequestReceived,
            DiameterPeerState.Closing,
            [PeerStateMachineAction.ResponderSendDisconnectAnswer]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.ResponderOpen,
            PeerStateMachineEvent.ResponderPeerDisconnected,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.ResponderDisconnect]);

        yield return CreatePeerTransitionCase(
            DiameterPeerState.InitiatorOpen,
            PeerStateMachineEvent.SendMessage,
            DiameterPeerState.InitiatorOpen,
            [PeerStateMachineAction.InitiatorSendMessage]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.InitiatorOpen,
            PeerStateMachineEvent.InitiatorMessageReceived,
            DiameterPeerState.InitiatorOpen,
            [PeerStateMachineAction.ProcessMessage]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.InitiatorOpen,
            PeerStateMachineEvent.InitiatorWatchdogRequestReceived,
            DiameterPeerState.InitiatorOpen,
            [
                PeerStateMachineAction.ProcessWatchdogRequest,
                PeerStateMachineAction.InitiatorSendWatchdogAnswer
            ]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.InitiatorOpen,
            PeerStateMachineEvent.InitiatorWatchdogAnswerReceived,
            DiameterPeerState.InitiatorOpen,
            [PeerStateMachineAction.ProcessWatchdogAnswer]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.InitiatorOpen,
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived,
            DiameterPeerState.InitiatorOpen,
            [PeerStateMachineAction.ResponderReject]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.InitiatorOpen,
            PeerStateMachineEvent.Stop,
            DiameterPeerState.Closing,
            [PeerStateMachineAction.InitiatorSendDisconnectRequest]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.InitiatorOpen,
            PeerStateMachineEvent.InitiatorDisconnectRequestReceived,
            DiameterPeerState.Closing,
            [PeerStateMachineAction.InitiatorSendDisconnectAnswer]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.InitiatorOpen,
            PeerStateMachineEvent.InitiatorPeerDisconnected,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.InitiatorDisconnect]);

        yield return CreatePeerTransitionCase(
            DiameterPeerState.Closing,
            PeerStateMachineEvent.InitiatorDisconnectAnswerReceived,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.InitiatorDisconnect]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.Closing,
            PeerStateMachineEvent.ResponderDisconnectAnswerReceived,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.ResponderDisconnect]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.Closing,
            PeerStateMachineEvent.Timeout,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.Error]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.Closing,
            PeerStateMachineEvent.InitiatorPeerDisconnected,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.InitiatorDisconnect]);
        yield return CreatePeerTransitionCase(
            DiameterPeerState.Closing,
            PeerStateMachineEvent.ResponderPeerDisconnected,
            DiameterPeerState.Closed,
            [PeerStateMachineAction.ResponderDisconnect]);
    }

    private static TestCaseData CreatePeerTransitionCase(
        DiameterPeerState initialState,
        PeerStateMachineEvent peerEvent,
        DiameterPeerState expectedState,
        PeerStateMachineAction[] expectedActions)
    {
        return new TestCaseData(
            initialState,
            peerEvent,
            expectedState,
            expectedActions).SetName(initialState + "_" + peerEvent);
    }

    private static bool IsDefinedPeerTransitionCell(
        DiameterPeerState state,
        PeerStateMachineEvent peerEvent)
    {
        return state switch
        {
            DiameterPeerState.Closed => peerEvent is
                PeerStateMachineEvent.Start or
                PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived,
            DiameterPeerState.WaitConnectionAcknowledgement => peerEvent is
                PeerStateMachineEvent.InitiatorConnectionAcknowledged or
                PeerStateMachineEvent.InitiatorConnectionRejected or
                PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived or
                PeerStateMachineEvent.Timeout,
            DiameterPeerState.WaitInitiatorCapabilitiesAnswer => peerEvent is
                PeerStateMachineEvent.InitiatorCapabilitiesAnswerReceived or
                PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived or
                PeerStateMachineEvent.InitiatorPeerDisconnected or
                PeerStateMachineEvent.InitiatorNonCapabilitiesAnswerReceived or
                PeerStateMachineEvent.Timeout,
            DiameterPeerState.WaitConnectionAcknowledgementElection => peerEvent is
                PeerStateMachineEvent.InitiatorConnectionAcknowledged or
                PeerStateMachineEvent.InitiatorConnectionRejected or
                PeerStateMachineEvent.ResponderPeerDisconnected or
                PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived or
                PeerStateMachineEvent.Timeout,
            DiameterPeerState.WaitReturns => peerEvent is
                PeerStateMachineEvent.ElectionWon or
                PeerStateMachineEvent.InitiatorPeerDisconnected or
                PeerStateMachineEvent.InitiatorCapabilitiesAnswerReceived or
                PeerStateMachineEvent.ResponderPeerDisconnected or
                PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived or
                PeerStateMachineEvent.Timeout,
            DiameterPeerState.ResponderOpen => peerEvent is
                PeerStateMachineEvent.SendMessage or
                PeerStateMachineEvent.ResponderMessageReceived or
                PeerStateMachineEvent.ResponderWatchdogRequestReceived or
                PeerStateMachineEvent.ResponderWatchdogAnswerReceived or
                PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived or
                PeerStateMachineEvent.Stop or
                PeerStateMachineEvent.ResponderDisconnectRequestReceived or
                PeerStateMachineEvent.ResponderPeerDisconnected,
            DiameterPeerState.InitiatorOpen => peerEvent is
                PeerStateMachineEvent.SendMessage or
                PeerStateMachineEvent.InitiatorMessageReceived or
                PeerStateMachineEvent.InitiatorWatchdogRequestReceived or
                PeerStateMachineEvent.InitiatorWatchdogAnswerReceived or
                PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived or
                PeerStateMachineEvent.Stop or
                PeerStateMachineEvent.InitiatorDisconnectRequestReceived or
                PeerStateMachineEvent.InitiatorPeerDisconnected,
            DiameterPeerState.Closing => peerEvent is
                PeerStateMachineEvent.InitiatorDisconnectAnswerReceived or
                PeerStateMachineEvent.ResponderDisconnectAnswerReceived or
                PeerStateMachineEvent.Timeout or
                PeerStateMachineEvent.InitiatorPeerDisconnected or
                PeerStateMachineEvent.ResponderPeerDisconnected,
            _ => false
        };
    }
}
