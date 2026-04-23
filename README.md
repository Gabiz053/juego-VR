<!-- markdownlint-disable MD013 MD060 -->

# VR Project Bootstrap -- Base Unity 6 + OpenXR

![Unity](https://img.shields.io/badge/Unity-6-black?style=flat-square&logo=unity)
![OpenXR](https://img.shields.io/badge/OpenXR-Enabled-blue?style=flat-square)
![XRI](https://img.shields.io/badge/XR_Interaction_Toolkit-3.x-5b9bd5?style=flat-square)
![URP](https://img.shields.io/badge/Render-URP-red?style=flat-square)
![Device](https://img.shields.io/badge/Target-Meta_Quest_3-green?style=flat-square&logo=meta)

Plantilla tecnica inicial para un juego VR Standalone en Unity 6. Esta base hereda el estilo de arquitectura, convenciones y estructura de UI del proyecto anterior, pero queda desacoplada de cualquier tematica especifica para que puedas construir una nueva experiencia desde cero.

| Dato | Valor |
|------|-------|
| **Hardware de referencia** | Meta Quest 3 |
| **Motor** | Unity 6 (6000.0.x) |
| **Pipeline** | Universal Render Pipeline (URP) |
| **XR Runtime** | OpenXR |
| **Toolkit** | XR Interaction Toolkit 3.x |
| **Input** | Input System + XR Input |
| **Plataforma** | Android (Standalone VR) |
| **Version** | 0.1.0-template |

---

## Tabla de contenidos

1. [Como abrir el proyecto](#1-como-abrir-el-proyecto)
2. [Estructura de carpetas](#2-estructura-de-carpetas)
3. [Escenas base](#3-escenas-base)
4. [Arquitectura base](#4-arquitectura-base)
5. [Gameplay -- plantilla](#5-gameplay----plantilla)
6. [Flujos detallados -- plantilla](#6-flujos-detallados----plantilla)
7. [Shaders y rendering](#7-shaders-y-rendering)
8. [Inventario de assets -- plantilla](#8-inventario-de-assets----plantilla)
9. [Catalogo de scripts -- plantilla](#9-catalogo-de-scripts----plantilla)
10. [Estado del proyecto](#10-estado-del-proyecto)
11. [Dependencias base](#11-dependencias-base)

---

## 1. Como abrir el proyecto

1. Instala **Unity 6** con modulos: Android Build Support, OpenXR Plugin, Universal RP.
2. Clona el repositorio:

    ```bash
    git clone <tu-repo-vr>.git
    ```

3. Abre la carpeta raiz en Unity Hub.
4. Configura Build Target a **Android**.
5. Escenas recomendadas para bootstrap:
    - `Assets/_Project/Scenes/Title_Screen.unity`
    - `Assets/_Project/Scenes/Main_VR.unity`
6. En Project Settings, valida OpenXR + Interaction Profiles del visor objetivo.

---

## 2. Estructura de carpetas

```text
Assets/
+-- _Project/
|   +-- Assets/
|   |   +-- Config/
|   |   +-- Fonts/
|   |   +-- ScriptableObjects/
|   +-- Audio/
|   |   +-- Music/
|   |   +-- SFX/
|   |       +-- UI/
|   |       +-- Interaction/
|   +-- Materials/
|   +-- Models/
|   +-- Prefabs/
|   |   +-- XR/
|   |   +-- Locomotion/
|   |   +-- Interaction/
|   |   +-- UI/
|   |   +-- VFX/
|   +-- Scenes/
|   |   +-- Title_Screen.unity
|   |   +-- Main_VR.unity
|   +-- Scripts/
|   |   +-- XR/
|   |   +-- Locomotion/
|   |   +-- Interaction/
|   |   +-- Core/
|   |   +-- Infrastructure/
|   |   +-- UI/
|   +-- Shaders/
|   +-- Textures/
+-- Settings/
|   +-- Mobile_RPAsset.asset
|   +-- Mobile_Renderer.asset
|   +-- PC_RPAsset.asset
|   +-- PC_Renderer.asset
+-- XR/
+-- XRI/
+-- InputSystem_Actions.inputactions
```

**Regla clave:** dentro de `Scripts/` se mantienen obligatoriamente `Core/`, `Infrastructure/` y `UI/`, y se reemplaza lo especifico de AR por carpetas VR como `XR/`, `Locomotion/` e `Interaction/`.

**Regla de organizacion:** todo asset propio del proyecto debe vivir bajo `Assets/_Project/`. Evita crear carpetas custom en la raiz de `Assets/`.

**Nota de base:** la estructura inicial se mantiene intencionalmente corta (similar al AR), y se extiende solo cuando aparezca una necesidad real.

---

## 3. Escenas base

### 3.1 Title_Screen

- Escena de entrada del proyecto.
- Menu de inicio, configuracion inicial de sesion y transicion a gameplay.
- Espacio recomendado para UI de acceso rapido (modo juego, ajustes, calibracion).

### 3.2 Main_VR

- Escena principal de runtime VR.
- Contendra rig XR, locomocion, interaccion y sistemas de juego.
- La jerarquia estructural de referencia se mantiene temporalmente igual a la plantilla original (ver `CONVENTIONS.md`, seccion 4).

### 3.3 Escenas por planeta (Sistema Solar)

Cada cuerpo del Sistema Solar tiene su propia escena con gravedad correcta, skybox propio y un test de caida de piedra ("drop rock") sobre una plataforma. Todas comparten la misma estructura runtime generada por `PlanetSceneBootstrap` a partir de un `PlanetConfigSO`.

| Escena | Cuerpo | Gravedad (m/s^2) | Config SO |
|--------|--------|------------------|-----------|
| `Mercurio.unity` | Mercurio | -3.70 | `PlanetConfig_Mercury.asset` |
| `Venus.unity` | Venus | -8.87 | `PlanetConfig_Venus.asset` |
| `Tierra.unity` | Tierra | -9.81 | `PlanetConfig_Earth.asset` (opcional) |
| `Luna.unity` | Luna | -1.62 | `PlanetConfig_Moon.asset` (opcional) |
| `Marte.unity` | Marte | -3.71 | `PlanetConfig_Mars.asset` |
| `Jupiter.unity` | Jupiter | -24.79 | `PlanetConfig_Jupiter.asset` |
| `Saturno.unity` | Saturno | -10.44 | `PlanetConfig_Saturn.asset` |
| `Urano.unity` | Urano | -8.87 | `PlanetConfig_Uranus.asset` |
| `Neptuno.unity` | Neptuno | -11.15 | `PlanetConfig_Neptune.asset` |
| `Pluton.unity` | Pluton | -0.62 | `PlanetConfig_Pluto.asset` |
| `Sol.unity` | Sol | -274.0 | `PlanetConfig_Sun.asset` |

Cada escena contiene:

- `Main Camera` temporal (se sustituira por el rig XR cuando llegue la locomocion).
- `Directional Light` tintada como el sol del planeta.
- `SceneManager` con `PlanetSceneBootstrap` que, al iniciar:
    1. Aplica `Physics.gravity.y` desde el `PlanetConfigSO`.
    2. Construye un skybox procedural (`Skybox/Procedural`) tintado.
    3. Instancia una plataforma circular con aro de seguridad.
    4. Spawnea la piedra `GravityTestRock` sobre un pedestal.
    5. Genera scenery de rocas low-poly alrededor de la plataforma (seed reproducible por planeta).

`Tierra.unity` y `Luna.unity` conservan la version minima con `GravitySettings` para no romper flujos ya probados; su migracion al bootstrap es trivial (anadir un `SceneManager` con el `PlanetConfig_*` correspondiente).

---

## 4. Arquitectura base

| Patron | Para que | Ejemplo de uso |
|--------|----------|----------------|
| **Service Locator** | Resolver servicios por interfaz | `ServiceLocator.TryGet<IAudioService>(out _audio)` |
| **EventBus** | Comunicacion desacoplada entre sistemas | `EventBus.Publish(new ItemGrabbedEvent(...))` |
| **C# Events** | UI reactiva intra-capa | `OnScoreChanged` -> `HUDScore.SetValue()` |
| **Command Pattern** | Undo/Redo o acciones reversibles | `IUndoableAction`, `PlaceAction`, `RemoveAction` |
| **ScriptableObject Data** | Config compartida | `GameConfig`, `InteractionConfig`, `LocomotionConfig` |
| **Static Context** | Datos cross-scene de bajo acoplamiento | `SessionContext.SelectedMode` |
| **Facade** | Simplificar subsistemas complejos | `GridManager` / `InteractionFacade` |

Principios del proyecto:

- Arquitectura event-driven para evitar acoplamientos fuertes.
- Servicios registrados por interfaz para pruebas y escalabilidad.
- Flujo de datos claro entre gameplay, UI y persistencia.
- Convenciones de nomenclatura estrictas para facilitar mantenimiento en equipo.

---

## 5. Gameplay -- plantilla

> Este bloque se deja intencionalmente como plantilla para documentar mecanicas reales cuando el juego avance.

### 5.1 Loop principal (pendiente)

<!-- TODO: Describe aqui el bucle principal de juego: objetivos, interacciones, estados de victoria/derrota, progresion. -->

### 5.2 Herramientas o habilidades del jugador (pendiente)

| Slot/Sistema | Estado | Notas de implementacion |
|--------------|--------|-------------------------|
| Pendiente | Draft | Definir en iteraciones de gameplay |

### 5.3 Sistemas de feedback (pendiente)

<!-- TODO: Audio, hapticos, VFX, diegetic UI, tutoriales contextuales. -->

---

## 6. Flujos detallados -- plantilla

> Esta seccion queda preparada para diagramar flujos reales del proyecto.

### 6.1 Flujo de input XR (pendiente)

```text
XR Input (Controller/Hands)
    -> Input Router
    -> Interaction Resolver
    -> Gameplay System
    -> UI/Feedback
```

### 6.2 Flujo de interaccion de objetos (pendiente)

```text
Ray/Direct Interactor
    -> Validacion de objetivo
    -> Accion (grab/use/place)
    -> Eventos + Feedback
```

### 6.3 Flujo de guardado/carga (pendiente)

```text
Request Save/Load
    -> SaveLoadService
    -> Serializacion
    -> Confirmacion UI
```

---

## 7. Shaders y rendering

Linea base recomendada:

- URP configurado para VR Standalone.
- OpenXR + render estereo optimizado.
- Materiales preparados para bajo costo en GPU movil.
- Evitar transparencias y overdraw innecesario en UI world-space.

<!-- TODO: Cuando existan shaders propios, documentar rutas, propiedades y reglas de uso. -->

---

## 8. Inventario de assets -- plantilla

| Categoria | Ruta sugerida | Estado |
|----------|----------------|--------|
| ScriptableObjects | `Assets/_Project/Assets/ScriptableObjects/` | Pendiente |
| ScriptableObjects | `Assets/_Project/Assets/ScriptableObjects/` | `PlanetConfig_{Mercury,Venus,Earth,Moon,Mars,Jupiter,Saturn,Uranus,Neptune,Pluto,Sun}.asset` |
| Prefabs XR | `Assets/_Project/Prefabs/XR/` | Pendiente |
| Prefabs UI | `Assets/_Project/Prefabs/UI/` | Pendiente |
| Materiales | `Assets/_Project/Materials/` | Pendiente |
| Audio | `Assets/_Project/Audio/` | Pendiente |
| Modelos | `Assets/_Project/Models/` | Pendiente |
| Scenery runtime | N/A | Procedural (ver `PlanetSceneBootstrap`) |

<!-- TODO: Sustituir "Pendiente" por inventario real conforme se creen assets. -->

---

## 9. Catalogo de scripts -- plantilla

> El catalogo se completara por capas cuando existan scripts reales.

### XR -- `Assets/_Project/Scripts/XR/`

| Script | Responsabilidad |
|--------|-----------------|
| Pendiente | Definir |

### Locomotion -- `Assets/_Project/Scripts/Locomotion/`

| Script | Responsabilidad |
|--------|-----------------|
| Pendiente | Definir |

### Interaction -- `Assets/_Project/Scripts/Interaction/`

| Script | Responsabilidad |
|--------|-----------------|
| Pendiente | Definir |

### Core -- `Assets/_Project/Scripts/Core/`

| Script | Responsabilidad |
|--------|-----------------|
| `PlanetConfigSO` | ScriptableObject con gravedad, colores de cielo, fog y parametros de scenery para cada cuerpo del Sistema Solar. |
| `PlanetSceneBootstrap` | Aplica gravedad, skybox procedural, luz key, plataforma y scenery al entrar a una escena de planeta. |
| `GravitySettings` | Setter minimo de `Physics.gravity.y` (usado en Tierra/Luna como version legada). |
| `MenuController` | Menu inicial en `Menu.unity` que permite saltar a cualquiera de las 11 escenas. |

### Interaction -- `Assets/_Project/Scripts/Interaction/`

| Script | Responsabilidad |
|--------|-----------------|
| `GravityTestRock` | Rigidbody con respawn automatico + medicion de tiempo de caida; funciona con o sin XR Grab Interactable. |
| `RespawnButton` | Pedestal mundial que respawnea la piedra al pulsar (OnMouseDown/OnTriggerEnter/Tecla R) y muestra HUD con planeta/gravedad/ultimo tiempo de caida. |

### Infrastructure -- `Assets/_Project/Scripts/Infrastructure/`

| Script | Responsabilidad |
|--------|-----------------|
| Pendiente | Definir |

### UI -- `Assets/_Project/Scripts/UI/`

| Script | Responsabilidad |
|--------|-----------------|
| Pendiente | Definir |

---

## 10. Estado del proyecto

### Base lista

- Estructura fisica inicial creada.
- Convenciones de desarrollo establecidas.
- Plantillas de documentacion listas para evolucionar.

### Trabajo a futuro

| Feature | Detalle |
|--------|---------|
| Locomocion | Snap Turn, Teleport, opcional Smooth Move |
| Interaction | Grab, Use, Socket, UI world-space |
| Performance | Perfilado GPU/CPU por escena objetivo |
| Persistence | Guardado de progreso/configuracion |
| QA XR | Matriz de pruebas por visor y framerate |

---

## 11. Dependencias base

| Paquete | Version recomendada | Uso |
|---------|----------------------|-----|
| `com.unity.xr.openxr` | 1.11.x o superior | Runtime OpenXR |
| `com.unity.xr.interaction.toolkit` | 3.x | Interaccion XR |
| `com.unity.inputsystem` | 1.8.x o superior | Input System |
| `com.unity.render-pipelines.universal` | 17.x | URP |
| `com.unity.ugui` | 2.x | UI Canvas |
| `com.unity.xr.management` | 4.x | XR Plugin Management |

> Ajusta versiones segun compatibilidad del editor Unity exacto que uses en el proyecto.
