using System;
using System.Collections.Generic;
using System.Text;

namespace z2d.Models
{
    public sealed class PresetTestProgress
    {
        public required string CurrentPresetName { get; init; }

        public int PresetIndex { get; init; }
        public int PresetCount { get; init; }

        public int CompletedTargetRuns { get; init; }
        public int TotalTargetRuns { get; init; }

        public double Percent =>
            TotalTargetRuns == 0
                ? 0
                : CompletedTargetRuns * 100.0 / TotalTargetRuns;
    }
}
