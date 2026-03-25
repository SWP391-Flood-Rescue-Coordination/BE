USE [master]
GO
/****** Object:  Database [DisasterRescueReliefDB]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Table [dbo].[rescue_operations]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Table [dbo].[rescue_operation_vehicles]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  View [dbo].[v_inprogress_vehicle_assignments]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Index [UX_v_inprogress_vehicle_assignments_vehicle]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE UNIQUE CLUSTERED INDEX [UX_v_inprogress_vehicle_assignments_vehicle] ON [dbo].[v_inprogress_vehicle_assignments]
(
	[vehicle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[blacklisted_tokens]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Table [dbo].[item_categories]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Table [dbo].[notifications]    Script Date: 3/6/2026 10:53:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[notifications](
	[notification_id] [int] IDENTITY(1,1) NOT NULL,
	[user_id] [int] NOT NULL,
	[title] [nvarchar](200) NOT NULL,
	[message] [nvarchar](1000) NOT NULL,
	[is_read] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_notifications] PRIMARY KEY CLUSTERED 
(
	[notification_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[priority_levels]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Table [dbo].[refresh_tokens]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Table [dbo].[relief_distribution_items]    Script Date: 3/6/2026 10:53:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[relief_distribution_items](
	[distribution_item_id] [int] IDENTITY(1,1) NOT NULL,
	[distribution_id] [int] NOT NULL,
	[item_id] [int] NOT NULL,
	[quantity] [int] NOT NULL,
 CONSTRAINT [PK_relief_distribution_items] PRIMARY KEY CLUSTERED 
(
	[distribution_item_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[relief_distributions]    Script Date: 3/6/2026 10:53:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[relief_distributions](
	[distribution_id] [int] IDENTITY(1,1) NOT NULL,
	[request_id] [int] NOT NULL,
	[distributed_by] [int] NOT NULL,
	[distribution_date] [datetime2](3) NULL,
	[status] [varchar](20) NULL,
	[notes] [nvarchar](500) NULL,
 CONSTRAINT [PK_relief_distributions] PRIMARY KEY CLUSTERED 
(
	[distribution_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[relief_items]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Table [dbo].[rescue_operation_reports]    Script Date: 3/6/2026 10:53:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rescue_operation_reports](
	[report_id] [int] IDENTITY(1,1) NOT NULL,
	[operation_id] [int] NOT NULL,
	[people_rescued] [int] NULL,
	[situation_description] [nvarchar](1000) NULL,
	[actions_taken] [nvarchar](1000) NULL,
	[reported_by] [int] NOT NULL,
	[reported_at] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_rescue_operation_reports] PRIMARY KEY CLUSTERED 
(
	[report_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_operation_vehicle_audit]    Script Date: 3/6/2026 10:53:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rescue_operation_vehicle_audit](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[operation_id] [int] NOT NULL,
	[vehicle_id] [int] NOT NULL,
	[action] [nvarchar](20) NOT NULL,
	[action_at] [datetime2](3) NOT NULL,
	[action_by] [int] NOT NULL,
	[note] [nvarchar](500) NULL,
 CONSTRAINT [PK_rescue_operation_vehicle_audit] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_request_status_history]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Table [dbo].[rescue_requests]    Script Date: 3/6/2026 10:53:04 PM ******/
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
 CONSTRAINT [PK_rescue_requests] PRIMARY KEY CLUSTERED 
(
	[request_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_team_members]    Script Date: 3/6/2026 10:53:04 PM ******/
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
	[left_at] [datetime2](3) NULL,
 CONSTRAINT [PK_rescue_team_members] PRIMARY KEY CLUSTERED 
(
	[team_id] ASC,
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_teams]    Script Date: 3/6/2026 10:53:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rescue_teams](
	[team_id] [int] IDENTITY(1,1) NOT NULL,
	[team_name] [nvarchar](100) NOT NULL,
	[status] [varchar](20) NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_rescue_teams] PRIMARY KEY CLUSTERED 
