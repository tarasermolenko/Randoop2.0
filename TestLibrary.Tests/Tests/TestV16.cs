using Xunit;

namespace GeneratedTests
{
    public class Test1_Test16
    {
        [Fact]
        public void Test_Test1_Echo_16()
        {
            string v0 = "kwQb1";
            var instance = new TestLibrary.Test1();
            var result = instance.Echo(v0);
            Assert.Equal("Echo: kwQb1", result);

        }
    }
}
