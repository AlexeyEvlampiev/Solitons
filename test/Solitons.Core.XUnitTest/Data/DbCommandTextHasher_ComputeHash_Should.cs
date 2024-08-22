using Xunit;

namespace Solitons.Data;

public sealed class DbCommandTextHasher_ComputeHash_Should
{
    [Theory]
    [InlineData(@"
        SELECT * FROM public.test
    ")]
    [InlineData(@"
        -- This is a test
        SELECT * FROM public.test
    ")]
    [InlineData(@"
        SELECT * FROM public.test
        /*
            This is a test
        */
    ")]
    [InlineData(@"
        -- This is a test
        SELECT * FROM public.test
        /*
            This is a test
        */
    ")]
    public void Work(string command)
    {
        using var alg = new SqlCommandTextHasher();
        var expectedHash = alg.ComputeHash("SELECT * FROM public.test");
        var actualHash = alg.ComputeHash(command);
        Assert.Equal(expectedHash, actualHash);
    }
}