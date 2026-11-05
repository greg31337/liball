namespace sip.Tests;

public class SipPublicApiTraceabilityTests
{
    [Test]
    public void EveryContractMethodHasARuntimeInvocationPath()
    {
        Type[] contracts = typeof(ISipEndpoint).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.IsInterface
                && type.Namespace == "sip"
                && type.Name.StartsWith("ISip", StringComparison.Ordinal))
            .OrderBy(type => type.FullName).ToArray();
        List<string> invoked = new List<string>();
        List<string> unsafeFailures = new List<string>();
        int returned = 0;
        int rejected = 0;

        foreach (Type contract in contracts)
        {
            foreach (System.Reflection.MethodInfo method in
                contract.GetMethods().OrderBy(value => value.Name))
            {
                object instance = CreateContractInstance(contract);
                object?[] arguments = method.GetParameters().Select(
                    parameter => InvocationValue(parameter.ParameterType))
                    .ToArray();
                string key = contract.Name + "." + method.Name;
                invoked.Add(key);
                try
                {
                    object? value = method.Invoke(instance, arguments);
                    returned++;
                    ObserveReturnedValue(value);
                }
                catch (System.Reflection.TargetInvocationException error)
                {
                    rejected++;
                    Exception cause = error.InnerException == null
                        ? error : error.InnerException;
                    if (cause is NullReferenceException
                        || cause is IndexOutOfRangeException
                        || cause is AccessViolationException)
                    {
                        unsafeFailures.Add(key + ": "
                            + cause.GetType().Name + " " + cause.Message);
                    }
                }
            }
        }

