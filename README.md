<!-- markdownlint-disable MD013 MD060 -->

# Juego VR — Sistema Solar Educativo

![Unity](https://img.shields.io/badge/Unity-6-black?style=flat-square&logo=unity)
![OpenXR](https://img.shields.io/badge/OpenXR-1.16-blue?style=flat-square)
![XRI](https://img.shields.io/badge/XR_Interaction_Toolkit-3.0.10-5b9bd5?style=flat-square)
![URP](https://img.shields.io/badge/Render-URP_17-red?style=flat-square)
![Device](https://img.shields.io/badge/Target-Meta_Quest_3-green?style=flat-square&logo=meta)

Videojuego educativo VR del Sistema Solar para **Meta Quest 3** (Android Standalone). El jugador navega entre cuatro modos de aprendizaje cruzando portales físicos en una sala de cristal flotante en el espacio.

| Dato | Valor |
|------|-------|
| **Hardware de referencia** | Meta Quest 3 |
| **Motor** | Unity 6 (6000.0.x) |
| **Pipeline** | Universal Render Pipeline (URP) 17.0.4 |
| **XR Runtime** | OpenXR 1.16.1 |
| **Toolkit** | XR Interaction Toolkit 3.0.10 |
| **Input** | Input System 1.17.0 |
| **Plataforma** | Android (Standalone VR) |
| **Version** | 0.5.0 |

---

## Tabla de contenidos

1. [Como abrir el proyecto](#1-como-abrir-el-proyecto)
2. [Estructura de carpetas](#2-estructura-de-carpetas)
3. [Escenas del proyecto](#3-escenas-del-proyecto)
4. [Arquitectura base](#4-arquitectura-base)
5. [Modos de juego](#5-modos-de-juego)
6. [Flujos detallados](#6-flujos-detallados)
7. [Shaders y rendering](#7-shaders-y-rendering)
8. [Inventario de assets](#8-inventario-de-assets)
9. [Catalogo de scripts](#9-catalogo-de-scripts)
10. [Estado del proyecto](#10-estado-del-proyecto)
11. [Dependencias](#11-dependencias)

---

## 1. Como abrir el proyecto

> **Requisito previo — Git LFS:** Este repositorio usa Git Large File Storage para modelos 3D, texturas y audio. Debes instalarlo antes de clonar o los archivos binarios aparecerán corruptos.
>
> ```bash
> # Instalar Git LFS (solo la primera vez en cada maquina)
> git lfs install
> ```
>
> Descarga Git LFS desde [git-lfs.com](https://git-lfs.com) si no lo tienes. En Windows se puede instalar con `winget install GitHub.GitLFS`.

1. Instala **Unity 6** con modulos: Android Build Support, OpenXR Plugin, Universal RP.
2. Instala **Git LFS** (ver requisito previo) y ejecuta `git lfs install`.
3. Clona el repositorio:

    ```bash
    git clone <repo>.git
    cd <carpeta>
    git lfs pull   # descarga modelos, texturas y audio
    ```

4. Abre la carpeta raiz en Unity Hub.
5. Configura Build Target a **Android**.
6. Escena de entrada: `Assets/_Project/Scenes/Main_VR.unity`.
7. En Project Settings, valida OpenXR + Meta Quest Touch Controller Profile activo.

---

## 2. Estructura de carpetas

```text
Assets/
+-- _Project/
|   +-- Assets/
|   |   +-- ScriptableObjects/       <- PlanetConfig_*.asset (11 planetas)
|   +-- Audio/
|   |   +-- Music/
|   |   |   +-- InterstellarComplete/   <- 26 pistas MP3
|   |   +-- SFX/
|   |       +-- UI/
|   |       +-- Interaction/               <- break.wav, hit*.wav, pickup*.wav
|   +-- Materials/
|   |   +-- UnityURPGlassShader/  <- suelo cristal portal room
|   +-- Prefabs/
|   |   +-- Locomotion/
|   |   +-- Interaction/
|   |   +-- UI/
|   |   +-- VFX/
|   +-- Scenes/
|   |   +-- Main_VR.unity            <- Hub: sala de portales (GameState: MainMenu)
|   |   +-- SolarSystem.unity        <- Leccion 1  (GameState: SolarSystem)
|   |   +-- GravityLab.unity         <- Selector de planetas Leccion 2
|   |   +-- KeplerLab 1.unity        <- Leccion 3 — Ley 1 (orbitas elipticas)
|   |   +-- KeplerLab 2.unity        <- Leccion 3 — Ley 2 (areas iguales)
|   |   +-- KeplerLab 3.unity        <- Leccion 3 — Ley 3 (periodo²  ∝  semieje³)
|   |   +-- Sandbox.unity            <- Leccion 4  (GameState: Sandbox)
|   |   +-- Planets/
|   |       +-- Tierra.unity         <- Leccion 2  (GameState: PlanetSurface)
|   |       +-- Mercurio.unity
|   |       +-- Venus.unity
|   |       +-- Marte.unity
|   |       +-- Jupiter.unity
|   |       +-- Saturno.unity
|   |       +-- Urano.unity
|   |       +-- Neptuno.unity
|   |       +-- Pluton.unity
|   |       +-- Luna.unity
|   |       +-- Sol.unity
|   |   +-- _Tests/
|   |       +-- EscenaPruebaProfe.unity
|   |       +-- Main_VR_TEST.unity
|   +-- Scripts/
|   |   +-- Core/                    <- Managers, portales, bootstrap, conectores escena
|   |   +-- Interaction/             <- Rocas, spawner, teleporter, proxies planetas
|   |   +-- Planets/                 <- Orbitas, Kepler, gravedad, configuracion
|   |   +-- UI/                      <- Wrist Menu, data cards, HUD, labels
|   +-- Shaders/
|   +-- Textures/
+-- Settings/
|   +-- Mobile_RPAsset.asset         <- URP config Quest 3 (MSAA 4x, RenderScale 1.0)
|   +-- Mobile_Renderer.asset
|   +-- PC_RPAsset.asset             <- URP config PC (MSAA 4x, HDR color grading)
|   +-- PC_Renderer.asset
|   +-- DefaultVolumeProfile.asset   <- Post-process: Bloom, ACES, ColorAdj, Vignette
+-- XR/
+-- XRI/
```

---

## 3. Escenas del proyecto

### 3.1 Main_VR (Hub — sala de portales)

Escena persistente de entrada. El jugador se encuentra en una sala de cristal flotante en el espacio rodeada de asteroides y un agujero negro. Cuatro portales esféricos llevan a cada leccion. Contiene los tres singletons persistentes (GameManager, SceneController, AudioManager) y el `XRInteractionAudioBroadcaster`. El `MainVRSceneBootstrap` reposiciona al jugador en el spawn point al volver de cualquier leccion.

### 3.2 SolarSystem (Leccion 1)

Diorama a escala del sistema solar con orbitas visibles generadas por `OrbitalSplineGenerator`. El jugador puede apuntar con la mano derecha a cualquier planeta — `PlanetPointer` muestra una `PlanetDataCard` flotante y `PlanetClickTeleporter` carga la escena de superficie al pulsar el gatillo. El cinturon de asteroides es generado proceduralmente por `AsteroidBeltGenerator`.

### 3.3 GravityLab (selector Leccion 2)

Escena intermedia que permite elegir el planeta/cuerpo al que viajar antes de cargar su escena de superficie.

### 3.4 Planetas — Leccion 2 (PlanetSurface)

Cada cuerpo del Sistema Solar tiene su propia escena. `PlanetSceneSetup` aplica la gravedad del `PlanetConfig` y configura el entorno al cargar la escena. `LocalGravityModifier` ajusta `Physics.gravity` y lo restaura al salir. `GravityHUDDisplay` muestra un panel world-space con el nombre del planeta, la gravedad y una instrucción. El jugador puede lanzar rocas procedurales (`ProceduralPebble`) y observar la caida. `GrabbableCubeSpawner` reaparece objetos si caen fuera del escenario. `PlayerDeathHandler` recarga la escena si el jugador cae demasiado o entra en una zona de muerte.

| Escena | Cuerpo | Gravedad (m/s²) | Config SO |
|--------|--------|-----------------|-----------|
| `Mercurio.unity` | Mercurio | -3.70 | `PlanetConfig_Mercury.asset` |
| `Venus.unity` | Venus | -8.87 | `PlanetConfig_Venus.asset` |
| `Tierra.unity` | Tierra | -9.81 | `PlanetConfig_Earth.asset` |
| `Luna.unity` | Luna | -1.62 | `PlanetConfig_Moon.asset` |
| `Marte.unity` | Marte | -3.71 | `PlanetConfig_Mars.asset` |
| `Jupiter.unity` | Jupiter | -24.79 | `PlanetConfig_Jupiter.asset` |
| `Saturno.unity` | Saturno | -10.44 | `PlanetConfig_Saturn.asset` |
| `Urano.unity` | Urano | -8.87 | `PlanetConfig_Uranus.asset` |
| `Neptuno.unity` | Neptuno | -11.15 | `PlanetConfig_Neptune.asset` |
| `Pluton.unity` | Pluton | -0.62 | `PlanetConfig_Pluto.asset` |
| `Sol.unity` | Sol | -274.0 | `PlanetConfig_Sun.asset` |

### 3.5 KeplerLab — Leccion 3 (tres subscenas)

El laboratorio de Kepler se divide en tres escenas independientes, una por cada ley:

| Escena | Ley | Mecanica |
|--------|-----|----------|
| `KeplerLab 1.unity` | 1a Ley — Orbitas elipticas | El jugador agarra un planeta, lo suelta y `OrbitalLauncher` calcula los elementos keplerianos desde posicion y velocidad de suelta. `OrbitalMover` mueve el planeta por la espline eliptica resultante. Wrist menu en mano izquierda muestra datos orbitales. |
| `KeplerLab 2.unity` | 2a Ley — Areas iguales en tiempos iguales | `KeplerLab2Controller` gestiona una ventana de captura con el boton A del controlador derecho. `KeplerAreaVisualizer` dibuja triangulos ("quesitos") de las areas barridas simultaneamente por todos los orbitadores. `KeplerLabOrbiter` pausa solo la simulacion (no `Time.timeScale`) para que el jugador pueda comparar las areas mientras sigue moviendose. |
| `KeplerLab 3.unity` | 3a Ley — T² ∝ a³ | El jugador suelta planetas a distintas distancias del Sol. `OrbitalDataCard` aparece encima de cada planeta con semieje mayor y periodo calculados. `KeplerLab3SceneConnector` muestra paneles de introduccion y explicacion. |

Todos los sub-laboratorios comparten un Wrist Menu especifico (`WristMenuControllerKepler`) y un `LessonSceneBootstrap` que reposiciona al jugador al cargar la escena.

### 3.6 Sandbox (Leccion 4)

Modo libre. El jugador crea planetas con el Wrist Menu, los mueve con las manos y puede lanzar asteroides para destruirlos (explosion + particulas + sonido via `AudioManager`). `SandboxSceneConnector` conecta el menu con `SceneController` y `GameManager`.

---

## 4. Arquitectura base

### Managers persistentes (Singletons)

Tres singletons que sobreviven todas las transiciones de escena via `DontDestroyOnLoad`. Se acceden directamente por `Manager.Instance` — **no usar FindObjectOfType ni ServiceLocator**. No crear nuevos singletons fuera de estos tres.

| Manager | Responsabilidad |
|---------|-----------------|
| `GameManager` | Estado global (`GameState` enum). Evento `OnGameStateChanged`. |
| `SceneController` | Carga asincrona con fade-to-black WorldSpace. Previene freeze en VR. |
| `AudioManager` | Musica shuffle/fade, SFX 2D UI, SFX 3D espacial instanciado. |

### Scene Connectors

Cada escena de leccion tiene un conector dedicado que une el Wrist Menu con los managers globales. Deben colocarse en un GameObject de servicio (p.ej. `Svc_SceneConnector`).

| Conector | Escena |
|----------|--------|
| `SolarSystemSceneConnector` | SolarSystem |
| `KeplerSceneConnector` | KeplerLab 1 |
| `KeplerLab2SceneConnector` | KeplerLab 2 |
| `KeplerLab3SceneConnector` | KeplerLab 3 |
| `SandboxSceneConnector` | Sandbox |

### Comunicacion entre sistemas

| Patron | Cuando usarlo |
|--------|---------------|
| **Manager.Instance** | Llamadas a los tres managers core |
| **C# Events** (`event Action<T>`) | Notificaciones cross-sistema (OnGameStateChanged, OnPauseStateChanged) |
| **Inspector** `[SerializeField]` | Dependencias de escena entre MonoBehaviours |
| **ScriptableObjects** | Datos de planetas (`PlanetConfig`) — config sin depender de escena |
| **SessionContext** | Datos estaticos de bajo acoplamiento cross-escena (p.ej. spawn override) |

### DontDestroyOnLoad — regla critica

Todos los managers deben ser **GameObjects raiz** (sin padre). Llamar siempre `transform.SetParent(null)` antes de `DontDestroyOnLoad(gameObject)` en `Awake`.

---

## 5. Modos de juego

### Leccion 1 — Diorama Solar

Objetivo: entender las proporciones y distancias del Sistema Solar. El jugador apunta con la mano derecha a cada planeta para ver su ficha de datos y puede teletransportarse a su superficie pulsando el gatillo.

### Leccion 2 — Superficies Planetarias

Objetivo: comparar la gravedad en distintos cuerpos del Sistema Solar. El jugador lanza rocas procedurales y observa la diferencia de caida. El HUD muestra el valor de gravedad del planeta actual.

### Leccion 3 — Laboratorio de Kepler

Objetivo: visualizar las tres leyes de Kepler de forma interactiva y progresiva a traves de tres subscenas independientes.

### Leccion 4 — Sandbox

Objetivo: experimentacion libre. Crear planetas con el Wrist Menu, moverlos con las manos, lanzar asteroides. Los planetas con suficiente impacto explotan con particulas y sonido.

---

## 6. Flujos detallados

### 6.1 Flujo de navegacion entre escenas

```text
Main_VR (sala portales)
    -> Jugador camina fisicamente dentro de un portal (LessonPortal detecta camera position)
    -> LessonPortal.CheckActivation() -> SceneController.LoadScene(sceneName, newState)
    -> Fade a negro -> LoadSceneAsync -> Activar escena -> GameManager.SetState(newState)
    -> Fade entrada -> LessonSceneBootstrap reposiciona jugador en spawn point -> escena activa
```

**Importante:** la activacion del portal es por posicion de la camara/cabeza, NO por controladores. El collider de activacion es un SphereCollider ajustado al visual del portal.

### 6.2 Flujo de pausa

```text
Jugador pulsa boton Pausa en Wrist Menu
    -> WristMenuController.OnPauseButtonPressed()
    -> TimeController.TogglePause()
    -> Time.timeScale = 0  (freeze planetas, orbitas, fisica)
    -> VR tracking sigue funcionando (corre a nivel OS, no Unity)
    -> TimeController.OnPauseStateChanged(true) -> UI actualiza icono
```

En KeplerLab 2 la pausa es distinta: `KeplerLabOrbiter.Pause()` detiene solo los orbitadores sin tocar `Time.timeScale`, preservando la locomotion VR.

### 6.3 Flujo de orbita en KeplerLab

```text
Jugador agarra un planeta (XRGrabInteractable)
    -> Al soltar: OrbitalLauncher.OnSelectExited()
    -> Calcula elementos keplerianos (semieje, excentricidad, periodo) desde posicion + velocidad
    -> OrbitalMover recibe los elementos -> OrbitalSplineGenerator genera la espline eliptica
    -> SplineAnimate mueve el planeta en bucle -> OrbitLineRenderer dibuja la orbita
    -> (Ley 3) OrbitalDataCard aparece encima del planeta con datos T y a
```

### 6.4 Flujo de destruccion sandbox

```text
Jugador agarra asteroide y lo lanza contra un planeta
    -> SandboxDestruction detecta colision con velocidad > umbral
    -> Destroy(planeta) + Instantiate(VFX_Explosion) + AudioManager.PlayExplosionSound()
    -> Particulas se auto-destruyen
```

### 6.5 Flujo de muerte/respawn

```text
Jugador cae por debajo de _fallThreshold (o entra en zona de muerte)
    -> PlayerDeathHandler detecta la condicion
    -> SceneController.LoadScene(escenaActual) -> fade + reload + LessonSceneBootstrap
```

---

## 7. Shaders y rendering

### URP Assets configurados

| Parametro | Mobile (Quest 3) | PC (Editor/Link) |
|-----------|-----------------|-----------------|
| MSAA | 4x | 4x |
| Render Scale | 1.0 | 1.2 |
| HDR | Activo | Activo |
| Shadow Distance | 20 m | 50 m |
| Soft Shadows | No | Si |
| Color Grading | LDR | HDR (ACES, LUT 33) |

### Post-Processing (DefaultVolumeProfile)

| Efecto | Configuracion |
|--------|---------------|
| Bloom | Threshold 0.9, Intensity 0.4, Scatter 0.7, HQ On |
| Tonemapping | ACES |
| Color Adjustments | PostExposure +0.3, Contrast +15, Saturation +20 |
| Vignette | Intensity 0.2, Smoothness 0.5 |
| Motion Blur | Desactivado (mareos en VR) |
| Depth of Field | Desactivado (mareos en VR) |

### Notas de materiales

- El **Sol** usa shader `URP/Unlit` (no recibe luz, emite su propia).
- Los **planetas gaseosos** (Jupiter, Saturno, Urano, Neptuno) usan Layer collision matrix para que las rocas los atraviesen.
- El **suelo de cristal** de Main_VR usa el shader `Unity-URP-GlassShader` con Surface Type Transparent, Smoothness 0.95.
- `Fixed Foveated Rendering` (Level High) activo via OpenXR Meta Quest features para ganar ~20% GPU.

---

## 8. Inventario de assets

| Categoria | Ruta | Estado |
|-----------|------|--------|
| ScriptableObjects planetas | `_Project/Assets/ScriptableObjects/PlanetConfig_*.asset` | ✅ 11 assets creados |
| Modelos planetas 3D | `Assets/Planets of the Solar System 3D/` | ✅ Importado |
| Asteroides 3D | `Assets/Asteroids/` | ✅ Importado |
| Agujero negro URP | `Assets/Blackhole_URP_Unity-6/` | ✅ Importado |
| Cristal roto VR | `Assets/Broken Glass VR/` | ✅ Importado |
| Galaxy Skybox | `Assets/GalaxyBox2/` | ✅ Importado |
| Musica (Interstellar) | `_Project/Audio/Music/InterstellarComplete/` | ✅ 26 pistas MP3 |
| SFX interaccion | `_Project/Audio/SFX/Interaction/` | ✅ break.wav, hit*.wav, pickup*.wav |
| Shader cristal | `_Project/Materials/UnityURPGlassShader/` | ✅ Importado |
| UI Kit espacio | `Assets/Space_Exploration_GUI_Kit/` | ✅ Importado |
| Prefabs del proyecto | `_Project/Prefabs/` | ✅ Interaction, Locomotion, Planets, UI, VFX |

---

## 9. Catalogo de scripts

### Core — `Assets/_Project/Scripts/Core/`

| Script | Responsabilidad |
|--------|-----------------|
| `GameManager.cs` | Singleton DontDestroyOnLoad. Enum `GameState` (5 valores). Evento `OnGameStateChanged`. |
| `SceneController.cs` | Singleton DontDestroyOnLoad. Carga asincrona con fade WorldSpace. Guard doble-llamada. |
| `AudioManager.cs` | Singleton DontDestroyOnLoad. Musica shuffle Fisher-Yates, crossfade. SFX 3D instanciado en posicion. |
| `GameState.cs` | Definicion del enum `GameState` (MainMenu, SolarSystem, PlanetSurface, KeplerLab, Sandbox). |
| `TimeController.cs` | Pause/Resume via `Time.timeScale`. No singleton — referencia Inspector desde WristMenu. |
| `SessionContext.cs` | Clase estatica con datos cross-escena de bajo acoplamiento (posicion spawn override). |
| `LessonPortal.cs` | Portal esfera. Detecta entrada por posicion de camara. Carga escena via SceneController. Hum de proximidad. |
| `SpaceAmbientController.cs` | Rota agujero negro y skybox. Spawna asteroides con exclusion de plataforma. |
| `AsteroidFlyBy.cs` | Mueve y tumba un asteroide en linea recta. Se auto-destruye al expirar el lifetime. |
| `PortalRotator.cs` | Rota un portal sobre sus tres ejes a velocidad configurable. |
| `BillboardFace.cs` | Hace que un TextMeshPro 3D mire siempre al jugador (texto legible). |
| `SpaceFireflies.cs` | Efecto ambiental de pixeles flotantes con brillo suave distribuidos en espacio relativo a la camara. |
| `PlayerDeathHandler.cs` | Detecta caida bajo umbral Y o entrada en zonas de muerte y recarga la escena via SceneController. |
| `MainVRSceneBootstrap.cs` | Reposiciona al jugador en el spawn point de Main_VR al cargar la escena o volver de una leccion. |
| `LessonSceneBootstrap.cs` | Reposiciona al jugador en el spawn point al cargar cualquier escena de leccion. |
| `OrbitalPauseController.cs` | Pausa/reanuda todos los SplineAnimates y rotaciones planetarias de la escena SolarSystem. |
| `OrbitVisibilityController.cs` | Muestra/oculta las lineas de orbita de todos los planetas de la escena (toggle de grupo). |
| `SolarSystemSceneConnector.cs` | Conecta eventos del WristMenu con SceneController y GameManager en la escena SolarSystem. |
| `KeplerSceneConnector.cs` | Conector de escena para KeplerLab 1. |
| `KeplerLab2SceneConnector.cs` | Conector de escena para KeplerLab 2. Gestiona spawning y UI de captura de areas. |
| `KeplerLab3SceneConnector.cs` | Conector de escena para KeplerLab 3. Activa OrbitalDataCard al soltar planetas y muestra paneles de la 3a Ley. |
| `SandboxSceneConnector.cs` | Conecta eventos del WristMenu con SceneController y GameManager en la escena Sandbox. |
| `XRInteractionAudioBroadcaster.cs` | Componente persistente que engancha todos los XRBaseInteractable de cada escena para reproducir sonidos de grab/drop/hold automaticamente sin configuracion por objeto. |

### Interaction — `Assets/_Project/Scripts/Interaction/`

| Script | Responsabilidad |
|--------|-----------------|
| `GrabbableCubeSpawner.cs` | Spawna un objeto agarrable con fisica sobre una superficie de referencia. Reaparece si cae por debajo de un umbral. |
| `PhysicsImpactSoundEmitter.cs` | Reproduce un sonido de impacto aleatorio via AudioManager al colisionar con velocidad relativa suficiente. Se anade automaticamente a los cubos spawneados. |
| `PlanetClickTeleporter.cs` | Lanzador de rayos desde el controlador derecho. Carga la escena del planeta al pulsar el gatillo sobre un `PlanetSceneLink`. |
| `PlanetPointer.cs` (UI) | Lanzador de rayos desde la mano derecha para mostrar `PlanetDataCard` y etiqueta 3D al apuntar a un `PlanetProxy`. |
| `PlanetProxy.cs` | Contenedor de datos del planeta (icono, nombre) leido por PlanetPointer al hacer hover. |
| `PlanetSceneLink.cs` | Dato ligero que almacena el nombre de escena a cargar. Leido por PlanetClickTeleporter. |
| `ProceduralPebble.cs` | Genera una malla de piedra low-poly convexa en runtime desde un icosaedro jitteado. Cada instancia es unica via seed aleatoria. |

### Planets — `Assets/_Project/Scripts/Planets/`

| Script | Responsabilidad |
|--------|-----------------|
| `PlanetConfig.cs` | ScriptableObject con parametros del planeta: gravedad, nombre ES/EN, nombre de escena, colores de entorno. |
| `PlanetSceneSetup.cs` | Aplica gravedad, cielo, luz y niebla del `PlanetConfig` al cargar una escena de superficie. |
| `LocalGravityModifier.cs` | Ajusta `Physics.gravity` segun el `PlanetConfig` y lo restaura al destruirse el componente. |
| `PlanetRotation.cs` | Gira un planeta continuamente alrededor de un eje configurable. |
| `SolarSystemSetup.cs` | Escala todos los planetas del diorama al cargar la escena. Posiciones relativas al Sol en el Editor. |
| `AsteroidBeltGenerator.cs` | Genera un anillo de asteroides en orbitas elipticas alrededor del Sol. |
| `OrbitalSplineGenerator.cs` | Genera la espline eliptica kepleriana sobre un SplineContainer. Usada por OrbitLineRenderer y SplineAnimate. |
| `OrbitalMover.cs` | Mueve un planeta por la espline kepleriana via SplineAnimate. Delega la geometria a OrbitalSplineGenerator y el dibujado a OrbitLineRenderer. |
| `OrbitalLauncher.cs` | Detecta la suelta del planeta, calcula elementos keplerianos desde posicion+velocidad y los pasa a OrbitalMover. |
| `OrbitLineRenderer.cs` | Dibuja la linea de orbita sobre el SplineContainer via LineRenderer. Expone `Redraw()` y `Hide()`. |
| `KeplerOrbitSplineGenerator.cs` | Variante de OrbitalSplineGenerator para KeplerLab: genera la espline desde la posicion de suelta en vez de Start. |
| `KeplerLabOrbiter.cs` | Orbita kepleriana autocontenida para KeplerLab 2. Pause/Resume propios sin afectar `Time.timeScale`. Dibuja trayectoria pintada. |
| `KeplerLab2Controller.cs` | Controlador del lab de la 2a Ley. Ventana de captura activada con boton A. Reporta areas integradas en panel world-space. |
| `KeplerAreaVisualizer.cs` | Visualiza triangulos de area barrida ("quesitos") entre el Sol y el planeta para demostrar la 2a Ley. |

### UI — `Assets/_Project/Scripts/UI/`

| Script | Responsabilidad |
|--------|-----------------|
| `WristMenuController.cs` | Menu de muneca XR para SolarSystem y Sandbox. Pause, spawner planetas, ajuste masas. |
| `WristMenuControllerKepler.cs` | Variante del Wrist Menu para las escenas KeplerLab. Muestra al levantar la palma izquierda. |
| `UIButtonAutoFeedback.cs` | Aplica hover visual (glow) y sonido hover automaticamente a todos los botones hijos de un Canvas. |
| `PlanetDataCard.cs` | Tarjeta flotante world-space con datos del planeta al apuntar a el con PlanetPointer. |
| `OrbitalDataCard.cs` | Tarjeta world-space encima del planeta con datos de la 3a Ley (semieje, periodo). Se activa al entrar en orbita. |
| `GravityHUDDisplay.cs` | Panel world-space generado en runtime al cargar una escena de superficie. Muestra nombre del planeta, gravedad e instruccion. No requiere prefab. |
| `BillboardLabel.cs` | Rota el objeto para siempre mirar a la camara del jugador (billboard). Solo eje Y para que las etiquetas no se inclinen. |
| `PlanetPointer.cs` | Ver seccion Interaction. |

---

## 10. Estado del proyecto

**Fecha:** 6 Mayo 2026

### Fases y deadlines

| Fase | Descripcion | Deadline | Estado |
|------|-------------|----------|--------|
| Fase 1 | Estructura, managers, portales, audio | 2026-04-26 | ✅ Completa |
| Fase 2 | Mecanicas, lecciones, UI | 2026-05-01 | ✅ Completa |
| Fase 3 | Sandbox, pulido, QA, APK | 2026-05-05 | ⚙️ En progreso |

### Estado por escena

| Escena | Estado |
|--------|--------|
| Main_VR | ✅ Portales, asteroides, agujero negro, fireflies, audio |
| SolarSystem | ✅ Diorama, orbitas, cinturon asteroides, pointer, teleporter |
| GravityLab | ⚙️ En progreso |
| KeplerLab 1 | ✅ Orbitas elipticas, wrist menu, datos orbitales |
| KeplerLab 2 | ⚙️ Captura de areas y quesitos ✅ — modificacion dinamica de velocidad pendiente (#60) |
| KeplerLab 3 | ⚙️ OrbitalDataCard y paneles explicativos ✅ — formula del periodo orbital pendiente (#59) |
| Planetas (x11) | ✅ Gravedad local, HUD, rocas procedurales, respawn |
| Sandbox | ⚙️ En progreso — mecanica basica funcional, falta pulido |

### Issues por miembro

**Gabriel (Gabiz053):**

| Issue | Estado |
|-------|--------|
| Git LFS configurado | ✅ |
| GameManager / SceneController / AudioManager | ✅ |
| Sala de Portales completa (Main_VR) | ✅ |
| TimeController | ✅ |
| XRInteractionAudioBroadcaster | ✅ |
| PlayerDeathHandler | ✅ |
| SpaceFireflies | ✅ |
| SandboxDestruction | ✅ (#65) |
| Integracion SFX en UI e Interacciones XR | ⚙️ En progreso (#66) |
| QA + Build APK | ❌ Ultima fase (#67) |

**Astrak00:**

| Issue | Estado |
|-------|--------|
| XR Origin y locomocion (#37) | ✅ |
| Plantilla superficie planetaria (#44) | ✅ |
| 8 escenas de planetas (#47) | ✅ |
| Teletransporte a superficies (#51) | ✅ |
| Gravedad local y agarre de rocas (#52) | ✅ |
| Quesitos 2ª Ley de Kepler (#62) | ✅ |
| Excepcion gaseosos: caida al vacio y niebla (#56) | ❌ Pendiente |

**mruiz54:**

| Issue | Estado |
|-------|--------|
| Estandarizacion URP y prefabs celestes (#38) | ✅ |
| Traslacion eliptica y rotacion propia (#49) | ✅ |
| Diorama Sistema Solar a escala (#45) | ✅ |
| Visualizador de orbitas (LineRenderer) (#54) | ✅ |
| Spawner de planetas en Wrist Menu (#57) | ✅ |
| Interaccion manual: agarrar y recalcular orbita (#58) | ✅ |
| 2ª Ley de Kepler: modificacion dinamica de velocidad (#60) | ⚙️ En progreso |

**susanasrez:**

| Issue | Estado |
|-------|--------|
| Iconos 2D (#40) | ✅ |
| Data Card panel cientifico (#41) | ✅ |
| Wrist Menu holografico (#48) | ✅ |
| Hover: textos flotantes 3D (#50) | ✅ |
| Sistema de respawn de objetos (#55) | ✅ |
| 3ª Ley de Kepler: formula del periodo orbital (#59) | ⚙️ En progreso |

---

## 11. Dependencias

| Paquete | Version | Uso |
|---------|---------|-----|
| `com.unity.xr.openxr` | 1.16.1 | Runtime OpenXR |
| `com.unity.xr.interaction.toolkit` | 3.0.10 | Interaccion XR |
| `com.unity.inputsystem` | 1.17.0 | Input System |
| `com.unity.render-pipelines.universal` | 17.0.4 | URP |
| `com.unity.ugui` | 2.0.0 | UI Canvas |
| `com.unity.xr.management` | 4.5.4 | XR Plugin Management |
| `com.unity.xr.oculus` | 4.5.4 | Oculus XR Plugin |
| `com.unity.textmeshpro` | 3.x | Texto 3D legible en VR |
