using UnityEngine;
using System.Collections.Generic; // Для Dictionary
using System; // Для Enum.GetValues

// Класс для хранения данных прогрессии одной расы
[System.Serializable] // Чтобы можно было видеть в инспекторе (если понадобится)
public class RaceProgressData
{
    public string assignedBuildingSlot1 = ""; // Тип здания в слоте 1 (ToString() или "")
    public string assignedBuildingSlot2 = ""; // Тип здания в слоте 2 (ToString() или "")
    public Race race;
    public int level = 1; // Начинаем с 1 уровня
    public int currentXP = 0;
    public int racePoints = 0; // Очки Расы

    // Статус исследования зданий (ключ - тип здания, значение - true/false)
    public Dictionary<BuildingType, bool> researchStatus = new Dictionary<BuildingType, bool>();

    // Уровень зданий (ключ - тип здания, значение - уровень 0-3)
    public Dictionary<BuildingType, int> buildingLevels = new Dictionary<BuildingType, int>();

    // Конструктор для инициализации
    public RaceProgressData(Race r)
    {
        race = r;
        // Инициализируем словари для всех типов зданий
        foreach (BuildingType type in Enum.GetValues(typeof(BuildingType)))
        {
            researchStatus[type] = false; // Изначально ничего не исследовано
            buildingLevels[type] = 0;     // Изначально ничего не построено

            // --- Особое условие для Дома ---
            if (type == BuildingType.House)
            {
                // Считаем Дом исследованным и построенным на 1 уровне сразу
                researchStatus[type] = true;
                buildingLevels[type] = 1;
            }
            // --------------------------------
        }
    }
}

public class PlayerProgressManager : MonoBehaviour
{
    // --- Singleton ---
    public static PlayerProgressManager Instance { get; private set; }

    // --- Глобальные Данные ---
    private int coins = 0; // Начнем с 0 монет

    // --- Расовые Данные ---
    // Словарь для хранения прогресса каждой расы
    private Dictionary<Race, RaceProgressData> raceProgressData = new Dictionary<Race, RaceProgressData>();

    // --- Константы для PlayerPrefs Ключей ---

    private const string BUILDING_SLOT1_KEY_PREFIX = "_Slot1_Type";
    private const string BUILDING_SLOT2_KEY_PREFIX = "_Slot2_Type";
    private const string COINS_KEY = "PlayerCoins";
    private const string RACE_LEVEL_KEY_PREFIX = "_Level";
    private const string RACE_XP_KEY_PREFIX = "_XP";
    private const string RACE_POINTS_KEY_PREFIX = "_RP";
    private const string BUILDING_RESEARCH_KEY_PREFIX = "_Research_"; // Race_Research_BuildingType
    private const string BUILDING_LEVEL_KEY_PREFIX = "_Level_";     // Race_Level_BuildingType

    // --- Настройки Прогрессии ---
    private const int MAX_BUILDING_LEVEL = 3;

