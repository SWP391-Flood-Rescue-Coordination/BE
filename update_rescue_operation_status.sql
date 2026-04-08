-- Script to add 'Waiting' status to rescue_operations table
-- Run this script in SQL Server Management Studio (SSMS) or your SQL client

-- 1. Drop the existing constraint
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'CK_rescue_operations_status_allowed' AND parent_object_id = OBJECT_ID('dbo.rescue_operations'))
BEGIN
    ALTER TABLE [dbo].[rescue_operations] DROP CONSTRAINT [CK_rescue_operations_status_allowed];
END
GO

-- 2. Add the updated constraint including 'Waiting'
ALTER TABLE [dbo].[rescue_operations] WITH CHECK ADD CONSTRAINT [CK_rescue_operations_status_allowed] 
CHECK (([status]='Failed' OR [status]='Completed' OR [status]='Assigned' OR [status]='Waiting'));
GO

-- 3. Verify the constraint is enabled
ALTER TABLE [dbo].[rescue_operations] CHECK CONSTRAINT [CK_rescue_operations_status_allowed];
GO

PRINT 'Successfully updated status constraint for rescue_operations table.';
