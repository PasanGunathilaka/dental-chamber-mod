using Microsoft.Data.SqlClient;

namespace DentalManagement.DataMigration.LegacyReaders;

/// <summary>
/// Reads the legacy SQL Server database over raw ADO.NET.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not EF6. Reconstructing the legacy model in this solution would
/// import the very thing BL-001 exists to get away from — including a migration
/// chain that cannot build a fresh database. Treating the legacy schema as an input
/// format rather than a code dependency also means the synthetic stand-in database
/// (spec A4) is just SQL scripts (design D-5).
/// </para>
/// <para>
/// Every column is read positionally against an explicit <c>SELECT</c> list, so a
/// legacy column added or reordered later surfaces as a loud failure rather than a
/// silently shifted value.
/// </para>
/// </remarks>
public sealed class SqlServerLegacyDataSource(string connectionString) : ILegacyDataSource
{
    public async Task<LegacyDatabase> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return new LegacyDatabase
        {
            Patients = await ReadAsync(
                connection,
                """
                SELECT Id, Code, Name, Age, Phone, Email, Address, Gender, Note, Created, LastUpdate
                FROM dbo.Patient
                """,
                reader => new LegacyPatient(
                    reader.GetGuid(0),
                    GetNullableString(reader, 1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    GetNullableString(reader, 4),
                    GetNullableString(reader, 5),
                    GetNullableString(reader, 6),
                    GetNullableString(reader, 7),
                    GetNullableString(reader, 8),
                    reader.GetDateTime(9),
                    reader.GetDateTime(10)),
                cancellationToken),

            Prescriptions = await ReadAsync(
                connection,
                """
                SELECT Id, Code, PatientId, TotalCharge, DiscountPercent, DiscountAmount,
                       FixedDiscount, TotalPayable, TotalPaid, TotalDue, StatusId, Created, LastUpdate
                FROM dbo.Prescription
                """,
                reader => new LegacyPrescription(
                    reader.GetGuid(0),
                    GetNullableString(reader, 1),
                    reader.GetGuid(2),
                    reader.GetDouble(3),
                    reader.GetDouble(4),
                    reader.GetDouble(5),
                    reader.GetDouble(6),
                    reader.GetDouble(7),
                    reader.GetDouble(8),
                    reader.GetDouble(9),
                    reader.GetInt32(10),
                    reader.GetDateTime(11),
                    reader.GetDateTime(12)),
                cancellationToken),

            MedicalServices = await ReadAsync(
                connection,
                "SELECT Id, Code, Name, Charge, Created, LastUpdate FROM dbo.MedicalService",
                reader => new LegacyMedicalService(
                    reader.GetGuid(0),
                    reader.GetInt32(1),
                    reader.GetString(2),
                    GetNullableString(reader, 3),
                    reader.GetDateTime(4),
                    reader.GetDateTime(5)),
                cancellationToken),

            PatientMedicalServices = await ReadAsync(
                connection,
                """
                SELECT Id, PatientId, PrescriptionId, MedicalServiceId, Quantity, Created, LastUpdate
                FROM dbo.PatientMedicalService
                """,
                reader => new LegacyPatientMedicalService(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetGuid(2),
                    reader.GetGuid(3),
                    reader.GetInt32(4),
                    reader.GetDateTime(5),
                    reader.GetDateTime(6)),
                cancellationToken),

            MedicalInfos = await ReadAsync(
                connection,
                "SELECT Id, Name, Created, LastUpdate FROM dbo.MedicalInfo",
                reader => new LegacyMedicalInfo(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetDateTime(2),
                    reader.GetDateTime(3)),
                cancellationToken),

            PatientMedicalInfos = await ReadAsync(
                connection,
                "SELECT Id, PatientId, MedicalInfoId, Created, LastUpdate FROM dbo.PatientMedicalInfo",
                reader => new LegacyPatientMedicalInfo(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetGuid(2),
                    reader.GetDateTime(3),
                    reader.GetDateTime(4)),
                cancellationToken),

            Payments = await ReadAsync(
                connection,
                "SELECT Id, PrescriptionId, Amount, Comment, Created, LastUpdate FROM dbo.Payment",
                reader => new LegacyPayment(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetDouble(2),
                    GetNullableString(reader, 3),
                    reader.GetDateTime(4),
                    reader.GetDateTime(5)),
                cancellationToken),

            Products = await ReadAsync(
                connection,
                """
                SELECT Id, Code, Name, StartingInventory, Received, Shipped, OnHand,
                       MinimumRequired, UnitPrice, SalePrice, StatusId, Created, LastUpdate
                FROM dbo.Product
                """,
                reader => new LegacyProduct(
                    reader.GetGuid(0),
                    GetNullableString(reader, 1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4),
                    reader.GetInt32(5),
                    reader.GetInt32(6),
                    reader.GetInt32(7),
                    reader.GetDouble(8),
                    reader.GetDouble(9),
                    reader.GetInt32(10),
                    reader.GetDateTime(11),
                    reader.GetDateTime(12)),
                cancellationToken),

            Inventories = await ReadAsync(
                connection,
                """
                SELECT Id, ProductId, CashMemoNo, OnHand, ReceivedOrShippedQuantity,
                       StatusId, Created, LastUpdate
                FROM dbo.Inventory
                """,
                reader => new LegacyInventory(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4),
                    reader.GetInt32(5),
                    reader.GetDateTime(6),
                    reader.GetDateTime(7)),
                cancellationToken),

            Doctors = await ReadAsync(
                connection,
                "SELECT Id, Code, Name, Phone, Created, LastUpdate FROM dbo.Doctor",
                reader => new LegacyDoctor(
                    reader.GetGuid(0),
                    GetNullableString(reader, 1),
                    GetNullableString(reader, 2),
                    GetNullableString(reader, 3),
                    reader.GetDateTime(4),
                    reader.GetDateTime(5)),
                cancellationToken),

            Appointments = await ReadAsync(
                connection,
                """
                SELECT Id, Code, PatientNameOrId, Age, Phone, [Date], [Time],
                       DoctorId, StatusId, Created, LastUpdate
                FROM dbo.Appointment
                """,
                reader => new LegacyAppointment(
                    reader.GetGuid(0),
                    GetNullableString(reader, 1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    GetNullableString(reader, 4),
                    reader.GetDateTime(5),
                    reader.GetDateTime(6),
                    reader.GetGuid(7),
                    reader.GetInt32(8),
                    reader.GetDateTime(9),
                    reader.GetDateTime(10)),
                cancellationToken),

            Roles = await ReadAsync(
                connection,
                "SELECT Id, Name FROM dbo.AspNetRoles",
                reader => new LegacyRole(reader.GetString(0), reader.GetString(1)),
                cancellationToken),

            Users = await ReadAsync(
                connection,
                """
                SELECT Id, UserName, Email, EmailConfirmed, PasswordHash, SecurityStamp,
                       PhoneNumber, FirstName, LastName
                FROM dbo.AspNetUsers
                """,
                reader => new LegacyUser(
                    reader.GetString(0),
                    reader.GetString(1),
                    GetNullableString(reader, 2),
                    reader.GetBoolean(3),
                    GetNullableString(reader, 4),
                    GetNullableString(reader, 5),
                    GetNullableString(reader, 6),
                    GetNullableString(reader, 7),
                    GetNullableString(reader, 8)),
                cancellationToken),

            UserRoles = await ReadAsync(
                connection,
                "SELECT UserId, RoleId FROM dbo.AspNetUserRoles",
                reader => new LegacyUserRole(reader.GetString(0), reader.GetString(1)),
                cancellationToken),

            Resources = await ReadAsync(
                connection,
                "SELECT Id, Name, Route, IsPublic FROM dbo.Resources",
                reader => new LegacyResource(
                    reader.GetString(0),
                    GetNullableString(reader, 1),
                    reader.GetString(2),
                    reader.GetBoolean(3)),
                cancellationToken),

            Permissions = await ReadAsync(
                connection,
                "SELECT Id, RoleId, RoleName, ResourceId FROM dbo.Permissions",
                reader => new LegacyPermission(
                    reader.GetString(0),
                    reader.GetString(1),
                    GetNullableString(reader, 2),
                    reader.GetString(3)),
                cancellationToken),
        };
    }

    private static async Task<List<T>> ReadAsync<T>(
        SqlConnection connection,
        string sql,
        Func<SqlDataReader, T> project,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(project(reader));
        }

        return rows;
    }

    private static string? GetNullableString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
}
