using DentalManagement.DataMigration.Auditing;
using DentalManagement.DataMigration.LegacyReaders;
using DentalManagement.Domain.Entities;
using DentalManagement.Domain.Enums;

namespace DentalManagement.DataMigration;

/// <summary>
/// Turns legacy rows into rebuild entities, applying the decided type changes.
/// </summary>
/// <remarks>
/// Primary keys are carried across unchanged so every relationship survives and so
/// a migrated row can still be traced back to its legacy self (spec FR-20). The
/// type conversions here are the decided ones and nothing else: decimal money
/// (CQ-008), typed gender (CQ-007), typed per-entity status (CQ-006).
/// </remarks>
internal static class LegacyToRebuildMapper
{
    /// <summary>
    /// Legacy timestamps are carried across verbatim, with the kind stripped rather
    /// than converted.
    /// </summary>
    /// <remarks>
    /// Nothing in the legacy artifacts records the clinic's timezone, so treating
    /// these as UTC — or as any particular offset — would be a guess that silently
    /// shifts every historical date. Preserving the wall-clock value keeps the
    /// migration lossless and leaves the interpretation to whoever can answer it
    /// (spec A8, design D-8).
    /// </remarks>
    private static DateTime AsWallClock(DateTime legacy) =>
        DateTime.SpecifyKind(legacy, DateTimeKind.Unspecified);

    public static Patient ToPatient(LegacyPatient legacy)
    {
        // An unknown gender is recorded as a finding and stored as null rather than
        // guessed at — the row still migrates (CQ-007).
        LegacyValueAuditor.TryMapGender(legacy.Gender, out var gender);

        return new Patient
        {
            Id = legacy.Id,
            Code = legacy.Code!,
            Name = legacy.Name,
            Age = legacy.Age,
            Phone = legacy.Phone,
            Email = legacy.Email,
            Address = legacy.Address,
            Gender = gender,
            Note = legacy.Note,
            Created = AsWallClock(legacy.Created),
            LastUpdate = AsWallClock(legacy.LastUpdate),
        };
    }

    public static Prescription ToPrescription(LegacyPrescription legacy) => new()
    {
        Id = legacy.Id,
        Code = legacy.Code!,
        PatientId = legacy.PatientId,
        TotalCharge = (decimal)legacy.TotalCharge,
        DiscountPercent = (decimal)legacy.DiscountPercent,
        DiscountAmount = (decimal)legacy.DiscountAmount,
        FixedDiscount = (decimal)legacy.FixedDiscount,
        TotalPayable = (decimal)legacy.TotalPayable,
        TotalPaid = (decimal)legacy.TotalPaid,
        TotalDue = (decimal)legacy.TotalDue,
        Status = LegacyValueAuditor.TryMapStatus<BillStatus>(legacy.StatusId, out var status)
            ? status
            : BillStatus.Active,
        Created = AsWallClock(legacy.Created),
        LastUpdate = AsWallClock(legacy.LastUpdate),
    };

    /// <summary>
    /// The CQ-008 conversion: a string charge becomes a real decimal.
    /// </summary>
    /// <remarks>
    /// Only called for rows whose charge parsed — an unparseable one is a blocking
    /// audit finding and never reaches here, so no value is silently zeroed.
    /// </remarks>
    public static MedicalService ToMedicalService(LegacyMedicalService legacy)
    {
        LegacyValueAuditor.TryParseCharge(legacy.Charge, out var charge);

        return new MedicalService
        {
            Id = legacy.Id,
            Code = legacy.Code,
            Name = legacy.Name,
            Charge = charge,
            Created = AsWallClock(legacy.Created),
            LastUpdate = AsWallClock(legacy.LastUpdate),
        };
    }

    public static PatientMedicalService ToLineItem(LegacyPatientMedicalService legacy) => new()
    {
        Id = legacy.Id,
        PatientId = legacy.PatientId,
        PrescriptionId = legacy.PrescriptionId,
        MedicalServiceId = legacy.MedicalServiceId,
        Quantity = legacy.Quantity,
        Created = AsWallClock(legacy.Created),
        LastUpdate = AsWallClock(legacy.LastUpdate),
    };

    public static MedicalInfo ToMedicalInfo(LegacyMedicalInfo legacy) => new()
    {
        Id = legacy.Id,
        Name = legacy.Name,
        Created = AsWallClock(legacy.Created),
        LastUpdate = AsWallClock(legacy.LastUpdate),
    };

