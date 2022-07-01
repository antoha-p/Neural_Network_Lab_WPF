﻿using Qualia.Tools;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Qualia.Controls
{
    sealed public class IntValueControl : TextBox, IConfigParam
    {
        private Config _config;
        private event Action<Notification.ParameterChanged> _onChanged = delegate { };

        public long DefaultValue { get; set; } = Constants.InvalidLong;

        public IntValueControl Initialize(long? defaultValue = null, long? minValue = null, long? maxValue = null)
        {
            if (defaultValue.HasValue)
            {
                DefaultValue = defaultValue.Value;
            }

            if (minValue.HasValue)
            {
                MinimumValue = minValue.Value;
            }

            if (maxValue.HasValue)
            {
                MaximumValue = maxValue.Value;
            }

            return this;
        }

        public long MinimumValue { get; set; } = long.MinValue;

        public long MaximumValue { get; set; } = long.MaxValue;

        public Notification.ParameterChanged UIParam { get; private set; }

        public IntValueControl SetUIParam(Notification.ParameterChanged param)
        {
            UIParam = param;
            return this;
        }

        public IntValueControl()
        {
            Padding = new(0);
            Margin = new(3);
            MinWidth = 60;

            TextChanged += OnValueChanged;
        }

        private void OnValueChanged(object sender, EventArgs e)
        {
            if (IsValid())
            {
                Background = Brushes.White;
                _onChanged(UIParam);
            }
            else
            {
                Background = Brushes.Tomato;
            }
        }

        public bool IsValid()
        {
            if (IsNull())
            {
                return false;
            }

            long value = Converter.TextToInt(Text, DefaultValue);
            return value >= MinimumValue && value <= MaximumValue;
        }

        public bool IsNull() => string.IsNullOrEmpty(Text) && Constants.IsInvalid(DefaultValue);

        public long Value
        {
            get
            {
                return IsValid()
                       ? (IsNull() ? throw new InvalidValueException(Name, "null") : Converter.TextToInt(Text, DefaultValue))
                       : throw new InvalidValueException(Name, Text);
            }

            set
            {
                Text = Converter.IntToText(value);
                OnValueChanged(null, null);
            }
        }

        public void SetConfig(Config config)
        {
            _config = config;
        }

        public void LoadConfig()
        {
            var value = _config.Get(Name, DefaultValue);

            if (value < MinimumValue)
            {
                value = MinimumValue;
            }

            if (value > MaximumValue)
            {
                value = MaximumValue;
            }

            Value = value;
        }

        public void SaveConfig()
        {
            _config.Set(Name, Value);
        }

        public void RemoveFromConfig()
        {
            _config.Remove(Name);
        }

        public void SetOnChangeEvent(Action<Notification.ParameterChanged> onChanged)
        {
            _onChanged -= onChanged;
            _onChanged += onChanged;
        }

        public void InvalidateValue()
        {
            Background = Brushes.Tomato;
        }

        public string ToXml()
        {
            string name = Config.PrepareParamName(Name);
            return $"<{name} Value=\"{Value}\" /> \n";
        }
    }
}