        int declared = contracts.Sum(contract =>
            contract.GetMethods().Length);
        Assert.Multiple(() =>
        {
            Assert.That(contracts, Has.Length.EqualTo(55));
            Assert.That(declared, Is.EqualTo(278));
            Assert.That(invoked, Has.Count.EqualTo(declared));
            Assert.That(invoked.Distinct().Count(), Is.EqualTo(declared));
            Assert.That(returned, Is.GreaterThan(0));
            Assert.That(rejected, Is.GreaterThan(0));
            Assert.That(unsafeFailures, Is.Empty);
        });
    }

    [Test]
    public void ContractOutcomesExposeSuccessFailureCancellationTimeoutAndLimits()
    {
        ISipMessageCodec codec = new SipMessageCodec();
        byte[] validBytes = Bytes(RequestText("OPTIONS", "trace-success", 1,
            "trace-success", ""));
        SipParseResult success = codec.ParseDatagramMessage(
            validBytes, new SipParseOptions());
        SipParseResult incomplete = codec.ParseStreamMessage(
            validBytes.Take(validBytes.Length - 2).ToArray(),
            new SipParseOptions());
        SipParseOptions limitedOptions = new SipParseOptions();
        limitedOptions.MaxBodyBytes = 2;
        SipParseResult limited = codec.ParseDatagramMessage(
            Bytes(RequestText("MESSAGE", "trace-limit", 2,
                "trace-limit", "body")), limitedOptions);

        ISipOptionsService options = new SipOptionsService();
        SipCancellation cancelled = new SipCancellation();
        cancelled.Requested = true;
        SipOptionsResult cancellation = options.QueryOptions(
            Uri("sip:service@example.com"), cancelled);

        ISipEndpoint endpoint = new SipEndpoint();
        SipSendRequest send = new SipSendRequest();
        send.Message = success.Message;
        send.CorrelationId = "invalid-state";
        SipSendResult invalidState = endpoint.SendEndpointMessage(send);
        SipStackEvent endpointFailure = endpoint.PollStackEvent();

        SipTransactionOptions transactionOptions =
            new SipTransactionOptions();
        transactionOptions.Timers.T1Milliseconds = 10;
        transactionOptions.Timers.T2Milliseconds = 40;
        transactionOptions.Timers.T4Milliseconds = 50;
        SipTransactionLayer transactions = new SipTransactionLayer();
        transactions.ConfigureTransactionSources(
            new SipManualMonotonicClock(0),
            new SipHarnessRandomSource(75), transactionOptions);
        transactions.CreateTransaction(success.Message!, transactionOptions);
        DrainTransactionEvents(transactions);
        transactions.AdvanceTransactionTime(640);
        SipTransactionEvent[] timeoutEvents =
            DrainTransactionEvents(transactions);

        ISipRegistrationClient registrations = new SipRegistrationClient();
        SipRegistrationSnapshot registering = registrations.RegisterContact(
            RegistrationRequest());
        SipRegistrationEvent registrationEvent =
            registrations.PollRegistrationEvent();
        SipRegistrationSnapshot queuedRegistration =
            registrations.PollRegistrationSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(success.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(incomplete.Status,
                Is.EqualTo(SipParseStatus.Incomplete));
            Assert.That(limited.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(limited.ErrorMessage, Does.Contain("body"));
            Assert.That(cancellation.Success, Is.False);
            Assert.That(cancellation.ErrorMessage, Does.Contain("cancelled"));
            Assert.That(invalidState.Success, Is.False);
            Assert.That(invalidState.ErrorMessage,
                Does.Contain("not running"));
            Assert.That(endpointFailure.Kind,
                Is.EqualTo(SipStackEventKind.Error));
            Assert.That(timeoutEvents.Any(value => value.Kind
                == SipTransactionEventKind.TimerFired), Is.True);
            Assert.That(timeoutEvents.Any(value => value.Kind
                == SipTransactionEventKind.Terminated), Is.True);
            Assert.That(registering.State,
                Is.EqualTo(SipRegistrationState.Registering));
            Assert.That(registrationEvent.State,
                Is.EqualTo(SipRegistrationState.Registering));
            Assert.That(queuedRegistration.State,
                Is.EqualTo(SipRegistrationState.Registering));
        });
    }

    static object CreateContractInstance(Type contract)
    {
        Type? implementation = typeof(ISipEndpoint).Assembly.GetTypes()
            .Where(type => type.IsPublic && !type.IsAbstract
                && type.IsClass && contract.IsAssignableFrom(type))
            .OrderBy(type => type == typeof(UdpSipTransport) ? 0 : 1)
            .ThenBy(type => type.FullName).FirstOrDefault();
        if (implementation == null)
        {
            return CreateCallbackProxy(contract);
        }
        System.Reflection.ConstructorInfo constructor = implementation
            .GetConstructors().OrderBy(value => value.GetParameters().Length)
            .First();
        object?[] arguments = constructor.GetParameters().Select(
            parameter => ConstructorValue(parameter.ParameterType)).ToArray();
        return constructor.Invoke(arguments);
    }

    static object CreateCallbackProxy(Type contract)
    {
        System.Reflection.MethodInfo create = typeof(System.Reflection.DispatchProxy)
            .GetMethods(System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static)
            .Single(method => method.Name == "Create"
                && method.IsGenericMethodDefinition
                && method.GetGenericArguments().Length == 2
                && method.GetParameters().Length == 0);
        return create.MakeGenericMethod(
            contract, typeof(PublicApiCallbackProxy)).Invoke(null, null)!;
    }

    static object? ConstructorValue(Type type)
    {
        if (type.IsInterface)
        {
            return CreateCallbackProxy(type);
        }
        return InvocationValue(type);
    }

    internal static object? InvocationValue(Type type)
    {
        if (!type.IsValueType)
        {
            return null;
        }
        return Activator.CreateInstance(type);
    }

    static void ObserveReturnedValue(object? value)
    {
        if (value == null)
        {
            return;
        }
        Type type = value.GetType();
        foreach (System.Reflection.FieldInfo field in type.GetFields(
            System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance))
        {
            if (field.FieldType.IsEnum)
            {
                field.GetValue(value);
            }
        }
    }

    static SipTransactionEvent[] DrainTransactionEvents(
        SipTransactionLayer layer)
    {
        List<SipTransactionEvent> result = new List<SipTransactionEvent>();
        SipTransactionEvent value = layer.PollTransactionEvent();
        while (value.Kind != SipTransactionEventKind.None)
        {
            result.Add(value);
            value = layer.PollTransactionEvent();
        }
        return result.ToArray();
    }

    static SipRegistrationRequest RegistrationRequest()
    {
        SipRegistrationRequest request = new SipRegistrationRequest();
        request.AddressOfRecord = Uri("sip:alice@example.com");
        request.Target = Uri("sip:example.com");
        SipContactValue contact = new SipContactValue();
        SipNameAddress address = new SipNameAddress();
        address.Uri = Uri("sip:alice@device.example.com");
        contact.Address = address;
        request.Contacts = new SipContactValue[] { contact };
        return request;
    }

    static string RequestText(string method, string callId, int cseq,
        string branch, string body)
    {
        byte[] bytes = Bytes(body);
        return method + " sip:service@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/TCP client.example.com;branch=z9hG4bK-"
            + branch + "\r\nMax-Forwards: 70\r\n"
            + "From: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: Service <sip:service@example.com>\r\n"
            + "Call-ID: " + callId + "\r\nCSeq: " + cseq + " "
            + method + "\r\n"
            + (bytes.Length == 0 ? ""
                : "Content-Type: text/plain\r\n")
            + "Content-Length: " + bytes.Length + "\r\n\r\n" + body;
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            Bytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }
}

public class PublicApiCallbackProxy : System.Reflection.DispatchProxy
{
    protected override object? Invoke(
        System.Reflection.MethodInfo? targetMethod, object?[]? arguments)
    {
        if (targetMethod == null
            || targetMethod.ReturnType == typeof(void))
        {
            return null;
        }
        return SipPublicApiTraceabilityTests.InvocationValue(
            targetMethod.ReturnType);
    }
}
