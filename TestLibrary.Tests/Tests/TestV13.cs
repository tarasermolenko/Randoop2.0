using Xunit;

namespace GeneratedTests
{
    public class Test1_Test13
    {
        [Fact]
        public void Test_Test1_Add_13()
        {
            int v0 = 970;
            int v1 = 806;
            var instance = new TestLibrary.Test1();
            var result = instance.Add(v0,v1);
            Assert.Equal(1776, result);

        }
    }
}
