namespace asn1;

internal class ParameterInstantiationEngine
{
    internal ActualParameterValidator Validator;
    internal ParameterizedSubstituter Substituter;
    internal System.Collections.Generic.List<ParameterInstantiationCacheEntry> Cache =
        new System.Collections.Generic.List<ParameterInstantiationCacheEntry>();

    internal ParameterInstantiationEngine(ActualParameterValidator validator)
    {
        this.Validator = validator == null
            ? throw new System.ArgumentNullException("validator")
            : validator;
        this.Substituter = new ParameterizedSubstituter(validator.Diagnostics);
    }

    internal ParameterInstantiationResult InstantiateReference(
        ParsedParameterizedReferenceSyntax reference,
        SymbolTable actualModule,
        SymbolTableEntry actualDeclaration,
        SymbolTable targetModule)
    {
        bool previous = this.Validator.CreateInstantiations;
        this.Validator.CreateInstantiations = false;
        ParameterInstantiationResult result = this.InstantiateReferenceCached(
            reference,
            actualModule,
            actualDeclaration,
            targetModule,
            new System.Collections.Generic.List<ParameterInstantiationFrame>());
        this.Validator.CreateInstantiations = previous;
        return result;
    }

    internal ParameterInstantiationResult InstantiateReferenceCached(
        ParsedParameterizedReferenceSyntax reference,
        SymbolTable actualModule,
        SymbolTableEntry actualDeclaration,
        SymbolTable targetModule,
        System.Collections.Generic.List<ParameterInstantiationFrame> stack)
    {
        SymbolTableEntry? target = reference.Target;
        ParameterSubstitutionResult? substitution = reference.Substitution;
        if (target == null || substitution == null || !substitution.Success)
        {
            return new ParameterInstantiationResult
            {
                FailureMessage = "A successful substitution is required before instantiation."
            };
        }

        string key = this.CreateInstantiationCacheKey(
            reference,
            target,
            actualModule);
        ParameterInstantiationTrace trace = this.CreateInstantiationTrace(
            reference,
            actualModule,
            actualDeclaration);
        string targetIdentity = this.CreateInstantiationTargetIdentity(target);
        if (reference.RecursionTemplateReference
            && this.FindRecursiveFrame(stack, targetIdentity) != null)
        {
            return this.InstantiateReferenceCore(
                reference,
                actualModule,
                actualDeclaration,
                targetModule,
                stack);
        }

        ParameterInstantiationCacheEntry? cached = this.FindInstantiationCacheEntry(
            key);
        if (cached != null && cached.Result != null)
        {
            return this.CreateCachedUseResult(
                cached.Result,
                trace,
                target,
                targetModule);
        }

        ParameterInstantiationResult result = this.InstantiateReferenceCore(
            reference,
            actualModule,
            actualDeclaration,
            targetModule,
            stack);
        if (result.Success && !result.Deferred)
        {
            result.CacheKey = key;
            result.StableId = this.CreateInstantiationStableId(key);
            result.Trace = trace;
            this.Cache.Add(new ParameterInstantiationCacheEntry
            {
                Key = key,
                Result = result
            });
        }

        return result;
    }

