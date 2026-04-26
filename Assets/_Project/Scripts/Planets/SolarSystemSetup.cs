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
    void Start()
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
        SetupPlanet(mercury,  0.2f,  8f);
        SetupPlanet(venus,    0.45f, 12f);
        SetupPlanet(earth,    0.5f,  17f);
        SetupPlanet(moon,     0.15f, 2.5f, true); // Luna en órbita alrededor de la Tierra
        SetupPlanet(mars,     0.35f, 23f);
        SetupPlanet(jupiter,  1.8f,  35f);
        SetupPlanet(saturn,   1.5f,  48f);
        SetupPlanet(uranus,   1.0f,  60f);
        SetupPlanet(neptune,  0.95f, 72f);
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