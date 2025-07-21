using Xunit;

namespace GeneratedTests
{
    public class Test5_Test32
    {
        [Fact]
        public void Test_Test5_SumParams_32()
        {
            int v0 = 828;
            var instance = new TestLibrary.Test5();
            var result = instance.SumParams(new System.Int32[] { v0 });
            Assert.Equal(v0, result);

        }
    }
}