    internal ParameterInstantiationResult InstantiateReferenceCore(
        ParsedParameterizedReferenceSyntax reference,
        SymbolTable actualModule,
        SymbolTableEntry actualDeclaration,
        SymbolTable targetModule,
        System.Collections.Generic.List<ParameterInstantiationFrame> stack)
    {
        SymbolTableEntry? target = reference.Target;
        ParameterSubstitutionResult? initial = reference.Substitution;
        if (target == null || initial == null || !initial.Success)
        {
            return new ParameterInstantiationResult
            {
                FailureMessage = "A successful substitution is required before instantiation."
            };
        }

        string key = this.CreateInstantiationCacheKey(
            reference,
            target,
            actualModule);
        string targetIdentity = this.CreateInstantiationTargetIdentity(target);
        ParameterInstantiationFrame? recursiveKey =
            reference.RecursionTemplateReference
            ? this.FindRecursiveKeyFrame(stack, key)
            : null;
        ParameterInstantiationFrame? recursiveTarget =
            reference.RecursionTemplateReference
            ? this.FindRecursiveFrame(stack, targetIdentity)
            : null;
        if (recursiveTarget != null)
        {
            if (target.Kind != AsnAssignmentKind.Type)
            {
                return new ParameterInstantiationResult
                {
                    Kind = target.Kind,
                    FailureKind = ParameterInstantiationFailureKind.Recursion,
                    FailureMessage = "Recursive parameterized value, value-set, object, and object-set instantiations are not permitted."
                };
            }

            if (recursiveKey == null)
            {
                if (this.HasChangingGovernor(
                    recursiveTarget.Reference,
                    reference,
                    target))
                {
                    return new ParameterInstantiationResult
                    {
                        Kind = target.Kind,
                        FailureKind = ParameterInstantiationFailureKind.Recursion,
                        FailureMessage = "A recursive parameterized type changes a dummy governor."
                    };
                }

                if (!reference.RecursionActualsFinite)
                {
                    return new ParameterInstantiationResult
                    {
                        Kind = target.Kind,
                        FailureKind = ParameterInstantiationFailureKind.Recursion,
                        FailureMessage = "A recursive parameterized type has non-terminating actual-parameter expansion."
                    };
                }
            }

            if (recursiveKey != null
                && !reference.RecursionOptionalBoundary
                && !reference.RecursionChoiceBoundary)
            {
                return new ParameterInstantiationResult
                {
                    Kind = target.Kind,
                    FailureKind = ParameterInstantiationFailureKind.Recursion,
                    FailureMessage = "A recursive parameterized type reference is not guarded by OPTIONAL or a terminating CHOICE."
                };
            }

            if (recursiveKey != null)
            {
                return this.CreateDeferredInstantiation(
                    reference,
                    target,
                    actualDeclaration,
                    key);
            }
        }

        stack.Add(new ParameterInstantiationFrame
        {
            Key = key,
            TargetIdentity = targetIdentity,
            Reference = reference,
            Target = target,
            Declaration = actualDeclaration
        });
        ParameterSubstitutionResult current = initial;
        System.Collections.Generic.List<ParsedParameterizedReferenceSyntax> nested =
            new System.Collections.Generic.List<ParsedParameterizedReferenceSyntax>();
        System.Collections.Generic.List<ParameterInstantiationFixup> fixups =
            new System.Collections.Generic.List<ParameterInstantiationFixup>();
        bool changed = true;
        while (changed)
        {
            changed = false;
            int assignmentIndex = this.Substituter.FindAssignmentTokenIndex(
                current.Tokens);
            int index = assignmentIndex + 1;
            while (index < current.Tokens.Length)
            {
                SymbolTable originModule = this.FindReferenceOriginModule(
                    current.Origins,
                    index,
                    actualModule);
                ParameterizedTargetResolution resolution =
                    this.Validator.ResolveReferenceTarget(
                        current.Tokens,
                        index,
                        originModule);
                if (resolution.Target != null)
                {
                    ActualParameterTokenList actuals =
                        this.Validator.SplitActualParameters(
                            current.Tokens,
                            resolution.BraceIndex);
                    if (actuals.Success)
                    {
                        SymbolTableEntry detached = this.CreateDetachedDeclaration(
                            actualDeclaration,
                            current.Tokens);
                        ParsedParameterizedReferenceSyntax nestedReference =
                            this.Validator.ValidateReferenceArguments(
                                detached,
                                originModule,
                                resolution,
                                actuals);
                        nestedReference.RecursionTemplateReference =
                            resolution.StartIndex < 0
                            || resolution.StartIndex >= current.Origins.Length
                            || !current.Origins[resolution.StartIndex].Inserted;
                        nestedReference.RecursionActualsFinite =
                            this.RecursiveActualsAreFinite(actuals, current);
                        this.MarkTypeRecursionBoundary(
                            current.AssignmentSyntax == null
                                ? null
                                : current.AssignmentSyntax.TypeSyntax,
                            nestedReference,
                            current.Tokens,
                            actuals.EndIndex);
                        if (!nestedReference.Validated
                            || nestedReference.Substitution == null
                            || resolution.Target == null)
                        {
                            stack.RemoveAt(stack.Count - 1);
                            return new ParameterInstantiationResult
                            {
                                Kind = target.Kind,
                                FailureMessage = "A nested parameterized reference is invalid."
                            };
                        }

                        SymbolTable? nestedTargetModule =
                            this.Validator.ValueResolver.FindDeclaringModule(
                                resolution.Target);
                        if (nestedTargetModule == null)
                        {
                            stack.RemoveAt(stack.Count - 1);
                            return new ParameterInstantiationResult
                            {
                                Kind = target.Kind,
                                FailureMessage = "The nested target module is unavailable."
                            };
                        }

                        ParameterInstantiationResult nestedResult =
                            this.InstantiateReferenceCached(
                                nestedReference,
                                originModule,
                                detached,
                                nestedTargetModule,
                                stack);
                        if (!nestedResult.Success)
                        {
                            stack.RemoveAt(stack.Count - 1);
                            return new ParameterInstantiationResult
                            {
                                Kind = target.Kind,
                                FailureKind = nestedResult.FailureKind,
                                FailureMessage = nestedResult.FailureMessage
                            };
                        }

                        nestedReference.Instantiation = nestedResult;
                        nested.Add(nestedReference);
                        this.AppendDeferredFixups(
                            fixups,
                            nestedResult.DeferredFixups);
                        InstantiationTokenSequence replaced =
                            this.ReplaceNestedReference(
                                current,
                                resolution.StartIndex,
                                actuals.EndIndex,
                                nestedResult);
                        current = this.Substituter.ParseSubstitutedAssignment(
                            replaced.Tokens,
                            replaced.Origins,
                            target,
                            targetModule);
                        if (!current.Success)
                        {
                            stack.RemoveAt(stack.Count - 1);
                            return new ParameterInstantiationResult
                            {
                                Kind = target.Kind,
                                FailureKind = ParameterInstantiationFailureKind.InvalidSchema,
                                FailureMessage = current.FailureMessage
                            };
                        }

                        changed = true;
                        break;
                    }
                }

                index = index + 1;
            }
        }

        stack.RemoveAt(stack.Count - 1);
        InstantiationTokenSequence body = this.CreateInstantiationBody(
            current,
            target.Kind);
        return new ParameterInstantiationResult
        {
            Kind = target.Kind,
            Tokens = current.Tokens,
            Origins = current.Origins,
            BodyTokens = body.Tokens,
            BodyOrigins = body.Origins,
            AssignmentSyntax = current.AssignmentSyntax,
            NestedReferences = nested.ToArray(),
            DeferredFixups = fixups.ToArray(),
            Success = true
        };
    }

