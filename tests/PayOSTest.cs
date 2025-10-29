using Xunit;

namespace PayOS.Tests;

[Collection("payos-test")]
public class PayOSTest
{
    [Fact]
    public void TestShouldPass()
    {
        var expected = true;
        var actual = true;
        Assert.Equal(expected, actual);
    }
}