using System;
using System.Threading;
using System.Threading.Tasks;

namespace argon2;

internal static class Argon2Executor
{
    internal static void FillMemory(
        Argon2Parameters parameters,
        Argon2ExecutionOptions executionOptions,
        Argon2Workspace workspace)
    {
        Argon2ParameterValidator.ValidateExecutionOptions(executionOptions, parameters);
        if (workspace == null)
        {
            throw new Argon2ParameterException("An Argon2 workspace is required for execution.");
        }

        int maximumWorkerCount = executionOptions.MaximumWorkerCount;
        if (workspace.GetWorkerCount() < maximumWorkerCount)
        {
            throw new Argon2ParameterException("The Argon2 workspace does not provide enough worker buffers.");
        }

        Argon2Memory memory = workspace.GetMemory();
        long passIndex = 0L;
        while (passIndex < parameters.Iterations)
        {
            int sliceIndex = 0;
            while (sliceIndex < Argon2Limits.SynchronizationPoints)
            {
                FillSlice(
                    parameters,
                    memory,
                    workspace,
                    passIndex,
                    sliceIndex,
                    maximumWorkerCount);
                sliceIndex = sliceIndex + 1;
            }

            passIndex = passIndex + 1L;
        }
    }

    static void FillSlice(
        Argon2Parameters parameters,
        Argon2Memory memory,
        Argon2Workspace workspace,
        long passIndex,
        int sliceIndex,
        int maximumWorkerCount)
    {
        if (maximumWorkerCount == 1)
        {
            int laneIndex = 0;
            while (laneIndex < parameters.DegreeOfParallelism)
            {
                ExecuteLane(
                    parameters,
                    memory,
                    workspace,
                    passIndex,
                    laneIndex,
                    sliceIndex,
                    0);
                laneIndex = laneIndex + 1;
            }

            return;
        }

        int batchStartLane = 0;
        while (batchStartLane < parameters.DegreeOfParallelism)
        {
            int remainingLanes = parameters.DegreeOfParallelism - batchStartLane;
            int batchCount = remainingLanes < maximumWorkerCount
                ? remainingLanes
                : maximumWorkerCount;
            Task[] tasks = new Task[batchCount];
            int taskIndex = 0;
            while (taskIndex < batchCount)
            {
                int laneIndex = batchStartLane + taskIndex;
                tasks[taskIndex] = StartLaneTask(
                    parameters,
                    memory,
                    workspace,
                    passIndex,
                    laneIndex,
                    sliceIndex,
                    taskIndex);
                taskIndex = taskIndex + 1;
            }

            WaitForBatch(tasks);
            batchStartLane = batchStartLane + batchCount;
        }
    }

    static Task StartLaneTask(
        Argon2Parameters parameters,
        Argon2Memory memory,
        Argon2Workspace workspace,
        long passIndex,
        int laneIndex,
        int sliceIndex,
        int workerIndex)
    {
        return Task.Run(
            () => ExecuteLane(
                parameters,
                memory,
                workspace,
                passIndex,
                laneIndex,
                sliceIndex,
                workerIndex));
    }

    static void ExecuteLane(
        Argon2Parameters parameters,
        Argon2Memory memory,
        Argon2Workspace workspace,
        long passIndex,
        int laneIndex,
        int sliceIndex,
        int workerIndex)
    {
        Argon2LaneFiller.FillSegment(
            parameters,
            memory,
            passIndex,
            laneIndex,
            sliceIndex,
            workspace.GetInputBlock(workerIndex),
            workspace.GetAddressBlock(workerIndex),
            workspace.GetTemporaryBlock(workerIndex));
    }

    static void WaitForBatch(Task[] tasks)
    {
        int taskIndex = 0;
        while (taskIndex < tasks.Length)
        {
            using (WaitHandle completion = ((System.IAsyncResult)tasks[taskIndex]).AsyncWaitHandle)
            {
                completion.WaitOne();
            }

            taskIndex = taskIndex + 1;
        }

        taskIndex = 0;
        while (taskIndex < tasks.Length)
        {
            tasks[taskIndex].GetAwaiter().GetResult();
            taskIndex = taskIndex + 1;
        }
    }
}