    internal string CreateInstantiationTargetIdentity(SymbolTableEntry target)
    {
        string sourceName = target.Span == null
            ? ""
            : target.Span.Start.SourceName;
        return target.Kind.ToString()
            + ":" + target.QualifiedName.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + ":" + target.QualifiedName
            + ":" + sourceName.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + ":" + sourceName;
    }

    internal ParameterInstantiationFrame? FindRecursiveFrame(
        System.Collections.Generic.List<ParameterInstantiationFrame> stack,
        string targetIdentity)
    {
        int index = stack.Count - 1;
        while (index >= 0)
        {
            if (stack[index].TargetIdentity == targetIdentity)
            {
                return stack[index];
            }

            index = index - 1;
        }

        return null;
    }

    internal ParameterInstantiationFrame? FindRecursiveKeyFrame(
        System.Collections.Generic.List<ParameterInstantiationFrame> stack,
        string key)
    {
        int index = stack.Count - 1;
        while (index >= 0)
        {
            if (stack[index].Key == key)
            {
                return stack[index];
            }

            index = index - 1;
        }

        return null;
    }

    internal bool RecursiveActualsAreFinite(
        ActualParameterTokenList actuals,
        ParameterSubstitutionResult source)
    {
        int actualIndex = 0;
        while (actualIndex < actuals.Items.Length)
        {
            bool inserted = false;
            bool template = false;
            ParsedParameterSyntax? formal = null;
            AsnToken[] tokens = actuals.Items[actualIndex];
            int tokenIndex = 0;
            while (tokenIndex < tokens.Length)
            {
                int sourceIndex = this.FindInstantiationTokenIndex(
                    source.Tokens,
                    tokens[tokenIndex]);
                SubstitutionTokenOrigin? origin = sourceIndex < 0
                    || sourceIndex >= source.Origins.Length
                    ? null
                    : source.Origins[sourceIndex];
                if (origin != null && origin.Inserted)
                {
                    inserted = true;
                    if (formal == null)
                    {
                        formal = origin.FormalParameter;
                    }
                    else if (formal != origin.FormalParameter)
                    {
                        return false;
                    }
                }
                else
                {
                    template = true;
                }

                tokenIndex = tokenIndex + 1;
            }

            if (inserted && template)
            {
                return false;
            }

            actualIndex = actualIndex + 1;
        }

        return true;
    }

