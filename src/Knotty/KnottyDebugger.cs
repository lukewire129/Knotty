using System;
using System.Collections.Generic;
using System.Diagnostics;
#if NET5_0_OR_GREATER
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
#endif

namespace Knotty.Core
{
    public static class KnottyDebugger
    {
        private static readonly List<LogEntry> _logs = new ();

        public static bool IsEnabled { get; set; } = false;

        public static IReadOnlyList<LogEntry> Logs => _logs;

#if NET5_0_OR_GREATER
        public record LogEntry(
            DateTime Timestamp,
            string StoreType,
            string IntentType,
            object? Intent,
            object? OldState,
            object? NewState
        );
#else
    public class LogEntry
    {
        public DateTime Timestamp { get; }
        public string StoreType { get; }
        public string IntentType { get; }
        public object? Intent { get; }
        public object? OldState { get; }
        public object? NewState { get; }

        public LogEntry(DateTime timestamp, string storeType, string intentType, 
            object? intent, object? oldState, object? newState)
        {
            Timestamp = timestamp;
            StoreType = storeType;
            IntentType = intentType;
            Intent = intent;
            OldState = oldState;
            NewState = newState;
        }
    }
#endif

        internal static void LogIntent(string storeType, object intent)
        {
            if (!IsEnabled)
                return;

            _logs.Add (new LogEntry (DateTime.Now, storeType, intent.GetType ().Name,
                intent, null, null));
            Debug.WriteLine ($"[Knotty] {storeType} <- {intent.GetType ().Name}");
        }

        internal static void LogStateChange(string storeType, object oldState, object newState)
        {
            if (!IsEnabled)
                return;

            _logs.Add (new LogEntry (DateTime.Now, storeType, "StateChanged",
                null, oldState, newState));
        }

        public static void Clear() => _logs.Clear ();
#if NET5_0_OR_GREATER
        public static async Task ExportToFileAsync(string path)
        {
            var json = JsonSerializer.Serialize (_logs, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync (path, json);
        }
#endif
    }
}
