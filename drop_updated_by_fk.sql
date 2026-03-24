USE [DisasterRescueReliefDB];
GO

-- =====================================================
-- Xóa FK constraint: updated_by -> users.user_id
-- Lý do: updated_by = -1 cho Guest/System (không có tài khoản)
-- =====================================================

-- 1. Bảng rescue_request_status_history
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_rrsh_updated_by'
      AND parent_object_id = OBJECT_ID('dbo.rescue_request_status_history')
)
BEGIN
    ALTER TABLE [dbo].[rescue_request_status_history]
        DROP CONSTRAINT [FK_rrsh_updated_by];
    PRINT 'Dropped FK_rrsh_updated_by';
END
ELSE
    PRINT 'FK_rrsh_updated_by not found (already dropped or never existed)';
GO

-- 2. Bảng rescue_requests
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_rescue_requests_updated_by'
      AND parent_object_id = OBJECT_ID('dbo.rescue_requests')
)
BEGIN
    ALTER TABLE [dbo].[rescue_requests]
        DROP CONSTRAINT [FK_rescue_requests_updated_by];
    PRINT 'Dropped FK_rescue_requests_updated_by';
END
ELSE
    PRINT 'FK_rescue_requests_updated_by not found (already dropped or never existed)';
GO

PRINT 'Done. Gia tri -1 trong updated_by = Guest/System khong co tai khoan.';
GO
