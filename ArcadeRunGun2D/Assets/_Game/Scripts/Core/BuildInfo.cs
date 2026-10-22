namespace HyperArcade
{
    /// <summary>
    /// Versión del código. Se escribe en la consola al pulsar Play y aparece en
    /// Tools > HyperArcade > Versión y carpeta del proyecto: si no coincide con la última
    /// versión publicada en GitHub, Unity está abierto sobre una copia sin actualizar.
    /// </summary>
    public static class BuildInfo
    {
        public const string CodeVersion = "1.5.0 (2026-10-22)";

        /// <summary>Rama de GitHub con la versión de trabajo del juego.</summary>
        public const string GitBranch = "ArcadeGame_Rama";
    }
}
