using Xunit;

namespace GeneratedTests
{
    public class Test5_Test11
    {
        [Fact]
        public void Test_Test5_SumParams_numbers()
        {
            var instance = new TestLibrary.Test5();
            var result = instance.SumParams(new System.Int32[] { 0 });
            Assert.Equal(0, result);
        }
    }
}
