using System;
using System.Collections.Generic;
using System.Text;

namespace z2d.Models
{
    public sealed class PresetTestResult
    {
        public required string PresetName { get; init; }

        public bool LaunchFailed { get; init; }
        public string? LaunchError { get; init; }

        public required IReadOnlyList<TargetTestResult> TargetResults { get; init; }

        public int SuccessCount => TargetResults.Count(x => x.IsSuccessful);

        public int FailedCount => TargetResults.Count(x => !x.IsSuccessful);

        public int Score => SuccessCount * 10;
    }
}
