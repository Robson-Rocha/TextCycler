using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace TextCycler.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TryGenerateConfigFileTests : BaseTest
    {
        [TestMethod]
        public void TryGenerateConfigFile_ShouldCreateFileIfItDoesNotExists()
        {
            // Arrange
            try
            {
                var p = new TextCycler
                {
                    ConfigFile = configFile,
                    GenerateConfigFile = true
                };

                // Act
                var configGenerated = p.TryGenerateConfigFile();

                // Assert
                Assert.IsTrue(configGenerated);
                Assert.IsTrue(File.Exists(configFile));

            }
            finally
            {
                DeleteConfig(configFile);
            }
        }

        [TestMethod]
        public void TryGenerateConfigFile_ShouldNotCreateFileIfItDoesExist()
        {
            // Arrange
            try
            {
                File.WriteAllText(configFile, "{}");
                var p = new TextCycler
                {
                    ConfigFile = configFile,
                    GenerateConfigFile = true
                };

                // Act
                try
                {
                    _ = p.TryGenerateConfigFile();
                }
                catch (FailException fe)
                {
                    // Assert
                    Assert.IsTrue(fe.Message.Contains(configFile));
                    return;
                }
                Assert.Fail($"Should have catched {nameof(FailException)}");
            }
            finally
            {
                DeleteConfig(configFile);
            }
        }

        [TestMethod]
        public void TryGenerateConfigFile_ShouldNotCreateFileIfGenerateConfigFileOptionWasNotSet()
        {
            // Arrange
            try
            {
                var p = new TextCycler
                {
                    ConfigFile = configFile,
                    GenerateConfigFile = false
                };

                // Act
                bool configGenerated = p.TryGenerateConfigFile();

                // Assert
                Assert.IsFalse(configGenerated);
            }
            finally
            {
                DeleteConfig(configFile);
            }
        }
    }
}
