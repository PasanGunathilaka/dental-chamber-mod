-- Legacy-shaped SQL Server schema, standing in for the production export BL-001
-- names as a verification input but which this repository does not contain
-- (spec A4). Shapes follow domain-model.md exactly, including the parts the
-- rebuild deliberately changes:
--
--   * MedicalService.Charge is NVARCHAR, not a numeric type (DR-019 / CQ-008)
--   * Patient.Gender is NVARCHAR free text (CQ-007)
--   * one shared Status lookup table serves four unrelated entities (CQ-006)
--   * PatientMedicalInfo carries plain GUID columns with no foreign keys (GM-019)
--   * Appointment.PatientNameOrId is free text, not a Patient foreign key
--
-- The point of reproducing the legacy shape faithfully is that the migration tool
-- must be exercised against the data problems it exists to report, not against a
-- tidied-up source that would never surface them.

CREATE TABLE dbo.Status (
    Id   INT           NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.Patient (
    Id         UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Code       NVARCHAR(8)      NULL,
    Name       NVARCHAR(30)     NOT NULL,
    Age        INT              NOT NULL,
    Phone      NVARCHAR(30)     NULL,
    Email      NVARCHAR(100)    NULL,
    Address    NVARCHAR(200)    NULL,
    Gender     NVARCHAR(20)     NULL,
    Note       NVARCHAR(500)    NULL,
    Created    DATETIME         NOT NULL,
    LastUpdate DATETIME         NOT NULL
);

CREATE TABLE dbo.Prescription (
    Id                  UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Code                NVARCHAR(18)     NULL,
    PatientId           UNIQUEIDENTIFIER NOT NULL,
    TotalCharge         FLOAT            NOT NULL,
    DiscountPercent     FLOAT            NOT NULL,
    DiscountAmount      FLOAT            NOT NULL,
    FixedDiscount       FLOAT            NOT NULL DEFAULT 0,
    TotalPayable        FLOAT            NOT NULL,
    TotalPaid           FLOAT            NOT NULL,
    TotalDue            FLOAT            NOT NULL,
    StatusId            INT              NOT NULL,
    Created             DATETIME         NOT NULL,
    LastUpdate          DATETIME         NOT NULL,
    CONSTRAINT FK_Prescription_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient (Id),
    CONSTRAINT FK_Prescription_Status  FOREIGN KEY (StatusId)  REFERENCES dbo.Status (Id)
);

-- Charge is a string in legacy. That is the defect CQ-008 fixes, and the reason
-- the migration must audit rather than coerce.
CREATE TABLE dbo.MedicalService (
    Id         UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Code       INT              NOT NULL,
    Name       NVARCHAR(50)     NOT NULL,
    Charge     NVARCHAR(50)     NULL,
    Created    DATETIME         NOT NULL,
    LastUpdate DATETIME         NOT NULL
);

CREATE TABLE dbo.PatientMedicalService (
    Id               UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId        UNIQUEIDENTIFIER NOT NULL,
    PrescriptionId   UNIQUEIDENTIFIER NOT NULL,
    MedicalServiceId UNIQUEIDENTIFIER NOT NULL,
    Quantity         INT              NOT NULL DEFAULT 1,
    Created          DATETIME         NOT NULL,
    LastUpdate       DATETIME         NOT NULL
);

CREATE TABLE dbo.MedicalInfo (
    Id         UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Name       NVARCHAR(50)     NOT NULL,
    Created    DATETIME         NOT NULL,
    LastUpdate DATETIME         NOT NULL
);

-- No foreign keys, exactly as legacy declared it. GM-019 depends on this.
CREATE TABLE dbo.PatientMedicalInfo (
    Id            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PatientId     UNIQUEIDENTIFIER NOT NULL,
    MedicalInfoId UNIQUEIDENTIFIER NOT NULL,
    Created       DATETIME         NOT NULL,
    LastUpdate    DATETIME         NOT NULL
);

CREATE TABLE dbo.Payment (
    Id             UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    PrescriptionId UNIQUEIDENTIFIER NOT NULL,
    Amount         FLOAT            NOT NULL,
    Comment        NVARCHAR(500)    NULL,
    Created        DATETIME         NOT NULL,
    LastUpdate     DATETIME         NOT NULL,
    CONSTRAINT FK_Payment_Prescription FOREIGN KEY (PrescriptionId) REFERENCES dbo.Prescription (Id)
);

CREATE TABLE dbo.Product (
    Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Code              NVARCHAR(40)     NULL,
    Name              NVARCHAR(40)     NOT NULL,
    StartingInventory INT              NOT NULL,
    Received          INT              NOT NULL,
    Shipped           INT              NOT NULL,
    OnHand            INT              NOT NULL,
    MinimumRequired   INT              NOT NULL,
    UnitPrice         FLOAT            NOT NULL,
    SalePrice         FLOAT            NOT NULL,
    StatusId          INT              NOT NULL,
    Created           DATETIME         NOT NULL,
    LastUpdate        DATETIME         NOT NULL,
    CONSTRAINT FK_Product_Status FOREIGN KEY (StatusId) REFERENCES dbo.Status (Id)
);

CREATE TABLE dbo.Inventory (
    Id                        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ProductId                 UNIQUEIDENTIFIER NOT NULL,
    CashMemoNo                NVARCHAR(50)     NOT NULL,
    OnHand                    INT              NOT NULL,
    ReceivedOrShippedQuantity INT              NOT NULL,
    StatusId                  INT              NOT NULL,
    Created                   DATETIME         NOT NULL,
    LastUpdate                DATETIME         NOT NULL,
    CONSTRAINT FK_Inventory_Product FOREIGN KEY (ProductId) REFERENCES dbo.Product (Id),
    CONSTRAINT FK_Inventory_Status  FOREIGN KEY (StatusId)  REFERENCES dbo.Status (Id)
);

CREATE TABLE dbo.Doctor (
    Id         UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Code       NVARCHAR(20)     NULL,
    Name       NVARCHAR(60)     NULL,
    Phone      NVARCHAR(30)     NULL,
    Created    DATETIME         NOT NULL,
    LastUpdate DATETIME         NOT NULL
);

-- PatientNameOrId is free text, not a Patient foreign key.
CREATE TABLE dbo.Appointment (
    Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Code            NVARCHAR(20)     NULL,
    PatientNameOrId NVARCHAR(40)     NOT NULL,
    Age             INT              NOT NULL,
    Phone           NVARCHAR(30)     NULL,
    [Date]          DATETIME         NOT NULL,
    [Time]          DATETIME         NOT NULL,
    DoctorId        UNIQUEIDENTIFIER NOT NULL,
    StatusId        INT              NOT NULL,
    Created         DATETIME         NOT NULL,
    LastUpdate      DATETIME         NOT NULL,
    CONSTRAINT FK_Appointment_Doctor FOREIGN KEY (DoctorId) REFERENCES dbo.Doctor (Id),
    CONSTRAINT FK_Appointment_Status FOREIGN KEY (StatusId) REFERENCES dbo.Status (Id)
);

-- Identity/permission schema. Legacy ran these in a second, independently
-- migrated context over the same physical database (CQ-002).
CREATE TABLE dbo.AspNetRoles (
    Id   NVARCHAR(128) NOT NULL PRIMARY KEY,
    Name NVARCHAR(256) NOT NULL
);

CREATE TABLE dbo.AspNetUsers (
    Id                   NVARCHAR(128) NOT NULL PRIMARY KEY,
    UserName             NVARCHAR(256) NOT NULL,
    Email                NVARCHAR(256) NULL,
    EmailConfirmed       BIT           NOT NULL DEFAULT 0,
    PasswordHash         NVARCHAR(MAX) NULL,
    SecurityStamp        NVARCHAR(MAX) NULL,
    PhoneNumber          NVARCHAR(50)  NULL,
    PhoneNumberConfirmed BIT           NOT NULL DEFAULT 0,
    TwoFactorEnabled     BIT           NOT NULL DEFAULT 0,
    LockoutEndDateUtc    DATETIME      NULL,
    LockoutEnabled       BIT           NOT NULL DEFAULT 0,
    AccessFailedCount    INT           NOT NULL DEFAULT 0,
    FirstName            NVARCHAR(100) NULL,
    LastName             NVARCHAR(100) NULL
);

CREATE TABLE dbo.AspNetUserRoles (
    UserId NVARCHAR(128) NOT NULL,
    RoleId NVARCHAR(128) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_User FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id),
    CONSTRAINT FK_AspNetUserRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id)
);

CREATE TABLE dbo.Resources (
    Id       NVARCHAR(128) NOT NULL PRIMARY KEY,
    Name     NVARCHAR(100) NULL,
    Route    NVARCHAR(200) NOT NULL,
    IsPublic BIT           NOT NULL
);

CREATE TABLE dbo.Permissions (
    Id         NVARCHAR(128) NOT NULL PRIMARY KEY,
    RoleId     NVARCHAR(128) NOT NULL,
    RoleName   NVARCHAR(256) NULL,
    ResourceId NVARCHAR(128) NOT NULL,
    CONSTRAINT FK_Permissions_Role     FOREIGN KEY (RoleId)     REFERENCES dbo.AspNetRoles (Id),
    CONSTRAINT FK_Permissions_Resource FOREIGN KEY (ResourceId) REFERENCES dbo.Resources (Id)
);
