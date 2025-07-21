using Xunit;

namespace GeneratedTests
{
    public class Test1_Test31
    {
        [Fact]
        public void Test_Test1_Add_31()
        {
            int v0 = 55;
            int v1 = 712;
            var instance = new TestLibrary.Test1();
            var result = instance.Add(v0,v1);
            Assert.Equal(767, result);

        }
    }
}
