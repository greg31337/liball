namespace uri;

internal static class UriEditorCore
{
    internal static UriEditCoreResult BuildFromComponents(
        UriReference? components,
        UriValidationOptions? options)
    {
        UriDiagnostic? inputFailure = GetEditInputDiagnostic(components, options);
        if (inputFailure != null)
        {
            return CreateEditFailure(inputFailure);
        }

        UriReference? built = CloneEditableReference(components);
        if (built == null)
        {
            return CreateEditFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        built.Kind = UriNormalizerCore.ClassifyNormalizedReference(
            built,
            UriReferenceKind.Uri);
        built.OriginalText = "";
        return ValidateAndComposeEdit(built, options);
    }

    internal static UriEditCoreResult CopyWithChanges(
        UriReference? source,
        UriReferenceChanges? changes,
        UriValidationOptions? options)
    {
        UriDiagnostic? inputFailure = GetEditInputDiagnostic(source, options);
        if (inputFailure != null || changes == null)
        {
            if (inputFailure == null)
            {
                inputFailure = DiagnosticFactory.CreateInvalidInput(
                    DiagnosticDomain.Uri);
            }

            return CreateEditFailure(inputFailure);
        }

        UriValidationResult sourceValidation = UriValidatorCore.ValidateComponents(
            source,
            options);
        if (!sourceValidation.Success)
        {
            return CreateEditFailure(sourceValidation.Diagnostic);
        }

        UriReference? edited = CloneEditableReference(source);
        if (edited == null)
        {
            return CreateEditFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        ApplyChanges(edited, changes);
        edited.Kind = UriNormalizerCore.ClassifyNormalizedReference(
            edited,
            sourceValidation.Reference.Kind);
        edited.OriginalText = "";
        return ValidateAndComposeEdit(edited, options);
    }

    internal static void ApplyChanges(
        UriReference target,
        UriReferenceChanges changes)
    {
        if (changes.ChangeScheme)
        {
            target.HasScheme = changes.HasScheme;
            if (changes.HasScheme)
            {
                target.Scheme = changes.Scheme;
            }
            else
            {
                target.Scheme = "";
            }
        }

        if (changes.ChangeAuthority)
        {
            target.HasAuthority = changes.HasAuthority;
            if (changes.HasAuthority)
            {
                UriAuthority? authority = CloneEditableAuthority(changes.Authority);
                if (authority == null)
                {
                    target.Authority = changes.Authority;
                }
                else
                {
                    target.Authority = authority;
                }
            }
            else
            {
                target.Authority = new UriAuthority();
            }
        }

        if (changes.ChangePath)
        {
            target.Path = changes.Path;
        }

        if (changes.ChangeQuery)
        {
            target.HasQuery = changes.HasQuery;
            if (changes.HasQuery)
            {
                target.Query = changes.Query;
            }
            else
            {
                target.Query = "";
            }
        }

        if (changes.ChangeFragment)
        {
            target.HasFragment = changes.HasFragment;
            if (changes.HasFragment)
            {
                target.Fragment = changes.Fragment;
            }
            else
            {
                target.Fragment = "";
            }
        }
    }

    internal static UriReference? CloneEditableReference(UriReference? source)
    {
        if (source == null ||
            source.Scheme == null ||
            source.Authority == null ||
            source.Path == null ||
            source.Query == null ||
            source.Fragment == null)
        {
            return null;
        }

        UriAuthority? authority = CloneEditableAuthority(source.Authority);
        if (authority == null)
        {
            return null;
        }

        UriReference copy = new UriReference();
        copy.Kind = source.Kind;
        copy.HasScheme = source.HasScheme;
        copy.Scheme = source.Scheme;
        copy.HasAuthority = source.HasAuthority;
        copy.Authority = authority;
        copy.Path = source.Path;
        copy.HasQuery = source.HasQuery;
        copy.Query = source.Query;
        copy.HasFragment = source.HasFragment;
        copy.Fragment = source.Fragment;
        copy.OriginalText = "";
        return copy;
    }

    internal static UriAuthority? CloneEditableAuthority(UriAuthority? source)
    {
        if (source == null ||
            source.UserInfo == null ||
            source.Host == null ||
            source.AddressBytes == null ||
            source.Port == null)
        {
            return null;
        }

        UriAuthority copy = new UriAuthority();
        copy.HasUserInfo = source.HasUserInfo;
        copy.UserInfo = source.UserInfo;
        copy.HostKind = source.HostKind;
        copy.Host = source.Host;
        copy.AddressBytes = new byte[source.AddressBytes.Length];
        int offset = 0;
        while (offset < source.AddressBytes.Length)
        {
            copy.AddressBytes[offset] = source.AddressBytes[offset];
            offset = offset + 1;
        }

        copy.HasPort = source.HasPort;
        copy.Port = source.Port;
        return copy;
    }

    internal static UriEditCoreResult ValidateAndComposeEdit(
        UriReference reference,
        UriValidationOptions? options)
    {
        UriCompositionResult composition = UriComposerCore.ComposeValidatedReference(
            reference,
            options);
        if (!composition.Success)
        {
            return CreateEditFailure(composition.Diagnostic);
        }

        return new UriEditCoreResult(
            true,
            reference,
            composition.Text,
            composition.Length,
            null);
    }

    internal static UriDiagnostic? GetEditInputDiagnostic(
        UriReference? reference,
        UriValidationOptions? options)
    {
        if (reference == null || options == null || options.Limits == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }

        return null;
    }

    internal static UriEditCoreResult CreateEditFailure(UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriEditCoreResult(
            false,
            new UriReference(),
            "",
            0,
            failure);
    }
}
