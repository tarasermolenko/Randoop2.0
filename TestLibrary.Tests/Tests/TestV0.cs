using Xunit;

namespace GeneratedTests
{
    public class Test1_Test0
    {
        [Fact]
        public void Test_Test1_Add_a_b()
        {
            var instance = new TestLibrary.Test1();
            var result = instance.Add(0,0);
            Assert.Equal(0, result);
        }
    }
}