    public static PatientMedicalInfo ToPatientMedicalInfo(LegacyPatientMedicalInfo legacy) => new()
    {
        Id = legacy.Id,
        PatientId = legacy.PatientId,
        MedicalInfoId = legacy.MedicalInfoId,
        Created = AsWallClock(legacy.Created),
        LastUpdate = AsWallClock(legacy.LastUpdate),
    };

    public static Payment ToPayment(LegacyPayment legacy) => new()
    {
        Id = legacy.Id,
        PrescriptionId = legacy.PrescriptionId,
        Amount = (decimal)legacy.Amount,
        Comment = legacy.Comment,
        Created = AsWallClock(legacy.Created),
        LastUpdate = AsWallClock(legacy.LastUpdate),
    };

    public static Product ToProduct(LegacyProduct legacy) => new()
    {
        Id = legacy.Id,
        Code = legacy.Code,
        Name = legacy.Name,
        StartingInventory = legacy.StartingInventory,
        Received = legacy.Received,
        Shipped = legacy.Shipped,
        OnHand = legacy.OnHand,
        MinimumRequired = legacy.MinimumRequired,
        UnitPrice = (decimal)legacy.UnitPrice,
        SalePrice = (decimal)legacy.SalePrice,
        Status = LegacyValueAuditor.TryMapStatus<ProductStatus>(legacy.StatusId, out var status)
            ? status
            : ProductStatus.InStock,
        Created = AsWallClock(legacy.Created),
        LastUpdate = AsWallClock(legacy.LastUpdate),
    };

    public static Inventory ToInventory(LegacyInventory legacy) => new()
    {
        Id = legacy.Id,
        ProductId = legacy.ProductId,
        CashMemoNo = legacy.CashMemoNo,
        OnHand = legacy.OnHand,
        ReceivedOrShippedQuantity = legacy.ReceivedOrShippedQuantity,
        Status = LegacyValueAuditor.TryMapStatus<InventoryMovementStatus>(legacy.StatusId, out var status)
            ? status
            : InventoryMovementStatus.Received,
        Created = AsWallClock(legacy.Created),
        LastUpdate = AsWallClock(legacy.LastUpdate),
    };

    public static Doctor ToDoctor(LegacyDoctor legacy) => new()
    {
        // The legacy id as SQL Server actually generated it — not the GUID the
        // legacy seeder wrote and EF discarded. Carrying the real one across is what
        // keeps existing appointments pointing at a doctor that exists.
        Id = legacy.Id,
        Code = legacy.Code,
        Name = legacy.Name,
        Phone = legacy.Phone,
        Created = AsWallClock(legacy.Created),
        LastUpdate = AsWallClock(legacy.LastUpdate),
    };

    public static Appointment ToAppointment(LegacyAppointment legacy) => new()
    {
        Id = legacy.Id,
        Code = legacy.Code,
        PatientNameOrId = legacy.PatientNameOrId,
        Age = legacy.Age,
        Phone = legacy.Phone,
        Date = AsWallClock(legacy.Date),
        Time = AsWallClock(legacy.Time),
        DoctorId = legacy.DoctorId,
        Status = LegacyValueAuditor.TryMapStatus<AppointmentStatus>(legacy.StatusId, out var status)
            ? status
            : AppointmentStatus.Appointed,
        Created = AsWallClock(legacy.Created),
        LastUpdate = AsWallClock(legacy.LastUpdate),
    };

    public static Resource ToResource(LegacyResource legacy) => new()
    {
        Id = legacy.Id,
        Name = legacy.Name,
        Route = legacy.Route,
        IsPublic = legacy.IsPublic,
    };

    /// <summary>
    /// Re-points a legacy grant at the surviving role and resource ids.
    /// </summary>
    /// <remarks>
    /// The seeder may already have created a role or resource with the same name or
    /// route but a different id, so a grant cannot simply carry its legacy foreign
    /// keys across — it would reference rows that do not exist.
    /// </remarks>
    public static Permission ToPermission(
        LegacyPermission legacy,
        string roleId,
        string resourceId) => new()
    {
        Id = legacy.Id,
        RoleId = roleId,
        RoleName = legacy.RoleName,
        ResourceId = resourceId,
    };
}
