using UnityEngine;
using UnityEngine.UI;          // Required for UI elements like Button, Image, Slider
using UnityEngine.SceneManagement; // Required for loading scenes
using TMPro;                  // Required for TextMeshPro components (Text, Font Assets)
using System.Collections.Generic; // Required for Lists
using System;                   // Required for Enum, Action
using System.Linq;              // Required for FindObjectsOfType, FirstOrDefault etc.
using System.Reflection;      // Required for accessing private fields (alternative to making them public)
using System.Collections;     // Required for Coroutines (IEnumerator)

// --- STRUCTURE FOR MAPPING BUILDING TYPE TO PREFAB ---
// Used in the Inspector to link BuildingType enum values to their corresponding prefabs.
[System.Serializable]
public struct BuildingPrefabMapping
{
    [Tooltip("The type of building.")]
    public BuildingType type;
    [Tooltip("The prefab GameObject for this building type. Must have a Building script attached.")]
    public GameObject prefab;
}
// -------------------------------------------------

/// <summary>
/// Manages the main game state within the playable scene.
/// Responsible for initializing the player's chosen race, spawning/configuring buildings
/// based on menu progression, and potentially managing game start/end conditions.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton Pattern

    // Provides easy global access to the GameManager instance
    public static GameManager Instance { get; private set; }

    #endregion

    #region Public Properties

    // Stores the race selected by the player in the main menu
    public Race PlayerRace { get; private set; } = Race.Neutral;

    #endregion

    #region Serialized Fields (Inspector References)

    [Header("Building Configuration")]
    [Tooltip("Link each BuildingType to its corresponding Prefab here.")]
    [SerializeField] private List<BuildingPrefabMapping> buildingPrefabs = new List<BuildingPrefabMapping>();

    // --- TODO: Reference to your Building Data Source ---
    // How will GameManager find the correct BuildingData ScriptableObject for a given Race and Type?
    // Option 1: A ScriptableObject containing all BuildingDatas
    // [SerializeField] private AllBuildingDataSO allBuildingDataContainer;
    // Option 2: Reference to DataManager (if it holds BuildingDatas)
    // [SerializeField] private DataManager dataManager;
    // Option 3: Load from Resources (requires specific folder structure)
    // ----------------------------------------------------

    #endregion

    #region Private Fields

    // Reference to the progress manager (should persist from menu scene)
    private PlayerProgressManager progressManager;

    #endregion

    #region Unity Lifecycle Methods

    void Awake()
    {
        // --- Singleton Initialization ---
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Usually NOT needed for a GameManager specific to the game scene
        }
        else
        {
            Debug.LogWarning("Duplicate GameManager detected. Destroying the new instance.");
            Destroy(gameObject);
            return;
        }
        // -----------------------------

        // --- Get Dependencies ---
        progressManager = PlayerProgressManager.Instance;
        if (progressManager == null)
        {
            Debug.LogError("PlayerProgressManager not found! Building progress will not be loaded/applied. Game might not function correctly.");
            // Consider fallback behaviour: load default levels or return to menu?
        }

        // --- Load Player's Chosen Race ---
        LoadPlayerRace();

        if (PlayerRace == Race.Neutral)
        {
            Debug.LogError("Player Race is Neutral! Cannot proceed correctly. Return to menu or use a default race?");
            // Example: SceneManager.LoadScene("MainMenuSceneName"); // Define your menu scene name
        }
    }

    void Start()
    {
        // Initialize buildings after Awake ensures PlayerRace is set
        SpawnPlayerBuildings();       // Dynamically create player buildings based on menu choices
        InitializeOtherBuildings();   // Initialize pre-placed neutral/AI buildings
    }

    #endregion

    #region Initialization & Spawning Methods

    // Loads the player's chosen race from PlayerPrefs
    void LoadPlayerRace()
    {
        string raceName = PlayerPrefs.GetString("SelectedPlayerRace", Race.Neutral.ToString());
        try
        {
            PlayerRace = (Race)System.Enum.Parse(typeof(Race), raceName);
            Debug.Log($"GameManager: Player Race Loaded = {PlayerRace}");
        }
        catch (System.ArgumentException)
        {
            Debug.LogError($"GameManager: Could not parse saved race name '{raceName}'. Defaulting to Neutral.");
            PlayerRace = Race.Neutral;
            // PlayerPrefs.DeleteKey("SelectedPlayerRace"); // Optionally remove invalid key
        }
    }

    // --- Spawns Player Buildings based on Menu Progress ---
    void SpawnPlayerBuildings()
    {
        if (progressManager == null)
        {
            Debug.LogError("Cannot spawn player buildings: PlayerProgressManager is missing.");
            return;
        }
        if (PlayerRace == Race.Neutral)
        {
            Debug.LogError("Cannot spawn player buildings: Player Race is Neutral.");
            return;
        }

        Debug.Log($"GameManager: Spawning buildings for Player Race: {PlayerRace}");
        PlayerBuildingSpawnPoint[] spawnPoints = FindObjectsOfType<PlayerBuildingSpawnPoint>(); // Find all markers

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("No PlayerBuildingSpawnPoint markers found on the scene. Player buildings will not be spawned.");
            return;
        }
        Debug.Log($"Found {spawnPoints.Length} potential spawn points.");

        foreach (PlayerBuildingSpawnPoint spawnPoint in spawnPoints)
        {
            int slotIndex = spawnPoint.menuSlotIndex;
            if (slotIndex < 0 || slotIndex > 2) // Assuming 3 slots (0, 1, 2)
            {
                Debug.LogWarning($"Spawn point {spawnPoint.gameObject.name} has invalid slot index: {slotIndex}. Skipping.", spawnPoint);
                continue;
            }

            // 1. Get the Building Type assigned to this slot in the menu
            BuildingType? assignedType = progressManager.GetAssignedBuildingType(PlayerRace, slotIndex);

            // 2. Validate assignment (Slot 0 must be House if assigned)
            if (assignedType == null)
            {
                Debug.Log($"Slot {slotIndex} is not assigned in the menu for race {PlayerRace}. No building spawned at {spawnPoint.gameObject.name}.");
                spawnPoint.gameObject.SetActive(false); // Deactivate empty spawn point marker
                continue;
            }
            if (slotIndex == 0 && assignedType.Value != BuildingType.House)
            {
                Debug.LogWarning($"Spawn point for Slot 0 ({spawnPoint.gameObject.name}) expects House, but {assignedType.Value} is assigned. No building spawned.", spawnPoint);
                spawnPoint.gameObject.SetActive(false);
                continue;
            }

            BuildingType typeToSpawn = assignedType.Value;

            // 3. Get the building's level from progress
            int buildingLevel = progressManager.GetBuildingLevel(PlayerRace, typeToSpawn);

            // 4. Skip spawning if level is 0 (not built), except for House (always level 1+)
            if (buildingLevel <= 0 && typeToSpawn != BuildingType.House)
            {
                Debug.Log($"Building type {typeToSpawn} for slot {slotIndex} has level {buildingLevel} (not built). Skipping spawn at {spawnPoint.gameObject.name}.");
                spawnPoint.gameObject.SetActive(false);
                continue;
            }
            buildingLevel = Mathf.Max(1, buildingLevel); // Ensure level is at least 1

            // 5. Find the correct Prefab and BuildingData
            GameObject prefab = FindPrefabForType(typeToSpawn);
            BuildingData data = FindBuildingDataForRaceAndType(PlayerRace, typeToSpawn); // Use your implemented method here!

            if (prefab == null)
            {
                Debug.LogError($"Prefab not found for building type {typeToSpawn}! Cannot spawn at {spawnPoint.gameObject.name}. Check GameManager -> Building Prefabs list.", spawnPoint);
                continue;
            }
            if (data == null)
            {
                Debug.LogError($"BuildingData not found for type {typeToSpawn} and race {PlayerRace}! Cannot spawn at {spawnPoint.gameObject.name}. Check implementation of FindBuildingDataForRaceAndType.", spawnPoint);
                continue;
            }

            // --- 6. Instantiate the Building ---
            Debug.Log($"Spawning {typeToSpawn} (Level {buildingLevel}) at {spawnPoint.gameObject.name} for Slot {slotIndex}");
            // Instantiate as a child of the spawn point marker initially
            GameObject buildingInstance = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation, spawnPoint.transform);
            buildingInstance.name = $"Player_{PlayerRace}_{typeToSpawn}_Lv{buildingLevel}"; // Assign a descriptive name

            Building buildingScript = buildingInstance.GetComponent<Building>();
            if (buildingScript != null)
            {
                // --- 7. Configure and Initialize ---
                buildingScript.buildingData = data; // Assign the ScriptableObject data
                buildingScript.race = PlayerRace;   // Assign the player's race

                // Initialize after a frame delay using a Coroutine
                StartCoroutine(InitializeBuildingAfterFrame(buildingScript));
            }
            else
            {
                Debug.LogError($"Prefab '{prefab.name}' is missing the required Building script! Destroying spawned instance.", prefab);
                Destroy(buildingInstance);
            }

            // Deactivate the marker now that the building is spawned
            spawnPoint.gameObject.SetActive(false);
        }
        Debug.Log("GameManager: Player building spawning complete.");
    }

    // Coroutine to delay Building initialization until the end of the frame
    private IEnumerator InitializeBuildingAfterFrame(Building buildingScript)
    {
        yield return null; // Wait for the end of the current frame
        if (buildingScript != null && buildingScript.gameObject != null) // Check if the building wasn't destroyed
        {
            Debug.Log($"Coroutine: Calling InitializeBuilding for {buildingScript.gameObject.name}");
            buildingScript.InitializeBuilding();
        }
        else
        {
            Debug.LogWarning("Coroutine: Building script was null or destroyed before initialization could be called.");
        }
    }

    // Finds the correct prefab from the configured list
    private GameObject FindPrefabForType(BuildingType type)
    {
        BuildingPrefabMapping mapping = buildingPrefabs.FirstOrDefault(m => m.type == type);
        if (mapping.prefab == null)
        {
            Debug.LogWarning($"Prefab for building type {type} is not assigned in GameManager's Building Prefabs list.");
        }
        return mapping.prefab; // Returns null if not found or not assigned
    }

    // --- !!! IMPORTANT: REPLACE THIS WITH YOUR ACTUAL IMPLEMENTATION !!! ---
    // Finds the correct BuildingData ScriptableObject based on Race and Type.
    // This likely involves querying your DataManager or a ScriptableObject holding all BuildingDatas.
   // --- ПОИСК BUILDING DATA ЧЕРЕЗ DATAMANAGER ---
