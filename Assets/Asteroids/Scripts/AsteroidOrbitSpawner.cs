using System.Collections.Generic;
using UnityEngine;

namespace Asteroids
{
    public class AsteroidBeltSpawner : MonoBehaviour
    {
        [Tooltip("The central object which the asteroids orbit around.")]
        public GameObject sun;

        [Tooltip("The prefab used to represent asteroids.")]
        public GameObject asteroidPrefab;

        [Tooltip("The maximum radius of the asteroid belt orbit.")]
        public float maxOrbitRadius = 100f;

        [Tooltip("Total number of asteroids to spawn.")]
        public int maxAsteroidsCount = 2000;

        [Tooltip("Minimum size scale for asteroids.")]
        public float minSize = 0.5f;

        [Tooltip("Maximum size scale for asteroids.")]
        public float maxSize = 2.0f;

        [Tooltip("The curve controlling the horizontal spread of asteroids.")]
        public AnimationCurve horizontalSpreadCurve;

        [Tooltip("The curve controlling the vertical spread of asteroids.")]
        public AnimationCurve verticalSpreadCurve;

        [Tooltip("Minimum distance between spawned asteroids.")]
        public float spaceBetween = 2f;

        private List<Vector4> asteroidPositionsAndSizes = new List<Vector4>(); // Массив для хранения координат и размеров
      

        void Start()
        {
            SpawnAsteroids();
            // Destroy the prefab after generation
            if (asteroidPrefab != null)
            {
                Destroy(asteroidPrefab);
            }
        }

        void SpawnAsteroids()
        {
            int currentAsteroidsCount = 0;

            for (int i = 0; i < 360; i++)
            {
                // Get the curve values ​​for the current angle (in normalized coordinates)
                float horizontalDensity = horizontalSpreadCurve.Evaluate(i / 360f);
                float verticalDensity = verticalSpreadCurve.Evaluate(i / 360f);

                // Determine the number of asteroids for a given angle based on horizontal density
                int asteroidsInThisSegment = Mathf.RoundToInt(horizontalDensity * (maxAsteroidsCount / 360f));

                for (int j = 0; j < asteroidsInThisSegment && currentAsteroidsCount < maxAsteroidsCount; j++)
                {
                    // Calculate the main radius of the orbit
                    float orbitRadius = Random.Range(maxOrbitRadius * 0.8f, maxOrbitRadius);

                    // Calculate horizontal offset from orbit radius within width
                    float horizontalOffset = Random.Range(-horizontalDensity * orbitRadius, horizontalDensity * orbitRadius);

                    // Рассчитываем позицию на орбите
                    Vector3 orbitPosition = Quaternion.Euler(0, i, 0) * new Vector3(orbitRadius + horizontalOffset, 0, 0);

                    // Determine the number of vertical asteroid layers for a given angle
                    int verticalAsteroidsInThisSegment = Mathf.RoundToInt(verticalDensity * 10);  // Количество вертикальных слоёв

                    for (int k = 0; k < verticalAsteroidsInThisSegment && currentAsteroidsCount < maxAsteroidsCount; k++)
                    {
                        // Calculate the vertical offset within the curve value
                        float verticalOffset = Mathf.Lerp(0, Random.Range(-maxOrbitRadius / 10f, maxOrbitRadius / 10f), verticalDensity);

                        // Final position of the asteroid with vertical offset
                        Vector3 finalPosition = sun.transform.position + orbitPosition + new Vector3(0, verticalOffset, 0);

                        // Generate random asteroid size
                        float asteroidSize = Random.Range(minSize, maxSize);

                        // Check for intersection with other asteroids taking into account their size
                        if (!IsPositionValidWithSize(finalPosition, asteroidSize))
                        {
                            // If the position is invalid, skip this asteroid
                            continue;
                        }

                        // Add position and size to array
                        asteroidPositionsAndSizes.Add(new Vector4(finalPosition.x, finalPosition.y, finalPosition.z, asteroidSize));

                        // Спавним астероид
                        SpawnAsteroid(finalPosition, asteroidSize);
                        
                        currentAsteroidsCount++;  // Increase the asteroid counter

                        //Debug.Log($"Asteroid #{currentAsteroidsCount} spawned at {finalPosition} with size {asteroidSize}");

                        // If we have reached the maximum number of asteroids, we exit
                        if (currentAsteroidsCount >= maxAsteroidsCount)
                            return;
                    }
                }
            }
        }

        // Function for checking new coordinates taking into account the sizes of asteroids
        bool IsPositionValidWithSize(Vector3 newPosition, float newSize)
        {
            foreach (var asteroidData in asteroidPositionsAndSizes)
            {
                Vector3 existingPosition = new Vector3(asteroidData.x, asteroidData.y, asteroidData.z);
                float existingSize = asteroidData.w;

                // Function for checking new coordinates taking into account the sizes of asteroids
                float distanceBetweenAsteroids = Vector3.Distance(newPosition, existingPosition);
                if (distanceBetweenAsteroids < (existingSize / 2 + newSize / 2 + spaceBetween))
                {
                    return false;
                }
            }
            return true;
        }

        void SpawnAsteroid(Vector3 position, float size)
        {
            GameObject asteroid = Instantiate(asteroidPrefab, position, Quaternion.identity);
            asteroid.SetActive(true);
            asteroid.transform.localScale = new Vector3(size, size, size);
        }
    }
   
}
