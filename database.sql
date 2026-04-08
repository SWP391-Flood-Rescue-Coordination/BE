USE [master]
GO
/****** Object:  Database [DisasterRescueReliefDB]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE DATABASE [DisasterRescueReliefDB]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'DisasterRescueReliefDB', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.DATNGUYEN\MSSQL\DATA\DisasterRescueReliefDB.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'DisasterRescueReliefDB_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.DATNGUYEN\MSSQL\DATA\DisasterRescueReliefDB_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT
GO
ALTER DATABASE [DisasterRescueReliefDB] SET COMPATIBILITY_LEVEL = 150
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [DisasterRescueReliefDB].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [DisasterRescueReliefDB] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET ARITHABORT OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET  ENABLE_BROKER 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET RECOVERY FULL 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET  MULTI_USER 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [DisasterRescueReliefDB] SET DB_CHAINING OFF 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [DisasterRescueReliefDB] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'DisasterRescueReliefDB', N'ON'
GO
ALTER DATABASE [DisasterRescueReliefDB] SET QUERY_STORE = OFF
GO
USE [DisasterRescueReliefDB]
GO
/****** Object:  Table [dbo].[rescue_operations]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rescue_operations](
	[operation_id] [int] IDENTITY(1,1) NOT NULL,
	[request_id] [int] NOT NULL,
	[team_id] [int] NOT NULL,
	[assigned_by] [int] NOT NULL,
	[assigned_at] [datetime2](3) NOT NULL,
	[started_at] [datetime2](3) NULL,
	[completed_at] [datetime2](3) NULL,
	[status] [varchar](20) NOT NULL,
	[number_of_affected_people] [int] NULL,
	[estimated_time] [int] NULL,
 CONSTRAINT [PK_rescue_operations] PRIMARY KEY CLUSTERED 
(
	[operation_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_operation_vehicles]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rescue_operation_vehicles](
	[operation_id] [int] NOT NULL,
	[vehicle_id] [int] NOT NULL,
	[assigned_by] [int] NOT NULL,
	[assigned_at] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_rescue_operation_vehicles] PRIMARY KEY CLUSTERED 
(
	[operation_id] ASC,
	[vehicle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[v_inprogress_vehicle_assignments]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

    CREATE VIEW [dbo].[v_inprogress_vehicle_assignments]
    WITH SCHEMABINDING
    AS
    SELECT
        rov.vehicle_id,
        rov.operation_id
    FROM dbo.rescue_operation_vehicles AS rov
    JOIN dbo.rescue_operations AS ro
        ON ro.operation_id = rov.operation_id
    WHERE ro.status = 'In Progress';
    
GO
SET ARITHABORT ON
SET CONCAT_NULL_YIELDS_NULL ON
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
SET NUMERIC_ROUNDABORT OFF
GO
/****** Object:  Index [UX_v_inprogress_vehicle_assignments_vehicle]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE UNIQUE CLUSTERED INDEX [UX_v_inprogress_vehicle_assignments_vehicle] ON [dbo].[v_inprogress_vehicle_assignments]
(
	[vehicle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[blacklisted_tokens]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[blacklisted_tokens](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[token] [nvarchar](1000) NOT NULL,
	[blacklisted_at] [datetime2](7) NULL,
	[expires_at] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[item_categories]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[item_categories](
	[category_id] [int] IDENTITY(1,1) NOT NULL,
	[category_code] [varchar](50) NOT NULL,
	[category_name] [nvarchar](100) NOT NULL,
	[description] [nvarchar](255) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_item_categories] PRIMARY KEY CLUSTERED 
(
	[category_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_item_categories_category_code] UNIQUE NONCLUSTERED 
(
	[category_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[priority_levels]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[priority_levels](
	[priority_id] [int] IDENTITY(1,1) NOT NULL,
	[level_name] [varchar](20) NOT NULL,
	[priority_order] [int] NOT NULL,
	[description] [nvarchar](255) NULL,
 CONSTRAINT [PK_priority_levels] PRIMARY KEY CLUSTERED 
(
	[priority_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[refresh_tokens]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[refresh_tokens](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[user_id] [int] NOT NULL,
	[token] [nvarchar](500) NOT NULL,
	[expires_at] [datetime2](7) NOT NULL,
	[created_at] [datetime2](7) NULL,
	[revoked_at] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[relief_items]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[relief_items](
	[item_id] [int] IDENTITY(1,1) NOT NULL,
	[item_code] [varchar](50) NOT NULL,
	[item_name] [nvarchar](200) NOT NULL,
	[category_id] [int] NOT NULL,
	[unit] [varchar](20) NOT NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[quantity] [int] NOT NULL,
	[min_quantity] [int] NOT NULL,
 CONSTRAINT [PK_relief_items] PRIMARY KEY CLUSTERED 
(
	[item_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_relief_items_item_code] UNIQUE NONCLUSTERED 
(
	[item_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_delegation_action_logs]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rescue_delegation_action_logs](
	[delegation_action_log_id] [bigint] IDENTITY(1,1) NOT NULL,
	[action_batch_id] [uniqueidentifier] NULL,
	[request_id] [int] NULL,
	[operation_id] [int] NULL,
	[actor_user_id] [int] NOT NULL,
	[member_user_id] [int] NULL,
	[action_type] [varchar](50) NOT NULL,
	[action_reason] [nvarchar](500) NULL,
	[request_status] [varchar](20) NOT NULL,
	[operation_status] [varchar](20) NULL,
	[action_at] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_rescue_delegation_action_logs] PRIMARY KEY CLUSTERED 
(
	[delegation_action_log_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_request_status_history]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rescue_request_status_history](
	[status_id] [int] IDENTITY(1,1) NOT NULL,
	[request_id] [int] NOT NULL,
	[status] [varchar](20) NOT NULL,
	[notes] [nvarchar](500) NULL,
	[updated_by] [int] NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_rescue_request_status_history] PRIMARY KEY CLUSTERED 
(
	[status_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_requests]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rescue_requests](
	[request_id] [int] IDENTITY(1,1) NOT NULL,
	[citizen_id] [int] NULL,
	[title] [nvarchar](200) NULL,
	[phone] [varchar](20) NULL,
	[description] [nvarchar](1000) NULL,
	[latitude] [decimal](9, 6) NULL,
	[longitude] [decimal](9, 6) NULL,
	[address] [nvarchar](300) NULL,
	[priority_level_id] [int] NULL,
	[status] [varchar](20) NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NULL,
	[updated_by] [int] NULL,
	[number_of_affected_people] [int] NULL,
	[contact_name] [nvarchar](100) NULL,
	[contact_phone] [varchar](20) NULL,
	[access_code] [nvarchar](50) NULL,
	[adult_count] [int] NULL,
	[elderly_count] [int] NULL,
	[children_count] [int] NULL,
	[team_id] [int] NULL,
 CONSTRAINT [PK_rescue_requests] PRIMARY KEY CLUSTERED 
(
	[request_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_team_members]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rescue_team_members](
	[team_id] [int] NOT NULL,
	[user_id] [int] NOT NULL,
	[member_role] [varchar](20) NOT NULL,
	[is_active] [bit] NOT NULL,
	[joined_at] [datetime2](3) NOT NULL,
	[request_id] [int] NULL,
 CONSTRAINT [PK_rescue_team_members] PRIMARY KEY CLUSTERED 
(
	[team_id] ASC,
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_teams]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rescue_teams](
	[team_id] [int] IDENTITY(1,1) NOT NULL,
	[team_name] [nvarchar](100) NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[base_latitude] [decimal](9, 6) NULL,
	[base_longitude] [decimal](9, 6) NULL,
	[address] [nvarchar](300) NULL,
 CONSTRAINT [PK_rescue_teams] PRIMARY KEY CLUSTERED 
(
	[team_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[stock_history]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stock_history](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[type] [varchar](3) NOT NULL,
	[date] [datetime2](7) NOT NULL,
	[body] [nvarchar](max) NULL,
	[from_to] [nvarchar](255) NULL,
	[note] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[stock_units]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[stock_units](
	[stock_unit_id] [int] IDENTITY(1,1) NOT NULL,
	[unit_code] [nvarchar](50) NOT NULL,
	[unit_name] [nvarchar](200) NOT NULL,
	[unit_type] [nvarchar](100) NULL,
	[region] [nvarchar](150) NULL,
	[address] [nvarchar](300) NULL,
	[supports_import] [bit] NOT NULL,
	[supports_export] [bit] NOT NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NULL,
 CONSTRAINT [PK_stock_units] PRIMARY KEY CLUSTERED 
(
	[stock_unit_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_stock_units_unit_code] UNIQUE NONCLUSTERED 
(
	[unit_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[users]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[users](
	[user_id] [int] IDENTITY(1,1) NOT NULL,
	[username] [varchar](50) NOT NULL,
	[password_hash] [varchar](255) NOT NULL,
	[full_name] [nvarchar](100) NULL,
	[phone] [varchar](20) NULL,
	[email] [varchar](100) NULL,
	[role] [varchar](20) NOT NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[address] [nvarchar](300) NULL,
 CONSTRAINT [PK_users] PRIMARY KEY CLUSTERED 
(
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[vehicle_types]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[vehicle_types](
	[vehicle_type_id] [int] IDENTITY(1,1) NOT NULL,
	[type_code] [varchar](50) NOT NULL,
	[type_name] [varchar](50) NOT NULL,
	[description] [nvarchar](255) NULL,
 CONSTRAINT [PK_vehicle_types] PRIMARY KEY CLUSTERED 
(
	[vehicle_type_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[vehicles]    Script Date: 4/8/2026 4:14:55 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[vehicles](
	[vehicle_id] [int] IDENTITY(1,1) NOT NULL,
	[vehicle_code] [varchar](20) NOT NULL,
	[vehicle_name] [nvarchar](100) NULL,
	[vehicle_type_id] [int] NOT NULL,
	[license_plate] [varchar](20) NOT NULL,
	[capacity] [int] NULL,
	[status] [varchar](20) NOT NULL,
	[current_location] [nvarchar](300) NULL,
	[last_maintenance] [date] NULL,
	[updated_at] [datetime2](3) NULL,
 CONSTRAINT [PK_vehicles] PRIMARY KEY CLUSTERED 
(
	[vehicle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_BlacklistedTokens_ExpiresAt]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_BlacklistedTokens_ExpiresAt] ON [dbo].[blacklisted_tokens]
(
	[expires_at] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_BlacklistedTokens_Token]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_BlacklistedTokens_Token] ON [dbo].[blacklisted_tokens]
(
	[token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_RefreshTokens_Token]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_Token] ON [dbo].[refresh_tokens]
(
	[token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RefreshTokens_UserId]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] ON [dbo].[refresh_tokens]
(
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rdal_action_batch]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_rdal_action_batch] ON [dbo].[rescue_delegation_action_logs]
(
	[action_batch_id] ASC,
	[action_at] DESC
)
INCLUDE([operation_id],[request_id],[actor_user_id],[member_user_id],[action_type],[action_reason],[request_status],[operation_status]) 
WHERE ([action_batch_id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rdal_actor_history]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_rdal_actor_history] ON [dbo].[rescue_delegation_action_logs]
(
	[actor_user_id] ASC,
	[action_at] DESC
)
INCLUDE([operation_id],[request_id],[member_user_id],[action_type],[action_reason],[request_status],[operation_status],[action_batch_id]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rdal_member_history]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_rdal_member_history] ON [dbo].[rescue_delegation_action_logs]
(
	[member_user_id] ASC,
	[action_at] DESC
)
INCLUDE([operation_id],[request_id],[action_type],[action_reason],[actor_user_id],[request_status],[operation_status],[action_batch_id]) 
WHERE ([member_user_id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rdal_operation_history]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_rdal_operation_history] ON [dbo].[rescue_delegation_action_logs]
(
	[operation_id] ASC,
	[action_at] DESC
)
INCLUDE([action_type],[action_reason],[actor_user_id],[member_user_id],[request_status],[operation_status],[action_batch_id]) 
WHERE ([operation_id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rdal_request_history]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_rdal_request_history] ON [dbo].[rescue_delegation_action_logs]
(
	[request_id] ASC,
	[action_at] DESC
)
INCLUDE([operation_id],[action_type],[action_reason],[actor_user_id],[member_user_id],[request_status],[operation_status],[action_batch_id]) 
WHERE ([request_id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_operation]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_rov_operation] ON [dbo].[rescue_operation_vehicles]
(
	[operation_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_vehicle]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_rov_vehicle] ON [dbo].[rescue_operation_vehicles]
(
	[vehicle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_rescue_operations_request_id]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rescue_operations_request_id] ON [dbo].[rescue_operations]
(
	[request_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rrsh_request_updatedat]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_rrsh_request_updatedat] ON [dbo].[rescue_request_status_history]
(
	[request_id] ASC,
	[updated_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_rrsh_request_status]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rrsh_request_status] ON [dbo].[rescue_request_status_history]
(
	[request_id] ASC,
	[status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_rescue_requests_status_createdat_desc]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_rescue_requests_status_createdat_desc] ON [dbo].[rescue_requests]
(
	[status] ASC,
	[created_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_rescue_requests_one_open_per_citizen]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rescue_requests_one_open_per_citizen] ON [dbo].[rescue_requests]
(
	[citizen_id] ASC
)
WHERE ([status]<>'Completed' AND [status]<>'Cancelled' AND [status]<>'Duplicate' AND [citizen_id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_rtm_one_active_assignment_per_user]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rtm_one_active_assignment_per_user] ON [dbo].[rescue_team_members]
(
	[user_id] ASC
)
WHERE ([is_active]=(1) AND [request_id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_stock_units_active_export]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_stock_units_active_export] ON [dbo].[stock_units]
(
	[is_active] ASC,
	[supports_export] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_stock_units_active_import]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_stock_units_active_import] ON [dbo].[stock_units]
(
	[is_active] ASC,
	[supports_import] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_vehicles_status]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE NONCLUSTERED INDEX [IX_vehicles_status] ON [dbo].[vehicles]
(
	[status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_vehicles_license_plate]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UQ_vehicles_license_plate] ON [dbo].[vehicles]
(
	[license_plate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_vehicles_vehicle_code]    Script Date: 4/8/2026 4:14:55 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UQ_vehicles_vehicle_code] ON [dbo].[vehicles]
(
	[vehicle_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[blacklisted_tokens] ADD  DEFAULT (getdate()) FOR [blacklisted_at]
GO
ALTER TABLE [dbo].[item_categories] ADD  CONSTRAINT [DF_item_categories_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[item_categories] ADD  CONSTRAINT [DF_item_categories_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [dbo].[refresh_tokens] ADD  DEFAULT (getdate()) FOR [created_at]
GO
ALTER TABLE [dbo].[relief_items] ADD  CONSTRAINT [DF_relief_items_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[relief_items] ADD  CONSTRAINT [DF_relief_items_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [dbo].[relief_items] ADD  DEFAULT ((0)) FOR [quantity]
GO
ALTER TABLE [dbo].[relief_items] ADD  DEFAULT ((0)) FOR [min_quantity]
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs] ADD  CONSTRAINT [DF_rdal_action_at]  DEFAULT (sysutcdatetime()) FOR [action_at]
GO
ALTER TABLE [dbo].[rescue_operation_vehicles] ADD  CONSTRAINT [DF_rov_assigned_at]  DEFAULT (sysutcdatetime()) FOR [assigned_at]
GO
ALTER TABLE [dbo].[rescue_operations] ADD  CONSTRAINT [DF_rescue_operations_assigned_at]  DEFAULT (sysutcdatetime()) FOR [assigned_at]
GO
ALTER TABLE [dbo].[rescue_request_status_history] ADD  CONSTRAINT [DF_rrsh_updated_at]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO
ALTER TABLE [dbo].[rescue_requests] ADD  CONSTRAINT [DF_rescue_requests_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [dbo].[rescue_team_members] ADD  CONSTRAINT [DF_rescue_team_members_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[rescue_team_members] ADD  CONSTRAINT [DF_rescue_team_members_joined_at]  DEFAULT (sysutcdatetime()) FOR [joined_at]
GO
ALTER TABLE [dbo].[rescue_teams] ADD  CONSTRAINT [DF_rescue_teams_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [dbo].[stock_history] ADD  DEFAULT (getdate()) FOR [date]
GO
ALTER TABLE [dbo].[stock_units] ADD  CONSTRAINT [DF_stock_units_supports_import]  DEFAULT ((1)) FOR [supports_import]
GO
ALTER TABLE [dbo].[stock_units] ADD  CONSTRAINT [DF_stock_units_supports_export]  DEFAULT ((1)) FOR [supports_export]
GO
ALTER TABLE [dbo].[stock_units] ADD  CONSTRAINT [DF_stock_units_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[stock_units] ADD  CONSTRAINT [DF_stock_units_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [dbo].[users] ADD  CONSTRAINT [DF_users_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[users] ADD  CONSTRAINT [DF_users_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [dbo].[refresh_tokens]  WITH CHECK ADD  CONSTRAINT [FK_RefreshTokens_Users] FOREIGN KEY([user_id])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[refresh_tokens] CHECK CONSTRAINT [FK_RefreshTokens_Users]
GO
ALTER TABLE [dbo].[relief_items]  WITH CHECK ADD  CONSTRAINT [FK_relief_items_category] FOREIGN KEY([category_id])
REFERENCES [dbo].[item_categories] ([category_id])
GO
ALTER TABLE [dbo].[relief_items] CHECK CONSTRAINT [FK_relief_items_category]
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs]  WITH CHECK ADD  CONSTRAINT [FK_rdal_actor_user] FOREIGN KEY([actor_user_id])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs] CHECK CONSTRAINT [FK_rdal_actor_user]
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs]  WITH CHECK ADD  CONSTRAINT [FK_rdal_member_user] FOREIGN KEY([member_user_id])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs] CHECK CONSTRAINT [FK_rdal_member_user]
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs]  WITH CHECK ADD  CONSTRAINT [FK_rdal_operation] FOREIGN KEY([operation_id])
REFERENCES [dbo].[rescue_operations] ([operation_id])
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs] CHECK CONSTRAINT [FK_rdal_operation]
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs]  WITH CHECK ADD  CONSTRAINT [FK_rdal_request] FOREIGN KEY([request_id])
REFERENCES [dbo].[rescue_requests] ([request_id])
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs] CHECK CONSTRAINT [FK_rdal_request]
GO
ALTER TABLE [dbo].[rescue_operation_vehicles]  WITH CHECK ADD  CONSTRAINT [FK_rov_assigned_by] FOREIGN KEY([assigned_by])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[rescue_operation_vehicles] CHECK CONSTRAINT [FK_rov_assigned_by]
GO
ALTER TABLE [dbo].[rescue_operation_vehicles]  WITH CHECK ADD  CONSTRAINT [FK_rov_operation] FOREIGN KEY([operation_id])
REFERENCES [dbo].[rescue_operations] ([operation_id])
GO
ALTER TABLE [dbo].[rescue_operation_vehicles] CHECK CONSTRAINT [FK_rov_operation]
GO
ALTER TABLE [dbo].[rescue_operation_vehicles]  WITH CHECK ADD  CONSTRAINT [FK_rov_vehicle] FOREIGN KEY([vehicle_id])
REFERENCES [dbo].[vehicles] ([vehicle_id])
GO
ALTER TABLE [dbo].[rescue_operation_vehicles] CHECK CONSTRAINT [FK_rov_vehicle]
GO
ALTER TABLE [dbo].[rescue_operations]  WITH CHECK ADD  CONSTRAINT [FK_rescue_operations_assigned_by] FOREIGN KEY([assigned_by])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[rescue_operations] CHECK CONSTRAINT [FK_rescue_operations_assigned_by]
GO
ALTER TABLE [dbo].[rescue_operations]  WITH CHECK ADD  CONSTRAINT [FK_rescue_operations_request] FOREIGN KEY([request_id])
REFERENCES [dbo].[rescue_requests] ([request_id])
GO
ALTER TABLE [dbo].[rescue_operations] CHECK CONSTRAINT [FK_rescue_operations_request]
GO
ALTER TABLE [dbo].[rescue_operations]  WITH CHECK ADD  CONSTRAINT [FK_rescue_operations_team] FOREIGN KEY([team_id])
REFERENCES [dbo].[rescue_teams] ([team_id])
GO
ALTER TABLE [dbo].[rescue_operations] CHECK CONSTRAINT [FK_rescue_operations_team]
GO
ALTER TABLE [dbo].[rescue_request_status_history]  WITH CHECK ADD  CONSTRAINT [FK_rrsh_request] FOREIGN KEY([request_id])
REFERENCES [dbo].[rescue_requests] ([request_id])
GO
ALTER TABLE [dbo].[rescue_request_status_history] CHECK CONSTRAINT [FK_rrsh_request]
GO
ALTER TABLE [dbo].[rescue_requests]  WITH CHECK ADD  CONSTRAINT [FK_rescue_requests_citizen] FOREIGN KEY([citizen_id])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[rescue_requests] CHECK CONSTRAINT [FK_rescue_requests_citizen]
GO
ALTER TABLE [dbo].[rescue_requests]  WITH CHECK ADD  CONSTRAINT [FK_rescue_requests_priority] FOREIGN KEY([priority_level_id])
REFERENCES [dbo].[priority_levels] ([priority_id])
GO
ALTER TABLE [dbo].[rescue_requests] CHECK CONSTRAINT [FK_rescue_requests_priority]
GO
ALTER TABLE [dbo].[rescue_requests]  WITH CHECK ADD  CONSTRAINT [FK_rescue_requests_team] FOREIGN KEY([team_id])
REFERENCES [dbo].[rescue_teams] ([team_id])
GO
ALTER TABLE [dbo].[rescue_requests] CHECK CONSTRAINT [FK_rescue_requests_team]
GO
ALTER TABLE [dbo].[rescue_team_members]  WITH CHECK ADD  CONSTRAINT [FK_rescue_team_members_request] FOREIGN KEY([request_id])
REFERENCES [dbo].[rescue_requests] ([request_id])
GO
ALTER TABLE [dbo].[rescue_team_members] CHECK CONSTRAINT [FK_rescue_team_members_request]
GO
ALTER TABLE [dbo].[rescue_team_members]  WITH CHECK ADD  CONSTRAINT [FK_rescue_team_members_team] FOREIGN KEY([team_id])
REFERENCES [dbo].[rescue_teams] ([team_id])
GO
ALTER TABLE [dbo].[rescue_team_members] CHECK CONSTRAINT [FK_rescue_team_members_team]
GO
ALTER TABLE [dbo].[rescue_team_members]  WITH CHECK ADD  CONSTRAINT [FK_rescue_team_members_user] FOREIGN KEY([user_id])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[rescue_team_members] CHECK CONSTRAINT [FK_rescue_team_members_user]
GO
ALTER TABLE [dbo].[vehicles]  WITH CHECK ADD  CONSTRAINT [FK_vehicles_vehicle_type] FOREIGN KEY([vehicle_type_id])
REFERENCES [dbo].[vehicle_types] ([vehicle_type_id])
GO
ALTER TABLE [dbo].[vehicles] CHECK CONSTRAINT [FK_vehicles_vehicle_type]
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs]  WITH CHECK ADD  CONSTRAINT [CK_rdal_action_type_allowed] CHECK  (([action_type]='LEADER_REJECTED_REQUEST' OR [action_type]='LEADER_FAILED_OPERATION' OR [action_type]='LEADER_COMPLETED_OPERATION' OR [action_type]='LEADER_REMOVED_MEMBER' OR [action_type]='MEMBER_COMPLETED' OR [action_type]='MEMBER_REQUESTED_SUPPORT' OR [action_type]='LEADER_ASSIGNED_MEMBER'))
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs] CHECK CONSTRAINT [CK_rdal_action_type_allowed]
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs]  WITH CHECK ADD  CONSTRAINT [CK_rdal_member_actions_actor_rule] CHECK  ((NOT ([action_type]='MEMBER_COMPLETED' OR [action_type]='MEMBER_REQUESTED_SUPPORT') OR [actor_user_id]=[member_user_id]))
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs] CHECK CONSTRAINT [CK_rdal_member_actions_actor_rule]
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs]  WITH CHECK ADD  CONSTRAINT [CK_rdal_member_user_required_by_action] CHECK  ((([action_type]='LEADER_REMOVED_MEMBER' OR [action_type]='MEMBER_COMPLETED' OR [action_type]='MEMBER_REQUESTED_SUPPORT' OR [action_type]='LEADER_ASSIGNED_MEMBER') AND [member_user_id] IS NOT NULL AND [operation_id] IS NOT NULL OR ([action_type]='LEADER_FAILED_OPERATION' OR [action_type]='LEADER_COMPLETED_OPERATION') AND [member_user_id] IS NULL AND [operation_id] IS NOT NULL OR [action_type]='LEADER_REJECTED_REQUEST' AND [member_user_id] IS NULL AND [request_id] IS NOT NULL))
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs] CHECK CONSTRAINT [CK_rdal_member_user_required_by_action]
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs]  WITH CHECK ADD  CONSTRAINT [CK_rdal_operation_status_required_by_action] CHECK  (([action_type]='LEADER_REJECTED_REQUEST' OR [operation_status] IS NOT NULL))
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs] CHECK CONSTRAINT [CK_rdal_operation_status_required_by_action]
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs]  WITH CHECK ADD  CONSTRAINT [CK_rdal_reason_required_by_action] CHECK  ((NOT ([action_type]='LEADER_FAILED_OPERATION' OR [action_type]='LEADER_REJECTED_REQUEST') OR [action_reason] IS NOT NULL AND len(ltrim(rtrim([action_reason])))>=(1)))
GO
ALTER TABLE [dbo].[rescue_delegation_action_logs] CHECK CONSTRAINT [CK_rdal_reason_required_by_action]
GO
ALTER TABLE [dbo].[rescue_operations]  WITH CHECK ADD  CONSTRAINT [CK_rescue_operations_status_allowed] CHECK  (([status]='Failed' OR [status]='Completed' OR [status]='Assigned' OR [status]='Waiting'))
GO
ALTER TABLE [dbo].[rescue_operations] CHECK CONSTRAINT [CK_rescue_operations_status_allowed]
GO
ALTER TABLE [dbo].[rescue_requests]  WITH CHECK ADD  CONSTRAINT [CK_rescue_requests_status_allowed] CHECK  (([status]='Duplicate' OR [status]='Cancelled' OR [status]='Completed' OR [status]='Assigned' OR [status]='Verified' OR [status]='Pending'))
GO
ALTER TABLE [dbo].[rescue_requests] CHECK CONSTRAINT [CK_rescue_requests_status_allowed]
GO
ALTER TABLE [dbo].[stock_history]  WITH CHECK ADD CHECK  (([type]='OUT' OR [type]='IN'))
GO
ALTER TABLE [dbo].[vehicles]  WITH CHECK ADD  CONSTRAINT [CK_vehicles_status_allowed] CHECK  (([status]='Disabled' OR [status]='Maintenance' OR [status]='InUse' OR [status]='Available'))
GO
ALTER TABLE [dbo].[vehicles] CHECK CONSTRAINT [CK_vehicles_status_allowed]
GO
USE [master]
GO
ALTER DATABASE [DisasterRescueReliefDB] SET  READ_WRITE 
GO
