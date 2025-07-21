using Xunit;

namespace GeneratedTests
{
    public class Test8Derived_Test7
    {
        [Fact]
        public void Test_Test8Derived_VirtualMethod_7()
        {
            var instance = new TestLibrary.Test8Derived();
            var result = instance.VirtualMethod();
            Assert.Equal("Derived virtual method", result);

        }
    }
}
