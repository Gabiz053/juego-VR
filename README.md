<!-- markdownlint-disable MD013 MD060 -->

# Sistema Solar VR

Videojuego educativo de realidad virtual para Meta Quest 3. El jugador aparece en una sala de cristal flotando en el espacio y entra a cada lección cruzando un portal físico, andando.

| Dato | Valor |
|------|-------|
| **Hardware de referencia** | Meta Quest 3 |
| **Motor** | Unity 6 (6000.0.x) |
| **Pipeline** | Universal Render Pipeline (URP) 17.0.4 |
| **XR Runtime** | OpenXR 1.16.1 |
| **Toolkit** | XR Interaction Toolkit 3.0.10 |
| **Input** | Input System 1.17.0 |
| **Plataforma** | Android (Standalone VR) |
| **Versión** | 0.5.0 |

## Las cuatro lecciones

**Diorama solar.** Proporciones y distancias. Apuntas con la mano a un planeta para ver su ficha y aprietas el gatillo para teletransportarte a su superficie.

**Superficies planetarias.** Once cuerpos con su gravedad real. Lanzas rocas generadas por procedimiento y ves la diferencia de caída entre la Luna y Júpiter, con el valor de gravedad en el HUD.

**Laboratorio de Kepler.** Las tres leyes, una por subescena: órbitas elípticas, áreas iguales en tiempos iguales, y la relación entre periodo y semieje mayor.

**Sandbox.** Sin objetivo. Creas planetas desde un menú en la muñeca, los mueves con las manos y lanzas asteroides. Si el impacto es suficiente, el planeta revienta.

## Cómo abrirlo

El repositorio usa **Git LFS** para modelos, texturas y audio. Hay que instalarlo antes de clonar o los binarios llegan corruptos:

```bash
git lfs install
git clone https://github.com/Gabiz053/sistema-solar-vr.git
cd sistema-solar-vr
git lfs pull
```

Después: Unity 6 con Android Build Support, OpenXR Plugin y Universal RP, build target Android, y escena de entrada `Assets/_Project/Scenes/Main_VR.unity`.

Los pasos completos están en la [documentación técnica](docs/documentacion-tecnica.md#1-cómo-abrir-el-proyecto).

## Qué hay dentro

El proyecto son dieciocho escenas que tienen que compartir estado sin pisarse. La solución es un puñado de managers persistentes y un conector por escena, que es el único que sabe qué hay en ella: así una escena nueva se enchufa sin tocar el resto. Los objetos que sobreviven al cambio de escena están acotados a propósito, porque en VR una referencia colgando se traduce en un tirón que se nota en la cabeza.

La física de Kepler no está animada a mano. Los elementos keplerianos se derivan de la posición y la velocidad, y la línea de la órbita se dibuja a partir de ahí, así que cambiar la excentricidad cambia el movimiento y el dibujo a la vez.

## Documentación técnica

El detalle está en [`docs/documentacion-tecnica.md`](docs/documentacion-tecnica.md): las dieciocho escenas, la arquitectura de managers y conectores, los flujos de navegación y pausa, los shaders, el catálogo de scripts y el inventario de assets.

## Estado

Beta completa. Las cuatro lecciones, el hub de portales, el sandbox y los sistemas de audio, respawn y navegación funcionan de principio a fin sobre Meta Quest 3.

## Contexto

Proyecto de equipo de cuatro personas para la asignatura Sistemas Inmersivos del Doble Máster en Ingeniería Informática e Inteligencia Artificial Aplicada de la UC3M. El reparto de tareas y el histórico de desarrollo están en los issues del repositorio.

## Licencia y assets

El código de este repositorio es de autoría propia del equipo y se puede reutilizar citando la fuente.

El proyecto incluye modelos, texturas, audio y paquetes de terceros que no lo son. Por eso el repositorio no lleva un archivo de licencia global: publicarlo bajo MIT equivaldría a afirmar que todo lo que hay dentro se puede redistribuir libremente, y no es el caso. Cada componente de terceros se rige por la licencia de su titular.

Se publica como pieza de portafolio, para que se pueda leer el código y la arquitectura.
