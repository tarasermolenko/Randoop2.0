using Xunit;

namespace GeneratedTests
{
    public class Test1_Test4
    {
        [Fact]
        public void Test_Test1_Echo_4()
        {
            string v0 = "VE4DHXSBKaQY";
            var instance = new TestLibrary.Test1();
            var result = instance.Echo(v0);
            Assert.Equal("Echo: VE4DHXSBKaQY", result);

        }
    }
}
