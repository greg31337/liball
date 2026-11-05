using System;

namespace random;

/// <summary>
/// Contains immutable finite-state transition probabilities and reusable alias rows.
/// </summary>
/// <remarks>
/// The transition matrix is row-major. Every entry must be a probability in
/// <c>[0, 1]</c> and every row must sum to one within floating-point tolerance.
/// Construction defensively copies the matrix and preprocesses one alias table per
/// current state. The model can be shared; generators remain caller-owned.
/// </remarks>
public class MarkovChain : IMarkovChain
{
    const double RowSumTolerance = 1.0e-12;
    int stateCount;
    double[] transitionMatrix;
    AliasTable[] transitionTables;

    /// <summary>
    /// Creates a reusable finite-state Markov transition model.
    /// </summary>
    /// <param name="transitionMatrix">The square row-major stochastic matrix.</param>
    /// <param name="stateCount">The positive number of rows and columns.</param>
    /// <exception cref="RandomArgumentException">The shape is invalid, an entry is not a probability, or a row does not sum to one.</exception>
    public MarkovChain(double[] transitionMatrix, int stateCount)
    {
        Guard.NotNull(transitionMatrix, "transitionMatrix");
        Guard.MatrixShape(transitionMatrix.Length, stateCount, stateCount, "transitionMatrix", "stateCount", "stateCount");
        ValidateRows(transitionMatrix, stateCount);
        this.stateCount = stateCount;
        this.transitionMatrix = (double[])transitionMatrix.Clone();
        transitionTables = new AliasTable[stateCount];
        int row = 0;
        while (row < stateCount)
        {
            double[] probabilities = new double[stateCount];
            int column = 0;
            while (column < stateCount)
            {
                probabilities[column] = this.transitionMatrix[row * stateCount + column];
                column++;
            }

            transitionTables[row] = new AliasTable(probabilities);
            row++;
        }
    }

    /// <summary>
    /// Returns the number of states represented by the transition matrix.
    /// </summary>
    /// <returns>The positive state count.</returns>
    public int StateCount()
    {
        return stateCount;
    }

    /// <summary>
    /// Returns one validated transition probability.
    /// </summary>
    /// <param name="currentState">The row state index.</param>
    /// <param name="nextState">The column state index.</param>
    /// <returns>The probability of the requested transition.</returns>
    /// <exception cref="RandomArgumentException">A state index is outside the model.</exception>
    public double TransitionProbability(int currentState, int nextState)
    {
        ValidateState(currentState, "currentState");
        ValidateState(nextState, "nextState");
        return transitionMatrix[currentState * stateCount + nextState];
    }

    /// <summary>
    /// Returns the immutable alias table preprocessed for one current state.
    /// </summary>
    /// <param name="currentState">The transition-row state index.</param>
    /// <returns>The same reusable alias table on every request for this state.</returns>
    /// <exception cref="RandomArgumentException">The state index is outside the model.</exception>
    public AliasTable TransitionTable(int currentState)
    {
        ValidateState(currentState, "currentState");
        return transitionTables[currentState];
    }

    /// <summary>
    /// Samples the next state from one transition row.
    /// </summary>
    /// <param name="generator">The generator used by the preprocessed alias row.</param>
    /// <param name="currentState">The current state index.</param>
    /// <returns>The sampled next state index.</returns>
    /// <exception cref="RandomArgumentException">The generator is null or the current state is outside the model.</exception>
    public int NextState(IRandomSource generator, int currentState)
    {
        Guard.NotNull(generator, "generator");
        ValidateState(currentState, "currentState");
        return transitionTables[currentState].Sample(generator);
    }

    /// <summary>
    /// Returns one finite-state path including its initial state.
    /// </summary>
    /// <param name="generator">The generator used for transitions.</param>
    /// <param name="initialState">The first state index.</param>
    /// <param name="stepCount">The nonnegative transition count.</param>
    /// <returns>The initial state followed by every sampled state.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid or required storage is too large.</exception>
    public int[] Path(IRandomSource generator, int initialState, int stepCount)
    {
        int requiredLength = PathOutputLength(stepCount);
        ValidatePathArguments(generator, initialState);
        int[] result = new int[requiredLength];
        FillPaths(generator, initialState, 1, stepCount, result);
        return result;
    }

