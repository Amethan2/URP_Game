using UnityEngine;
using System.Collections.Generic; // Required for using List<>

/// <summary>
/// Stores the specific stats for a single level of a building.
/// Add any stat here that should change based on the building's upgrade level.
/// </summary>
[System.Serializable] // Makes this struct visible in the Unity Inspector
public struct BuildingLevelStats
{
    [Tooltip("The level these stats apply to (e.g., 1, 2, 3)")]
    public int level;

    [Header("Core Stats")]
    [Tooltip("Maximum unit capacity for this level")]
    public int maxCapacity;

    [Header("Resource & Spawning")]
    [Tooltip("Population growth per second (for House type)")]
    public float populationGrowthPerSecond;
    [Tooltip("Time in seconds to produce one unit (for Spawner types like Altar, Tower, Stable)")]
    public float spawnTime;
    [Tooltip("Mana generated per second (for Altar type)")]
    public float manaPerSecond;

    [Header("Combat Stats (if applicable)")]
    [Tooltip("Can the building attack at this level? (Relevant for Tower)")]
    public bool canAttack;
    [Tooltip("Attack damage (for Tower)")]
    public float attackDamage;
    [Tooltip("Attack range (for Tower)")]
    public float attackRange;
    [Tooltip("Time between attacks in seconds (for Tower)")]
    public float attackCooldown;
}

/// <summary>
/// ScriptableObject containing the definition and base data for a type of building.
/// This includes its visual representation, associated unit types, and stats for each upgrade level.
/// </summary>
[CreateAssetMenu(fileName = "BuildingData_New", menuName = "RTS Game/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Display name of the building (e.g., Nord Altar, Orden Tower)")]
    public string buildingName = "New Building";
    [Tooltip("The functional type of this building")]
    public BuildingType type;

    [Header("Visuals")]
    [Tooltip("Sprite displayed for this building on the map")]
    public Sprite buildingSprite;
    [Tooltip("Icon used for UI elements (menu slots, selection panels, etc.)")]
    public Sprite icon;

    [Header("Unit Types Associated")]
    [Tooltip("The type of unit representing 'population' (Used if Type == House)")]
    public UnitData populationUnitType; // Assign Peasant UnitData here for Houses
    [Tooltip("The type of unit this building produces (Used for Spawners: Altar, Tower, Stable)")]
    public UnitData unitToSpawn; // Assign Priest, Warrior, Rider etc. UnitData here

    // --- LIST OF STATS PER LEVEL ---
    [Header("Stats Per Level")]
    [Tooltip("Characteristics for each upgrade level (MUST create and configure elements for Level 1, 2, 3!)")]
    public List<BuildingLevelStats> levelStats = new List<BuildingLevelStats>(3);

    /// <summary>
    /// Retrieves the BuildingLevelStats struct for the specified level.
    /// If stats for the requested level aren't found, it attempts to return Level 1 stats as a fallback.
    /// Returns null if even Level 1 stats are missing (configuration error).
    /// </summary>
    /// <param name="level">The requested level (will be clamped to >= 1).</param>
    /// <returns>A BuildingLevelStats struct, or null if not found.</returns>
    public BuildingLevelStats? GetStatsForLevel(int level)
    {
        // Ensure the requested level is at least 1
        int validLevel = Mathf.Max(1, level);

        // Search for the stats matching the requested level
        foreach (var stats in levelStats)
        {
            if (stats.level == validLevel)
            {
                return stats; // Found exact match
            }
        }

        // Fallback: If exact level not found, try returning Level 1 stats
        if (validLevel > 1) // Only try fallback if we weren't looking for level 1 initially
        {
            Debug.LogWarning($"Stats for Level {validLevel} not found in BuildingData '{this.name}'. Attempting to return Level 1 stats.", this);
            foreach (var stats in levelStats)
            {
                if (stats.level == 1)
                {
                    return stats; // Found Level 1 stats
                }
            }
        }

        // Critical configuration error: Even Level 1 stats are missing
        Debug.LogError($"Stats for Level {validLevel} (and even Level 1) not found in BuildingData '{this.name}'! Please check the 'Level Stats' list in the Inspector.", this);
        return null; // Stats not found
    }

    // Вспомогательные методы для проверки корректности данных
    private void OnValidate()
    {
        // Проверяем, что есть хотя бы статы для уровня 1
        bool hasLevel1Stats = false;
        foreach (var stats in levelStats)
        {
            if (stats.level == 1)
            {
                hasLevel1Stats = true;
                break;
            }
        }

        if (!hasLevel1Stats)
        {
            Debug.LogWarning($"BuildingData '{name}': Отсутствуют статы для уровня 1!");
        }

        // Проверяем, что нет дубликатов уровней
        HashSet<int> levels = new HashSet<int>();
        foreach (var stats in levelStats)
        {
            if (!levels.Add(stats.level))
            {
                Debug.LogWarning($"BuildingData '{name}': Обнаружен дубликат для уровня {stats.level}!");
            }
        }

        // Проверяем корректность значений
        foreach (var stats in levelStats)
        {
            if (stats.maxCapacity <= 0)
                Debug.LogWarning($"BuildingData '{name}': Некорректная максимальная вместимость для уровня {stats.level}!");
            
            if (stats.populationGrowthPerSecond < 0)
                Debug.LogWarning($"BuildingData '{name}': Отрицательный прирост населения для уровня {stats.level}!");
            
            if (stats.spawnTime < 0)
                Debug.LogWarning($"BuildingData '{name}': Отрицательное время спавна для уровня {stats.level}!");
            
            if (stats.attackDamage < 0)
                Debug.LogWarning($"BuildingData '{name}': Отрицательный урон для уровня {stats.level}!");
            
            if (stats.attackRange < 0)
                Debug.LogWarning($"BuildingData '{name}': Отрицательная дальность атаки для уровня {stats.level}!");
            
            if (stats.attackCooldown < 0)
                Debug.LogWarning($"BuildingData '{name}': Отрицательная перезарядка для уровня {stats.level}!");
        }
    }
}