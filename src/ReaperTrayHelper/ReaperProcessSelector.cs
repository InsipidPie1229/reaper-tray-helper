using System;
using System.Collections.Generic;
using System.Linq;

namespace ReaperTrayHelper
{
    internal sealed class RunningReaper
    {
        internal RunningReaper(int processId, string executablePath, bool pathWasRead)
        {
            ProcessId = processId;
            ExecutablePath = executablePath;
            PathWasRead = pathWasRead;
        }

        internal int ProcessId { get; private set; }
        internal string ExecutablePath { get; private set; }
        internal bool PathWasRead { get; private set; }
    }

    internal enum ReaperSelectionKind
    {
        LaunchConfiguredProcess,
        AttachToConfiguredProcess
    }

    internal sealed class ReaperSelection
    {
        internal ReaperSelection(ReaperSelectionKind kind, int processId)
        {
            Kind = kind;
            ProcessId = processId;
        }

        internal ReaperSelectionKind Kind { get; private set; }
        internal int ProcessId { get; private set; }
    }

    internal static class ReaperProcessSelector
    {
        internal static ReaperSelection Select(IEnumerable<RunningReaper> runningProcesses, string configuredPath)
        {
            string configuredFullPath = System.IO.Path.GetFullPath(configuredPath);
            var processes = runningProcesses.ToList();

            if (processes.Any(process => !process.PathWasRead))
            {
                throw new InvalidOperationException(UiText.Get("process_path_unreadable"));
            }

            var matches = processes
                .Where(process => String.Equals(
                    System.IO.Path.GetFullPath(process.ExecutablePath),
                    configuredFullPath,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count > 1)
            {
                throw new InvalidOperationException(UiText.Get("multiple_processes"));
            }

            if (matches.Count == 1)
            {
                return new ReaperSelection(ReaperSelectionKind.AttachToConfiguredProcess, matches[0].ProcessId);
            }

            return new ReaperSelection(ReaperSelectionKind.LaunchConfiguredProcess, 0);
        }
    }
}
