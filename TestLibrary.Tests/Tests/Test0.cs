using Xunit;

namespace GeneratedTests
{
    public class Test1_Test1
    {
        [Fact]
        public void Test_Test1_Add_Int32_Int32()
        {
            var instance = new TestLibrary.Test1();
            var result = instance.Add(0, 0);
            Assert.Equal(0, result);
        }
    }
}
