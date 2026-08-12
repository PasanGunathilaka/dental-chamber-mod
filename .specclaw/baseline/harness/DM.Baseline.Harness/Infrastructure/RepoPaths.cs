using System;
using System.Configuration;
using System.IO;

namespace DM.Baseline.Harness.Infrastructure
{
    /// <summary>
    /// Resolves the legacy repo root and the fixtures output directory from wherever this test
    /// assembly happens to be running (bin\Debug or bin\Release under this project), instead of a
    /// hardcoded machine-specific absolute path -- so the harness keeps working after a `git clone`
    /// on a different machine.
    ///
    /// Project layout this depends on (fixed by where specclaw wrote this project):
    ///   &lt;RepoRoot&gt;\.specclaw\baseline\harness\DM.Baseline.Harness\bin\&lt;Config&gt;\   (test binary here)
    ///   &lt;RepoRoot&gt;\.specclaw\baseline\fixtures\                                        (fixtures land here)
    ///   &lt;RepoRoot&gt;\                                                                     (git repo root == project_root)
    /// </summary>
    public static class RepoPaths
    {
        public static readonly string RepoRoot = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..", ".."));

        public static string FixturesDir
        {
            get
            {
                var configured = ConfigurationManager.AppSettings["FixturesOutputDir"];
                var relative = string.IsNullOrEmpty(configured)
                    ? ".specclaw/baseline/fixtures"
                    : configured;

                return Path.GetFullPath(Path.Combine(RepoRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
            }
        }

        public static string AnchorDateSetting
        {
            get { return ConfigurationManager.AppSettings["AnchorDate"]; }
        }
    }
}