    void Awake()
    {
        // --- Singleton Setup ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Сохраняем менеджер между сценами
            LoadAllProgress(); // Загружаем прогресс при старте
        }
        else
        {
            Destroy(gameObject); // Уничтожаем дубликат
        }
    }

    #region Сохранение и Загрузка

    public void LoadAllProgress()
    {
        coins = PlayerPrefs.GetInt(COINS_KEY, 500); // Начнем с 500 монет для теста

        raceProgressData.Clear(); // Очищаем перед загрузкой

        foreach (Race race in Enum.GetValues(typeof(Race)))
        {
            if (race == Race.Neutral) continue; // Пропускаем нейтральную расу

            RaceProgressData data = new RaceProgressData(race); // Создаем с дефолтными значениями

            // Загружаем основные параметры расы
            data.level = PlayerPrefs.GetInt(race.ToString() + RACE_LEVEL_KEY_PREFIX, 1);
            data.currentXP = PlayerPrefs.GetInt(race.ToString() + RACE_XP_KEY_PREFIX, 0);
            data.racePoints = PlayerPrefs.GetInt(race.ToString() + RACE_POINTS_KEY_PREFIX, 100); // Дадим 100 RP для старта
            data.assignedBuildingSlot1 = PlayerPrefs.GetString(race.ToString() + BUILDING_SLOT1_KEY_PREFIX, "");
            data.assignedBuildingSlot2 = PlayerPrefs.GetString(race.ToString() + BUILDING_SLOT2_KEY_PREFIX, "");

            // Загружаем статус зданий
            foreach (BuildingType type in Enum.GetValues(typeof(BuildingType)))
            {
                string researchKey = race.ToString() + BUILDING_RESEARCH_KEY_PREFIX + type.ToString();
                string levelKey = race.ToString() + BUILDING_LEVEL_KEY_PREFIX + type.ToString();
                
                // Загружаем статус исследования (кроме Дома, он всегда исследован)
                if (type != BuildingType.House)
                {
                    data.researchStatus[type] = PlayerPrefs.GetInt(researchKey, 0) == 1;
                } else {
                    data.researchStatus[type] = true; // Дом всегда исследован
                }


                // Загружаем уровень здания (у Дома мин. 1, у остальных 0)
                int defaultLevel = (type == BuildingType.House) ? 1 : 0;
                data.buildingLevels[type] = PlayerPrefs.GetInt(levelKey, defaultLevel);
                // Доп. проверка, чтобы уровень дома не был 0
                 if (type == BuildingType.House && data.buildingLevels[type] == 0) {
                     data.buildingLevels[type] = 1;
                 }
            }

            raceProgressData[race] = data; // Добавляем данные расы в словарь
        }
        Debug.Log("Player progress loaded.");
    }

    public void SaveAllProgress()
    {
        PlayerPrefs.SetInt(COINS_KEY, coins);

        foreach (var kvp in raceProgressData)
        {
            Race race = kvp.Key;
            RaceProgressData data = kvp.Value;

            PlayerPrefs.SetInt(race.ToString() + RACE_LEVEL_KEY_PREFIX, data.level);
            PlayerPrefs.SetInt(race.ToString() + RACE_XP_KEY_PREFIX, data.currentXP);
            PlayerPrefs.SetInt(race.ToString() + RACE_POINTS_KEY_PREFIX, data.racePoints);
            PlayerPrefs.SetString(race.ToString() + BUILDING_SLOT1_KEY_PREFIX, data.assignedBuildingSlot1);
            PlayerPrefs.SetString(race.ToString() + BUILDING_SLOT2_KEY_PREFIX, data.assignedBuildingSlot2);

            foreach (BuildingType type in Enum.GetValues(typeof(BuildingType)))
            {
                string researchKey = race.ToString() + BUILDING_RESEARCH_KEY_PREFIX + type.ToString();
                string levelKey = race.ToString() + BUILDING_LEVEL_KEY_PREFIX + type.ToString();

                PlayerPrefs.SetInt(researchKey, data.researchStatus.ContainsKey(type) && data.researchStatus[type] ? 1 : 0);
                PlayerPrefs.SetInt(levelKey, data.buildingLevels.ContainsKey(type) ? data.buildingLevels[type] : 0);
            }
        }

        PlayerPrefs.Save(); // Сохраняем изменения на диск
        Debug.Log("Player progress saved.");
    }

     // Сохраняемся при выходе из приложения
     void OnApplicationQuit()
     {
         SaveAllProgress();
     }

    // Метод для сброса прогресса (для тестов)
    public void ResetAllProgress()
    {
         PlayerPrefs.DeleteAll();
         LoadAllProgress(); // Перезагружаем значения по умолчанию
         Debug.LogWarning("All player progress has been reset!");
    }


    #endregion

    #region Доступ к Данным

    // --- Глобальные ---
    public int GetCoins() => coins;

    // --- Расовые ---
    private RaceProgressData GetRaceData(Race race)
    {
        if (race == Race.Neutral) return null;
        if (!raceProgressData.ContainsKey(race))
        {
            // Если данных для расы нет (маловероятно после Load), создаем новые
            Debug.LogWarning($"RaceProgressData for {race} not found, creating default.");
            raceProgressData[race] = new RaceProgressData(race);
        }
        return raceProgressData[race];
    }

    public int GetRaceLevel(Race race) => GetRaceData(race)?.level ?? 1;
    public int GetRaceXP(Race race) => GetRaceData(race)?.currentXP ?? 0;
    public int GetRacePoints(Race race) => GetRaceData(race)?.racePoints ?? 0;

    public bool IsBuildingResearched(Race race, BuildingType type)
    {
        var data = GetRaceData(race);
        return data != null && data.researchStatus.ContainsKey(type) && data.researchStatus[type];
    }

    public int GetBuildingLevel(Race race, BuildingType type)
    {
        var data = GetRaceData(race);
        if (data != null && data.buildingLevels.ContainsKey(type))
        {
            return data.buildingLevels[type];
        }
        return (type == BuildingType.House) ? 1 : 0; // Возвращаем 1 для дома, 0 для остальных по умолчанию
    }

     public int GetMaxBuildingLevel() => MAX_BUILDING_LEVEL;

    // --- Расчеты ---
    public int GetXPForNextLevel(Race race)
    {
        int currentLevel = GetRaceLevel(race);
        // Простая формула для примера: 100 * уровень^2
        return 100 * currentLevel * currentLevel;
    }

    public int GetResearchCostRacePoints(BuildingType type)
    {
        // Задайте стоимость исследования для каждого типа здания
        switch (type)
        {
            case BuildingType.Altar: return 150;
            case BuildingType.Tower: return 100;
            case BuildingType.Stable: return 120;
            case BuildingType.House: return 0; // Дом не исследуется
            default: return 9999; // Неизвестное здание
        }
    }

    public int GetNextUpgradeCostCoins(BuildingType type, int currentLevel)
    {
        if (currentLevel >= MAX_BUILDING_LEVEL) return 0; // Макс уровень достигнут

        int nextLevel = currentLevel + 1; // Уровень, на который улучшаем
        int baseCost = 0;

        // Базовая стоимость для первого уровня (постройки)
        switch (type)
        {
            case BuildingType.House: baseCost = 100; break; // Улучшение дома
            case BuildingType.Altar: baseCost = 300; break; // Постройка Алтаря
            case BuildingType.Tower: baseCost = 200; break; // Постройка Башни
            case BuildingType.Stable: baseCost = 250; break; // Постройка Конюшни
            default: return 99999;
        }

        // Увеличиваем стоимость для последующих уровней
        // Пример: Уровень 2 = базовая * 2, Уровень 3 = базовая * 4
        return baseCost * (int)Mathf.Pow(2, nextLevel - 1);
    }


    #endregion

    #region Изменение Данных

    // --- Монеты ---
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        SaveAllProgress(); // Сохраняем после изменения
        // TODO: Вызвать событие OnCoinsChanged?
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return false;
        if (coins >= amount)
        {
            coins -= amount;
            SaveAllProgress();
             // TODO: Вызвать событие OnCoinsChanged?
            return true;
        }
        return false; // Недостаточно монет
    }

    // --- Очки Расы ---
     public void AddRacePoints(Race race, int amount)
     {
         if (amount <= 0 || race == Race.Neutral) return;
         var data = GetRaceData(race);
         if (data != null)
         {
             data.racePoints += amount;
             SaveAllProgress();
              // TODO: Вызвать событие OnRacePointsChanged(race)?
         }
     }

     public bool SpendRacePoints(Race race, int amount)
     {
         if (amount <= 0 || race == Race.Neutral) return false;
         var data = GetRaceData(race);
         if (data != null && data.racePoints >= amount)
         {
             data.racePoints -= amount;
             SaveAllProgress();
              // TODO: Вызвать событие OnRacePointsChanged(race)?
             return true;
         }
         return false; // Недостаточно очков
     }

     // --- Опыт и Уровень Расы ---
     public void AddRaceXP(Race race, int amount)
     {
         if (amount <= 0 || race == Race.Neutral) return;
         var data = GetRaceData(race);
         if (data != null)
         {
             data.currentXP += amount;
             Debug.Log($"{race} gained {amount} XP. Total: {data.currentXP}");

             // Проверка повышения уровня
             int xpNeeded = GetXPForNextLevel(race);
             while (data.currentXP >= xpNeeded)
             {
                 data.level++;
                 data.currentXP -= xpNeeded;
                 Debug.Log($"{race} leveled up to Level {data.level}! XP remaining: {data.currentXP}");
                 // TODO: Добавить награду за уровень? (Монеты, RP?)
                 // AddCoins(data.level * 50);
                 // AddRacePoints(race, data.level * 10);

                 xpNeeded = GetXPForNextLevel(race); // Пересчитываем для следующего возможного уровня
             }
             SaveAllProgress();
             // TODO: Вызвать событие OnRaceXPChanged(race) или OnRaceLevelUp(race)?
         }
     }

      // --- Исследования ---
      public bool MarkBuildingResearched(Race race, BuildingType type)
      {
          if (race == Race.Neutral || type == BuildingType.House) return false; // Нельзя исследовать нейтральные или Дом
          var data = GetRaceData(race);
          if (data != null && data.researchStatus.ContainsKey(type) && !data.researchStatus[type])
          {
              data.researchStatus[type] = true;
              SaveAllProgress();
              Debug.Log($"{type} researched for {race}");
              // TODO: Вызвать событие OnBuildingResearched(race, type)?
              return true;
          }
          return false; // Уже исследовано или тип не найден
      }
    public BuildingType? GetAssignedBuildingType(Race race, int slotIndex) // 0, 1, 2
    {
        if (slotIndex == 0) return BuildingType.House; // Слот 0 всегда Дом

        var data = GetRaceData(race);
        if (data == null || slotIndex < 1 || slotIndex > 2) return null;

        string typeStr = (slotIndex == 1) ? data.assignedBuildingSlot1 : data.assignedBuildingSlot2;

        if (string.IsNullOrEmpty(typeStr)) return null;

        try { return (BuildingType)Enum.Parse(typeof(BuildingType), typeStr); }
        catch { return null; } // На случай некорректной строки
    }

