using System;
using System.Collections.Generic;

namespace diameter;

internal class ApplicationInterceptorEntry
{
    internal IMessageInterceptor Interceptor;
    internal string Name;
    internal int Order;

    internal ApplicationInterceptorEntry(
        IMessageInterceptor interceptor,
        string name,
        int order)
    {
        Interceptor = interceptor;
        Name = name;
        Order = order;
    }
}

internal class ApplicationInterceptorPipeline
{
    internal DiameterApplicationResult ProcessInboundApplicationInterceptors(
        IDiameterApplication? application,
        DiameterInterceptorContext? context)
    {
        return ProcessApplicationInterceptorPipeline(
            application,
            context,
            true);
    }

    internal DiameterApplicationResult ProcessOutboundApplicationInterceptors(
        IDiameterApplication? application,
        DiameterInterceptorContext? context)
    {
        return ProcessApplicationInterceptorPipeline(
            application,
            context,
            false);
    }

    internal DiameterApplicationResult ProcessApplicationInterceptorPipeline(
        IDiameterApplication? application,
        DiameterInterceptorContext? context,
        bool inbound)
    {
        if (application == null || context == null ||
            !IsSafeApplicationInterceptorContext(context))
        {
            throw new ArgumentException(
                "An application and valid interceptor context are required.");
        }

        List<ApplicationInterceptorEntry> entries =
            ReadOrderedApplicationInterceptors(application);
        DiameterMessage current = ClonePipelineMessage(context.Message);
        DiameterApplicationResult? previous =
            ClonePipelineOptionalResult(context.PreviousResult);
        DiameterInterceptorContext? lastInvocation = null;
        bool replaced = false;
        int index = 0;
        while (index < entries.Count)
        {
            ApplicationInterceptorEntry entry = entries[index];
            DiameterInterceptorContext invocation =
                ClonePipelineInterceptorContext(context);
            invocation.Message = ClonePipelineMessage(current);
            invocation.PreviousResult =
                ClonePipelineOptionalResult(previous);
            invocation.Inbound = inbound;
            DiameterApplicationResult rawResult = inbound ?
                entry.Interceptor.InterceptInboundDiameterMessage(
                    invocation) :
                entry.Interceptor.InterceptOutboundDiameterMessage(
                    invocation);
            DiameterApplicationResult result =
                NormalizeApplicationInterceptorResult(
                    rawResult,
                    invocation,
                    current);
            lastInvocation = invocation;
            if (result.Replace && result.OutputMessage != null)
            {
                current = ClonePipelineMessage(result.OutputMessage);
                previous = ClonePipelineOptionalResult(result);
                replaced = true;
                index = index + 1;
                continue;
            }

            if (result.Continue)
            {
                previous = ClonePipelineOptionalResult(result);
                index = index + 1;
                continue;
            }

            return result;
        }

        DiameterApplicationResult completed =
            new DiameterApplicationResult();
        completed.Continue = !replaced;
        completed.Replace = replaced;
        if (replaced)
        {
            completed.OutputMessage = ClonePipelineMessage(current);
            completed.Code = "interceptor-pipeline-replaced";
        }
        else
        {
            completed.Code = "interceptor-pipeline-continued";
        }

        if (lastInvocation != null)
        {
            completed.InterceptorContext =
                ClonePipelineInterceptorContext(lastInvocation);
            if (lastInvocation.RequestContext != null)
            {
                completed.RequestContext =
                    ClonePipelineRequestContext(
                        lastInvocation.RequestContext);
            }
        }

        return completed;
    }

    internal List<ApplicationInterceptorEntry>
        ReadOrderedApplicationInterceptors(IDiameterApplication application)
    {
        List<IMessageInterceptor> supplied =
            application.ReadDiameterApplicationInterceptors();
        if (supplied == null)
        {
            throw new ArgumentException(
                "An application interceptor collection is required.");
        }

        List<ApplicationInterceptorEntry> result =
            new List<ApplicationInterceptorEntry>();
        int index = 0;
        while (index < supplied.Count)
        {
            IMessageInterceptor interceptor = supplied[index];
            if (interceptor == null)
            {
                throw new ArgumentException(
                    "Application interceptors must not contain null values.");
            }

            string name = interceptor.ReadDiameterMessageInterceptorName();
            int order = interceptor.ReadDiameterMessageInterceptorOrder();
            if (string.IsNullOrWhiteSpace(name) || order < 0 ||
                ValueCodecs.FindInvalidUtf16Index(name) >= 0)
            {
                throw new ArgumentException(
                    "Application interceptors require valid names and orders.");
            }

            ApplicationInterceptorEntry entry =
                new ApplicationInterceptorEntry(
                    interceptor,
                    name.Trim(),
                    order);
            InsertApplicationInterceptor(result, entry);
            index = index + 1;
        }

        ValidateUniqueApplicationInterceptors(result);
        return result;
    }

