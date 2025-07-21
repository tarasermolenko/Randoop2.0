using Xunit;

namespace GeneratedTests
{
    public class Test8Derived_Test32
    {
        [Fact]
        public void Test_Test8Derived_VirtualMethod_32()
        {
            var instance = new TestLibrary.Test8Derived();
            var result = instance.VirtualMethod();
            Assert.Equal("Derived virtual method", result);

        }
    }
}
