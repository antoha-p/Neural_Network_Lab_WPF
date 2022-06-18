﻿using System;
using System.Globalization;

namespace Qualia.Tools
{
    public static class Converter
    {
        public static long TicksToMicroseconds(long ticks)
        {
            return (long)(TimeSpan.FromTicks(ticks).TotalMilliseconds * 1000);
        }

        public static long? TextToInt(string text, long? defaultValue = null)
        {
            return string.IsNullOrEmpty(text) ? defaultValue : long.TryParse(text, out long a) ? a : defaultValue;
        }

        public static long TextToInt(string text, long defaultValue)
        {
            return string.IsNullOrEmpty(text) ? defaultValue : long.TryParse(text, out long a) ? a : defaultValue;
        }

        public static bool TryTextToInt(string text, out long result, long defaultValue)
        {
            if (string.IsNullOrEmpty(text))
            {
                result = defaultValue;
                return true;
            }

            if (long.TryParse(text, out long d))
            {
                result = d;
                return true;
            }

            result = 0;
            return false;
        }

        public static string IntToText(long? d)
        {
            return d.HasValue ? d.Value.ToString() : null;
        }

        public static double? TextToDouble(string text, double? defaultValue = null)
        {
            return string.IsNullOrEmpty(text) ? defaultValue : double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Culture.Current, out double a) ? a : defaultValue;
        }

        public static double TextToDouble(string text, double defaultValue)
        {
            return string.IsNullOrEmpty(text) ? defaultValue : double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Culture.Current, out double value) ? value : defaultValue;
        }

        public static bool TryTextToDouble(string text, out double result, double defaultValue)
        {
            result = defaultValue;

            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            if (double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Culture.Current, out double value))
            {
                result = value;
            }

            return true;
        }

        private static readonly char[] _0 = new[] { '0' };
        private static readonly char[] _S = new[] { Culture.Current.NumberFormat.NumberDecimalSeparator[0] };
        private static readonly string[] _postfixes = new[] {"", " K", " M", " B", " T" };

    public static string DoubleToText(double d, string format = "F20", bool trim = true)
        {
            var result = d.ToString(format, Culture.Current);
            if (trim && result.Contains(Culture.Current.NumberFormat.NumberDecimalSeparator))
            {
                result = result.TrimEnd(_0).TrimEnd(_S);
            }

            return result;
        }

        public static string RoundsToString(long rounds)
        {
            var s = rounds.ToString();

            int postfixId = 0;

            while (s.EndsWith("000") && postfixId < _postfixes.Length - 1)
            {
                s = s.Substring(0, s.Length - 3);
                ++postfixId;
            }

            if (s.Length > 3)
            {
                s = s.Insert(s.Length - 3, ".").TrimEnd(_0);
                postfixId += 1;
            }

            return s + _postfixes[postfixId];
        }
    }
}
