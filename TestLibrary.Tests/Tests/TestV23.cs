using Xunit;

namespace GeneratedTests
{
    public class Test1_Test23
    {
        [Fact]
        public void Test_Test1_Echo_23()
        {
            string v0 = "T0Ows0BRTw4NO";
            var instance = new TestLibrary.Test1();
            var result = instance.Echo(v0);
            Assert.Equal("Echo: T0Ows0BRTw4NO", result);

        }
    }
}
