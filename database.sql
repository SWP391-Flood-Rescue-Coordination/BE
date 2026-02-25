/*
Disaster Rescue and Relief Management - Full Database Create Script (SQL Server)
Generated: 2026-02-24

This script CREATES a NEW database (if not exists) and all tables, constraints, and indexes.
Notes:
- Keeps column names updated_at / updated_by as originally (NO renames).
- Implements multi-vehicle per operation via rescue_operation_vehicles (no rescue_operations.vehicle_id).
- Enforces: 1 citizen can have only 1 OPEN request at a time (OPEN excludes Completed/Cancelled/Duplicate).
- Enforces: each status appears at most once per request in rescue_request_status_history.
- Enforces: a vehicle cannot be assigned to more than one In Progress operation (indexed view + unique clustered index).
- Logging of ASSIGNED/UNASSIGNED is done by application; audit table is provided.
*/

/* =========================
   0) Create & use database
   ========================= */
IF DB_ID(N'DisasterRescueReliefDB') IS NULL
BEGIN
    CREATE DATABASE DisasterRescueReliefDB;
END
GO

USE DisasterRescueReliefDB;
GO

SET NOCOUNT ON;
GO

/* =========================
   1) Core tables
   ========================= */

-- USERS
IF OBJECT_ID(N'dbo.users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.users (
        user_id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_users PRIMARY KEY,
        username       VARCHAR(50)  NOT NULL,
        password_hash  VARCHAR(255) NOT NULL,
        full_name      NVARCHAR(100) NULL,
        phone          VARCHAR(20)  NULL,
        email          VARCHAR(100) NULL,
        role           VARCHAR(20)  NOT NULL,
        is_active      BIT          NOT NULL CONSTRAINT DF_users_is_active DEFAULT (1),
        created_at     DATETIME2(3) NOT NULL CONSTRAINT DF_users_created_at DEFAULT SYSUTCDATETIME(),
        address        NVARCHAR(300) NULL
    );
END
GO

