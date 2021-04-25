using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TextCycler.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class SucceedAndFailTests
    {
        [TestMethod]
        public void Succeed_ShouldSleep()
        {
            // Arrange
            var p = new Program
            {
                Delay = 1
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            p.Succeed("this is a test");
            sw.Stop();

            // Assert
            Assert.IsTrue(sw.ElapsedMilliseconds >= 1000);
        }

        [TestMethod]
        public void Fail_ShouldSleep()
        {
            // Arrange
            var p = new Program
            {
                Delay = 1
            };

            // Act
            var sw = new Stopwatch();
            sw.Start();
            p.Fail("this is a test", throwException: false);
            sw.Stop();

            // Assert
            Assert.IsTrue(sw.ElapsedMilliseconds >= 1000);
        }
    }
}
