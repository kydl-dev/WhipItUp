using System;
using Kydl.Common;
using Kydl.Core.Commands;
using Kydl.Core.Time;
using WhipItUp.Integrations.Valheim;

namespace WhipItUp.Bootstrap
{
    /// <summary>
    /// The shared services every feature gets (spec "DependencyRegistry"): built once by <see cref="ModInstaller"/>. Pure layers receive what
    /// they need from here through their constructors (trace sink, clock, command dispatcher) instead of reaching for statics.
    /// </summary>
    internal sealed class DependencyRegistry
    {
        /// The trace sink of pure code (MIGRATION_DECISIONS B10): ModLog.Verbose (written only while [DebugLog] Enable is on).
        internal Action<string> Trace { get; }
        /// Game time for pure code (Time.time).
        internal IClock Clock { get; }
        /// Routes the requests a player / UI / timer makes to their handlers.
        internal CommandDispatcher Commands { get; }

        internal DependencyRegistry()
        {
            Trace = line => ModLog.Verbose(line);
            Clock = new GameClock();
            Commands = new CommandDispatcher(Trace);
        }
    }
}
