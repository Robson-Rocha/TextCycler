using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TextCycler.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class ParseInitialTextTests : BaseTest
    {
        private List<string> _consoleWritten = null;
        private List<string> _historyAdded = null;

        private IConsole CreateConsoleMock(Func<string, string, string> readCallback = null, Action<Mock<IConsole>> mockConfig = null)
        {
            _consoleWritten = new List<string>();
            _historyAdded = new List<string>();
            var consoleMock = new Mock<IConsole>();
            consoleMock.SetupAllProperties();
            consoleMock.Setup(console => console.WriteLine(It.IsAny<string>()))
                       .Callback((string value) => _consoleWritten.Add(value));
            consoleMock.Setup(console => console.AddHistory(It.IsAny<string[]>()))
                       .Callback((string[] text) => _historyAdded.AddRange(text));
            consoleMock.Setup(console => console.Read(It.IsAny<string>(), It.IsAny<string>()))
                        .Returns((string prompt, string @default) => readCallback != null ? readCallback(prompt, @default) : @default);
            if (mockConfig != null)
            {
                mockConfig(consoleMock);
            }
            return consoleMock.Object;
        }

        [TestMethod]
        public void ParseInitialText_ShouldGetEnteredTextWhenPromptedForText()
        {
            try
            {
                // Arrange
                CreateConfig();
                string expected = "Lorem Ipsum";
                var p = new TextCycler(console: CreateConsoleMock((p, d) => expected))
                {
                    ConfigFile = configFile,
                    PromptForText = true
                };
                p.TryLoadConfigFile();

                // Act
                p.ParseInitialText();

                // Assert
                Assert.AreEqual(expected, p.Text);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void ParseInitialText_ShouldGetFirstTextWhenNotPromptedForTextAndNotSpecifiedTextIndex()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new TextCycler
                {
                    ConfigFile = configFile,
                };
                p.TryLoadConfigFile();

                // Act
                p.ParseInitialText();

                // Assert
                Assert.AreEqual(p.CurrentConfig.Texts[0], p.Text);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void ParseInitialText_ShouldGetSpecifiedTextWhenNotPromptedForTextAndSpecifiedTextIndex()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new TextCycler
                {
                    ConfigFile = configFile,
                    TextIndex = 3
                };
                p.TryLoadConfigFile();

                // Act
                p.ParseInitialText();

                // Assert
                Assert.AreEqual(p.CurrentConfig.Texts[3], p.Text);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void ParseInitialText_ShouldGetNextTextWhenNotPromptedForTextAndANextTextIndexExists()
        {
            try
            {
                // Arrange
                CreateConfig();

                UpdateConfig(config => config.NextTextIndex = 3);

                var p = new TextCycler
                {
                    ConfigFile = configFile
                };
                p.TryLoadConfigFile();

                // Act
                p.ParseInitialText();

                // Assert
                Assert.AreEqual(p.CurrentConfig.Texts[3], p.Text);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void ParseInitialText_ShouldListAllTextsAndOptionToEnterNewTextWhenMenu()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new TextCycler(console: CreateConsoleMock())
                {
                    ConfigFile = configFile,
                    Menu = true
                };
                p.TryLoadConfigFile();

                // Act
                p.ParseInitialText();

                // Assert
                int i = 0;
                for (; i < p.CurrentConfig.Texts.Length; i++)
                {
                    Assert.AreEqual($"[{i}] {p.CurrentConfig.Texts[i]}", _consoleWritten[i]);
                }
                Assert.AreEqual($"[#] [Enter a custom text]", _consoleWritten[i]);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void ParseInitialText_ShouldReturnSelectedTextWhenMenu()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new TextCycler(console: CreateConsoleMock(readCallback: null, mockConfig: mock =>
                    mock.Setup(c => c.Read("\r\nEnter the desired text index: ", It.IsAny<string>())).Returns("3")
                ))
                {
                    ConfigFile = configFile,
                    Menu = true
                };
                p.TryLoadConfigFile();

                // Act
                p.ParseInitialText();

                // Assert
                Assert.AreEqual(p.CurrentConfig.Texts[3], p.Text);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void ParseInitialText_ShouldReturnInputTextWhenSelectedLastOptionInMenu()
        {
            try
            {
                // Arrange
                CreateConfig();
                string expected = "Lorem Ipsum";
                var p = new TextCycler(console: CreateConsoleMock(readCallback: null, mockConfig: mock => {
                    mock.Setup(c => c.Read("\r\nEnter the desired text index: ", It.IsAny<string>())).Returns("#");
                    mock.Setup(c => c.Read("Enter your text: ", It.IsAny<string>())).Returns(expected);
                }))
                {
                    ConfigFile = configFile,
                    Menu = true
                };
                p.TryLoadConfigFile();

                // Act
                p.ParseInitialText();

                // Assert
                Assert.AreEqual(expected, p.Text);
            }
            finally
            {
                DeleteConfig();
            }
        }

        [TestMethod]
        public void ParseInitialText_ShouldAskAgainWhenInvalidIndexSelectedInMenu()
        {
            try
            {
                // Arrange
                CreateConfig();
                var p = new TextCycler(console: CreateConsoleMock(readCallback: null, mockConfig: mock => {
                    mock.Setup(c => c.Read("\r\nEnter the desired text index: ", It.IsAny<string>())).Returns("99");
                    mock.Setup(c => c.Read("\r\nEnter the desired text index: ", "99")).Returns("3");
                }))
                {
                    ConfigFile = configFile,
                    Menu = true
                };
                p.TryLoadConfigFile();
                string expected = p.CurrentConfig.Texts[3];

                // Act
                p.ParseInitialText();

                // Assert
                Assert.AreEqual(expected, p.Text);
            }
            finally
            {
                DeleteConfig();
            }
        }
    }
}
