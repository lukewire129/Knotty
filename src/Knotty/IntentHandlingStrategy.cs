namespace Knotty.Core;

public enum IntentHandlingStrategy
{
    Block,           // 현재 방식 (진행 중이면 무시)
    Queue,           // 큐에 쌓아서 순서대로 처리
    Debounce,        // 일정 시간 대기 후 마지막 것만
    CancelPrevious,  // 이전 작업 취소하고 새 것 시작
    Parallel         // 동시 처리 (IsLoading 체크 안 함)
}