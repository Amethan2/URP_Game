using UnityEngine; // Нужно для Mathf

// Перечисление для рас
public enum Race
{
    Neutral, // Должен быть первым (значение 0 по умолчанию)
    Nord,
    Orden,
    Rus,
    Orda
}

// Статический класс для получения характеристик расы
public static class RaceStats
{
    // Получить показатель атаки для расы
    public static int GetAttack(Race race)
    {
        switch (race)
        {
            case Race.Nord:    return 3;
            case Race.Orden:   return 2;
            case Race.Rus:     return 2;
            case Race.Orda:    return 2;
            case Race.Neutral: return 0; // Нейтральные не атакуют сами
            default:           return 1; // Значение по умолчанию на всякий случай
        }
    }

    // Получить показатель защиты для расы
    public static int GetDefense(Race race)
    {
        switch (race)
        {
            case Race.Nord:    return 1;
            case Race.Orden:   return 3;
            case Race.Rus:     return 2;
            case Race.Orda:    return 1;
            case Race.Neutral: return 1; // Нейтральные имеют минимальную защиту
            default:           return 1;
        }
    }

    // Получить показатель скорости перемещения для расы
    public static float GetSpeed(Race race)
    {
        switch (race)
        {
            case Race.Nord:    return 2.0f;
            case Race.Orden:   return 1.0f;
            case Race.Rus:     return 2.0f;
            case Race.Orda:    return 3.0f;
            case Race.Neutral: return 0f;  // Нейтральные не двигаются
            default:           return 1.5f;
        }
    }

     // Можно добавить метод для получения цвета расы, если нужно
     public static Color GetColor(Race race)
     {
         switch (race)
         {
             case Race.Nord:    return new Color(0.8f, 0.3f, 0.3f); // Красноватый
             case Race.Orden:   return new Color(0.9f, 0.9f, 0.9f); // Почти белый
             case Race.Rus:     return new Color(0.3f, 0.8f, 0.3f); // Зеленоватый
             case Race.Orda:    return new Color(0.8f, 0.8f, 0.2f); // Желтоватый/Охра
             case Race.Neutral: return Color.grey;
             default:           return Color.magenta; // Явно ошибочный цвет
         }
     }
}