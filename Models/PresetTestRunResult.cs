using System;
using System.Collections.Generic;
using System.Text;

namespace z2d.Models
{
    public sealed class PresetTestRunResult
    {
        public required IReadOnlyList<PresetTestResult> PresetResults { get; init; }

        public PresetTestResult? BestPreset => PresetResults
            .Where(x => !x.LaunchFailed)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();
    }
}
