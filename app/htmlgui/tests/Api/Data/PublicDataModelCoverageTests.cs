using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using htmlgui;

namespace htmlgui.Tests;

public class PublicDataModelCoverageTests
{
    [Test]
    public void EveryPublicDataFieldHasDeterministicFreshDefaults()
    {
        object[] models = CreateDataModels();

        foreach (object first in models)
        {
            Type type = first.GetType();
            object second = Activator.CreateInstance(type)!;
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            Assert.That(fields, Is.Not.Empty, type.Name);
            foreach (FieldInfo field in fields)
            {
                string fieldName = type.Name + "." + field.Name;
                object? firstValue = field.GetValue(first);
                object? secondValue = field.GetValue(second);
                if (firstValue is Array firstArray)
                {
                    Assert.That(secondValue, Is.InstanceOf<Array>(), fieldName);
                    Assert.That(firstArray, Is.Empty, fieldName);
                    Assert.That((Array)secondValue!, Is.Empty, fieldName);
                }
                else if (firstValue != null && !field.FieldType.IsValueType && field.FieldType != typeof(string))
                {
                    Assert.That(secondValue, Is.Not.Null, fieldName);
                    Assert.That(secondValue!.GetType(), Is.EqualTo(firstValue.GetType()), fieldName);
                    Assert.That(firstValue, Is.Not.SameAs(secondValue), fieldName);
                }
                else
                {
                    Assert.That(firstValue, Is.EqualTo(secondValue), fieldName);
                }
            }
        }
    }

    [Test]
    public void EveryPublicDataFieldCanRetainAPopulatedValue()
    {
        object[] models = CreateDataModels();
        HashSet<Type> populatedEnumTypes = new HashSet<Type>();

        foreach (object model in models)
        {
            Type type = model.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                object? sample = CreateSample(field.FieldType);
                field.SetValue(model, sample);
                object? retained = field.GetValue(model);
                Assert.That(retained, Is.EqualTo(sample), type.Name + "." + field.Name);
                if (field.FieldType.IsEnum)
                {
                    populatedEnumTypes.Add(field.FieldType);
                }
            }
        }

