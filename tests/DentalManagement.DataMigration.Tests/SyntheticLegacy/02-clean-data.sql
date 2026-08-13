-- Well-formed legacy data: everything here must migrate cleanly with no audit
-- finding. Reconciliation counts and monetary totals are computed from these rows.

INSERT INTO dbo.Status (Id, Name) VALUES
    (1, 'In Stock'), (2, 'Out Of Stock'), (3, 'Received'), (4, 'Shipped'),
    (5, 'Active'),   (6, 'Closed'),       (7, 'Appointed'), (8, 'Visited');

-- Two patients, each with the legacy code format and a known gender value.
INSERT INTO dbo.Patient (Id, Code, Name, Age, Phone, Email, Address, Gender, Note, Created, LastUpdate) VALUES
    ('11111111-1111-1111-1111-111111111111', 'P000001', 'Asha Perera',   34, '0771234567', 'asha@example.com',  'Colombo 03', 'Female', NULL, '2026-01-05T09:15:00', '2026-01-05T09:15:00'),
    ('22222222-2222-2222-2222-222222222222', 'P000002', 'Nuwan Silva',   41, '0779876543', 'nuwan@example.com', 'Kandy',      'Male',   NULL, '2026-02-11T10:30:00', '2026-03-01T14:00:00');

-- Bills: one closed with a full payment, one active with a part payment.
INSERT INTO dbo.Prescription (Id, Code, PatientId, TotalCharge, DiscountPercent, DiscountAmount, FixedDiscount, TotalPayable, TotalPaid, TotalDue, StatusId, Created, LastUpdate) VALUES
    ('aaaa1111-0000-0000-0000-000000000001', 'BILL001-P000001', '11111111-1111-1111-1111-111111111111', 4500.00, 10, 450.00, 0,   4050.00, 4050.00,    0.00, 6, '2026-01-05T09:20:00', '2026-01-06T11:00:00'),
    ('aaaa1111-0000-0000-0000-000000000002', 'BILL002-P000001', '11111111-1111-1111-1111-111111111111', 2000.00,  0,   0.00, 100, 1900.00,  500.00, 1400.00, 5, '2026-03-02T08:00:00', '2026-03-02T08:45:00'),
    ('aaaa1111-0000-0000-0000-000000000003', 'BILL003-P000002', '22222222-2222-2222-2222-222222222222', 1250.50,  0,   0.00, 0,   1250.50, 1250.50,    0.00, 6, '2026-02-11T10:35:00', '2026-02-11T12:00:00');

-- Service catalog. Charges here are all parseable as decimal; the values that
-- are not live in 03-problem-data.sql.
INSERT INTO dbo.MedicalService (Id, Code, Name, Charge, Created, LastUpdate) VALUES
    ('bbbb1111-0000-0000-0000-000000000001', 1, 'Scaling',       '1500',    '2025-11-01T08:00:00', '2025-11-01T08:00:00'),
    ('bbbb1111-0000-0000-0000-000000000002', 2, 'Extraction',    '3000',    '2025-11-01T08:00:00', '2025-11-01T08:00:00'),
    -- A fractional charge: legacy threw FormatException computing TotalCharge for
    -- this row (GM-017). CQ-008 says the rebuild must accept it as 1250.50.
    ('bbbb1111-0000-0000-0000-000000000003', 3, 'Consultation',  '1250.50', '2025-11-01T08:00:00', '2025-11-01T08:00:00');

