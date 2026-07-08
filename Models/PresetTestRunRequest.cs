using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace z2d.Models
{
    public sealed class PresetTestRunRequest
    {
        public required IReadOnlyList<TargetItem> Targets { get; init; }
        public required IReadOnlyList<FileInfo> Presets { get; init; }

        public int TimeoutSeconds { get; init; } = 5;
        public int MaxParallelTargets { get; init; } = 8;
    }
}
