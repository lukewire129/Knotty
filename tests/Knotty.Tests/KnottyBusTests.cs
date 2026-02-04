using System;
using FluentAssertions;
using Knotty.Core;
using Xunit;

namespace Knotty.Tests;

public class KnottyBusTests
{
    public abstract record BusTestIntent
    {
        public record TestMessage(string Content) : BusTestIntent;
    }

    [Fact]
    public void Subscribe_ShouldReceiveMessages()
    {
        // Arrange
        var recipient = new object();
        string? receivedContent = null;

        using var subscription = KnottyBus.Subscribe<BusTestIntent.TestMessage>(
            recipient,
            intent => receivedContent = intent.Content
        );

        // Act
        KnottyBus.Send(new BusTestIntent.TestMessage("Hello"));

        // Assert
        receivedContent.Should().Be("Hello");
    }

    [Fact]
    public void Unsubscribe_ShouldStopReceivingMessages()
    {
        // Arrange
        var recipient = new object();
        var count = 0;

        var subscription = KnottyBus.Subscribe<BusTestIntent.TestMessage>(
            recipient,
            intent => count++
        );

        KnottyBus.Send(new BusTestIntent.TestMessage("Test1"));
        count.Should().Be(1);

        // Act
        subscription.Dispose();
        KnottyBus.Send(new BusTestIntent.TestMessage("Test2"));

        // Assert
        count.Should().Be(1); // Should not increase
    }

    [Fact]
    public void Send_WithNullIntent_ShouldNotThrow()
    {
        // Act & Assert
        Action act = () => KnottyBus.Send<BusTestIntent.TestMessage>(null!);
        act.Should().NotThrow();
    }

    [Fact]
    public void Subscribe_MultipleRecipients_ShouldAllReceive()
    {
        // Arrange
        var recipient1 = new object();
        var recipient2 = new object();
        var count1 = 0;
        var count2 = 0;

        using var sub1 = KnottyBus.Subscribe<BusTestIntent.TestMessage>(
            recipient1,
            intent => count1++
        );

        using var sub2 = KnottyBus.Subscribe<BusTestIntent.TestMessage>(
            recipient2,
            intent => count2++
        );

        // Act
        KnottyBus.Send(new BusTestIntent.TestMessage("Broadcast"));

        // Assert
        count1.Should().Be(1);
        count2.Should().Be(1);
    }
}
