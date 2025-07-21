using Xunit;

namespace GeneratedTests
{
    public class Test1_Test11
    {
        [Fact]
        public void Test_Test1_DivideFloat_11()
        {
            double v0 = 192.53653180001507;
            double v1 = -872.6416638946336;
            var instance = new TestLibrary.Test1();
            var result = instance.DivideFloat(v0,v1);
            Assert.Equal(-0.2206364190092839d, result);

        }
    }
}
