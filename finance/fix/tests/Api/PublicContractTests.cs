using System;
using System.Collections.Generic;
using System.Reflection;
using fix;

namespace fix.Tests;

[TestFixture]
public class PublicContractTests
{
    [Test]
    public void EveryPublicInterfaceMethodHasThePublishedSignature()
    {
        HashSet<Type> coveredInterfaces = [];

        Assert.Multiple(() =>
        {
            AssertMethod(coveredInterfaces, typeof(IFieldValueCodec), nameof(IFieldValueCodec.DecodeFieldValue), typeof(FieldParseResult), typeof(ReadOnlySpan<byte>), typeof(int), typeof(FixFieldType), typeof(DecodeOptions));
            AssertMethod(coveredInterfaces, typeof(IFieldValueCodec), nameof(IFieldValueCodec.EncodeFieldValue), typeof(EncodeResult), typeof(FixField), typeof(FixFieldType), typeof(Span<byte>), typeof(EncodeOptions));
            AssertMethod(coveredInterfaces, typeof(IMessageDecoder), nameof(IMessageDecoder.DecodeMessage), typeof(DecodeResult), typeof(ReadOnlySpan<byte>), typeof(FixDictionary), typeof(DecodeOptions));
            AssertMethod(coveredInterfaces, typeof(IMessageEncoder), nameof(IMessageEncoder.EncodeMessage), typeof(EncodeResult), typeof(FixMessage), typeof(FixDictionary), typeof(Span<byte>), typeof(EncodeOptions));
            AssertMethod(coveredInterfaces, typeof(IMessageFramer), nameof(IMessageFramer.ReadFrame), typeof(FrameResult), typeof(ReadOnlySpan<byte>), typeof(FrameOptions));
            AssertMethod(coveredInterfaces, typeof(IMessageOperations), nameof(IMessageOperations.CreateApplicationMessage), typeof(ApplicationMessageResult), typeof(FixDictionary), typeof(string), typeof(string));
            AssertMethod(coveredInterfaces, typeof(IMessageOperations), nameof(IMessageOperations.IdentifyApplicationMessage), typeof(ApplicationMessageResult), typeof(FixMessage), typeof(FixDictionary), typeof(string));
            AssertMethod(coveredInterfaces, typeof(IMessageOperations), nameof(IMessageOperations.SetField), typeof(MessageEditResult), typeof(FixMessage), typeof(FixField));
            AssertMethod(coveredInterfaces, typeof(IMessageOperations), nameof(IMessageOperations.RemoveField), typeof(MessageEditResult), typeof(FixMessage), typeof(int));
            AssertMethod(coveredInterfaces, typeof(IMessageOperations), nameof(IMessageOperations.AddGroup), typeof(MessageEditResult), typeof(FixMessage), typeof(FixGroup));
            AssertMethod(coveredInterfaces, typeof(IMessageOperations), nameof(IMessageOperations.RemoveGroup), typeof(MessageEditResult), typeof(FixMessage), typeof(int));
            AssertMethod(coveredInterfaces, typeof(IMessageOperations), nameof(IMessageOperations.CloneMessage), typeof(MessageEditResult), typeof(FixMessage));
            AssertMethod(coveredInterfaces, typeof(IMessageCrackHandler), nameof(IMessageCrackHandler.HandleAdministrativeMessage), typeof(ApplicationDecision), typeof(MessageCrackContext));
            AssertMethod(coveredInterfaces, typeof(IMessageCrackHandler), nameof(IMessageCrackHandler.HandleApplicationMessage), typeof(ApplicationDecision), typeof(MessageCrackContext));
            AssertMethod(coveredInterfaces, typeof(IMessageCracker), nameof(IMessageCracker.CrackMessage), typeof(MessageCrackResult), typeof(FixMessage), typeof(FixDictionary), typeof(FixDictionary), typeof(string), typeof(IMessageCrackHandler));
            AssertMethod(coveredInterfaces, typeof(IMessageCracker), nameof(IMessageCracker.LookupMessageName), typeof(MessageLookupResult), typeof(FixDictionary), typeof(string), typeof(string));
            AssertMethod(coveredInterfaces, typeof(IMessageCracker), nameof(IMessageCracker.LookupMessageCategory), typeof(MessageLookupResult), typeof(FixDictionary), typeof(string), typeof(string));
            AssertMethod(coveredInterfaces, typeof(IDictionaryLoader), nameof(IDictionaryLoader.LoadDictionary), typeof(DictionaryLoadResult), typeof(System.IO.Stream), typeof(FixDictionaryOptions));
            AssertMethod(coveredInterfaces, typeof(IDictionaryWriter), nameof(IDictionaryWriter.WriteDictionary), typeof(DictionaryLoadResult), typeof(System.IO.Stream), typeof(FixDictionary), typeof(FixDictionaryOptions));
            AssertMethod(coveredInterfaces, typeof(IDictionaryProvider), nameof(IDictionaryProvider.ResolveDictionary), typeof(DictionaryLoadResult), typeof(FixDictionaryIdentity));
            AssertMethod(coveredInterfaces, typeof(IDictionaryComposer), nameof(IDictionaryComposer.ComposeDictionary), typeof(DictionaryLoadResult), typeof(FixDictionary), typeof(FixDictionary), typeof(FixDictionaryOptions));
            AssertMethod(coveredInterfaces, typeof(IDictionaryComposer), nameof(IDictionaryComposer.DiffDictionaries), typeof(DictionaryDiff), typeof(FixDictionary), typeof(FixDictionary));
            AssertMethod(coveredInterfaces, typeof(IDictionaryQuery), nameof(IDictionaryQuery.ResolveFieldDefinition), typeof(FixFieldDefinition), typeof(FixDictionary), typeof(int));
            AssertMethod(coveredInterfaces, typeof(IDictionaryQuery), nameof(IDictionaryQuery.ResolveMessageDefinition), typeof(FixMessageDefinition), typeof(FixDictionary), typeof(string), typeof(string));
            AssertMethod(coveredInterfaces, typeof(IDictionaryQuery), nameof(IDictionaryQuery.ResolveComponentDefinition), typeof(FixComponentDefinition), typeof(FixDictionary), typeof(int));
            AssertMethod(coveredInterfaces, typeof(IDictionaryQuery), nameof(IDictionaryQuery.ResolveGroupDefinition), typeof(FixGroupDefinition), typeof(FixDictionary), typeof(int));
            AssertMethod(coveredInterfaces, typeof(IDictionaryQuery), nameof(IDictionaryQuery.ResolveCodeSetDefinition), typeof(FixCodeSetDefinition), typeof(FixDictionary), typeof(string));
            AssertMethod(coveredInterfaces, typeof(IDictionaryQuery), nameof(IDictionaryQuery.ResolveScenarioDefinition), typeof(FixScenarioDefinition), typeof(FixDictionary), typeof(string));
            AssertMethod(coveredInterfaces, typeof(IMessageValidator), nameof(IMessageValidator.ValidateMessage), typeof(ValidationResult), typeof(FixMessage), typeof(FixDictionary), typeof(ValidationOptions), typeof(ValidationContext));
            AssertMethod(coveredInterfaces, typeof(IRuleEvaluator), nameof(IRuleEvaluator.EvaluateRule), typeof(RuleEvaluationResult), typeof(FixMemberDefinition), typeof(FixMessage), typeof(ValidationContext));
            AssertMethod(coveredInterfaces, typeof(IMessageStore), nameof(IMessageStore.StoreMessage), typeof(StoreRangeResult), typeof(StoredMessage));
            AssertMethod(coveredInterfaces, typeof(IMessageStore), nameof(IMessageStore.ReadMessageRange), typeof(StoreRangeResult), typeof(int), typeof(int));
            AssertMethod(coveredInterfaces, typeof(IMessageStore), nameof(IMessageStore.SaveSequenceState), typeof(StoreRangeResult), typeof(int), typeof(int));
            AssertMethod(coveredInterfaces, typeof(IMessageStore), nameof(IMessageStore.RefreshStore), typeof(StoreRangeResult));
            AssertMethod(coveredInterfaces, typeof(IMessageStore), nameof(IMessageStore.ResetStore), typeof(StoreRangeResult));
            AssertMethod(coveredInterfaces, typeof(IFixLog), nameof(IFixLog.WriteLog), typeof(void), typeof(LogEntry));
            AssertMethod(coveredInterfaces, typeof(ISessionClock), nameof(ISessionClock.GetUtcNow), typeof(DateTime));
            AssertMethod(coveredInterfaces, typeof(ISessionTransport), nameof(ISessionTransport.OpenTransport), typeof(TransportWriteResult), typeof(SessionEndpoint), typeof(SessionTlsSettings));
            AssertMethod(coveredInterfaces, typeof(ISessionTransport), nameof(ISessionTransport.ReadTransport), typeof(TransportReadResult), typeof(Span<byte>));
            AssertMethod(coveredInterfaces, typeof(ISessionTransport), nameof(ISessionTransport.WriteTransport), typeof(TransportWriteResult), typeof(ReadOnlySpan<byte>));
            AssertMethod(coveredInterfaces, typeof(ISessionTransport), nameof(ISessionTransport.CloseTransport), typeof(TransportWriteResult));
            AssertMethod(coveredInterfaces, typeof(ISessionAuthenticator), nameof(ISessionAuthenticator.AuthenticateSession), typeof(AuthenticationResult), typeof(AuthenticationRequest));
            AssertMethod(coveredInterfaces, typeof(ISessionApplication), nameof(ISessionApplication.OnSessionCreated), typeof(ApplicationDecision), typeof(SessionId));
            AssertMethod(coveredInterfaces, typeof(ISessionApplication), nameof(ISessionApplication.OnSessionLogon), typeof(ApplicationDecision), typeof(ApplicationMessageContext));
            AssertMethod(coveredInterfaces, typeof(ISessionApplication), nameof(ISessionApplication.OnSessionLogout), typeof(ApplicationDecision), typeof(ApplicationMessageContext));
            AssertMethod(coveredInterfaces, typeof(ISessionApplication), nameof(ISessionApplication.OnAdministrativeMessage), typeof(ApplicationDecision), typeof(ApplicationMessageContext));
            AssertMethod(coveredInterfaces, typeof(ISessionApplication), nameof(ISessionApplication.OnApplicationMessage), typeof(ApplicationDecision), typeof(ApplicationMessageContext));
            AssertMethod(coveredInterfaces, typeof(ISessionApplication), nameof(ISessionApplication.OnReplayMessage), typeof(ApplicationDecision), typeof(ApplicationMessageContext));
            AssertMethod(coveredInterfaces, typeof(IMessageThrottle), nameof(IMessageThrottle.CheckMessageThrottle), typeof(ApplicationDecision), typeof(ApplicationMessageContext), typeof(SessionThrottleSettings));
            AssertMethod(coveredInterfaces, typeof(IFixSession), nameof(IFixSession.StartSession), typeof(SessionPollResult));
            AssertMethod(coveredInterfaces, typeof(IFixSession), nameof(IFixSession.PollSession), typeof(SessionPollResult));
            AssertMethod(coveredInterfaces, typeof(IFixSession), nameof(IFixSession.SendMessage), typeof(SessionSendResult), typeof(FixMessage));
            AssertMethod(coveredInterfaces, typeof(IFixSession), nameof(IFixSession.LogoutSession), typeof(SessionPollResult), typeof(string));
            AssertMethod(coveredInterfaces, typeof(IFixSession), nameof(IFixSession.StopSession), typeof(SessionPollResult));
            AssertMethod(coveredInterfaces, typeof(IFixSession), nameof(IFixSession.GetSessionSnapshot), typeof(SessionSnapshot));
            AssertMethod(coveredInterfaces, typeof(ISessionEngine), nameof(ISessionEngine.AddSession), typeof(SessionPollResult), typeof(SessionSettings));
            AssertMethod(coveredInterfaces, typeof(ISessionEngine), nameof(ISessionEngine.RemoveSession), typeof(SessionPollResult), typeof(SessionId));
            AssertMethod(coveredInterfaces, typeof(ISessionEngine), nameof(ISessionEngine.PollSessions), typeof(SessionPollResult));
            AssertMethod(coveredInterfaces, typeof(ISessionEngine), nameof(ISessionEngine.ResolveSessionSnapshot), typeof(SessionSnapshot), typeof(SessionId));
            AssertMethod(coveredInterfaces, typeof(ISessionSettingsLoader), nameof(ISessionSettingsLoader.LoadSessionSettings), typeof(SessionSettings[]), typeof(System.IO.Stream));
            AssertMethod(coveredInterfaces, typeof(IOrchestraReader), nameof(IOrchestraReader.ReadRepository), typeof(OrchestraReadResult), typeof(System.IO.Stream), typeof(OrchestraReadOptions));
            AssertMethod(coveredInterfaces, typeof(IOrchestraWriter), nameof(IOrchestraWriter.WriteRepository), typeof(OrchestraReadResult), typeof(System.IO.Stream), typeof(OrchestraRepository), typeof(OrchestraWriteOptions));
            AssertMethod(coveredInterfaces, typeof(IOrchestraValidator), nameof(IOrchestraValidator.ValidateRepository), typeof(OrchestraReadResult), typeof(OrchestraRepository));
            AssertMethod(coveredInterfaces, typeof(IOrchestraMapper), nameof(IOrchestraMapper.MapRepositoryToDictionary), typeof(DictionaryLoadResult), typeof(OrchestraRepository), typeof(FixDictionaryOptions));
            AssertMethod(coveredInterfaces, typeof(IOrchestraMapper), nameof(IOrchestraMapper.MapDictionaryToRepository), typeof(OrchestraReadResult), typeof(FixDictionary), typeof(OrchestraVersion));
            AssertMethod(coveredInterfaces, typeof(IOrchestraDiffer), nameof(IOrchestraDiffer.DiffRepositories), typeof(OrchestraDiff), typeof(OrchestraRepository), typeof(OrchestraRepository));
            AssertMethod(coveredInterfaces, typeof(IOrchestraSlicer), nameof(IOrchestraSlicer.SliceRepository), typeof(OrchestraReadResult), typeof(OrchestraRepository), typeof(string[]), typeof(string[]));
            AssertMethod(coveredInterfaces, typeof(IModelGenerator), nameof(IModelGenerator.GenerateModels), typeof(GenerationResult), typeof(FixDictionary), typeof(ModelGenerationOptions));
            AssertMethod(coveredInterfaces, typeof(ISbeSchemaGenerator), nameof(ISbeSchemaGenerator.GenerateSbeSchema), typeof(GenerationResult), typeof(FixDictionary), typeof(SbeGenerationOptions));
            AssertMethod(coveredInterfaces, typeof(IBinaryMessageCodec), nameof(IBinaryMessageCodec.DecodeBinary), typeof(BinaryDecodeResult), typeof(ReadOnlySpan<byte>), typeof(BinaryMessageContext));
            AssertMethod(coveredInterfaces, typeof(IBinaryMessageCodec), nameof(IBinaryMessageCodec.EncodeBinary), typeof(BinaryEncodeResult), typeof(FixMessage), typeof(BinaryMessageContext), typeof(Span<byte>));
            AssertMethod(coveredInterfaces, typeof(IFixApiCapabilitiesProvider), nameof(IFixApiCapabilitiesProvider.GetFixApiCapabilities), typeof(FixApiCapabilities));
        });

        Type[] exportedTypes = typeof(FixField).Assembly.GetExportedTypes();
        List<Type> interfaceTypes = [];
        for (int typeIndex = 0; typeIndex < exportedTypes.Length; typeIndex++)
        {
            if (exportedTypes[typeIndex].IsInterface)
            {
                interfaceTypes.Add(exportedTypes[typeIndex]);
            }
        }

        Assert.That(interfaceTypes, Has.Count.EqualTo(34));
        Assert.That(coveredInterfaces, Has.Count.EqualTo(interfaceTypes.Count));
        Assert.Multiple(() =>
        {
            for (int typeIndex = 0; typeIndex < interfaceTypes.Count; typeIndex++)
            {
                Type interfaceType = interfaceTypes[typeIndex];
                Assert.That(coveredInterfaces.Contains(interfaceType), Is.True, interfaceType.FullName);
                Assert.That(interfaceType.GetInterfaces(), Is.Empty, interfaceType.FullName);
                MethodInfo[] methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                HashSet<string> methodNames = [];
                for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                {
                    Assert.That(methodNames.Add(methods[methodIndex].Name), Is.True, interfaceType.FullName + "." + methods[methodIndex].Name);
                }
            }
        });
    }

    private static void AssertMethod(HashSet<Type> coveredInterfaces, Type interfaceType, string methodName, Type returnType, params Type[] parameterTypes)
    {
        coveredInterfaces.Add(interfaceType);
        MethodInfo? method = interfaceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.That(method, Is.Not.Null, interfaceType.FullName + "." + methodName);
        if (method == null)
        {
            return;
        }

        Assert.That(method.ReturnType, Is.EqualTo(returnType), interfaceType.FullName + "." + methodName + " return type");
        ParameterInfo[] parameters = method.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(parameterTypes.Length), interfaceType.FullName + "." + methodName + " parameter count");
        int comparedCount = parameters.Length;
        if (parameterTypes.Length < comparedCount)
        {
            comparedCount = parameterTypes.Length;
        }
        for (int index = 0; index < comparedCount; index++)
        {
            Assert.That(parameters[index].ParameterType, Is.EqualTo(parameterTypes[index]), interfaceType.FullName + "." + methodName + " parameter " + index);
        }
    }
}
