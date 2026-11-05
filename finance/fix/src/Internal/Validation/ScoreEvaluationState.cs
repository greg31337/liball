namespace fix;

internal class ScoreEvaluationState
{
    internal FixDictionary Dictionary = new FixDictionary();
    internal FixMessage Message = new FixMessage();
    internal ValidationContext Context = new ValidationContext();
    internal FixGroupEntry? Entry;
    internal int Depth;
}
