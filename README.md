<!-- markdownlint-disable MD013 MD060 -->

# Juego VR — Sistema Solar Educativo

![Unity](https://img.shields.io/badge/Unity-6-black?style=flat-square&logo=unity)
![OpenXR](https://img.shields.io/badge/OpenXR-Enabled-blue?style=flat-square)
![XRI](https://img.shields.io/badge/XR_Interaction_Toolkit-3.x-5b9bd5?style=flat-square)
![URP](https://img.shields.io/badge/Render-URP-red?style=flat-square)
![Device](https://img.shields.io/badge/Target-Meta_Quest_3-green?style=flat-square&logo=meta)

Videojuego educativo VR del Sistema Solar para **Meta Quest 3** (Android Standalone). El jugador navega entre cuatro modos de aprendizaje cruzando portales físicos en una sala de cristal flotante en el espacio.

| Dato | Valor |
|------|-------|
| **Hardware de referencia** | Meta Quest 3 |
| **Motor** | Unity 6 (6000.0.x) |
| **Pipeline** | Universal Render Pipeline (URP) |
| **XR Runtime** | OpenXR |
| **Toolkit** | XR Interaction Toolkit 3.x |
| **Input** | Input System 1.8+ |
| **Plataforma** | Android (Standalone VR) |
| **Version** | 0.3.0 |

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
|   |   +-- KeplerLab.unity          <- Leccion 3  (GameState: KeplerLab)
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
|   +-- Scripts/
|   |   +-- Core/                    <- Managers, portales, asteroides, tiempo
|   |   +-- Interaction/             <- Rocas, destruccion sandbox
|   |   +-- UI/                      <- Wrist Menu, data cards, spawner
|   |   +-- XR/                      <- XR rig, locomotion (Astrak00)
|   |   +-- Locomotion/
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

Escena persistente de entrada. El jugador se encuentra en una sala de cristal flotante en el espacio rodeado de asteroides y un agujero negro. Cuatro portales esféricos le llevan a cada leccion. Contiene los tres singletons persistentes (GameManager, SceneController, AudioManager).

### 3.2 SolarSystem (Leccion 1)

Diorama a escala del sistema solar con orbitas visibles. El jugador puede caminar alrededor y observar las proporciones reales. (Tarea de mruiz54.)

### 3.3 Planetas — Leccion 2 (PlanetSurface)

Cada cuerpo del Sistema Solar tiene su propia escena con gravedad real. El jugador puede lanzar rocas y observar como la gravedad afecta la caida.

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

### 3.4 KeplerLab (Leccion 3)

El jugador puede cambiar masas de planetas, ajustar velocidades orbitales y observar las leyes de Kepler en tiempo real. (Tarea de mruiz54.)

### 3.5 Sandbox (Leccion 4)

Modo libre. El jugador spawna planetas y los mueve con las manos. Tambien puede lanzar asteroides para destruir planetas (explosion + particulas).

---

## 4. Arquitectura base

### Managers persistentes (Singletons)

Tres singletons que sobreviven todas las transiciones de escena via `DontDestroyOnLoad`. Se acceden directamente por `Manager.Instance` — **no usar FindObjectOfType ni ServiceLocator**. No crear nuevos singletons fuera de estos tres.

| Manager | Responsabilidad |
|---------|-----------------|
| `GameManager` | Estado global (`GameState` enum). Evento `OnGameStateChanged`. |
| `SceneController` | Carga asincrona con fade-to-black WorldSpace. Previene freeze en VR. |
| `AudioManager` | Musica shuffle/fade, SFX 2D UI, SFX 3D espacial instanciado. |

### Comunicacion entre sistemas

| Patron | Cuando usarlo |
|--------|---------------|
| **Manager.Instance** | Llamadas a los tres managers core |
| **C# Events** (`event Action<T>`) | Notificaciones cross-sistema (OnGameStateChanged, OnPauseStateChanged) |
| **Inspector** `[SerializeField]` | Dependencias de escena entre MonoBehaviours |
| **ScriptableObjects** | Datos de planetas (`PlanetConfigSO`) — config sin depender de escena |

### DontDestroyOnLoad — regla critica

Todos los managers deben ser **GameObjects raiz** (sin padre). Llamar siempre `transform.SetParent(null)` antes de `DontDestroyOnLoad(gameObject)` en `Awake`.

---

## 5. Modos de juego

### Leccion 1 — Diorama Solar

Objetivo: entender las proporciones y distancias del Sistema Solar. El jugador observa el diorama a escala con orbitas visibles y puede acercarse a cada planeta.

### Leccion 2 — Superficies Planetarias

Objetivo: comparar la gravedad en distintos cuerpos del Sistema Solar. El jugador lanza una roca desde la misma altura en cada planeta y observa como cae mas rapido o lento. El HUD muestra el tiempo de caida y el valor de gravedad.

### Leccion 3 — Laboratorio de Kepler

Objetivo: visualizar las leyes de Kepler. El jugador ajusta masas y velocidades y observa como cambian las orbitas en tiempo real.

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
    -> Fade entrada -> escena activa
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

### 6.3 Flujo de destruccion sandbox

```text
Jugador agarra asteroide y lo lanza contra un planeta
    -> SandboxDestruction detecta colision con velocidad > umbral
    -> Destroy(planeta) + Instantiate(VFX_Explosion) + AudioManager.PlayExplosionSound()
    -> Particulas se auto-destruyen
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
| `TimeController.cs` | Pause/Resume via `Time.timeScale`. No singleton — referencia Inspector desde WristMenu. |
| `LessonPortal.cs` | Portal esfera. Detecta entrada por posicion de camara. Carga escena via SceneController. Hum de proximidad. |
| `SpaceAmbientController.cs` | Rota agujero negro y skybox. Spawna asteroides con exclusion de plataforma. |
| `AsteroidFlyBy.cs` | Mueve y tumba un asteroide en linea recta. Se auto-destruye al expirar el lifetime. |
| `PortalRotator.cs` | Rota un portal sobre sus tres ejes a velocidad configurable. |
| `BillboardFace.cs` | Hace que un TextMeshPro 3D mire siempre al jugador (texto legible). |

### Interaction — `Assets/_Project/Scripts/Interaction/`

| Script | Responsabilidad |
|--------|-----------------|
| `SandboxDestruction.cs` | Detecta colision asteroide-planeta con velocidad minima. Explosion + VFX + SFX. *(Pendiente)* |
| `GravityTestRock.cs` | Rigidbody con respawn automatico y medicion de tiempo de caida. *(Astrak00)* |

### UI — `Assets/_Project/Scripts/UI/`

| Script | Responsabilidad |
|--------|-----------------|
| `WristMenuController.cs` | Menu de muneca XR. Pause, spawner planetas, ajuste masas. *(susanasrez)* |
| `PlanetDataCard.cs` | Tarjeta flotante con datos del planeta al acercarse. *(susanasrez)* |
| `PlanetSpawnerUI.cs` | Botones para crear planetas en Sandbox. *(susanasrez)* |

---

## 10. Estado del proyecto

**Fecha:** 25 Abril 2026

### Fases y deadlines

| Fase | Descripcion | Deadline | Estado |
|------|-------------|----------|--------|
| Fase 1 | Estructura, managers, portales, audio | 2026-04-26 | ✅ Scripts completos |
| Fase 2 | Mecanicas, lecciones, UI | 2026-05-01 | ⚙️ En progreso |
| Fase 3 | Sandbox, pulido, QA, APK | 2026-05-05 | ❌ Pendiente |

### Issues por miembro

**Gabriel (Gabiz053):**

| Issue | Estado |
|-------|--------|
| Git LFS configurado | ✅ |
| GameManager.cs | ✅ |
| SceneController.cs | ✅ |
| AudioManager.cs | ✅ |
| Sala de Portales (LessonPortal, SpaceAmbientController, AsteroidFlyBy, PortalRotator, BillboardFace) | ✅ Scripts — ⚙️ Montar Main_VR.unity |
| TimeController.cs | ✅ |
| SandboxDestruction.cs | ❌ Espera prefabs mruiz54 |
| SFX integracion | ❌ Espera Wrist Menu susanasrez |
| QA + Build APK | ❌ Ultima fase |

**Astrak00:** XR Origin ✅, escenas planetas ⚙️, gravedad local ⚙️

**mruiz54:** Prefabs 3D ⚙️, diorama solar ⚙️, orbitas ⚙️, interaccion manual ⚙️

**susanasrez:** Wrist Menu ⚙️, Data Cards ⚙️, iconos 2D ✅

---

## 11. Dependencias

| Paquete | Version | Uso |
|---------|---------|-----|
| `com.unity.xr.openxr` | 1.11.x+ | Runtime OpenXR |
| `com.unity.xr.interaction.toolkit` | 3.x | Interaccion XR |
| `com.unity.inputsystem` | 1.8.x+ | Input System |
| `com.unity.render-pipelines.universal` | 17.x | URP |
| `com.unity.ugui` | 2.x | UI Canvas |
| `com.unity.xr.management` | 4.x | XR Plugin Management |
| `com.unity.textmeshpro` | 3.x | Texto 3D legible en VR |

> Ajusta versiones segun compatibilidad con tu Unity 6 exacto.
