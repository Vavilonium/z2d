using System;
using System.Collections.Generic;
using System.Text;

namespace z2d.Models
{
    public sealed class SelectableItem<T>
    {
        public T Value { get; }
        public string DisplayName { get; }
        public bool IsSelected { get; set; }

        public SelectableItem(T value, string displayName, bool isSelected = true)
        {
            Value = value;
            DisplayName = displayName;
            IsSelected = isSelected;
        }
    }
}
