using System;
using System.Data.Entity.Migrations;
using System.Reflection;

namespace DM.Baseline.Harness.Infrastructure
{
    /// <summary>
    /// Both legacy Migrations `Configuration` classes (<c>DM.Models.Migrations.Configuration</c> and
    /// <c>DM.AuthServer.Migrations.Configuration</c>) are declared <c>internal sealed</c> -- normal
    /// for an EF6 migrations configuration, since Visual Studio's own Package-Manager-Console
    /// `Update-Database`/`migrate.exe` tooling finds and invokes them purely by reflection and never
    /// needs compile-time access. This harness does the same thing those tools do: construct the
    /// type via <see cref="Activator.CreateInstance(Type, bool)"/>'s <c>nonPublic: true</c> overload,
    /// which is a fully-supported, standard .NET reflection API for exactly this case -- not a hack,
    /// and not a modification of the legacy source (both Configuration classes are read, never
    /// touched, by this harness).
    ///
    /// `DM.AuthServer.Migrations.Configuration.AddPermissions(ApplicationDbContext)` (the actual seam
    /// GM-039/GM-040 pin) is additionally `private static`, so it is invoked the same way, via
    /// <see cref="MethodInfo.Invoke"/> with <see cref="BindingFlags.NonPublic"/>.
    /// </summary>
    public static class ReflectionHelpers
    {
        public static DbMigrationsConfiguration CreateMigrationsConfiguration(string typeName, string assemblyName)
        {
            var type = Type.GetType(typeName + ", " + assemblyName);
            if (type == null)
                throw new InvalidOperationException(
                    "Could not load type '" + typeName + "' from assembly '" + assemblyName +
                    "'. Confirm both DM.Models.dll and DM.AuthServer.dll built successfully before running this harness.");

            return (DbMigrationsConfiguration)Activator.CreateInstance(type, nonPublic: true);
        }

        public static object InvokePrivateStatic(Type declaringType, string methodName, params object[] args)
        {
            var method = declaringType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException(
                    "Could not find private static method '" + methodName + "' on " + declaringType.FullName +
                    " via reflection. If the legacy Configuration class's method signature changed, update the" +
                    " scenario test that calls this helper.");

            return method.Invoke(null, args);
        }
    }
}
