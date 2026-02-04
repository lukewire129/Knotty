using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Knotty.Core;
using Xunit;

namespace Knotty.Tests;

public class ErrorHandlingTests
{
    public record ErrorTestState(int Value = 0);

    public abstract record ErrorIntent
    {
        public record ThrowException(string Message) : ErrorIntent;
        public record Success : ErrorIntent;
    }

    public class ErrorTestStore : KnottyStore<ErrorTestState, ErrorIntent>
    {
        public Exception? LastError { get; private set; }

        public ErrorTestStore() : base(new ErrorTestState()) { }

        protected override async Task HandleIntent(ErrorIntent intent, CancellationToken ct = default)
        {
            await Task.Yield();

            switch (intent)
            {
                case ErrorIntent.ThrowException ex:
                    throw new InvalidOperationException(ex.Message);

                case ErrorIntent.Success:
                    State = State with { Value = State.Value + 1 };
                    break;
            }
        }

        protected override void OnHandleError(Exception ex)
        {
            LastError = ex;
        }
    }

    [Fact]
    public async Task HandleIntent_WhenExceptionThrown_ShouldCaptureError()
    {
        // Arrange
        var store = new ErrorTestStore();

        // Act
        await store.DispatchAsync(new ErrorIntent.ThrowException("Test error"));

        // Assert
        store.HasErrors.Should().BeTrue();
        var errors = store.GetErrors("Store")?.Cast<string>().ToList();
        errors.Should().NotBeNull();
        errors.Should().Contain("Test error");
    }

    [Fact]
    public async Task HandleIntent_WhenExceptionThrown_ShouldCallOnHandleError()
    {
        // Arrange
        var store = new ErrorTestStore();

        // Act
        await store.DispatchAsync(new ErrorIntent.ThrowException("Test error"));

        // Assert
        store.LastError.Should().NotBeNull();
        store.LastError!.Message.Should().Be("Test error");
    }

    [Fact]
    public async Task HandleIntent_AfterError_ShouldStillWork()
    {
        // Arrange
        var store = new ErrorTestStore();

        // Act
        await store.DispatchAsync(new ErrorIntent.ThrowException("Error"));
        await store.DispatchAsync(new ErrorIntent.Success());

        // Assert
        store.State.Value.Should().Be(1);
    }

    [Fact]
    public async Task HandleIntent_Success_ShouldClearPreviousErrors()
    {
        // Arrange
        var store = new ErrorTestStore();

        // Act
        await store.DispatchAsync(new ErrorIntent.ThrowException("Error"));
        store.HasErrors.Should().BeTrue();

        await store.DispatchAsync(new ErrorIntent.Success());

        // Assert
        store.HasErrors.Should().BeFalse();
    }
}