(
	[team_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[stock_history]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Table [dbo].[users]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Table [dbo].[vehicle_types]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Table [dbo].[vehicles]    Script Date: 3/6/2026 10:53:04 PM ******/
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
/****** Object:  Index [IX_BlacklistedTokens_ExpiresAt]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE NONCLUSTERED INDEX [IX_BlacklistedTokens_ExpiresAt] ON [dbo].[blacklisted_tokens]
(
	[expires_at] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_BlacklistedTokens_Token]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE NONCLUSTERED INDEX [IX_BlacklistedTokens_Token] ON [dbo].[blacklisted_tokens]
(
	[token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_RefreshTokens_Token]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_Token] ON [dbo].[refresh_tokens]
(
	[token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RefreshTokens_UserId]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] ON [dbo].[refresh_tokens]
(
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_audit_operation_time]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE NONCLUSTERED INDEX [IX_rov_audit_operation_time] ON [dbo].[rescue_operation_vehicle_audit]
(
	[operation_id] ASC,
	[action_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_audit_vehicle_time]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE NONCLUSTERED INDEX [IX_rov_audit_vehicle_time] ON [dbo].[rescue_operation_vehicle_audit]
(
	[vehicle_id] ASC,
	[action_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_operation]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE NONCLUSTERED INDEX [IX_rov_operation] ON [dbo].[rescue_operation_vehicles]
(
	[operation_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_vehicle]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE NONCLUSTERED INDEX [IX_rov_vehicle] ON [dbo].[rescue_operation_vehicles]
(
	[vehicle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_rescue_operations_request_id]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rescue_operations_request_id] ON [dbo].[rescue_operations]
(
	[request_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rrsh_request_updatedat]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE NONCLUSTERED INDEX [IX_rrsh_request_updatedat] ON [dbo].[rescue_request_status_history]
(
	[request_id] ASC,
	[updated_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_rrsh_request_status]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rrsh_request_status] ON [dbo].[rescue_request_status_history]
(
	[request_id] ASC,
	[status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_rescue_requests_status_createdat_desc]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE NONCLUSTERED INDEX [IX_rescue_requests_status_createdat_desc] ON [dbo].[rescue_requests]
(
	[status] ASC,
	[created_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_rescue_requests_one_open_per_citizen]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rescue_requests_one_open_per_citizen] ON [dbo].[rescue_requests]
(
	[citizen_id] ASC
)
WHERE ([status]<>'Completed' AND [status]<>'Cancelled' AND [status]<>'Duplicate' AND [citizen_id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_vehicles_status]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE NONCLUSTERED INDEX [IX_vehicles_status] ON [dbo].[vehicles]
(
	[status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_vehicles_license_plate]    Script Date: 3/6/2026 10:53:04 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UQ_vehicles_license_plate] ON [dbo].[vehicles]
(
	[license_plate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_vehicles_vehicle_code]    Script Date: 3/6/2026 10:53:04 PM ******/
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
ALTER TABLE [dbo].[notifications] ADD  CONSTRAINT [DF_notifications_is_read]  DEFAULT ((0)) FOR [is_read]
GO
ALTER TABLE [dbo].[notifications] ADD  CONSTRAINT [DF_notifications_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
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
ALTER TABLE [dbo].[rescue_operation_reports] ADD  CONSTRAINT [DF_rescue_operation_reports_reported_at]  DEFAULT (sysutcdatetime()) FOR [reported_at]
GO
ALTER TABLE [dbo].[rescue_operation_vehicle_audit] ADD  CONSTRAINT [DF_rov_audit_action_at]  DEFAULT (sysutcdatetime()) FOR [action_at]
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
ALTER TABLE [dbo].[users] ADD  CONSTRAINT [DF_users_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[users] ADD  CONSTRAINT [DF_users_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [dbo].[notifications]  WITH CHECK ADD  CONSTRAINT [FK_notifications_user] FOREIGN KEY([user_id])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[notifications] CHECK CONSTRAINT [FK_notifications_user]
GO
ALTER TABLE [dbo].[refresh_tokens]  WITH CHECK ADD  CONSTRAINT [FK_RefreshTokens_Users] FOREIGN KEY([user_id])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[refresh_tokens] CHECK CONSTRAINT [FK_RefreshTokens_Users]
GO
ALTER TABLE [dbo].[relief_distribution_items]  WITH CHECK ADD  CONSTRAINT [FK_relief_distribution_items_distribution] FOREIGN KEY([distribution_id])
REFERENCES [dbo].[relief_distributions] ([distribution_id])
GO
ALTER TABLE [dbo].[relief_distribution_items] CHECK CONSTRAINT [FK_relief_distribution_items_distribution]
GO
ALTER TABLE [dbo].[relief_distribution_items]  WITH CHECK ADD  CONSTRAINT [FK_relief_distribution_items_item] FOREIGN KEY([item_id])
REFERENCES [dbo].[relief_items] ([item_id])
GO
ALTER TABLE [dbo].[relief_distribution_items] CHECK CONSTRAINT [FK_relief_distribution_items_item]
GO
ALTER TABLE [dbo].[relief_distributions]  WITH CHECK ADD  CONSTRAINT [FK_relief_distributions_distributed_by] FOREIGN KEY([distributed_by])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[relief_distributions] CHECK CONSTRAINT [FK_relief_distributions_distributed_by]
GO
ALTER TABLE [dbo].[relief_distributions]  WITH CHECK ADD  CONSTRAINT [FK_relief_distributions_request] FOREIGN KEY([request_id])
REFERENCES [dbo].[rescue_requests] ([request_id])
GO
ALTER TABLE [dbo].[relief_distributions] CHECK CONSTRAINT [FK_relief_distributions_request]
GO
ALTER TABLE [dbo].[relief_items]  WITH CHECK ADD  CONSTRAINT [FK_relief_items_category] FOREIGN KEY([category_id])
REFERENCES [dbo].[item_categories] ([category_id])
GO
ALTER TABLE [dbo].[relief_items] CHECK CONSTRAINT [FK_relief_items_category]
GO
ALTER TABLE [dbo].[rescue_operation_reports]  WITH CHECK ADD  CONSTRAINT [FK_rescue_operation_reports_operation] FOREIGN KEY([operation_id])
REFERENCES [dbo].[rescue_operations] ([operation_id])
GO
ALTER TABLE [dbo].[rescue_operation_reports] CHECK CONSTRAINT [FK_rescue_operation_reports_operation]
GO
ALTER TABLE [dbo].[rescue_operation_reports]  WITH CHECK ADD  CONSTRAINT [FK_rescue_operation_reports_reported_by] FOREIGN KEY([reported_by])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[rescue_operation_reports] CHECK CONSTRAINT [FK_rescue_operation_reports_reported_by]
GO
ALTER TABLE [dbo].[rescue_operation_vehicle_audit]  WITH CHECK ADD  CONSTRAINT [FK_rov_audit_action_by] FOREIGN KEY([action_by])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[rescue_operation_vehicle_audit] CHECK CONSTRAINT [FK_rov_audit_action_by]
GO
ALTER TABLE [dbo].[rescue_operation_vehicle_audit]  WITH CHECK ADD  CONSTRAINT [FK_rov_audit_operation] FOREIGN KEY([operation_id])
REFERENCES [dbo].[rescue_operations] ([operation_id])
GO
ALTER TABLE [dbo].[rescue_operation_vehicle_audit] CHECK CONSTRAINT [FK_rov_audit_operation]
GO
ALTER TABLE [dbo].[rescue_operation_vehicle_audit]  WITH CHECK ADD  CONSTRAINT [FK_rov_audit_vehicle] FOREIGN KEY([vehicle_id])
REFERENCES [dbo].[vehicles] ([vehicle_id])
GO
ALTER TABLE [dbo].[rescue_operation_vehicle_audit] CHECK CONSTRAINT [FK_rov_audit_vehicle]
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
ALTER TABLE [dbo].[rescue_request_status_history]  WITH CHECK ADD  CONSTRAINT [FK_rrsh_updated_by] FOREIGN KEY([updated_by])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[rescue_request_status_history] CHECK CONSTRAINT [FK_rrsh_updated_by]
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
ALTER TABLE [dbo].[rescue_requests]  WITH CHECK ADD  CONSTRAINT [FK_rescue_requests_updated_by] FOREIGN KEY([updated_by])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[rescue_requests] CHECK CONSTRAINT [FK_rescue_requests_updated_by]
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
ALTER TABLE [dbo].[rescue_operation_vehicle_audit]  WITH CHECK ADD  CONSTRAINT [CK_rov_audit_action] CHECK  (([action]=N'UNASSIGNED' OR [action]=N'ASSIGNED'))
GO
ALTER TABLE [dbo].[rescue_operation_vehicle_audit] CHECK CONSTRAINT [CK_rov_audit_action]
GO
ALTER TABLE [dbo].[rescue_operations]  WITH CHECK ADD  CONSTRAINT [CK_rescue_operations_status_allowed] CHECK  (([status]='Duplicate' OR [status]='Cancelled' OR [status]='Completed' OR [status]='Confirmed' OR [status]='Assigned' OR [status]='Verified' OR [status]='Pending'))
GO
ALTER TABLE [dbo].[rescue_operations] CHECK CONSTRAINT [CK_rescue_operations_status_allowed]
GO
ALTER TABLE [dbo].[rescue_requests]  WITH CHECK ADD  CONSTRAINT [CK_rescue_requests_status_allowed] CHECK  (([status]='Duplicate' OR [status]='Cancelled' OR [status]='Completed' OR [status]='Confirmed' OR [status]='Assigned' OR [status]='Verified' OR [status]='Pending'))
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
GO

CREATE TABLE [dbo].[stock_history] (
    [id]       INT            IDENTITY(1,1) NOT NULL,
    [type]     VARCHAR(3)     NOT NULL,        -- 'IN' hoặc 'OUT'
    [date]     DATETIME2(3)   NOT NULL,
    [body]     NVARCHAR(500)  NOT NULL,        -- danh sách item-số lượng, vd: '1-200,5-100'
    [from_to]  NVARCHAR(200)  NULL,            -- nguồn nhập / nơi xuất
    [note]     NVARCHAR(500)  NULL,
    CONSTRAINT [PK_stock_history] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_stock_history_type] CHECK ([type] IN ('IN', 'OUT'))
)
GO
SET IDENTITY_INSERT stock_history ON;

INSERT INTO stock_history (id, type, date, body, from_to, note) VALUES
-- Phiếu NHẬP (IN)
(1, 'IN',  DATEADD(DAY, -10, GETDATE()), '1-5000,2-10000',          N'Nhà cung cấp Minh Phát',     N'Nhập lương thực đợt 1 đầu mùa lũ'),
(2, 'IN',  DATEADD(DAY, -9,  GETDATE()), '5-15000,6-1000',          N'Nhà cung cấp Aqua Việt',     N'Nhập nước uống dự trữ'),
(3, 'IN',  DATEADD(DAY, -8,  GETDATE()), '3-3000,4-2000',           N'Nhà cung cấp Minh Phát',     N'Nhập thịt hộp và sữa'),
(4, 'IN',  DATEADD(DAY, -7,  GETDATE()), '7-500,8-300,9-400,10-2000', N'Kho Y Tế Quận 7',          N'Nhập vật tư y tế đợt 1'),
(5, 'IN',  DATEADD(DAY, -6,  GETDATE()), '11-3000,12-1500,13-2000', N'Nhà cung cấp Dệt May ABC',   N'Nhập quần áo và đồ dùng'),
(6, 'IN',  DATEADD(DAY, -5,  GETDATE()), '14-200,15-800',           N'Tổ chức Cứu Trợ Quốc Tế',   N'Nhận lều và bạt viện trợ'),

-- Phiếu XUẤT (OUT)
(7, 'OUT', DATEADD(DAY, -4,  GETDATE()), '1-200,5-100,3-50',        N'Yêu cầu cứu hộ #1 - Q7',    N'Xuất cứu trợ khẩn cấp cho 5 người'),
(8, 'OUT', DATEADD(DAY, -3,  GETDATE()), '2-300,5-200,1-150,12-20', N'Yêu cầu cứu hộ #2 - Q9',    N'Xuất hàng cho khu vực bị cô lập'),
(9, 'OUT', DATEADD(DAY, -2,  GETDATE()), '7-50,8-30,9-40,10-100',   N'Yêu cầu cứu hộ #3 - Thủ Đức', N'Xuất vật tư y tế cho người bị thương'),
(10,'OUT', DATEADD(DAY, -1,  GETDATE()), '11-100,14-10,15-30',      N'Điểm sơ tán tập trung Q1',   N'Hỗ trợ khu sơ tán tập trung'),
(11,'IN',  GETDATE(),                    '1-2000,2-3000,5-5000',     N'Ủy ban nhân dân TP.HCM',     N'Nhận hàng bổ sung từ UBND thành phố'),
(12,'OUT', GETDATE(),                    '4-100,9-50,10-200',        N'Trại cứu trợ Quận 8',        N'Xuất sữa và thuốc cho trại cứu trợ');

SET IDENTITY_INSERT stock_history OFF;
GO
-- =============================================
-- MIGRATION: Add quantity & min_quantity to relief_items
-- Then insert 20 items from screenshot data
-- =============================================

USE [DisasterRescueReliefDB];
GO

-- 1. Add columns if not exist
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE Name = N'quantity' AND Object_ID = OBJECT_ID(N'relief_items')
)
BEGIN
    PRINT 'Adding column: quantity';
    ALTER TABLE [dbo].[relief_items]
    ADD [quantity] INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE Name = N'min_quantity' AND Object_ID = OBJECT_ID(N'relief_items')
)
BEGIN
    PRINT 'Adding column: min_quantity';
    ALTER TABLE [dbo].[relief_items]
    ADD [min_quantity] INT NOT NULL DEFAULT 0;
END
GO

-- 2. Ensure item_categories có đủ category
-- category_id: 1=Lương thực, 2=Nước, 3=Y tế, 4=Quần áo, 5=Nơi ở

-- Insert categories nếu chưa có
IF NOT EXISTS (SELECT 1 FROM [dbo].[item_categories] WHERE [category_id] = 1)
BEGIN
    SET IDENTITY_INSERT [dbo].[item_categories] ON;
    INSERT INTO [dbo].[item_categories] ([category_id], [category_code], [category_name], [description], [is_active], [created_at])
    VALUES 
        (1, 'FOOD',  N'Lương thực',    N'Các loại thực phẩm cứu trợ',       1, SYSUTCDATETIME()),
        (2, 'WATER', N'Nước',          N'Nước uống và lọc nước',             1, SYSUTCDATETIME()),
        (3, 'MED',   N'Y tế',          N'Vật tư y tế và thuốc men',          1, SYSUTCDATETIME()),
        (4, 'CLO',   N'Quần áo',       N'Quần áo và đồ dùng thiết yếu',      1, SYSUTCDATETIME()),
        (5, 'SHE',   N'Nơi ở',         N'Lều, bạt và thiết bị tạm trú',      1, SYSUTCDATETIME());
    SET IDENTITY_INSERT [dbo].[item_categories] OFF;
END
GO

-- 3. Insert 20 relief_items (dữ liệu từ ảnh)
SET IDENTITY_INSERT [dbo].[relief_items] ON;

-- Xoá data cũ nếu cần chạy lại (comment out nếu không muốn xoá)
-- DELETE FROM [dbo].[relief_items] WHERE item_id BETWEEN 1 AND 20;

INSERT INTO [dbo].[relief_items] 
    ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity])
