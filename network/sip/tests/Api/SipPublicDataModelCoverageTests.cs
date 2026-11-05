namespace sip.Tests;

public class SipPublicDataModelCoverageTests
{
    [Test]
    public void EveryPublicDataFieldSupportsWriteReadAndNullRoundTrip()
    {
        Type[] models = typeof(SipMessage).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.IsClass
                && !type.IsAbstract && type.Namespace == "sip"
                && !typeof(Exception).IsAssignableFrom(type)
                && type.GetFields(System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.DeclaredOnly).Length > 0)
            .OrderBy(type => type.FullName).ToArray();
        List<string> fields = new List<string>();
        List<string> failures = new List<string>();
        int nullFields = 0;

        foreach (Type model in models)
        {
            object instance = Activator.CreateInstance(model)!;
            foreach (System.Reflection.FieldInfo field in model.GetFields(
                System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.DeclaredOnly)
                .OrderBy(value => value.Name))
            {
                string key = model.Name + "." + field.Name;
                fields.Add(key);
                object? expected = FieldValue(field.FieldType, key);
                field.SetValue(instance, expected);
                object? actual = field.GetValue(instance);
                if (!ValuesMatch(field.FieldType, expected, actual))
                {
                    failures.Add(key + " did not retain its assigned value.");
                }
                if (!field.FieldType.IsValueType)
                {
                    nullFields++;
                    field.SetValue(instance, null);
                    if (field.GetValue(instance) != null)
                    {
                        failures.Add(key + " did not retain null.");
                    }
                }
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(models, Has.Length.EqualTo(143));
            Assert.That(fields, Has.Count.EqualTo(1178));
            Assert.That(fields.Distinct().Count(), Is.EqualTo(fields.Count));
            Assert.That(nullFields, Is.GreaterThan(0));
            Assert.That(failures, Is.Empty);
        });
    }

    [Test]
    public void EveryEnumMemberAndStaticConstantHasAProtocolValue()
    {
        System.Reflection.Assembly assembly = typeof(SipMessage).Assembly;
        Type[] enums = assembly.GetTypes().Where(type =>
            type.IsPublic && type.IsEnum && type.Namespace == "sip")
            .OrderBy(type => type.FullName).ToArray();
        List<string> enumMembers = new List<string>();
        foreach (Type enumType in enums)
        {
            foreach (string name in Enum.GetNames(enumType))
            {
                object parsed = Enum.Parse(enumType, name);
                enumMembers.Add(enumType.Name + "." + name);
                Assert.That(Enum.GetName(enumType, parsed), Is.EqualTo(name));
                Assert.That(Convert.ToInt64(parsed),
                    Is.InRange((long)int.MinValue, (long)int.MaxValue));
            }
        }

        Type[] staticTypes = assembly.GetTypes().Where(type =>
            type.IsPublic && type.IsAbstract && type.IsSealed
                && type.Namespace == "sip"
                && type.GetFields(System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.DeclaredOnly).Length > 0)
            .OrderBy(type => type.FullName).ToArray();
        List<string> constants = new List<string>();
        foreach (Type staticType in staticTypes)
        {
            foreach (System.Reflection.FieldInfo field in staticType.GetFields(
                System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.DeclaredOnly))
            {
                constants.Add(staticType.Name + "." + field.Name);
                object? value = field.GetValue(null);
                Assert.That(value, Is.Not.Null, constants[^1]);
                if (value is string text)
                {
                    Assert.That(text, Is.Not.Empty, constants[^1]);
                }
            }
        }

        AssertProtocolMethodConstants();
        AssertProtocolStatusConstants();
        AssertProtocolHeaderConstants();
        AssertProtocolMediaConstants();
        AssertProtocolEventAndOptionConstants();

        Assert.Multiple(() =>
        {
            Assert.That(enums, Has.Length.EqualTo(36));
            Assert.That(enumMembers, Has.Count.EqualTo(232));
            Assert.That(staticTypes, Has.Length.EqualTo(11));
            Assert.That(constants, Has.Count.EqualTo(400));
            Assert.That(enumMembers.Distinct().Count(),
                Is.EqualTo(enumMembers.Count));
            Assert.That(constants.Distinct().Count(),
                Is.EqualTo(constants.Count));
        });
    }

    [Test]
    public void SupportedOperationsDetachOwnedDataAndRejectNullInputs()
    {
        SipMessage source = Parse(RequestText(
            SipMethods.Message, "ownership", 1, "ownership", "original"));
        SipTransportMessage transport = new SipTransportMessage();
        transport.Message = source;
        transport.Transport = SipTransportKind.Tcp;
        transport.CorrelationId = "owned-message";
        SipEndpoint endpoint = new SipEndpoint();
        endpoint.StartEndpoint(new SipStackOptions());
        Drain(endpoint);

        endpoint.ReceiveEndpointMessage(transport);
        source.Body[0] = (byte)'X';
        HeaderModel(source, SipHeaderNames.CallId).Value = "changed";
        transport.CorrelationId = "changed-correlation";
        SipStackEvent[] events = Drain(endpoint);
        SipStackEvent incoming = events.Single(value =>
            value.Kind == SipStackEventKind.IncomingRequest);
        SipStackEvent transaction = events.First(value =>
            value.Kind == SipStackEventKind.Transaction
            && value.Message != null);
        incoming.Message!.Body[0] = (byte)'Y';
        HeaderModel(incoming.Message, SipHeaderNames.CallId).Value =
            "changed-event";

        SipTransportMessage nullBytes = new SipTransportMessage();
        typeof(SipTransportMessage).GetField("Bytes")!
            .SetValue(nullBytes, null);
        nullBytes.CorrelationId = "null-bytes";
        endpoint.ReceiveEndpointMessage(nullBytes);
        SipStackEvent nullFailure = Drain(endpoint).Single();
        Stop(endpoint);

        SipCapabilitySet capabilities = new SipCapabilitySet();
        capabilities.Methods = new string[] { SipMethods.Options };
        SipCapabilityMessageResult advertised = new SipCapabilityNegotiator()
            .AddCapabilityAdvertisement(Parse(RequestText(
                SipMethods.Options, "capability-copy", 1,
                "capability-copy", "")), capabilities,
                new SipExtensionHeaderValue[0]);
        capabilities.Methods[0] = SipMethods.Bye;

        Assert.Multiple(() =>
        {
            Assert.That(System.Text.Encoding.UTF8.GetString(
                transaction.Message!.Body), Is.EqualTo("original"));
            Assert.That(Header(transaction.Message, SipHeaderNames.CallId),
                Is.EqualTo("ownership"));
            Assert.That(transaction.CorrelationId,
                Is.EqualTo("owned-message"));
            Assert.That(nullFailure.Kind, Is.EqualTo(SipStackEventKind.Error));
            Assert.That(nullFailure.CorrelationId, Is.Null);
            Assert.That(nullFailure.ErrorMessage,
                Does.Contain("valid received transport message"));
            Assert.That(advertised.Success, Is.True,
                advertised.ErrorMessage);
            Assert.That(Header(advertised.Message!, SipHeaderNames.Allow),
                Is.EqualTo(SipMethods.Options));
        });
    }

    static void AssertProtocolMethodConstants()
    {
        foreach (System.Reflection.FieldInfo field in typeof(SipMethods)
            .GetFields(System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static))
        {
            string method = (string)field.GetValue(null)!;
            SipMessage parsed = Parse(RequestText(
                method, "method-" + field.Name, 1,
                "method-" + field.Name, ""));
            Assert.That(parsed.MethodToken, Is.EqualTo(method));
        }
    }

    static void AssertProtocolStatusConstants()
    {
        foreach (System.Reflection.FieldInfo field in typeof(SipStatusCodes)
            .GetFields(System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static))
        {
            int status = (int)field.GetValue(null)!;
            SipMessage parsed = Parse("SIP/2.0 " + status + " Trace\r\n"
                + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-status\r\n"
                + "From: <sip:alice@example.com>;tag=from-tag\r\n"
                + "To: <sip:bob@example.com>;tag=to-tag\r\n"
                + "Call-ID: status-" + field.Name + "\r\n"
                + "CSeq: 1 OPTIONS\r\nContent-Length: 0\r\n\r\n");
            Assert.That(parsed.StatusCode, Is.EqualTo(status));
        }
    }

    static void AssertProtocolHeaderConstants()
    {
        SipMessage message = new SipMessage();
        message.Headers = typeof(SipHeaderNames).GetFields(
            System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static).Select(field =>
            {
                SipHeader header = new SipHeader();
                header.Name = (string)field.GetValue(null)!;
                header.Value = "trace";
                return header;
            }).ToArray();
        SipHeaderCodec codec = new SipHeaderCodec();
        foreach (SipHeader header in message.Headers)
        {
            Assert.That(codec.FindHeaders(message, header.Name!),
                Is.Not.Empty, header.Name);
        }
    }

    static void AssertProtocolMediaConstants()
    {
        foreach (System.Reflection.FieldInfo field in typeof(SipMediaTypes)
            .GetFields(System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static))
        {
            string mediaType = (string)field.GetValue(null)!;
            SipHeaderParseResult parsed = new SipHeaderCodec().ParseHeader(
                Bytes("Content-Type: " + mediaType + "\r\n"),
                new SipParseOptions());
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
                mediaType);
        }
    }

    static void AssertProtocolEventAndOptionConstants()
    {
        string[] packages = typeof(SipEventPackages).GetFields(
            System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static).Select(field =>
                    (string)field.GetValue(null)!).ToArray();
        SipEventPackageRegistration[] registrations =
            new SipEventPackageRegistry().GetEventPackageRegistrations();
        Assert.That(registrations.Select(value => value.EventPackage),
            Is.SupersetOf(packages));

        string[] tags = typeof(SipOptionTags).GetFields(
            System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static).Select(field =>
                    (string)field.GetValue(null)!).ToArray();
        SipCapabilitySet capabilities = new SipCapabilitySet();
        capabilities.OptionTags = tags;
        SipCapabilityMessageResult advertised = new SipCapabilityNegotiator()
            .AddCapabilityAdvertisement(Parse(RequestText(
                SipMethods.Options, "option-tags", 1, "option-tags", "")),
                capabilities, new SipExtensionHeaderValue[0]);
        Assert.That(advertised.Success, Is.True, advertised.ErrorMessage);
        Assert.That(Header(advertised.Message!, SipHeaderNames.Supported)
            .Split(',').Length, Is.EqualTo(tags.Length));
    }

    static object? FieldValue(Type type, string key)
    {
        if (type == typeof(string))
        {
            return new string(("trace-" + key).ToCharArray());
        }
        if (type == typeof(bool))
        {
            return true;
        }
        if (type == typeof(byte))
        {
            return (byte)17;
        }
        if (type == typeof(short))
        {
            return (short)19;
        }
        if (type == typeof(int))
        {
            return 23;
        }
        if (type == typeof(long))
        {
            return 29L;
        }
        if (type == typeof(uint))
        {
            return 31U;
        }
        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1);
        }
        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 1);
        }
        if (type.IsInterface)
        {
            return CreateCallbackProxy(type);
        }
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        System.Reflection.ConstructorInfo? constructor =
            type.GetConstructor(Type.EmptyTypes);
        return constructor == null ? null : constructor.Invoke(null);
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

    static bool ValuesMatch(Type type, object? expected, object? actual)
    {
        if (!type.IsValueType)
        {
            return object.ReferenceEquals(expected, actual);
        }
        return object.Equals(expected, actual);
    }

    static SipStackEvent[] Drain(SipEndpoint endpoint)
    {
        List<SipStackEvent> result = new List<SipStackEvent>();
        SipStackEvent value = endpoint.PollStackEvent();
        while (value.Kind != SipStackEventKind.None)
        {
            result.Add(value);
            value = endpoint.PollStackEvent();
        }
        return result.ToArray();
    }

    static void Stop(SipEndpoint endpoint)
    {
        SipCancellation cancellation = new SipCancellation();
        cancellation.Requested = true;
        endpoint.StopEndpoint(cancellation);
    }

    static SipHeader HeaderModel(SipMessage message, string name)
    {
        return message.Headers.First(value => string.Equals(
            value.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    static string Header(SipMessage message, string name)
    {
        return HeaderModel(message, name).Value!;
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
            + method + "\r\nContact: <sip:alice@client.example.com>\r\n"
            + (bytes.Length == 0 ? ""
                : "Content-Type: text/plain\r\n")
            + "Content-Length: " + bytes.Length + "\r\n\r\n" + body;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            Bytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }
}
