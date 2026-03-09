USE [master]
GO
/****** Object:  Database [DisasterRescueReliefDB]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE DATABASE [DisasterRescueReliefDB]
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
/****** Object:  Table [dbo].[rescue_operations]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[rescue_operation_vehicles]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  View [dbo].[v_inprogress_vehicle_assignments]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Index [UX_v_inprogress_vehicle_assignments_vehicle]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE UNIQUE CLUSTERED INDEX [UX_v_inprogress_vehicle_assignments_vehicle] ON [dbo].[v_inprogress_vehicle_assignments]
(
	[vehicle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[item_categories]    Script Date: 2/26/2026 1:30:51 PM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[notifications]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[priority_levels]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[relief_distribution_items]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[relief_distributions]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[relief_items]    Script Date: 2/26/2026 1:30:51 PM ******/
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
 CONSTRAINT [PK_relief_items] PRIMARY KEY CLUSTERED 
(
	[item_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_operation_reports]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[rescue_operation_vehicle_audit]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[rescue_request_status_history]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[rescue_requests]    Script Date: 2/26/2026 1:30:51 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[rescue_requests](
	[request_id] [int] IDENTITY(1,1) NOT NULL,
	[citizen_id] [int] NOT NULL,
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
 CONSTRAINT [PK_rescue_requests] PRIMARY KEY CLUSTERED 
(
	[request_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_team_members]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[rescue_teams]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[users]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[vehicle_types]    Script Date: 2/26/2026 1:30:51 PM ******/
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
/****** Object:  Table [dbo].[vehicles]    Script Date: 2/26/2026 1:30:51 PM ******/
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
SET IDENTITY_INSERT [dbo].[notifications] ON 

INSERT [dbo].[notifications] ([notification_id], [user_id], [title], [message], [is_read], [created_at]) VALUES (1, 16, N'Đã tiếp nhận yêu cầu cứu hộ', N'Yêu cầu #1 của bạn đã được tiếp nhận. Đội cứu hộ sẽ đến trong thời gian sớm nhất.', 1, CAST(N'2026-02-20T08:31:00.0000000' AS DateTime2))
INSERT [dbo].[notifications] ([notification_id], [user_id], [title], [message], [is_read], [created_at]) VALUES (2, 16, N'Cứu hộ hoàn tất', N'Yêu cầu #1 đã được xử lý thành công. Cảm ơn bạn đã liên hệ hệ thống cứu hộ.', 1, CAST(N'2026-02-20T14:05:00.0000000' AS DateTime2))
INSERT [dbo].[notifications] ([notification_id], [user_id], [title], [message], [is_read], [created_at]) VALUES (3, 17, N'Đội cứu hộ đang đến', N'Đội Alpha đang di chuyển đến vị trí của bạn. Vui lòng giữ bình tĩnh và ở nguyên vị trí.', 0, CAST(N'2026-02-24T07:00:00.0000000' AS DateTime2))
INSERT [dbo].[notifications] ([notification_id], [user_id], [title], [message], [is_read], [created_at]) VALUES (4, 5, N'Nhiệm vụ mới được phân công', N'Bạn được phân công cứu hộ yêu cầu #2 tại Lê Văn Lương, Q7. Có trẻ em bị sốt cao.', 0, CAST(N'2026-02-24T06:46:00.0000000' AS DateTime2))
INSERT [dbo].[notifications] ([notification_id], [user_id], [title], [message], [is_read], [created_at]) VALUES (5, 2, N'Báo cáo tổng hợp ngày 24/02', N'Hôm nay có 1 yêu cầu mới, 1 đang xử lý, 0 hoàn thành. Xem chi tiết trên dashboard.', 0, CAST(N'2026-02-24T18:00:00.0000000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[notifications] OFF
GO
SET IDENTITY_INSERT [dbo].[priority_levels] ON 

INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (1, N'CRITICAL', 1, N'Khẩn cấp - nguy hiểm tính mạng')
INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (2, N'HIGH', 2, N'Cao - cần xử lý nhanh')
INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (3, N'MEDIUM', 3, N'Trung bình - ưu tiên bình thường')
INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (4, N'LOW', 4, N'Thấp - không khẩn cấp')
INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (5, N'CRITICAL', 1, N'Nguy hiểm tính mạng, cần cứu hộ ngay lập tức')
INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (6, N'HIGH', 2, N'Tình huống khẩn cấp, cần xử lý trong vài giờ')
INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (7, N'MEDIUM', 3, N'Cần hỗ trợ nhưng chưa nguy hiểm trực tiếp')
INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (8, N'LOW', 4, N'Yêu cầu hỗ trợ thông thường, không khẩn cấp')
INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (9, N'INFO', 5, N'Báo cáo tình hình, không cần can thiệp')
SET IDENTITY_INSERT [dbo].[priority_levels] OFF
GO
SET IDENTITY_INSERT [dbo].[relief_distributions] ON 

INSERT [dbo].[relief_distributions] ([distribution_id], [request_id], [distributed_by], [distribution_date], [status], [notes]) VALUES (1, 1, 4, CAST(N'2026-02-20T15:00:00.0000000' AS DateTime2), N'Completed', N'Phân phối nhu yếu phẩm cho 5 người sau khi được cứu hộ')
INSERT [dbo].[relief_distributions] ([distribution_id], [request_id], [distributed_by], [distribution_date], [status], [notes]) VALUES (2, 3, 4, CAST(N'2026-02-21T20:00:00.0000000' AS DateTime2), N'Completed', N'Hỗ trợ 8 người từ vụ sập cầu Bình Điền')
INSERT [dbo].[relief_distributions] ([distribution_id], [request_id], [distributed_by], [distribution_date], [status], [notes]) VALUES (3, 5, 4, CAST(N'2026-02-23T17:00:00.0000000' AS DateTime2), N'Completed', N'Cung cấp đồ cứu trợ cho 32 người di dời tại trường học')
INSERT [dbo].[relief_distributions] ([distribution_id], [request_id], [distributed_by], [distribution_date], [status], [notes]) VALUES (4, 5, 4, CAST(N'2026-02-24T08:00:00.0000000' AS DateTime2), N'Completed', N'Đợt phân phối bổ sung cho các hộ dân di dời')
INSERT [dbo].[relief_distributions] ([distribution_id], [request_id], [distributed_by], [distribution_date], [status], [notes]) VALUES (5, 2, 4, CAST(N'2026-02-24T09:00:00.0000000' AS DateTime2), N'Pending', N'Chuẩn bị phân phối cho trường hợp cứu hộ đang diễn ra')
SET IDENTITY_INSERT [dbo].[relief_distributions] OFF
GO
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (3, 4, 2, CAST(N'2026-02-24T22:14:58.2510000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (3, 5, 2, CAST(N'2026-02-24T22:14:58.2510000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (4, 7, 3, CAST(N'2026-02-25T08:43:53.1350000' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[rescue_operations] ON 

INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [status], [number_of_affected_people], [estimated_time]) VALUES (2, 6, 6, 2, CAST(N'2026-02-24T22:13:36.9210000' AS DateTime2), NULL, NULL, N'Assigned', NULL, NULL)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [status], [number_of_affected_people], [estimated_time]) VALUES (3, 7, 10, 2, CAST(N'2026-02-24T22:14:58.2510000' AS DateTime2), NULL, NULL, N'Assigned', NULL, NULL)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [status], [number_of_affected_people], [estimated_time]) VALUES (4, 8, 12, 3, CAST(N'2026-02-25T08:43:53.1350000' AS DateTime2), NULL, NULL, N'Assigned', NULL, NULL)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [status], [number_of_affected_people], [estimated_time]) VALUES (5, 9, 15, 3, CAST(N'2026-02-26T06:24:31.5390000' AS DateTime2), NULL, NULL, N'Assigned', NULL, 30)
SET IDENTITY_INSERT [dbo].[rescue_operations] OFF
GO
SET IDENTITY_INSERT [dbo].[rescue_request_status_history] ON 

INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (1, 1, N'Pending', N'Yêu cầu mới được tiếp nhận từ công dân', 2, CAST(N'2026-02-20T08:30:00.0000000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2, 1, N'Verified', N'Đã xác minh qua điện thoại, tình huống nghiêm trọng', 2, CAST(N'2026-02-20T09:00:00.0000000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (3, 1, N'In Progress', N'Đội Alpha đã xuất phát, dự kiến đến trong 30 phút', 2, CAST(N'2026-02-20T09:30:00.0000000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (4, 1, N'Completed', N'Đã cứu hộ thành công 5 người, chuyển đến điểm tập kết an toàn', 2, CAST(N'2026-02-20T14:00:00.0000000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (5, 2, N'Pending', N'Yêu cầu khẩn cấp, trẻ em bị sốt cao trên mái nhà', 2, CAST(N'2026-02-24T06:15:00.0000000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (6, 6, N'Assigned', N'Phân công cho team Đội Cứu Hộ Alpha (ID=6)', 2, CAST(N'2026-02-24T22:13:36.9210000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (7, 7, N'Assigned', N'Phân công cho team Đội Cứu Hộ Charlie (ID=10)', 2, CAST(N'2026-02-24T22:14:58.2510000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (8, 8, N'Assigned', N'Phân công cho team Đội Cứu Hộ Bravo - Đường Bộ (ID=12)', 3, CAST(N'2026-02-25T08:43:53.1350000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (9, 9, N'Verified', N'Coordinator thiết lập mức độ ưu tiên 1 và xác minh yêu cầu', 3, CAST(N'2026-02-26T06:22:37.6640000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (10, 9, N'Assigned', N'Phân công cho team Đội Hậu Cần Echo (ID=15)', 3, CAST(N'2026-02-26T06:24:31.5390000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[rescue_request_status_history] OFF
GO
SET IDENTITY_INSERT [dbo].[rescue_requests] ON 

INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people]) VALUES (1, 16, N'Nước ngập 1.5m, gia đình 5 người mắc kẹt', N'0916234567', N'Tầng 1 ngập hoàn toàn, có người già bị bệnh tim cần thuốc gấp. Đã lên tầng 2 chờ cứu hộ.', CAST(10.776900 AS Decimal(9, 6)), CAST(106.700900 AS Decimal(9, 6)), N'123 Nguyễn Văn Linh, Quận 7, TP.HCM', 1, N'Completed', CAST(N'2026-02-20T08:30:00.0000000' AS DateTime2), CAST(N'2026-02-20T14:00:00.0000000' AS DateTime2), 2, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people]) VALUES (2, 17, N'Kẹt trên mái nhà do nước lũ dâng cao', N'0917234567', N'Gia đình 3 người trèo lên mái nhà, nước vẫn đang dâng. Trẻ em 5 tuổi bị sốt cao.', CAST(10.762500 AS Decimal(9, 6)), CAST(106.682100 AS Decimal(9, 6)), N'456 Lê Văn Lương, Quận 7, TP.HCM', 1, N'In Progress', CAST(N'2026-02-24T06:15:00.0000000' AS DateTime2), CAST(N'2026-02-24T07:00:00.0000000' AS DateTime2), 2, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people]) VALUES (3, 18, N'Cầu sập, 2 xe bị mắc kẹt giữa dòng nước', N'0918234567', N'Cầu Bình Điền bị sập một phần, 2 ô tô với 8 người bên trong. Nước chảy xiết.', CAST(10.823100 AS Decimal(9, 6)), CAST(106.629700 AS Decimal(9, 6)), N'Cầu Bình Điền, Bình Chánh, TP.HCM', 2, N'Completed', CAST(N'2026-02-21T10:00:00.0000000' AS DateTime2), CAST(N'2026-02-21T18:30:00.0000000' AS DateTime2), 3, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people]) VALUES (4, 19, N'Khu dân cư bị cô lập, hết lương thực 2 ngày', N'0919234567', N'Khoảng 30 hộ dân bị cô lập, không có thức ăn và nước sạch. Có phụ nữ mang thai.', CAST(10.850200 AS Decimal(9, 6)), CAST(106.752300 AS Decimal(9, 6)), N'Ấp 3, xã Vĩnh Lộc B, Bình Chánh', 2, N'Cancelled', CAST(N'2026-02-22T09:00:00.0000000' AS DateTime2), CAST(N'2026-02-22T11:00:00.0000000' AS DateTime2), 2, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people]) VALUES (5, 20, N'Sạt lở đất đe dọa 10 hộ dân ven sông', N'0920234567', N'Bờ sông sạt lở nghiêm trọng, nứt đất cách nhà dân 5m. Cần di dời khẩn cấp.', CAST(10.891500 AS Decimal(9, 6)), CAST(106.610800 AS Decimal(9, 6)), N'Ấp 2, xã Tân Kiên, Bình Chánh', 3, N'Completed', CAST(N'2026-02-23T07:45:00.0000000' AS DateTime2), CAST(N'2026-02-23T16:00:00.0000000' AS DateTime2), 3, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people]) VALUES (6, 16, N'Nước lũ tái ngập khu vực đã cứu hộ trước đó', N'0916234567', N'Khu vực Quận 7 bị ngập lại sau đợt mưa lớn đêm qua. Mực nước dâng nhanh 0.8m/giờ. 3 hộ cần sơ tán.', CAST(10.778200 AS Decimal(9, 6)), CAST(106.698500 AS Decimal(9, 6)), N'78 Huỳnh Tấn Phát, Quận 7, TP.HCM', 2, N'Assigned', CAST(N'2026-02-25T02:00:00.0000000' AS DateTime2), CAST(N'2026-02-24T22:13:36.9210000' AS DateTime2), 2, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people]) VALUES (7, 19, N'Đường bị ngập sâu, xe cứu thương không vào được', N'0919234567', N'Bệnh nhân cần cấp cứu nhưng đường ngập 1m, xe cứu thương không tiếp cận được. Cần xuồng y tế.', CAST(10.848700 AS Decimal(9, 6)), CAST(106.748900 AS Decimal(9, 6)), N'129 Quốc Lộ 1A, Bình Chánh', 1, N'Assigned', CAST(N'2026-02-25T01:30:00.0000000' AS DateTime2), CAST(N'2026-02-24T22:14:58.2510000' AS DateTime2), 2, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people]) VALUES (8, 20, N'Trường học bị ngập, 50 người dân tạm trú cần hỗ trợ', N'0920234567', N'Trường tiểu học đang làm điểm tạm trú bị nước tràn vào sân. 50 người cần di chuyển lên tầng 2.', CAST(10.893100 AS Decimal(9, 6)), CAST(106.612500 AS Decimal(9, 6)), N'Trường TH Tân Kiên, xã Tân Kiên, Bình Chánh', 2, N'Assigned', CAST(N'2026-02-25T03:00:00.0000000' AS DateTime2), CAST(N'2026-02-25T08:43:53.1350000' AS DateTime2), 3, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people]) VALUES (9, 18, N'test', N'0328485142', N'test1', CAST(0.000000 AS Decimal(9, 6)), CAST(0.000000 AS Decimal(9, 6)), N'test', 1, N'Assigned', CAST(N'2026-02-24T21:29:25.5890000' AS DateTime2), CAST(N'2026-02-26T06:24:31.5390000' AS DateTime2), 3, NULL)
SET IDENTITY_INSERT [dbo].[rescue_requests] OFF
GO
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (6, 5, N'Leader', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (6, 8, N'Member', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (6, 11, N'Member', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (6, 14, N'Member', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (9, 6, N'Leader', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (9, 9, N'Member', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (9, 12, N'Member', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (9, 15, N'Member', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (10, 7, N'Leader', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (10, 10, N'Member', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (10, 13, N'Member', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
GO
SET IDENTITY_INSERT [dbo].[rescue_teams] ON 

INSERT [dbo].[rescue_teams] ([team_id], [team_name], [status], [created_at]) VALUES (6, N'Đội Cứu Hộ Alpha', N'BUSY', CAST(N'2026-01-19T13:15:07.7030000' AS DateTime2))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [status], [created_at]) VALUES (9, N'Đội Cứu Hộ Beta', N'ON_MISSION', CAST(N'2026-01-19T13:15:07.7030000' AS DateTime2))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [status], [created_at]) VALUES (10, N'Đội Cứu Hộ Charlie', N'BUSY', CAST(N'2026-01-19T13:15:07.7030000' AS DateTime2))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [status], [created_at]) VALUES (11, N'Đội Cứu Hộ Alpha - Đường Thủy', N'ON_MISSION', CAST(N'2026-01-15T08:00:00.0000000' AS DateTime2))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [status], [created_at]) VALUES (12, N'Đội Cứu Hộ Bravo - Đường Bộ', N'BUSY', CAST(N'2026-01-15T08:00:00.0000000' AS DateTime2))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [status], [created_at]) VALUES (13, N'Đội Cứu Hộ Charlie - Tổng Hợp', N'AVAILABLE', CAST(N'2026-01-20T10:00:00.0000000' AS DateTime2))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [status], [created_at]) VALUES (14, N'Đội Y Tế Cơ Động Delta', N'RESTING', CAST(N'2026-02-01T09:00:00.0000000' AS DateTime2))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [status], [created_at]) VALUES (15, N'Đội Hậu Cần Echo', N'BUSY', CAST(N'2026-02-10T08:00:00.0000000' AS DateTime2))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [status], [created_at]) VALUES (16, N'Đội Cứu Hộ Alpha', N'Available', CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [status], [created_at]) VALUES (17, N'Đội Cứu Hộ Beta', N'Available', CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [status], [created_at]) VALUES (18, N'Đội Cứu Hộ Gamma', N'Available', CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[rescue_teams] OFF
GO
SET IDENTITY_INSERT [dbo].[users] ON 

INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (1, N'admin', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Nguyễn Văn An', N'0901234567', N'admin@rescue.vn', N'ADMIN', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (2, N'coordinator1', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Trần Thị Bình', N'0902234567', N'coordinator1@rescue.vn', N'COORDINATOR', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (3, N'coordinator2', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Lê Văn Cường', N'0903234567', N'coordinator2@rescue.vn', N'COORDINATOR', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (4, N'manager1', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Phạm Thị Dung', N'0904234567', N'manager1@rescue.vn', N'MANAGER', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (5, N'team_leader1', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Hoàng Văn Em', N'0905234567', N'leader1@rescue.vn', N'RESCUE_TEAM', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (6, N'team_leader2', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Vũ Thị Phương', N'0906234567', N'leader2@rescue.vn', N'RESCUE_TEAM', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (7, N'team_leader3', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Đặng Văn Giang', N'0907234567', N'leader3@rescue.vn', N'RESCUE_TEAM', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (8, N'member1', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Nguyễn Văn Hùng', N'0908234567', N'member1@rescue.vn', N'RESCUE_TEAM', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (9, N'member2', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Trần Thị Lan', N'0909234567', N'member2@rescue.vn', N'RESCUE_TEAM', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (10, N'member3', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Lê Văn Minh', N'0910234567', N'member3@rescue.vn', N'RESCUE_TEAM', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (11, N'member4', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Phạm Thị Nga', N'0911234567', N'member4@rescue.vn', N'RESCUE_TEAM', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (12, N'member5', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Hoàng Văn Oanh', N'0912234567', N'member5@rescue.vn', N'RESCUE_TEAM', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (13, N'member6', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Vũ Thị Phúc', N'0913234567', N'member6@rescue.vn', N'RESCUE_TEAM', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (14, N'member7', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Đặng Văn Quân', N'0914234567', N'member7@rescue.vn', N'RESCUE_TEAM', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (15, N'member8', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Nguyễn Thị Rạng', N'0915234567', N'member8@rescue.vn', N'RESCUE_TEAM', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (16, N'citizen1', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Trần Văn Sơn', N'0916234567', N'citizen1@gmail.com', N'CITIZEN', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (17, N'citizen2', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Lê Thị Tâm', N'0917234567', N'citizen2@gmail.com', N'CITIZEN', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (18, N'citizen3', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Phạm Văn Út', N'0918234567', N'citizen3@gmail.com', N'CITIZEN', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (19, N'citizen4', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Hoàng Thị Vân', N'0919234567', N'citizen4@gmail.com', N'CITIZEN', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (20, N'citizen5', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Vũ Văn Xuân', N'0920234567', N'citizen5@gmail.com', N'CITIZEN', 1, CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (21, N'tandat', N'$2a$11$Z4MAuoB8F26Ydr4rxXGiMOhesMKy5a.aTKUa.B3XOF3EGsRoqcSD2', N'dddddddddd', N'0328485142', N'usesr@example.com', N'CITIZEN', 1, CAST(N'2026-02-26T05:42:47.2780000' AS DateTime2), NULL)
SET IDENTITY_INSERT [dbo].[users] OFF
GO
SET IDENTITY_INSERT [dbo].[vehicle_types] ON 

INSERT [dbo].[vehicle_types] ([vehicle_type_id], [type_code], [type_name], [description]) VALUES (2, N'BOAT', N'Boat', N'Thuyền, xuồng cứu hộ cho vùng ngập')
INSERT [dbo].[vehicle_types] ([vehicle_type_id], [type_code], [type_name], [description]) VALUES (3, N'TRUCK', N'Truck', N'Xe tải, xe bán tải cứu hộ đường bộ')
INSERT [dbo].[vehicle_types] ([vehicle_type_id], [type_code], [type_name], [description]) VALUES (4, N'HELICOPTER', N'Helicopter', N'Trực thăng cứu hộ đường không')
INSERT [dbo].[vehicle_types] ([vehicle_type_id], [type_code], [type_name], [description]) VALUES (5, N'AMPHIBIOUS', N'Amphibious', N'Phương tiện lưỡng cư, hoạt động cả trên cạn và dưới nước')
INSERT [dbo].[vehicle_types] ([vehicle_type_id], [type_code], [type_name], [description]) VALUES (6, N'DRONE', N'Drone', N'Drone trinh sát, khảo sát vùng thiên tai')
SET IDENTITY_INSERT [dbo].[vehicle_types] OFF
GO
SET IDENTITY_INSERT [dbo].[vehicles] ON 

INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [current_location], [last_maintenance], [updated_at]) VALUES (3, N'BOAT-001', N'Xuồng Cao Tốc SR-01', 2, N'51S-0001', 12, N'InUse', N'Khu vực Quận 7, TP.HCM', CAST(N'2026-02-01' AS Date), NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [current_location], [last_maintenance], [updated_at]) VALUES (4, N'TRUCK-001', N'Xe Cứu Hộ Ford Ranger 4x4', 3, N'51A-1234', 6, N'InUse', N'Trụ sở Cứu Hộ Quận 1', CAST(N'2026-01-20' AS Date), CAST(N'2026-02-24T22:14:58.2510000' AS DateTime2))
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [current_location], [last_maintenance], [updated_at]) VALUES (5, N'HELI-001', N'Trực Thăng EC-135', 4, N'VN-8888', 4, N'InUse', N'Sân bay Tân Sơn Nhất', CAST(N'2026-02-10' AS Date), CAST(N'2026-02-24T22:14:58.2510000' AS DateTime2))
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [current_location], [last_maintenance], [updated_at]) VALUES (6, N'AMPH-001', N'Xe Lưỡng Cư Sherp N1200', 5, N'51C-5678', 8, N'Maintenance', N'Xưởng bảo dưỡng Củ Chi', CAST(N'2025-12-15' AS Date), NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [current_location], [last_maintenance], [updated_at]) VALUES (7, N'DRONE-001', N'DJI Matrice 350 RTK', 6, N'51D-0099', 0, N'InUse', N'Kho thiết bị Thủ Đức', CAST(N'2026-02-18' AS Date), CAST(N'2026-02-25T08:43:53.1350000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[vehicles] OFF
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_item_categories_category_code]    Script Date: 2/26/2026 1:30:51 PM ******/
ALTER TABLE [dbo].[item_categories] ADD  CONSTRAINT [UQ_item_categories_category_code] UNIQUE NONCLUSTERED 
(
	[category_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_relief_items_item_code]    Script Date: 2/26/2026 1:30:51 PM ******/
ALTER TABLE [dbo].[relief_items] ADD  CONSTRAINT [UQ_relief_items_item_code] UNIQUE NONCLUSTERED 
(
	[item_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_audit_operation_time]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_rov_audit_operation_time] ON [dbo].[rescue_operation_vehicle_audit]
(
	[operation_id] ASC,
	[action_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_audit_vehicle_time]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_rov_audit_vehicle_time] ON [dbo].[rescue_operation_vehicle_audit]
(
	[vehicle_id] ASC,
	[action_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_operation]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_rov_operation] ON [dbo].[rescue_operation_vehicles]
(
	[operation_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_vehicle]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_rov_vehicle] ON [dbo].[rescue_operation_vehicles]
(
	[vehicle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_rescue_operations_request_id]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rescue_operations_request_id] ON [dbo].[rescue_operations]
(
	[request_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rrsh_request_updatedat]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_rrsh_request_updatedat] ON [dbo].[rescue_request_status_history]
(
	[request_id] ASC,
	[updated_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_rrsh_request_status]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rrsh_request_status] ON [dbo].[rescue_request_status_history]
(
	[request_id] ASC,
	[status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_rescue_requests_status_createdat_desc]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_rescue_requests_status_createdat_desc] ON [dbo].[rescue_requests]
(
	[status] ASC,
	[created_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_rescue_requests_one_open_per_citizen]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rescue_requests_one_open_per_citizen] ON [dbo].[rescue_requests]
(
	[citizen_id] ASC
)
WHERE ([status]<>'Completed' AND [status]<>'Cancelled' AND [status]<>'Duplicate')
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_vehicles_status]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE NONCLUSTERED INDEX [IX_vehicles_status] ON [dbo].[vehicles]
(
	[status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_vehicles_license_plate]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UQ_vehicles_license_plate] ON [dbo].[vehicles]
(
	[license_plate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_vehicles_vehicle_code]    Script Date: 2/26/2026 1:30:51 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UQ_vehicles_vehicle_code] ON [dbo].[vehicles]
(
	[vehicle_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[item_categories] ADD  CONSTRAINT [DF_item_categories_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[item_categories] ADD  CONSTRAINT [DF_item_categories_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [dbo].[notifications] ADD  CONSTRAINT [DF_notifications_is_read]  DEFAULT ((0)) FOR [is_read]
GO
ALTER TABLE [dbo].[notifications] ADD  CONSTRAINT [DF_notifications_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [dbo].[relief_items] ADD  CONSTRAINT [DF_relief_items_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[relief_items] ADD  CONSTRAINT [DF_relief_items_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
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
ALTER TABLE [dbo].[users] ADD  CONSTRAINT [DF_users_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[users] ADD  CONSTRAINT [DF_users_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [dbo].[notifications]  WITH CHECK ADD  CONSTRAINT [FK_notifications_user] FOREIGN KEY([user_id])
REFERENCES [dbo].[users] ([user_id])
GO
ALTER TABLE [dbo].[notifications] CHECK CONSTRAINT [FK_notifications_user]
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
ALTER TABLE [dbo].[rescue_operations]  WITH CHECK ADD  CONSTRAINT [CK_rescue_operations_status_allowed] CHECK  (([status]='Cancelled' OR [status]='Completed' OR [status]='In Progress' OR [status]='Assigned'))
GO
ALTER TABLE [dbo].[rescue_operations] CHECK CONSTRAINT [CK_rescue_operations_status_allowed]
GO
ALTER TABLE [dbo].[rescue_requests]  WITH CHECK ADD  CONSTRAINT [CK_rescue_requests_status_allowed] CHECK  (([status]='Duplicate' OR [status]='Cancelled' OR [status]='Completed' OR [status]='In Progress' OR [status]='Assigned' OR [status]='Verified' OR [status]='Pending'))
GO
ALTER TABLE [dbo].[rescue_requests] CHECK CONSTRAINT [CK_rescue_requests_status_allowed]
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



