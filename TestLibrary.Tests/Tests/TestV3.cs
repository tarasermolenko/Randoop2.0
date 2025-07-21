using Xunit;

namespace GeneratedTests
{
    public class Test1_Test3
    {
        [Fact]
        public void Test_Test1_Echo_3()
        {
            string v0 = "t4A5CYxmr";
            var instance = new TestLibrary.Test1();
            var result = instance.Echo(v0);
            Assert.Equal("Echo: t4A5CYxmr", result);

        }
    }
}
