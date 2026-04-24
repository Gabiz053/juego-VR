using UnityEngine;
using System.Text.RegularExpressions;

namespace Asteroids
{
    public class RND_sliders_random : MonoBehaviour
    {
        void Start()
        {
            // Получаем материал
            Material material = GetComponent<Renderer>().material;

            // Регулярное выражение для извлечения диапазонов в формате float (с поддержкой отрицательных значений)
            Regex rangeRegex = new Regex(@"_RND_(-?\d+(\.\d+)?)_(-?\d+(\.\d+)?)");

            // Проходим по всем свойствам шейдера
            for (int i = 0; i < material.shader.GetPropertyCount(); i++)
            {
                // Выбираем свойства, содержащие "_RND" в названии
                if (material.shader.GetPropertyName(i).Contains("_RND"))
                {
                    string sliderName = material.shader.GetPropertyName(i);

                    // Извлекаем диапазон значений из имени свойства
                    Match match = rangeRegex.Match(sliderName);
                    if (match.Success && match.Groups.Count >= 3)
                    {
                        // Парсим минимальное и максимальное значения из групп регулярного выражения
                        float minValue = float.Parse(match.Groups[1].Value);
                        float maxValue = float.Parse(match.Groups[3].Value);
                        //Debug.Log($"Диапазон для {sliderName}: {minValue} - {maxValue}");

                        // Генерируем случайное значение в заданном диапазоне
                        float randomValue = UnityEngine.Random.Range(minValue, maxValue);

                        // Устанавливаем значение параметра
                        material.SetFloat(sliderName, randomValue);
                    }
                    else
                    {
                        Debug.LogWarning($"Параметр {sliderName} не содержит корректного диапазона в названии.");
                    }
                }
            }
        }
    }
}
