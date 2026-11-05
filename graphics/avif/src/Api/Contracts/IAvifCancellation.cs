namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifCancellation"/>.</summary>
public interface IAvifCancellation
{
    /// <summary>Requests cancellation of operations observing this signal.</summary>
    void Cancel();

    /// <summary>Returns whether cancellation has been requested.</summary>
    /// <returns><c>true</c> after cancellation has been requested; otherwise <c>false</c>.</returns>
    bool IsCancellationRequested();
}
