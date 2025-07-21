using Xunit;

namespace GeneratedTests
{
    public class Test2_Test43
    {
        [Fact]
        public void Test_Test2_ReturnString_43()
        {
            var instance = new TestLibrary.Test2();
            var result = instance.ReturnString();
            Assert.Equal("Name is Default", result);

        }
    }
}
