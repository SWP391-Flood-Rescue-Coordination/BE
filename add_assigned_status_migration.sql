/*
  Migration: Add 'Assigned' to rescue_requests and rescue_operations CHECK constraints
  
  Run this script on your DisasterRescueReliefDB database BEFORE using the new assign API.
  This script is idempotent (safe to run multiple times).
*/

USE DisasterRescueReliefDB;
GO

-- 1. Update rescue_requests status CHECK constraint to allow 'Assigned'
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_rescue_requests_status_allowed')
BEGIN
    ALTER TABLE dbo.rescue_requests DROP CONSTRAINT CK_rescue_requests_status_allowed;
END
GO

ALTER TABLE dbo.rescue_requests
ADD CONSTRAINT CK_rescue_requests_status_allowed
CHECK (status IN ('Pending','Verified','Assigned','In Progress','Completed','Cancelled','Duplicate'));
GO

-- 2. Update rescue_operations status CHECK constraint to allow 'Assigned'
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_rescue_operations_status_allowed')
BEGIN
    ALTER TABLE dbo.rescue_operations DROP CONSTRAINT CK_rescue_operations_status_allowed;
END
GO

ALTER TABLE dbo.rescue_operations
ADD CONSTRAINT CK_rescue_operations_status_allowed
CHECK (status IN ('Assigned','In Progress','Completed','Cancelled'));
GO

-- 3. Update rescue_request_status_history unique index to allow 'Assigned'
--    (The existing unique index UX_rrsh_request_status allows only one record per status per request -- this is fine)

PRINT 'Migration completed: Assigned status added to CHECK constraints.';
GO
