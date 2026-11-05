using System;
using System.Collections.Generic;

namespace htmlgui;

internal class FormOperationResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal UiForm? Form;
    internal UiField? Field;
}

internal class FormCore
{
    internal List<UiForm> Forms = new List<UiForm>();
    internal StateStoreCore ValueStore = new StateStoreCore();

    internal FormOperationResult RegisterForm(UiForm? form)
    {
        string validationCode = ValidateForm(form);
        if (validationCode.Length > 0 || form == null)
        {
            return CreateFormFailure(validationCode);
        }

        if (FindFormIndex(form.Key) >= 0)
        {
            return CreateFormFailure("form.duplicate");
        }

        UiForm registered = CopyFormDefinition(form);
        registered.Fields = Array.Empty<UiField>();
        registered.State = new UiFormState();
        registered.Validation = new UiValidationReport();
        int index = 0;
        while (index < form.Fields.Length)
        {
            UiField field = CopyFormField(form.Fields[index]);
            ResetFormFieldInteraction(field);
            InsertFormField(registered, field);
            index++;
        }

        InitializeFormState(registered);
        InsertForm(registered);
        return CreateFormSuccess(registered, null, true);
    }

    internal FormOperationResult ReadForm(string? formKey)
    {
        if (string.IsNullOrWhiteSpace(formKey))
        {
            return CreateFormFailure("form.invalid-key");
        }

        int index = FindFormIndex(formKey);
        if (index < 0)
        {
            return CreateFormFailure("form.unknown");
        }

        return CreateFormSuccess(Forms[index], null, false);
    }

    internal FormOperationResult UnregisterForm(string? formKey)
    {
        if (string.IsNullOrWhiteSpace(formKey))
        {
            return CreateFormFailure("form.invalid-key");
        }

        int index = FindFormIndex(formKey);
        if (index < 0)
        {
            return CreateFormFailure("form.unknown");
        }

        UiForm removed = Forms[index];
        Forms.RemoveAt(index);
        return CreateFormSuccess(removed, null, true);
    }

    internal FormOperationResult RegisterFormField(string? formKey, UiField? field)
    {
        int formIndex = FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateFormFailure(GetFormLookupCode(formKey));
        }

        string validationCode = ValidateFormField(field);
        if (validationCode.Length > 0 || field == null)
        {
            return CreateFormFailure(validationCode);
        }

        UiForm form = Forms[formIndex];
        if (FindFormFieldIndex(form, field.Key) >= 0)
        {
            return CreateFormFailure("form.duplicate-field");
        }

        if (!CanAdvanceFormVersion(form))
        {
            return CreateFormFailure("form.version-exhausted");
        }

