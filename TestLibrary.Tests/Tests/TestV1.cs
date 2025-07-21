using Xunit;

namespace GeneratedTests
{
    public class Test1_Test1
    {
        [Fact]
        public void Test_Test1_DivideFloat_1()
        {
            double v0 = 868.9870585947126;
            double v1 = 813.18656655805;
            var instance = new TestLibrary.Test1();
            var result = instance.DivideFloat(v0,v1);
            Assert.Equal(1.0686195448024278d, result);

        }
    }
}
