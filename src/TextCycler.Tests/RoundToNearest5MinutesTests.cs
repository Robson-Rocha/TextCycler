using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;

namespace TextCycler.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class RoundToNearest5MinutesTests
    {
        [TestMethod]
        public void RoundToNearest5Minutes_ShouldRoundUp()
        {
            // Arrange
            var dt = new DateTime(2021, 01, 01, 12, 03, 00);
            var expected = new DateTime(2021, 01, 01, 12, 05, 00);

            // Act
            var actual = TextCycler.RoundToNearest5Minutes(dt);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RoundToNearest5Minutes_ShouldRoundDown()
        {
            // Arrange
            var dt = new DateTime(2021, 01, 01, 12, 07, 00);
            var expected = new DateTime(2021, 01, 01, 12, 05, 00);

            // Act
            var actual = TextCycler.RoundToNearest5Minutes(dt);

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
