using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Нужно для .ToDictionary()

// Этот менеджер будет хранить ссылки на данные всех фракций и предоставлять доступ к ним
public class DataManager : MonoBehaviour
{
    // --- Синглтон ---
    public static DataManager Instance { get; private set; }

    [Header("Faction Data")]
    [Tooltip("Перетащите сюда ВСЕ ассеты FactionDataHolder для каждой играбельной расы.")]
    [SerializeField] private List<FactionDataHolder> allFactionData = new List<FactionDataHolder>();

    // Словарь для быстрого доступа к FactionDataHolder по значению enum Race
    private Dictionary<Race, FactionDataHolder> factionDataLookup;

    void Awake()
    {
        // --- Реализация Синглтона ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Раскомментируйте, если менеджер должен сохраняться между сценами
            InitializeDataManager(); // Инициализируем менеджер
        }
        else
        {
            // Уничтожаем дубликат, если он уже есть
            Debug.LogWarning("Duplicate DataManager detected. Destroying the new one.", this);
            Destroy(gameObject);
        }
    }

    // Метод для инициализации словаря быстрого доступа
    private void InitializeDataManager()
    {
        // Проверяем, есть ли данные для инициализации
        if (allFactionData == null || allFactionData.Count == 0)
        {
            Debug.LogError("Список 'All Faction Data' в DataManager пуст! Невозможно инициализировать.", this);
            factionDataLookup = new Dictionary<Race, FactionDataHolder>(); // Создаем пустой словарь
            return;
        }

        // Создаем словарь: ключ - раса из FactionDataHolder, значение - сам FactionDataHolder
        // Используем try-catch на случай дубликатов рас в списке allFactionData
        try
        {
             factionDataLookup = allFactionData.Where(data => data != null).ToDictionary(data => data.factionRace, data => data);
             Debug.Log("DataManager Initialized. Loaded data for races: " + string.Join(", ", factionDataLookup.Keys));
        }
        catch (System.ArgumentException ex)
        {
             Debug.LogError($"Ошибка при инициализации DataManager: Обнаружены дублирующиеся расы в списке 'All Faction Data'. {ex.Message}", this);
             // Создаем словарь только с уникальными записями
              factionDataLookup = new Dictionary<Race, FactionDataHolder>();
              foreach(var data in allFactionData) {
                  if (data != null && !factionDataLookup.ContainsKey(data.factionRace)) {
                      factionDataLookup.Add(data.factionRace, data);
                  }
              }
        }
    }

    /// <summary>
    /// Возвращает UnitData, который должен производиться зданием указанного типа для указанной расы.
    /// </summary>
    /// <param name="race">Раса-владелец здания.</param>
    /// <param name="buildingType">Тип здания.</param>
    /// <returns>Соответствующий UnitData или null, если данные не найдены.</returns>
    public UnitData GetUnitDataForBuilding(Race race, BuildingType buildingType)
    {
        // Проверяем, инициализирован ли словарь
        if (factionDataLookup == null)
        {
            Debug.LogError("DataManager lookup dictionary is not initialized!", this);
            return null;
        }

        // Пытаемся найти данные для указанной расы
        if (factionDataLookup.TryGetValue(race, out FactionDataHolder factionData))
        {
            // Если нашли, просим у FactionDataHolder нужный UnitData для типа здания
            return factionData.GetUnitForBuilding(buildingType);
        }
        else
        {
            // Если данных для такой расы нет в менеджере
            Debug.LogWarning($"FactionData for race '{race}' not found in DataManager lookup.");
            return null;
        }
    }
 public BuildingData GetBuildingData(Race race, BuildingType buildingType)
    {
        // Проверка инициализации словаря
        if (factionDataLookup == null) {
             Debug.LogError("Словарь factionDataLookup в DataManager не инициализирован!", this);
             return null;
        }

        // Пытаемся найти данные для указанной расы
        if (factionDataLookup.TryGetValue(race, out FactionDataHolder factionData))
        {
             // Проверяем, что найденный FactionDataHolder не null
             if (factionData != null)
             {
                  // Вызываем метод GetBuildingData из FactionDataHolder
                  BuildingData bd = factionData.GetBuildingData(buildingType);
                  if (bd == null) {
                       // Логируем предупреждение, если FactionDataHolder не нашел данные
                       Debug.LogWarning($"BuildingData для типа {buildingType} не найден в FactionDataHolder расы {race}.", factionData);
                  }
                  return bd; // Возвращаем найденные данные (или null, если их не было)
             }
             else
             {
                  // Логируем ошибку, если сам FactionDataHolder оказался null
                  Debug.LogError($"Найденный FactionDataHolder для расы {race} является null!", this);
                  return null;
             }
        }
        else
        {
             // Логируем предупреждение, если данных для такой расы нет в словаре
             Debug.LogWarning($"Данные для расы '{race}' не найдены в DataManager.", this);
             return null;
        }
    }
    
    // Сюда можно добавить другие методы для получения данных, например:
    // public BuildingData GetBuildingDataForRace(Race race, BuildingType type) { ... }
}