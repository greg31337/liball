namespace sip.Tests;

public class SipPublicImplementationCoverageTests
{
    [Test]
    public void EveryPublicImplementationConstructsAndExercisesItsContracts()
    {
        Type[] implementations = PublicImplementations();
        List<string> constructed = new List<string>();
        List<string> invoked = new List<string>();
        List<string> unsafeFailures = new List<string>();
        List<string> categoryFailures = new List<string>();
        int returned = 0;
        int rejected = 0;

        foreach (Type implementation in implementations)
        {
            Construct(implementation);
            constructed.Add(implementation.Name);
            foreach (Type contract in implementation.GetInterfaces()
                .OrderBy(value => value.FullName))
            {
                foreach (System.Reflection.MethodInfo method in contract
                    .GetMethods().OrderBy(value => value.Name))
                {
                    object instance = Construct(implementation);
                    object?[] arguments = method.GetParameters().Select(
                        parameter => InvocationValue(parameter.ParameterType))
                        .ToArray();
                    string key = implementation.Name + "." + contract.Name
                        + "." + method.Name;
                    invoked.Add(key);
                    try
                    {
                        method.Invoke(instance, arguments);
                        returned++;
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
                        if (IsSipException(cause)
                            && !ExceptionMatchesImplementation(
                                implementation, cause))
                        {
                            categoryFailures.Add(key + ": unexpected "
                                + cause.GetType().Name);
                        }
                    }
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(implementations, Has.Length.EqualTo(43));
            Assert.That(constructed, Has.Count.EqualTo(43));
            Assert.That(invoked, Has.Count.EqualTo(270));
            Assert.That(constructed.Distinct().Count(),
                Is.EqualTo(constructed.Count));
            Assert.That(invoked.Distinct().Count(), Is.EqualTo(invoked.Count));
            Assert.That(returned, Is.GreaterThan(0));
            Assert.That(rejected, Is.GreaterThan(0));
            Assert.That(unsafeFailures, Is.Empty);
            Assert.That(categoryFailures, Is.Empty);
        });
    }

    [Test]
    public void EveryDocumentedExceptionHasOneDistinctFailureCategory()
    {
        Type[] exceptionTypes = typeof(SipProtocolException).Assembly
            .GetTypes().Where(type => type.IsPublic && type.IsClass
                && type.BaseType == typeof(Exception)
                && type.Namespace == "sip")
            .OrderBy(type => type.FullName).ToArray();
        List<string> categories = new List<string>();

        foreach (Type exceptionType in exceptionTypes)
        {
            string category = ExceptionCategory(exceptionType);
            System.Reflection.ConstructorInfo constructor = exceptionType
                .GetConstructors().Single();
            object?[] arguments = constructor.GetParameters().Select(
                parameter => ExceptionArgument(parameter.ParameterType,
                    parameter.Name, category)).ToArray();
            Exception instance = (Exception)constructor.Invoke(arguments);
            categories.Add(category);

            Assert.Multiple(() =>
            {
                Assert.That(category, Is.Not.EqualTo("unknown"),
                    exceptionType.Name);
                Assert.That(instance.GetType(), Is.EqualTo(exceptionType));
                Assert.That(instance.Message,
                    Is.EqualTo(category + " failure"));
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(exceptionTypes, Has.Length.EqualTo(9));
            Assert.That(categories, Has.Count.EqualTo(9));
            Assert.That(categories.Distinct().Count(), Is.EqualTo(9));
        });
    }

    [Test]
    public void OperationalExceptionsRemainBoundToTheirFailureDomains()
    {
        SipProtocolException protocol = Assert.Throws<SipProtocolException>(
            () => new SipMessageCodec().MeasureMessage(
                new SipMessage(), new SipWriteOptions()))!;
        SipTransportException transport = Assert.Throws<SipTransportException>(
            () => new UdpSipTransport().ConnectTransport(
                new SipEndpointAddress()))!;
        SipTransportException webSocketNullOptions =
            Assert.Throws<SipTransportException>(() =>
                new WebSocketSipTransport().BindTransport(
                    null!, new SipTlsOptions(),
                    new SipWebSocketOptions()))!;
        SipTransactionException transaction =
            Assert.Throws<SipTransactionException>(() =>
                new SipTransactionLayer().GetTransactionSnapshot(
                    "missing-transaction"))!;
        SipDialogId dialogId = new SipDialogId();
        dialogId.CallId = "missing-dialog";
        dialogId.LocalTag = "local";
        dialogId.RemoteTag = "remote";
        SipDialogException dialog = Assert.Throws<SipDialogException>(() =>
            new SipDialogLayer().FindDialog(dialogId))!;

        Assert.Multiple(() =>
        {
            Assert.That(ExceptionCategory(protocol.GetType()),
                Is.EqualTo("protocol"));
            Assert.That(protocol.StatusCode, Is.EqualTo(0));
            Assert.That(ExceptionCategory(transport.GetType()),
                Is.EqualTo("transport"));
            Assert.That(transport.Transport, Is.EqualTo(SipTransportKind.Udp));
            Assert.That(webSocketNullOptions.Transport,
                Is.EqualTo(SipTransportKind.WebSocket));
            Assert.That(webSocketNullOptions.Message,
                Does.Contain("bind options"));
            Assert.That(ExceptionCategory(transaction.GetType()),
                Is.EqualTo("transaction"));
            Assert.That(transaction.TransactionId,
                Is.EqualTo("missing-transaction"));
            Assert.That(ExceptionCategory(dialog.GetType()),
                Is.EqualTo("dialog"));
            Assert.That(dialog.DialogId, Is.SameAs(dialogId));
        });
    }

    static Type[] PublicImplementations()
    {
        return typeof(ISipEndpoint).Assembly.GetTypes().Where(type =>
            type.IsPublic && type.IsClass && !type.IsAbstract
                && type.Namespace == "sip"
                && !typeof(Exception).IsAssignableFrom(type)
                && type.GetInterfaces().Length > 0)
            .OrderBy(type => type.FullName).ToArray();
    }

    static object Construct(Type implementation)
    {
        System.Reflection.ConstructorInfo constructor = implementation
            .GetConstructors().Single();
        object?[] arguments = constructor.GetParameters().Select(
            parameter => ConstructorValue(parameter.ParameterType)).ToArray();
        return constructor.Invoke(arguments);
    }

    static object? ConstructorValue(Type type)
    {
        if (type.IsInterface)
        {
            return CreateCallbackProxy(type);
        }
        return InvocationValue(type);
    }

    static object? InvocationValue(Type type)
    {
        if (!type.IsValueType)
        {
            return null;
        }
        return Activator.CreateInstance(type);
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

    static bool IsSipException(Exception error)
    {
        return error.GetType().Namespace == "sip"
            && error.GetType().Name.StartsWith("Sip", StringComparison.Ordinal)
            && error.GetType().Name.EndsWith(
                "Exception", StringComparison.Ordinal);
    }

    static bool ExceptionMatchesImplementation(
        Type implementation, Exception error)
    {
        Type errorType = error.GetType();
        if (typeof(ISipTransport).IsAssignableFrom(implementation)
            || typeof(ISipTransportManager).IsAssignableFrom(implementation))
        {
            return errorType == typeof(SipTransportException);
        }
        if (typeof(ISipTransactionLayer).IsAssignableFrom(implementation))
        {
            return errorType == typeof(SipTransactionException);
        }
        if (typeof(ISipDialogLayer).IsAssignableFrom(implementation))
        {
            return errorType == typeof(SipDialogException)
                || errorType == typeof(SipProtocolException);
        }
        if (typeof(ISipMessageCodec).IsAssignableFrom(implementation)
            || typeof(ISipInviteSession).IsAssignableFrom(implementation))
        {
            return errorType == typeof(SipProtocolException);
        }
        if (typeof(ISipRegistrationClient).IsAssignableFrom(implementation))
        {
            return errorType == typeof(SipRegistrationException);
        }
        return false;
    }

    static string ExceptionCategory(Type exceptionType)
    {
        if (exceptionType == typeof(SipAuthenticationException))
        {
            return "authentication";
        }
        if (exceptionType == typeof(SipDialogException))
        {
            return "dialog";
        }
        if (exceptionType == typeof(SipParseException))
        {
            return "parse";
        }
        if (exceptionType == typeof(SipPolicyException))
        {
            return "policy";
        }
        if (exceptionType == typeof(SipProtocolException))
        {
            return "protocol";
        }
        if (exceptionType == typeof(SipRegistrationException))
        {
            return "registration";
        }
        if (exceptionType == typeof(SipResolutionException))
        {
            return "resolution";
        }
        if (exceptionType == typeof(SipTransactionException))
        {
            return "transaction";
        }
        if (exceptionType == typeof(SipTransportException))
        {
            return "transport";
        }
        return "unknown";
    }

    static object? ExceptionArgument(
        Type type, string? name, string category)
    {
        if (name == "message")
        {
            return category + " failure";
        }
        if (type == typeof(string))
        {
            return category + "-context";
        }
        if (type == typeof(int))
        {
            return 400;
        }
        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1);
        }
        if (type == typeof(SipDialogId))
        {
            SipDialogId id = new SipDialogId();
            id.CallId = category;
            id.LocalTag = "local";
            id.RemoteTag = "remote";
            return id;
        }
        if (type == typeof(SipUri))
        {
            SipUri uri = new SipUri();
            uri.Scheme = SipUriScheme.Sip;
            uri.Host = category + ".example.com";
            return uri;
        }
        return null;
    }
}
