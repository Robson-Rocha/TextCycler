using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace TextCycler.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class WriteExceptionFileTests
    {
        [TestMethod]
        public void WriteExceptionFile_ShouldCreateDifferentFilePaths()
        {
            // Arrange
            var paths = new string[2];
            var ex = new Exception("This is a test");
            try
            {
                // Act
                paths[0] = Program.WriteExceptionFile(ex);
                paths[1] = Program.WriteExceptionFile(ex);

                // Assert
                Assert.IsTrue(File.Exists(paths[0]));
                Assert.IsTrue(File.Exists(paths[1]));
                Assert.AreNotEqual(paths[0], paths[1]);
            }
            finally
            {
                for (int i = 0; i <= 1; i++)
                {
                    if (File.Exists(paths[i]))
                    {
                        File.Delete(paths[i]);
                    }
                }
            }
        }
    }
}