private BuildingData FindBuildingDataForRaceAndType(Race race, BuildingType type)
{
    // Проверяем наличие DataManager
    if (DataManager.Instance != null)
    {
        // Запрашиваем данные у DataManager
        BuildingData data = DataManager.Instance.GetBuildingData(race, type);
        if (data == null)
        {
            // Если DataManager не нашел, логируем ошибку (он сам должен был вывести Warning)
            Debug.LogError($"DataManager не смог найти BuildingData для Расы={race}, Типа={type}. Проверьте настройки FactionDataHolder для расы {race}.", this);
        }
        return data; // Возвращаем найденные данные или null
    }
    else
    {
        // Если DataManager вообще отсутствует
        Debug.LogError("DataManager.Instance не найден! Невозможно получить BuildingData.", this);
        return null;
    }
}
// ------------------------------------------
    // --- !!! REPLACE THE ABOVE METHOD'S BODY !!! ---


    // Initializes other buildings already present in the scene (Neutral, AI)
    void InitializeOtherBuildings()
    {
        Debug.Log("GameManager: Инициализация не-игровых зданий на сцене...");
        // Находим ВСЕ здания, включая неактивные и только что созданные
        Building[] allBuildings = Resources.FindObjectsOfTypeAll<Building>(); // Find inactive too
        int initializedCount = 0;

        foreach (Building building in allBuildings)
        {
            // --- ИСПРАВЛЕНАЯ ПРОВЕРКА НА ПРЕФАБ/АССЕТ ---
            // Пропускаем объекты, которые не находятся в загруженной сцене (это ассеты/префабы)
            if (building.gameObject.scene.rootCount == 0) continue;
            // ---------------------------------------------

            // Пропускаем здания, принадлежащие игроку (они инициализируются отдельно)
            if (building.race == PlayerRace && PlayerRace != Race.Neutral)
            {
                continue;
            }

            // Пропускаем здания, находящиеся на деактивированных точках спавна
            PlayerBuildingSpawnPoint parentSpawnPoint = building.GetComponentInParent<PlayerBuildingSpawnPoint>();
            if (parentSpawnPoint != null && !parentSpawnPoint.gameObject.activeSelf)
            {
                continue;
            }

            // Инициализируем только те, что активны в иерархии сцены
            if (building.gameObject.activeInHierarchy)
            {
                // Проверяем, было ли здание уже инициализировано
                 FieldInfo initField = typeof(Building).GetField("isInitialized", BindingFlags.NonPublic | BindingFlags.Instance);
                 bool initialized = initField != null && (bool)initField.GetValue(building);

                 if (!initialized)
                 {
                    Debug.Log($"Инициализация не-игрового активного здания: {building.gameObject.name} (Раса: {building.race})");
                    building.InitializeBuilding(); // Инициализируем Нейтральные или ИИ здания
                    initializedCount++;
                 }
            }
        }
        Debug.Log($"GameManager: Инициализация не-игровых зданий завершена. Инициализировано: {initializedCount}");
    }

    #endregion

    #region Game Logic Placeholders
    // Add methods here for managing the actual game flow, win/loss conditions, etc.
    #endregion

    #region Public Accessors

    // Example: Allows other scripts to easily get the PlayerProgressManager instance
    public PlayerProgressManager GetProgressManager()
    {
        // Ensure progressManager is assigned (it should be from Awake)
        if (progressManager == null)
        {
            progressManager = PlayerProgressManager.Instance;
            if (progressManager == null)
            {
                Debug.LogError("Attempted to get PlayerProgressManager, but it's still null!");
            }
        }
        return progressManager;
    }

    #endregion

} // -- End of GameManager class --