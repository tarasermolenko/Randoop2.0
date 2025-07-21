using Xunit;

namespace GeneratedTests
{
    public class Test1_Test17
    {
        [Fact]
        public void Test_Test1_DivideInt_17()
        {
            int v0 = -523;
            int v1 = 10;
            var instance = new TestLibrary.Test1();
            var result = instance.DivideInt(v0,v1);
            Assert.Equal(-52, result);

        }
    }
}
