using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace TextCycler.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TrySetTargetFileTests : BaseTest
    {
        [TestMethod]
        public void TrySetTargetFile_ShouldFailIfNoTargetWasSet()
        {
            try
            {
                // Arrange
                CreateConfig();
                UpdateConfig(config =>
                {
                    config.TargetFile = null;
                });
                var p = new Program
                {
                    ConfigFile = configFile,
                    TargetFile = null
                };
                p.TryLoadConfigFile();

                try
                {
                    // Act
                    p.TrySetTargetFile();
                }
                catch (FailException fe)
                {
                    // Assert
                    Assert.IsTrue(fe.Message.Contains("The target file was not set"));
                    return;
                }
                Assert.Fail($"Should have catched {nameof(FailException)}");
            }
            finally
            {
                DeleteConfig();
                DeleteTarget();
            }
        }

        [TestMethod]
        public void TrySetTargetFile_ShouldWriteEmptyTargetFileIfItNotExists()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new Program
                {
                    ConfigFile = configFile,
                };
                p.TryLoadConfigFile();

                // Act
                p.TrySetTargetFile();

                // Assert
                Assert.IsTrue(File.Exists(p.TargetFile));
                Assert.AreEqual("", File.ReadAllText(p.TargetFile));
            }
            finally
            {
                DeleteConfig();
                DeleteTarget();
            }
        }

        [TestMethod]
        public void TrySetTargetFile_ShouldPreserveTargetFileIfItExists()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new Program
                {
                    ConfigFile = configFile,
                };
                p.TryLoadConfigFile();
                File.WriteAllText(p.CurrentConfig.TargetFile, "lorem ipsum");

                // Act
                p.TrySetTargetFile();

                // Assert
                Assert.IsTrue(File.Exists(p.TargetFile));
                Assert.AreEqual("lorem ipsum", File.ReadAllText(p.TargetFile));
            }
            finally
            {
                DeleteConfig();
                DeleteTarget();
            }
        }

        [TestMethod]
        public void TrySetTargetFile_ShouldFailIfTargetFileCannotBeWritten()
        {
            try
            {
                // Arrange
                CreateConfig();

                var fileMock = new Mock<IFile>();
                fileMock.Setup(file => file.Exists(It.IsAny<string>()))
                        .Returns((string path) => File.Exists(path));
                fileMock.Setup(file => file.Exists(targetFile))
                        .Returns(false);
                fileMock.Setup(file => file.WriteAllText(targetFile, ""))
                        .Throws<UnauthorizedAccessException>();

                var p = new Program(file: fileMock.Object)
                {
                    ConfigFile = configFile,
                };
                p.TryLoadConfigFile();

                try
                {
                    // Act
                    p.TrySetTargetFile();
                }
                catch (FailException fe)
                {
                    // Assert
                    Assert.IsTrue(fe.Message.Contains("cannot be created"));
                    return;
                }
                Assert.Fail($"Should have catched {nameof(FailException)}");
            }
            finally
            {
                DeleteConfig();
                DeleteTarget();
            }
        }
    }
}
