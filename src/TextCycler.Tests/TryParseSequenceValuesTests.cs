using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TextCycler.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TryParseSequenceValuesTests : BaseTest
    {
        [TestMethod]
        public void TryParseSequenceValues_ShouldDoNothingIfThereAreNoSequenceValues()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new Program();

                // Act
                p.TryParseSequenceValues();

                // Assert
                Assert.IsNull(p.ParsedSequenceValues);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void TryParseSequenceValues_ShouldFailIfSequenceValueDoesNotContainsComma()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new Program
                {
                    SequenceValues = new[] { "0lorem" }
                };

                try
                {
                    // Act
                    p.TryParseSequenceValues();
                }
                catch(FailException fe)
                {
                    // Assert
                    Assert.IsTrue(fe.Message.Contains("Sequence values must be specified"));
                    return;
                }
                Assert.Fail($"Should have catched {nameof(FailException)}");
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void TryParseSequenceValues_ShouldFailIfSequenceValueContainsMoreThanOneComma()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new Program
                {
                    SequenceValues = new[] { "0,lorem,ipsum" }
                };

                try
                {
                    // Act
                    p.TryParseSequenceValues();
                }
                catch (FailException fe)
                {
                    // Assert
                    Assert.IsTrue(fe.Message.Contains("Sequence values must be specified"));
                    return;
                }
                Assert.Fail($"Should have catched {nameof(FailException)}");
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void TryParseSequenceValues_ShouldFailIfSequenceValueIndexIsNotNumeric()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new Program
                {
                    SequenceValues = new[] { "lorem,ipsum" },
                    ConfigFile = configFile
                };
                p.TryLoadConfigFile();

                try
                {
                    // Act
                    p.TryParseSequenceValues();
                }
                catch (FailException fe)
                {
                    // Assert
                    Assert.IsTrue(fe.Message.Contains("Sequence index must be numeric"));
                    return;
                }
                Assert.Fail($"Should have catched {nameof(FailException)}");
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void TryParseSequenceValues_ShouldParseEscapedAndCorrectSequenceValues()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new Program
                {
                    SequenceValues = new[] { "0,lorem", "1,ip\\,sum" },
                    ConfigFile = configFile
                };
                p.TryLoadConfigFile();

                // Act
                p.TryParseSequenceValues();

                // Assert
                Assert.AreEqual(2, p.ParsedSequenceValues.Count);
                Assert.AreEqual("lorem", p.ParsedSequenceValues[0]);
                Assert.AreEqual("ip,sum", p.ParsedSequenceValues[1]);
            }
            finally
            {
                DeleteConfig();
            }
        }
    }
}
