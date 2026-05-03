using de.openelp.feuerwehr.mobile.ViewModels;
using Xunit;

namespace de.openelp.feuerwehr.mobile.UnitTests.ViewModels;

/// <summary>
/// Unit tests for the MainViewModel class.
/// </summary>
public class MainViewModelTests
{
    /// <summary>
    /// Tests that MainViewModel initializes with the default greeting message.
    /// Input: Create a new instance of MainViewModel.
    /// Expected: The Greeting property contains "Welcome to Avalonia!".
    /// </summary>
    [Fact]
    public void MainViewModel_WhenCreated_HasDefaultGreeting()
    {
        // Arrange & Act
        var viewModel = new MainViewModel();

        // Assert
        Assert.Equal("Welcome to Avalonia!", viewModel.Greeting);
    }

    /// <summary>
    /// Tests that the Greeting property can be set and retrieved.
    /// Input: Set a new value for the Greeting property.
    /// Expected: The property returns the newly set value.
    /// </summary>
    [Fact]
    public void Greeting_WhenSet_ReturnsNewValue()
    {
        // Arrange
        var viewModel = new MainViewModel();
        var newGreeting = "Hallo Avalonia!";

        // Act
        viewModel.Greeting = newGreeting;

        // Assert
        Assert.Equal(newGreeting, viewModel.Greeting);
    }

    /// <summary>
    /// Tests that the Greeting property can be set to an empty string.
    /// Input: Set the Greeting property to an empty string.
    /// Expected: The property accepts and returns an empty string.
    /// </summary>
    [Fact]
    public void Greeting_WhenSetToEmpty_ReturnsEmptyString()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act
        viewModel.Greeting = string.Empty;

        // Assert
        Assert.Empty(viewModel.Greeting);
    }

    /// <summary>
    /// Tests that the Greeting property can be set to null.
    /// Input: Set the Greeting property to null.
    /// Expected: The property accepts null.
    /// </summary>
    [Fact]
    public void Greeting_WhenSetToNull_ReturnsNull()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        viewModel.Greeting = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        Assert.Null(viewModel.Greeting);
    }

    /// <summary>
    /// Tests that the Greeting property supports binding by raising PropertyChanged.
    /// Input: Set the Greeting property and verify that PropertyChanged event is raised.
    /// Expected: The PropertyChanged event is triggered with the correct property name.
    /// </summary>
    [Fact]
    public void Greeting_WhenChanged_RaisesPropertyChangedEvent()
    {
        // Arrange
        var viewModel = new MainViewModel();
        var propertyChangedRaised = false;
        var changedProperty = string.Empty;

        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.Greeting))
            {
                propertyChangedRaised = true;
                changedProperty = args.PropertyName;
            }
        };

        // Act
        viewModel.Greeting = "New Greeting";

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(nameof(MainViewModel.Greeting), changedProperty);
    }

    /// <summary>
    /// Tests that multiple sequential changes to the Greeting property work correctly.
    /// Input: Change the Greeting property multiple times.
    /// Expected: The final value is correct and each change preserves data integrity.
    /// </summary>
    [Theory]
    [InlineData("First Greeting")]
    [InlineData("Second Greeting")]
    [InlineData("Third Greeting")]
    public void Greeting_WhenChangedMultipleTimes_MaintainsCorrectValue(string expectedGreeting)
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act
        viewModel.Greeting = "First Greeting";
        viewModel.Greeting = "Second Greeting";
        viewModel.Greeting = expectedGreeting;

        // Assert
        Assert.Equal(expectedGreeting, viewModel.Greeting);
    }
}
