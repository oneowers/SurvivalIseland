// Path: Assets/Project/Scpripts/Ghosts/Configs/GhostSpawnerConfig.cs
// Purpose: Preserves compatibility with older GhostSpawnerConfig assets while delegating authored data to GhostSpawnConfig.
// Dependencies: GhostSpawnConfig, UnityEngine.

using UnityEngine;

namespace ProjectResonance.Ghosts
{
    /// <summary>
    /// Legacy config alias kept so existing asset GUID references continue to load in Unity.
    /// </summary>
    [CreateAssetMenu(fileName = "GhostSpawnerConfig", menuName = "Project Resonance/Ghosts/Ghost Spawner Config")]
    public sealed class GhostSpawnerConfig : GhostSpawnConfig
    {
    }
}
