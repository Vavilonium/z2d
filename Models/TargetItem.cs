using System;
using System.Collections.Generic;
using System.Text;

namespace z2d.Models
{
    public sealed class TargetItem
    {
        public required string Name { get; init; }
        public required string Target {  get; init; }
        public required TargetType Type { get; init; }

        public override string ToString()
        {
            return $"{Name} ({Target})";
        }
    }
}
