using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace diameter;

internal interface IApplicationWorkCompletionHandler
{
    void CompleteApplicationWork(ApplicationWorkCompletion completion);
}

internal class ApplicationWorkAdmission
{
    internal long WorkIdentifier;
    internal bool Accepted;
    internal bool Started;
    internal bool Queued;
    internal bool Overloaded;
    internal DiameterApplicationResult? RejectionResult;
}

internal class ApplicationWorkCompletion
{
    internal long WorkIdentifier;
    internal bool HandlerFailed;
    internal DiameterApplicationResult Result =
        new DiameterApplicationResult();
}

internal class ApplicationWorkQueueSnapshot
{
    internal string ApplicationName = string.Empty;
    internal int MaximumConcurrency;
    internal int QueueCapacity;
    internal int ActiveCount;
    internal int QueuedCount;
    internal int CallbackCount;
    internal bool Accepting;
    internal bool Forced;
    internal long AcceptedCount;
    internal long CompletedCount;
    internal long FailedCount;
    internal long RejectedCount;
}

internal class ApplicationWorkItem
{
    internal long WorkIdentifier;
    internal IDiameterRequestHandler Handler;
    internal DiameterRequestContext Context;
    internal IApplicationWorkCompletionHandler CompletionHandler;

    internal ApplicationWorkItem(
        long workIdentifier,
        IDiameterRequestHandler handler,
        DiameterRequestContext context,
        IApplicationWorkCompletionHandler completionHandler)
    {
        WorkIdentifier = workIdentifier;
        Handler = handler;
        Context = context;
        CompletionHandler = completionHandler;
    }
}

internal class ApplicationWorkQueue
{
    internal object QueueLock = new object();
    internal List<ApplicationWorkItem> QueuedItems =
        new List<ApplicationWorkItem>();
    internal string ApplicationName;
    internal int MaximumConcurrency;
    internal int QueueCapacity;
    internal int ActiveCount;
    internal int CallbackCount;
    internal bool Accepting = true;
    internal bool Forced;
    internal long NextWorkIdentifier = 1L;
    internal long AcceptedCount;
    internal long CompletedCount;
    internal long FailedCount;
    internal long RejectedCount;
    internal ApplicationInterceptorPipeline Cloner =
        new ApplicationInterceptorPipeline();
    internal DiameterOverloadAdmissionController OverloadAdmission =
        new DiameterOverloadAdmissionController();
    internal ManualResetEventSlim DrainSignal =
        new ManualResetEventSlim(true);

    internal ApplicationWorkQueue(
        DiameterApplicationRegistration? registration)
    {
        if (registration == null)
        {
            throw new ArgumentException(
                "An application work queue requires a registration.");
        }

        registration.ValidateApplicationRegistration();
        ApplicationName = registration.Name.Trim();
        MaximumConcurrency = registration.MaximumConcurrency;
        QueueCapacity = registration.QueueCapacity;
    }

    internal ApplicationWorkAdmission EnqueueApplicationWork(
        IDiameterRequestHandler? handler,
        DiameterRequestContext? context,
        IApplicationWorkCompletionHandler? completionHandler)
    {
        if (handler == null || context == null || completionHandler == null)
        {
            throw new ArgumentException(
                "Application work requires a handler, context, and completion handler.");
        }

        context.ValidateRequestContext();
        DiameterRequestContext ownedContext =
            Cloner.ClonePipelineRequestContext(context);
        ApplicationWorkItem? startingItem = null;
        ApplicationWorkAdmission admission =
            new ApplicationWorkAdmission();
        DiameterAdmissionDecision overload =
            OverloadAdmission.EvaluateDiameterApplicationAdmission(
                ownedContext);
        lock (QueueLock)
        {
            admission.WorkIdentifier = NextWorkIdentifier;
            NextWorkIdentifier = NextWorkIdentifier == long.MaxValue ?
                1L : NextWorkIdentifier + 1L;
            if (!overload.Admitted)
            {
                RejectedCount = IncrementApplicationWorkCounter(
                    RejectedCount);
                admission.Overloaded = true;
                admission.RejectionResult =
                    CreateApplicationOverloadAdmissionResult(
                        ownedContext,
                        overload);
            }
            else if (!Accepting)
            {
                RejectedCount = IncrementApplicationWorkCounter(
                    RejectedCount);
                admission.RejectionResult =
                    CreateApplicationQueueNotAcceptingResult(ownedContext);
            }
            else if (ActiveCount < MaximumConcurrency)
            {
                startingItem = new ApplicationWorkItem(
                    admission.WorkIdentifier,
                    handler,
                    ownedContext,
                    completionHandler);
                ActiveCount = ActiveCount + 1;
                AcceptedCount = IncrementApplicationWorkCounter(
                    AcceptedCount);
                admission.Accepted = true;
                admission.Started = true;
                UpdateApplicationDrainSignal();
            }
            else if (QueuedItems.Count < QueueCapacity)
            {
                QueuedItems.Add(new ApplicationWorkItem(
                    admission.WorkIdentifier,
                    handler,
                    ownedContext,
                    completionHandler));
                AcceptedCount = IncrementApplicationWorkCounter(
                    AcceptedCount);
                admission.Accepted = true;
                admission.Queued = true;
                UpdateApplicationDrainSignal();
            }
            else
            {
                RejectedCount = IncrementApplicationWorkCounter(
                    RejectedCount);
                admission.Overloaded = true;
                admission.RejectionResult =
                    CreateApplicationQueueOverloadResult(ownedContext);
            }
        }

        if (startingItem != null)
        {
            StartApplicationWorkItem(startingItem);
        }

        return admission;
    }

