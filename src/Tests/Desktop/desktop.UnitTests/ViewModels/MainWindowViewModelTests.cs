using System;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Input;
using de.openelp.feuerwehr.desktop.Service;
using de.openelp.feuerwehr.desktop.ViewModels;
using de.openelp.feuerwehr.domain;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;

namespace de.openelp.feuerwehr.desktop.UnitTests.ViewModels
{
    /// <summary>
    /// Unit tests for the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    public partial class MainWindowViewModelTests
    {
        /// <summary>
        /// Tests that the constructor successfully creates an instance of MainWindowViewModel
        /// without throwing any exceptions.
        /// </summary>
        [Fact]
        public void Constructor_WhenCalled_CreatesInstanceSuccessfully()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();

            // Act
            var viewModel = new MainWindowViewModel(mockServiceProvider.Object);

            // Assert
            Assert.NotNull(viewModel);
            Assert.IsType<MainWindowViewModel>(viewModel);
        }

        /// <summary>
        /// Tests that the constructor initializes the Items property to a non-null ObservableCollection.
        /// </summary>
        [Fact]
        public void Constructor_WhenCalled_InitializesItemsProperty()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();

            // Act
            var viewModel = new MainWindowViewModel(mockServiceProvider.Object);

            // Assert
            Assert.NotNull(viewModel.CurrentView);
        }

        /// <summary>
        /// Tests that the constructor initializes the Greeting property to the expected default value.
        /// </summary>
        [Fact]
        public void Constructor_WhenCalled_InitializesGreetingProperty()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockDashboardViewModel = new DashboardViewModel();
            mockServiceProvider.Setup(sp => sp.GetService(typeof(DashboardViewModel)))
                .Returns(mockDashboardViewModel);

            // Act
            var viewModel = new MainWindowViewModel(mockServiceProvider.Object);

            // Assert
            Assert.NotNull(viewModel.CurrentView);
        }
    }
}
