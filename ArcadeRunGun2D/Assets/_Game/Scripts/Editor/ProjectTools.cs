using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace HyperArcade.EditorTools
{
    /// <summary>
    /// Conexión del proyecto abierto en Unity con GitHub:
    ///  - "Versión y carpeta del proyecto": muestra qué carpeta, rama y commit tiene abierto Unity.
    ///  - "Actualizar desde GitHub": descarga los cambios de la rama de trabajo en ESTA carpeta (git fetch + pull).
    /// </summary>
    public static class ProjectTools
    {
        private static string ProjectDir => Path.GetDirectoryName(Application.dataPath);

        // ------------------------------------------------------------ Información

        [MenuItem("Tools/HyperArcade/Versión y carpeta del proyecto", priority = 80)]
        public static void ShowInfo()
        {
            string repo = FindRepoRoot(ProjectDir);
            string gitInfo = repo == null
                ? "ATENCIÓN: esta carpeta NO está conectada a Git/GitHub.\nLos cambios publicados en GitHub no llegan aquí."
                : $"Repositorio local: {repo}\nRama: {ReadBranch(repo) ?? "(desconocida)"}\nCommit: {ReadCommit(repo) ?? "(desconocido)"}";
            string message = $"Versión del código: {BuildInfo.CodeVersion}\n\nCarpeta abierta en Unity:\n{ProjectDir}\n\n{gitInfo}";
            Debug.Log("[HyperArcade] " + message);
            if (EditorUtility.DisplayDialog("HyperArcade · Proyecto", message, "Abrir carpeta", "Cerrar"))
                EditorUtility.RevealInFinder(Application.dataPath);
        }

        // ------------------------------------------------------------ Actualizar

        [MenuItem("Tools/HyperArcade/Actualizar desde GitHub", priority = 81)]
        public static void UpdateFromGitHub()
        {
            string repo = FindRepoRoot(ProjectDir);
            if (repo == null)
            {
                EditorUtility.DisplayDialog("HyperArcade",
                    "Esta carpeta no está conectada a GitHub, así que no se puede actualizar desde aquí:\n\n" + ProjectDir +
                    "\n\nClona el repositorio con GitHub Desktop (rama " + BuildInfo.GitBranch +
                    ") y abre en Unity Hub la carpeta ArcadeRunGun2D de ese clon.", "OK");
                return;
            }

            string git = FindGit();
            if (git == null)
            {
                EditorUtility.DisplayDialog("HyperArcade",
                    "No encontré Git en este equipo.\n\nActualiza con GitHub Desktop (Fetch origin y luego Pull origin) " +
                    "sobre esta carpeta:\n" + repo, "OK");
                return;
            }

            var log = new StringBuilder();
            try
            {
                EditorUtility.DisplayProgressBar("HyperArcade", "Descargando cambios de GitHub...", 0.3f);
                if (!Run(git, "fetch origin --prune", repo, log)) { Fail(log); return; }

                if (ReadBranch(repo) != BuildInfo.GitBranch)
                {
                    EditorUtility.DisplayProgressBar("HyperArcade", "Cambiando a la rama " + BuildInfo.GitBranch + "...", 0.6f);
                    if (!Run(git, "checkout " + BuildInfo.GitBranch, repo, log)) { Fail(log); return; }
                }

                EditorUtility.DisplayProgressBar("HyperArcade", "Aplicando cambios...", 0.85f);
                if (!Run(git, "pull --ff-only origin " + BuildInfo.GitBranch, repo, log)) { Fail(log); return; }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log("[HyperArcade] Actualización desde GitHub\n" + log);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("HyperArcade",
                $"Proyecto actualizado.\nRama: {ReadBranch(repo)} · Commit: {ReadCommit(repo)}\n\n" +
                "Unity recompilará los scripts en unos segundos. Si el cambio afecta a escenas, " +
                "ejecuta Tools > HyperArcade > 1. Crear contenido por defecto.", "OK");
        }

        private static void Fail(StringBuilder log)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[HyperArcade] No se pudo actualizar desde GitHub\n" + log);
            EditorUtility.DisplayDialog("HyperArcade",
                "No se pudo actualizar. Detalle en la consola.\n\n" +
                "Si el mensaje habla de 'local changes', descarta esos cambios en GitHub Desktop y vuelve a intentarlo.", "OK");
        }

        // ------------------------------------------------------------ Git

        private static bool Run(string git, string args, string workingDir, StringBuilder log)
        {
            log.AppendLine("> git " + args);
            try
            {
                var psi = new ProcessStartInfo(git, args)
                {
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0"; // nunca quedarse esperando una contraseña
                using (var process = Process.Start(psi))
                {
                    var errorTask = process.StandardError.ReadToEndAsync();
                    string output = process.StandardOutput.ReadToEnd();
                    if (!process.WaitForExit(120000))
                    {
                        process.Kill();
                        log.AppendLine("Tiempo de espera agotado.");
                        return false;
                    }
                    log.AppendLine(output.Trim());
                    log.AppendLine(errorTask.Result.Trim());
                    return process.ExitCode == 0;
                }
            }
            catch (Exception e)
            {
                log.AppendLine(e.Message);
                return false;
            }
        }

        private static string FindGit()
        {
            if (CanRun("git")) return "git";

            var candidates = new List<string>();
            // Git incluido con GitHub Desktop (Windows).
            string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GitHubDesktop");
            if (Directory.Exists(desktop))
                foreach (var app in Directory.GetDirectories(desktop, "app-*").OrderByDescending(d => d))
                    candidates.Add(Path.Combine(app, "resources", "app", "git", "cmd", "git.exe"));
            candidates.Add(@"C:\Program Files\Git\cmd\git.exe");
            candidates.Add("/usr/bin/git");
            candidates.Add("/usr/local/bin/git");
            candidates.Add("/opt/homebrew/bin/git");
            return candidates.FirstOrDefault(File.Exists);
        }

        private static bool CanRun(string exe)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, "--version")
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit(5000);
                    return p.HasExited && p.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ------------------------------------------------------------ Lectura de .git (sin ejecutar git)

        private static string FindRepoRoot(string dir)
        {
            while (!string.IsNullOrEmpty(dir))
            {
                string dotGit = Path.Combine(dir, ".git");
                if (Directory.Exists(dotGit) || File.Exists(dotGit)) return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        private static string GitDir(string repo)
        {
            string dotGit = Path.Combine(repo, ".git");
            if (Directory.Exists(dotGit)) return dotGit;
            // Worktree / submódulo: el archivo .git contiene "gitdir: <ruta>".
            string line = File.ReadAllText(dotGit).Trim();
            const string prefix = "gitdir:";
            if (!line.StartsWith(prefix)) return dotGit;
            string path = line.Substring(prefix.Length).Trim();
            return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(repo, path));
        }

        private static string ReadHead(string repo)
        {
            try { return File.ReadAllText(Path.Combine(GitDir(repo), "HEAD")).Trim(); }
            catch (Exception) { return null; }
        }

        private static string ReadBranch(string repo)
        {
            string head = ReadHead(repo);
            if (head == null) return null;
            const string prefix = "ref: refs/heads/";
            return head.StartsWith(prefix) ? head.Substring(prefix.Length) : "(sin rama: " + Short(head) + ")";
        }

        private static string ReadCommit(string repo)
        {
            string head = ReadHead(repo);
            if (head == null) return null;
            if (!head.StartsWith("ref: ")) return Short(head);

            string refName = head.Substring(5).Trim();
            string gitDir = GitDir(repo);
            string refFile = Path.Combine(gitDir, refName.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(refFile)) return Short(File.ReadAllText(refFile).Trim());

            string packed = Path.Combine(gitDir, "packed-refs");
            if (!File.Exists(packed)) return null;
            foreach (var line in File.ReadAllLines(packed))
                if (line.EndsWith(" " + refName)) return Short(line.Split(' ')[0]);
            return null;
        }

        private static string Short(string hash) => hash != null && hash.Length > 7 ? hash.Substring(0, 7) : hash;
    }
}
