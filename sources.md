## Links a recursos utilizados

Planetas [Unity Asset Store](https://assetstore.unity.com/packages/3d/environments/planets-of-the-solar-system-3d-90219)

## Recursos sugeridos para sceneries (pendientes de importar)

Todos gratis en el Unity Asset Store y compatibles con URP. Se listan aqui para anadirlos cuando se quiera sustituir las sceneries proceduales por arte mas rico:

- Skybox Series Free -- Avionx: paquete con varios cielos estelares/atmosfericos utiles para reemplazar los skybox procedurales.
- AllSky Free -- rpgwhitelock: 10 cielos listos para usar (estrellados, atardeceres, nebulas).
- Rock and Boulders 2 -- Manufactura K4: rocas low-poly con colliders, reutilizables para Mercurio/Marte/Luna.
- Terrain Sample Asset Pack -- Unity Technologies: terrenos variados para generar landscapes realistas.
- Yughues Free Sand Materials / Free Concrete Materials: texturas PBR para el suelo de cada planeta.

Para aprovecharlos luego: importar el paquete, crear un prefab `Scenery_<Planeta>` dentro de `_Project/Prefabs/VFX/` o `_Project/Models/`, y desactivar `_buildScenery` en el `PlanetSceneBootstrap` de la escena correspondiente antes de colocar el prefab manualmente.