        Assert.That(populatedEnumTypes, Has.Count.EqualTo(40));
    }

    [Test]
    public void EveryPopulatedPublicDataModelRoundTripsThroughFieldSerialization()
    {
        JsonSerializerOptions options = new JsonSerializerOptions { IncludeFields = true };

        foreach (object model in CreateDataModels())
        {
            Type type = model.GetType();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                field.SetValue(model, CreateSample(field.FieldType));
            }

            string serialized = JsonSerializer.Serialize(model, type, options);
            object? deserialized = JsonSerializer.Deserialize(serialized, type, options);

            Assert.That(deserialized, Is.Not.Null, type.Name);
            Assert.That(JsonSerializer.Serialize(deserialized, type, options), Is.EqualTo(serialized), type.Name);
        }
    }

    [Test]
    public void AdditionalInvalidTaggedOptionalPayloadsAreRejected()
    {
        UiValue missingBinary = new UiValue { Kind = UiValueKind.Binary };
        UiValue missingArray = new UiValue { Kind = UiValueKind.Array };
        UiValue missingObject = new UiValue { Kind = UiValueKind.Object };
        UiValue missingDateTime = new UiValue { Kind = UiValueKind.DateTime };
        UiValue unexpectedOptionalPayload = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true, StringValue = "unexpected" };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => Validate(missingBinary));
            Assert.Throws<ArgumentException>(() => Validate(missingArray));
            Assert.Throws<ArgumentException>(() => Validate(missingObject));
            Assert.Throws<ArgumentException>(() => Validate(missingDateTime));
            Assert.Throws<ArgumentException>(() => Validate(unexpectedOptionalPayload));
        });
    }

    private static object? CreateSample(Type type)
    {
        if (type.IsArray)
        {
            Type elementType = type.GetElementType()!;
            Array array = Array.CreateInstance(elementType, 1);
            array.SetValue(CreateSample(elementType), 0);
            return array;
        }

        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1);
        }

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
                return true;
            case TypeCode.Byte:
                return (byte)7;
            case TypeCode.Char:
                return 'x';
            case TypeCode.DateTime:
                return new DateTime(2026, 9, 14);
            case TypeCode.Decimal:
                return 7.5m;
            case TypeCode.Double:
                return 7.5;
            case TypeCode.Int16:
                return (short)7;
            case TypeCode.Int32:
                return 7;
            case TypeCode.Int64:
                return 7L;
            case TypeCode.SByte:
                return (sbyte)7;
            case TypeCode.Single:
                return 7.5F;
            case TypeCode.String:
                return "populated";
            case TypeCode.UInt16:
                return (ushort)7;
            case TypeCode.UInt32:
                return 7U;
            case TypeCode.UInt64:
                return 7UL;
            default:
                return Activator.CreateInstance(type);
        }
    }

    private static void Validate(UiValue value)
    {
        ValueJsonCodec.ValidateValue(value, 16, 100, 100, 100);
    }

    private static object[] CreateDataModels()
    {
        return new object[]
        {
            new UiAccessibility(),
            new UiAriaRelationship(),
            new UiLiveAnnouncement(),
            new UiAccessibilityReport(),
            new UiApplication(),
            new UiWindow(),
            new UiPage(),
            new UiView(),
            new UiDocumentMetadata(),
            new UiResource(),
            new UiSlot(),
            new UiPortal(),
            new UiForm(),
            new UiField(),
            new UiValidationRule(),
            new UiValidationIssue(),
            new UiValidationReport(),
            new UiFormState(),
            new UiLayout(),
            new UiLength(),
            new UiSpacing(),
            new UiGridTrack(),
            new UiGridPlacement(),
            new UiPosition(),
            new UiScrollOptions(),
            new UiValue(),
            new UiValueEntry(),
            new UiDiagnostic(),
            new UiCapability(),
            new UiResult(),
            new UiEnumProfile(),
            new UiTokenList(),
            new UiRawContentPolicy(),
            new UiConditional(),
            new UiRepeat(),
            new UiEventBinding(),
            new UiEventData(),
            new UiPointerData(),
            new UiKeyboardData(),
            new UiInputData(),
            new UiClipboardData(),
            new UiDragData(),
            new UiEventHandlerContext(),
            new UiEventHandlerResult(),
            new UiEventDispatchResult(),
            new UiFocusRequest(),
            new UiFocusScope(),
            new UiFocusTarget(),
            new UiFocusState(),
            new UiFocusResult(),
            new UiKeyboardMap(),
            new UiTypeAheadState(),
            new UiKeyboardResult(),
            new UiComponentDefinition(),
            new UiComponentContext(),
            new UiComponentRenderRequest(),
            new UiLifecycleNotification(),
            new UiErrorBoundaryState(),
            new UiComponentRenderResult(),
            new UiLifecycleHandlerResult(),
            new UiComponentResult(),
            new DomNode(),
            new DomAttribute(),
            new DomProperty(),
            new DomEventSubscription(),
            new DomSnapshot(),
            new DomMutation(),
            new UiBinding(),
            new UiBindingContext(),
            new UiBindingUpdate(),
            new UiConversionResult(),
            new UiBindingResult(),
            new UiTree(),
            new UiTreeItem(),
            new UiTreeRelationship(),
            new UiTreeRange(),
            new UiBridgeOptions(),
            new UiBridgeEnvelope(),
            new UiBridgeRequest(),
            new UiBridgeResponse(),
            new UiBridgeError(),
            new UiTransportCapabilities(),
            new UiText(),
            new UiButton(),
            new UiInput(),
            new UiChoice(),
            new UiMedia(),
            new UiFeedback(),
            new UiDisclosure(),
            new UiVirtualization(),
            new UiRangeRequest(),
            new UiRangeResult(),
            new UiMeasuredItem(),
            new UiScrollTarget(),
            new UiTable(),
            new UiTableColumn(),
            new UiTableRow(),
            new UiTableCell(),
            new UiTableSummary(),
            new UiEditState(),
            new UiCollection(),
            new UiCollectionItem(),
            new UiSelection(),
            new UiSort(),
            new UiFilter(),
            new UiGroup(),
            new UiPageRange(),
            new UiCommand(),
            new UiCommandInvocation(),
            new UiCommandStatus(),
            new UiShortcut(),
            new UiShortcutChord(),
            new UiCommandHandlerResult(),
            new UiCommandResult(),
            new UiCommandShortcutResult(),
            new UiStateEntry(),
            new UiStateChange(),
            new UiStatePatch(),
            new UiDerivedState(),
            new UiEffect(),
            new UiSubscription(),
            new UiScopedStateEntry(),
            new UiScopedStateChange(),
            new UiStateResult(),
            new UiStateBatchResult(),
            new UiStateSnapshotResult(),
            new UiDerivedStateResult(),
            new UiStateEffectResult(),
            new UiStateSubscriptionResult(),
            new UiStateObserverContext(),
            new UiTheme(),
            new UiThemeVariant(),
            new UiStyle(),
            new UiCssDeclaration(),
            new UiCssRule(),
            new UiCssCondition(),
            new UiResponsiveRule(),
            new UiAnimation(),
            new UiCssCompilation(),
            new UiRoute(),
            new UiRouteSegment(),
            new UiRouteData(),
            new UiNavigationRequest(),
            new UiNavigationResult(),
            new UiNavigationNotification(),
            new UiNavigationObserverResult(),
            new UiHistoryEntry(),
            new UiRenderOptions(),
            new UiRenderResult(),
            new UiUpdateBatch(),
            new UiUpdateResult(),
            new UiHydrationManifest(),
            new UiHydrationResult(),
            new UiPreservationState(),
            new UiMenu(),
            new UiMenuItem(),
            new UiTabs(),
            new UiTab(),
            new UiToolbar(),
            new UiTool(),
            new UiBreadcrumb(),
            new UiPagination(),
            new UiNode(),
            new UiNodeIdentity(),
            new UiNodeState(),
            new UiDialog(),
            new UiPopover(),
            new UiTooltip(),
            new UiToast(),
            new UiAnchor(),
            new UiOverlayState(),
            new UiValidationContext(),
            new UiValidationHandlerResult(),
            new UiValidationResult()
        };
    }
}
