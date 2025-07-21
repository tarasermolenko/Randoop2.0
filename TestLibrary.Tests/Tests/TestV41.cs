using Xunit;

namespace GeneratedTests
{
    public class Test1_Test41
    {
        [Fact]
        public void Test_Test1_Echo_41()
        {
            string v0 = "kSoRYDSuo9GB";
            var instance = new TestLibrary.Test1();
            var result = instance.Echo(v0);
            Assert.Equal("Echo: kSoRYDSuo9GB", result);

        }
    }
}