    internal void InsertApplicationInterceptor(
        List<ApplicationInterceptorEntry> entries,
        ApplicationInterceptorEntry entry)
    {
        int index = 0;
        while (index < entries.Count)
        {
            ApplicationInterceptorEntry current = entries[index];
            if (entry.Order < current.Order ||
                (entry.Order == current.Order &&
                string.Compare(
                    entry.Name,
                    current.Name,
                    StringComparison.Ordinal) < 0))
            {
                entries.Insert(index, entry);
                return;
            }

            index = index + 1;
        }

        entries.Add(entry);
    }

    internal void ValidateUniqueApplicationInterceptors(
        List<ApplicationInterceptorEntry> entries)
    {
        int index = 0;
        while (index < entries.Count)
        {
            int comparisonIndex = index + 1;
            while (comparisonIndex < entries.Count)
            {
                if (string.Equals(
                    entries[index].Name,
                    entries[comparisonIndex].Name,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new DiameterApplicationException(
                        "Application interceptor name '" +
                        entries[index].Name + "' is registered more than once.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            index = index + 1;
        }
    }

    internal DiameterApplicationResult NormalizeApplicationInterceptorResult(
        DiameterApplicationResult? source,
        DiameterInterceptorContext invocation,
        DiameterMessage current)
    {
        if (!IsValidApplicationInterceptorResult(source, current) ||
            source == null)
        {
            DiameterApplicationResult invalid =
                CreateInvalidApplicationInterceptorResult();
            invalid.InterceptorContext =
                ClonePipelineInterceptorContext(invocation);
            if (invocation.RequestContext != null)
            {
                invalid.RequestContext =
                    ClonePipelineRequestContext(
                        invocation.RequestContext);
            }

            return invalid;
        }

        DiameterApplicationResult result =
            ClonePipelineResult(source);
        result.InterceptorContext =
            ClonePipelineInterceptorContext(invocation);
        if (invocation.RequestContext != null)
        {
            result.RequestContext =
                ClonePipelineRequestContext(invocation.RequestContext);
        }

        return result;
    }

    internal bool IsValidApplicationInterceptorResult(
        DiameterApplicationResult? result,
        DiameterMessage current)
    {
        if (result == null || result.RedirectHosts == null ||
            result.Code == null || result.Description == null ||
            CountApplicationInterceptorActions(result) != 1 ||
            ValueCodecs.FindInvalidUtf16Index(result.Code) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(result.Description) >= 0 ||
            (result.ResultCode != 0U &&
            (result.ResultCode < 1000U || result.ResultCode > 5999U)))
        {
            return false;
        }

        if (result.Continue)
        {
            return result.OutputMessage == null &&
                result.RedirectHosts.Count == 0 &&
                result.ResultCode == 0U;
        }

        if (result.Replace)
        {
            return result.OutputMessage != null &&
                result.RedirectHosts.Count == 0 &&
                result.ResultCode == 0U &&
                IsSafeApplicationPipelineMessage(result.OutputMessage) &&
                HasMatchingApplicationMessageDirection(
                    current,
                    result.OutputMessage);
        }

        if (result.Answer)
        {
            return result.OutputMessage != null &&
                result.RedirectHosts.Count == 0 &&
                IsSafeApplicationPipelineMessage(result.OutputMessage) &&
                IsCorrelatedApplicationInterceptorAnswer(
                    current,
                    result.OutputMessage);
        }

        if (result.Drop)
        {
            return result.OutputMessage == null &&
                result.RedirectHosts.Count == 0 &&
                result.ResultCode == 0U;
        }

        if (result.Redirect)
        {
            return result.OutputMessage == null &&
                result.RedirectHosts.Count > 0 &&
                ValidateApplicationInterceptorRedirects(
                    result.RedirectHosts);
        }

        return result.Fail && result.OutputMessage == null &&
            result.RedirectHosts.Count == 0 &&
            result.ResultCode >= 3000U &&
            result.ResultCode < 6000U;
    }

    internal bool ValidateApplicationInterceptorRedirects(
        List<DiameterUri> redirects)
    {
        int index = 0;
        while (index < redirects.Count)
        {
            DiameterUri redirect = redirects[index];
            if (redirect == null)
            {
                return false;
            }

            DiameterValueResult normalized =
                ValueCodecs.NormalizeDiameterUri(redirect);
            if (normalized.Status != DiameterCodecStatus.Success ||
                normalized.Uri == null ||
                normalized.Uri.Protocol != "diameter")
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsCorrelatedApplicationInterceptorAnswer(
        DiameterMessage current,
        DiameterMessage answer)
    {
        return (current.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None &&
            (answer.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None &&
            (answer.Flags & DiameterMessageFlags.PotentiallyRetransmitted) ==
                DiameterMessageFlags.None &&
            answer.Version == current.Version &&
            answer.CommandCode == current.CommandCode &&
            answer.ApplicationId == current.ApplicationId &&
            answer.HopByHopIdentifier == current.HopByHopIdentifier &&
            answer.EndToEndIdentifier == current.EndToEndIdentifier;
    }

    internal bool HasMatchingApplicationMessageDirection(
        DiameterMessage current,
        DiameterMessage replacement)
    {
        bool currentRequest =
            (current.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None;
        bool replacementRequest =
            (replacement.Flags & DiameterMessageFlags.Request) !=
                DiameterMessageFlags.None;
        return currentRequest == replacementRequest;
    }

    internal bool IsSafeApplicationInterceptorContext(
        DiameterInterceptorContext context)
    {
        if (!IsSafeApplicationPipelineMessage(context.Message) ||
            context.RouteIdentifier == null || context.SessionId == null ||
            ValueCodecs.FindInvalidUtf16Index(context.RouteIdentifier) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(context.SessionId) >= 0 ||
            context.InvokedAt == default(DateTimeOffset))
        {
            return false;
        }

        if (context.PeerIdentity != null &&
            ValueCodecs.NormalizeDiameterIdentity(
                context.PeerIdentity).Status != DiameterCodecStatus.Success)
        {
            return false;
        }

        if (context.RequestContext != null &&
            (context.RequestContext.Request == null ||
            context.RequestContext.Request.CommandCode !=
                context.Message.CommandCode ||
            context.RequestContext.Request.ApplicationId !=
                context.Message.ApplicationId ||
            (context.Message.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None))
        {
            return false;
        }

        return true;
    }

    internal bool IsSafeApplicationPipelineMessage(DiameterMessage? message)
    {
        if (message == null || message.Avps == null ||
            message.Version != DiameterProtocolConstants.Version ||
            message.CommandCode == 0U ||
            message.CommandCode > DiameterProtocolConstants.MaximumCommandCode)
        {
            return false;
        }

        int flags = (int)message.Flags;
        if (flags < 0 || flags > 255 ||
            (flags & DiameterProtocolConstants.MessageReservedFlagMask) != 0 ||
            ((message.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None &&
            (message.Flags & DiameterMessageFlags.PotentiallyRetransmitted) !=
                DiameterMessageFlags.None))
        {
            return false;
        }

        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp == null || avp.Data == null)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal int CountApplicationInterceptorActions(
        DiameterApplicationResult result)
    {
        int count = 0;
        if (result.Continue)
        {
            count = count + 1;
        }

        if (result.Replace)
        {
            count = count + 1;
        }

        if (result.Answer)
        {
            count = count + 1;
        }

        if (result.Drop)
        {
            count = count + 1;
        }

        if (result.Redirect)
        {
            count = count + 1;
        }

        if (result.Fail)
        {
            count = count + 1;
        }

        return count;
    }

    internal DiameterApplicationResult CreateInvalidApplicationInterceptorResult()
    {
        DiameterApplicationResult result =
            new DiameterApplicationResult();
        result.Continue = false;
        result.Fail = true;
        result.ResultCode = DiameterResultCodes.UnableToComply;
        result.Code = "interceptor-result-invalid";
        result.Description =
            "An application interceptor returned an invalid result.";
        return result;
    }

    internal DiameterApplicationResult ClonePipelineResult(
        DiameterApplicationResult source)
    {
        DiameterApplicationResult result =
            new DiameterApplicationResult();
        result.Continue = source.Continue;
        result.Replace = source.Replace;
        result.Answer = source.Answer;
        result.Drop = source.Drop;
        result.Redirect = source.Redirect;
        result.Fail = source.Fail;
        result.ResultCode = source.ResultCode;
        result.Code = source.Code;
        result.Description = source.Description;
        if (source.OutputMessage != null)
        {
            result.OutputMessage =
                ClonePipelineMessage(source.OutputMessage);
        }

        int index = 0;
        while (index < source.RedirectHosts.Count)
        {
            result.RedirectHosts.Add(
                ClonePipelineUri(source.RedirectHosts[index]));
            index = index + 1;
        }

        return result;
    }

    internal DiameterApplicationResult? ClonePipelineOptionalResult(
        DiameterApplicationResult? source)
    {
        if (source == null)
        {
            return null;
        }

        if (source.RedirectHosts == null)
        {
            return CreateInvalidApplicationInterceptorResult();
        }

        return ClonePipelineResult(source);
    }

    internal DiameterInterceptorContext ClonePipelineInterceptorContext(
        DiameterInterceptorContext source)
    {
        DiameterInterceptorContext result =
            new DiameterInterceptorContext();
        result.Message = ClonePipelineMessage(source.Message);
        if (source.RequestContext != null)
        {
            result.RequestContext =
                ClonePipelineRequestContext(source.RequestContext);
        }

        result.PreviousResult =
            ClonePipelineOptionalResult(source.PreviousResult);
        if (source.PeerIdentity != null)
        {
            result.PeerIdentity =
                ClonePipelineIdentity(source.PeerIdentity);
        }

        result.RouteIdentifier = source.RouteIdentifier;
        result.SessionId = source.SessionId;
        result.InvokedAt = source.InvokedAt;
        result.Inbound = source.Inbound;
        result.Duplicate = source.Duplicate;
        result.Retransmission = source.Retransmission;
        result.Cancelled = source.Cancelled;
        result.ShuttingDown = source.ShuttingDown;
        return result;
    }

    internal DiameterRequestContext ClonePipelineRequestContext(
        DiameterRequestContext source)
    {
        DiameterRequestContext result = new DiameterRequestContext();
        result.Registration =
            ApplicationRegistryCloner.CloneApplicationRegistration(
                source.Registration);
        result.Command =
            ApplicationRegistryCloner.CloneApplicationCommand(
                source.Command);
        result.Request = ClonePipelineMessage(source.Request);
        result.PeerIdentity = ClonePipelineIdentity(source.PeerIdentity);
        result.RouteIdentifier = source.RouteIdentifier;
        result.SessionId = source.SessionId;
        if (source.ValidationResult != null)
        {
            DiameterValidationResult validation =
                new DiameterValidationResult();
            validation.Status = source.ValidationResult.Status;
            result.ValidationResult = validation;
        }

        result.ReceivedAt = source.ReceivedAt;
        result.Duplicate = source.Duplicate;
        result.Retransmission = source.Retransmission;
        result.Cancelled = source.Cancelled;
        result.ShuttingDown = source.ShuttingDown;
        return result;
    }

    internal DiameterMessage ClonePipelineMessage(DiameterMessage source)
    {
        DiameterMessage result = new DiameterMessage();
        result.Version = source.Version;
        result.Flags = source.Flags;
        result.CommandCode = source.CommandCode;
        result.ApplicationId = source.ApplicationId;
        result.HopByHopIdentifier = source.HopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        int index = 0;
        while (index < source.Avps.Count)
        {
            DiameterAvp sourceAvp = source.Avps[index];
            DiameterAvp avp = new DiameterAvp();
            avp.Code = sourceAvp.Code;
            avp.Flags = sourceAvp.Flags;
            avp.VendorId = sourceAvp.VendorId;
            avp.Data = new byte[sourceAvp.Data.Length];
            Array.Copy(sourceAvp.Data, avp.Data, sourceAvp.Data.Length);
            result.Avps.Add(avp);
            index = index + 1;
        }

        return result;
    }

    internal DiameterIdentity ClonePipelineIdentity(DiameterIdentity source)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = source.Value;
        return result;
    }

    internal DiameterUri ClonePipelineUri(DiameterUri source)
    {
        DiameterUri result = new DiameterUri();
        result.Scheme = source.Scheme;
        result.Host = ClonePipelineIdentity(source.Host);
        result.Port = source.Port;
        result.Transport = source.Transport;
        result.Protocol = source.Protocol;
        return result;
    }
}
