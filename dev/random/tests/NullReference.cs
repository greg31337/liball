namespace random.Tests;

internal static class NullReference
{
    internal static T Value<T>() where T : class
    {
        // A newly allocated reference array contains null while its indexed expression has type T.
        return (new T[1])[0];
    }
}

internal static class ReflectionInfo
{
    internal static T Required<T>(object? value) where T : class
    {
        if (value is T result)
        {
            return result;
        }

        throw new InvalidOperationException("The required reflection metadata was not found.");
    }
}
