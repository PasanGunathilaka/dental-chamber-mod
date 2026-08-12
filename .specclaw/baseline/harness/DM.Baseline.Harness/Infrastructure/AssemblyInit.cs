using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DM.Baseline.Harness.Infrastructure
{
    /// <summary>
    /// MSTest v1 (Microsoft.VisualStudio.QualityTools.UnitTestFramework -- the same framework the
    /// pre-existing DM.Server.Tests stub already references, see this harness's README) requires
    /// [AssemblyInitialize] to live on a class carrying [TestClass]. This is the only place that runs.
    /// </summary>
    [TestClass]
    public class AssemblyInit
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            TestDatabase.EnsureSchemaAndSeed();
        }
    }
}
