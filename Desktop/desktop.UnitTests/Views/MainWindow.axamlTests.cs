using Avalonia.Controls;
using de.openelp.feuerwehr.desktop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace de.openelp.feuerwehr.desktop.Views.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="MainWindow"/> class.
    /// </summary>
    [TestClass]
    public partial class MainWindowTests
    {
        /// <summary>
        /// Tests that the MainWindow constructor successfully creates an instance.
        /// Verifies that the constructor executes without throwing and produces a valid instance.
        /// Note: This test may require Avalonia application context to be initialized.
        /// If this test fails with initialization errors, Avalonia's AppBuilder may need to be configured.
        /// </summary>
        [TestMethod]
        public void MainWindow_Constructor_CreatesValidInstance()
        {
            // Arrange & Act
            MainWindow? result = null;
            Exception? exception = null;
            try
            {
                result = new MainWindow();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Assert
            Assert.IsNull(exception, "Constructor should not throw an exception.");
            Assert.IsNotNull(result, "MainWindow instance should be created.");
            Assert.IsInstanceOfType<MainWindow>(result, "Instance should be of type MainWindow.");
            Assert.IsInstanceOfType<Window>(result, "Instance should inherit from Avalonia Window.");
        }
    }

    /// <summary>
    /// Helper class to capture exceptions during test execution.
    /// </summary>
    internal static class Record
    {
        /// <summary>
        /// Executes the provided action and captures any exception that occurs.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>The exception that was thrown, or null if no exception occurred.</returns>
        public static System.Exception? Exception(System.Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (System.Exception ex)
            {
                return ex;
            }
        }
    }
}