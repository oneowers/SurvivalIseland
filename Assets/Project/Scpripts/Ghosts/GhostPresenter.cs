// Path: Assets/Project/Scpripts/Ghosts/GhostPresenter.cs
// Purpose: Preserves compatibility with older Pale Drift prefab references while delegating behavior to the new PaleDrift implementation.
// Dependencies: PaleDrift, UnityEngine.

using UnityEngine;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// Legacy Pale Drift alias kept to avoid breaking existing prefab script references.
    /// </summary>
    [AddComponentMenu("Project Resonance/Ghosts/Ghost Presenter (Legacy)")]
    public sealed class GhostPresenter : PaleDrift
    {
    }
}
