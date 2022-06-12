﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Qualia.Tools
{
    public static class Rand
    {
        public static Random Flat = new((int)(DateTime.UtcNow.Ticks % int.MaxValue));
        public static GaussianRandom GaussianRand = new(Flat);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetFlatRandom(double upperBound = 1)
        {
            return upperBound * Flat.NextDouble();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetSpreadRandom(double lowerBound, double upperBound)
        {
            return -lowerBound + upperBound * Flat.NextDouble();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetSpreadInRange(double range)
        {
            return -range / 2 + range * Flat.NextDouble();
        }
    }

    sealed public class GaussianRandom
    {
        private bool _hasDeviate;
        private double _storedDeviate;
        private readonly Random _random;

        public GaussianRandom(Random random = null)
        {
            _random = random ?? new((int)(DateTime.UtcNow.Ticks % int.MaxValue));
        }

        /// <summary>
        /// Obtains normally (Gaussian) distributed random numbers, using the Box-Muller
        /// transformation.  This transformation takes two uniformly distributed deviates
        /// within the unit circle, and transforms them into two independently
        /// distributed normal deviates.
        /// </summary>
        /// <param name="mu">The mean of the distribution.  Default is zero.</param>
        /// <param name="sigma">The standard deviation of the distribution.  Default is one.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double NextGaussian(double mu = 0, double sigma = 1)
        {
            if (sigma <= 0)
            {
                throw new ArgumentOutOfRangeException("sigma", "Must be greater than zero.");
            }

            if (_hasDeviate)
            {
                _hasDeviate = false;
                return _storedDeviate * sigma + mu;
            }

            double v1, v2, squared;
            do
            {
                // two random values between -1.0 and 1.0
                v1 = 2 * _random.NextDouble() - 1;
                v2 = 2 * _random.NextDouble() - 1;
                squared = v1 * v1 + v2 * v2;
                // ensure within the unit circle
            }
            while (squared >= 1 || squared == 0);

            // calculate polar tranformation for each deviate
            var polar = Math.Sqrt(-2 * Math.Log(squared) / squared);
            // store first deviate
            _storedDeviate = v2 * polar;
            _hasDeviate = true;

            // return second deviate
            return v1 * polar * sigma + mu;
        }
    }

    public static class UniqId
    {
        private static long s_prevId;

        public static long GetNextId(long existingId)
        {
            if (existingId > Constants.UnknownId)
            {
                return existingId;
            }

            long id;
            do
            {
                id = DateTime.UtcNow.Ticks;
                Thread.Sleep(0);
            }
            while (id <= s_prevId);

            s_prevId = id;
            return id;
        }
    }
}
