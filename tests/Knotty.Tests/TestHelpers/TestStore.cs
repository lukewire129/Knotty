using System.Threading;
using System.Threading.Tasks;
using Knotty.Core;

namespace Knotty.Tests.TestHelpers;

public record TestState(int Counter = 0, string Message = "");

public abstract record TestIntent
{
    public record Increment : TestIntent;
    public record Decrement : TestIntent;
    public record SetMessage(string Message) : TestIntent;
    public record AsyncIncrement(int Delay = 100) : TestIntent;
}

public class TestStore : KnottyStore<TestState, TestIntent>
{
    public TestStore() : base(new TestState()) { }

    public TestStore(TestState initialState) : base(initialState) { }

    protected override async Task HandleIntent(TestIntent intent, CancellationToken ct = default)
    {
        switch (intent)
        {
            case TestIntent.Increment:
                State = State with { Counter = State.Counter + 1 };
                break;

            case TestIntent.Decrement:
                State = State with { Counter = State.Counter - 1 };
                break;

            case TestIntent.SetMessage msg:
                State = State with { Message = msg.Message };
                break;

            case TestIntent.AsyncIncrement async:
                await Task.Delay(async.Delay, ct);
                State = State with { Counter = State.Counter + 1 };
                break;
        }
    }
}
