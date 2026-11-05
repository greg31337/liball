using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class RegisteredJmapMethod
{
    internal string? Name;
    internal List<string> RequiredCapabilities = new List<string>();
    internal IJmapVendorMethodHandler? VendorHandler;
}

internal class MethodRegistry
{
    internal ValueValidator Values = new ValueValidator();
    internal Dictionary<string, RegisteredJmapMethod> Methods =
        new Dictionary<string, RegisteredJmapMethod>(StringComparer.Ordinal);
    internal List<string> Capabilities = new List<string>();
    internal HashSet<string> CapabilitySet = new HashSet<string>(StringComparer.Ordinal);

    internal MethodRegistry(JmapServerServices services)
    {
        if (services == null)
        {
            throw new ArgumentNullException("services");
        }

        AddCapability(JmapCapabilities.Core);
        AddCapability(JmapCapabilities.Mail);
        AddCapability(JmapCapabilities.Submission);
        AddCapability(JmapCapabilities.VacationResponse);
        AddStandardMethods();
        AddVendorRegistrations(services);
    }

    internal RegisteredJmapMethod? FindMethod(string? methodName)
    {
        if (methodName == null || !Methods.ContainsKey(methodName))
        {
            return null;
        }

        return Methods[methodName];
    }

    internal bool HasCapability(string? capability)
    {
        return capability != null && CapabilitySet.Contains(capability);
    }

    internal List<string> GetSupportedCapabilities()
    {
        return new List<string>(Capabilities);
    }

    internal Dictionary<string, List<string>> GetMethodCapabilities()
    {
        Dictionary<string, List<string>> result =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, RegisteredJmapMethod> item in Methods)
        {
            result.Add(item.Key, new List<string>(item.Value.RequiredCapabilities));
        }

        return result;
    }

    internal Task<JmapInvocation> ProcessVendorMethodAsync(
        JmapInvocation invocation,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        if (invocation == null || invocation.Name == null)
        {
            throw ValidationFailure("A named vendor method invocation is required.", "methodCall");
        }

        RegisteredJmapMethod? method = FindMethod(invocation.Name);
        if (method == null || method.VendorHandler == null)
        {
            throw ValidationFailure("The invocation is not registered to a vendor handler.",
                "methodCall/name");
        }

        if (context == null)
        {
            throw ValidationFailure("A request context is required.", "context");
        }

        return method.VendorHandler.ProcessVendorMethodAsync(
            invocation, context, cancellationToken);
    }

    internal void AddStandardMethods()
    {
        AddStandardMethod(JmapMethods.CoreEcho, JmapCapabilities.Core);
        AddStandardMethod(JmapMethods.BlobCopy, JmapCapabilities.Core);
        AddStandardMethod(JmapMethods.PushSubscriptionGet, JmapCapabilities.Core);
        AddStandardMethod(JmapMethods.PushSubscriptionSet, JmapCapabilities.Core);

        AddStandardMethod(JmapMethods.MailboxGet, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.MailboxChanges, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.MailboxQuery, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.MailboxQueryChanges, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.MailboxSet, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.ThreadGet, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.ThreadChanges, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.EmailGet, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.EmailChanges, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.EmailQuery, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.EmailQueryChanges, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.EmailSet, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.EmailCopy, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.EmailImport, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.EmailParse, JmapCapabilities.Mail);
        AddStandardMethod(JmapMethods.SearchSnippetGet, JmapCapabilities.Mail);

        AddStandardMethod(JmapMethods.IdentityGet, JmapCapabilities.Submission);
        AddStandardMethod(JmapMethods.IdentityChanges, JmapCapabilities.Submission);
        AddStandardMethod(JmapMethods.IdentitySet, JmapCapabilities.Submission);
        AddStandardMethod(JmapMethods.EmailSubmissionGet, JmapCapabilities.Submission);
        AddStandardMethod(JmapMethods.EmailSubmissionChanges, JmapCapabilities.Submission);
        AddStandardMethod(JmapMethods.EmailSubmissionQuery, JmapCapabilities.Submission);
        AddStandardMethod(JmapMethods.EmailSubmissionQueryChanges, JmapCapabilities.Submission);
        AddStandardMethod(JmapMethods.EmailSubmissionSet, JmapCapabilities.Submission);

        AddStandardMethod(JmapMethods.VacationResponseGet, JmapCapabilities.VacationResponse);
        AddStandardMethod(JmapMethods.VacationResponseSet, JmapCapabilities.VacationResponse);
    }

    internal void AddStandardMethod(string methodName, string capability)
    {
        RegisteredJmapMethod method = new RegisteredJmapMethod();
        method.Name = methodName;
        method.RequiredCapabilities.Add(JmapCapabilities.Core);
        if (capability != JmapCapabilities.Core)
        {
            method.RequiredCapabilities.Add(capability);
        }

        Methods.Add(methodName, method);
    }

    internal void AddVendorRegistrations(JmapServerServices services)
    {
        if (services.VendorCapabilities == null || services.VendorMethods == null)
        {
            throw ValidationFailure("Vendor registry collections cannot be null.", "services");
        }

        foreach (string capability in services.VendorCapabilities)
        {
            AddVendorCapability(capability);
        }

        foreach (JmapVendorMethodRegistration registration in services.VendorMethods)
        {
            AddVendorMethod(registration);
        }
    }

    internal void AddVendorCapability(string? capability)
    {
        if (!Values.IsCapability(capability) || capability == null)
        {
            throw ValidationFailure("A vendor capability must be an absolute URI.",
                "vendorCapabilities");
        }

        AddCapability(capability);
    }

    internal void AddVendorMethod(JmapVendorMethodRegistration? registration)
    {
        if (registration == null || !Values.IsMethodName(registration.MethodName) ||
            registration.MethodName == null || registration.RequiredCapabilities == null ||
            registration.Handler == null)
        {
            throw ValidationFailure("A complete vendor method registration is required.",
                "vendorMethods");
        }

        if (Methods.ContainsKey(registration.MethodName))
        {
            throw ValidationFailure("A method name may only be registered once.",
                "vendorMethods/methodName");
        }

        RegisteredJmapMethod method = new RegisteredJmapMethod();
        method.Name = registration.MethodName;
        method.RequiredCapabilities.Add(JmapCapabilities.Core);
        method.VendorHandler = registration.Handler;
        foreach (string capability in registration.RequiredCapabilities)
        {
            if (!Values.IsCapability(capability))
            {
                throw ValidationFailure("A vendor method capability must be an absolute URI.",
                    "vendorMethods/requiredCapabilities");
            }

            AddCapability(capability);
            if (!method.RequiredCapabilities.Contains(capability))
            {
                method.RequiredCapabilities.Add(capability);
            }
        }

        Methods.Add(registration.MethodName, method);
    }

    internal void AddCapability(string capability)
    {
        if (CapabilitySet.Add(capability))
        {
            Capabilities.Add(capability);
        }
    }

    internal JmapValidationException ValidationFailure(string message, string path)
    {
        return new JmapValidationException(
            message,
            new List<string> { path },
            default(JsonElement));
    }
}
