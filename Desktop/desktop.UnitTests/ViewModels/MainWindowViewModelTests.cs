using System;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Input;
using de.openelp.feuerwehr.desktop.ViewModels;
using de.openelp.feuerwehr.domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace de.openelp.feuerwehr.desktop.ViewModels.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    [TestClass]
    public partial class MainWindowViewModelTests
    {
        /// <summary>
        /// Tests that the constructor successfully creates an instance of MainWindowViewModel
        /// without throwing any exceptions.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalled_CreatesInstanceSuccessfully()
        {
            // Arrange & Act
            var viewModel = new MainWindowViewModel();

            // Assert
            Assert.IsNotNull(viewModel);
            Assert.IsInstanceOfType(viewModel, typeof(MainWindowViewModel));
        }

        /// <summary>
        /// Tests that the constructor initializes the Items property to a non-null ObservableCollection.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalled_InitializesItemsProperty()
        {
            // Arrange & Act
            var viewModel = new MainWindowViewModel();

            // Assert
            Assert.IsNotNull(viewModel.Items);
        }

        /// <summary>
        /// Tests that the constructor initializes the Greeting property to the expected default value.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenCalled_InitializesGreetingProperty()
        {
            // Arrange & Act
            var viewModel = new MainWindowViewModel();

            // Assert
            Assert.AreEqual("Welcome to Avalonia!", viewModel.Greeting);
        }

        /// <summary>
        /// Tests that LoadItems executes without throwing an exception when called.
        /// This test verifies the method can be invoked successfully, initiating the fire-and-forget async operation.
        /// Note: Due to the fire-and-forget pattern and lack of dependency injection for HttpClient,
        /// comprehensive testing of the async behavior and HTTP operations requires design changes.
        /// </summary>
        [TestMethod]
        public void LoadItems_WhenCalled_DoesNotThrow()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();

            // Act & Assert
            viewModel.LoadItems();
            // The method uses fire-and-forget pattern, so we can only verify it doesn't throw immediately
        }

        /// <summary>
        /// Tests that LoadItems can be called multiple times without throwing an exception.
        /// This verifies the method is safe to invoke repeatedly, which is important for UI scenarios
        /// where users might trigger the load operation multiple times.
        /// </summary>
        [TestMethod]
        public void LoadItems_WhenCalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();

            // Act & Assert
            viewModel.LoadItems();
            viewModel.LoadItems();
            viewModel.LoadItems();
            // The method should handle multiple invocations without throwing
        }

        /// <summary>
        /// Tests that LoadItems does not immediately modify the Items collection.
        /// Since LoadItems uses a fire-and-forget async pattern, the Items collection
        /// should not be modified synchronously when the method returns.
        /// </summary>
        [TestMethod]
        public void LoadItems_WhenCalled_DoesNotImmediatelyModifyItems()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            int initialCount = viewModel.Items.Count;

            // Act
            viewModel.LoadItems();

            // Assert
            Assert.AreEqual(initialCount, viewModel.Items.Count, "Items collection should not be modified immediately due to fire-and-forget async pattern.");
        }
    }
}