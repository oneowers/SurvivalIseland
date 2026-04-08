// Path: Assets/Project/Scpripts/Common/Random/UnityRandomProvider.cs
// Purpose: Default random provider backed by UnityEngine.Random.
// Dependencies: ProjectResonance.Common.Random, UnityEngine.

using UnityEngine;

namespace ProjectResonance.Common.Random
{
    /// <summary>
    /// Supplies random values from Unity's runtime random source.
    /// </summary>
    public sealed class UnityRandomProvider : IRandomProvider
    {
        /// <summary>
        /// Returns a random float in the inclusive range.
        /// </summary>
        /// <param name="minInclusive">Minimum value.</param>
        /// <param name="maxInclusive">Maximum value.</param>
        /// <returns>Random float value.</returns>
        public float Range(float minInclusive, float maxInclusive)
        {
            return UnityEngine.Random.Range(minInclusive, maxInclusive);
        }

        /// <summary>
        /// Returns a random point inside a unit circle.
        /// </summary>
        /// <returns>Random point inside a unit circle.</returns>
        public Vector2 InsideUnitCircle()
        {
            return UnityEngine.Random.insideUnitCircle;
        }
    }
}
