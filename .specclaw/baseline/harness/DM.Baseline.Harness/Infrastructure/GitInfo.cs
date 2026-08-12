using System;
using System.Diagnostics;

namespace DM.Baseline.Harness.Infrastructure
{
    /// <summary>
    /// Reads the legacy repo's own commit SHA at capture time, per CONTRACT.md (a)'s
    /// <c>legacy_commit_sha</c> field. Shells out to the `git` on PATH rather than vendoring a git
    /// library, exactly once per test run (cached), against <see cref="RepoPaths.RepoRoot"/>, which
    /// is the same directory `git status`/`git log` were already run against for this design.
    /// </summary>
    public static class GitInfo
    {
        private static string _cachedSha;

        public static string LegacyCommitSha
        {
            get
            {
                if (_cachedSha != null) return _cachedSha;

                try
                {
                    var startInfo = new ProcessStartInfo("git", "rev-parse HEAD")
                    {
                        WorkingDirectory = RepoPaths.RepoRoot,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();
                        _cachedSha = process.ExitCode == 0 && output.Length > 0 ? output : "UNKNOWN";
                    }
                }
                catch (Exception)
                {
                    // Never fail a capture run just because `git` isn't on PATH -- record the gap
                    // honestly instead of guessing a SHA.
                    _cachedSha = "UNKNOWN";
                }

                return _cachedSha;
            }
        }
    }
}
