using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace z2d.Models
{
    public sealed class SelectableItem<T> : INotifyPropertyChanged
    {
        private bool _isSelected;

        public T Value { get; }
        public string DisplayName { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SelectableItem(T value, string displayName, bool isSelected = true)
        {
            Value = value;
            DisplayName = displayName;
            IsSelected = isSelected;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
