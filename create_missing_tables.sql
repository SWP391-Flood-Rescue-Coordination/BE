-- =============================================
-- Script tạo các bảng còn thiếu cho Authentication
-- Chạy script này trong SQL Server Management Studio
-- =============================================

USE RescueManagementDB;
GO

-- Tạo bảng refresh_tokens nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'refresh_tokens')
BEGIN
    CREATE TABLE refresh_tokens (
        id INT IDENTITY(1,1) PRIMARY KEY,
        user_id INT NOT NULL,
        token NVARCHAR(500) NOT NULL,
        expires_at DATETIME2 NOT NULL,
        created_at DATETIME2 DEFAULT GETDATE(),
        revoked_at DATETIME2 NULL,
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (user_id) REFERENCES users(user_id)
    );

    CREATE INDEX IX_RefreshTokens_Token ON refresh_tokens(token);
    CREATE INDEX IX_RefreshTokens_UserId ON refresh_tokens(user_id);
    
    PRINT 'Created table: refresh_tokens';
END
ELSE
BEGIN
    PRINT 'Table refresh_tokens already exists';
END
GO

-- Tạo bảng blacklisted_tokens nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'blacklisted_tokens')
BEGIN
    CREATE TABLE blacklisted_tokens (
        id INT IDENTITY(1,1) PRIMARY KEY,
        token NVARCHAR(1000) NOT NULL,
        blacklisted_at DATETIME2 DEFAULT GETDATE(),
        expires_at DATETIME2 NOT NULL
    );

    CREATE INDEX IX_BlacklistedTokens_Token ON blacklisted_tokens(token);
    CREATE INDEX IX_BlacklistedTokens_ExpiresAt ON blacklisted_tokens(expires_at);
    
    PRINT 'Created table: blacklisted_tokens';
END
ELSE
BEGIN
    PRINT 'Table blacklisted_tokens already exists';
END
GO

PRINT 'Script completed successfully!';
GO