VALUES
-- Lương thực (category_id = 1)
(1,  'FOOD-001', N'Mì gói',             1, N'Thùng', 1, '2026-03-04 16:53:51.530',  120, 0),
(2,  'FOOD-002', N'Gạo',                1, N'Kg',    1, '2026-03-04 16:53:51.530',  500, 0),
(3,  'FOOD-003', N'Thịt hộp',           1, N'Hộp',   1, '2026-03-04 16:53:51.530',    4, 0),
(4,  'FOOD-004', N'Sữa hộp',            1, N'Hộp',   1, '2026-03-04 16:53:51.530',   80, 0),
(5,  'FOOD-005', N'Cháo ăn liền',       1, N'Thùng', 1, '2026-03-04 16:53:51.530',    3, 0),

-- Nước (category_id = 2)
(6,  'WATER-001', N'Nước khoáng chai',  2, N'Thùng', 1, '2026-03-04 16:53:51.530',  200, 0),
(7,  'WATER-002', N'Nước tinh khiết',   2, N'Thùng', 1, '2026-03-04 16:53:51.530',    5, 0),
(8,  'WATER-003', N'Viên lọc nước',     2, N'Hộp',   1, '2026-03-04 16:53:51.530',    2, 0),

-- Y tế (category_id = 3)
(9,  'MED-001', N'Băng gạc y tế',       3, N'Hộp',   1, '2026-03-04 16:53:51.530',    6, 0),
(10, 'MED-002', N'Cồn sát trùng',       3, N'Lít',   1, '2026-03-04 16:53:51.530',    1, 0),
(11, 'MED-003', N'Thuốc hạ sốt',        3, N'Hộp',   1, '2026-03-04 16:53:51.530',   45, 0),
(12, 'MED-004', N'Khẩu trang y tế',     3, N'Hộp',   1, '2026-03-04 16:53:51.530',    0, 0),
(13, 'MED-005', N'Oxy già',             3, N'Chai',  1, '2026-03-04 16:53:51.530',    3, 0),

