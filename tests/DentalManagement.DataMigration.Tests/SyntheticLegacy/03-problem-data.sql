-- Legacy rows the new schema cannot accept as-is. Every one of these must appear
-- in the audit report rather than being coerced, defaulted, or dropped
-- (spec FR-21, AC-20, design R-5).
--
-- Applied as a separate script so a test can migrate clean data alone, or clean
-- plus problem data, and compare the two audit reports.

-- 1. Charge strings that will not parse as decimal. Legacy stored Charge as
--    NVARCHAR, so nothing stopped these. CQ-008 requires them reported under the
--    existing NON_INTEGER_CHARGE code from error-map.md, never silently zeroed.
INSERT INTO dbo.MedicalService (Id, Code, Name, Charge, Created, LastUpdate) VALUES
    ('bbbb2222-0000-0000-0000-000000000001', 4, 'Unparseable Charge', 'abc',       '2025-11-02T08:00:00', '2025-11-02T08:00:00'),
    ('bbbb2222-0000-0000-0000-000000000002', 5, 'Empty Charge',       '',          '2025-11-02T08:00:00', '2025-11-02T08:00:00'),
    ('bbbb2222-0000-0000-0000-000000000003', 6, 'Null Charge',        NULL,        '2025-11-02T08:00:00', '2025-11-02T08:00:00'),
    ('bbbb2222-0000-0000-0000-000000000004', 7, 'Currency Symbol',    'Rs. 1,500', '2025-11-02T08:00:00', '2025-11-02T08:00:00');

-- 2. Gender values outside Male/Female/Others. The legacy column was free text
--    and the client hardcoded its own option list, so nothing tied them together
--    (CQ-007).
INSERT INTO dbo.Patient (Id, Code, Name, Age, Phone, Email, Address, Gender, Note, Created, LastUpdate) VALUES
    ('33333333-3333-3333-3333-333333333333', 'P000003', 'Unknown Gender',  29, NULL, NULL, NULL, 'Unknown', NULL, '2026-04-01T09:00:00', '2026-04-01T09:00:00'),
    ('44444444-4444-4444-4444-444444444444', 'P000004', 'Lowercase Male',  52, NULL, NULL, NULL, 'male',    NULL, '2026-04-02T09:00:00', '2026-04-02T09:00:00'),
    ('55555555-5555-5555-5555-555555555555', 'P000005', 'Empty Gender',    19, NULL, NULL, NULL, '',        NULL, '2026-04-03T09:00:00', '2026-04-03T09:00:00');

-- 3. A StatusId that does not belong to the owning entity's set. Nothing
--    partitioned the shared legacy Status table by entity, so an appointment could
--    hold "Received" (3) with nothing to stop it — the exact integrity hole CQ-006
--    closes.
INSERT INTO dbo.Appointment (Id, Code, PatientNameOrId, Age, Phone, [Date], [Time], DoctorId, StatusId, Created, LastUpdate) VALUES
    ('d1d1d1d1-0000-0000-0000-000000000003', 'AP003', 'Wrong status', 33, NULL, '2026-04-05T00:00:00', '2026-04-05T11:00:00', 'c1c1c1c1-0000-0000-0000-000000000001', 3, '2026-04-01T08:00:00', '2026-04-01T08:00:00');

-- A bill holding a product status ("In Stock", 1) for the same reason.
INSERT INTO dbo.Prescription (Id, Code, PatientId, TotalCharge, DiscountPercent, DiscountAmount, FixedDiscount, TotalPayable, TotalPaid, TotalDue, StatusId, Created, LastUpdate) VALUES
    ('aaaa2222-0000-0000-0000-000000000001', 'BILL004-P000003', '33333333-3333-3333-3333-333333333333', 800.00, 0, 0.00, 0, 800.00, 0.00, 800.00, 1, '2026-04-01T09:05:00', '2026-04-01T09:05:00');

-- 4. Duplicate Patient.Code. GM-002 captured legacy returning 200 OK while the
--    unique index rejected the insert, so a real database can hold a collision
--    created by a manual Code edit. The new schema's unique index means both rows
--    cannot migrate — they must be reported, not silently dropped.
INSERT INTO dbo.Patient (Id, Code, Name, Age, Phone, Email, Address, Gender, Note, Created, LastUpdate) VALUES
    ('66666666-6666-6666-6666-666666666666', 'P000001', 'Duplicate Code', 60, NULL, NULL, NULL, 'Male', NULL, '2026-04-04T09:00:00', '2026-04-04T09:00:00');

-- 5. Orphaned PatientMedicalInfo rows pointing at a patient that no longer
--    exists. GM-019 proves legacy produced these, so they must migrate as-is; a
--    "cleanup" here would destroy data outside this item's mandate.
INSERT INTO dbo.PatientMedicalInfo (Id, PatientId, MedicalInfoId, Created, LastUpdate) VALUES
    ('eeee2222-0000-0000-0000-000000000001', '99999999-9999-9999-9999-999999999999', 'dddd1111-0000-0000-0000-000000000001', '2026-01-01T09:00:00', '2026-01-01T09:00:00'),
    ('eeee2222-0000-0000-0000-000000000002', '99999999-9999-9999-9999-999999999999', 'dddd1111-0000-0000-0000-000000000002', '2026-01-01T09:00:00', '2026-01-01T09:00:00');

-- 6. A null in a column the rebuild makes required. Legacy allowed Patient.Code
--    to be null; the new schema does not.
INSERT INTO dbo.Patient (Id, Code, Name, Age, Phone, Email, Address, Gender, Note, Created, LastUpdate) VALUES
    ('77777777-7777-7777-7777-777777777777', NULL, 'No Code', 45, NULL, NULL, NULL, 'Female', NULL, '2026-04-06T09:00:00', '2026-04-06T09:00:00');