    internal void ConfigureDiameterOverloadAdmission(
        IDiameterOverloadManager? manager)
    {
        OverloadAdmission.ConfigureDiameterOverloadAdmission(manager);
    }

    internal ApplicationWorkQueueSnapshot ReadApplicationWorkQueueSnapshot()
    {
        lock (QueueLock)
        {
            ApplicationWorkQueueSnapshot result =
                new ApplicationWorkQueueSnapshot();
            result.ApplicationName = ApplicationName;
            result.MaximumConcurrency = MaximumConcurrency;
            result.QueueCapacity = QueueCapacity;
            result.ActiveCount = ActiveCount;
            result.QueuedCount = QueuedItems.Count;
            result.CallbackCount = CallbackCount;
            result.Accepting = Accepting;
            result.Forced = Forced;
            result.AcceptedCount = AcceptedCount;
            result.CompletedCount = CompletedCount;
            result.FailedCount = FailedCount;
            result.RejectedCount = RejectedCount;
            return result;
        }
    }

    internal void BeginApplicationWorkDrain()
    {
        lock (QueueLock)
        {
            Accepting = false;
            UpdateApplicationDrainSignal();
        }
    }

    internal bool WaitForApplicationWorkDrain(int timeoutMilliseconds)
    {
        if (timeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException("timeoutMilliseconds");
        }

        return DrainSignal.Wait(timeoutMilliseconds);
    }

    internal int ForceStopApplicationWork()
    {
        List<ApplicationWorkItem> removedItems =
            new List<ApplicationWorkItem>();
        lock (QueueLock)
        {
            Accepting = false;
            Forced = true;
            int index = 0;
            while (index < QueuedItems.Count)
            {
                removedItems.Add(QueuedItems[index]);
                index = index + 1;
            }

            QueuedItems.Clear();
            CompletedCount = AddApplicationWorkCounter(
                CompletedCount,
                removedItems.Count);
            FailedCount = AddApplicationWorkCounter(
                FailedCount,
                removedItems.Count);
            CallbackCount = CallbackCount + removedItems.Count;
            UpdateApplicationDrainSignal();
        }

        int removedIndex = 0;
        while (removedIndex < removedItems.Count)
        {
            ApplicationWorkItem item = removedItems[removedIndex];
            ApplicationWorkCompletion completion =
                new ApplicationWorkCompletion();
            completion.WorkIdentifier = item.WorkIdentifier;
            completion.HandlerFailed = true;
            completion.Result =
                CreateApplicationForcedStopResult(item.Context);
            StartApplicationWorkCompletionCallback(
                item.CompletionHandler,
                completion);
            removedIndex = removedIndex + 1;
        }

        return removedItems.Count;
    }

    internal void StartApplicationWorkItem(ApplicationWorkItem item)
    {
        Task<DiameterApplicationResult> execution = Task.Run(() =>
            item.Handler.HandleDiameterApplicationRequest(item.Context));
        execution.ContinueWith(completed =>
            CompleteApplicationWorkItem(item, completed),
            TaskScheduler.Default);
    }

    internal void CompleteApplicationWorkItem(
        ApplicationWorkItem item,
        Task<DiameterApplicationResult> execution)
    {
        ApplicationWorkCompletion completion =
            CreateApplicationWorkCompletion(item, execution);
        ApplicationWorkItem? nextItem = null;
        lock (QueueLock)
        {
            ActiveCount = ActiveCount - 1;
            CompletedCount = IncrementApplicationWorkCounter(
                CompletedCount);
            if (completion.HandlerFailed)
            {
                FailedCount = IncrementApplicationWorkCounter(FailedCount);
            }

            if (QueuedItems.Count > 0)
            {
                nextItem = QueuedItems[0];
                QueuedItems.RemoveAt(0);
                ActiveCount = ActiveCount + 1;
            }

            CallbackCount = CallbackCount + 1;
            UpdateApplicationDrainSignal();
        }

        if (nextItem != null)
        {
            StartApplicationWorkItem(nextItem);
        }

        StartApplicationWorkCompletionCallback(
            item.CompletionHandler,
            completion);
    }

