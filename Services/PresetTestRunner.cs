using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using z2d.Models;

namespace z2d.Services
{
    public sealed class PresetTestRunner
    {
        private const string ExeDirectoryName = "exe";
        private const string ExecutableName = "winws2.exe";
        private const int StartupDelayMilliseconds = 4000;

        private readonly string _baseDir;
        private readonly TargetTestService _targetTestService = new();

        public PresetTestRunner(string baseDir)
        {
            if (string.IsNullOrWhiteSpace(baseDir))
                throw new ArgumentException("Base directory cannot be null or empty.", nameof(baseDir));

            _baseDir = baseDir;
        }

        public async Task<PresetTestRunResult> RunAsync(
            PresetTestRunRequest request,
            IProgress<PresetTestProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var presetResults = new List<PresetTestResult>();

            var totalTargetRuns = request.Presets.Count * request.Targets.Count;
            var completedTargetRuns = 0;
            var presetIndex = 0;

            foreach (var preset in request.Presets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                presetIndex++;

                var presetName = Path.GetFileNameWithoutExtension(preset.Name);

                progress?.Report(new PresetTestProgress
                { 
                    CurrentPresetName = presetName,
                    PresetIndex = presetIndex,
                    PresetCount = request.Presets.Count,
                    CompletedTargetRuns = completedTargetRuns,
                    TotalTargetRuns = totalTargetRuns
                });

                WinwsProcessService.Stop();
                await Task.Delay(500, cancellationToken);

                Process? process = null;

                try 
                {
                    var exePath = Path.Combine(_baseDir, ExeDirectoryName, ExecutableName);
                    var arguments = await WinwsArgsBuilder.BuildArgumentsAsync(preset.FullName);

                    process = await WinwsProcessService.StartAsync(
                        exePath,
                        arguments,
                        _baseDir,
                        createNoWindow: false,
                        cancellationToken);

                    await Task.Delay(StartupDelayMilliseconds, cancellationToken);

                    if (!IsWinws2Running())
                    {
                        presetResults.Add(new PresetTestResult
                        {
                            PresetName = presetName,
                            LaunchFailed = true,
                            LaunchError = "winws2 did not start.",
                            TargetResults = Array.Empty<TargetTestResult>()
                        });

                        completedTargetRuns += request.Targets.Count;

                        progress?.Report(new PresetTestProgress
                        {
                            CurrentPresetName = presetName,
                            PresetIndex = presetIndex,
                            PresetCount = request.Presets.Count,
                            CompletedTargetRuns = completedTargetRuns,
                            TotalTargetRuns = totalTargetRuns
                        });

                        continue;
                    }

                    var targetResults = await TestTargetsAsync(
                        request.Targets,
                        request.TimeoutSeconds,
                        request.MaxParallelTargets,
                        () =>
                        {
                            completedTargetRuns++;

                            progress?.Report(new PresetTestProgress
                            {
                                CurrentPresetName = presetName,
                                PresetIndex = presetIndex,
                                PresetCount = request.Presets.Count,
                                CompletedTargetRuns = completedTargetRuns,
                                TotalTargetRuns = totalTargetRuns
                            });
                        },
                        cancellationToken);

                    presetResults.Add(new PresetTestResult
                    {
                        PresetName = presetName,
                        LaunchFailed = false,
                        LaunchError = null,
                        TargetResults = targetResults
                    });
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    presetResults.Add(new PresetTestResult
                    {
                        PresetName = presetName,
                        LaunchFailed = true,
                        LaunchError = ex.Message,
                        TargetResults = Array.Empty<TargetTestResult>()
                    });

                    completedTargetRuns += request.Targets.Count;

                    progress?.Report(new PresetTestProgress
                    {
                        CurrentPresetName = presetName,
                        PresetIndex = presetIndex,
                        PresetCount = request.Presets.Count,
                        CompletedTargetRuns = completedTargetRuns,
                        TotalTargetRuns = totalTargetRuns
                    });
                }
                finally
                {
                    try
                    {
                        WinwsProcessService.Stop();

                        if (process is { HasExited: false })
                        {
                            process.Kill(true);
                        }

                        process?.Dispose();
                    }
                    catch
                    {
                    }
                }
            }

            return new PresetTestRunResult
            {
                PresetResults = presetResults
            };
        }

        private async Task<IReadOnlyList<TargetTestResult>> TestTargetsAsync(
            IReadOnlyList<TargetItem> targets,
            int timeoutSeconds,
            int maxParallelTargets,
            Action onTargetCompleted,
            CancellationToken cancellationToken)
        {
            if (targets.Count == 0)
            {
                return Array.Empty<TargetTestResult>();
            }

            var actualMaxParallel = Math.Max(1, maxParallelTargets);
            using var semaphore = new SemaphoreSlim(actualMaxParallel);

            var tasks = targets.Select(target =>
                TestTargetWithLimitAsync(
                    target,
                    timeoutSeconds,
                    semaphore,
                    onTargetCompleted,
                    cancellationToken));

            return await Task.WhenAll(tasks);
        }

        private async Task<TargetTestResult> TestTargetWithLimitAsync(
            TargetItem target,
            int timeoutSeconds,
            SemaphoreSlim semaphore,
            Action onTargetCompleted,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                return await _targetTestService.TestAsync(
                    target,
                    timeoutSeconds,
                    cancellationToken);
            }
            finally
            {
                semaphore.Release();
                onTargetCompleted();
            }
        }

        private static bool IsWinws2Running()
        {
            return Process.GetProcessesByName("winws2").Length > 0;
        }
    }
}
