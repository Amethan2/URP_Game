using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Добавлено для FirstOrDefault

[CreateAssetMenu(fileName = "FactionData_New", menuName = "RTS Game/Faction Data Holder")]
public class FactionDataHolder : ScriptableObject
{
    public Race factionRace;

    [System.Serializable]
    public struct BuildingUnitPair {
         public BuildingType buildingType;
         public UnitData unitData;
    }
    public List<BuildingUnitPair> buildingProduction = new List<BuildingUnitPair>();

    [Header("Available Building Data")]
    [Tooltip("Список ВСЕХ BuildingData ассетов, доступных этой расе")]
    public List<BuildingData> availableBuildingDatas = new List<BuildingData>(); // <-- Убедитесь, что это поле есть!

    // --- МЕТОД ДЛЯ ПОЛУЧЕНИЯ ЮНИТОВ (УЖЕ БЫЛ) ---
    public UnitData GetUnitForBuilding(BuildingType type)
    {
        foreach (var pair in buildingProduction) {
             if (pair.buildingType == type) {
                  if (pair.unitData != null) return pair.unitData;
                  else { Debug.LogWarning($"UnitData не назначен для BuildingType '{type}' в FactionDataHolder для расы '{factionRace}'."); return null; }
             }
        }
        Debug.LogWarning($"Запись для BuildingType '{type}' не найдена в списке buildingProduction для расы '{factionRace}'.");
        return null;
    }

    // --- !!! НОВЫЙ МЕТОД ДЛЯ ПОЛУЧЕНИЯ BUILDING DATA !!! ---
    /// <summary>
    /// Находит BuildingData для указанного типа здания СРЕДИ доступных этой расе.
    /// </summary>
    /// <param name="type">Тип искомого здания.</param>
    /// <returns>Ассет BuildingData или null, если не найден.</returns>
    public BuildingData GetBuildingData(BuildingType type)
    {
        // Проверяем, что список доступных зданий назначен
         if (availableBuildingDatas == null)
         {
              Debug.LogWarning($"Список 'Available Building Datas' не назначен в FactionDataHolder для расы {factionRace}.", this);
              return null;
         }

         // Ищем в списке здание с нужным типом
         // Используем FirstOrDefault, чтобы безопасно получить null, если ничего не найдено
         BuildingData foundData = availableBuildingDatas.FirstOrDefault(data => data != null && data.type == type);

         if (foundData == null)
         {
              // Выводим предупреждение, если не нашли (для отладки)
              Debug.LogWarning($"BuildingData для типа {type} не найден в списке 'Available Building Datas' у FactionDataHolder расы {factionRace}.", this);
         }

         return foundData; // Возвращаем найденные данные или null
    }
    // --- КОНЕЦ НОВОГО МЕТОДА ---

} // Конец класса FactionDataHolder