    internal void StartApplicationWorkCompletionCallback(
        IApplicationWorkCompletionHandler completionHandler,
        ApplicationWorkCompletion completion)
    {
        Task callback = Task.Run(() =>
            completionHandler.CompleteApplicationWork(completion));
        callback.ContinueWith(completed =>
            CompleteApplicationWorkCallback(completed),
            TaskScheduler.Default);
    }

    internal void CompleteApplicationWorkCallback(Task callback)
    {
        if (callback.IsFaulted)
        {
            callback.Exception?.GetBaseException();
        }

        lock (QueueLock)
        {
            CallbackCount = CallbackCount - 1;
            UpdateApplicationDrainSignal();
        }
    }

    internal ApplicationWorkCompletion CreateApplicationWorkCompletion(
        ApplicationWorkItem item,
        Task<DiameterApplicationResult> execution)
    {
        ApplicationWorkCompletion completion =
            new ApplicationWorkCompletion();
        completion.WorkIdentifier = item.WorkIdentifier;
        if (execution.IsFaulted || execution.IsCanceled)
        {
            if (execution.IsFaulted)
            {
                execution.Exception?.GetBaseException();
            }

            completion.HandlerFailed = true;
            completion.Result =
                CreateApplicationHandlerFailureResult(item.Context);
            return completion;
        }

        DiameterApplicationResult? result = execution.Result;
        if (result == null)
        {
            completion.HandlerFailed = true;
            completion.Result =
                CreateApplicationHandlerFailureResult(item.Context);
            return completion;
        }

        completion.Result = result;
        return completion;
    }

    internal DiameterApplicationResult CreateApplicationQueueOverloadResult(
        DiameterRequestContext context)
    {
        DiameterApplicationResult result =
            new DiameterApplicationResult();
        result.Continue = false;
        result.Fail = true;
        result.RequestContext = context;
        result.ResultCode = DiameterResultCodes.TooBusy;
        result.Code = "application-queue-overloaded";
        result.Description =
            "The application work queue has reached its configured capacity.";
        return result;
    }

    internal DiameterApplicationResult CreateApplicationOverloadAdmissionResult(
        DiameterRequestContext context,
        DiameterAdmissionDecision decision)
    {
        DiameterApplicationResult result =
            new DiameterApplicationResult();
        result.Continue = false;
        result.Fail = true;
        result.RequestContext = context;
        result.ResultCode = decision.ResultCode == 0U ?
            DiameterResultCodes.TooBusy : decision.ResultCode;
        result.Code = decision.Code.Length == 0 ?
            "application-overload-admission-rejected" : decision.Code;
        result.Description = decision.Message.Length == 0 ?
            "Overload admission rejected the application request." :
            decision.Message;
        return result;
    }

    internal DiameterApplicationResult CreateApplicationQueueNotAcceptingResult(
        DiameterRequestContext context)
    {
        DiameterApplicationResult result =
            new DiameterApplicationResult();
        result.Continue = false;
        result.Fail = true;
        result.RequestContext = context;
        result.ResultCode = DiameterResultCodes.TooBusy;
        result.Code = "application-queue-not-accepting";
        result.Description =
            "The application work queue is draining or stopped.";
        return result;
    }

    internal DiameterApplicationResult CreateApplicationHandlerFailureResult(
        DiameterRequestContext context)
    {
        DiameterApplicationResult result =
            new DiameterApplicationResult();
        result.Continue = false;
        result.Fail = true;
        result.RequestContext = context;
        result.ResultCode = DiameterResultCodes.UnableToComply;
        result.Code = "application-handler-exception";
        result.Description =
            "The application handler failed while processing the request.";
        return result;
    }

    internal DiameterApplicationResult CreateApplicationForcedStopResult(
        DiameterRequestContext context)
    {
        DiameterApplicationResult result =
            new DiameterApplicationResult();
        result.Continue = false;
        result.Fail = true;
        result.RequestContext = context;
        result.ResultCode = DiameterResultCodes.UnableToComply;
        result.Code = "application-work-forced-stop";
        result.Description =
            "Queued application work was removed by a forced stop.";
        return result;
    }

    internal long IncrementApplicationWorkCounter(long value)
    {
        return value == long.MaxValue ? value : value + 1L;
    }

    internal long AddApplicationWorkCounter(long value, int addition)
    {
        if (addition <= 0 || value == long.MaxValue)
        {
            return value;
        }

        long available = long.MaxValue - value;
        if ((long)addition > available)
        {
            return long.MaxValue;
        }

        return value + addition;
    }

    internal void UpdateApplicationDrainSignal()
    {
        if (ActiveCount == 0 && QueuedItems.Count == 0 &&
            CallbackCount == 0)
        {
            DrainSignal.Set();
        }
        else
        {
            DrainSignal.Reset();
        }
    }
}
