using System;
using System.Threading.Tasks;
using FluentAssertions;
using Knotty.Tests.TestHelpers;
using Xunit;

namespace Knotty.Tests;

public class KnottyStoreTests
{
    [Fact]
    public void Constructor_WithValidState_ShouldInitialize()
    {
        // Arrange & Act
        var store = new TestStore();

        // Assert
        store.State.Should().NotBeNull();
        store.State.Counter.Should().Be(0);
        store.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullState_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        Action act = () => new TestStore(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Dispatch_SyncIntent_ShouldUpdateState()
    {
        // Arrange
        var store = new TestStore();

        // Act
        await store.DispatchAsync(new TestIntent.Increment());

        // Assert
        store.State.Counter.Should().Be(1);
    }

    [Fact]
    public async Task Dispatch_MultipleIntents_ShouldUpdateStateCorrectly()
    {
        // Arrange
        var store = new TestStore();

        // Act
        await store.DispatchAsync(new TestIntent.Increment());
        await store.DispatchAsync(new TestIntent.Increment());
        await store.DispatchAsync(new TestIntent.Decrement());

        // Assert
        store.State.Counter.Should().Be(1);
    }

    [Fact]
    public async Task Dispatch_AsyncIntent_ShouldSetIsLoadingDuringExecution()
    {
        // Arrange
        var store = new TestStore();
        var wasLoading = false;

        store.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(store.IsLoading) && store.IsLoading)
            {
                wasLoading = true;
            }
        };

        // Act
        await store.DispatchAsync(new TestIntent.AsyncIncrement(50));

        // Assert
        wasLoading.Should().BeTrue();
        store.IsLoading.Should().BeFalse();
        store.State.Counter.Should().Be(1);
    }

    [Fact]
    public void State_Change_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var store = new TestStore();
        var eventRaised = false;

        store.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(store.State))
            {
                eventRaised = true;
            }
        };

        // Act
        store.Dispatch(new TestIntent.Increment());

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void State_Change_ShouldRaisePropertyChangingEvent()
    {
        // Arrange
        var store = new TestStore();
        var eventRaised = false;

        store.PropertyChanging += (sender, args) =>
        {
            if (args.PropertyName == nameof(store.State))
            {
                eventRaised = true;
            }
        };

        // Act
        store.Dispatch(new TestIntent.Increment());

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldUnsubscribeFromBus()
    {
        // Arrange
        var store = new TestStore();

        // Act
        store.Dispose();

        // Assert - No exception should be thrown
        store.Dispose(); // Calling again should be safe
    }
}
