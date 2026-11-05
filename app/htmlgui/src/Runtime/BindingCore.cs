using System;
using System.Collections.Generic;

namespace htmlgui;

internal class BindingRegistration
{
    internal string ScopeId = string.Empty;
    internal UiBinding Binding = new UiBinding();
    internal bool OneTimeCompleted;
}

internal class BindingConverterRegistration
{
    internal string Id = string.Empty;
    internal IUiValueConverter? Converter;
}

internal class BindingOperationResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal string ScopeId = string.Empty;
    internal UiBinding? Binding;
    internal UiStateEntry? Entry;
    internal UiDiagnostic[] Diagnostics = Array.Empty<UiDiagnostic>();
}

internal class BindingTargetDescriptor
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string Kind = string.Empty;
    internal string Name = string.Empty;
}

internal class BindingSourceValueResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiValue? Value;
    internal int Version;
    internal string Origin = string.Empty;
    internal bool IsDerived;
}

internal class BindingValueResult
{
    internal bool Success;
    internal bool UsedFallback;
    internal string Code = string.Empty;
    internal UiValue? Value;
    internal UiConversionResult? Conversion;
    internal UiDiagnostic[] Diagnostics = Array.Empty<UiDiagnostic>();
}

internal class BindingTargetResult
{
    internal bool Success;
    internal bool Applied;
    internal bool RemoveTarget;
    internal string Code = string.Empty;
    internal string Kind = string.Empty;
    internal string Name = string.Empty;
    internal string? Text;
    internal bool BooleanValue;
    internal DomAttribute? Attribute;
    internal DomProperty? Property;
    internal UiValue? Value;
    internal UiValue[] Items = Array.Empty<UiValue>();
    internal string? CommandId;
    internal UiBinding? Binding;
    internal UiBindingUpdate? Update;
    internal UiDiagnostic[] Diagnostics = Array.Empty<UiDiagnostic>();
}

internal class BindingCore
{
    internal List<BindingRegistration> Bindings = new List<BindingRegistration>();
    internal List<BindingConverterRegistration> Converters = new List<BindingConverterRegistration>();

    internal BindingOperationResult RegisterBindingConverter(string? converterId, IUiValueConverter? converter)
    {
        if (string.IsNullOrWhiteSpace(converterId))
        {
            return CreateBindingFailure("binding.invalid-converter", string.Empty);
        }

        if (converter == null)
        {
            return CreateBindingFailure("binding.null-converter", string.Empty);
        }

        if (FindBindingConverterIndex(converterId) >= 0)
        {
            return CreateBindingFailure("binding.duplicate-converter", string.Empty);
        }

        BindingConverterRegistration registration = new BindingConverterRegistration();
        registration.Id = converterId;
        registration.Converter = converter;
        InsertBindingConverter(registration);
        return new BindingOperationResult { Success = true, Applied = true };
    }

