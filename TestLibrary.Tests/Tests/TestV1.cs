using Xunit;

namespace GeneratedTests
{
    public class Test1_Test1
    {
        [Fact]
        public void Test_Test1_Echo_1()
        {
            string v0 = "0L9";
            var instance = new TestLibrary.Test1();
            var result = instance.Echo(v0);
            Assert.Equal("Echo: 0L9", result);

        }
    }
}
