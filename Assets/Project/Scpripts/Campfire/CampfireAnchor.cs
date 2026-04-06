// Path: Assets/Project/Scpripts/Campfire/CampfireAnchor.cs
// Purpose: Holds scene references used by the campfire runtime system.
// Dependencies: UnityEngine.

using UnityEngine;

namespace ProjectResonance.Campfire
{
    /// <summary>
    /// Scene anchor for campfire visuals and world position.
    /// </summary>
    public sealed class CampfireAnchor : MonoBehaviour
    {
        [SerializeField]
        private Transform _firePoint;

        [SerializeField]
        private Light _campfireLight;

        [SerializeField]
        private ParticleSystem _embers;

        /// <summary>
        /// Gets the world transform used as the campfire origin.
        /// </summary>
        public Transform FirePoint => _firePoint != null ? _firePoint : transform;

        /// <summary>
        /// Applies the current runtime visual state to the campfire.
        /// </summary>
        /// <param name="isLit">Whether the campfire is lit.</param>
        /// <param name="fuelNormalized">Fuel amount normalized to [0..1].</param>
        /// <param name="minIntensity">Minimum visual light intensity.</param>
        /// <param name="maxIntensity">Maximum visual light intensity.</param>
        public void ApplyState(bool isLit, float fuelNormalized, float minIntensity, float maxIntensity)
        {
            if (_campfireLight != null)
            {
                _campfireLight.enabled = isLit;
                _campfireLight.intensity = isLit
                    ? Mathf.Lerp(minIntensity, maxIntensity, Mathf.Clamp01(fuelNormalized))
                    : 0f;
            }

            if (_embers == null)
            {
                return;
            }

            if (isLit)
            {
                if (!_embers.isPlaying)
                {
                    _embers.Play();
                }
            }
            else if (_embers.isPlaying)
            {
                _embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
