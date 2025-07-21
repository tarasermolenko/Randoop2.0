using Xunit;

namespace GeneratedTests
{
    public class Test1_Test29
    {
        [Fact]
        public void Test_Test1_DivideFloat_29()
        {
            double v0 = -33.30620906166371;
            double v1 = 186.53805381356915;
            var instance = new TestLibrary.Test1();
            var result = instance.DivideFloat(v0,v1);
            Assert.Equal(-0.1785491398712178d, result);

        }
    }
}
