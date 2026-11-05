using System.IO;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class OrchestraBehaviorParserTests
{
    [Test]
    public void ParsesActorsFlowsStateMachinesTransitionsVariablesAndTimers()
    {
        string xml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2026/orchestra/repository"
                             xmlns:dcterms="http://purl.org/dc/terms/"
                             xmlns:venue="urn:venue:behavior"
                             name="Venue" version="FIX.Latest">
              <fixr:metadata><dcterms:title>Venue workflow</dcterms:title></fixr:metadata>
              <fixr:datatypes><fixr:datatype name="Boolean"/><fixr:datatype name="String"/></fixr:datatypes>
              <fixr:codeSets/>
              <fixr:fields><fixr:field id="1" name="ClOrdID" type="String"/></fixr:fields>
              <fixr:actors>
                <fixr:actor name="Buyer" venue:role="client">
                  <fixr:field id="900" name="marketOpen" type="Boolean" value="true"><fixr:annotation><fixr:documentation>Trading gate</fixr:documentation></fixr:annotation></fixr:field>
                  <fixr:fieldRef id="1" name="ClOrdID"/>
                  <fixr:timer name="responseTimeout"/>
                  <fixr:states name="OrderLifecycle">
                    <fixr:initial name="Ready">
                      <fixr:transition name="send" target="Waiting"><fixr:when>$Buyer.marketOpen == true</fixr:when><fixr:annotation><fixr:documentation>Submit an order</fixr:documentation></fixr:annotation></fixr:transition>
                      <fixr:onentry>
                        <fixr:fieldRef id="1"/>
                        <fixr:assign>$Buyer.counter = 1</fixr:assign>
                        <fixr:timerSchedule actor="Buyer" name="responseTimeout" operation="START" interval="PT5S">
                          <fixr:activity><fixr:trigger actor="Buyer" stateMachine="OrderLifecycle" name="expire"/></fixr:activity>
                        </fixr:timerSchedule>
                      </fixr:onentry>
                      <fixr:activity><fixr:messageRef id="3000" name="NewOrderSingle" scenario="base" msgType="D"/></fixr:activity>
                      <fixr:onexit><fixr:trigger actor="Buyer" stateMachine="OrderLifecycle" name="sent"/></fixr:onexit>
                      <fixr:annotation><fixr:documentation>Ready to send</fixr:documentation></fixr:annotation>
                    </fixr:initial>
                    <fixr:state name="Waiting"><fixr:transition name="acknowledge" target="Done"/></fixr:state>
                    <fixr:state name="Done"/>
                    <fixr:annotation><fixr:documentation>Order state machine</fixr:documentation></fixr:annotation>
                  </fixr:states>
                  <fixr:annotation><fixr:documentation>Order originator</fixr:documentation></fixr:annotation>
                </fixr:actor>
                <fixr:actor name="Seller"/>
                <fixr:flow name="Orders" source="Buyer" destination="Seller" reliability="recoverable"><fixr:annotation><fixr:documentation>Order flow</fixr:documentation></fixr:annotation></fixr:flow>
              </fixr:actors>
              <fixr:messages>
                <fixr:message id="3000" name="NewOrderSingle" msgType="D" flow="Orders"/>
                <fixr:message id="3001" name="ExecutionReport" msgType="8" flow="Orders"/>
              </fixr:messages>
            </fixr:repository>
            """;

        OrchestraRepository repository = Parse(xml, "workflow-v11.xml");
        OrchestraActor buyer = repository.Actors[0];
        OrchestraStateMachine machine = repository.StateMachines.Single();
        OrchestraState ready = machine.States[0];

        Assert.Multiple(() =>
        {
            Assert.That(repository.Actors.Select(item => item.Name), Is.EqualTo(new[] { "Buyer", "Seller" }));
            Assert.That(buyer.Attributes["venue:role"], Is.EqualTo("client"));
            Assert.That(buyer.Annotations.Single().Content, Is.EqualTo("Order originator"));
            Assert.That(buyer.Variables, Has.Count.EqualTo(2));
            Assert.That(repository.Variables, Is.EqualTo(buyer.Variables));
            Assert.That(buyer.Variables[0].Kind, Is.EqualTo("field"));
            Assert.That(buyer.Variables[0].Name, Is.EqualTo("marketOpen"));
            Assert.That(buyer.Variables[0].Datatype, Is.EqualTo("Boolean"));
            Assert.That(buyer.Variables[0].InitialValue, Is.EqualTo("true"));
            Assert.That(buyer.Variables[0].IsExternal, Is.False);
            Assert.That(buyer.Variables[0].Annotations.Single().Content, Is.EqualTo("Trading gate"));
            Assert.That(buyer.Variables[1].ReferenceId, Is.EqualTo("1"));
            Assert.That(buyer.Variables[1].IsExternal, Is.True);
            Assert.That(buyer.Timers.Single().Name, Is.EqualTo("responseTimeout"));
            Assert.That(repository.Timers.Single(), Is.SameAs(buyer.Timers.Single()));

            Assert.That(buyer.StateMachines.Single(), Is.EqualTo("OrderLifecycle"));
            Assert.That(machine.InitialState, Is.EqualTo("Ready"));
            Assert.That(machine.Actor, Is.EqualTo("Buyer"));
            Assert.That(machine.States.Select(item => item.Name), Is.EqualTo(new[] { "Ready", "Waiting", "Done" }));
            Assert.That(machine.Annotations.Single().Content, Is.EqualTo("Order state machine"));
            Assert.That(ready.Transitions[0].Name, Is.EqualTo("send"));
            Assert.That(ready.Transitions[0].TargetState, Is.EqualTo("Waiting"));
            Assert.That(ready.Transitions[0].Condition, Is.EqualTo("$Buyer.marketOpen == true"));
            Assert.That(ready.Transitions[0].Rule!.Kind, Is.EqualTo(OrchestraRuleKind.Conditional));
            Assert.That(ready.Transitions[0].Annotations.Single().Content, Is.EqualTo("Submit an order"));
            Assert.That(ready.EntryActions.Select(item => item.Kind), Is.EqualTo(new[] { "fieldRef", "assign", "timerSchedule" }));
            Assert.That(ready.EntryActions[1].AssignmentTarget, Is.EqualTo("$Buyer.counter"));
            Assert.That(ready.EntryActions[1].Rule!.Kind, Is.EqualTo(OrchestraRuleKind.Assignment));
            Assert.That(ready.EntryActions[2].Actor, Is.EqualTo("Buyer"));
            Assert.That(ready.EntryActions[2].Operation, Is.EqualTo("START"));
            Assert.That(ready.EntryActions[2].Interval, Is.EqualTo("PT5S"));
            Assert.That(ready.EntryActions[2].Timer!.Name, Is.EqualTo("responseTimeout"));
            Assert.That(ready.EntryActions[2].Actions.Single().Kind, Is.EqualTo("trigger"));
            Assert.That(ready.EntryActions[2].Actions.Single().StateMachine, Is.EqualTo("OrderLifecycle"));
            Assert.That(ready.ActivityActions.Single().Name, Is.EqualTo("NewOrderSingle"));
            Assert.That(ready.ExitActions.Single().Name, Is.EqualTo("sent"));

            Assert.That(repository.Flows.Single().SourceActor, Is.EqualTo("Buyer"));
            Assert.That(repository.Flows.Single().DestinationActor, Is.EqualTo("Seller"));
            Assert.That(repository.Flows.Single().Reliability, Is.EqualTo("recoverable"));
            Assert.That(repository.Flows.Single().Messages, Is.EqualTo(new[] { "NewOrderSingle", "ExecutionReport" }));
            Assert.That(repository.Flows.Single().Annotations.Single().Content, Is.EqualTo("Order flow"));
        });
    }

    [Test]
    public void ParsesConditionalRulesAssignmentsAndMixedMessageResponses()
    {
        string xml = """
            <fixr:repository xmlns:fixr="http://fixprotocol.io/2026/orchestra/repository"
                             xmlns:dcterms="http://purl.org/dc/terms/"
                             name="Venue" version="FIX.Latest">
              <fixr:metadata><dcterms:title>Venue rules</dcterms:title></fixr:metadata>
              <fixr:datatypes><fixr:datatype name="String"/></fixr:datatypes>
              <fixr:codeSets/>
              <fixr:fields>
                <fixr:field id="1" name="ClOrdID" type="String">
                  <fixr:rule name="requiredId" presence="required">
                    <fixr:unique><fixr:fieldRef id="2"/></fixr:unique>
                    <fixr:assign>$Buyer.lastId = in.ClOrdID</fixr:assign>
                    <fixr:when>in.ClOrdID != ""</fixr:when>
                  </fixr:rule>
                  <fixr:rule name="stringId" type="String"><fixr:when>in.ClOrdID != null</fixr:when></fixr:rule>
                  <fixr:assign>out.ClOrdID = in.ClOrdID</fixr:assign>
                </fixr:field>
                <fixr:field id="2" name="SecondaryID" type="String"/>
              </fixr:fields>
              <fixr:components>
                <fixr:component id="1000" name="OrderCore">
                  <fixr:fieldRef id="1">
                    <fixr:rule name="forbidId" presence="forbidden"><fixr:when>cancel == true</fixr:when></fixr:rule>
                    <fixr:assign>out.ClOrdID = generatedId</fixr:assign>
                  </fixr:fieldRef>
                </fixr:component>
              </fixr:components>
              <fixr:messages>
                <fixr:message id="3000" name="NewOrderSingle" msgType="D">
                  <fixr:structure>
                    <fixr:componentRef id="1000"><fixr:rule name="coreRequired" presence="required"><fixr:when>submit == true</fixr:when></fixr:rule></fixr:componentRef>
                  </fixr:structure>
                  <fixr:when>scenario == "submit"</fixr:when>
                  <fixr:responses>
                    <fixr:response name="acknowledge" sync="pipelined">
                      <fixr:fieldRef id="1"/>
                      <fixr:messageRef id="3001" name="ExecutionReport" scenario="ack" msgType="8"/>
                      <fixr:assign>$Buyer.lastResponse = "ack"</fixr:assign>
                      <fixr:trigger actor="Buyer" stateMachine="OrderLifecycle" name="acknowledge"/>
                      <fixr:timerSchedule actor="Buyer" name="responseTimeout" operation="CANCEL"><fixr:activity><fixr:assign>$Buyer.timedOut = false</fixr:assign></fixr:activity></fixr:timerSchedule>
                      <fixr:when>accepted == true</fixr:when>
                      <fixr:annotation><fixr:documentation>Successful acknowledgement</fixr:documentation></fixr:annotation>
                    </fixr:response>
                  </fixr:responses>
                </fixr:message>
              </fixr:messages>
            </fixr:repository>
            """;

        OrchestraRepository repository = Parse(xml, "rules-v11.xml");
        OrchestraFieldDefinition field = repository.Fields[0];
        OrchestraRule required = field.Rules[0];
        OrchestraResponse response = repository.Messages.Single().Responses.Single();

        Assert.Multiple(() =>
        {
            Assert.That(field.Rules, Has.Count.EqualTo(3));
            Assert.That(required.Name, Is.EqualTo("requiredId"));
            Assert.That(required.Kind, Is.EqualTo(OrchestraRuleKind.Presence));
            Assert.That(required.Presence, Is.EqualTo(FixPresence.Required));
            Assert.That(required.Expression, Is.EqualTo("in.ClOrdID != \"\""));
            Assert.That(required.UniqueFieldIds.Single(), Is.EqualTo("2"));
            Assert.That(required.Assignments.Single().AssignmentTarget, Is.EqualTo("$Buyer.lastId"));
            Assert.That(field.Rules[1].Kind, Is.EqualTo(OrchestraRuleKind.Conditional));
            Assert.That(field.Rules[1].Type, Is.EqualTo("String"));
            Assert.That(field.Rules[2].Kind, Is.EqualTo(OrchestraRuleKind.Assignment));
            Assert.That(field.Rules[2].AssignmentTarget, Is.EqualTo("out.ClOrdID"));

            Assert.That(repository.Components.Single().Members.Single().Rules.Select(item => item.Kind), Is.EqualTo(new[] { OrchestraRuleKind.Presence, OrchestraRuleKind.Assignment }));
            Assert.That(repository.Components.Single().Members.Single().Rules[0].Presence, Is.EqualTo(FixPresence.Forbidden));
            Assert.That(repository.Messages.Single().Members.Single().Rules.Single().Name, Is.EqualTo("coreRequired"));
            Assert.That(repository.Messages.Single().Rules.Single().Expression, Is.EqualTo("scenario == \"submit\""));

            Assert.That(response.Name, Is.EqualTo("acknowledge"));
            Assert.That(response.Synchronization, Is.EqualTo("pipelined"));
            Assert.That(response.Condition, Is.EqualTo("accepted == true"));
            Assert.That(response.Rule!.Kind, Is.EqualTo(OrchestraRuleKind.Conditional));
            Assert.That(response.MessageName, Is.EqualTo("ExecutionReport"));
            Assert.That(response.Scenario, Is.EqualTo("ack"));
            Assert.That(response.Actions.Select(item => item.Kind), Is.EqualTo(new[] { "fieldRef", "messageRef", "assign", "trigger", "timerSchedule" }));
            Assert.That(response.Assignments.Single().AssignmentTarget, Is.EqualTo("$Buyer.lastResponse"));
            Assert.That(response.Actions[3].StateMachine, Is.EqualTo("OrderLifecycle"));
            Assert.That(response.Actions[4].Operation, Is.EqualTo("CANCEL"));
            Assert.That(response.Actions[4].Actions.Single().Rule!.AssignmentTarget, Is.EqualTo("$Buyer.timedOut"));
            Assert.That(response.Annotations.Single().Content, Is.EqualTo("Successful acknowledgement"));
            Assert.That(repository.Rules.Select(item => item.Id), Is.EqualTo(Enumerable.Range(1, repository.Rules.Count)));
            Assert.That(repository.Rules.Count(item => item.Kind == OrchestraRuleKind.Assignment), Is.EqualTo(5));
        });
    }

    private static OrchestraRepository Parse(string xml, string sourceName)
    {
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        OrchestraXmlLoadResult loaded = OrchestraXmlLoader.LoadDocument(source, new OrchestraReadOptions { BaseUri = sourceName });
        Assert.That(loaded.Success, Is.True, Codes(loaded));
        return OrchestraVersion11Parser.ParseDocument(loaded);
    }

    private static string Codes(OrchestraXmlLoadResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }
}
