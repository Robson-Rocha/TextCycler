using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace TextCycler.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TryLoadConfigFileTests : BaseTest
    {
        [TestMethod]
        public void TryLoadConfigFile_ShouldFailIfConfigFileDoesNotExists()
        {
            // Arrange
            const string configFile = "notExists.json";
            var p = new Program
            {
                ConfigFile = configFile
            };

            try
            {
                // Act
                p.TryLoadConfigFile();
            }
            catch (FailException fe)
            {
                // Assert
                Assert.IsTrue(fe.Message.Contains(configFile));
                Assert.IsTrue(fe.Message.Contains("not exist"));
                return;
            }
            Assert.Fail($"Should have catched {nameof(FailException)}");
        }

        [TestMethod]
        public void TryLoadConfigFile_ShouldFailIfConfigFileIsInvalid()
        {
            // Arrange
            const string configFile = "invalid.json";
            try
            {
                File.WriteAllText(configFile, "{");
                var p = new Program
                {
                    ConfigFile = configFile
                };

                try
                {
                    // Act
                    p.TryLoadConfigFile();
                }
                catch (FailException fe)
                {
                    // Assert
                    Assert.IsTrue(fe.Message.Contains(configFile));
                    Assert.IsTrue(fe.Message.Contains("has errors"));
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
        public void TryLoadConfigFile_ShouldSucceedIfConfigFileIsValid()
        {
            // Arrange
            try
            {
                CreateConfig();

                var p_act = new Program
                {
                    ConfigFile = configFile
                };

                // Act
                p_act.TryLoadConfigFile();

                // Assert
                Assert.AreEqual(0, p_act.CurrentConfig.NextTextIndex);
                Assert.AreEqual("targetfile.txt", p_act.CurrentConfig.TargetFile);
                Assert.AreEqual(4, p_act.CurrentConfig.Texts.Length);
                Assert.AreEqual(2, p_act.CurrentConfig.Sequences.Length);
                Assert.AreEqual(5, p_act.CurrentConfig.Sequences[0].Length);
                Assert.AreEqual(3, p_act.CurrentConfig.Sequences[1].Length);
                Assert.AreEqual(2, p_act.CurrentConfig.SequencePositions.Length);
                Assert.AreEqual(0, p_act.CurrentConfig.LastTextIndexUsedInMenu);
                Assert.AreEqual("", p_act.CurrentConfig.LastWrittenText);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void TryLoadConfigFile_ShouldFixMissingNextTextIndex()
        {
            // Arrange
            try
            {
                CreateConfig();

                UpdateConfig(config => config.NextTextIndex = null);

                var p_act = new Program
                {
                    ConfigFile = configFile
                };

                // Act
                p_act.TryLoadConfigFile();

                // Assert
                Assert.AreEqual(0, p_act.CurrentConfig.NextTextIndex);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void TryLoadConfigFile_ShouldFixMissingSequencePositions()
        {
            // Arrange
            try
            {
                CreateConfig();

                UpdateConfig(config => config.SequencePositions = null);

                var p_act = new Program
                {
                    ConfigFile = configFile
                };

                // Act
                p_act.TryLoadConfigFile();

                // Assert
                Assert.AreEqual(2, p_act.CurrentConfig.SequencePositions.Length);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void TryLoadConfigFile_ShouldFixIncorrectSequencePositionsLength()
        {
            // Arrange
            try
            {
                CreateConfig();

                UpdateConfig(config => config.SequencePositions = new int[10]);

                var p_act = new Program
                {
                    ConfigFile = configFile
                };

                // Act
                p_act.TryLoadConfigFile();

                // Assert
                Assert.AreEqual(2, p_act.CurrentConfig.SequencePositions.Length);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void TryLoadConfigFile_ShouldFixSequencePositionsIfThereAreNoSequences()
        {
            // Arrange
            try
            {
                CreateConfig();

                UpdateConfig(config => config.Sequences = null);

                var p_act = new Program
                {
                    ConfigFile = configFile
                };

                // Act
                p_act.TryLoadConfigFile();

                // Assert
                Assert.IsNull(p_act.CurrentConfig.SequencePositions);
            }
            finally
            {
                DeleteConfig();
            }
        }
    }
}