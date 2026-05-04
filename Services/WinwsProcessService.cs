using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace z2d.Services
{
    public static class WinwsProcessService
    {
        public static async Task<Process?> StartAsync(
            string exePath,
            string arguments,
            string workingDirectory,
            bool createNoWindow,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentException("Executable path cannot be null or empty.", nameof(exePath));

            if (!File.Exists(exePath))
                throw new FileNotFoundException("The specified executable was not found.", exePath);

            if (string.IsNullOrWhiteSpace(workingDirectory))
                throw new ArgumentException("Working directory cannot be null or empty.", nameof(workingDirectory));

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = createNoWindow,
                    WorkingDirectory = workingDirectory
                };

                return Process.Start(startInfo);
            }, cancellationToken);
        }

        public static void Stop()
        {
            var processes = Process.GetProcesses()
                .Where(p => p.ProcessName.StartsWith("winws", StringComparison.OrdinalIgnoreCase));

            foreach (var process in processes)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {

                }
            }
        }
    }
}
