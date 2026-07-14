using System;

namespace AetherNexus.UIWidgets
{
    /// <summary>
    /// Serializable min/max value pair constrained to an outer limit range.
    /// Backing data for <see cref="MinMaxSlider"/>.
    /// </summary>
    [Serializable]
    public struct MinMaxValues
    {
        /// <summary>Tolerance used when comparing slider values for equality.</summary>
        public const float Tolerance = 0.01f;

        public float minValue;
        public float maxValue;
        public float minLimit;
        public float maxLimit;

        public static readonly MinMaxValues Default = new MinMaxValues(25f, 75f, 0f, 100f);

        public MinMaxValues(float minValue, float maxValue, float minLimit, float maxLimit)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.minLimit = minLimit;
            this.maxLimit = maxLimit;
        }

        /// <summary>Creates a range whose limits coincide with its values.</summary>
        public MinMaxValues(float minValue, float maxValue)
            : this(minValue, maxValue, minValue, maxValue)
        {
        }

        /// <summary>True when both values sit on their respective limits.</summary>
        public bool IsAtMinAndMax()
        {
            return Math.Abs(minValue - minLimit) < Tolerance
                && Math.Abs(maxValue - maxLimit) < Tolerance;
        }

        public override string ToString()
        {
            return $"MinMaxValues [{minValue}..{maxValue}] within [{minLimit}..{maxLimit}]";
        }
    }
}