        UiField registered = CopyFormField(field);
        ResetFormFieldInteraction(registered);
        InsertFormField(form, registered);
        InsertValueEntry(form.State.InitialValues, registered.Key, registered.Value, true, form);
        form.State.Version++;
        RefreshFormState(form);
        return CreateFormSuccess(form, registered, true);
    }

    internal FormOperationResult UnregisterFormField(string? formKey, string? fieldKey)
    {
        int formIndex = FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateFormFailure(GetFormLookupCode(formKey));
        }

        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            return CreateFormFailure("form.invalid-field-key");
        }

        UiForm form = Forms[formIndex];
        int fieldIndex = FindFormFieldIndex(form, fieldKey);
        if (fieldIndex < 0)
        {
            return CreateFormFailure("form.unknown-field");
        }

        if (!CanAdvanceFormVersion(form))
        {
            return CreateFormFailure("form.version-exhausted");
        }

        UiField removed = form.Fields[fieldIndex];
        form.Fields = RemoveFormField(form.Fields, fieldIndex);
        form.State.InitialValues = RemoveValueEntry(form.State.InitialValues, fieldKey);
        form.State.Version++;
        RefreshFormState(form);
        return CreateFormSuccess(form, removed, true);
    }

    internal FormOperationResult UpdateFormFieldValue(string? formKey, string? fieldKey, UiValue? value)
    {
        FormOperationResult lookup = FindFormField(formKey, fieldKey);
        if (!lookup.Success || lookup.Form == null || lookup.Field == null)
        {
            return lookup;
        }

        string valueCode = ValueStore.ValidateStateValue(value);
        if (valueCode.Length > 0 || value == null)
        {
            return CreateFormFailure(valueCode);
        }

        if (ValueStore.StateValuesEqual(lookup.Field.Value, value))
        {
            return CreateFormSuccess(lookup.Form, lookup.Field, false);
        }

        if (!CanAdvanceFormVersion(lookup.Form))
        {
            return CreateFormFailure("form.version-exhausted");
        }

        lookup.Field.Value = ValueStore.CopyStateValue(value);
        if (lookup.Field.Input != null)
        {
            lookup.Field.Input.Value = ValueStore.CopyStateValue(value);
        }

        lookup.Form.State.Version++;
        RefreshFormState(lookup.Form);
        return CreateFormSuccess(lookup.Form, lookup.Field, true);
    }

    internal FormOperationResult TouchFormField(string? formKey, string? fieldKey)
    {
        FormOperationResult lookup = FindFormField(formKey, fieldKey);
        if (!lookup.Success || lookup.Form == null || lookup.Field == null)
        {
            return lookup;
        }

        if (lookup.Field.IsTouched)
        {
            return CreateFormSuccess(lookup.Form, lookup.Field, false);
        }

        lookup.Field.IsTouched = true;
        return CreateFormSuccess(lookup.Form, lookup.Field, true);
    }

    internal FormOperationResult VisitFormField(string? formKey, string? fieldKey)
    {
        FormOperationResult lookup = FindFormField(formKey, fieldKey);
        if (!lookup.Success || lookup.Form == null || lookup.Field == null)
        {
            return lookup;
        }

        if (lookup.Field.IsVisited)
        {
            return CreateFormSuccess(lookup.Form, lookup.Field, false);
        }

        lookup.Field.IsVisited = true;
        return CreateFormSuccess(lookup.Form, lookup.Field, true);
    }

    internal FormOperationResult SetFormFieldPending(string? formKey, string? fieldKey, bool pending)
    {
        FormOperationResult lookup = FindFormField(formKey, fieldKey);
        if (!lookup.Success || lookup.Form == null || lookup.Field == null)
        {
            return lookup;
        }

        if (lookup.Field.IsPending == pending)
        {
            return CreateFormSuccess(lookup.Form, lookup.Field, false);
        }

        lookup.Field.IsPending = pending;
        RefreshFormState(lookup.Form);
        return CreateFormSuccess(lookup.Form, lookup.Field, true);
    }

    internal FormOperationResult SetFormFieldValidity(string? formKey, string? fieldKey, bool valid)
    {
        FormOperationResult lookup = FindFormField(formKey, fieldKey);
        if (!lookup.Success || lookup.Form == null || lookup.Field == null)
        {
            return lookup;
        }

        if (lookup.Field.IsValid == valid)
        {
            return CreateFormSuccess(lookup.Form, lookup.Field, false);
        }

        lookup.Field.IsValid = valid;
        RefreshFormState(lookup.Form);
        return CreateFormSuccess(lookup.Form, lookup.Field, true);
    }

    internal FormOperationResult ResetForm(string? formKey)
    {
        return RestoreInitialFormState(formKey, false);
    }

    internal FormOperationResult CancelForm(string? formKey)
    {
        return RestoreInitialFormState(formKey, true);
    }

    internal FormOperationResult BeginFormSubmit(string? formKey)
    {
        int index = FindCheckedFormIndex(formKey);
        if (index < 0)
        {
            return CreateFormFailure(GetFormLookupCode(formKey));
        }

        UiForm form = Forms[index];
        if (form.State.IsSubmitting)
        {
            return CreateFormFailure("form.submit-active");
        }

        if (!form.State.CanSubmit)
        {
            return CreateFormFailure("form.cannot-submit");
        }

        form.State.IsSubmitting = true;
        RefreshFormState(form);
        return CreateFormSuccess(form, null, true);
    }

    internal FormOperationResult CompleteFormSubmit(string? formKey, bool success)
    {
        int index = FindCheckedFormIndex(formKey);
        if (index < 0)
        {
            return CreateFormFailure(GetFormLookupCode(formKey));
        }

        UiForm form = Forms[index];
        if (!form.State.IsSubmitting)
        {
            return CreateFormFailure("form.no-submit");
        }

        form.State.IsSubmitting = false;
        if (success)
        {
            form.State.InitialValues = CopyValueEntries(form.State.CurrentValues);
            int fieldIndex = 0;
            while (fieldIndex < form.Fields.Length)
            {
                form.Fields[fieldIndex].IsDirty = false;
                fieldIndex++;
            }
        }

        RefreshFormState(form);
        return CreateFormSuccess(form, null, true);
    }

    internal FormOperationResult RestoreInitialFormState(string? formKey, bool cancel)
    {
        int index = FindCheckedFormIndex(formKey);
        if (index < 0)
        {
            return CreateFormFailure(GetFormLookupCode(formKey));
        }

        UiForm form = Forms[index];
        bool valueChanged = FormValuesDiffer(form);
        bool interactionChanged = FormInteractionActive(form) || form.State.IsSubmitting;
        if (valueChanged && !CanAdvanceFormVersion(form))
        {
            return CreateFormFailure("form.version-exhausted");
        }

        int fieldIndex = 0;
        while (fieldIndex < form.Fields.Length)
        {
            UiField field = form.Fields[fieldIndex];
            int valueIndex = FindValueEntryIndex(form.State.InitialValues, field.Key);
            if (valueIndex >= 0)
            {
                field.Value = ValueStore.CopyStateValue(form.State.InitialValues[valueIndex].Value);
                if (field.Input != null)
                {
                    field.Input.Value = ValueStore.CopyStateValue(field.Value);
                }
            }

            ResetFormFieldInteraction(field);
            fieldIndex++;
        }

        if (valueChanged)
        {
            form.State.Version++;
        }

        form.State.IsSubmitting = false;
        RefreshFormState(form);
        bool applied = valueChanged || interactionChanged || cancel;
        return CreateFormSuccess(form, null, applied);
    }

    internal FormOperationResult FindFormField(string? formKey, string? fieldKey)
    {
        int formIndex = FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateFormFailure(GetFormLookupCode(formKey));
        }

        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            return CreateFormFailure("form.invalid-field-key");
        }

        UiForm form = Forms[formIndex];
        int fieldIndex = FindFormFieldIndex(form, fieldKey);
        if (fieldIndex < 0)
        {
            return CreateFormFailure("form.unknown-field");
        }

        FormOperationResult result = new FormOperationResult();
        result.Success = true;
        result.Form = form;
        result.Field = form.Fields[fieldIndex];
        return result;
    }

    internal string ValidateForm(UiForm? form)
    {
        if (form == null)
        {
            return "form.null";
        }

        if (string.IsNullOrWhiteSpace(form.Key))
        {
            return "form.invalid-key";
        }

        if (form.Fields == null)
        {
            return "form.null-fields";
        }

        string ruleCode = ValidateFormRuleValues(form.Rules);
        if (ruleCode.Length > 0)
        {
            return ruleCode;
        }

        int index = 0;
        while (index < form.Fields.Length)
        {
            UiField? field = form.Fields[index];
            string fieldCode = ValidateFormField(field);
            if (fieldCode.Length > 0 || field == null)
            {
                return fieldCode;
            }

            int priorIndex = 0;
            while (priorIndex < index)
            {
                if (ValueComparer.TextEquals(form.Fields[priorIndex].Key, field.Key))
                {
                    return "form.duplicate-field";
                }

                priorIndex++;
            }

            index++;
        }

        return string.Empty;
    }

    internal string ValidateFormField(UiField? field)
    {
        if (field == null)
        {
            return "form.null-field";
        }

        if (string.IsNullOrWhiteSpace(field.Key))
        {
            return "form.invalid-field-key";
        }

        if (string.IsNullOrWhiteSpace(field.Name))
        {
            return "form.invalid-field-name";
        }

        string valueCode = ValueStore.ValidateStateValue(field.Value);
        if (valueCode.Length > 0)
        {
            return valueCode;
        }

        if (field.Input != null)
        {
            valueCode = ValidateFormInputValues(field.Input);
            if (valueCode.Length > 0)
            {
                return valueCode;
            }
        }

        valueCode = ValidateFormChoiceValues(field.Choices);
        if (valueCode.Length > 0)
        {
            return valueCode;
        }

        valueCode = ValidateFormRuleValues(field.Rules);
        if (valueCode.Length > 0)
        {
            return valueCode;
        }

        return ValidateFormIssues(field.Issues);
    }

    internal string ValidateFormInputValues(UiInput input)
    {
        string code = ValueStore.ValidateStateValue(input.Value);
        if (code.Length > 0)
        {
            return code;
        }

        if (input.Minimum != null)
        {
            code = ValueStore.ValidateStateValue(input.Minimum);
        }

        if (code.Length == 0 && input.Maximum != null)
        {
            code = ValueStore.ValidateStateValue(input.Maximum);
        }

        if (code.Length == 0 && input.Step != null)
        {
            code = ValueStore.ValidateStateValue(input.Step);
        }

        if (code.Length > 0)
        {
            return code;
        }

        return ValidateFormChoiceValues(input.Choices);
    }

    internal string ValidateFormChoiceValues(UiChoice[]? choices)
    {
        UiChoice[] values = choices ?? Array.Empty<UiChoice>();
        int index = 0;
        while (index < values.Length)
        {
            UiChoice? choice = values[index];
            if (choice == null)
            {
                return "form.null-choice";
            }

            string code = ValueStore.ValidateStateValue(choice.Value);
            if (code.Length > 0)
            {
                return code;
            }

            index++;
        }

        return string.Empty;
    }

    internal string ValidateFormRuleValues(UiValidationRule[]? rules)
    {
        UiValidationRule[] values = rules ?? Array.Empty<UiValidationRule>();
        int ruleIndex = 0;
        while (ruleIndex < values.Length)
        {
            UiValidationRule? rule = values[ruleIndex];
            if (rule == null)
            {
                return "form.null-rule";
            }

            UiValueEntry[] parameters = rule.Parameters ?? Array.Empty<UiValueEntry>();
            int parameterIndex = 0;
            while (parameterIndex < parameters.Length)
            {
                UiValueEntry? parameter = parameters[parameterIndex];
                if (parameter == null)
                {
                    return "form.null-rule-parameter";
                }

                string code = ValueStore.ValidateStateValue(parameter.Value);
                if (code.Length > 0)
                {
                    return code;
                }

                parameterIndex++;
            }

            ruleIndex++;
        }

        return string.Empty;
    }

    internal string ValidateFormIssues(UiValidationIssue[]? issues)
    {
        UiValidationIssue[] values = issues ?? Array.Empty<UiValidationIssue>();
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                return "form.null-issue";
            }

            index++;
        }

        return string.Empty;
    }

    internal void InitializeFormState(UiForm form)
    {
        form.State.InitialValues = CreateValuesFromFields(form.Fields);
        form.State.Version = 0;
        RefreshFormState(form);
    }

    internal void RefreshFormState(UiForm form)
    {
        form.State.CurrentValues = CreateValuesFromFields(form.Fields);
        bool dirty = false;
        bool pending = false;
        bool valid = true;
        int index = 0;
        while (index < form.Fields.Length)
        {
            UiField field = form.Fields[index];
            int initialIndex = FindValueEntryIndex(form.State.InitialValues, field.Key);
            field.IsDirty = initialIndex < 0 || !ValueStore.StateValuesEqual(field.Value, form.State.InitialValues[initialIndex].Value);
            dirty = dirty || field.IsDirty;
            pending = pending || field.IsPending;
            valid = valid && field.IsValid;
            index++;
        }

        form.State.IsDirty = dirty;
        form.State.IsPending = pending;
        form.Validation.IsPending = pending;
        form.Validation.IsValid = valid;
        form.State.CanSubmit = valid && !pending && !form.State.IsSubmitting;
    }

    internal bool FormValuesDiffer(UiForm form)
    {
        int index = 0;
        while (index < form.Fields.Length)
        {
            int initialIndex = FindValueEntryIndex(form.State.InitialValues, form.Fields[index].Key);
            if (initialIndex < 0 || !ValueStore.StateValuesEqual(form.Fields[index].Value, form.State.InitialValues[initialIndex].Value))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool FormInteractionActive(UiForm form)
    {
        int index = 0;
        while (index < form.Fields.Length)
        {
            UiField field = form.Fields[index];
            if (field.IsDirty || field.IsTouched || field.IsVisited || field.IsPending || !field.IsValid)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void ResetFormFieldInteraction(UiField field)
    {
        if (field.Input != null)
        {
            field.Input.Value = ValueStore.CopyStateValue(field.Value);
        }

        field.IsDirty = false;
        field.IsTouched = false;
        field.IsVisited = false;
        field.IsPending = false;
        field.IsValid = true;
        field.Issues = Array.Empty<UiValidationIssue>();
    }

    internal bool CanAdvanceFormVersion(UiForm form)
    {
        return form.State.Version < int.MaxValue;
    }

    internal int FindCheckedFormIndex(string? formKey)
    {
        if (string.IsNullOrWhiteSpace(formKey))
        {
            return -1;
        }

        return FindFormIndex(formKey);
    }

    internal string GetFormLookupCode(string? formKey)
    {
        return string.IsNullOrWhiteSpace(formKey) ? "form.invalid-key" : "form.unknown";
    }

    internal int FindFormIndex(string formKey)
    {
        int index = 0;
        while (index < Forms.Count)
        {
            int comparison = ValueComparer.CompareText(Forms[index].Key, formKey);
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

    internal int FindFormFieldIndex(UiForm form, string fieldKey)
    {
        int index = 0;
        while (index < form.Fields.Length)
        {
            int comparison = ValueComparer.CompareText(form.Fields[index].Key, fieldKey);
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

    internal int FindValueEntryIndex(UiValueEntry[] entries, string key)
    {
        int index = 0;
        while (index < entries.Length)
        {
            int comparison = ValueComparer.CompareText(entries[index].Key, key);
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

    internal void InsertForm(UiForm form)
    {
        int index = 0;
        while (index < Forms.Count && ValueComparer.CompareText(Forms[index].Key, form.Key) < 0)
        {
            index++;
        }

        Forms.Insert(index, form);
    }

    internal void InsertFormField(UiForm form, UiField field)
    {
        UiField[] result = new UiField[form.Fields.Length + 1];
        int sourceIndex = 0;
        int targetIndex = 0;
        bool inserted = false;
        while (targetIndex < result.Length)
        {
            if (!inserted && (sourceIndex >= form.Fields.Length || ValueComparer.CompareText(field.Key, form.Fields[sourceIndex].Key) < 0))
            {
                result[targetIndex] = field;
                inserted = true;
            }
            else
            {
                result[targetIndex] = form.Fields[sourceIndex];
                sourceIndex++;
            }

            targetIndex++;
        }

        form.Fields = result;
    }

    internal void InsertValueEntry(UiValueEntry[] source, string key, UiValue value, bool initial, UiForm form)
    {
        UiValueEntry[] result = new UiValueEntry[source.Length + 1];
        int sourceIndex = 0;
        int targetIndex = 0;
        bool inserted = false;
        while (targetIndex < result.Length)
        {
            if (!inserted && (sourceIndex >= source.Length || ValueComparer.CompareText(key, source[sourceIndex].Key) < 0))
            {
                result[targetIndex] = new UiValueEntry { Key = key, Value = ValueStore.CopyStateValue(value) };
                inserted = true;
            }
            else
            {
                result[targetIndex] = new UiValueEntry { Key = source[sourceIndex].Key, Value = ValueStore.CopyStateValue(source[sourceIndex].Value) };
                sourceIndex++;
            }

            targetIndex++;
        }

        if (initial)
        {
            form.State.InitialValues = result;
        }
        else
        {
            form.State.CurrentValues = result;
        }
    }

    internal UiField[] RemoveFormField(UiField[] source, int removedIndex)
    {
        UiField[] result = new UiField[source.Length - 1];
        int sourceIndex = 0;
        int targetIndex = 0;
        while (sourceIndex < source.Length)
        {
            if (sourceIndex != removedIndex)
            {
                result[targetIndex] = source[sourceIndex];
                targetIndex++;
            }

            sourceIndex++;
        }

        return result;
    }

    internal UiValueEntry[] RemoveValueEntry(UiValueEntry[] source, string key)
    {
        int removedIndex = FindValueEntryIndex(source, key);
        if (removedIndex < 0)
        {
            return CopyValueEntries(source);
        }

        UiValueEntry[] result = new UiValueEntry[source.Length - 1];
        int sourceIndex = 0;
        int targetIndex = 0;
        while (sourceIndex < source.Length)
        {
            if (sourceIndex != removedIndex)
            {
                result[targetIndex] = new UiValueEntry { Key = source[sourceIndex].Key, Value = ValueStore.CopyStateValue(source[sourceIndex].Value) };
                targetIndex++;
            }

            sourceIndex++;
        }

        return result;
    }

    internal UiValueEntry[] CreateValuesFromFields(UiField[] fields)
    {
        UiValueEntry[] result = new UiValueEntry[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            result[index] = new UiValueEntry { Key = fields[index].Key, Value = ValueStore.CopyStateValue(fields[index].Value) };
            index++;
        }

        return result;
    }

    internal UiValueEntry[] CopyValueEntries(UiValueEntry[] source)
    {
        UiValueEntry[] result = new UiValueEntry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = new UiValueEntry { Key = source[index].Key, Value = ValueStore.CopyStateValue(source[index].Value) };
            index++;
        }

        return result;
    }

    internal UiForm CopyFormDefinition(UiForm source)
    {
        UiForm result = new UiForm();
        result.Key = source.Key;
        result.Action = source.Action;
        result.Method = source.Method;
        result.Rules = CopyValidationRules(source.Rules);
        result.SubmitCommandId = source.SubmitCommandId;
        result.ResetCommandId = source.ResetCommandId;
        result.CancelCommandId = source.CancelCommandId;
        result.DisableNativeValidation = source.DisableNativeValidation;
        return result;
    }

    internal UiForm CopyForm(UiForm source)
    {
        UiForm result = CopyFormDefinition(source);
        result.Fields = new UiField[source.Fields.Length];
        int index = 0;
        while (index < source.Fields.Length)
        {
            result.Fields[index] = CopyFormField(source.Fields[index]);
            index++;
        }

        result.State = CopyFormState(source.State);
        result.Validation = CopyValidationReport(source.Validation);
        return result;
    }

    internal UiField CopyFormField(UiField source)
    {
        UiField result = new UiField();
        result.Key = source.Key;
        result.Name = source.Name;
        result.Label = source.Label;
        result.Value = ValueStore.CopyStateValue(source.Value);
        if (source.Input != null)
        {
            result.Input = CopyFormInput(source.Input);
        }

        result.Choices = CopyFormChoices(source.Choices);
        result.Rules = CopyValidationRules(source.Rules);
        result.Issues = CopyValidationIssues(source.Issues);
        result.IsDirty = source.IsDirty;
        result.IsTouched = source.IsTouched;
        result.IsVisited = source.IsVisited;
        result.IsPending = source.IsPending;
        result.IsValid = source.IsValid;
        return result;
    }

    internal UiInput CopyFormInput(UiInput source)
    {
        UiInput result = new UiInput();
        result.Kind = source.Kind;
        result.Name = source.Name;
        result.Value = ValueStore.CopyStateValue(source.Value);
        result.Placeholder = source.Placeholder;
        result.HelperText = source.HelperText;
        result.ErrorText = source.ErrorText;
        result.Prefix = source.Prefix;
        result.Suffix = source.Suffix;
        result.HasClearAction = source.HasClearAction;
        result.HasRevealAction = source.HasRevealAction;
        result.IsRevealed = source.IsRevealed;
        result.InputMode = source.InputMode;
        result.AutoComplete = source.AutoComplete;
        result.EnterKeyHint = source.EnterKeyHint;
        result.HasSpellCheck = source.HasSpellCheck;
        result.SpellCheck = source.SpellCheck;
        result.Minimum = CopyOptionalFormValue(source.Minimum);
        result.Maximum = CopyOptionalFormValue(source.Maximum);
        result.Step = CopyOptionalFormValue(source.Step);
        result.Pattern = source.Pattern;
        result.Choices = CopyFormChoices(source.Choices);
        result.HasMinimumLength = source.HasMinimumLength;
        result.MinimumLength = source.MinimumLength;
        result.HasMaximumLength = source.HasMaximumLength;
        result.MaximumLength = source.MaximumLength;
        result.IsRequired = source.IsRequired;
        result.IsDisabled = source.IsDisabled;
        result.Multiple = source.Multiple;
        result.AcceptedFileTypes = CopyFormTextArray(source.AcceptedFileTypes);
        result.UpdateTrigger = source.UpdateTrigger;
        return result;
    }

    internal UiValue? CopyOptionalFormValue(UiValue? source)
    {
        return source == null ? null : ValueStore.CopyStateValue(source);
    }

    internal UiChoice[] CopyFormChoices(UiChoice[]? source)
    {
        UiChoice[] values = source ?? Array.Empty<UiChoice>();
        UiChoice[] result = new UiChoice[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            UiChoice choice = values[index];
            result[index] = new UiChoice
            {
                Key = choice.Key,
                Label = choice.Label,
                Value = ValueStore.CopyStateValue(choice.Value),
                Url = choice.Url,
                GroupKey = choice.GroupKey,
                IsSelected = choice.IsSelected,
                IsDisabled = choice.IsDisabled
            };
            index++;
        }

        return result;
    }

    internal UiValidationRule[] CopyValidationRules(UiValidationRule[]? source)
    {
        UiValidationRule[] values = source ?? Array.Empty<UiValidationRule>();
        UiValidationRule[] result = new UiValidationRule[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            UiValidationRule rule = values[index];
            result[index] = new UiValidationRule
            {
                Key = rule.Key,
                Kind = rule.Kind,
                Trigger = rule.Trigger,
                Severity = rule.Severity,
                Message = rule.Message,
                HandlerId = rule.HandlerId,
                Parameters = CopyValueEntries(rule.Parameters ?? Array.Empty<UiValueEntry>())
            };
            index++;
        }

        return result;
    }

    internal UiValidationIssue[] CopyValidationIssues(UiValidationIssue[]? source)
    {
        UiValidationIssue[] values = source ?? Array.Empty<UiValidationIssue>();
        UiValidationIssue[] result = new UiValidationIssue[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = CopyValidationIssue(values[index]);
            index++;
        }

        return result;
    }

    internal UiValidationIssue CopyValidationIssue(UiValidationIssue source)
    {
        UiValidationIssue result = new UiValidationIssue();
        result.Id = source.Id;
        result.FieldKey = source.FieldKey;
        result.Code = source.Code;
        result.Message = source.Message;
        result.Severity = source.Severity;
        result.Source = source.Source;
        result.ValueVersion = source.ValueVersion;
        return result;
    }

    internal UiFormState CopyFormState(UiFormState source)
    {
        UiFormState result = new UiFormState();
        result.InitialValues = CopyValueEntries(source.InitialValues);
        result.CurrentValues = CopyValueEntries(source.CurrentValues);
        result.IsDirty = source.IsDirty;
        result.IsPending = source.IsPending;
        result.IsSubmitting = source.IsSubmitting;
        result.CanSubmit = source.CanSubmit;
        result.Version = source.Version;
        return result;
    }

    internal UiValidationReport CopyValidationReport(UiValidationReport source)
    {
        UiValidationReport result = new UiValidationReport();
        result.Issues = CopyValidationIssues(source.Issues);
        result.IsValid = source.IsValid;
        result.IsPending = source.IsPending;
        result.FirstInvalidFieldKey = source.FirstInvalidFieldKey;
        result.Version = source.Version;
        return result;
    }

    internal string[] CopyFormTextArray(string[]? source)
    {
        string[] values = source ?? Array.Empty<string>();
        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index++;
        }

        return result;
    }

    internal FormOperationResult CreateFormSuccess(UiForm form, UiField? field, bool applied)
    {
        FormOperationResult result = new FormOperationResult();
        result.Success = true;
        result.Applied = applied;
        result.Form = CopyForm(form);
        if (field != null)
        {
            result.Field = CopyFormField(field);
        }

        return result;
    }

    internal FormOperationResult CreateFormFailure(string code)
    {
        return new FormOperationResult { Code = code };
    }
}
