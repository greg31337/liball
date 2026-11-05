using websocket;

namespace websocket.Tests;

public class MaskKeySourceTests
{
    [Test]
    public void RepeatedCallsProduceFreshFullWidthValues()
    {
        var source = new CryptographicWebSocketMaskKeySource();
        var keys = new HashSet<uint>();
        uint combinedSetBits = 0;
        uint combinedClearBits = uint.MaxValue;

        for (var index = 0; index < 128; index++)
        {
            var key = source.CreateMaskingKey();
            keys.Add(key);
            combinedSetBits |= key;
            combinedClearBits &= key;
        }

        Assert.Multiple(() =>
        {
            Assert.That(keys.Count, Is.GreaterThan(120));
            Assert.That(combinedSetBits, Is.EqualTo(uint.MaxValue));
            Assert.That(combinedClearBits, Is.Zero);
        });
    }

    [Test]
    public void IndependentInstancesRequireNoSharedLifecycle()
    {
        var first = new CryptographicWebSocketMaskKeySource();
        var second = new CryptographicWebSocketMaskKeySource();
        var values = new HashSet<uint>();

        for (var index = 0; index < 16; index++)
        {
            values.Add(first.CreateMaskingKey());
            values.Add(second.CreateMaskingKey());
        }

        Assert.That(values.Count, Is.GreaterThan(1));
    }
}
