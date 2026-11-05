namespace json;

internal enum JsonContainerState
{
    RootValue = 0,
    RootComplete = 1,
    ArrayFirstValueOrEnd = 2,
    ArrayValue = 3,
    ArrayCommaOrEnd = 4,
    ObjectFirstNameOrEnd = 5,
    ObjectName = 6,
    ObjectColon = 7,
    ObjectValue = 8,
    ObjectCommaOrEnd = 9,
    Closed = 10
}
