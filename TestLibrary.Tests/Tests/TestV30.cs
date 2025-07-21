using Xunit;

namespace GeneratedTests
{
    public class Test1_Test30
    {
        [Fact]
        public void Test_Test1_DivideFloat_30()
        {
            double v0 = 866.0533890051347;
            double v1 = -946.0053508127587;
            var instance = new TestLibrary.Test1();
            var result = instance.DivideFloat(v0,v1);
            Assert.Equal(-0.9154846621756066d, result);

        }
    }
}