-- PRIORITY
IF OBJECT_ID(N'dbo.priority_levels', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.priority_levels (
        priority_id     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_priority_levels PRIMARY KEY,
        level_name      VARCHAR(20)  NOT NULL,
        priority_order  INT          NOT NULL,
        description     NVARCHAR(255) NULL
    );
END
GO

-- RESCUE REQUEST
IF OBJECT_ID(N'dbo.rescue_requests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.rescue_requests (
        request_id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_rescue_requests PRIMARY KEY,
        citizen_id          INT NOT NULL,
        title               NVARCHAR(200) NULL,
        phone               VARCHAR(20) NULL,
        description         NVARCHAR(1000) NULL,
        latitude            DECIMAL(9,6) NULL,
        longitude           DECIMAL(9,6) NULL,
        address             NVARCHAR(300) NULL,
        priority_level_id   INT NULL,
        status              VARCHAR(20) NOT NULL,
        number_of_affected_people INT NULL,
        created_at          DATETIME2(3) NOT NULL CONSTRAINT DF_rescue_requests_created_at DEFAULT SYSUTCDATETIME(),
        updated_at          DATETIME2(3) NULL,
        updated_by          INT NULL
    );
END
GO

-- RESCUE REQUEST STATUS HISTORY
IF OBJECT_ID(N'dbo.rescue_request_status_history', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.rescue_request_status_history (
        status_id   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_rescue_request_status_history PRIMARY KEY,
        request_id  INT NOT NULL,
        status      VARCHAR(20) NOT NULL,
        notes       NVARCHAR(500) NULL,
        updated_by  INT NOT NULL,
        updated_at  DATETIME2(3) NOT NULL CONSTRAINT DF_rrsh_updated_at DEFAULT SYSUTCDATETIME()
    );
END
GO

-- RESCUE TEAM
IF OBJECT_ID(N'dbo.rescue_teams', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.rescue_teams (
        team_id     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_rescue_teams PRIMARY KEY,
        team_name   NVARCHAR(100) NOT NULL,
        status      VARCHAR(20) NOT NULL,
        created_at  DATETIME2(3) NOT NULL CONSTRAINT DF_rescue_teams_created_at DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID(N'dbo.rescue_team_members', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.rescue_team_members (
        team_id      INT NOT NULL,
        user_id      INT NOT NULL,
        member_role  VARCHAR(20) NOT NULL, -- LEADER | MEMBER
        is_active    BIT NOT NULL CONSTRAINT DF_rescue_team_members_is_active DEFAULT (1),
        joined_at    DATETIME2(3) NOT NULL CONSTRAINT DF_rescue_team_members_joined_at DEFAULT SYSUTCDATETIME(),
        left_at      DATETIME2(3) NULL,
        CONSTRAINT PK_rescue_team_members PRIMARY KEY (team_id, user_id)
    );
END
GO

-- VEHICLE
IF OBJECT_ID(N'dbo.vehicle_types', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.vehicle_types (
        vehicle_type_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_vehicle_types PRIMARY KEY,
        type_code       VARCHAR(50) NOT NULL,
        type_name       VARCHAR(50) NOT NULL,
        description     NVARCHAR(255) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.vehicles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.vehicles (
        vehicle_id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_vehicles PRIMARY KEY,
        vehicle_code      VARCHAR(20) NOT NULL,
        vehicle_name      NVARCHAR(100) NULL,
        vehicle_type_id   INT NOT NULL,
        license_plate     VARCHAR(20) NOT NULL,
        capacity          INT NULL,
        status            VARCHAR(20) NOT NULL,
        current_location  NVARCHAR(300) NULL,
        last_maintenance  DATE NULL,
        updated_at        DATETIME2(3) NULL
    );
END
GO

-- RESCUE OPERATION (NO vehicle_id column here)
IF OBJECT_ID(N'dbo.rescue_operations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.rescue_operations (
        operation_id  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_rescue_operations PRIMARY KEY,
        request_id    INT NOT NULL,
        team_id       INT NOT NULL,
        assigned_by   INT NOT NULL,
        assigned_at   DATETIME2(3) NOT NULL CONSTRAINT DF_rescue_operations_assigned_at DEFAULT SYSUTCDATETIME(),
        started_at    DATETIME2(3) NULL,
        completed_at  DATETIME2(3) NULL,
        status        VARCHAR(20) NOT NULL,
        number_of_affected_people INT NULL,
        estimated_time INT NULL
    );
END
GO

-- Join table: many vehicles per operation
IF OBJECT_ID(N'dbo.rescue_operation_vehicles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.rescue_operation_vehicles (
        operation_id INT NOT NULL,
        vehicle_id   INT NOT NULL,
        assigned_by  INT NOT NULL,
        assigned_at  DATETIME2(3) NOT NULL CONSTRAINT DF_rov_assigned_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_rescue_operation_vehicles PRIMARY KEY (operation_id, vehicle_id)
    );
END
GO

-- Audit table for vehicle assignment changes (ASSIGNED/UNASSIGNED), logged by app
IF OBJECT_ID(N'dbo.rescue_operation_vehicle_audit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.rescue_operation_vehicle_audit (
        id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_rescue_operation_vehicle_audit PRIMARY KEY,
        operation_id INT NOT NULL,
        vehicle_id   INT NOT NULL,
        action       NVARCHAR(20) NOT NULL, -- ASSIGNED | UNASSIGNED
        action_at    DATETIME2(3) NOT NULL CONSTRAINT DF_rov_audit_action_at DEFAULT SYSUTCDATETIME(),
        action_by    INT NOT NULL,
        note         NVARCHAR(500) NULL
    );
END
GO

-- RESCUE REPORT
IF OBJECT_ID(N'dbo.rescue_operation_reports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.rescue_operation_reports (
        report_id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_rescue_operation_reports PRIMARY KEY,
        operation_id           INT NOT NULL,
        people_rescued         INT NULL,
        situation_description  NVARCHAR(1000) NULL,
        actions_taken          NVARCHAR(1000) NULL,
        reported_by            INT NOT NULL,
        reported_at            DATETIME2(3) NOT NULL CONSTRAINT DF_rescue_operation_reports_reported_at DEFAULT SYSUTCDATETIME()
    );
END
GO

/* =========================
   2) Relief module tables
   ========================= */

IF OBJECT_ID(N'dbo.item_categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.item_categories (
        category_id    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_item_categories PRIMARY KEY,
        category_code  VARCHAR(50) NOT NULL,
        category_name  NVARCHAR(100) NOT NULL,
        description    NVARCHAR(255) NULL,
        is_active      BIT NOT NULL CONSTRAINT DF_item_categories_is_active DEFAULT (1),
        created_at     DATETIME2(3) NOT NULL CONSTRAINT DF_item_categories_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_item_categories_category_code UNIQUE (category_code)
    );
END
GO

IF OBJECT_ID(N'dbo.relief_items', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.relief_items (
        item_id     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_relief_items PRIMARY KEY,
        item_code   VARCHAR(50) NOT NULL,
        item_name   NVARCHAR(200) NOT NULL,
        category_id INT NOT NULL,
        unit        VARCHAR(20) NOT NULL,
        is_active   BIT NOT NULL CONSTRAINT DF_relief_items_is_active DEFAULT (1),
        created_at  DATETIME2(3) NOT NULL CONSTRAINT DF_relief_items_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_relief_items_item_code UNIQUE (item_code)
    );
END
GO

IF OBJECT_ID(N'dbo.relief_distributions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.relief_distributions (
        distribution_id   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_relief_distributions PRIMARY KEY,
        request_id        INT NOT NULL,
        distributed_by    INT NOT NULL,
        distribution_date DATETIME2(3) NULL,
        status            VARCHAR(20) NULL,
        notes             NVARCHAR(500) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.relief_distribution_items', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.relief_distribution_items (
        distribution_item_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_relief_distribution_items PRIMARY KEY,
        distribution_id      INT NOT NULL,
        item_id              INT NOT NULL,
        quantity             INT NOT NULL
    );
END
GO

-- NOTIFICATION
IF OBJECT_ID(N'dbo.notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.notifications (
        notification_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_notifications PRIMARY KEY,
        user_id         INT NOT NULL,
        title           NVARCHAR(200) NOT NULL,
        message         NVARCHAR(1000) NOT NULL,
        is_read         BIT NOT NULL CONSTRAINT DF_notifications_is_read DEFAULT (0),
        created_at      DATETIME2(3) NOT NULL CONSTRAINT DF_notifications_created_at DEFAULT SYSUTCDATETIME()
    );
END
GO

/* =========================
   3) Foreign keys
   ========================= */

-- rescue_requests
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rescue_requests_citizen')
BEGIN
    ALTER TABLE dbo.rescue_requests
    ADD CONSTRAINT FK_rescue_requests_citizen
        FOREIGN KEY (citizen_id) REFERENCES dbo.users(user_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rescue_requests_priority')
BEGIN
    ALTER TABLE dbo.rescue_requests
    ADD CONSTRAINT FK_rescue_requests_priority
        FOREIGN KEY (priority_level_id) REFERENCES dbo.priority_levels(priority_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rescue_requests_updated_by')
BEGIN
    ALTER TABLE dbo.rescue_requests
    ADD CONSTRAINT FK_rescue_requests_updated_by
        FOREIGN KEY (updated_by) REFERENCES dbo.users(user_id);
END
GO

-- status history
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rrsh_request')
BEGIN
    ALTER TABLE dbo.rescue_request_status_history
    ADD CONSTRAINT FK_rrsh_request
        FOREIGN KEY (request_id) REFERENCES dbo.rescue_requests(request_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rrsh_updated_by')
BEGIN
    ALTER TABLE dbo.rescue_request_status_history
    ADD CONSTRAINT FK_rrsh_updated_by
        FOREIGN KEY (updated_by) REFERENCES dbo.users(user_id);
END
GO

-- team members
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rescue_team_members_team')
BEGIN
    ALTER TABLE dbo.rescue_team_members
    ADD CONSTRAINT FK_rescue_team_members_team
        FOREIGN KEY (team_id) REFERENCES dbo.rescue_teams(team_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rescue_team_members_user')
BEGIN
    ALTER TABLE dbo.rescue_team_members
    ADD CONSTRAINT FK_rescue_team_members_user
        FOREIGN KEY (user_id) REFERENCES dbo.users(user_id);
END
GO

-- vehicles
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_vehicles_vehicle_type')
BEGIN
    ALTER TABLE dbo.vehicles
    ADD CONSTRAINT FK_vehicles_vehicle_type
        FOREIGN KEY (vehicle_type_id) REFERENCES dbo.vehicle_types(vehicle_type_id);
END
GO

-- operations
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rescue_operations_request')
BEGIN
    ALTER TABLE dbo.rescue_operations
    ADD CONSTRAINT FK_rescue_operations_request
        FOREIGN KEY (request_id) REFERENCES dbo.rescue_requests(request_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rescue_operations_team')
BEGIN
    ALTER TABLE dbo.rescue_operations
    ADD CONSTRAINT FK_rescue_operations_team
        FOREIGN KEY (team_id) REFERENCES dbo.rescue_teams(team_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rescue_operations_assigned_by')
BEGIN
    ALTER TABLE dbo.rescue_operations
    ADD CONSTRAINT FK_rescue_operations_assigned_by
        FOREIGN KEY (assigned_by) REFERENCES dbo.users(user_id);
END
GO

-- operation vehicles join
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rov_operation')
BEGIN
    ALTER TABLE dbo.rescue_operation_vehicles
    ADD CONSTRAINT FK_rov_operation
        FOREIGN KEY (operation_id) REFERENCES dbo.rescue_operations(operation_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rov_vehicle')
BEGIN
    ALTER TABLE dbo.rescue_operation_vehicles
    ADD CONSTRAINT FK_rov_vehicle
        FOREIGN KEY (vehicle_id) REFERENCES dbo.vehicles(vehicle_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rov_assigned_by')
BEGIN
    ALTER TABLE dbo.rescue_operation_vehicles
    ADD CONSTRAINT FK_rov_assigned_by
        FOREIGN KEY (assigned_by) REFERENCES dbo.users(user_id);
END
GO

-- audit
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rov_audit_operation')
BEGIN
    ALTER TABLE dbo.rescue_operation_vehicle_audit
    ADD CONSTRAINT FK_rov_audit_operation
        FOREIGN KEY (operation_id) REFERENCES dbo.rescue_operations(operation_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rov_audit_vehicle')
BEGIN
    ALTER TABLE dbo.rescue_operation_vehicle_audit
    ADD CONSTRAINT FK_rov_audit_vehicle
        FOREIGN KEY (vehicle_id) REFERENCES dbo.vehicles(vehicle_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rov_audit_action_by')
BEGIN
    ALTER TABLE dbo.rescue_operation_vehicle_audit
    ADD CONSTRAINT FK_rov_audit_action_by
        FOREIGN KEY (action_by) REFERENCES dbo.users(user_id);
END
GO

-- reports
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rescue_operation_reports_operation')
BEGIN
    ALTER TABLE dbo.rescue_operation_reports
    ADD CONSTRAINT FK_rescue_operation_reports_operation
        FOREIGN KEY (operation_id) REFERENCES dbo.rescue_operations(operation_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rescue_operation_reports_reported_by')
BEGIN
    ALTER TABLE dbo.rescue_operation_reports
    ADD CONSTRAINT FK_rescue_operation_reports_reported_by
        FOREIGN KEY (reported_by) REFERENCES dbo.users(user_id);
END
GO

-- relief
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_relief_items_category')
BEGIN
    ALTER TABLE dbo.relief_items
    ADD CONSTRAINT FK_relief_items_category
        FOREIGN KEY (category_id) REFERENCES dbo.item_categories(category_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_relief_distributions_request')
BEGIN
    ALTER TABLE dbo.relief_distributions
    ADD CONSTRAINT FK_relief_distributions_request
        FOREIGN KEY (request_id) REFERENCES dbo.rescue_requests(request_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_relief_distributions_distributed_by')
BEGIN
    ALTER TABLE dbo.relief_distributions
    ADD CONSTRAINT FK_relief_distributions_distributed_by
        FOREIGN KEY (distributed_by) REFERENCES dbo.users(user_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_relief_distribution_items_distribution')
BEGIN
    ALTER TABLE dbo.relief_distribution_items
    ADD CONSTRAINT FK_relief_distribution_items_distribution
        FOREIGN KEY (distribution_id) REFERENCES dbo.relief_distributions(distribution_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_relief_distribution_items_item')
BEGIN
    ALTER TABLE dbo.relief_distribution_items
    ADD CONSTRAINT FK_relief_distribution_items_item
        FOREIGN KEY (item_id) REFERENCES dbo.relief_items(item_id);
END
GO

-- notifications
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_notifications_user')
BEGIN
    ALTER TABLE dbo.notifications
    ADD CONSTRAINT FK_notifications_user
        FOREIGN KEY (user_id) REFERENCES dbo.users(user_id);
END
GO

/* =========================
   4) CHECK constraints
   ========================= */

-- rescue_requests status state machine
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_rescue_requests_status_allowed')
BEGIN
    ALTER TABLE dbo.rescue_requests
    ADD CONSTRAINT CK_rescue_requests_status_allowed
    CHECK (status IN ('Pending','Verified','In Progress','Completed','Cancelled','Duplicate'));
END
GO

-- rescue_operations status allowed
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_rescue_operations_status_allowed')
BEGIN
    ALTER TABLE dbo.rescue_operations
    ADD CONSTRAINT CK_rescue_operations_status_allowed
    CHECK (status IN ('In Progress','Completed','Cancelled'));
END
GO

-- vehicles status allowed
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_vehicles_status_allowed')
BEGIN
    ALTER TABLE dbo.vehicles
    ADD CONSTRAINT CK_vehicles_status_allowed
    CHECK (status IN ('Available','InUse','Maintenance','Disabled'));
END
GO

-- audit action allowed
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_rov_audit_action')
BEGIN
    ALTER TABLE dbo.rescue_operation_vehicle_audit
    ADD CONSTRAINT CK_rov_audit_action
    CHECK (action IN (N'ASSIGNED', N'UNASSIGNED'));
END
GO

/* =========================
   5) Uniqueness & indexes
   ========================= */

-- UNIQUE: vehicles code & plate
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_vehicles_vehicle_code' AND object_id = OBJECT_ID(N'dbo.vehicles'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_vehicles_vehicle_code
    ON dbo.vehicles(vehicle_code);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_vehicles_license_plate' AND object_id = OBJECT_ID(N'dbo.vehicles'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_vehicles_license_plate
    ON dbo.vehicles(license_plate);
END
GO

-- UNIQUE: one status per request in history
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_rrsh_request_status' AND object_id = OBJECT_ID(N'dbo.rescue_request_status_history'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_rrsh_request_status
    ON dbo.rescue_request_status_history(request_id, status);
END
GO

-- UNIQUE: 1 request -> 1 operation
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_rescue_operations_request_id' AND object_id = OBJECT_ID(N'dbo.rescue_operations'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_rescue_operations_request_id
    ON dbo.rescue_operations(request_id);
END
GO

-- UNIQUE (filtered): 1 OPEN request per citizen (OPEN excludes Completed/Cancelled/Duplicate)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_rescue_requests_one_open_per_citizen' AND object_id = OBJECT_ID(N'dbo.rescue_requests'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_rescue_requests_one_open_per_citizen
    ON dbo.rescue_requests(citizen_id)
    WHERE status <> 'Completed'
      AND status <> 'Cancelled'
      AND status <> 'Duplicate';
END
GO

-- Coordinator list index: (status, created_at DESC)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_rescue_requests_status_createdat_desc' AND object_id = OBJECT_ID(N'dbo.rescue_requests'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_rescue_requests_status_createdat_desc
    ON dbo.rescue_requests(status, created_at DESC);
END
GO

-- Helpful indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_rrsh_request_updatedat' AND object_id = OBJECT_ID(N'dbo.rescue_request_status_history'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_rrsh_request_updatedat
    ON dbo.rescue_request_status_history(request_id, updated_at DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_rov_operation' AND object_id = OBJECT_ID(N'dbo.rescue_operation_vehicles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_rov_operation
    ON dbo.rescue_operation_vehicles(operation_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_rov_vehicle' AND object_id = OBJECT_ID(N'dbo.rescue_operation_vehicles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_rov_vehicle
    ON dbo.rescue_operation_vehicles(vehicle_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_vehicles_status' AND object_id = OBJECT_ID(N'dbo.vehicles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_vehicles_status
    ON dbo.vehicles(status);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_rov_audit_operation_time' AND object_id = OBJECT_ID(N'dbo.rescue_operation_vehicle_audit'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_rov_audit_operation_time
    ON dbo.rescue_operation_vehicle_audit(operation_id, action_at DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_rov_audit_vehicle_time' AND object_id = OBJECT_ID(N'dbo.rescue_operation_vehicle_audit'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_rov_audit_vehicle_time
    ON dbo.rescue_operation_vehicle_audit(vehicle_id, action_at DESC);
END
GO

/* =========================
   6) Enforce: vehicle only in one In Progress operation
      using indexed view + unique clustered index
   ========================= */

-- Required SET options for indexed views
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'dbo.v_inprogress_vehicle_assignments', N'V') IS NULL
BEGIN
    EXEC('
    CREATE VIEW dbo.v_inprogress_vehicle_assignments
    WITH SCHEMABINDING
    AS
    SELECT
        rov.vehicle_id,
        rov.operation_id
    FROM dbo.rescue_operation_vehicles AS rov
    JOIN dbo.rescue_operations AS ro
        ON ro.operation_id = rov.operation_id
    WHERE ro.status = ''In Progress'';
    ');
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.v_inprogress_vehicle_assignments')
      AND name = N'UX_v_inprogress_vehicle_assignments_vehicle'
)
BEGIN
    CREATE UNIQUE CLUSTERED INDEX UX_v_inprogress_vehicle_assignments_vehicle
    ON dbo.v_inprogress_vehicle_assignments(vehicle_id);
END
GO

PRINT 'Database create script completed.';
GO