﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Qualia.Tools
{
    public interface IConfigParam
    {
        void SetConfig(Config config);
        void LoadConfig();
        void SaveConfig();
        void VanishConfig();
        bool IsValid();
        void SetChangeEvent(Action action);
        void InvalidateValue();
    }

    sealed public class Config
    {
        public static Config Main = new("config.txt");
        public readonly string Name;
        public Config ParentConfig;

        private static object s_locker = new();
        
        private string _extender;

        private static readonly Dictionary<string, Dictionary<string, string>> s_cache = new();

        public Config(string fileName, Config parentConfig = null)
        {
            Name = fileName;
            ParentConfig = parentConfig;
        }

        private string CutName(string name)
        {
            return name.StartsWith("Ctl", StringComparison.InvariantCultureIgnoreCase) ? name.Substring(3) : name;
        }

        public Config Extend(long extender)
        {
            Config config = new(Name)
            {
                ParentConfig = this,
                _extender = "." + extender
            };

            return config;
        }

        public string GetString(Constants.Param paramName, string defaultValue = null)
        {
            return GetValue(paramName, defaultValue);
        }

        public string GetString(string paramName, string defaultValue = null)
        {
            return GetValue(paramName, defaultValue);
        }

        public double? GetDouble(Constants.Param paramName, double? defaultValue = null)
        {
            if (Converter.TryTextToDouble(GetValue(paramName, Converter.DoubleToText(defaultValue)), out double? value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        public double? GetDouble(string paramName, double? defaultValue = null)
        {
            if (Converter.TryTextToDouble(GetValue(paramName, Converter.DoubleToText(defaultValue)), out double? value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        public long? GetInt(Constants.Param paramName, long? defaultValue = null)
        {
            if (Converter.TryTextToInt(GetValue(paramName, Converter.IntToText(defaultValue)), out long? value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        public long? GetInt(string paramName, long? defaultValue = null)
        {
            if (Converter.TryTextToInt(GetValue(paramName, Converter.IntToText(defaultValue)), out long? value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        public bool GetBool(Constants.Param paramName, bool defaultValue = false)
        {
            return 1 == GetInt(paramName, defaultValue ? 1 : 0);
        }

        public bool GetBool(string paramName, bool defaultValue = false)
        {
            return 1 == GetInt(paramName, defaultValue ? 1 : 0);
        }

        public long[] GetArray(Constants.Param paramName, string defaultValue = null)
        {
            if (defaultValue == null)
            {
                defaultValue = string.Empty;
            }

            string value = GetValue(paramName, defaultValue);
            return string.IsNullOrEmpty(value) ? Array.Empty<long>() : value.Split(new[] { ',' }).Select(s => long.Parse(s.Trim())).ToArray();
        }

        public long[] GetArray(string paramName, string defaultValue = null)
        {
            if (defaultValue == null)
            {
                defaultValue = string.Empty;
            }

            string value = GetValue(paramName, defaultValue);
            return string.IsNullOrEmpty(value) ? Array.Empty<long>() : value.Split(new[] { ',' }).Select(s => long.Parse(s.Trim())).ToArray();
        }

        public void Remove(Constants.Param paramName)
        {
            var values = GetValues();
            if (values.TryGetValue(paramName.ToString("G") + _extender, out _))
            {
                values.Remove(paramName.ToString("G") + _extender);
            }

            SaveValues(values);
        }

        public void Remove(string paramName)
        {
            var values = GetValues();
            if (values.TryGetValue(paramName + _extender, out _))
            {
                values.Remove(paramName + _extender);
            }

            SaveValues(values);
        }

        public void Set(Constants.Param paramName, string value)
        {
            var values = GetValues();
            values[paramName.ToString("G") + _extender] = value;

            SaveValues(values);
        }

        public void Set(string paramName, string value)
        {
            paramName = CutName(paramName);

            var values = GetValues();
            values[paramName + _extender] = value;

            SaveValues(values);
        }

        public void Set(Constants.Param paramName, double? value)
        {
            Set(paramName, Converter.DoubleToText(value));
        }

        public void Set(string paramName, double? value)
        {
            Set(paramName, Converter.DoubleToText(value));
        }

        public void Set(Constants.Param paramName, long? value)
        {
            Set(paramName, Converter.IntToText(value));
        }

        public void Set(string paramName, long? value)
        {
            Set(paramName, Converter.IntToText(value));
        }

        public void Set(Constants.Param paramName, bool value)
        {
            Set(paramName, value ? 1 : 0);
        }

        public void Set(string paramName, bool value)
        {
            Set(paramName, value ? 1 : 0);
        }

        public void Set<T>(Constants.Param paramName, IEnumerable<T> list)
        {
            Set(paramName, string.Join(",", list.Select(l => l.ToString())));
        }

        public void Set<T>(string paramName, IEnumerable<T> list)
        {
            Set(paramName, string.Join(",", list.Select(l => l.ToString())));
        }

        private string GetValue(Constants.Param paramName, string defaultValue = null)
        {
            var values = GetValues();

            if (values.TryGetValue(paramName.ToString("G") + _extender, out string value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        private string GetValue(string paramName, string defaultValue = null)
        {
            paramName = CutName(paramName);

            var values = GetValues();

            if (values.TryGetValue(paramName + _extender, out string value))
            {
                return value;
            }
            else
            {
                Set(paramName, defaultValue);
                return defaultValue;
            }
        }

        private void SaveValues(Dictionary<string, string> values)
        {
            if (s_cache.ContainsKey(Name))
            {
                s_cache[Name] = values;
            }
            else
            {
                s_cache.Add(Name, values);
            }
        }

        public void FlushToDrive()
        {
            if (!s_cache.ContainsKey(Name))
            {
                return;
            }

            List<string> lines = new();

            var values = s_cache[Name];
            foreach (var pair in values)
            {
                lines.Add(pair.Key + ":" + pair.Value);
            }

            lock (s_locker)
            {
                File.WriteAllLines(Name, lines);
            }
        }

        private Dictionary<string, string> GetValues()
        {
            if (s_cache.ContainsKey(Name))
            {
                return s_cache[Name];
            }

            Dictionary<string, string> result = new();

            if (!File.Exists(Name))
            {
                Clear();
            }

            var lines = File.ReadAllLines(Name);

            foreach (var line in lines)
            {
                if (line.Contains(":"))
                {
                    var parts = line.Split(new[] { ':' });
                    if (parts.Length > 1)
                    {
                        result[parts[0]] = string.Join(":", parts.Except(parts.Take(1)));
                    }
                }
            }

            s_cache.Add(Name, result);

            return result;
        }

        public void Clear()
        {
            lock (s_locker)
            {
                File.WriteAllLines(Name, Array.Empty<string>());
            }

            s_cache.Clear();
        }
    }
}