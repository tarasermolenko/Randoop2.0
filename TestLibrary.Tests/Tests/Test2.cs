using Xunit;

namespace GeneratedTests
{
    public class Test1_Test3
    {
        [Fact]
        public void Test_Test1_Echo_String()
        {
            var instance = new TestLibrary.Test1();
            var result = instance.Echo("test");
            Assert.Equal("Echo: test", result);
        }
    }
}
