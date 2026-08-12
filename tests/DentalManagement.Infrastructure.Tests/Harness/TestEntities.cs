using DentalManagement.Domain.Entities;
using DentalManagement.Domain.Enums;

namespace DentalManagement.Infrastructure.Tests.Harness;

/// <summary>
/// Minimal valid entities for arranging database state.
/// </summary>
/// <remarks>
/// Timestamps use <see cref="DateTimeKind.Unspecified"/> to match how
/// <c>SystemClock</c> writes them and how the
/// <c>timestamp without time zone</c> columns store them (spec A8).
/// </remarks>
public static class TestEntities
{
    public static readonly DateTime FixedNow =
        new(2026, 8, 12, 12, 0, 0, DateTimeKind.Unspecified);

    public static Patient Patient(string code, string name = "Test Patient") => new()
    {
        Code = code,
        Name = name,
        Age = 30,
        Gender = Gender.Female,
        Created = FixedNow,
        LastUpdate = FixedNow,
    };

    public static Prescription Prescription(Guid patientId, string code) => new()
    {
        Code = code,
        PatientId = patientId,
        Status = BillStatus.Active,
        Created = FixedNow,
        LastUpdate = FixedNow,
    };

    public static MedicalService MedicalService(int code, string name, decimal charge) => new()
    {
        Code = code,
        Name = name,
        Charge = charge,
        Created = FixedNow,
        LastUpdate = FixedNow,
    };

    public static MedicalInfo MedicalInfo(string name) => new()
    {
        Name = name,
        Created = FixedNow,
        LastUpdate = FixedNow,
    };

    public static PatientMedicalInfo PatientMedicalInfo(Guid patientId, Guid medicalInfoId) => new()
    {
        PatientId = patientId,
        MedicalInfoId = medicalInfoId,
        Created = FixedNow,
        LastUpdate = FixedNow,
    };

    public static PatientMedicalService LineItem(
        Guid patientId,
        Guid prescriptionId,
        Guid medicalServiceId) => new()
    {
        PatientId = patientId,
        PrescriptionId = prescriptionId,
        MedicalServiceId = medicalServiceId,
        Quantity = 1,
        Created = FixedNow,
        LastUpdate = FixedNow,
    };

    public static Payment Payment(Guid prescriptionId, decimal amount) => new()
    {
        PrescriptionId = prescriptionId,
        Amount = amount,
        Created = FixedNow,
        LastUpdate = FixedNow,
    };

    public static Product Product(string name, int onHand = 0) => new()
    {
        Name = name,
        OnHand = onHand,
        Status = ProductStatus.InStock,
        Created = FixedNow,
        LastUpdate = FixedNow,
    };

    public static Inventory InventoryMovement(Guid productId, int quantity) => new()
    {
        ProductId = productId,
        CashMemoNo = "CM-001",
        OnHand = quantity,
        ReceivedOrShippedQuantity = quantity,
        Status = InventoryMovementStatus.Received,
        Created = FixedNow,
        LastUpdate = FixedNow,
    };

    public static Doctor Doctor(string name = "Dental Doctor") => new()
    {
        Code = "DR001",
        Name = name,
        Created = FixedNow,
        LastUpdate = FixedNow,
    };

    public static Appointment Appointment(Guid doctorId, string patientNameOrId = "Walk-in") => new()
    {
        Code = "AP001",
        PatientNameOrId = patientNameOrId,
        Age = 40,
        Date = FixedNow.Date,
        Time = FixedNow,
        DoctorId = doctorId,
        Status = AppointmentStatus.Appointed,
        Created = FixedNow,
        LastUpdate = FixedNow,
    };
}
