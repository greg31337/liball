using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class CallbackContractTests
{
    [Test]
    public async Task EveryCallbackMethodCanBeCalledDirectlyThroughItsPublicInterface()
    {
        CoverageCallbacks callbacks = new CoverageCallbacks();
        IUiValueConverter converter = callbacks;
        IUiBridgeMessageHandler bridgeHandler = callbacks;
        IUiCommandHandler commandHandler = callbacks;
        IUiComponentRenderer componentRenderer = callbacks;
        IUiLifecycleHandler lifecycleHandler = callbacks;
        IUiEventHandler eventHandler = callbacks;
        IUiNavigationObserver navigationObserver = callbacks;
        IUiValidationHandler validationHandler = callbacks;
        IUiStateObserver stateObserver = callbacks;
        IUiRangeProvider rangeProvider = callbacks;
        IUiMessageTransport transport = callbacks;
        UiValue value = Text("value");
        UiBindingContext bindingContext = new UiBindingContext();
        UiCommandInvocation command = new UiCommandInvocation { CommandId = "save" };
        UiComponentRenderRequest component = new UiComponentRenderRequest
        {
            Context = new UiComponentContext { ComponentId = "component", Key = "component" }
        };

        UiConversionResult target = converter.ConvertBindingValueToTarget(value, bindingContext);
        UiConversionResult source = converter.ConvertBindingValueToSource(value, bindingContext);
        UiResult bridge = bridgeHandler.HandleUiBridgeMessage(new UiBridgeEnvelope());
        UiCommandStatus status = commandHandler.QueryUiCommandStatus(command);
        UiCommandHandlerResult handledCommand = commandHandler.HandleUiCommand(command);
        UiComponentRenderResult rendered = await componentRenderer.RenderUiComponent(component);
        UiLifecycleHandlerResult lifecycle = await lifecycleHandler.HandleUiComponentLifecycle(new UiLifecycleNotification());
        UiEventHandlerResult handledEvent = eventHandler.HandleUiEvent(new UiEventHandlerContext());
        UiNavigationObserverResult navigation = await navigationObserver.HandleUiNavigation(new UiNavigationNotification());
        UiValidationHandlerResult validation = await validationHandler.HandleUiValidation(new UiValidationContext());
        UiResult observed = stateObserver.ObserveStateChange(new UiStateObserverContext());
        UiRangeResult range = await rangeProvider.ProvideUiRange(new UiRangeRequest { Generation = 7, Start = 3, Count = 2 });
        UiResult started = transport.StartUiMessageTransport();
        UiResult sent = transport.SendUiMessage(new UiBridgeEnvelope());
        UiTransportCapabilities capabilities = transport.GetUiMessageTransportCapabilities();
        UiResult stopped = transport.StopUiMessageTransport();

        Assert.Multiple(() =>
        {
            Assert.That(target.Success, Is.True);
            Assert.That(target.Value, Is.SameAs(value));
            Assert.That(source.Success, Is.True);
            Assert.That(source.Value, Is.SameAs(value));
            Assert.That(bridge.Success, Is.True);
            Assert.That(status.IsEnabled, Is.True);
            Assert.That(handledCommand.Success, Is.True);
            Assert.That(rendered.Success, Is.True);
            Assert.That(rendered.Content!.Text, Is.EqualTo("component"));
            Assert.That(lifecycle.Success, Is.True);
            Assert.That(handledEvent.Success, Is.True);
            Assert.That(navigation.Success, Is.True);
            Assert.That(validation.Success, Is.True);
            Assert.That(observed.Success, Is.True);
            Assert.That(range.Generation, Is.EqualTo(7));
            Assert.That(range.Start, Is.EqualTo(3));
            Assert.That(range.Items, Has.Length.EqualTo(2));
            Assert.That(started.Success, Is.True);
            Assert.That(sent.Success, Is.True);
            Assert.That(capabilities.State, Is.EqualTo(UiTransportState.Ready));
            Assert.That(stopped.Success, Is.True);
            Assert.That(callbacks.TransportMessages, Is.EqualTo(1));
        });
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private sealed class CoverageCallbacks : IUiValueConverter, IUiBridgeMessageHandler, IUiCommandHandler, IUiComponentRenderer, IUiLifecycleHandler, IUiEventHandler, IUiNavigationObserver, IUiValidationHandler, IUiStateObserver, IUiRangeProvider, IUiMessageTransport
    {
        public int TransportMessages;

        public UiConversionResult ConvertBindingValueToTarget(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = true, Value = value };
        }

        public UiConversionResult ConvertBindingValueToSource(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = true, Value = value };
        }

        public UiResult HandleUiBridgeMessage(UiBridgeEnvelope message)
        {
            return new UiResult { Success = true };
        }

        public UiCommandStatus QueryUiCommandStatus(UiCommandInvocation invocation)
        {
            return new UiCommandStatus { IsEnabled = true, IsVisible = true };
        }

        public UiCommandHandlerResult HandleUiCommand(UiCommandInvocation invocation)
        {
            return new UiCommandHandlerResult { Success = true };
        }

        public Task<UiComponentRenderResult> RenderUiComponent(UiComponentRenderRequest request)
        {
            return Task.FromResult(new UiComponentRenderResult
            {
                Success = true,
                Content = new UiNode { Kind = UiNodeKind.Text, Text = request.Context.Key }
            });
        }

        public Task<UiLifecycleHandlerResult> HandleUiComponentLifecycle(UiLifecycleNotification notification)
        {
            return Task.FromResult(new UiLifecycleHandlerResult { Success = true });
        }

        public UiEventHandlerResult HandleUiEvent(UiEventHandlerContext context)
        {
            return new UiEventHandlerResult { Success = true };
        }

        public Task<UiNavigationObserverResult> HandleUiNavigation(UiNavigationNotification notification)
        {
            return Task.FromResult(new UiNavigationObserverResult { Success = true, AllowNavigation = true });
        }

        public Task<UiValidationHandlerResult> HandleUiValidation(UiValidationContext context)
        {
            return Task.FromResult(new UiValidationHandlerResult { Success = true });
        }

        public UiResult ObserveStateChange(UiStateObserverContext context)
        {
            return new UiResult { Success = true };
        }

        public Task<UiRangeResult> ProvideUiRange(UiRangeRequest request)
        {
            UiCollectionItem[] items = new UiCollectionItem[request.Count];
            int index = 0;
            while (index < items.Length)
            {
                items[index] = new UiCollectionItem { Key = "item-" + (request.Start + index), Value = Text("item") };
                index++;
            }

            return Task.FromResult(new UiRangeResult
            {
                Generation = request.Generation,
                Start = request.Start,
                Items = items
            });
        }

        public UiResult StartUiMessageTransport()
        {
            return new UiResult { Success = true };
        }

        public UiResult SendUiMessage(UiBridgeEnvelope message)
        {
            TransportMessages++;
            return new UiResult { Success = true };
        }

        public UiResult StopUiMessageTransport()
        {
            return new UiResult { Success = true };
        }

        public UiTransportCapabilities GetUiMessageTransportCapabilities()
        {
            return new UiTransportCapabilities { State = UiTransportState.Ready };
        }
    }
}
