<!-- markdownlint-disable MD013 MD060 -->

# Convenciones del proyecto VR

Referencia obligatoria para mantener consistencia. **Todo asset, script o carpeta nuevo debe cumplir estas reglas.**

> **Ultima auditoria:** plantilla base activa - 2 escenas referencia - arquitectura por capas - convenciones estrictas.

---

## Tabla de contenidos

1. [Nombrado de assets](#1-nombrado-de-assets)
2. [Codigo CSharp](#2-codigo-csharp)
3. [Variables y campos CSharp](#3-variables-y-campos-csharp)
4. [Patrones de arquitectura](#4-patrones-de-arquitectura)
5. [Rendimiento -- target visores VR](#5-rendimiento----target-visores-vr)
6. [Estetica URP para VR](#6-estetica-urp-para-vr)
7. [Reglas generales](#7-reglas-generales)

---

## 1. Nombrado de assets

### 1.1 Carpetas

| Regla | Correcto | Incorrecto |
|-------|----------|------------|
| PascalCase | `Scripts/` | `scripts/` |
| Plural | `Materials/`, `Prefabs/` | `Material/`, `Prefab/` |
| Sin espacios | `XR/` | `My Folder/` |
| Raiz del proyecto | `_Project/` (con `_` para ordenar primero) | `Project/` |
| Carpetas vacias | Confirmar con `.gitkeep` | Dejar vacia (Git la ignora) |

### 1.2 GameObjects

| Prefijo | Uso | Ejemplos |
|---------|-----|----------|
| *(ninguno)* | Objetos estandar de Unity | `XR Origin`, `Main Camera`, `EventSystem` |
| `HUD_` | Regiones persistentes de pantalla | `HUD_Hotbar`, `HUD_Status`, `HUD_UndoRedo` |
| `Pnl_` | Paneles contenidos en una seccion | `Pnl_OptionsDropdown`, `Pnl_ConfirmDialog` |
| `Popup_` | Modales fullscreen | `Popup_ConfirmClearAll`, `Popup_SaveGame` |
| `Overlay_` | Fondos oscuros/transparentes | `Overlay_Background` |
| `Img_` | Imagenes y barras de progreso | `Img_BarBackground`, `Img_BarFill`, `Img_Preview` |
| `Btn_` | Botones (contiene hijo `Txt_` o `Icon_`) | `Btn_Grab`, `Btn_Settings`, `Btn_Confirm` |
| `Txt_` | Labels de TextMesh Pro | `Txt_Status`, `Txt_Title` |
| `Icon_` | Imagenes de icono dentro de botones | `Icon_Undo`, `Icon_Grab`, `Icon_Settings` |
| `Sld_` | Sliders | `Sld_MusicVolume` |
| `*_LayoutGroup` | Objetos con LayoutGroup component | `Hotbar_LayoutGroup`, `Dialog_LayoutGroup` |
| `Svc_` | GameObjects de servicio (sin visual) | `Svc_Audio`, `Svc_Interaction`, `Svc_SaveLoad` |
| PascalCase | Singletons / contenedores | `WorldContainer`, `ToolManager`, `MainCanvas` |

**Reglas obligatorias:**

- Cada `Btn_X` debe contener al menos un hijo `Txt_X` o `Icon_X` con el mismo sufijo.
- Todos los nombres en **ingles**.
- Sin espacios en nombres propios. Usar `_` para separar prefijo de nombre.
- Los objetos estandar de Unity mantienen su nombre por defecto (`Main Camera`, `Directional Light`, etc.).

### 1.3 Materiales

Prefijo obligatorio: **`M_`**

| Patron | Ejemplo |
|--------|---------|
| `M_{Nombre}` | `M_Floor.mat` |
| `M_{Categoria}{Tipo}` | `M_BlockStone.mat`, `M_UIBase.mat` |
| `M_{Sistema}{Nombre}` | `M_GridLines.mat` |

### 1.4 Prefabs

| Categoria | Prefijo | Ejemplo |
|-----------|---------|---------|
| Elementos XR | `XR_` | `XR_RigRoot.prefab`, `XR_HandMenu.prefab` |
| Objetos interactivos | `Interactable_` | `Interactable_Lever.prefab` |
| Elementos de locomocion | `Locomotion_` | `Locomotion_TeleportArea.prefab` |
| Efectos visuales | `VFX_` | `VFX_Impact.prefab` |
| Elementos UI | `UI_` | `UI_Panel.prefab` |

### 1.5 Texturas y modelos 3D

**Texturas:**

| Tipo | Prefijo | Ejemplo |
|------|---------|---------|
| Albedo / Diffuse | `T_` | `T_Floor_D.png` |
| Normal map | `T_` + sufijo `_N` | `T_Wall_N.png` |
| Mask (ORM) | `T_` + sufijo `_M` | `T_Metal_M.png` |
| UI icons | `Icon_` | `Icon_Grab.png` |
| UI sprites | `UI_` | `UI_Background.png` |

**Modelos 3D:**

| Regla | Ejemplo |
|-------|---------|
| Prefijo `Model_` + PascalCase | `Model_Tool.glb`, `Model_Prop.glb` |
| Ubicacion base | `_Project/Models/` |
| Formato recomendado | `.fbx` o `.glb` |

### 1.6 Audio

| Tipo | Prefijo | Ejemplo |
|------|---------|---------|
| Efectos de sonido | `SFX_` | `SFX_ButtonPress.wav` |
| Musica de fondo | `MUS_` | `MUS_MainTheme.mp3` |
| Voz / Narracion | `VO_` | `VO_Tutorial01.wav` |

**Subcarpetas:**

| Carpeta | Contenido |
|---------|-----------|
| `Audio/Music/` | Pistas de fondo |
| `Audio/SFX/UI/` | Sonidos de interfaz |
| `Audio/SFX/Interaction/` | Sonidos de objetos e interaccion |

### 1.7 Shaders

| Regla | Ejemplo |
|-------|---------|
| Ruta en menu de shader | `Shader "ProyectoVR/{Carpeta}/{Nombre}"` |
| Archivo en `_Project/Shaders/` | `WorldLit.shader`, `UIBillboard.shader` |
| Nombre del pass | `Name "ForwardLit"` |

**Reglas tecnicas:**

- `CBUFFER_START(UnityPerMaterial)` para compatibilidad con SRP Batcher.
- `#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE` para sombras.
- `#pragma multi_compile _ _SHADOWS_SOFT` para sombras suaves.
- `#pragma multi_compile_fog` si soporta fog.
- `MaterialPropertyBlock` en vez de material instances para propiedades per-object en runtime.

### 1.8 ScriptableObjects

| Regla | Ejemplo |
|-------|---------|
| `CreateAssetMenu` con ruta de proyecto | `[CreateAssetMenu(menuName = "ProyectoVR/Core/Game Config")]` |
| Nombre de archivo = `{Tipo}` o `{Tipo}_{Variante}` | `GameConfig.asset`, `WorldModeConfig_Default.asset` |
| Ubicacion | `_Project/Assets/ScriptableObjects/` |

### 1.9 Escenas

| Regla | Ejemplo |
|-------|---------|
| PascalCase con contexto | `Main_VR.unity` |
| Pantalla de titulo | `Title_Screen.unity` |
| Escena de test | `Test_Interaction.unity` |

---

## 2. Codigo CSharp

### 2.1 Archivos y namespaces

| Regla | Ejemplo |
|-------|---------|
| PascalCase | `GridManager.cs` |
| Nombre de archivo = nombre de clase | `InteractionPlacer.cs` -> `public class InteractionPlacer` |
| Namespace sigue ruta de carpeta | `namespace _Project.Scripts.Core` |

**Namespaces del proyecto:**

| Carpeta | Namespace |
|---------|-----------|
| `Scripts/XR/` | `_Project.Scripts.XR` |
| `Scripts/Locomotion/` | `_Project.Scripts.Locomotion` |
| `Scripts/Interaction/` | `_Project.Scripts.Interaction` |
| `Scripts/Core/` | `_Project.Scripts.Core` |
| `Scripts/Infrastructure/` | `_Project.Scripts.Infrastructure` |
| `Scripts/UI/` | `_Project.Scripts.UI` |

### 2.2 Atributos de clase

Cada MonoBehaviour lleva estos atributos **obligatorios**:

```csharp
[DisallowMultipleComponent]
[AddComponentMenu("ProyectoVR/{Carpeta}/{NombreClase}")]
public class MiScript : MonoBehaviour { }
```

Ejemplo real:

```csharp
[DisallowMultipleComponent]
[AddComponentMenu("ProyectoVR/Core/Grid Manager")]
public class GridManager : MonoBehaviour { }
```

### 2.3 Orden de regiones

Cada script sigue este orden de `#region`:

```csharp
#region Constants              // private const, static readonly
#region Inspector              // [SerializeField] con [Header] y [Tooltip]
#region Events                 // public event Action<T>
#region Cached Components      // o "#region State" -- referencias y estado runtime
#region Public API             // Propiedades y metodos publicos
#region Unity Lifecycle        // Awake, OnEnable, Start, Update, LateUpdate, OnDisable, OnDestroy
#region Internals              // Metodos privados auxiliares
#region Validation             // ValidateReferences() llamado desde Start()
```

### 2.4 Reglas de estilo

| Regla | Ejemplo |
|-------|---------|
| Cada `[SerializeField]` lleva `[Tooltip]` | `[Tooltip("Desc.")] [SerializeField] private float _value;` |
| Cada grupo de campos lleva `[Header]` | `[Header("Dependencies")]` |
| XML `<summary>` en toda clase publica | `/// <summary>Gestiona la rejilla.</summary>` |
| Yield cacheados como campo | `private readonly WaitForSeconds _wait = new(0.5f);` |
| No dejar `using` sin usar | Limpiar imports |
| `Debug.Log` en decisiones clave | `Debug.Log($"[ClassName] Action -- context.");` |

### 2.5 Convencion de Debug.Log

Cada servicio y controlador lleva `Debug.Log` estrategicos en puntos clave para facilitar diagnostico:

| Cuando | Ejemplo |
|--------|---------|
| Inicializacion de servicio | `Debug.Log($"[ScoreService] Initialized -- value: {_currentScore:F2}.");` |
| Cambio de estado relevante | `Debug.Log($"[ToolManager] Tool changed to {CurrentTool}.");` |
| Evento de juego importante | `Debug.Log("[ObjectiveService] Objective completed.");` |
| Toggle ON/OFF | `Debug.Log($"[BrushTool] Brush {(IsBrushActive ? "ON" : "OFF")}.");` |
| Operacion destructiva | `Debug.Log($"[WorldResetService] World reset complete -- destroyed {count} objects.");` |

**Formato:** `[ClassName] Message -- context.`

No meter `Debug.Log` en hot paths (`Update`, loops de coroutine).

### 2.6 Convencion de ValidateReferences()

Cada MonoBehaviour tiene un `ValidateReferences()` llamado desde `Start()` para comprobar dependencias. **Formato unico:**

```csharp
#region Validation ----------------------------------------

private void ValidateReferences()
{
    if (_fieldName == null)
        Debug.LogWarning("[ClassName] _fieldName is not assigned.", this);
}

#endregion
```

**Reglas:**

| Regla | Correcto | Incorrecto |
|-------|----------|------------|
| Nivel de log | `Debug.LogWarning` | `Debug.LogError` |
| Nombre del campo | `_fieldName` (nombre real del campo privado) | `FieldName`, `"No Collider found"` |
| Mensaje | `"[Clase] _campo is not assigned."` | `"[Clase] _campo not found!"` |
| Puntuacion | Punto final `.` | Exclamacion `!` |
| Contexto `this` | Siempre `this` como segundo argumento | Sin contexto |
| `OnValidate()` | **Prohibido** en MonoBehaviours | Solo `ScriptableObject` puede usarlo |
| Arrays vacios | `if (_arr == null \|\| _arr.Length == 0)` | Solo `_arr == null` |
| GetComponent | `"[Clase] ComponentType is not assigned."` | `"[Clase] ComponentType not found!"` |

---

## 3. Variables y campos CSharp

| Tipo | Convencion | Ejemplo |
|------|------------|---------|
| `[SerializeField]` private | `_camelCase` | `_gridManager`, `_audioService` |
| Private field | `_camelCase` | `_lastScore`, `_knocked` |
| Public property | PascalCase | `GridSize`, `IsWorldAnchored` |
| Constant (`const`) | `UPPER_SNAKE_CASE` | `MIN_FORWARD_SQR_MAG`, `RAY_DURATION` |
| Static readonly | `UPPER_SNAKE_CASE` | `GRID_MATRIX_ID`, `GRID_ENABLED_ID` |
| Local variable | `camelCase` | `snappedPosition`, `halfSize` |
| Method parameter | `camelCase` | `hitPose`, `playerCamera` |
| Public method | PascalCase | `AnchorWorld()`, `GetSnappedPosition()` |
| Private method | PascalCase | `OrientTowardsCamera()`, `Recalculate()` |
| Event (`Action`) | `On` + PascalCase | `OnToolChanged`, `OnScoreChanged` |
| Enum values | PascalCase con `_` si compuesto | `Build_Sand`, `Tool_Destroy`, `Mode_Creative` |
| Boolean properties | `Is`/`Can`/`Has` + PascalCase | `IsGridActive`, `CanUndo`, `IsBuildTool` |

---

## 4. Patrones de arquitectura

Tabla con ejemplos de referencia para la plantilla base:

| Patron | Uso | Ejemplo real |
|--------|-----|--------------|
| **Service Locator** | Resolucion de dependencias por interfaz sin singletons ni `Find*` | `ServiceLocator.Register<IAudioService>(this)` en `Awake`; `ServiceLocator.TryGet<IAudioService>(out _audioService)` en consumidor |
| **EventBus** (pub/sub tipado) | Comunicacion cross-sistema sin referencias directas | `EventBus.Publish(new ItemPlacedEvent(slotId))` -> cualquier suscriptor lo recibe |
| **C# Events** (`event Action<T>`) | Notificacion intra-capa, UI reactiva | `IScoreService.OnScoreChanged` -> `HUDScore.SetValue` |
| **Llamada directa** | Acoplamiento estrecho dentro de la misma capa | `InteractionController` -> `IGridManager.GetSnappedPosition()` |
| **Inspector** `[SerializeField]` | Inyeccion de dependencias para MonoBehaviours de escena | Toda seccion `#region Inspector` en scripts de escena |
| **Command Pattern** | Undo/Redo | `IUndoableAction` -> `PlaceItemAction` / `RemoveItemAction` |
| **Facade** | Simplificar acceso a subsistema | `InteractionFacade` envuelve input y feedback |
| **ScriptableObject data** | Config compartida sin depender de escena | `GameConfigSO`, `InteractionConfigSO`, `LocomotionConfigSO` |
| **Static context** | Dato cross-escena sin singletons | `SessionContext.SelectedMode` |
| **Internal static helper** | Logica compartida entre Commands sin estado | `PlacementAction.PrecomputePose()` |
| **ServiceLocator.TryGet en Awake** | Servicios de escena desde prefabs instanciados | `ItemSpawn`, `ItemDestroy`, `ImpactFeedback` resuelven interfaces en `Awake` |
| **Prefab-owns-feedback** | Audio y VFX viven en el prefab, no en el caller | `ItemSpawn` reproduce place sounds/VFX; `ItemDestroy` reproduce break sounds/VFX |
| **OnClick directo** | Botones toggle que no seleccionan herramienta | `Btn_Menu.OnClick -> MenuController.ToggleMenu()` |
| **OnClick con int param** | Seleccion indexada desde botones UI | `Btn_ModeA.OnClick -> ModeSelector.SelectMode(0)` |
| **Scene transition** | Carga de escena con fade y dato estatico pre-escrito | `ModeSelector` escribe `SessionContext.SelectedMode`, luego `SceneTransitionService.TransitionTo("Main_VR")` |
| **DontDestroyOnLoad + ServiceLocator** | Servicio cross-escena | `SceneTransitionService` se registra como `ISceneTransitionService` y persiste |
| **JSON persistence** | Guardado/carga de estado del mundo a disco | `SaveLoadService` serializa `SessionSaveData` via `JsonUtility` |

---

## 5. Rendimiento -- target visores VR

Reglas obligatorias para estabilidad en visores VR Standalone (Meta Quest 3 y similares):

| Regla | Por que | Como |
|-------|---------|------|
| **Objetivo fijo de framerate** | VR exige latencia baja para evitar discomfort | Diseñar para **72fps minimo** y perfil opcional a **90fps** |
| **Single Pass Instanced** | Reduce costo de render estereo | Activar Stereo Rendering Mode = Single Pass Instanced |
| **Reducir transparencias** | Overdraw mata GPU en VR movil | Minimizar UI alpha-blended y particulas fullscreen |
| **Evitar PostFX costosos** | Alto impacto por doble ojo | Mantener post-procesado minimo (Bloom leve, sin efectos pesados) |
| **No GetComponent en Update** | Presion CPU y GC por frame | Cachear en `Awake()` / `Start()` |
| **No Find/FindObjectOfType runtime** | Barridos O(n) penalizan frame time | Usar `[SerializeField]`, referencias directas y Service Locator |
| **No allocations en hot paths** | GC spikes causan judder | Reutilizar colecciones y buffers |
| **Canvas world-space optimizado** | UI VR puede disparar batches | Separar canvas estaticos/dinamicos y reducir rebuilds |
| **Baked lighting cuando aplique** | Menos costo en GPU movil | Hornear luces para entornos estaticos |
| **Occlusion culling + LODs** | Reduce draw calls y tris | Configurar LODGroup y culling por sala/sector |
| **Limitar sombras en tiempo real** | Sombras estereo son costosas | Usar pocas luces con sombras, resolucion moderada |
| **Perfilado continuo en device** | Editor no refleja costo real VR | Medir con Unity Profiler + OVR Metrics / herramientas del visor |

---

## 6. Estetica URP para VR

| Regla | Configuracion |
|-------|---------------|
| **Materiales sobrios** | Evitar shaders complejos en objetos frecuentes |
| **Texturas comprimidas adecuadas** | ASTC para Android VR (segun tipo de mapa) |
| **Sombras estables** | Limitar distancia y cascadas para evitar shimmering |
| **UI legible** | Tamaño y contraste pensados para lectura a distancia en HMD |
| **Escala real** | Validar medidas en metros para confort y presencia |
| **Iluminacion coherente** | Priorizar consistencia visual sobre efectos costosos |

---

## 7. Reglas generales

### Obligatorio

- Todos los nombres en **ingles**.
- Sin espacios en nombres de assets o carpetas.
- Sin caracteres especiales (solo letras, numeros, `_`).
- Todo asset propio del juego debe ubicarse dentro de `Assets/_Project/`.
- No crear carpetas custom en la raiz de `Assets/` (fuera de `_Project`).
- Cada asset tiene su `.meta` -- **nunca mover ni renombrar assets fuera de Unity Editor**.
- Carpetas vacias confirmadas con `.gitkeep` cuando sea necesario versionarlas.
- Scripts XR en `Scripts/XR/`, locomocion en `Scripts/Locomotion/`, interaccion en `Scripts/Interaction/`.
- Mantener `Core/`, `Infrastructure/` y `UI/` como capas obligatorias.
- Cada `[SerializeField]` lleva `[Tooltip]`.
- Cada grupo de `[SerializeField]` lleva `[Header]`.
- Cada MonoBehaviour tiene `ValidateReferences()` llamado en `Start()`.
- Cachear yield objects de coroutines como campos.
- Usar `#region` siguiendo el orden de la plantilla.
- Usar `[DisallowMultipleComponent]` en cada MonoBehaviour.
- Usar `[AddComponentMenu("ProyectoVR/...")]` en cada MonoBehaviour para consistencia heredada de plantilla.
- Cada clase publica tiene XML `<summary>`.
- Cada `using` no utilizado se elimina.

### Prohibido

- `GetComponent` en `Update()`.
- `Find()` o `FindObjectOfType()` para flujo normal de runtime.
- Allocations en hot paths (`Update`, loops de coroutine).
- Hardcodear magic numbers sin `const` o `[SerializeField]`.
- Duplicar referencias de escena en prefabs si puede resolverse por interfaces/servicios.
- `OnValidate()` en MonoBehaviours (solo permitido en ScriptableObject).
- `Debug.LogError` dentro de `ValidateReferences()` (usar `Debug.LogWarning`).

### Mantenimiento de documentacion

Cada cambio estructural (scripts, prefabs, carpetas, escena, paquetes) debe reflejarse en:

- `README.md` -- estructura, estado, dependencias, inventario.
- `CONVENTIONS.md` -- reglas, patrones y criterios de estructura.

**La documentacion desactualizada se considera un bug igual que el codigo roto.**