// Назначить тип здания на слот (или очистить, если type = null)
    public bool SetAssignedBuildingType(Race race, int slotIndex, BuildingType? type)
    {
        if (race == Race.Neutral || slotIndex <= 0 || slotIndex > 2) return false; // Только слоты 1 и 2

        var data = GetRaceData(race);
        if (data == null) return false;

        string typeStr = type?.ToString() ?? ""; // Конвертируем в строку или пустую строку

        if (slotIndex == 1) data.assignedBuildingSlot1 = typeStr;
        else data.assignedBuildingSlot2 = typeStr;

        SaveAllProgress(); // Сохраняем изменение
        return true;
    }

// Получить список зданий, доступных для назначения в пустой слот
    public List<BuildingType> GetAvailableBuildingsToAssign(Race race)
    {
        List<BuildingType> available = new List<BuildingType>();
        var data = GetRaceData(race);
        if (data == null) return available;

        BuildingType? slot1Type = GetAssignedBuildingType(race, 1);
        BuildingType? slot2Type = GetAssignedBuildingType(race, 2);

        foreach (BuildingType type in Enum.GetValues(typeof(BuildingType)))
        {
            // Пропускаем Дом и Нейтральные типы, если они есть
            if (type == BuildingType.House) continue;

            // Доступно если: Исследовано И (Не назначено на слот 1 И Не назначено на слот 2)
            if (IsBuildingResearched(race, type) &&
               (slot1Type == null || slot1Type.Value != type) &&
                (slot2Type == null || slot2Type.Value != type))
            {
                available.Add(type);
            }
        }
        return available;
    }
      // --- Уровни Зданий ---
      public bool SetBuildingLevel(Race race, BuildingType type, int newLevel)
      {
            if (race == Race.Neutral) return false;
            // Ограничиваем уровень
             int levelToSet = Mathf.Clamp(newLevel, (type == BuildingType.House ? 1 : 0), MAX_BUILDING_LEVEL);

            var data = GetRaceData(race);
            if (data != null && data.buildingLevels.ContainsKey(type))
            {
                if (data.buildingLevels[type] != levelToSet) // Изменяем, только если уровень отличается
                {
                     // Дополнительная проверка: нельзя установить уровень > 0, если не исследовано (кроме Дома)
                     if (levelToSet > 0 && type != BuildingType.House && !IsBuildingResearched(race, type))
                     {
                          Debug.LogError($"Cannot set level {levelToSet} for unresearched building {type} for {race}");
                          return false;
                     }

                    data.buildingLevels[type] = levelToSet;
                    SaveAllProgress();
                    Debug.Log($"{type} level set to {levelToSet} for {race}");
                    // TODO: Вызвать событие OnBuildingLevelChanged(race, type, levelToSet)?
                    return true;
                }
                return true; // Уровень уже был таким
            }
            return false; // Тип не найден
      }

    #endregion
}