-- Quần áo (category_id = 4)
(14, 'CLO-001', N'Áo mưa',             4, N'Cái',   1, '2026-03-04 16:53:51.530',  150, 0),
(15, 'CLO-002', N'Chăn mỏng',          4, N'Cái',   1, '2026-03-04 16:53:51.530',    6, 0),
(16, 'CLO-003', N'Áo phông',           4, N'Cái',   1, '2026-03-04 16:53:51.530',  200, 0),
(17, 'CLO-004', N'Quần đùi',           4, N'Cái',   1, '2026-03-04 16:53:51.530',    0, 0),

-- Nơi ở (category_id = 5)
(18, 'SHE-001', N'Lều cứu trợ',        5, N'Cái',   1, '2026-03-04 16:53:51.530',   12, 0),
(19, 'SHE-002', N'Bạt che mưa',        5, N'Tấm',   1, '2026-03-04 16:53:51.530',    4, 0),
(20, 'SHE-003', N'Đèn pin sạc',        5, N'Cái',   1, '2026-03-04 16:53:51.530',    2, 0);

SET IDENTITY_INSERT [dbo].[relief_items] OFF;
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRAN;

    -- 1) Kiểm tra bảng/cột tồn tại
    IF OBJECT_ID(N'dbo.users', N'U') IS NULL
        THROW 50001, N'Bảng dbo.users không tồn tại.', 1;

    IF COL_LENGTH(N'dbo.users', N'username') IS NULL
        THROW 50002, N'Cột username không tồn tại trong dbo.users.', 1;

    -- 2) Drop unique index cũ (nếu có)
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_users_username'
          AND object_id = OBJECT_ID(N'dbo.users')
    )
    BEGIN
        DROP INDEX IX_users_username ON dbo.users;
    END

    -- 3) Nếu có unique constraint trên username thì drop luôn
    DECLARE @uqName sysname;
    SELECT @uqName = kc.name
    FROM sys.key_constraints kc
    INNER JOIN sys.index_columns ic
        ON ic.object_id = kc.parent_object_id
       AND ic.index_id = kc.unique_index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id
       AND c.column_id = ic.column_id
    WHERE kc.parent_object_id = OBJECT_ID(N'dbo.users')
      AND kc.type = 'UQ'
      AND c.name = N'username';

    IF @uqName IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE dbo.users DROP CONSTRAINT [' + @uqName + N']');
    END

    -- 4) Đổi cột username thành nullable
    ALTER TABLE dbo.users
    ALTER COLUMN username NVARCHAR(50) NULL;

    -- 5) Tạo filtered unique index (cho phép nhiều NULL, nhưng username khác NULL thì phải unique)
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_users_username_not_null'
          AND object_id = OBJECT_ID(N'dbo.users')
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX IX_users_username_not_null
            ON dbo.users(username)
            WHERE username IS NOT NULL;
    END

    COMMIT TRAN;
    PRINT N'Đã cập nhật username -> NULL thành công.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    THROW;
END CATCH;
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
ALTER TABLE [dbo].[vehicles]
DROP COLUMN current_location;

ALTER TABLE [dbo].[vehicles]
ADD 
    latitude FLOAT NULL,
    longitude FLOAT NULL,
    current_location NVARCHAR(255) NULL;