    /// <summary>
    /// Writes one finite-state path including its initial state.
    /// </summary>
    /// <param name="generator">The generator used for transitions.</param>
    /// <param name="initialState">The first state index.</param>
    /// <param name="stepCount">The nonnegative transition count.</param>
    /// <param name="destination">The exact step count plus one state destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or required storage is too large.</exception>
    public void PathInto(IRandomSource generator, int initialState, int stepCount, Span<int> destination)
    {
        int requiredLength = PathOutputLength(stepCount);
        ValidatePathArguments(generator, initialState);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        FillPaths(generator, initialState, 1, stepCount, destination);
    }

    /// <summary>
    /// Returns a path-major batch of finite-state paths.
    /// </summary>
    /// <param name="generator">The generator used for transitions.</param>
    /// <param name="initialState">The first state index of every path.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative transition count per path.</param>
    /// <returns>Path-major states with each initial state included.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid or required storage is too large.</exception>
    public int[] Paths(IRandomSource generator, int initialState, int pathCount, int stepCount)
    {
        int requiredLength = PathBatchOutputLength(pathCount, stepCount);
        ValidatePathArguments(generator, initialState);
        int[] result = new int[requiredLength];
        FillPaths(generator, initialState, pathCount, stepCount, result);
        return result;
    }

    /// <summary>
    /// Writes a path-major batch of finite-state paths.
    /// </summary>
    /// <param name="generator">The generator used for transitions.</param>
    /// <param name="initialState">The first state index of every path.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative transition count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or required storage is too large.</exception>
    public void PathsInto(IRandomSource generator, int initialState, int pathCount, int stepCount, Span<int> destination)
    {
        int requiredLength = PathBatchOutputLength(pathCount, stepCount);
        ValidatePathArguments(generator, initialState);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        FillPaths(generator, initialState, pathCount, stepCount, destination);
    }

    /// <summary>
    /// Returns the state storage required for one path.
    /// </summary>
    /// <param name="stepCount">The nonnegative transition count.</param>
    /// <returns>Step count plus one.</returns>
    /// <exception cref="RandomArgumentException">The step count is negative or the state count is not representable.</exception>
    public int PathOutputLength(int stepCount)
    {
        Guard.Count(stepCount, "stepCount");
        if (stepCount == int.MaxValue)
        {
            throw RandomArgumentException.Create("stepCount", "The path contains too many states.");
        }

        return stepCount + 1;
    }

    /// <summary>
    /// Returns the state storage required for a path-major batch.
    /// </summary>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative transition count per path.</param>
    /// <returns>Path count multiplied by step count plus one.</returns>
    /// <exception cref="RandomArgumentException">A count is invalid or required storage exceeds <c>int.MaxValue</c>.</exception>
    public int PathBatchOutputLength(int pathCount, int stepCount)
    {
        Guard.Count(pathCount, "pathCount");
        int statesPerPath = PathOutputLength(stepCount);
        if (pathCount > int.MaxValue / statesPerPath)
        {
            throw RandomArgumentException.Create("pathCount", "The path batch contains too many states.");
        }

        return pathCount * statesPerPath;
    }

    void FillPaths(IRandomSource generator, int initialState, int pathCount, int stepCount, Span<int> destination)
    {
        int statesPerPath = stepCount + 1;
        int path = 0;
        while (path < pathCount)
        {
            int offset = path * statesPerPath;
            int state = initialState;
            destination[offset] = state;
            int step = 1;
            while (step <= stepCount)
            {
                state = transitionTables[state].Sample(generator);
                destination[offset + step] = state;
                step++;
            }

            path++;
        }
    }

    void ValidatePathArguments(IRandomSource generator, int initialState)
    {
        Guard.NotNull(generator, "generator");
        ValidateState(initialState, "initialState");
    }

    void ValidateState(int state, string argumentName)
    {
        if (state < 0 || state >= stateCount)
        {
            throw RandomArgumentException.Create(argumentName, "The state index is outside the transition model.");
        }
    }

    static void ValidateRows(double[] matrix, int stateCount)
    {
        int row = 0;
        while (row < stateCount)
        {
            double sum = 0.0;
            double compensation = 0.0;
            int column = 0;
            while (column < stateCount)
            {
                double probability = matrix[row * stateCount + column];
                if (!double.IsFinite(probability) || probability < 0.0 || probability > 1.0)
                {
                    throw RandomArgumentException.Create("transitionMatrix", "Every transition entry must be a finite probability.");
                }

                double adjusted = probability - compensation;
                double next = sum + adjusted;
                compensation = (next - sum) - adjusted;
                sum = next;
                column++;
            }

            if (Math.Abs(sum - 1.0) > RowSumTolerance)
            {
                throw RandomArgumentException.Create("transitionMatrix", "Every transition row must sum to one.");
            }

            row++;
        }
    }
}
