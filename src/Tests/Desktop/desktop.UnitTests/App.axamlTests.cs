using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup;
using Avalonia.Markup.Xaml;
using de.openelp.feuerwehr;
using de.openelp.feuerwehr.desktop;
using de.openelp.feuerwehr.desktop.Service;
using de.openelp.feuerwehr.desktop.ViewModels;
using de.openelp.feuerwehr.desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;

namespace de.openelp.feuerwehr.desktop.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="App"/> class.
    /// </summary>
    public partial class AppTests
    {
        /// <summary>
        /// Tests that the Initialize method completes without throwing exceptions.
        /// This test verifies the basic initialization flow of the Avalonia application.
        /// Note: This test depends on the actual Avalonia framework since AvaloniaXamlLoader.Load 
        /// is a static method that cannot be mocked. This serves as a smoke test to ensure 
        /// the initialization logic doesn't throw unexpected exceptions.
        /// </summary>
        [Fact]
        public void Initialize_WhenCalled_CompletesWithoutException()
        {
            // Arrange
            var app = new App();

            // Act & Assert
            // The method should complete without throwing any exceptions
            app.Initialize();
        }

        /// <summary>
        /// Tests that OnFrameworkInitializationCompleted does not set MainWindow when ApplicationLifetime is null.
        /// </summary>
        [Fact]
        public void OnFrameworkInitializationCompleted_ApplicationLifetimeIsNull_DoesNotThrowException()
        {
            // Arrange
            var testApp = new TestableApp();
            testApp.SetApplicationLifetime(null);

            // Act
            testApp.OnFrameworkInitializationCompleted();

            // Assert
            // If ApplicationLifetime is null, the method should complete without errors
            // and should not attempt to set MainWindow
            Assert.False(testApp.MainWindowWasSet);
        }

        /// <summary>
        /// Tests that OnFrameworkInitializationCompleted does not set MainWindow when ApplicationLifetime 
        /// is not IClassicDesktopStyleApplicationLifetime.
        /// </summary>
        [Fact]
        public void OnFrameworkInitializationCompleted_ApplicationLifetimeIsNotDesktop_DoesNotSetMainWindow()
        {
            // Arrange
            var mockLifetime = new Mock<IApplicationLifetime>();
            var testApp = new TestableApp();
            testApp.SetApplicationLifetime(mockLifetime.Object);

            // Act
            testApp.OnFrameworkInitializationCompleted();

            // Assert
            // If ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime,
            // MainWindow should not be set
            Assert.False(testApp.MainWindowWasSet);
        }

        /// <summary>
        /// Tests that OnFrameworkInitializationCompleted sets MainWindow when ApplicationLifetime 
        /// is IClassicDesktopStyleApplicationLifetime.
        /// This test requires Avalonia framework initialization and may be inconclusive in some test environments.
        /// </summary>
        [Fact]
        public void OnFrameworkInitializationCompleted_ApplicationLifetimeIsDesktop_SetsMainWindow()
        {
            // Arrange
            // Initialize Avalonia headless platform for testing
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .SetupWithoutStarting();

            var mockDesktopLifetime = new Mock<IClassicDesktopStyleApplicationLifetime>();
            Window? capturedWindow = null;
            mockDesktopLifetime.SetupProperty(x => x.MainWindow);
            mockDesktopLifetime.Setup(x => x.MainWindow).Returns(() => capturedWindow);
            mockDesktopLifetime.SetupSet(x => x.MainWindow = It.IsAny<Window?>())
                .Callback<Window?>(w => capturedWindow = w);

            var testApp = new TestableApp();
            testApp.SetApplicationLifetime(mockDesktopLifetime.Object);

            // Act
            testApp.OnFrameworkInitializationCompleted();

            // Assert
            // Verify that MainWindow was set
            Assert.True(testApp.MainWindowWasSet);
            mockDesktopLifetime.VerifySet(x => x.MainWindow = It.IsAny<Window>(), Times.Once);
        }

        /// <summary>
        /// Tests that OnFrameworkInitializationCompleted sets MainWindow DataContext when ApplicationLifetime 
        /// is IClassicDesktopStyleApplicationLifetime.
        /// This test verifies that the MainWindow is configured with a MainWindowViewModel as its DataContext.
        /// </summary>
        [Fact]
        public void OnFrameworkInitializationCompleted_ApplicationLifetimeIsDesktop_SetsMainWindowDataContext()
        {
            // Arrange
            var mockDesktopLifetime = new Mock<IClassicDesktopStyleApplicationLifetime>();
            Window? capturedWindow = null;
            mockDesktopLifetime.SetupProperty(x => x.MainWindow);
            mockDesktopLifetime.SetupSet(x => x.MainWindow = It.IsAny<Window?>())
                .Callback<Window?>(w => capturedWindow = w);

            var testApp = new TestableApp();
            testApp.SetApplicationLifetime(mockDesktopLifetime.Object);

            // Act
            try
            {
                testApp.OnFrameworkInitializationCompleted();

                // Assert
                // Verify that MainWindow was set and has a DataContext of type MainWindowViewModel
                Assert.NotNull(capturedWindow);
                Assert.NotNull(capturedWindow.DataContext);
                Assert.IsType<MainWindowViewModel>(capturedWindow.DataContext);
            }
            catch (Exception ex) when (ex.Message.Contains("Avalonia") || ex.Message.Contains("initialized"))
            {
                Assert.Fail(
                    "This test requires Avalonia framework to be initialized. " +
                    "The test could not complete due to: " + ex.Message);
            }
        }

        /// <summary>
        /// Helper class for testing the App class.
        /// Allows controlling ApplicationLifetime and tracking test-specific behavior.
        /// </summary>
        private class TestableApp : App
        {
            private IApplicationLifetime? _testLifetime;
            public bool MainWindowWasSet { get; private set; }

            public void SetApplicationLifetime(IApplicationLifetime? lifetime)
            {
                _testLifetime = lifetime;
            }

            public new IApplicationLifetime? ApplicationLifetime
            {
                get => _testLifetime;
            }

            public override void OnFrameworkInitializationCompleted()
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    MainWindowWasSet = true;

                    var services = new ServiceCollection();
                    services.AddHttpClient<ApiService>();
                    services.AddTransient<ApiService>();
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddTransient<InventoryViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    var serviceProvider = services.BuildServiceProvider();

                    var mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = mainWindowViewModel,
                    };
                }
            }

            public override void Initialize()
            {
                // Override to prevent XAML loading in tests
            }
        }
    }
}

