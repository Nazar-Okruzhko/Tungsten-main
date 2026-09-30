using System;
using System.Collections.Concurrent;

namespace Tungsten.Core
{
    public enum LogLevel { Info, Warning, Error }

    public readonly struct LogEntry
    {
        public readonly LogLevel Level;
        public readonly string Message;
        public readonly DateTime Time;

        public LogEntry(LogLevel level, string message)
        {
            Level = level;
            Message = message;
            Time = DateTime.Now;
        }
    }

    /// <summary>
    /// Very small, allocation-light logging hub.
    /// Every engine subsystem (renderer, physics, gun system, vehicle system...)
    /// writes here instead of Console.WriteLine so that the Studio's "Output"
    /// panel can subscribe and show engine activity live, the same way
    /// Roblox Studio / Unity have an Output/Console window docked at the bottom.
    /// </summary>
    public static class Logger
    {
        // Bounded ring buffer so a runaway loop can't eat all your RAM logging.
        private const int MaxEntries = 2000;
        private static readonly ConcurrentQueue<LogEntry> _entries = new();

        public static event Action<LogEntry>? OnLog;

        public static void Info(string msg) => Write(LogLevel.Info, msg);
        public static void Warn(string msg) => Write(LogLevel.Warning, msg);
        public static void Error(string msg) => Write(LogLevel.Error, msg);

        private static void Write(LogLevel level, string msg)
        {
            var entry = new LogEntry(level, msg);
            _entries.Enqueue(entry);
            while (_entries.Count > MaxEntries) _entries.TryDequeue(out _);
            OnLog?.Invoke(entry);

            // Also mirror to the real console/debug output. This matters
            // early in boot (before the Output panel exists) and for any
            // GL/shader error that would otherwise be invisible if a bug
            // ever breaks the UI itself - exactly what happened with the
            // uTint type-mismatch bug, which silently zeroed every UI pixel
            // with no visible trace anywhere in the editor.
            if (level == LogLevel.Error) Console.Error.WriteLine($"[Tungsten:ERROR] {msg}");
            else if (level == LogLevel.Warning) Console.WriteLine($"[Tungsten:WARN] {msg}");
        }

        public static LogEntry[] Snapshot() => _entries.ToArray();
    }
}
