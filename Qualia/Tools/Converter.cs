﻿using System;
using System.Globalization;

namespace Qualia.Tools
{
    public static class Converter
    {
        private static readonly char[] _0 = new[] { '0' };
        private static readonly char[] _S = new[] { Culture.Current.NumberFormat.NumberDecimalSeparator[0] };
        private static readonly string[] _postfixes = new[] { "", " K", " M", " B", " T" };

        public static long TicksToMicroseconds(long ticks)
        {
            return (long)(TimeSpan.FromTicks(ticks).TotalMilliseconds * 1000);
        }

        public static long TextToInt(string text, long defaultValue)
        {
            bool valid = long.TryParse(text, out long value);
            
            if (valid)
            {
                return value;
            }

            if (Constants.IsInvalid(defaultValue))
            {
                throw new InvalidValueException("long", "NaN");
            }

            return defaultValue;
        }

        public static string IntToText(long value)
        {
            if (Constants.IsInvalid(value))
            {
                throw new InvalidValueException("long", "NaN");
            }

            return value.ToString(Culture.Current);
        }

        public static double TextToDouble(string text, double defaultValue)
        {
            bool valid = double.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Culture.Current, out double value);

            if (valid)
            {
                return value;
            }

            if (double.IsNaN(defaultValue))
            {
                throw new InvalidValueException("double", "NaN");
            }

            return defaultValue;
        }

        public static string DoubleToText(double value, string format = "F20", bool trim = true)
        {
            if (double.IsNaN(value))
            {
                throw new InvalidValueException("double", "NaN");
            }

            var result = value.ToString(format == "auto" ? "F50" : format, Culture.Current);
            if (trim && result.Contains(Culture.Current.NumberFormat.NumberDecimalSeparator))
            {
                result = result.TrimEnd(_0).TrimEnd(_S);
            }

            if (format == "auto")
            {
                int ind = result.IndexOf(Culture.Current.NumberFormat.NumberDecimalSeparator[0]);
                if (ind > 0)
                {
                    ++ind;
                    while (result[ind++] == '0')
                    {
                        //
                    }
                    result = result.Substring(0, ind);
                }
            }

            return result;
        }

        public static string RoundsToString(long rounds)
        {
            var s = IntToText(rounds);

            int postfixId = 0;

            while (s.EndsWith("000", true, Culture.Current) && postfixId < _postfixes.Length - 1)
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

        public static double TruncateDouble(double value, int fractionLength)
        {
            int factor = 1;
            for (int i = 0; i < fractionLength; ++i)
            {
                factor *= 10;
            }

            return Math.Round(value * factor) / factor;
        }
    }
}
