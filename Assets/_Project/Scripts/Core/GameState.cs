namespace _Project.Scripts.Core
{
    /// <summary>
    /// Represents the high-level state of the application at any given moment.
    /// Each value maps to one learning mode of the game.
    /// Set by SceneController after every scene load.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// The portal room (Main_VR.unity). The player is standing in the glass hub
        /// and has not entered any lesson yet.
        /// </summary>
        MainMenu,

        /// <summary>
        /// Lesson 1 — Solar system diorama. The player observes the miniature
        /// model of all planets with their orbits visible.
        /// </summary>
        SolarSystem,

        /// <summary>
        /// Lesson 2 — Planet surface. The player stands on a planet under its
        /// real surface gravity. Used for rock-drop gravity experiments.
        /// Active in scenes: Mercury, Venus, Earth, Moon, Mars, Jupiter,
        /// Saturn, Uranus, Neptune, Pluto, Sun.
        /// </summary>
        PlanetSurface,

        /// <summary>
        /// Lesson 3 — Kepler lab. The player changes planet masses and orbital
        /// velocities to observe Kepler's laws in real time.
        /// </summary>
        KeplerLab,

        /// <summary>
        /// Lesson 4 — Free sandbox. The player spawns planets and moves them
        /// by hand. Also hosts the asteroid destruction experiment.
        /// </summary>
        Sandbox
    }
}