INSERT INTO dbo.PatientMedicalService (Id, PatientId, PrescriptionId, MedicalServiceId, Quantity, Created, LastUpdate) VALUES
    ('cccc1111-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'aaaa1111-0000-0000-0000-000000000001', 'bbbb1111-0000-0000-0000-000000000001', 3, '2026-01-05T09:21:00', '2026-01-05T09:21:00'),
    ('cccc1111-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'aaaa1111-0000-0000-0000-000000000002', 'bbbb1111-0000-0000-0000-000000000002', 1, '2026-03-02T08:05:00', '2026-03-02T08:05:00'),
    ('cccc1111-0000-0000-0000-000000000003', '22222222-2222-2222-2222-222222222222', 'aaaa1111-0000-0000-0000-000000000003', 'bbbb1111-0000-0000-0000-000000000003', 1, '2026-02-11T10:36:00', '2026-02-11T10:36:00');

INSERT INTO dbo.MedicalInfo (Id, Name, Created, LastUpdate) VALUES
    ('dddd1111-0000-0000-0000-000000000001', 'Diabetic',  '2025-11-01T08:00:00', '2025-11-01T08:00:00'),
    ('dddd1111-0000-0000-0000-000000000002', 'Asthmatic', '2025-11-01T08:00:00', '2025-11-01T08:00:00');

INSERT INTO dbo.PatientMedicalInfo (Id, PatientId, MedicalInfoId, Created, LastUpdate) VALUES
    ('eeee1111-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'dddd1111-0000-0000-0000-000000000001', '2026-01-05T09:16:00', '2026-01-05T09:16:00');

INSERT INTO dbo.Payment (Id, PrescriptionId, Amount, Comment, Created, LastUpdate) VALUES
    ('ffff1111-0000-0000-0000-000000000001', 'aaaa1111-0000-0000-0000-000000000001', 4050.00, 'Settled in full', '2026-01-06T11:00:00', '2026-01-06T11:00:00'),
    ('ffff1111-0000-0000-0000-000000000002', 'aaaa1111-0000-0000-0000-000000000002',  500.00, 'Part payment',    '2026-03-02T08:45:00', '2026-03-02T08:45:00'),
    ('ffff1111-0000-0000-0000-000000000003', 'aaaa1111-0000-0000-0000-000000000003', 1250.50, NULL,              '2026-02-11T12:00:00', '2026-02-11T12:00:00');

INSERT INTO dbo.Product (Id, Code, Name, StartingInventory, Received, Shipped, OnHand, MinimumRequired, UnitPrice, SalePrice, StatusId, Created, LastUpdate) VALUES
    ('a1a1a1a1-0000-0000-0000-000000000001', 'PR001', 'Latex Gloves',   100, 50, 30, 120, 20, 25.50, 40.00, 1, '2025-12-01T08:00:00', '2026-03-01T08:00:00'),
    ('a1a1a1a1-0000-0000-0000-000000000002', 'PR002', 'Dental Floss',    40,  0, 40,   0,  10, 90.00, 150.00, 2, '2025-12-01T08:00:00', '2026-02-20T08:00:00');

INSERT INTO dbo.Inventory (Id, ProductId, CashMemoNo, OnHand, ReceivedOrShippedQuantity, StatusId, Created, LastUpdate) VALUES
    ('b1b1b1b1-0000-0000-0000-000000000001', 'a1a1a1a1-0000-0000-0000-000000000001', 'CM-1001', 150, 50, 3, '2026-01-15T09:00:00', '2026-01-15T09:00:00'),
    ('b1b1b1b1-0000-0000-0000-000000000002', 'a1a1a1a1-0000-0000-0000-000000000001', 'CM-1002', 120, 30, 4, '2026-02-15T09:00:00', '2026-02-15T09:00:00'),
    ('b1b1b1b1-0000-0000-0000-000000000003', 'a1a1a1a1-0000-0000-0000-000000000002', 'CM-1003',   0, 40, 4, '2026-02-20T08:00:00', '2026-02-20T08:00:00');

-- The single legacy doctor. Its id is whatever SQL Server actually generated at
-- seed time, NOT the GUID the legacy seeder wrote and EF discarded — which is the
-- whole point of the defect this rebuild must not reproduce.
INSERT INTO dbo.Doctor (Id, Code, Name, Phone, Created, LastUpdate) VALUES
    ('c1c1c1c1-0000-0000-0000-000000000001', 'DR001', 'Dental Doctor', '0112345678', '2025-11-01T08:00:00', '2025-11-01T08:00:00');

INSERT INTO dbo.Appointment (Id, Code, PatientNameOrId, Age, Phone, [Date], [Time], DoctorId, StatusId, Created, LastUpdate) VALUES
    ('d1d1d1d1-0000-0000-0000-000000000001', 'AP001', 'Asha Perera',   34, '0771234567', '2026-03-10T00:00:00', '2026-03-10T10:30:00', 'c1c1c1c1-0000-0000-0000-000000000001', 7, '2026-03-01T08:00:00', '2026-03-01T08:00:00'),
    -- A walk-in who is not a registered Patient: exactly why PatientNameOrId is
    -- free text and not a foreign key.
    ('d1d1d1d1-0000-0000-0000-000000000002', 'AP002', 'Walk-in caller', 28, '0715555555', '2026-03-11T00:00:00', '2026-03-11T15:00:00', 'c1c1c1c1-0000-0000-0000-000000000001', 8, '2026-03-02T08:00:00', '2026-03-05T16:00:00');

INSERT INTO dbo.AspNetRoles (Id, Name) VALUES
    ('role-systemadmin', 'SystemAdmin'),
    ('role-admin',       'Admin'),
    ('role-manager',     'Manager'),
    ('role-user',        'User'),
    ('role-inventory',   'Inventory'),
    ('role-patient',     'Patient'),
    ('role-doctor',      'Doctor'),
    ('role-compounder',  'Compounder');

INSERT INTO dbo.AspNetUsers (Id, UserName, Email, EmailConfirmed, PasswordHash, SecurityStamp, FirstName, LastName) VALUES
    ('user-superadmin', 'superadmin', 'superadmin@clinic.local', 1, 'AQAAAAEAACcQAAAAE-legacy-hash-superadmin', 'stamp-superadmin', 'Super', 'Admin'),
    ('user-reception',  'reception',  'reception@clinic.local',  1, 'AQAAAAEAACcQAAAAE-legacy-hash-reception',  'stamp-reception',  'Front', 'Desk');

-- One role per user in practice, even though the legacy model allowed many
-- (CQ-015).
INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES
    ('user-superadmin', 'role-systemadmin'),
    ('user-reception',  'role-user');

INSERT INTO dbo.Resources (Id, Name, Route, IsPublic) VALUES
    ('res-login',   'Login',        'root.login',        1),
    ('res-denied',  'Access Denied','root.access-denied',1),
    ('res-patient', 'Patient List', 'root.patient',      0),
    ('res-product', 'Product Catalog', 'root.product',   0),
    ('res-user',    'Manage Users', 'root.user',         0);

INSERT INTO dbo.Permissions (Id, RoleId, RoleName, ResourceId) VALUES
    ('perm-sa-patient', 'role-systemadmin', 'SystemAdmin', 'res-patient'),
    ('perm-sa-product', 'role-systemadmin', 'SystemAdmin', 'res-product'),
    ('perm-sa-user',    'role-systemadmin', 'SystemAdmin', 'res-user'),
    -- A grant to a non-SystemAdmin role, created at runtime through the Permission
    -- screen rather than by DR-016's seed. It must survive migration.
    ('perm-user-patient', 'role-user', 'User', 'res-patient');
