-- =============================================
-- MIGRATION SCRIPT
-- Description: Add estimated_time (nullable INT)
--              to rescue_operations
-- Date: 2026-02-25
-- =============================================

USE DisasterRescueReliefDB;
GO

PRINT 'Starting migration: add estimated_time to rescue_operations...';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'estimated_time'
      AND Object_ID = OBJECT_ID(N'dbo.rescue_operations')
)
BEGIN
    PRINT 'Adding estimated_time to rescue_operations...';
    ALTER TABLE dbo.rescue_operations
    ADD estimated_time INT NULL;
    PRINT 'Done.';
END
ELSE
BEGIN
    PRINT 'Column estimated_time already exists in rescue_operations. Skipping.';
END
GO

PRINT 'Migration completed successfully.';
GO
