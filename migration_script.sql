-- =============================================
-- MIGRATION SCRIPT
-- Description: Applies schema refactoring changes
-- 1. Drops system_configs, rescue_team_members, vehicle_assignments
-- 2. Removes fuel_level from vehicles
-- 3. Renames rescue_assignments -> rescue_operations
-- =============================================

USE RescueManagementSystem;
GO

PRINT 'Starting Migration...';
GO

-- 1. Remove fuel_level from vehicles
-- Check if column exists before dropping
IF EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'fuel_level' AND Object_ID = Object_ID(N'vehicles'))
BEGIN
    PRINT 'Dropping fuel_level column from vehicles...';
    ALTER TABLE vehicles DROP COLUMN fuel_level;
END
GO

-- 2. Drop Tables
PRINT 'Dropping obsolete tables...';
DROP TABLE IF EXISTS system_configs;
DROP TABLE IF EXISTS rescue_team_members;
DROP TABLE IF EXISTS vehicle_assignments;
GO

-- 3. Rename rescue_assignments -> rescue_operations
-- WARNING: Renaming Primary Keys involves dropping referencing FKs first.

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'rescue_assignments') AND type in (N'U'))
BEGIN
    PRINT 'Renaming rescue_assignments to rescue_operations...';
    
    -- A. Drop Foreign Keys referencing rescue_assignments to allow renaming
    DECLARE @sql NVARCHAR(MAX) = N'';
    SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))
        + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + 
        N' DROP CONSTRAINT ' + QUOTENAME(name) + N'; '
    FROM sys.foreign_keys
    WHERE referenced_object_id = OBJECT_ID('rescue_assignments');

    -- Drop FKs referencing assignment_id in other tables that we want to remain implicitly linked? 
    -- Actually, simpler to drop them and recreate them.
    EXEC sp_executesql @sql;

    -- B. Rename Table
    EXEC sp_rename 'rescue_assignments', 'rescue_operations';

    -- C. Rename PK Column
    EXEC sp_rename 'rescue_operations.assignment_id', 'operation_id', 'COLUMN';

    PRINT 'Renamed main table and column.';
END
GO

-- 4. Rename rescue_assignment_reports -> rescue_operation_reports
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'rescue_assignment_reports') AND type in (N'U'))
BEGIN
    PRINT 'Renaming rescue_assignment_reports...';
    EXEC sp_rename 'rescue_assignment_reports', 'rescue_operation_reports';
    EXEC sp_rename 'rescue_operation_reports.assignment_id', 'operation_id', 'COLUMN';
END
GO

-- 5. Rename column in relief_distributions
IF EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'assignment_id' AND Object_ID = Object_ID(N'relief_distributions'))
BEGIN
    PRINT 'Renaming assignment_id in relief_distributions...';
    EXEC sp_rename 'relief_distributions.assignment_id', 'operation_id', 'COLUMN';
END
GO

-- 6. Re-create Foreign Keys
PRINT 'Re-creating Foreign Keys...';

-- Re-link rescue_operation_reports -> rescue_operations
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'FK_Reports_Operations'))
BEGIN
    ALTER TABLE rescue_operation_reports 
    ADD CONSTRAINT FK_Reports_Operations FOREIGN KEY (operation_id) REFERENCES rescue_operations(operation_id);
END

-- Re-link relief_distributions -> rescue_operations
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'FK_Distributions_Operations'))
BEGIN
    ALTER TABLE relief_distributions
    ADD CONSTRAINT FK_Distributions_Operations FOREIGN KEY (operation_id) REFERENCES rescue_operations(operation_id);
END
GO

PRINT 'Migration Complete Successfully.';
GO