    internal int FindInstantiationTokenIndex(AsnToken[] tokens, AsnToken token)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == token)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool HasChangingGovernor(
        ParsedParameterizedReferenceSyntax? previous,
        ParsedParameterizedReferenceSyntax current,
        SymbolTableEntry target)
    {
        if (previous == null)
        {
            return false;
        }

        int index = 0;
        while (index < target.FormalParameterSyntax.Length
            && index < previous.ActualParameterSyntax.Length
            && index < current.ActualParameterSyntax.Length)
        {
            if (this.FormalGovernsAnotherParameter(
                target.FormalParameterSyntax[index],
                target.FormalParameterSyntax)
                && !this.ActualParameterTokensEqual(
                    previous.ActualParameterSyntax[index].SourceTokens,
                    current.ActualParameterSyntax[index].SourceTokens))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool FormalGovernsAnotherParameter(
        ParsedParameterSyntax candidate,
        ParsedParameterSyntax[] formals)
    {
        int index = 0;
        while (index < formals.Length)
        {
            if (formals[index].DummyGovernor == candidate)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ActualParameterTokensEqual(AsnToken[] left, AsnToken[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index].Kind != right[index].Kind
                || left[index].Text != right[index].Text)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal ParameterInstantiationResult CreateDeferredInstantiation(
        ParsedParameterizedReferenceSyntax reference,
        SymbolTableEntry target,
        SymbolTableEntry declaration,
        string key)
    {
        AsnToken source = reference.SourceTokens.Length == 0
            ? new AsnToken()
            : reference.SourceTokens[0];
        AsnToken token = new AsnToken
        {
            Kind = AsnTokenKind.TypeReference,
            Text = declaration.Name,
            Span = source.Span
        };
        SubstitutionTokenOrigin origin = new SubstitutionTokenOrigin
        {
            Token = token,
            SourceToken = source,
            Declaration = declaration,
            Inserted = true
        };
        ParameterInstantiationFixup fixup = new ParameterInstantiationFixup
        {
            Reference = reference,
            Target = target,
            Token = token,
            Span = reference.Span,
            ReplacementName = declaration.Name,
            TargetStableId = this.CreateInstantiationStableId(key),
            OptionalBoundary = reference.RecursionOptionalBoundary,
            ChoiceBoundary = reference.RecursionChoiceBoundary
        };
        return new ParameterInstantiationResult
        {
            Kind = target.Kind,
            BodyTokens = [token],
            BodyOrigins = [origin],
            DeferredFixups = [fixup],
            CacheKey = key,
            StableId = fixup.TargetStableId,
            Deferred = true,
            Success = true
        };
    }

    internal void AppendDeferredFixups(
        System.Collections.Generic.List<ParameterInstantiationFixup> target,
        ParameterInstantiationFixup[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    internal void MarkTypeRecursionBoundary(
        ParsedTypeSyntax? type,
        ParsedParameterizedReferenceSyntax reference,
        AsnToken[] tokens,
        int referenceEnd)
    {
        ParameterRecursionBoundary boundary = this.FindTypeRecursionBoundary(
            type,
            reference,
            false,
            false);
        reference.RecursionOptionalBoundary = boundary.Optional
            || this.HasFollowingOptionalBoundary(tokens, referenceEnd);
        reference.RecursionChoiceBoundary = boundary.Choice;
    }

    internal ParameterRecursionBoundary FindTypeRecursionBoundary(
        ParsedTypeSyntax? type,
        ParsedParameterizedReferenceSyntax reference,
        bool optional,
        bool choice)
    {
        if (type == null)
        {
            return new ParameterRecursionBoundary();
        }

        if (type.ParameterizedReference != null
            && this.IsSameParameterizedReference(
                type.ParameterizedReference,
                reference))
        {
            return new ParameterRecursionBoundary
            {
                Found = true,
                Optional = optional,
                Choice = choice
            };
        }

        bool choiceBoundary = choice;
        if (type.Type.Kind == AsnTypeKind.Choice
            && this.HasTerminatingChoiceAlternative(type, reference.ReferenceName))
        {
            choiceBoundary = true;
        }

        int index = 0;
        while (index < type.ComponentSyntax.Length)
        {
            ParsedComponentSyntax component = type.ComponentSyntax[index];
            bool componentOptional = optional
                || (component.Component != null && component.Component.Optional);
            ParameterRecursionBoundary nested = this.FindTypeRecursionBoundary(
                component.TypeSyntax,
                reference,
                componentOptional,
                choiceBoundary);
            if (nested.Found)
            {
                return nested;
            }

            index = index + 1;
        }

        return this.FindTypeRecursionBoundary(
            type.InnerTypeSyntax,
            reference,
            optional,
            choiceBoundary);
    }

    internal bool IsSameParameterizedReference(
        ParsedParameterizedReferenceSyntax left,
        ParsedParameterizedReferenceSyntax right)
    {
        if (left.ReferenceName != right.ReferenceName
            || left.ReferenceModule != right.ReferenceModule
            || left.Span == null
            || right.Span == null)
        {
            return false;
        }

        return left.Span.Start.SourceName == right.Span.Start.SourceName
            && left.Span.Start.Offset == right.Span.Start.Offset
            && left.Span.End.Offset == right.Span.End.Offset;
    }

    internal bool HasTerminatingChoiceAlternative(
        ParsedTypeSyntax choice,
        string referenceName)
    {
        int index = 0;
        while (index < choice.ComponentSyntax.Length)
        {
            if (!this.ContainsParameterizedReference(
                choice.ComponentSyntax[index].TypeSyntax,
                referenceName))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsParameterizedReference(
        ParsedTypeSyntax? type,
        string referenceName)
    {
        if (type == null)
        {
            return false;
        }

        if (type.ParameterizedReference != null
            && type.ParameterizedReference.ReferenceName == referenceName)
        {
            return true;
        }

        int index = 0;
        while (index < type.ComponentSyntax.Length)
        {
            if (this.ContainsParameterizedReference(
                type.ComponentSyntax[index].TypeSyntax,
                referenceName))
            {
                return true;
            }

            index = index + 1;
        }

        return this.ContainsParameterizedReference(
            type.InnerTypeSyntax,
            referenceName);
    }

    internal bool HasFollowingOptionalBoundary(AsnToken[] tokens, int start)
    {
        int index = start + 1;
        while (index < tokens.Length)
        {
            if (tokens[index].Text == "OPTIONAL")
            {
                return true;
            }

            if (tokens[index].Kind == AsnTokenKind.Comma
                || tokens[index].Kind == AsnTokenKind.Assignment)
            {
                return false;
            }

            index = index + 1;
        }

        return false;
    }

    internal string CreateInstantiationCacheKey(
        ParsedParameterizedReferenceSyntax reference,
        SymbolTableEntry target,
        SymbolTable actualModule)
    {
        string sourceName = target.Span == null
            ? ""
            : target.Span.Start.SourceName;
        string key = "target="
            + target.Kind.ToString()
            + ":" + target.QualifiedName.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + ":" + target.QualifiedName
            + ":" + sourceName.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + ":" + sourceName
            + "|context=" + actualModule.ModuleName.Length.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + ":" + actualModule.ModuleName
            + "|actuals=";
        int actualIndex = 0;
        while (actualIndex < reference.ActualParameterSyntax.Length)
        {
            ParsedActualParameterSyntax actual =
                reference.ActualParameterSyntax[actualIndex];
            key = key + "[" + actual.Parameter.Kind.ToString() + ":";
            AsnToken[] tokens = actual.SourceTokens;
            int tokenIndex = 0;
            while (tokenIndex < tokens.Length)
            {
                key = key
                    + ((int)tokens[tokenIndex].Kind).ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + ":" + tokens[tokenIndex].Text.Length.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + ":" + tokens[tokenIndex].Text + ";";
                tokenIndex = tokenIndex + 1;
            }

            key = key + "]";
            actualIndex = actualIndex + 1;
        }

        return key;
    }

    internal string CreateInstantiationStableId(string key)
    {
        return "asn1.parameter-instantiation:" + key;
    }

    internal ParameterInstantiationCacheEntry? FindInstantiationCacheEntry(
        string key)
    {
        int index = 0;
        while (index < this.Cache.Count)
        {
            if (this.Cache[index].Key == key)
            {
                return this.Cache[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal ParameterInstantiationTrace CreateInstantiationTrace(
        ParsedParameterizedReferenceSyntax reference,
        SymbolTable actualModule,
        SymbolTableEntry actualDeclaration)
    {
        return new ParameterInstantiationTrace
        {
            Reference = reference,
            Module = actualModule,
            Declaration = actualDeclaration,
            Span = reference.Span,
            InputOrigins = reference.Substitution == null
                ? []
                : reference.Substitution.Origins
        };
    }

    internal ParameterInstantiationResult CreateCachedUseResult(
        ParameterInstantiationResult cached,
        ParameterInstantiationTrace trace,
        SymbolTableEntry target,
        SymbolTable targetModule)
    {
        ParameterInstantiationTrace canonicalTrace = cached.Trace
            ?? throw new System.InvalidOperationException();
        SubstitutionTokenOrigin[] origins = this.CloneOriginsForUse(
            cached.Origins,
            canonicalTrace,
            trace);
        SubstitutionTokenOrigin[] bodyOrigins = this.CloneOriginsForUse(
            cached.BodyOrigins,
            canonicalTrace,
            trace);
        AsnToken[] tokens = cached.Tokens;
        AsnToken[] bodyTokens = cached.BodyTokens;
        ParsedParameterizedAssignmentSyntax? assignment = cached.AssignmentSyntax;
        ParameterInstantiationFixup[] fixups = cached.DeferredFixups;
        string failure = "";
        bool success = true;
        if (fixups.Length > 0 && trace.Declaration != null)
        {
            tokens = this.CloneTokensWithDeferredName(
                cached.Tokens,
                origins,
                fixups,
                trace.Declaration.Name);
            bodyTokens = this.CloneTokensWithDeferredName(
                cached.BodyTokens,
                bodyOrigins,
                fixups,
                trace.Declaration.Name);
            ParameterSubstitutionResult parsed =
                this.Substituter.ParseSubstitutedAssignment(
                    tokens,
                    origins,
                    target,
                    targetModule);
            assignment = parsed.AssignmentSyntax;
            failure = parsed.FailureMessage;
            success = parsed.Success;
            fixups = this.CloneDeferredFixupsForUse(
                cached.DeferredFixups,
                trace.Declaration.Name,
                tokens,
                cached.StableId);
        }

        return new ParameterInstantiationResult
        {
            Kind = cached.Kind,
            Tokens = tokens,
            Origins = origins,
            BodyTokens = bodyTokens,
            BodyOrigins = bodyOrigins,
            AssignmentSyntax = assignment,
            NestedReferences = cached.NestedReferences,
            DeferredFixups = fixups,
            Trace = trace,
            CacheKey = cached.CacheKey,
            StableId = cached.StableId,
            FailureMessage = failure,
            CacheHit = true,
            Deferred = cached.Deferred,
            Success = success
        };
    }

    internal AsnToken[] CloneTokensWithDeferredName(
        AsnToken[] source,
        SubstitutionTokenOrigin[] origins,
        ParameterInstantiationFixup[] fixups,
        string replacementName)
    {
        AsnToken[] result = new AsnToken[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            AsnToken token = this.Substituter.CopySourceToken(source[index]);
            if (this.IsDeferredFixupToken(fixups, source[index]))
            {
                token.Kind = AsnTokenKind.TypeReference;
                token.Text = replacementName;
            }

            result[index] = token;
            if (index < origins.Length)
            {
                origins[index].Token = token;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool IsDeferredFixupToken(
        ParameterInstantiationFixup[] fixups,
        AsnToken token)
    {
        int index = 0;
        while (index < fixups.Length)
        {
            if (fixups[index].Token == token)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal ParameterInstantiationFixup[] CloneDeferredFixupsForUse(
        ParameterInstantiationFixup[] source,
        string replacementName,
        AsnToken[] tokens,
        string stableId)
    {
        ParameterInstantiationFixup[] result =
            new ParameterInstantiationFixup[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = new ParameterInstantiationFixup
            {
                Reference = source[index].Reference,
                Target = source[index].Target,
                Token = this.FindDeferredReplacementToken(
                    tokens,
                    replacementName,
                    source[index].Span),
                Span = source[index].Span,
                ReplacementName = replacementName,
                TargetStableId = stableId,
                OptionalBoundary = source[index].OptionalBoundary,
                ChoiceBoundary = source[index].ChoiceBoundary
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnToken? FindDeferredReplacementToken(
        AsnToken[] tokens,
        string replacementName,
        AsnSourceSpan? span)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Text == replacementName
                && tokens[index].Kind == AsnTokenKind.TypeReference
                && span != null
                && tokens[index].Span != null
                && tokens[index].Span?.Start.SourceName == span.Start.SourceName
                && tokens[index].Span?.Start.Offset == span.Start.Offset)
            {
                return tokens[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal SubstitutionTokenOrigin[] CloneOriginsForUse(
        SubstitutionTokenOrigin[] origins,
        ParameterInstantiationTrace canonicalTrace,
        ParameterInstantiationTrace trace)
    {
        SubstitutionTokenOrigin[] result =
            new SubstitutionTokenOrigin[origins.Length];
        int index = 0;
        while (index < origins.Length)
        {
            result[index] = this.CloneOriginForUse(
                origins[index],
                canonicalTrace,
                trace);
            index = index + 1;
        }

        return result;
    }

    internal SubstitutionTokenOrigin CloneOriginForUse(
        SubstitutionTokenOrigin origin,
        ParameterInstantiationTrace canonicalTrace,
        ParameterInstantiationTrace trace)
    {
        SubstitutionTokenOrigin result = new SubstitutionTokenOrigin
        {
            Token = origin.Token,
            SourceToken = origin.SourceToken,
            Module = origin.Module,
            Declaration = origin.Declaration,
            FormalParameter = origin.FormalParameter,
            ActualParameter = origin.ActualParameter,
            Inserted = origin.Inserted
        };
        int inputIndex = this.FindTraceOriginIndex(
            canonicalTrace.InputOrigins,
            origin.SourceToken);
        if (inputIndex >= 0 && inputIndex < trace.InputOrigins.Length)
        {
            SubstitutionTokenOrigin current = trace.InputOrigins[inputIndex];
            result.SourceToken = current.SourceToken;
            result.Module = current.Module;
            result.Declaration = current.Declaration;
            result.FormalParameter = current.FormalParameter;
            result.ActualParameter = current.ActualParameter;
            result.Inserted = current.Inserted;
        }

        return result;
    }

    internal int FindTraceOriginIndex(
        SubstitutionTokenOrigin[] origins,
        AsnToken? token)
    {
        int index = 0;
        while (index < origins.Length)
        {
            if (origins[index].Token == token
                || origins[index].SourceToken == token)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal SymbolTable FindReferenceOriginModule(
        SubstitutionTokenOrigin[] origins,
        int index,
        SymbolTable fallback)
    {
        return index >= 0
            && index < origins.Length
            && origins[index].Module != null
            ? origins[index].Module
                ?? throw new System.InvalidOperationException()
            : fallback;
    }

    internal SymbolTableEntry CreateDetachedDeclaration(
        SymbolTableEntry source,
        AsnToken[] tokens)
    {
        return new SymbolTableEntry
        {
            Name = source.Name,
            QualifiedName = source.QualifiedName,
            Kind = source.Kind,
            KindClassified = source.KindClassified,
            DeclarationTokens = tokens,
            Parameterized = source.Parameterized,
            ParameterNames = source.ParameterNames,
            FormalParameters = source.FormalParameters,
            FormalParameterSyntax = source.FormalParameterSyntax
        };
    }

    internal InstantiationTokenSequence ReplaceNestedReference(
        ParameterSubstitutionResult source,
        int start,
        int end,
        ParameterInstantiationResult nested)
    {
        int removed = end - start + 1;
        int length = source.Tokens.Length - removed + nested.BodyTokens.Length;
        AsnToken[] tokens = new AsnToken[length];
        SubstitutionTokenOrigin[] origins = new SubstitutionTokenOrigin[length];
        int sourceIndex = 0;
        int resultIndex = 0;
        while (sourceIndex < start)
        {
            tokens[resultIndex] = source.Tokens[sourceIndex];
            origins[resultIndex] = source.Origins[sourceIndex];
            sourceIndex = sourceIndex + 1;
            resultIndex = resultIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < nested.BodyTokens.Length)
        {
            tokens[resultIndex] = nested.BodyTokens[nestedIndex];
            origins[resultIndex] = nested.BodyOrigins[nestedIndex];
            nestedIndex = nestedIndex + 1;
            resultIndex = resultIndex + 1;
        }

        sourceIndex = end + 1;
        while (sourceIndex < source.Tokens.Length)
        {
            tokens[resultIndex] = source.Tokens[sourceIndex];
            origins[resultIndex] = source.Origins[sourceIndex];
            sourceIndex = sourceIndex + 1;
            resultIndex = resultIndex + 1;
        }

        return new InstantiationTokenSequence
        {
            Tokens = tokens,
            Origins = origins
        };
    }

    internal InstantiationTokenSequence CreateInstantiationBody(
        ParameterSubstitutionResult source,
        AsnAssignmentKind kind)
    {
        int assignment = this.Substituter.FindAssignmentTokenIndex(source.Tokens);
        if (kind != AsnAssignmentKind.ValueSet)
        {
            return this.CopyInstantiationRange(
                source,
                assignment + 1,
                source.Tokens.Length);
        }

        int governorStart = this.FindInstantiationGovernorStart(source.Tokens);
        int rightStart = assignment + 1;
        int rightEnd = source.Tokens.Length;
        bool braces = rightEnd - rightStart >= 2
            && source.Tokens[rightStart].Kind == AsnTokenKind.LeftBrace
            && source.Tokens[rightEnd - 1].Kind == AsnTokenKind.RightBrace;
        int constraintStart = braces ? rightStart + 1 : rightStart;
        int constraintEnd = braces ? rightEnd - 1 : rightEnd;
        int length = assignment - governorStart
            + constraintEnd - constraintStart + 2;
        AsnToken[] tokens = new AsnToken[length];
        SubstitutionTokenOrigin[] origins = new SubstitutionTokenOrigin[length];
        int resultIndex = 0;
        int index = governorStart;
        while (index < assignment)
        {
            tokens[resultIndex] = source.Tokens[index];
            origins[resultIndex] = source.Origins[index];
            index = index + 1;
            resultIndex = resultIndex + 1;
        }

        AsnToken leftSource = source.Tokens[rightStart];
        tokens[resultIndex] = new AsnToken
        {
            Kind = AsnTokenKind.LeftParenthesis,
            Text = "(",
            Span = leftSource.Span
        };
        origins[resultIndex] = this.CopyOriginForGeneratedToken(
            source.Origins[rightStart],
            tokens[resultIndex]);
        resultIndex = resultIndex + 1;
        index = constraintStart;
        while (index < constraintEnd)
        {
            tokens[resultIndex] = source.Tokens[index];
            origins[resultIndex] = source.Origins[index];
            index = index + 1;
            resultIndex = resultIndex + 1;
        }

        AsnToken rightSource = source.Tokens[rightEnd - 1];
        tokens[resultIndex] = new AsnToken
        {
            Kind = AsnTokenKind.RightParenthesis,
            Text = ")",
            Span = rightSource.Span
        };
        origins[resultIndex] = this.CopyOriginForGeneratedToken(
            source.Origins[rightEnd - 1],
            tokens[resultIndex]);
        return new InstantiationTokenSequence
        {
            Tokens = tokens,
            Origins = origins
        };
    }

    internal int FindInstantiationGovernorStart(AsnToken[] tokens)
    {
        if (tokens.Length < 2 || tokens[1].Kind != AsnTokenKind.LeftBrace)
        {
            return 1;
        }

        int depth = 0;
        int index = 1;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind == AsnTokenKind.LeftBrace)
            {
                depth = depth + 1;
            }
            else if (tokens[index].Kind == AsnTokenKind.RightBrace)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    return index + 1;
                }
            }

            index = index + 1;
        }

        return tokens.Length;
    }

    internal InstantiationTokenSequence CopyInstantiationRange(
        ParameterSubstitutionResult source,
        int start,
        int end)
    {
        AsnToken[] tokens = new AsnToken[end - start];
        SubstitutionTokenOrigin[] origins =
            new SubstitutionTokenOrigin[end - start];
        int index = 0;
        while (index < tokens.Length)
        {
            tokens[index] = source.Tokens[start + index];
            origins[index] = source.Origins[start + index];
            index = index + 1;
        }

        return new InstantiationTokenSequence
        {
            Tokens = tokens,
            Origins = origins
        };
    }

    internal SubstitutionTokenOrigin CopyOriginForGeneratedToken(
        SubstitutionTokenOrigin source,
        AsnToken token)
    {
        return new SubstitutionTokenOrigin
        {
            Token = token,
            SourceToken = source.SourceToken,
            Module = source.Module,
            Declaration = source.Declaration,
            FormalParameter = source.FormalParameter,
            ActualParameter = source.ActualParameter,
            Inserted = source.Inserted
        };
    }
}
