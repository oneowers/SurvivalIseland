// Path: Assets/Project/Scpripts/Common/Random/IRandomProvider.cs
// Purpose: Provides an injectable random source for gameplay systems.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.Common.Random
{
    /// <summary>
    /// Provides random values for gameplay systems through dependency injection.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>
        /// Returns a random float in the inclusive range.
        /// </summary>
        /// <param name="minInclusive">Minimum value.</param>
        /// <param name="maxInclusive">Maximum value.</param>
        /// <returns>Random float value.</returns>
        float Range(float minInclusive, float maxInclusive);

        /// <summary>
        /// Returns a random point inside a unit circle.
        /// </summary>
        /// <returns>Random point inside a unit circle.</returns>
        Vector2 InsideUnitCircle();
    }
}
