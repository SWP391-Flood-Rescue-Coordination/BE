-- =============================================
-- MIGRATION SCRIPT
-- Description: Add number_of_affected_people (nullable INT)
--              to rescue_requests and rescue_operations
-- Date: 2026-02-25
-- =============================================

USE DisasterRescueReliefDB;
GO

PRINT 'Starting migration: add number_of_affected_people...';
GO

-- 1. Add to rescue_requests
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'number_of_affected_people'
      AND Object_ID = OBJECT_ID(N'dbo.rescue_requests')
)
BEGIN
    PRINT 'Adding number_of_affected_people to rescue_requests...';
    ALTER TABLE dbo.rescue_requests
    ADD number_of_affected_people INT NULL;
    PRINT 'Done.';
END
ELSE
BEGIN
    PRINT 'Column number_of_affected_people already exists in rescue_requests. Skipping.';
END
GO

-- 2. Add to rescue_operations
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'number_of_affected_people'
      AND Object_ID = OBJECT_ID(N'dbo.rescue_operations')
)
BEGIN
    PRINT 'Adding number_of_affected_people to rescue_operations...';
    ALTER TABLE dbo.rescue_operations
    ADD number_of_affected_people INT NULL;
    PRINT 'Done.';
END
ELSE
BEGIN
    PRINT 'Column number_of_affected_people already exists in rescue_operations. Skipping.';
END
GO

PRINT 'Migration completed successfully.';
GO
