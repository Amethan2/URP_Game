using UnityEngine;

public class PlayerBuildingSpawnPoint : MonoBehaviour
{
    [Tooltip("Индекс слота в меню, которому соответствует эта точка спавна (0 для Дома, 1 или 2 для выбираемых слотов)")]
    public int menuSlotIndex = -1; // -1 означает невалидный слот
}