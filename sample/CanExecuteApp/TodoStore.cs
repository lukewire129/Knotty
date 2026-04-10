using Knotty;
using System.Collections.Immutable;

namespace CanExecuteApp;

public record TodoState(
    ImmutableArray<string> Items,
    bool IsExporting,
    string Status
);

public record TodoIntent
{
    public record Add(string Text) : TodoIntent;
    public record ClearAll : TodoIntent;
    public record Export : TodoIntent;
}

public partial class TodoStore : KnottyStore<TodoState, TodoIntent>
{
    // ── CanExecute 패턴 1: 필드 + bool 프로퍼티 ──────────────────────────────────
    // 수정된 버그 케이스: generator가 () => CanClearAll 람다를 emit한다.
    [IntentCommand(CanExecute = nameof(CanClearAll))]
    private readonly TodoIntent.ClearAll _clearAll = new();

    private bool CanClearAll => State.Items.Length > 0;

    // ── CanExecute 패턴 2: 파라미터 있는 메서드 + 파라미터 있는 CanExecute ─────────
    // generator가 CanAdd를 그대로 직접 전달한다: Command<string>(CreateAdd, CanAdd)
    [IntentCommand(CanExecute = nameof(CanAdd))]
    private TodoIntent.Add CreateAdd(string text) => new(text);

    private bool CanAdd(string text) => !string.IsNullOrWhiteSpace(text);

    // ── CanExecute 패턴 3: 필드 + 파라미터 없는 메서드 ───────────────────────────
    // generator가 메서드 참조를 그대로 전달한다: AsyncCommand(_export, CanExport)
    [AsyncIntentCommand(CanExecute = nameof(CanExport))]
    private readonly TodoIntent.Export _export = new();

    private bool CanExport() => !IsLoading && State.Items.Length > 0;

    public TodoStore() : base(new TodoState(ImmutableArray<string>.Empty, false, "항목을 추가하세요.")) { }

    protected override async Task HandleIntent(TodoIntent intent, CancellationToken ct)
    {
        switch (intent)
        {
            case TodoIntent.Add(var text):
                State = State with
                {
                    Items = State.Items.Add(text.Trim()),
                    Status = $"'{text.Trim()}' 추가됨"
                };
                break;

            case TodoIntent.ClearAll:
                State = State with
                {
                    Items = ImmutableArray<string>.Empty,
                    Status = "모두 삭제됨"
                };
                break;

            case TodoIntent.Export:
                State = State with { Status = "내보내는 중..." };
                await Task.Delay(1500, ct);
                State = State with { Status = $"{State.Items.Length}개 항목 내보내기 완료" };
                break;
        }
    }
}
