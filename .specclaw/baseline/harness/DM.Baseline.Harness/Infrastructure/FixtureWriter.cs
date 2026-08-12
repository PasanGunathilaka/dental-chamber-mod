using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DM.Baseline.Harness.Infrastructure
{
    /// <summary>
    /// Writes one <c>&lt;fixtures_dir&gt;/&lt;GM-ID&gt;.json</c> file per CONTRACT.md (a), with exactly
    /// the seven verbatim top-level fields specclaw-bf-baseline record extracts by exact name via jq:
    /// scenario_id, captured_at, anchor_date, legacy_commit_sha, runtime_version, normalized_fields,
    /// input, output. Never rename or restructure these.
    ///
    /// This module is only ever invoked by a human actually running the test suite (`vstest.console`) --
    /// specclaw's own generation step never executes it and never writes a fixture file itself.
    /// </summary>
    public static class FixtureWriter
    {
        /// <summary>
        /// Writes a fixture. <paramref name="output"/> must already carry outcome/error_code/threw
        /// per CONTRACT.md (b.1) whenever the scenario's seam can succeed-or-reject, and the four
        /// representation-class fields (ExceptionType/InnerExceptionType/ExceptionMessage/
        /// InnerExceptionMessage) per (b.2) whenever the seam actually raised.
        /// </summary>
        public static void Write(
            string scenarioId,
            object input,
            object output,
            IEnumerable<string> normalizedFields = null)
        {
            var fixture = new JObject
            {
                ["scenario_id"] = scenarioId,
                ["captured_at"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["anchor_date"] = ResolveAnchorDate(),
                ["legacy_commit_sha"] = GitInfo.LegacyCommitSha,
                ["runtime_version"] = Environment.Version.ToString(),
                ["normalized_fields"] = JArray.FromObject(normalizedFields ?? new string[0]),
                ["input"] = JToken.FromObject(input ?? new object()),
                ["output"] = JToken.FromObject(output ?? new object())
            };

            Directory.CreateDirectory(RepoPaths.FixturesDir);
            var path = Path.Combine(RepoPaths.FixturesDir, scenarioId + ".json");
            File.WriteAllText(path, fixture.ToString(Formatting.Indented));
        }

        private static string ResolveAnchorDate()
        {
            var configured = RepoPaths.AnchorDateSetting;
            if (!string.IsNullOrEmpty(configured))
            {
                DateTime parsed;
                if (DateTime.TryParse(configured, out parsed))
                    return parsed.ToString("yyyy-MM-dd");
            }

            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }
    }

    /// <summary>
    /// Small, ordered, forgiving key/value bag used to build up each scenario's own <c>output</c>
    /// (and, where relevant, <c>input</c>) shape before handing it to <see cref="FixtureWriter"/>.
    /// A plain <c>Dictionary&lt;string, object&gt;</c> would serialize identically; this type exists
    /// only so every scenario file reads the same way (<c>new Fields { { "outcome", "OK" }, ... }</c>)
    /// rather than each inventing its own anonymous-object shape.
    /// </summary>
    public class Fields : Dictionary<string, object>
    {
    }
}