    internal BindingOperationResult AttachBinding(StateStoreCore store, string? scopeId, UiBinding? binding)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return CreateBindingFailure("binding.invalid-scope", checkedScope);
        }

        string validationCode = ValidateBinding(store, checkedScope, binding);
        if (validationCode.Length > 0 || binding == null)
        {
            return CreateBindingFailure(validationCode, checkedScope);
        }

        if (FindBindingIndex(checkedScope, binding.Id) >= 0)
        {
            return CreateBindingFailure("binding.duplicate", checkedScope);
        }

        BindingRegistration registration = new BindingRegistration();
        registration.ScopeId = checkedScope;
        registration.Binding = CopyBinding(store, binding);
        registration.Binding.Context = null;
        registration.Binding.Update = null;
        registration.Binding.Conversion = null;
        InsertBinding(registration);
        return CreateBindingSuccess(store, registration, true);
    }

    internal BindingOperationResult ReadBinding(StateStoreCore store, string? scopeId, string? bindingId)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return CreateBindingFailure("binding.invalid-scope", checkedScope);
        }

        if (string.IsNullOrWhiteSpace(bindingId))
        {
            return CreateBindingFailure("binding.invalid-id", checkedScope);
        }

        int index = FindBindingIndex(checkedScope, bindingId);
        if (index < 0)
        {
            return CreateBindingFailure("binding.unknown", checkedScope);
        }

        return CreateBindingSuccess(store, Bindings[index], false);
    }

    internal BindingOperationResult DetachBinding(StateStoreCore store, string? scopeId, string? bindingId)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return CreateBindingFailure("binding.invalid-scope", checkedScope);
        }

        if (string.IsNullOrWhiteSpace(bindingId))
        {
            return CreateBindingFailure("binding.invalid-id", checkedScope);
        }

        int index = FindBindingIndex(checkedScope, bindingId);
        if (index < 0)
        {
            return CreateBindingFailure("binding.unknown", checkedScope);
        }

        BindingRegistration registration = Bindings[index];
        BindingOperationResult result = CreateBindingSuccess(store, registration, true);
        Bindings.RemoveAt(index);
        return result;
    }

    internal BindingTargetResult EvaluateBindingTarget(StateStoreCore store, string? scopeId, string? bindingId)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (string.IsNullOrWhiteSpace(bindingId))
        {
            return CreateBindingTargetFailure("binding.invalid-id");
        }

        int index = FindBindingIndex(checkedScope, bindingId);
        if (index < 0)
        {
            return CreateBindingTargetFailure("binding.unknown");
        }

        BindingRegistration registration = Bindings[index];
        if (registration.Binding.Mode == UiBindingMode.OneWayToSource)
        {
            return CreateBindingTargetNoUpdate(store, registration, "binding.source-only");
        }

        if (registration.Binding.Mode == UiBindingMode.OneTime && registration.OneTimeCompleted)
        {
            return CreateBindingTargetNoUpdate(store, registration, string.Empty);
        }

        BindingSourceValueResult source = ResolveBindingSourceValue(store, registration);
        BindingTargetDescriptor descriptor = ParseBindingTarget(registration.Binding.Target);
        if (!descriptor.Success)
        {
            return CreateBindingTargetFailure(descriptor.Code);
        }

        string bindingOrigin = CreateBindingOrigin(registration);
        if (source.Success && ValueComparer.TextEquals(source.Origin, bindingOrigin))
        {
            return CreateBindingTargetNoUpdate(store, registration, "binding.origin-loop");
        }

        BindingValueResult prepared = PrepareBindingTargetValue(store, registration, descriptor, source, bindingOrigin);
        if (!prepared.Success || prepared.Value == null)
        {
            BindingTargetResult failure = CreateBindingTargetFailure(prepared.Code);
            failure.Diagnostics = CopyBindingDiagnosticArray(prepared.Diagnostics);
            registration.Binding.Conversion = prepared.Conversion == null ? null : CopyBindingConversion(store, prepared.Conversion);
            failure.Binding = CopyBinding(store, registration.Binding);
            return failure;
        }

        BindingSourceValueResult projectedSource = new BindingSourceValueResult();
        projectedSource.Success = true;
        projectedSource.Value = prepared.Value;
        projectedSource.Version = source.Version;
        projectedSource.Origin = bindingOrigin;
        projectedSource.IsDerived = source.IsDerived;
        BindingTargetResult result = ProjectBindingTargetValue(store, registration, descriptor, projectedSource);
        result.Diagnostics = CopyBindingDiagnosticArray(prepared.Diagnostics);
        registration.Binding.Context = CreateBindingContext(registration.Binding.Id, registration.ScopeId, bindingOrigin, source.Version, false);
        registration.Binding.Update = CopyBindingUpdate(store, result.Update);
        registration.Binding.Conversion = prepared.Conversion == null ? null : CopyBindingConversion(store, prepared.Conversion);
        if (registration.Binding.Mode == UiBindingMode.OneTime)
        {
            registration.OneTimeCompleted = true;
        }

        result.Binding = CopyBinding(store, registration.Binding);
        return result;
    }

    internal BindingOperationResult UpdateBindingSource(StateStoreCore store, string? scopeId, string? bindingId, UiValue? value, string? origin)
    {
        return ApplyBindingSourceValue(store, scopeId, bindingId, value, origin);
    }

    internal BindingOperationResult UpdateTriggeredBindingSource(StateStoreCore store, string? scopeId, string? bindingId, UiValue? value, string? origin, UiBindingTrigger trigger)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if ((int)trigger < (int)UiBindingTrigger.Immediate || (int)trigger > (int)UiBindingTrigger.Explicit)
        {
            return CreateBindingFailure("binding.invalid-trigger", checkedScope);
        }

        if (string.IsNullOrWhiteSpace(bindingId))
        {
            return CreateBindingFailure("binding.invalid-id", checkedScope);
        }

        int index = FindBindingIndex(checkedScope, bindingId);
        if (index < 0)
        {
            return CreateBindingFailure("binding.unknown", checkedScope);
        }

        BindingRegistration registration = Bindings[index];
        if (registration.Binding.Trigger != trigger)
        {
            return CreateBindingNoUpdate(store, registration, "binding.trigger-mismatch");
        }

        return ApplyBindingSourceValue(store, scopeId, bindingId, value, origin);
    }

    internal BindingOperationResult ApplyBindingSourceValue(StateStoreCore store, string? scopeId, string? bindingId, UiValue? value, string? origin)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (string.IsNullOrWhiteSpace(bindingId))
        {
            return CreateBindingFailure("binding.invalid-id", checkedScope);
        }

        int index = FindBindingIndex(checkedScope, bindingId);
        if (index < 0)
        {
            return CreateBindingFailure("binding.unknown", checkedScope);
        }

        BindingRegistration registration = Bindings[index];
        if (registration.Binding.Mode != UiBindingMode.TwoWay && registration.Binding.Mode != UiBindingMode.OneWayToSource)
        {
            return CreateBindingNoUpdate(store, registration, "binding.target-read-only");
        }

        string bindingOrigin = CreateBindingOrigin(registration);
        if (ValueComparer.TextEquals(origin, bindingOrigin))
        {
            return CreateBindingNoUpdate(store, registration, "binding.origin-loop");
        }

        string valueCode = store.ValidateStateValue(value);
        if (valueCode.Length > 0 || value == null)
        {
            return CreateBindingFailure(valueCode, checkedScope);
        }

        BindingTargetDescriptor descriptor = ParseBindingTarget(registration.Binding.Target);
        if (!descriptor.Success)
        {
            return CreateBindingFailure(descriptor.Code, checkedScope);
        }

        string targetValueCode = ValidateBindingTargetValue(descriptor, value);
        if (targetValueCode.Length > 0)
        {
            return CreateBindingFailure(targetValueCode, checkedScope);
        }

        if (store.DerivedCore.FindStateDerivedIndex(checkedScope, registration.Binding.StateId) >= 0)
        {
            return CreateBindingFailure("binding.derived-source-read-only", checkedScope);
        }

        UiBindingContext conversionContext = CreateBindingContext(registration.Binding.Id, registration.ScopeId, origin ?? string.Empty, 0, true);
        BindingValueResult prepared = PrepareBindingSourceValue(store, registration, value, conversionContext);
        if (!prepared.Success || prepared.Value == null)
        {
            BindingOperationResult failure = CreateBindingFailure(prepared.Code, checkedScope);
            failure.Diagnostics = CopyBindingDiagnosticArray(prepared.Diagnostics);
            registration.Binding.Conversion = prepared.Conversion == null ? null : CopyBindingConversion(store, prepared.Conversion);
            failure.Binding = CopyBinding(store, registration.Binding);
            return failure;
        }

        UiValue sourceValue = prepared.Value;
        StateOperationResult written;
        if (registration.Binding.Path.Length == 0)
        {
            written = store.WriteStateEntry(checkedScope, registration.Binding.StateId, sourceValue, bindingOrigin);
        }
        else
        {
            UiStatePatch patch = new UiStatePatch();
            patch.Operation = "replace";
            patch.Path = registration.Binding.Path;
            patch.Value = store.CopyStateValue(sourceValue);
            written = store.PatchStateEntry(checkedScope, registration.Binding.StateId, new UiStatePatch[] { patch }, bindingOrigin);
        }

        if (!written.Success)
        {
            BindingOperationResult failure = CreateBindingFailure(written.Code, checkedScope);
            failure.Diagnostics = new UiDiagnostic[] { CreateBindingDiagnostic(written.Code) };
            return failure;
        }

        int version = written.Entry == null ? 0 : written.Entry.Version;
        registration.Binding.Context = CreateBindingContext(registration.Binding.Id, registration.ScopeId, bindingOrigin, version, true);
        registration.Binding.Conversion = prepared.Conversion == null ? null : CopyBindingConversion(store, prepared.Conversion);
        UiBindingUpdate update = new UiBindingUpdate();
        update.BindingId = registration.Binding.Id;
        update.TargetNodeId = registration.Binding.TargetNodeId;
        update.Target = registration.Binding.Target;
        update.Value = store.CopyStateValue(sourceValue);
        update.Version = version;
        update.Origin = bindingOrigin;
        registration.Binding.Update = update;
        BindingOperationResult result = CreateBindingSuccess(store, registration, written.Applied);
        result.Diagnostics = CopyBindingDiagnosticArray(prepared.Diagnostics);
        if (written.Entry != null)
        {
            result.Entry = store.CopyStateEntry(written.Entry);
        }

        return result;
    }

    internal string ValidateBinding(StateStoreCore store, string scopeId, UiBinding? binding)
    {
        if (binding == null)
        {
            return "binding.null";
        }

        if (string.IsNullOrWhiteSpace(binding.Id))
        {
            return "binding.invalid-id";
        }

        if (string.IsNullOrWhiteSpace(binding.StateId))
        {
            return "binding.invalid-state";
        }

        if (string.IsNullOrWhiteSpace(binding.TargetNodeId))
        {
            return "binding.invalid-target-node";
        }

        if ((int)binding.Mode < (int)UiBindingMode.OneTime || (int)binding.Mode > (int)UiBindingMode.OneWayToSource)
        {
            return "binding.invalid-mode";
        }

        if ((int)binding.Trigger < (int)UiBindingTrigger.Immediate || (int)binding.Trigger > (int)UiBindingTrigger.Explicit)
        {
            return "binding.invalid-trigger";
        }

        StatePathResult path = new StatePatchCore().ParseStatePath(binding.Path);
        if (!path.Success)
        {
            return path.Code;
        }

        BindingTargetDescriptor target = ParseBindingTarget(binding.Target);
        if (!target.Success)
        {
            return target.Code;
        }

        int stateIndex = store.FindStateEntryIndex(scopeId, binding.StateId);
        int derivedIndex = store.DerivedCore.FindStateDerivedIndex(scopeId, binding.StateId);
        if (stateIndex < 0 && derivedIndex < 0)
        {
            return "binding.unknown-state";
        }

        if (derivedIndex >= 0 && (binding.Mode == UiBindingMode.TwoWay || binding.Mode == UiBindingMode.OneWayToSource))
        {
            return "binding.derived-source-read-only";
        }

        if (binding.ConverterId != null && string.IsNullOrWhiteSpace(binding.ConverterId))
        {
            return "binding.invalid-converter";
        }

        if (binding.ConverterId != null && FindBindingConverterIndex(binding.ConverterId) < 0)
        {
            return "binding.unknown-converter";
        }

        if (binding.FallbackValue != null)
        {
            string fallbackCode = store.ValidateStateValue(binding.FallbackValue);
            if (fallbackCode.Length > 0)
            {
                return fallbackCode;
            }

            string fallbackTargetCode = ValidateBindingTargetValue(target, binding.FallbackValue);
            if (fallbackTargetCode.Length > 0)
            {
                return "binding.invalid-fallback";
            }
        }

        return string.Empty;
    }

    internal BindingTargetDescriptor ParseBindingTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return new BindingTargetDescriptor { Code = "binding.invalid-target" };
        }

        if (target == "text" || target == "visibility" || target == "enabled" || target == "selection" || target == "value" || target == "items" || target == "command")
        {
            return new BindingTargetDescriptor { Success = true, Kind = target };
        }

        int separator = target.IndexOf(':');
        if (separator <= 0 || separator == target.Length - 1)
        {
            return new BindingTargetDescriptor { Code = "binding.invalid-target" };
        }

        string kind = target.Substring(0, separator);
        string name = target.Substring(separator + 1);
        if (string.IsNullOrWhiteSpace(name))
        {
            return new BindingTargetDescriptor { Code = "binding.invalid-target-name" };
        }

        if (kind != "attribute" && kind != "property" && kind != "class" && kind != "style")
        {
            return new BindingTargetDescriptor { Code = "binding.invalid-target" };
        }

        return new BindingTargetDescriptor { Success = true, Kind = kind, Name = name };
    }

    internal string ValidateBindingTargetValue(BindingTargetDescriptor target, UiValue value)
    {
        if (value.Kind == UiValueKind.Null)
        {
            return string.Empty;
        }

        if (target.Kind == "text" || target.Kind == "style" || target.Kind == "command")
        {
            return value.Kind == UiValueKind.String && value.StringValue != null ? string.Empty : "binding.target-requires-string";
        }

        if (target.Kind == "attribute")
        {
            return value.Kind == UiValueKind.String || value.Kind == UiValueKind.Boolean ? string.Empty : "binding.target-requires-text-or-boolean";
        }

        if (target.Kind == "class" || target.Kind == "visibility" || target.Kind == "enabled")
        {
            return value.Kind == UiValueKind.Boolean ? string.Empty : "binding.target-requires-boolean";
        }

        if (target.Kind == "items")
        {
            return value.Kind == UiValueKind.Array && value.ArrayValue != null ? string.Empty : "binding.target-requires-array";
        }

        return string.Empty;
    }

    internal BindingSourceValueResult ResolveBindingSourceValue(StateStoreCore store, BindingRegistration registration)
    {
        UiValue? root = null;
        int version = 0;
        string origin = string.Empty;
        bool isDerived = false;
        int stateIndex = store.FindStateEntryIndex(registration.ScopeId, registration.Binding.StateId);
        if (stateIndex >= 0)
        {
            UiStateEntry entry = store.Entries[stateIndex].Entry;
            root = entry.Value;
            version = entry.Version;
            origin = entry.Origin;
        }
        else
        {
            int derivedIndex = store.DerivedCore.FindStateDerivedIndex(registration.ScopeId, registration.Binding.StateId);
            if (derivedIndex < 0)
            {
                return new BindingSourceValueResult { Code = "binding.unknown-state" };
            }

            UiDerivedState derived = store.DerivedCore.DerivedStates[derivedIndex].Derived;
            root = derived.Value;
            version = derived.Version;
            origin = "derived";
            isDerived = true;
        }

        StatePathResult path = new StatePatchCore().ParseStatePath(registration.Binding.Path);
        if (!path.Success)
        {
            return new BindingSourceValueResult { Code = path.Code };
        }

        StatePathValueResult resolved = new StatePatchCore().ResolveStatePathValue(root, path.Tokens);
        if (!resolved.Success || resolved.Value == null)
        {
            BindingSourceValueResult failure = new BindingSourceValueResult();
            failure.Code = resolved.Code;
            failure.Version = version;
            failure.Origin = origin;
            failure.IsDerived = isDerived;
            return failure;
        }

        BindingSourceValueResult result = new BindingSourceValueResult();
        result.Success = true;
        result.Value = store.CopyStateValue(resolved.Value);
        result.Version = version;
        result.Origin = origin;
        result.IsDerived = isDerived;
        return result;
    }

    internal BindingValueResult PrepareBindingTargetValue(StateStoreCore store, BindingRegistration registration, BindingTargetDescriptor descriptor, BindingSourceValueResult source, string bindingOrigin)
    {
        if (!source.Success || source.Value == null)
        {
            return CreateBindingFallbackValue(store, registration, source.Code, null);
        }

        UiValue candidate = store.CopyStateValue(source.Value);
        UiConversionResult? conversion = null;
        if (registration.Binding.ConverterId != null)
        {
            int converterIndex = FindBindingConverterIndex(registration.Binding.ConverterId);
            IUiValueConverter? converter = converterIndex < 0 ? null : Converters[converterIndex].Converter;
            if (converter == null)
            {
                return CreateBindingFallbackValue(store, registration, "binding.unknown-converter", null);
            }

            UiBindingContext context = CreateBindingContext(registration.Binding.Id, registration.ScopeId, bindingOrigin, source.Version, false);
            UiConversionResult? callback = converter.ConvertBindingValueToTarget(store.CopyStateValue(candidate), CopyBindingContext(context));
            if (callback == null)
            {
                UiDiagnostic diagnostic = CreateBindingDiagnostic("binding.null-conversion-result");
                return CreateBindingFallbackValue(store, registration, "binding.null-conversion-result", diagnostic);
            }

            conversion = CopySafeBindingConversion(store, callback);
            if (!callback.Success || callback.Value == null)
            {
                string code = callback.Value == null && callback.Success ? "binding.null-conversion-value" : "binding.conversion-failed";
                UiDiagnostic diagnostic = callback.Diagnostic == null ? CreateBindingDiagnostic(code) : CopyBindingDiagnostic(callback.Diagnostic);
                BindingValueResult fallback = CreateBindingFallbackValue(store, registration, code, diagnostic);
                fallback.Conversion = conversion;
                return fallback;
            }

            string convertedCode = store.ValidateStateValue(callback.Value);
            if (convertedCode.Length > 0)
            {
                BindingValueResult fallback = CreateBindingFallbackValue(store, registration, convertedCode, CreateBindingDiagnostic(convertedCode));
                fallback.Conversion = conversion;
                return fallback;
            }

            candidate = store.CopyStateValue(callback.Value);
        }

        if (candidate.Kind == UiValueKind.Null && registration.Binding.FallbackValue != null)
        {
            BindingValueResult fallback = CreateBindingFallbackValue(store, registration, string.Empty, null);
            fallback.Conversion = conversion;
            return fallback;
        }

        string targetCode = ValidateBindingTargetValue(descriptor, candidate);
        if (targetCode.Length > 0)
        {
            BindingValueResult fallback = CreateBindingFallbackValue(store, registration, targetCode, CreateBindingDiagnostic(targetCode));
            fallback.Conversion = conversion;
            return fallback;
        }

        BindingValueResult result = new BindingValueResult();
        result.Success = true;
        result.Value = candidate;
        result.Conversion = conversion;
        return result;
    }

    internal BindingValueResult PrepareBindingSourceValue(StateStoreCore store, BindingRegistration registration, UiValue value, UiBindingContext context)
    {
        if (registration.Binding.ConverterId == null)
        {
            return new BindingValueResult { Success = true, Value = store.CopyStateValue(value) };
        }

        int converterIndex = FindBindingConverterIndex(registration.Binding.ConverterId);
        IUiValueConverter? converter = converterIndex < 0 ? null : Converters[converterIndex].Converter;
        if (converter == null)
        {
            return CreateBindingValueFailure("binding.unknown-converter", null);
        }

        UiConversionResult? callback = converter.ConvertBindingValueToSource(store.CopyStateValue(value), CopyBindingContext(context));
        if (callback == null)
        {
            return CreateBindingValueFailure("binding.null-conversion-result", null);
        }

        UiConversionResult conversion = CopySafeBindingConversion(store, callback);
        if (!callback.Success || callback.Value == null)
        {
            string code = callback.Value == null && callback.Success ? "binding.null-conversion-value" : "binding.conversion-failed";
            UiDiagnostic? diagnostic = callback.Diagnostic == null ? null : CopyBindingDiagnostic(callback.Diagnostic);
            BindingValueResult failure = CreateBindingValueFailure(code, diagnostic);
            failure.Conversion = conversion;
            return failure;
        }

        string valueCode = store.ValidateStateValue(callback.Value);
        if (valueCode.Length > 0)
        {
            BindingValueResult failure = CreateBindingValueFailure(valueCode, null);
            failure.Conversion = conversion;
            return failure;
        }

        BindingValueResult result = new BindingValueResult();
        result.Success = true;
        result.Value = store.CopyStateValue(callback.Value);
        result.Conversion = conversion;
        return result;
    }

    internal BindingValueResult CreateBindingFallbackValue(StateStoreCore store, BindingRegistration registration, string code, UiDiagnostic? diagnostic)
    {
        if (registration.Binding.FallbackValue == null)
        {
            return CreateBindingValueFailure(code, diagnostic);
        }

        BindingValueResult result = new BindingValueResult();
        result.Success = true;
        result.UsedFallback = true;
        result.Value = store.CopyStateValue(registration.Binding.FallbackValue);
        if (diagnostic != null)
        {
            result.Diagnostics = new UiDiagnostic[] { CopyBindingDiagnostic(diagnostic) };
        }
        else if (code.Length > 0)
        {
            result.Diagnostics = new UiDiagnostic[] { CreateBindingDiagnostic(code) };
        }

        return result;
    }

    internal BindingValueResult CreateBindingValueFailure(string code, UiDiagnostic? diagnostic)
    {
        BindingValueResult result = new BindingValueResult();
        result.Code = code;
        result.Diagnostics = new UiDiagnostic[] { diagnostic == null ? CreateBindingDiagnostic(code) : CopyBindingDiagnostic(diagnostic) };
        return result;
    }

    internal BindingTargetResult ProjectBindingTargetValue(StateStoreCore store, BindingRegistration registration, BindingTargetDescriptor descriptor, BindingSourceValueResult source)
    {
        UiValue value = source.Value ?? new UiValue();
        UiBindingUpdate update = new UiBindingUpdate();
        update.BindingId = registration.Binding.Id;
        update.TargetNodeId = registration.Binding.TargetNodeId;
        update.Target = registration.Binding.Target;
        update.Value = store.CopyStateValue(value);
        update.Version = source.Version;
        update.Origin = source.Origin;
        BindingTargetResult result = new BindingTargetResult();
        result.Success = true;
        result.Applied = true;
        result.Kind = descriptor.Kind;
        result.Name = descriptor.Name;
        result.Update = CopyBindingUpdate(store, update);
        result.Value = store.CopyStateValue(value);
        if (descriptor.Kind == "text")
        {
            result.Text = value.Kind == UiValueKind.Null ? string.Empty : value.StringValue;
        }
        else if (descriptor.Kind == "attribute")
        {
            DomAttribute attribute = new DomAttribute();
            attribute.Name = descriptor.Name;
            if (value.Kind == UiValueKind.Null)
            {
                result.RemoveTarget = true;
            }
            else if (value.Kind == UiValueKind.Boolean)
            {
                attribute.IsBoolean = true;
                result.BooleanValue = value.BooleanValue;
                result.RemoveTarget = !value.BooleanValue;
            }
            else
            {
                attribute.Value = value.StringValue ?? string.Empty;
            }

            result.Attribute = attribute;
        }
        else if (descriptor.Kind == "property")
        {
            result.Property = new DomProperty { Name = descriptor.Name, Value = store.CopyStateValue(value) };
        }
        else if (descriptor.Kind == "class" || descriptor.Kind == "visibility" || descriptor.Kind == "enabled")
        {
            result.BooleanValue = value.Kind == UiValueKind.Null ? false : value.BooleanValue;
        }
        else if (descriptor.Kind == "style")
        {
            result.Text = value.Kind == UiValueKind.Null ? string.Empty : value.StringValue;
            result.RemoveTarget = value.Kind == UiValueKind.Null;
        }
        else if (descriptor.Kind == "items")
        {
            result.Items = CopyBindingValueArray(store, value.ArrayValue);
        }
        else if (descriptor.Kind == "command")
        {
            result.CommandId = value.Kind == UiValueKind.Null ? null : value.StringValue;
            result.RemoveTarget = value.Kind == UiValueKind.Null;
        }

        return result;
    }

    internal BindingTargetResult CreateBindingTargetNoUpdate(StateStoreCore store, BindingRegistration registration, string code)
    {
        BindingTargetResult result = new BindingTargetResult();
        result.Success = true;
        result.Code = code;
        result.Binding = CopyBinding(store, registration.Binding);
        return result;
    }

    internal BindingTargetResult CreateBindingTargetFailure(string code)
    {
        BindingTargetResult result = new BindingTargetResult();
        result.Code = code;
        result.Diagnostics = new UiDiagnostic[] { CreateBindingDiagnostic(code) };
        return result;
    }

    internal BindingOperationResult CreateBindingNoUpdate(StateStoreCore store, BindingRegistration registration, string code)
    {
        BindingOperationResult result = CreateBindingSuccess(store, registration, false);
        result.Code = code;
        return result;
    }

    internal BindingOperationResult CreateBindingSuccess(StateStoreCore store, BindingRegistration registration, bool applied)
    {
        BindingOperationResult result = new BindingOperationResult();
        result.Success = true;
        result.Applied = applied;
        result.ScopeId = registration.ScopeId;
        result.Binding = CopyBinding(store, registration.Binding);
        return result;
    }

    internal BindingOperationResult CreateBindingFailure(string code, string scopeId)
    {
        BindingOperationResult result = new BindingOperationResult();
        result.Code = code;
        result.ScopeId = scopeId;
        result.Diagnostics = new UiDiagnostic[] { CreateBindingDiagnostic(code) };
        return result;
    }

    internal UiBindingContext CreateBindingContext(string bindingId, string scopeId, string origin, int version, bool toSource)
    {
        UiBindingContext result = new UiBindingContext();
        result.BindingId = bindingId;
        result.ScopeId = scopeId.Length == 0 ? null : scopeId;
        result.Origin = origin;
        result.Version = version;
        result.ToSource = toSource;
        return result;
    }

    internal string CreateBindingOrigin(BindingRegistration registration)
    {
        return "binding:" + registration.ScopeId + ":" + registration.Binding.Id;
    }

    internal int FindBindingIndex(string scopeId, string bindingId)
    {
        int index = 0;
        while (index < Bindings.Count)
        {
            int comparison = CompareBindingIdentity(Bindings[index].ScopeId, Bindings[index].Binding.Id, scopeId, bindingId);
            if (comparison == 0)
            {
                return index;
            }

            if (comparison > 0)
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    internal int CompareBindingIdentity(string leftScopeId, string leftBindingId, string rightScopeId, string rightBindingId)
    {
        int scopeComparison = ValueComparer.CompareText(leftScopeId, rightScopeId);
        if (scopeComparison != 0)
        {
            return scopeComparison;
        }

        return ValueComparer.CompareText(leftBindingId, rightBindingId);
    }

    internal void InsertBinding(BindingRegistration registration)
    {
        int index = 0;
        while (index < Bindings.Count && CompareBindingIdentity(Bindings[index].ScopeId, Bindings[index].Binding.Id, registration.ScopeId, registration.Binding.Id) < 0)
        {
            index++;
        }

        Bindings.Insert(index, registration);
    }

    internal int FindBindingConverterIndex(string converterId)
    {
        int index = 0;
        while (index < Converters.Count)
        {
            int comparison = ValueComparer.CompareText(Converters[index].Id, converterId);
            if (comparison == 0)
            {
                return index;
            }

            if (comparison > 0)
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    internal void InsertBindingConverter(BindingConverterRegistration registration)
    {
        int index = 0;
        while (index < Converters.Count && ValueComparer.CompareText(Converters[index].Id, registration.Id) < 0)
        {
            index++;
        }

        Converters.Insert(index, registration);
    }

    internal UiBinding CopyBinding(StateStoreCore store, UiBinding source)
    {
        UiBinding result = new UiBinding();
        result.Id = source.Id;
        result.StateId = source.StateId;
        result.Path = source.Path;
        result.TargetNodeId = source.TargetNodeId;
        result.Target = source.Target;
        result.Mode = source.Mode;
        result.Trigger = source.Trigger;
        result.ConverterId = source.ConverterId;
        if (source.FallbackValue != null)
        {
            result.FallbackValue = store.CopyStateValue(source.FallbackValue);
        }

        if (source.Context != null)
        {
            result.Context = CopyBindingContext(source.Context);
        }

        if (source.Update != null)
        {
            result.Update = CopyBindingUpdate(store, source.Update);
        }

        if (source.Conversion != null)
        {
            result.Conversion = CopyBindingConversion(store, source.Conversion);
        }

        return result;
    }

    internal UiBindingContext CopyBindingContext(UiBindingContext source)
    {
        UiBindingContext result = new UiBindingContext();
        result.BindingId = source.BindingId;
        result.ScopeId = source.ScopeId;
        result.Origin = source.Origin;
        result.Version = source.Version;
        result.ToSource = source.ToSource;
        return result;
    }

    internal UiBindingUpdate? CopyBindingUpdate(StateStoreCore store, UiBindingUpdate? source)
    {
        if (source == null)
        {
            return null;
        }

        UiBindingUpdate result = new UiBindingUpdate();
        result.BindingId = source.BindingId;
        result.TargetNodeId = source.TargetNodeId;
        result.Target = source.Target;
        result.Value = store.CopyStateValue(source.Value);
        result.Version = source.Version;
        result.Origin = source.Origin;
        return result;
    }

    internal UiConversionResult CopyBindingConversion(StateStoreCore store, UiConversionResult source)
    {
        UiConversionResult result = new UiConversionResult();
        result.Success = source.Success;
        if (source.Value != null)
        {
            result.Value = store.CopyStateValue(source.Value);
        }

        if (source.Diagnostic != null)
        {
            UiDiagnostic diagnostic = new UiDiagnostic();
            diagnostic.Severity = source.Diagnostic.Severity;
            diagnostic.Code = source.Diagnostic.Code;
            diagnostic.Message = source.Diagnostic.Message;
            diagnostic.Location = source.Diagnostic.Location;
            result.Diagnostic = diagnostic;
        }

        return result;
    }

    internal UiConversionResult CopySafeBindingConversion(StateStoreCore store, UiConversionResult source)
    {
        UiConversionResult result = new UiConversionResult();
        result.Success = source.Success;
        if (source.Value != null && store.ValidateStateValue(source.Value).Length == 0)
        {
            result.Value = store.CopyStateValue(source.Value);
        }

        if (source.Diagnostic != null)
        {
            result.Diagnostic = CopyBindingDiagnostic(source.Diagnostic);
        }

        return result;
    }

    internal UiValue[] CopyBindingValueArray(StateStoreCore store, UiValue[]? source)
    {
        UiValue[] values = source ?? Array.Empty<UiValue>();
        UiValue[] result = new UiValue[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = store.CopyStateValue(values[index]);
            index++;
        }

        return result;
    }

    internal UiDiagnostic CreateBindingDiagnostic(string code)
    {
        UiDiagnostic result = new UiDiagnostic();
        result.Severity = UiDiagnosticSeverity.Error;
        result.Code = code;
        result.Message = "The binding operation reported code '" + code + "'.";
        result.Location = "/binding";
        return result;
    }

    internal UiDiagnostic CopyBindingDiagnostic(UiDiagnostic source)
    {
        UiDiagnostic result = new UiDiagnostic();
        result.Severity = source.Severity;
        result.Code = source.Code;
        result.Message = source.Message;
        result.Location = source.Location;
        return result;
    }

    internal UiDiagnostic[] CopyBindingDiagnosticArray(UiDiagnostic[]? source)
    {
        UiDiagnostic[] diagnostics = source ?? Array.Empty<UiDiagnostic>();
        UiDiagnostic[] result = new UiDiagnostic[diagnostics.Length];
        int index = 0;
        while (index < diagnostics.Length)
        {
            result[index] = CopyBindingDiagnostic(diagnostics[index]);
            index++;
        }

        return result;
    }
}
