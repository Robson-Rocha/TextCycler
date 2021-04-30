using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;

namespace TextCycler.Tests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class ValidateOptionsTests : BaseTest
    {
        [TestMethod]
        public void ValidateOptions_ShouldFailWhenNotPromptedForTextAndNoTextsAreDefined()
        {
            // Arrange
            try
            {
                CreateConfig();

                UpdateConfig(config => config.Texts = null);

                var p_act = new TextCycler
                {
                    ConfigFile = configFile
                };
                p_act.TryLoadConfigFile();

                try 
                {
                    // Act
                    p_act.ValidateOptions();
                }
                catch (FailException fe)
                {
                    // Assert
                    Assert.IsTrue(fe.Message.Contains("At least one text must be defined"));
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
        public void ValidateOptions_ShouldFailWhenNotPromptedForTextAndTextIndexIsGreaterThanNumberOfTexts()
        {
            // Arrange
            try
            {
                CreateConfig();

                var p_act = new TextCycler
                {
                    ConfigFile = configFile,
                    TextIndex = 10
                };
                p_act.TryLoadConfigFile();

                try
                {
                    // Act
                    p_act.ValidateOptions();
                }
                catch (FailException fe)
                {
                    // Assert
                    Assert.IsTrue(fe.Message.Contains($"As you have {p_act.CurrentConfig.Texts.Length} texts defined"));
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
        public void ValidateOptions_ShouldFailWhenPromptedForTextAndMenuIsSupplied()
        {
            // Arrange
            try
            {
                CreateConfig();

                var p_act = new TextCycler
                {
                    ConfigFile = configFile,
                    PromptForText = true,
                    Menu = true
                };
                p_act.TryLoadConfigFile();

                try
                {
                    // Act
                    p_act.ValidateOptions();
                }
                catch (FailException fe)
                {
                    // Assert
                    Assert.IsTrue(fe.Message.Contains("You can't use the '-p' and '-m' options at the same time."));
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
        public void ValidateOptions_ShouldFailWhenSequenceValuesAreProvidedAndThereAreNoSequences()
        {
            // Arrange
            try
            {
                CreateConfig();

                UpdateConfig(config => config.Sequences = null);

                var p_act = new TextCycler
                {
                    ConfigFile = configFile,
                    SequenceValues = new[] { "0,lorem" },
                    Menu = true
                };
                p_act.TryLoadConfigFile();

                try
                {
                    // Act
                    p_act.ValidateOptions();
                }
                catch (FailException fe)
                {
                    // Assert
                    Assert.IsTrue(fe.Message.Contains("There are no sequences declared"));
                    return;
                }
                Assert.Fail($"Should have catched {nameof(FailException)}");
            }
            finally
            {
                DeleteConfig();
            }
        }
    }
}
