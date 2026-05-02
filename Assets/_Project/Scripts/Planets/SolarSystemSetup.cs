using UnityEngine;

public class SolarSystemSetup : MonoBehaviour
{
    [Header("Planets:")]
    public Transform sun;
    public Transform mercury;
    public Transform venus;
    public Transform earth;
    public Transform mars;
    public Transform jupiter;
    public Transform saturn;
    public Transform uranus;
    public Transform neptune;
    [Header("Moons:")]
    public Transform moon;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        SetupSolarSystem();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetupSolarSystem()
    {
        // Sol
        sun.localScale = Vector3.one * 5f;
        sun.position = Vector3.zero;

        // Planeta: (escala, radio de órbita)
        SetupPlanet(mercury, 0.55f, 6f);
        SetupPlanet(venus, 0.85f, 9f);
        SetupPlanet(earth, 0.9f, 13f);
        SetupPlanet(moon, 0.4f, 2.5f, true);
        SetupPlanet(mars, 0.65f, 17f);
        SetupPlanet(jupiter, 3.2f, 26f);
        SetupPlanet(saturn, 2.7f, 36f);
        SetupPlanet(uranus, 1.8f, 45f);
        SetupPlanet(neptune, 1.7f, 54f);
    }

    void SetupPlanet(Transform planet, float scale, float orbitRadius, bool isLocal = false)
    {
        planet.localScale = Vector3.one * scale;
        if (isLocal)
        {
            // Posición inicial en el borde del radio de órbita
            planet.localPosition = new Vector3(orbitRadius, 0, 0);
        }
        else
        {
            // Posición inicial en el borde del radio de órbita desde el sol
            planet.position = new Vector3(orbitRadius, 0, 0);
        }
    }
}