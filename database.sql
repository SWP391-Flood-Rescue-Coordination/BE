USE [master]
GO
/****** Object:  Database [DisasterRescueReliefDB]    Script Date: 4/1/2026 1:05:32 AM ******/
CREATE DATABASE [DisasterRescueReliefDB]

USE [DisasterRescueReliefDB]
GO
/****** Object:  User [thuan]    Script Date: 4/1/2026 1:05:32 AM ******/
CREATE USER [thuan] FOR LOGIN [thuan] WITH DEFAULT_SCHEMA=[dbo]
GO
/****** Object:  Table [dbo].[rescue_operations]    Script Date: 4/1/2026 1:05:32 AM ******/
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
	[number_of_affected_people] [int] NULL,
	[estimated_time] [int] NULL,
 CONSTRAINT [PK_rescue_operations] PRIMARY KEY CLUSTERED 
(
	[operation_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_operation_vehicles]    Script Date: 4/1/2026 1:05:32 AM ******/
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
/****** Object:  View [dbo].[v_inprogress_vehicle_assignments]    Script Date: 4/1/2026 1:05:32 AM ******/
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
    JOIN dbo.rescue_requests AS rr
        ON rr.request_id = ro.request_id
    WHERE rr.status = 'Assigned';
    
GO
SET ARITHABORT ON
SET CONCAT_NULL_YIELDS_NULL ON
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
SET NUMERIC_ROUNDABORT OFF
GO
/****** Object:  Index [UX_v_inprogress_vehicle_assignments_vehicle]    Script Date: 4/1/2026 1:05:32 AM ******/
CREATE UNIQUE CLUSTERED INDEX [UX_v_inprogress_vehicle_assignments_vehicle] ON [dbo].[v_inprogress_vehicle_assignments]
(
	[vehicle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[blacklisted_tokens]    Script Date: 4/1/2026 1:05:32 AM ******/
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
/****** Object:  Table [dbo].[item_categories]    Script Date: 4/1/2026 1:05:32 AM ******/
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
/****** Object:  Table [dbo].[priority_levels]    Script Date: 4/1/2026 1:05:32 AM ******/
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
/****** Object:  Table [dbo].[refresh_tokens]    Script Date: 4/1/2026 1:05:32 AM ******/
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
/****** Object:  Table [dbo].[relief_items]    Script Date: 4/1/2026 1:05:32 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[relief_items](
	[item_id] [int] IDENTITY(1,1) NOT NULL,
	[item_code] [varchar](50) NOT NULL,
	[item_name] [nvarchar](200) NOT NULL,
	[category_id] [int] NOT NULL,
	[unit] [nvarchar](50) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[quantity] [int] NOT NULL,
	[min_quantity] [int] NOT NULL,
 CONSTRAINT [PK_relief_items] PRIMARY KEY CLUSTERED 
(
	[item_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_request_status_history]    Script Date: 4/1/2026 1:05:32 AM ******/
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
/****** Object:  Table [dbo].[rescue_requests]    Script Date: 4/1/2026 1:05:32 AM ******/
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
	[team_id] [int] NULL,
	[status] [varchar](20) NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NULL,
	[updated_by] [int] NULL,
	[number_of_affected_people] [int] NULL,
	[contact_name] [nvarchar](100) NULL,
	[contact_phone] [varchar](20) NULL,
	[adult_count] [int] NULL,
	[elderly_count] [int] NULL,
	[children_count] [int] NULL,
 CONSTRAINT [PK_rescue_requests] PRIMARY KEY CLUSTERED 
(
	[request_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[rescue_team_members]    Script Date: 4/1/2026 1:05:32 AM ******/
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
/****** Object:  Table [dbo].[rescue_teams]    Script Date: 4/1/2026 1:05:32 AM ******/
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
 CONSTRAINT [PK_rescue_teams] PRIMARY KEY CLUSTERED 
(
	[team_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[stock_history]    Script Date: 4/1/2026 1:05:32 AM ******/
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
/****** Object:  Table [dbo].[stock_units]    Script Date: 4/1/2026 1:05:33 AM ******/
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[users]    Script Date: 4/1/2026 1:05:33 AM ******/
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
/****** Object:  Table [dbo].[vehicle_types]    Script Date: 4/1/2026 1:05:33 AM ******/
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
/****** Object:  Table [dbo].[vehicles]    Script Date: 4/1/2026 1:05:33 AM ******/
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
	[last_maintenance] [date] NULL,
	[updated_at] [datetime2](3) NULL,
	[latitude] [float] NULL,
	[longitude] [float] NULL,
	[current_location] [nvarchar](255) NULL,
 CONSTRAINT [PK_vehicles] PRIMARY KEY CLUSTERED 
(
	[vehicle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[users] ON 

INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (1, N'admin', N'AQAAAAEAACcQAAAAEN3f...', N'Hệ Thống Quản Trị', N'0123456789', N'admin@rescue.com', N'Admin', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'Trụ Sở Chính')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (2, N'manager1', N'AQAAAAEAACcQAAAAEN3f...', N'Nguyễn Quản Lý', N'0901234567', N'manager1@rescue.com', N'Manager', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'VP Điều Hành')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (3, N'coord1', N'AQAAAAEAACcQAAAAEN3f...', N'Trần Điều Phối', N'0902345678', N'coord1@rescue.com', N'Coordinator', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'Phòng Điều Phối')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (5, N'leader1', N'AQAAAAEAACcQAAAAEN3f...', N'Lê Đội Trưởng', N'0903456789', N'leader1@rescue.com', N'RescueTeam', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'Trạm Cứu Hộ A')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (8, N'member1', N'AQAAAAEAACcQAAAAEN3f...', N'Phạm Thành Viên', N'0904567890', N'member1@rescue.com', N'RescueTeam', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'Trạm Cứu Hộ A')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (11, N'member2', N'AQAAAAEAACcQAAAAEN3f...', N'Hoàng Thành Viên', N'0905678901', N'member2@rescue.com', N'RescueTeam', 1, CAST(N'2026-02-24T20:24:32.0000000' AS DateTime2), N'Trạm Cứu Hộ A')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (16, N'citizen1', N'AQAAAAEAACcQAAAAEN3f...', N'Lê Văn Citizens', N'0916234567', N'citizen1@gmail.com', N'Citizen', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'Quận 7')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (17, N'citizen2', N'AQAAAAEAACcQAAAAEN3f...', N'Nguyễn Thị Dân', N'0917234567', N'citizen2@gmail.com', N'Citizen', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'Quận 7')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (18, N'citizen3', N'AQAAAAEAACcQAAAAEN3f...', N'Trần Văn Tèo', N'0918234567', N'citizen3@gmail.com', N'Citizen', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'Bình Chánh')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (22, N'citizen4', N'AQAAAAEAACcQAAAAEN3f...', N'Phạm Văn Citizen', N'0922123456', N'citizen4@gmail.com', N'Citizen', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'Thủ Đức')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (23, N'leader2', N'AQAAAAEAACcQAAAAEN3f...', N'Đặng Đội Trưởng', N'0923123456', N'leader2@rescue.com', N'RescueTeam', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'Trạm Cứu Hộ B')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (24, N'leader3', N'AQAAAAEAACcQAAAAEN3f...', N'Vũ Đội Trưởng', N'0924123456', N'leader3@rescue.com', N'RescueTeam', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'Trạm Cứu Hộ C')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (29, N'coordinator2', N'AQAAAAEAACcQAAAAEN3f...', N'Lý Điều Phối', N'0929123456', N'coord2@rescue.com', N'Coordinator', 1, CAST(N'2026-02-20T00:00:00.0000000' AS DateTime2), N'Phòng Điều Phối')
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (1030, N'external1', N'AQAAAAEAACcQAAAAEN3f...', N'Khách Vãng Lai', NULL, NULL, N'Citizen', 1, CAST(N'2026-03-25T00:00:00.0000000' AS DateTime2), NULL)

SET IDENTITY_INSERT [dbo].[users] OFF
GO

SET IDENTITY_INSERT [dbo].[vehicle_types] ON 

INSERT [dbo].[vehicle_types] ([vehicle_type_id], [type_code], [type_name], [description]) VALUES (1, N'CANOE', N'Cano cứu hộ', N'Cano cao tốc dùng cho vùng ngập sâu')
INSERT [dbo].[vehicle_types] ([vehicle_type_id], [type_code], [type_name], [description]) VALUES (2, N'TRUCK', N'Xe tải lội nước', N'Xe tải chuyên dụng có khả năng lội nước')
INSERT [dbo].[vehicle_types] ([vehicle_type_id], [type_code], [type_name], [description]) VALUES (3, N'AMBULANCE', N'Xe cứu thương', N'Xe cấp cứu y tế')
INSERT [dbo].[vehicle_types] ([vehicle_type_id], [type_code], [type_name], [description]) VALUES (4, N'BOAT', N'Xuồng máy', N'Xuồng máy nhỏ linh hoạt')

SET IDENTITY_INSERT [dbo].[vehicle_types] OFF
GO

SET IDENTITY_INSERT [dbo].[vehicles] ON 

INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (4, N'VH-004', N'Cano Alpha 1', 1, N'CN-1234', 6, N'Available', NULL, NULL, 10.776900, 106.700900, N'Bến Bạch Đằng')
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (5, N'VH-005', N'Xe tải lội nước 1', 2, N'51C-123.45', 20, N'Available', NULL, NULL, 10.762500, 106.682100, N'Kho Quận 7')
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (7, N'VH-007', N'Xe cứu thương 1', 3, N'51B-999.99', 4, N'InUse', NULL, NULL, 10.848700, 106.748900, N'BV Thủ Đức')
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (8, N'VH-008', N'Xuồng máy 1', 4, N'XM-001', 4, N'Available', NULL, NULL, 10.755951, 106.698460, N'Kênh Tẻ')
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (11, N'VH-011', N'Cano Beta 1', 1, N'CN-5678', 6, N'Available', NULL, NULL, 10.781922, 106.704208, N'Cầu Sài Gòn')
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (12, N'VH-012', N'Xe tải lội nước 2', 2, N'51C-678.90', 25, N'Maintenance', NULL, NULL, 10.893100, 106.612500, N'Xưởng Bình Chánh')

SET IDENTITY_INSERT [dbo].[vehicles] OFF
GO

SET IDENTITY_INSERT [dbo].[item_categories] ON 

INSERT [dbo].[item_categories] ([category_id], [category_code], [category_name], [description], [is_active], [created_at]) VALUES (1, N'FOOD', N'Thực phẩm', NULL, 1, CAST(N'2026-03-04T16:53:51.5130000' AS DateTime2))
INSERT [dbo].[item_categories] ([category_id], [category_code], [category_name], [description], [is_active], [created_at]) VALUES (2, N'WATER', N'Nước uống', NULL, 1, CAST(N'2026-03-04T16:53:51.5130000' AS DateTime2))
INSERT [dbo].[item_categories] ([category_id], [category_code], [category_name], [description], [is_active], [created_at]) VALUES (3, N'MEDICINE', N'Y tế', NULL, 1, CAST(N'2026-03-04T16:53:51.5130000' AS DateTime2))
INSERT [dbo].[item_categories] ([category_id], [category_code], [category_name], [description], [is_active], [created_at]) VALUES (4, N'CLOTHING', N'Quần áo', NULL, 1, CAST(N'2026-03-04T16:53:51.5130000' AS DateTime2))
INSERT [dbo].[item_categories] ([category_id], [category_code], [category_name], [description], [is_active], [created_at]) VALUES (5, N'SHELTER', N'Trang thiết bị trú ẩn', NULL, 1, CAST(N'2026-03-04T16:53:51.5130000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[item_categories] OFF
GO
SET IDENTITY_INSERT [dbo].[priority_levels] ON 

INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (1, N'CRITICAL', 1, N'Khẩn cấp - nguy hiểm tính mạng')
INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (2, N'HIGH', 2, N'Cao - cần xử lý nhanh')
INSERT [dbo].[priority_levels] ([priority_id], [level_name], [priority_order], [description]) VALUES (3, N'MEDIUM', 3, N'Trung bình - ưu tiên bình thường')
SET IDENTITY_INSERT [dbo].[priority_levels] OFF
GO
SET IDENTITY_INSERT [dbo].[refresh_tokens] ON 

INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1, 16, N'pQyNGLOmR1iDC4hd5HqDJcSQFMD8Lw2UL8kZH1PqSgw=', CAST(N'2026-03-11T16:03:58.6419888' AS DateTime2), CAST(N'2026-03-04T16:03:58.6420806' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (2, 18, N'ZqjIbDjG252vvehhO9Cd8q/4g6qwnlwQ9egffx7x4I4=', CAST(N'2026-03-11T16:11:53.0731700' AS DateTime2), CAST(N'2026-03-04T16:11:53.0731704' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3, 18, N'cF3FrtXym2DMmBRbqVPlb+wZCWvrj/Qw00NjZtWjLws=', CAST(N'2026-03-11T16:36:47.5367972' AS DateTime2), CAST(N'2026-03-04T16:36:47.5368285' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4, 4, N'M1dE4LvbgclnaZ93IabtfYS0ubnesZxcXkmdv2H2eEY=', CAST(N'2026-03-11T16:54:45.9839865' AS DateTime2), CAST(N'2026-03-04T16:54:45.9840195' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (5, 17, N'T/Io8RTWe4QZc2WKqLcCnDFwDWBJ1Iou4e/mIcc1d/Y=', CAST(N'2026-03-11T17:42:52.5416039' AS DateTime2), CAST(N'2026-03-04T17:42:52.5416356' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1002, 1, N'+it7wvkgScnJv2C1yky52jSkJEvi0b5oifRIEW5NJZ8=', CAST(N'2026-03-17T15:23:59.7840184' AS DateTime2), CAST(N'2026-03-10T15:23:59.7840487' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1003, 1, N'KjjAoLHv/AwbpgD9R5pKYq+hyuu3cEnHwZiXirxQc/M=', CAST(N'2026-03-17T15:40:02.6928780' AS DateTime2), CAST(N'2026-03-10T15:40:02.6929107' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1004, 2, N'zpyqOOZF7eLEGhtRlDumXPvUc3YZVFILcEHdsznqR34=', CAST(N'2026-03-19T05:59:19.9837073' AS DateTime2), CAST(N'2026-03-12T05:59:19.9837387' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1005, 22, N'ljgTxJI9aBMyRxZNOh+LI31XJtq355iToL+IwPVP5pQ=', CAST(N'2026-03-24T15:27:06.2264120' AS DateTime2), CAST(N'2026-03-17T15:27:06.2264443' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1006, 22, N'orHd8Zrk54QPFqxZTAP/20QiKssixTNhMM7Q3LZoVb0=', CAST(N'2026-03-24T15:27:33.2050846' AS DateTime2), CAST(N'2026-03-17T15:27:33.2050851' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1007, 22, N'4dI0uv97MvPsS5v52Pi6PBMoXjeUCKv+HpuMy1k19Xw=', CAST(N'2026-03-24T15:35:58.1762399' AS DateTime2), CAST(N'2026-03-17T15:35:58.1762715' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1008, 22, N'U6eqfCi+BQiLIEPxaia0Z+WuhKqG1jSaAeoPD9IN32o=', CAST(N'2026-03-24T15:38:46.8886178' AS DateTime2), CAST(N'2026-03-17T15:38:46.8886496' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1009, 22, N'fSKwC9MEXBnDfm7LNuTL+iL0ja2bPElc9HFa1PGmXk8=', CAST(N'2026-03-24T15:43:48.9173648' AS DateTime2), CAST(N'2026-03-17T15:43:48.9173976' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1010, 2, N'zgYVpDo7fLc1TATduY5ovo9/ie+isPRr/qK73w2bKEo=', CAST(N'2026-03-24T15:55:37.3686638' AS DateTime2), CAST(N'2026-03-17T15:55:37.3686968' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1011, 4, N'0G1XrBnZ9rg7m/MM59oypmmqb34tAAGommdN9k0UNlU=', CAST(N'2026-03-24T17:05:47.3915508' AS DateTime2), CAST(N'2026-03-17T17:05:47.3916095' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1012, 1, N'ops5Ineh40WWx1pjlx8RFI/GuF3zRl1R0B9X1JBpp9k=', CAST(N'2026-03-24T17:36:10.3277142' AS DateTime2), CAST(N'2026-03-17T17:36:10.3277147' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1013, 4, N'YHJJSLgLOuG8kUjBAup03Li+NjHdzIa0MjsR8GGP2OI=', CAST(N'2026-03-25T07:39:38.9054483' AS DateTime2), CAST(N'2026-03-18T07:39:38.9055850' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1014, 4, N'YEpIPOpOOBgc/pxN0Hd3K66OHw2IIvkZs1K9j6WCSnU=', CAST(N'2026-03-25T08:53:12.9284265' AS DateTime2), CAST(N'2026-03-18T08:53:12.9284271' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1015, 3, N'9sSn4qFW2OmRDLvJcUn6aRFUpPpzz5eB57sUhqjjx7Q=', CAST(N'2026-03-26T06:10:42.2616709' AS DateTime2), CAST(N'2026-03-19T06:10:42.2616961' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1016, 4, N'knKpIphq/UITMUJuBUGPs+uPwQHw12nIdSofWlOurz0=', CAST(N'2026-03-26T06:30:30.1678152' AS DateTime2), CAST(N'2026-03-19T06:30:30.1678169' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1017, 3, N'jLjMQgYYsK0dLdSICfKvXxfhn+uKykn5XJJvsalUaYs=', CAST(N'2026-03-26T06:43:01.1000809' AS DateTime2), CAST(N'2026-03-19T06:43:01.1000815' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1018, 4, N'nCmFHZyUkq7KAZSBsVXDj3QwJEdWo3jCLESj5UhVLIw=', CAST(N'2026-03-26T06:53:43.3964204' AS DateTime2), CAST(N'2026-03-19T06:53:43.3964210' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1019, 24, N'XGefrQAD207AUgvTkYW2ZRIOaNXnp8tctCteWFwUlwU=', CAST(N'2026-03-26T07:23:08.6819939' AS DateTime2), CAST(N'2026-03-19T07:23:08.6819945' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1020, 3, N'mcdceB/LRYPCJ6ETsM6w1xdE24qUJlXPAp6RYU9fxGU=', CAST(N'2026-03-26T09:09:16.5816111' AS DateTime2), CAST(N'2026-03-19T09:09:16.5816348' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1021, 4, N'qWi1XwdQE+U8qA0lfF0KSITtSXTbu9LeSLzy+XR5oso=', CAST(N'2026-03-26T09:09:28.2593783' AS DateTime2), CAST(N'2026-03-19T09:09:28.2593789' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1022, 5, N'bIqPfvWGOkPa2dKHJbh3JiFyTjOOvG1J7pq8PGZLq6Q=', CAST(N'2026-03-26T09:09:59.2191877' AS DateTime2), CAST(N'2026-03-19T09:09:59.2191880' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1023, 6, N'ZQ2oKMFWjBwtrui1/4Q3pORkZJI7I11cXlQkJZlBclg=', CAST(N'2026-03-26T09:12:47.5797434' AS DateTime2), CAST(N'2026-03-19T09:12:47.5797436' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1024, 7, N'2FWaDMqEgTGmAQNEx3lK22vW+h1i7dqbum2DtSw16mk=', CAST(N'2026-03-26T09:12:59.8382012' AS DateTime2), CAST(N'2026-03-19T09:12:59.8382014' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1025, 5, N'gXCA1RSc4+h5oWB8Cu7ouT2hT1fk6/VKbG7zv/O3gu4=', CAST(N'2026-03-26T14:21:11.1177901' AS DateTime2), CAST(N'2026-03-19T14:21:11.1178308' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1026, 3, N'SQgoKPzdKHEjOUCDvulLI1C5vwZ6BbNlOpfHAMJpKXo=', CAST(N'2026-03-26T14:22:57.7340489' AS DateTime2), CAST(N'2026-03-19T14:22:57.7340493' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1027, 3, N'laovZxo8dTVymHie045BEONWXq7a2UbYMgTCGUVmrMU=', CAST(N'2026-03-26T14:24:20.6090201' AS DateTime2), CAST(N'2026-03-19T14:24:20.6090205' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1028, 23, N'tOKEL42v5gey3rB4maY12PJWITCnfCrbNHpcH6PrggA=', CAST(N'2026-03-26T14:25:34.3910486' AS DateTime2), CAST(N'2026-03-19T14:25:34.3910489' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1029, 3, N'f/LRtLlophmsSbsO3TlB8Iu+4CXg0dFDOCFAEKx70Zs=', CAST(N'2026-03-26T14:34:26.9417716' AS DateTime2), CAST(N'2026-03-19T14:34:26.9417719' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1030, 23, N'Tvf8e1kESYClXvwQqsKEFJY+8vOQst1Ua8esmmMycZ4=', CAST(N'2026-03-26T14:43:42.2574087' AS DateTime2), CAST(N'2026-03-19T14:43:42.2574091' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1031, 3, N'ujWiRzMMwdBqooYxAIQT8kTIsBQYFy7xqEcYv1VoI/8=', CAST(N'2026-03-26T14:56:34.5520924' AS DateTime2), CAST(N'2026-03-19T14:56:34.5520928' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (1032, 23, N'N2//srC0udK6BjzldpHGrp/fVmHmUsBQCswC0xR6bw0=', CAST(N'2026-03-26T14:57:04.0430216' AS DateTime2), CAST(N'2026-03-19T14:57:04.0430221' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (2020, 30, N'BiW5BF98MXhJCD2nudvO/35gHJ54+syFkrm872laCI8=', CAST(N'2026-03-30T01:29:22.6087325' AS DateTime2), CAST(N'2026-03-23T01:29:22.6087689' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (2021, 30, N'GMi2bqJ7UE8mXFui5QUxhTh0EDFapuATy+Z8fo3DPag=', CAST(N'2026-03-30T01:32:30.0126051' AS DateTime2), CAST(N'2026-03-23T01:32:30.0126060' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3020, 3, N'HS7xane25KCLu+HxjB6XWGFs7PFWCBI9D/S07HTEiJg=', CAST(N'2026-03-31T04:19:53.2599306' AS DateTime2), CAST(N'2026-03-24T04:19:53.2599572' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3021, 5, N'MwSOtNF+PqZfzGZwVFZtENBvyVArwaE4HaTwTSDcKOk=', CAST(N'2026-03-31T04:21:53.5409837' AS DateTime2), CAST(N'2026-03-24T04:21:53.5409841' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3022, 7, N'cbO9nMZ7oanqLggBEKSb0CsaOYjHqWVlOpLtd5BbJU8=', CAST(N'2026-03-31T04:22:06.4838582' AS DateTime2), CAST(N'2026-03-24T04:22:06.4838584' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3023, 14, N'uiSCzRUuQE6CQ1pUjBOm8Gy3jyOs+Q5734sJ9+CGbxE=', CAST(N'2026-03-31T04:22:16.8383037' AS DateTime2), CAST(N'2026-03-24T04:22:16.8383039' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3024, 15, N'R4q6uIKaMr2wxpByX68zhMflbRh9xyv3vce7CGLV9go=', CAST(N'2026-03-31T04:22:25.5672138' AS DateTime2), CAST(N'2026-03-24T04:22:25.5672141' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3025, 24, N'/6fpkpbf5Gdt3ezPMeASfxgvDRUnw/9ZxS8nY2SsiwA=', CAST(N'2026-03-31T04:22:48.1738127' AS DateTime2), CAST(N'2026-03-24T04:22:48.1738130' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3026, 4, N'4mpKCxKgBpXjND+sT1rvXtVF73zYEt2Cx8fFwY98Gds=', CAST(N'2026-03-31T04:23:10.5030000' AS DateTime2), CAST(N'2026-03-24T04:23:10.5030004' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3027, 4, N'kFhn5e1/QXc8zWu+bol/A6WaEpbBv5mmwvGwPKzAqWI=', CAST(N'2026-03-31T04:31:18.1158617' AS DateTime2), CAST(N'2026-03-24T04:31:18.1158623' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3028, 3, N'4Bc1JGR1ifF5jqR0/SzWg91xCOLanYY73aBGMrQjQE8=', CAST(N'2026-03-31T06:41:58.5225894' AS DateTime2), CAST(N'2026-03-24T06:41:58.5226333' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3029, 3, N'Mzd+1YpMe8j+e+ZM2DnfelKvjA4hftUcK5ibeTDXwd8=', CAST(N'2026-03-31T06:46:56.6957117' AS DateTime2), CAST(N'2026-03-24T06:46:56.6957123' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3030, 26, N'Mn7xxUljfPdq33E93YvITcs2KYA15/CfKg/v6q3EPvQ=', CAST(N'2026-03-31T06:55:16.9796554' AS DateTime2), CAST(N'2026-03-24T06:55:16.9796562' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3031, 3, N'CNWeYLkNnwNE52YdJPxQZbFAkfk/DXrnRKQUIAl1dNM=', CAST(N'2026-03-31T07:07:18.4022704' AS DateTime2), CAST(N'2026-03-24T07:07:18.4022717' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3032, 3, N'DlUb815dyIy0AYzCCShqln3FvHZbuxRBeTz2M2hGEYA=', CAST(N'2026-03-31T07:24:58.0016379' AS DateTime2), CAST(N'2026-03-24T07:24:58.0016388' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3033, 3, N'q57LkKhoe7GK3TApB7ZQckUtLyigU93lnynoz9O9BDs=', CAST(N'2026-03-31T07:30:00.9060981' AS DateTime2), CAST(N'2026-03-24T07:30:00.9060992' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3034, 26, N'8v3ozyJKiyE1UJtY95meEa5jD6QuTMXHFm5jL2WAt8M=', CAST(N'2026-03-31T07:43:22.5355935' AS DateTime2), CAST(N'2026-03-24T07:43:22.5355952' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3035, 28, N'C4nIcBh1U4Uvx9ggFHCDp8kIkgoWokV4pCSlIX50GPs=', CAST(N'2026-03-31T08:01:41.8136638' AS DateTime2), CAST(N'2026-03-24T08:01:41.8136645' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3036, 3, N'gHbLJ/UOlNgXU3H57mky7S5KGiPvci0ufUZTqs4UHzc=', CAST(N'2026-03-31T10:01:55.0257198' AS DateTime2), CAST(N'2026-03-24T10:01:55.0257571' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3037, 3, N'3D2xEPtobg6vbfWTGfw0j5Via0z0G70r2++oRIFxkLI=', CAST(N'2026-03-31T10:59:56.9032008' AS DateTime2), CAST(N'2026-03-24T10:59:56.9032013' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3038, 3, N'tIfLXOs4oR88SHpahkr5yyT8Pv/A4cYO5bWxXHNp87o=', CAST(N'2026-03-31T11:35:12.1719085' AS DateTime2), CAST(N'2026-03-24T11:35:12.1719091' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3039, 5, N'F0K0+/f7EEkgdY/HCspyHf733EBQLm9HFArd/fFcdS0=', CAST(N'2026-03-31T11:36:31.2531136' AS DateTime2), CAST(N'2026-03-24T11:36:31.2531139' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3040, 4, N'qyD/hmcT0N2JDnDItFlCArFDQsBfsZc4kvlqHfY4zmw=', CAST(N'2026-03-31T11:37:00.3654235' AS DateTime2), CAST(N'2026-03-24T11:37:00.3654239' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3041, 5, N'0rnemou+lX+AeMOLnGN/4ltQ5adKuD4OPamDvdFE6Ts=', CAST(N'2026-03-31T11:37:57.5270433' AS DateTime2), CAST(N'2026-03-24T11:37:57.5270436' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3042, 4, N'wmLA9nPSyjOBqyXUTh47Oji/zISZFYp6eQNTchMhmGY=', CAST(N'2026-03-31T11:41:50.7017225' AS DateTime2), CAST(N'2026-03-24T11:41:50.7017232' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3043, 1, N'xsSnaDLXUCZKMawHaunRUmD7Ss93myZgtEgnPG6fXho=', CAST(N'2026-03-31T12:13:07.8143607' AS DateTime2), CAST(N'2026-03-24T12:13:07.8143613' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3044, 1, N'0jdT0A/skrNAEfUSEupYzbbavF5REVToQVWeJy1MgpE=', CAST(N'2026-03-31T12:18:40.5416300' AS DateTime2), CAST(N'2026-03-24T12:18:40.5416303' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (3045, 30, N'LLtg1iCssEEBPthoK6RxQZGnEzxclPQuW6s0zF+O1DA=', CAST(N'2026-03-31T12:42:15.7279835' AS DateTime2), CAST(N'2026-03-24T12:42:15.7279843' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4020, 3, N'CD0ALhqjUaxQKc/HmvKSUz+DNMi+DcT8SudgBuvjMfM=', CAST(N'2026-03-31T17:17:10.4145914' AS DateTime2), CAST(N'2026-03-24T17:17:10.4146265' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4021, 3, N'gUc8ReR+AHHTjPJd49+g/8kGQvmNz0lC/W4FPm9BUwM=', CAST(N'2026-03-31T17:19:16.2284112' AS DateTime2), CAST(N'2026-03-24T17:19:16.2284120' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4022, 3, N'cH6C5cvRp0LpGscQReShyjA0O4gihUD+49FwYlAF280=', CAST(N'2026-03-31T17:20:36.0892434' AS DateTime2), CAST(N'2026-03-24T17:20:36.0892442' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4023, 28, N'VZa+ASffbG+bl4ZDMmd+24E0Mmi1TKWRIvh1pnzzykE=', CAST(N'2026-03-31T17:29:33.4426952' AS DateTime2), CAST(N'2026-03-24T17:29:33.4426955' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4024, 3, N'ZqiboTRU5Sm1NrXUhi01KZCnrTq55VNew/U5w08U3Qg=', CAST(N'2026-04-01T07:45:37.0466029' AS DateTime2), CAST(N'2026-03-25T07:45:37.0466507' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4025, 28, N'V5c68xzHyW3oRosAv8CYMcmbdyH7rfQQEepl+uJyOR0=', CAST(N'2026-04-01T07:47:39.2564077' AS DateTime2), CAST(N'2026-03-25T07:47:39.2564081' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4026, 28, N'wLcWSnA+KRG088PYtExHpAmimMLoE8p4EPsxTYIvBp4=', CAST(N'2026-04-01T08:09:41.4036623' AS DateTime2), CAST(N'2026-03-25T08:09:41.4036630' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4027, 1, N'OBJsgtAjhrVdzZl4+HwEv8GedKLCnQlTVofgV9dYKbw=', CAST(N'2026-04-01T08:47:16.7819387' AS DateTime2), CAST(N'2026-03-25T08:47:16.7819623' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4028, 3, N'vYC5ntH40EVvMS18HvAVx9HS5dMzfx5a316+/dktZc8=', CAST(N'2026-04-01T08:48:45.0117363' AS DateTime2), CAST(N'2026-03-25T08:48:45.0117366' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4029, 3, N'qbU9Q1RMrzPNxcT9LSZ+niC0cl4IBwSwspccX27IjXs=', CAST(N'2026-04-01T09:03:30.3455137' AS DateTime2), CAST(N'2026-03-25T09:03:30.3455139' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4030, 1030, N'cYyW59XgeNaKX1Orzk2Q18nuIi3RpZyXOvpA9AMuPSI=', CAST(N'2026-04-01T09:15:12.3067596' AS DateTime2), CAST(N'2026-03-25T09:15:12.3067599' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4031, 1030, N'X9GUcMhpORMhe9U+T0CVa8WuNTWskV5aj0I2N3VK5dE=', CAST(N'2026-04-01T09:15:24.8734436' AS DateTime2), CAST(N'2026-03-25T09:15:24.8734441' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4032, 1030, N'DLf8NlnfF6odOgAVreOY2nHdSwbhxrj+G1695Kp8V0g=', CAST(N'2026-04-01T09:18:48.5243384' AS DateTime2), CAST(N'2026-03-25T09:18:48.5243386' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4033, 3, N'bOlq2dl85JnoXMBEk+q5DiySD15j0IeKUgGsd+BfzSk=', CAST(N'2026-04-01T09:19:58.4722078' AS DateTime2), CAST(N'2026-03-25T09:19:58.4722081' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4034, 28, N'GAniL8ieeoplupW3LSHfqsHjVqyp/3G5BEchNmzOI84=', CAST(N'2026-04-01T09:21:04.1099706' AS DateTime2), CAST(N'2026-03-25T09:21:04.1099709' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4035, 1, N'/AuqKTKKcINDxTc2wKi4w+RVKsDW8yo9LLWcRi2e6r8=', CAST(N'2026-04-01T09:32:01.9846948' AS DateTime2), CAST(N'2026-03-25T09:32:01.9846950' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4036, 1, N'ASiaOMvnnS0w2AIu5Au04sdvtENGl77MLIWhYPQg6LU=', CAST(N'2026-04-01T09:33:50.5531024' AS DateTime2), CAST(N'2026-03-25T09:33:50.5531026' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4037, 4, N'LjPwxnw0gnvvzT2HG9qhlZbrPu24rg6lERVM3s1+HfI=', CAST(N'2026-04-01T09:43:49.3262791' AS DateTime2), CAST(N'2026-03-25T09:43:49.3262793' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4038, 3, N'Yy83GWoaL0ne7Y2Ty2y3yrWuLI/KrI+LGSSF2TU02Dc=', CAST(N'2026-04-01T09:53:08.3642142' AS DateTime2), CAST(N'2026-03-25T09:53:08.3642144' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4039, 1, N'/uGg1PcTIQgP2BQLwQmfyItBOacU9HbZ6ulSc8YzImM=', CAST(N'2026-04-01T09:53:46.3997663' AS DateTime2), CAST(N'2026-03-25T09:53:46.3997666' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4040, 3, N'WU+6bhmx2P/P2Op7hIj3RHK3cn0zr0Vbr7gmko2Lm9U=', CAST(N'2026-04-01T09:54:55.8260626' AS DateTime2), CAST(N'2026-03-25T09:54:55.8260628' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4041, 1, N'i62SGsYNne2G6IlChb3lMRw4tP3+dhX0GrDmY666oMI=', CAST(N'2026-04-01T09:55:21.4259022' AS DateTime2), CAST(N'2026-03-25T09:55:21.4259024' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4042, 3, N'QRyJL72oDEoEQ2DdS7VgBaidDKKhoD4w+waDTilNxk8=', CAST(N'2026-04-01T10:43:58.5208852' AS DateTime2), CAST(N'2026-03-25T10:43:58.5208857' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4043, 1, N'dBw5jzW8zcyj2n9qLok02kiqYsk2mcY1wjVXYZLknwQ=', CAST(N'2026-04-01T10:44:39.3155166' AS DateTime2), CAST(N'2026-03-25T10:44:39.3155168' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4044, 3, N'nYgCB2DBIJpsbKCunwqJijo/tJ7mGKEkK6hVqX1/tl8=', CAST(N'2026-04-01T10:51:01.6403866' AS DateTime2), CAST(N'2026-03-25T10:51:01.6403868' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4045, 3, N'CQvb+R3omCfJH4BnTGFoKglWhURKsrOF2HL1OdvjrmE=', CAST(N'2026-04-01T10:53:18.4399086' AS DateTime2), CAST(N'2026-03-25T10:53:18.4399088' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4046, 3, N'J66jHhv6Nu9tfliILnBncLgmTx6AmnTzkzUYAqSE7ic=', CAST(N'2026-04-01T10:57:50.1987893' AS DateTime2), CAST(N'2026-03-25T10:57:50.1987895' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4047, 1, N'5XAheOGHqgMDxY5snENMFZ3LHTA5PxHkK9vXoE74L0g=', CAST(N'2026-04-01T11:00:42.8993905' AS DateTime2), CAST(N'2026-03-25T11:00:42.8993908' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4048, 1, N'ULjieC6Brr0fNkS+cRfQXbXCnVapRNKM3a4z8vkqh9Y=', CAST(N'2026-04-01T11:06:20.9722961' AS DateTime2), CAST(N'2026-03-25T11:06:20.9723279' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4049, 1, N'pXOrJEr9YVkjbtuwW45WCGshDpAQ4qr4NEpUC/HHsXE=', CAST(N'2026-04-01T11:29:16.4241150' AS DateTime2), CAST(N'2026-03-25T11:29:16.4241154' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4050, 1, N'hPOM4F9GbQzOB1BKnv+AFa61mo/txwBj1piLO0BF3b0=', CAST(N'2026-04-01T11:41:17.2519783' AS DateTime2), CAST(N'2026-03-25T11:41:17.2520053' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4051, 1, N'VUiKhPI9w42GSbpEnh0qT7lKWOmptdSyOXoAuJ4kjLI=', CAST(N'2026-04-01T11:54:23.9870014' AS DateTime2), CAST(N'2026-03-25T11:54:23.9870026' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4052, 1, N'cHCrG/8lg94mrNo6kM5q+56xc+HfhM77J1/KZ/UnwDU=', CAST(N'2026-04-01T12:00:16.5694670' AS DateTime2), CAST(N'2026-03-25T12:00:16.5694675' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4053, 3, N'ekjRaXuiwqVCoOKU4EpBGPly6RMWvGtYuulLFgCJRM8=', CAST(N'2026-04-01T17:14:04.6013920' AS DateTime2), CAST(N'2026-03-25T17:14:04.6014263' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4054, 29, N'J0XYsI+6iw5EvFbgvIXwmyA+cjrGflBegir0YZ3VZvE=', CAST(N'2026-04-01T17:34:52.8576879' AS DateTime2), CAST(N'2026-03-25T17:34:52.8576884' AS DateTime2), NULL)
GO
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4055, 3, N'4pjeCkVU1UOI7kzoRDvZbgKHkfNA4IofmrSuu3rMywE=', CAST(N'2026-04-01T17:41:20.1775586' AS DateTime2), CAST(N'2026-03-25T17:41:20.1775592' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4056, 3, N'd1Mg5ji8bSIl2/eiuyVdUN8WJ/s9F+ofNOhc/u5EuoE=', CAST(N'2026-04-01T17:45:37.1811416' AS DateTime2), CAST(N'2026-03-25T17:45:37.1811423' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4057, 29, N'9/hwLzi5nhVsxQaDmeC0d2LIZ+TpyxCRSzJAiCPJXj4=', CAST(N'2026-04-01T17:47:01.7241470' AS DateTime2), CAST(N'2026-03-25T17:47:01.7241473' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4058, 5, N'KQIFC5l6IiSqleiXdDaWgSuX89hRMHZPK8xRYgTp+gU=', CAST(N'2026-04-01T18:13:08.6194046' AS DateTime2), CAST(N'2026-03-25T18:13:08.6194224' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4059, 3, N'Wl54OhHEwujPq9KfQqqAX4QO5QmLd/1fxITxoiME5kg=', CAST(N'2026-04-01T18:16:34.3826888' AS DateTime2), CAST(N'2026-03-25T18:16:34.3826911' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4060, 5, N'+dP8qWwpwB5LK5ST8Q2QN4hat7J4Di7LU2qkpXpTzj8=', CAST(N'2026-04-01T18:22:55.7297129' AS DateTime2), CAST(N'2026-03-25T18:22:55.7297135' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4061, 4, N'N5M1beeU7MRf/HNmATXxsyoGkJy2XrByw654z0b3w1Y=', CAST(N'2026-04-01T18:23:11.5369617' AS DateTime2), CAST(N'2026-03-25T18:23:11.5369621' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4062, 1, N'+hltCqcoDbR/HzGARzeVZRSMkdOFmk7CDhTMagHf/do=', CAST(N'2026-04-07T11:43:27.6520252' AS DateTime2), CAST(N'2026-03-31T11:43:27.6520629' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4063, 3, N'riXh5rIQfuqJxGAppNBab2GcsnJglm/F/KriQMtk9S8=', CAST(N'2026-04-07T11:45:33.9166183' AS DateTime2), CAST(N'2026-03-31T11:45:33.9166187' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4064, 4, N'wAAJme7bQmk1Pavk7f/0dqiPgsP1hToAKXp4tyO4Qw0=', CAST(N'2026-04-07T11:48:00.7890221' AS DateTime2), CAST(N'2026-03-31T11:48:00.7890224' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4065, 4, N'Wr3XzyfcsCQguRbr/8sLDnzpC7ZmTQqld8WNtjD/toE=', CAST(N'2026-04-07T16:01:47.0657017' AS DateTime2), CAST(N'2026-03-31T16:01:47.0657724' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4066, 23, N'WbcwdmSNnNeSiy5OaiXT/IL0A4Ykg/xaPkfUiGDy/nc=', CAST(N'2026-04-07T17:24:38.6308298' AS DateTime2), CAST(N'2026-03-31T17:24:38.6308715' AS DateTime2), NULL)
INSERT [dbo].[refresh_tokens] ([id], [user_id], [token], [expires_at], [created_at], [revoked_at]) VALUES (4067, 20, N'UmT8G4D8z0jvFoa+YK7W+Yn/7cbJT8FEbvGIZsNxSns=', CAST(N'2026-04-07T17:38:12.9837527' AS DateTime2), CAST(N'2026-03-31T17:38:12.9838264' AS DateTime2), NULL)
SET IDENTITY_INSERT [dbo].[refresh_tokens] OFF
GO
SET IDENTITY_INSERT [dbo].[relief_items] ON 

INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (1, N'FOOD-001', N'Mì gói', 1, N'Thùng', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 120, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (2, N'FOOD-002', N'Gạo', 1, N'Kg', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 500, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (3, N'FOOD-003', N'Thịt hộp', 1, N'Hộp', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 4, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (4, N'FOOD-004', N'Sữa hộp', 1, N'Hộp', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 80, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (5, N'FOOD-005', N'Cháo ăn liền', 1, N'Thùng', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 3, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (6, N'WATER-001', N'Nước khoáng chai', 2, N'Thùng', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 200, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (7, N'WATER-002', N'Nước tinh khiết', 2, N'Thùng', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 5, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (8, N'WATER-003', N'Viên lọc nước', 2, N'Hộp', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 38, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (9, N'MED-001', N'Băng gạc y tế', 3, N'Hộp', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 6, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (10, N'MED-002', N'Cồn sát trùng', 3, N'Lít', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 70, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (11, N'MED-003', N'Thuốc hạ sốt', 3, N'Hộp', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 45, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (12, N'MED-004', N'Khẩu trang y tế', 3, N'Hộp', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 36, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (13, N'MED-005', N'Oxy già', 3, N'Chai', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 3, 69)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (14, N'CLO-001', N'Áo mưa', 4, N'Cái', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 81, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (15, N'CLO-002', N'Chăn mỏng', 4, N'Cái', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 6, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (16, N'CLO-003', N'Áo phông', 4, N'Cái', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 200, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (17, N'CLO-004', N'Quần đùi', 4, N'Cái', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 999, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (18, N'SHE-001', N'Lều cứu trợ', 5, N'Cái', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 12, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (19, N'SHE-002', N'Bạt che mưa', 5, N'Tấm', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 4, 0)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (20, N'SHE-003', N'Đèn pin sạc', 5, N'Cái', 1, CAST(N'2026-03-04T16:53:51.5300000' AS DateTime2), 100, 10)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (21, N'MED-006', N'Nước rửa tay diệt khuẩn', 3, N'Chai', 1, CAST(N'2026-04-05T14:51:00.0000000' AS DateTime2), 250, 20)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (22, N'FOOD-006', N'Bánh chưng hút chân không', 1, N'Cái', 1, CAST(N'2026-04-05T14:51:00.0000000' AS DateTime2), 300, 50)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (23, N'WATER-004', N'Thùng nước khoáng 5L', 2, N'Thùng', 1, CAST(N'2026-04-05T14:51:00.0000000' AS DateTime2), 150, 30)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (24, N'CLO-005', N'Bộ đồ bảo hộ chống nước', 4, N'Bộ', 1, CAST(N'2026-04-05T14:51:00.0000000' AS DateTime2), 80, 10)
INSERT [dbo].[relief_items] ([item_id], [item_code], [item_name], [category_id], [unit], [is_active], [created_at], [quantity], [min_quantity]) VALUES (25, N'SHE-004', N'Áo phao cứu hộ', 5, N'Cái', 1, CAST(N'2026-04-05T14:51:00.0000000' AS DateTime2), 120, 25)
SET IDENTITY_INSERT [dbo].[relief_items] OFF
GO
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (3, 4, 2, CAST(N'2026-02-24T22:14:58.2510000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (3, 5, 2, CAST(N'2026-02-24T22:14:58.2510000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (4, 7, 3, CAST(N'2026-02-25T08:43:53.1350000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (6, 11, 3, CAST(N'2026-03-19T06:19:59.0840000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (7, 8, 3, CAST(N'2026-03-19T07:21:19.7370000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (7, 12, 3, CAST(N'2026-03-19T07:21:19.7370000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (8, 8, 3, CAST(N'2026-03-19T14:24:46.5890000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (9, 8, 3, CAST(N'2026-03-19T14:34:43.2100000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (10, 8, 3, CAST(N'2026-03-19T14:56:51.2560000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (1008, 12, 3, CAST(N'2026-03-24T06:54:45.0210000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (1009, 5, 3, CAST(N'2026-03-24T08:01:16.1100000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (1010, 5, 3, CAST(N'2026-03-24T17:21:38.4770000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (1011, 5, 3, CAST(N'2026-03-25T07:47:05.8210000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (1012, 5, 3, CAST(N'2026-03-25T09:20:22.4100000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (1013, 5, 3, CAST(N'2026-03-25T17:33:16.9500000' AS DateTime2))
INSERT [dbo].[rescue_operation_vehicles] ([operation_id], [vehicle_id], [assigned_by], [assigned_at]) VALUES (1014, 5, 3, CAST(N'2026-03-25T17:46:27.1570000' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[rescue_teams] ON 

INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (6, N'Đội Cứu Hộ Alpha', CAST(N'2026-02-24T20:24:32.0000000' AS DateTime2), CAST(10.776900 AS Decimal(9, 6)), CAST(106.700900 AS Decimal(9, 6)))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (10, N'Đội Cứu Hộ Charlie', CAST(N'2026-02-24T20:24:32.0000000' AS DateTime2), CAST(10.762500 AS Decimal(9, 6)), CAST(106.682100 AS Decimal(9, 6)))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (12, N'Đội Cứu Hộ Bravo - Đường Bộ', CAST(N'2026-02-24T20:24:32.0000000' AS DateTime2), CAST(10.893100 AS Decimal(9, 6)), CAST(106.612500 AS Decimal(9, 6)))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (15, N'Đội Hậu Cần Echo', CAST(N'2026-02-24T20:24:32.0000000' AS DateTime2), CAST(10.848700 AS Decimal(9, 6)), CAST(106.748900 AS Decimal(9, 6)))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (17, N'Đội Cứu Hộ Beta', CAST(N'2026-02-24T20:24:32.0000000' AS DateTime2), CAST(10.781922 AS Decimal(9, 6)), CAST(106.704208 AS Decimal(9, 6)))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (19, N'Đội Cứu Hộ Delta', CAST(N'2026-02-24T20:24:32.0000000' AS DateTime2), CAST(10.755951 AS Decimal(9, 6)), CAST(106.698460 AS Decimal(9, 6)))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (20, N'Đội Cứu Hộ Echo', CAST(N'2026-02-24T20:24:32.0000000' AS DateTime2), CAST(10.784757 AS Decimal(9, 6)), CAST(106.728334 AS Decimal(9, 6)))
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (21, N'Đội Cứu Hộ Foxtrot', CAST(N'2026-02-24T20:24:32.0000000' AS DateTime2), CAST(10.814322 AS Decimal(9, 6)), CAST(106.799218 AS Decimal(9, 6)))

SET IDENTITY_INSERT [dbo].[rescue_teams] OFF
GO
SET IDENTITY_INSERT [dbo].[rescue_operations] ON 

INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (2, 6, 6, 2, CAST(N'2026-02-24T22:13:36.9210000' AS DateTime2), CAST(N'2026-03-19T09:11:55.5990000' AS DateTime2), CAST(N'2026-03-19T09:11:55.5990000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (3, 7, 10, 2, CAST(N'2026-02-24T22:14:58.2510000' AS DateTime2), CAST(N'2026-03-19T09:28:55.3090000' AS DateTime2), CAST(N'2026-03-19T09:28:55.3090000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (4, 8, 12, 3, CAST(N'2026-02-25T08:43:53.1350000' AS DateTime2), NULL, NULL, NULL, NULL)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (5, 9, 15, 3, CAST(N'2026-02-26T06:24:31.5390000' AS DateTime2), NULL, CAST(N'2026-03-04T16:37:33.7280000' AS DateTime2), NULL, 30)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (6, 12, 17, 3, CAST(N'2026-03-19T06:19:59.0840000' AS DateTime2), NULL, NULL, NULL, 90)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (7, 13, 19, 3, CAST(N'2026-03-19T07:21:19.7370000' AS DateTime2), CAST(N'2026-03-19T07:36:16.0680000' AS DateTime2), CAST(N'2026-03-19T07:36:16.0680000' AS DateTime2), NULL, 90)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (8, 14, 19, 3, CAST(N'2026-03-19T14:24:46.5890000' AS DateTime2), CAST(N'2026-03-19T14:31:19.2420000' AS DateTime2), CAST(N'2026-03-19T14:31:19.2420000' AS DateTime2), NULL, 90)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (9, 15, 19, 3, CAST(N'2026-03-19T14:34:43.2100000' AS DateTime2), CAST(N'2026-03-19T14:43:48.7210000' AS DateTime2), CAST(N'2026-03-19T14:43:48.7210000' AS DateTime2), NULL, 90)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (10, 16, 19, 3, CAST(N'2026-03-19T14:56:51.2560000' AS DateTime2), CAST(N'2026-03-31T17:30:27.9680000' AS DateTime2), CAST(N'2026-03-31T17:30:27.9680000' AS DateTime2), NULL, 90)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (1008, 23, 20, 3, CAST(N'2026-03-24T06:54:45.0210000' AS DateTime2), NULL, NULL, 22, 90)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (1009, 24, 21, 3, CAST(N'2026-03-24T08:01:16.1100000' AS DateTime2), CAST(N'2026-03-24T08:16:41.8080000' AS DateTime2), CAST(N'2026-03-24T08:16:41.8080000' AS DateTime2), 22, 90)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (1010, 28, 21, 3, CAST(N'2026-03-24T17:21:38.4770000' AS DateTime2), CAST(N'2026-03-24T17:29:43.0790000' AS DateTime2), CAST(N'2026-03-24T17:29:43.0790000' AS DateTime2), NULL, 90)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (1011, 29, 21, 3, CAST(N'2026-03-25T07:47:05.8210000' AS DateTime2), CAST(N'2026-03-25T08:16:27.9420000' AS DateTime2), CAST(N'2026-03-25T08:16:27.9420000' AS DateTime2), NULL, 67)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (1012, 34, 21, 3, CAST(N'2026-03-25T09:20:22.4100000' AS DateTime2), CAST(N'2026-03-25T09:21:54.7130000' AS DateTime2), CAST(N'2026-03-25T09:21:54.7130000' AS DateTime2), NULL, 90)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (1013, 45, 21, 3, CAST(N'2026-03-25T17:33:16.9500000' AS DateTime2), CAST(N'2026-03-25T17:35:04.3950000' AS DateTime2), CAST(N'2026-03-25T17:35:04.3950000' AS DateTime2), NULL, 90)
INSERT [dbo].[rescue_operations] ([operation_id], [request_id], [team_id], [assigned_by], [assigned_at], [started_at], [completed_at], [number_of_affected_people], [estimated_time]) VALUES (1014, 46, 21, 3, CAST(N'2026-03-25T17:46:27.1570000' AS DateTime2), NULL, NULL, 2, 90)
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
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (11, 9, N'Completed', N'Công dân xác nhận đã được cứu hộ thành công.', 18, CAST(N'2026-03-04T16:37:33.7280000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (12, 11, N'Cancelled', N'Trạng thái cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-17T17:36:31.4880000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (13, 12, N'Verified', N'Coordinator thiết lập mức độ ưu tiên 3 và xác minh yêu cầu', 3, CAST(N'2026-03-19T06:11:11.6330000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (14, 12, N'Assigned', N'Phân công cho team Đội Cứu Hộ Beta (ID=17)', 3, CAST(N'2026-03-19T06:19:59.0840000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (15, 13, N'Verified', N'Coordinator thiết lập mức độ ưu tiên 2 và xác minh yêu cầu', 3, CAST(N'2026-03-19T06:44:48.7990000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (16, 13, N'Assigned', N'Phân công cho team Đội Cứu Hộ Delta (ID=19)', 3, CAST(N'2026-03-19T07:21:19.7370000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (19, 13, N'Completed', N'Đội cứu hộ hoàn tất nhiệm vụ, yêu cầu chuyển trực tiếp sang Completed.', 24, CAST(N'2026-03-19T07:36:16.0680000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (20, 6, N'Completed', N'Đội cứu hộ hoàn tất nhiệm vụ, yêu cầu chuyển trực tiếp sang Completed.', 5, CAST(N'2026-03-19T09:11:55.5990000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (23, 7, N'Cancelled', N'Đội cứu hộ hủy nhiệm vụ, yêu cầu chuyển trực tiếp sang Cancelled.', 7, CAST(N'2026-03-19T09:28:55.3090000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (24, 14, N'Verified', N'Coordinator thiết lập mức độ ưu tiên 2 và xác minh yêu cầu', 3, CAST(N'2026-03-19T14:24:31.0790000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (25, 14, N'Assigned', N'Phân công cho team Đội Cứu Hộ Delta (ID=19)', 3, CAST(N'2026-03-19T14:24:46.5890000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (26, 14, N'Cancelled', N'Đội cứu hộ hủy nhiệm vụ, yêu cầu chuyển trực tiếp sang Cancelled.', 23, CAST(N'2026-03-19T14:31:19.2420000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (27, 15, N'Verified', N'Coordinator thiết lập mức độ ưu tiên 3 và xác minh yêu cầu', 3, CAST(N'2026-03-19T14:34:35.6760000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (28, 15, N'Assigned', N'Phân công cho team Đội Cứu Hộ Delta (ID=19)', 3, CAST(N'2026-03-19T14:34:43.2100000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (29, 15, N'Completed', N'Đội cứu hộ hoàn tất nhiệm vụ, yêu cầu chuyển trực tiếp sang Completed.', 23, CAST(N'2026-03-19T14:43:48.7210000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (30, 16, N'Verified', N'Coordinator thiết lập mức độ ưu tiên 3 và xác minh yêu cầu', 3, CAST(N'2026-03-19T14:56:43.1140000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (31, 16, N'Assigned', N'Phân công cho team Đội Cứu Hộ Delta (ID=19)', 3, CAST(N'2026-03-19T14:56:51.2560000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (1020, 22, N'Verified', N'Coordinator xac minh yeu cau (uu tien hien tai: 1)', 3, CAST(N'2026-03-24T06:42:25.8480000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (1021, 18, N'Verified', N'Coordinator xac minh yeu cau (uu tien hien tai: )', 3, CAST(N'2026-03-24T06:42:39.2220000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (1022, 23, N'Verified', N'Coordinator xac minh yeu cau (uu tien hien tai: 1)', 3, CAST(N'2026-03-24T06:47:06.9810000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (1023, 23, N'Assigned', N'Phân công cho team Đội Cứu Hộ Echo (ID=20)', 3, CAST(N'2026-03-24T06:54:45.0210000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (1027, 24, N'Verified', N'Coordinator xac minh yeu cau (uu tien hien tai: 1)', 3, CAST(N'2026-03-24T08:00:17.2120000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (1028, 24, N'Assigned', N'Phân công cho team Đội Cứu Hộ Foxtrot (ID=21)', 3, CAST(N'2026-03-24T08:01:16.1100000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2020, 17, N'Duplicate', N'Trang thai cap nhat boi he thong quan ly', 3, CAST(N'2026-03-24T17:19:31.9540000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2021, 28, N'Verified', N'Coordinator xac minh yeu cau (uu tien hien tai: 1)', 3, CAST(N'2026-03-24T17:20:51.1670000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2022, 28, N'Assigned', N'Phân công cho team Đội Cứu Hộ Foxtrot (ID=21)', 3, CAST(N'2026-03-24T17:21:38.4770000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2023, 28, N'Completed', N'Khach vang lai da bao an toan sau khi doi cuu ho xac nhan hoan tat.', -1, CAST(N'2026-03-24T17:30:16.9520000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2024, 29, N'Verified', N'Điều phối viên xác minh yêu cầu (ưu tiên hiện tại: 1)', 3, CAST(N'2026-03-25T07:46:48.4090000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2025, 29, N'Assigned', N'Phân công cho team Đội Cứu Hộ Foxtrot (ID=21)', 3, CAST(N'2026-03-25T07:47:05.8210000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2026, 29, N'Completed', N'Khách vãng lai đã báo an toàn sau khi đội cứu hộ xác nhận hoàn tất.', -1, CAST(N'2026-03-25T08:32:28.9980000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2027, 22, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T08:47:45.4370000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2028, 32, N'Verified', N'Điều phối viên xác minh yêu cầu (ưu tiên hiện tại: 2)', 3, CAST(N'2026-03-25T09:04:11.4010000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2029, 34, N'Verified', N'Điều phối viên xác minh yêu cầu (ưu tiên hiện tại: 1)', 3, CAST(N'2026-03-25T09:20:06.7130000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2030, 34, N'Assigned', N'Phân công cho team Đội Cứu Hộ Foxtrot (ID=21)', 3, CAST(N'2026-03-25T09:20:22.4100000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2031, 26, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T09:32:13.0900000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2032, 25, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T09:32:28.8990000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2033, 30, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T09:32:53.8480000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2034, 31, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T09:34:01.4400000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2035, 34, N'Completed', N'Khách vãng lai đã báo an toàn sau khi đội cứu hộ xác nhận hoàn tất.', -1, CAST(N'2026-03-25T09:49:54.6780000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2036, 37, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T09:54:14.7490000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2037, 21, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T10:44:46.4210000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2038, 19, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T10:44:51.1100000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2039, 18, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T10:44:55.5340000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2040, 20, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T10:45:08.0460000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2041, 40, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T10:45:09.6130000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2042, 38, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T10:45:11.1890000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2043, 27, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T10:45:12.7520000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2044, 32, N'Cancelled', N'Trạng thái được cập nhật bởi hệ thống quản lý', 1, CAST(N'2026-03-25T10:45:15.2240000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2045, 42, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-25T11:09:44.5770000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2046, 41, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-25T11:09:46.9430000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2047, 45, N'Verified', N'Coordinator xac minh yeu cau (uu tien hien tai: 2)', 3, CAST(N'2026-03-25T17:33:03.6250000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2048, 45, N'Assigned', N'Phân công cho team Đội Cứu Hộ Foxtrot (ID=21)', 3, CAST(N'2026-03-25T17:33:16.9500000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2049, 45, N'Completed', N'Khach vang lai da bao an toan sau khi doi cuu ho xac nhan hoan tat.', -1, CAST(N'2026-03-25T17:35:31.6530000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2050, 46, N'Verified', N'Coordinator xac minh yeu cau (uu tien hien tai: 2)', 3, CAST(N'2026-03-25T17:46:14.4230000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2051, 46, N'Assigned', N'Phân công cho team Đội Cứu Hộ Foxtrot (ID=21)', 3, CAST(N'2026-03-25T17:46:27.1570000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2052, 46, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 29, CAST(N'2026-03-25T17:48:57.0570000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2053, 47, N'Verified', N'Coordinator xac minh yeu cau (uu tien hien tai: 1)', 3, CAST(N'2026-03-25T18:17:32.1900000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2054, 47, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:43:43.8700000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2055, 45, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:43:45.9290000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2056, 43, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:43:48.2980000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2057, 44, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:43:50.2160000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2058, 39, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:43:52.2840000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2059, 36, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:43:55.3200000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2060, 35, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:01.0960000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2061, 34, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:01.1140000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2062, 33, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:04.5420000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2063, 29, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:07.6840000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2064, 28, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:10.5610000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2065, 23, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:13.0070000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2066, 24, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:15.8780000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2067, 17, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:18.4900000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2068, 16, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:21.7360000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2069, 15, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:24.2260000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2070, 12, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:27.0940000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2071, 13, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:29.9430000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2072, 6, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:33.0630000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2073, 8, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:36.1350000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2074, 9, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:39.5270000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2075, 2, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:42.5690000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2076, 5, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:45.5620000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2077, 3, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:44:48.4460000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2078, 1, N'Cancelled', N'Trang thai cap nhat boi he thong quan ly', 1, CAST(N'2026-03-31T11:45:00.8010000' AS DateTime2))
INSERT [dbo].[rescue_request_status_history] ([status_id], [request_id], [status], [notes], [updated_by], [updated_at]) VALUES (2079, 48, N'Verified', N'Coordinator xac minh yeu cau (uu tien hien tai: 2)', 3, CAST(N'2026-03-31T11:47:10.5300000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[rescue_request_status_history] OFF
GO
SET IDENTITY_INSERT [dbo].[rescue_requests] ON 

INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (1, 16, N'Nước ngập 1.5m, gia đình 5 người mắc kẹt', N'0916234567', N'Tầng 1 ngập hoàn toàn, có người già bị bệnh tim cần thuốc gấp. Đã lên tầng 2 chờ cứu hộ.', CAST(10.776900 AS Decimal(9, 6)), CAST(106.700900 AS Decimal(9, 6)), N'123 Nguyễn Văn Linh, Quận 7, TP.HCM', 1, NULL, N'Cancelled', CAST(N'2026-02-20T08:30:00.0000000' AS DateTime2), CAST(N'2026-03-31T11:45:00.8010000' AS DateTime2), 1, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (2, 17, N'Kẹt trên mái nhà do nước lũ dâng cao', N'0917234567', N'Gia đình 3 người trèo lên mái nhà, nước vẫn đang dâng. Trẻ em 5 tuổi bị sốt cao.', CAST(10.762500 AS Decimal(9, 6)), CAST(106.682100 AS Decimal(9, 6)), N'456 Lê Văn Lương, Quận 7, TP.HCM', 1, NULL, N'Cancelled', CAST(N'2026-02-24T06:15:00.0000000' AS DateTime2), CAST(N'2026-03-31T11:44:42.5690000' AS DateTime2), 1, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (3, 18, N'Cầu sập, 2 xe bị mắc kẹt giữa dòng nước', N'0918234567', N'Cầu Bình Điền bị sập một phần, 2 ô tô với 8 người bên trong. Nước chảy xiết.', CAST(10.823100 AS Decimal(9, 6)), CAST(106.629700 AS Decimal(9, 6)), N'Cầu Bình Điền, Bình Chánh, TP.HCM', 2, NULL, N'Cancelled', CAST(N'2026-02-21T10:00:00.0000000' AS DateTime2), CAST(N'2026-03-31T11:44:48.4460000' AS DateTime2), 1, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (4, 19, N'Khu dân cư bị cô lập, hết lương thực 2 ngày', N'0919234567', N'Khoảng 30 hộ dân bị cô lập, không có thức ăn và nước sạch. Có phụ nữ mang thai.', CAST(10.850200 AS Decimal(9, 6)), CAST(106.752300 AS Decimal(9, 6)), N'Ấp 3, xã Vĩnh Lộc B, Bình Chánh', 2, NULL, N'Cancelled', CAST(N'2026-02-22T09:00:00.0000000' AS DateTime2), CAST(N'2026-02-22T11:00:00.0000000' AS DateTime2), 2, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (5, 20, N'Sạt lở đất đe dọa 10 hộ dân ven sông', N'0920234567', N'Bờ sông sạt lở nghiêm trọng, nứt đất cách nhà dân 5m. Cần di dời khẩn cấp.', CAST(10.891500 AS Decimal(9, 6)), CAST(106.610800 AS Decimal(9, 6)), N'Ấp 2, xã Tân Kiên, Bình Chánh', 3, NULL, N'Cancelled', CAST(N'2026-02-23T07:45:00.0000000' AS DateTime2), CAST(N'2026-03-31T11:44:45.5620000' AS DateTime2), 1, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (6, 16, N'Nước lũ tái ngập khu vực đã cứu hộ trước đó', N'0916234567', N'Khu vực Quận 7 bị ngập lại sau đợt mưa lớn đêm qua. Mực nước dâng nhanh 0.8m/giờ. 3 hộ cần sơ tán.', CAST(10.778200 AS Decimal(9, 6)), CAST(106.698500 AS Decimal(9, 6)), N'78 Huỳnh Tấn Phát, Quận 7, TP.HCM', 2, NULL, N'Cancelled', CAST(N'2026-02-25T02:00:00.0000000' AS DateTime2), CAST(N'2026-03-31T11:44:33.0630000' AS DateTime2), 1, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (7, 19, N'Đường bị ngập sâu, xe cứu thương không vào được', N'0919234567', N'Bệnh nhân cần cấp cứu nhưng đường ngập 1m, xe cứu thương không tiếp cận được. Cần xuồng y tế.', CAST(10.848700 AS Decimal(9, 6)), CAST(106.748900 AS Decimal(9, 6)), N'129 Quốc Lộ 1A, Bình Chánh', 1, NULL, N'Cancelled', CAST(N'2026-02-25T01:30:00.0000000' AS DateTime2), CAST(N'2026-03-19T09:28:55.3090000' AS DateTime2), 7, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (8, 20, N'Trường học bị ngập, 50 người dân tạm trú cần hỗ trợ', N'0920234567', N'Trường tiểu học đang làm điểm tạm trú bị nước tràn vào sân. 50 người cần di chuyển lên tầng 2.', CAST(10.893100 AS Decimal(9, 6)), CAST(106.612500 AS Decimal(9, 6)), N'Trường TH Tân Kiên, xã Tân Kiên, Bình Chánh', 2, NULL, N'Assigned', CAST(N'2026-02-25T03:00:00.0000000' AS DateTime2), CAST(N'2026-03-31T11:44:36.1350000' AS DateTime2), 1, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (9, 18, N'test', N'0328485142', N'test1', CAST(0.000000 AS Decimal(9, 6)), CAST(0.000000 AS Decimal(9, 6)), N'test', 1, NULL, N'Cancelled', CAST(N'2026-02-24T21:29:25.5890000' AS DateTime2), CAST(N'2026-03-31T11:44:39.5270000' AS DateTime2), 1, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (11, 21, N'test16', N'0328485142', N'test16', CAST(0.000000 AS Decimal(9, 6)), CAST(0.000000 AS Decimal(9, 6)), N'sdfsdfsdf', NULL, NULL, N'Cancelled', CAST(N'2026-02-26T07:01:18.1400000' AS DateTime2), CAST(N'2026-03-17T17:36:31.4880000' AS DateTime2), 1, 36, NULL, NULL, 5, 10, 21)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (12, 22, N'yesyeys', N'0937600539', N'yesyeys', CAST(0.000000 AS Decimal(9, 6)), CAST(0.000000 AS Decimal(9, 6)), N'yesyeys', 3, NULL, N'Assigned', CAST(N'2026-03-17T15:44:08.1860000' AS DateTime2), CAST(N'2026-03-31T11:44:27.0940000' AS DateTime2), 1, 69, NULL, N'0937600539', 5, 9, 55)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (13, NULL, N'Can ho tro y te khan cap', N'0384365488', N'dfvegn. Tinh trang: Het nhu yeu pham; Sap nha; Can dieu tri y te; Ngap duoi 1m.', CAST(10.787293 AS Decimal(9, 6)), CAST(106.606965 AS Decimal(9, 6)), N'Hẻm 228 Gò Xoài, Phường Bình Hưng Hòa, Ho Chi Minh City, 72011, Vietnam', 2, NULL, N'Cancelled', CAST(N'2026-03-19T06:29:58.9000000' AS DateTime2), CAST(N'2026-03-31T11:44:29.9430000' AS DateTime2), 1, 12, NULL, N'0384365488', NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (14, NULL, N'Sap nha can cuu ho', N'0904234567', N'hehe. Tinh trang: Sap nha; Ngap duoi 1m.', CAST(10.840899 AS Decimal(9, 6)), CAST(106.778161 AS Decimal(9, 6)), N'Truong Van Hai, Khu phố 53, Phường Tăng Nhơn Phú, Thủ Đức, Ho Chi Minh City, 71211, Vietnam', 2, NULL, N'Cancelled', CAST(N'2026-03-19T14:23:57.3980000' AS DateTime2), CAST(N'2026-03-19T14:31:19.2420000' AS DateTime2), 23, 18, NULL, N'0904234567', NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (15, NULL, N'Yeu cau cuu ho khan cap', N'0904234567', N'hehe. Tinh trang: Het nhu yeu pham; Ngap duoi 1m.', CAST(10.836703 AS Decimal(9, 6)), CAST(106.855907 AS Decimal(9, 6)), N'Phường Long Phước, Dĩ An, Ho Chi Minh City, 71216, Vietnam', 3, NULL, N'Cancelled', CAST(N'2026-03-19T14:33:59.0270000' AS DateTime2), CAST(N'2026-03-31T11:44:24.2260000' AS DateTime2), 1, 22, NULL, N'0904234567', NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (16, NULL, N'Can ho tro y te khan cap', N'0904234567', N'ề. Tinh trang: Sap nha; Can dieu tri y te.', CAST(10.755951 AS Decimal(9, 6)), CAST(106.698460 AS Decimal(9, 6)), N'Hẻm 209 Tôn Thất Thuyết, Phường Vĩnh Hội, Thủ Đức, Ho Chi Minh City, 72800, Vietnam', 3, NULL, N'Completed', CAST(N'2026-03-19T14:56:17.4450000' AS DateTime2), CAST(N'2026-03-31T17:30:27.9680000' AS DateTime2), 23, 11, NULL, N'0904234567', NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (17, NULL, N'Cần hỗ trợ y tế khẩn cấp', N'0384365402', N'1. Tinh trang: Can dieu tri y te; Ngap duoi 1m.. Tinh trang: Can dieu tri y te; Ngap duoi 1m.. Tình trạng: Can dieu tri y te; Ngap duoi 1m.', CAST(10.696945 AS Decimal(9, 6)), CAST(106.692138 AS Decimal(9, 6)), N'Hẻm 423 Đào Sư Tích, Xã Nhà Bè, Ho Chi Minh City, 72915, Vietnam', NULL, NULL, N'Cancelled', CAST(N'2026-03-21T17:29:36.7900000' AS DateTime2), CAST(N'2026-03-31T11:44:18.4900000' AS DateTime2), 1, 11, NULL, N'0384365402', 7, 2, 2)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (18, NULL, N'Can ho tro y te khan cap', N'0384365402', N'22. Tinh trang: Can dieu tri y te; Ngap duoi 1m.. Tinh trang: Can dieu tri y te; Ngap duoi 1m.', CAST(10.778274 AS Decimal(9, 6)), CAST(106.721712 AS Decimal(9, 6)), N'Tố Hữu, Khu phố 39, Phường An Khánh, Thủ Đức, Ho Chi Minh City, 72806, Vietnam', NULL, NULL, N'Cancelled', CAST(N'2026-03-23T00:19:32.8570000' AS DateTime2), CAST(N'2026-03-25T10:44:55.5340000' AS DateTime2), 1, 24, NULL, N'0384365402', NULL, NULL, NULL)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (19, NULL, N'Can ho tro y te khan cap', N'0933729766', N'99. Tinh trang: Het nhu yeu pham; Can dieu tri y te.. Tinh trang: Het nhu yeu pham; Can dieu tri y te.. Tinh trang: Het nhu yeu pham; Can dieu tri y te.', CAST(10.772616 AS Decimal(9, 6)), CAST(106.708429 AS Decimal(9, 6)), N'Cầu đi bộ, Khu phố 36, Phường An Khánh, Thủ Đức, Ho Chi Minh City, 71006, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-23T00:55:32.2370000' AS DateTime2), CAST(N'2026-03-25T10:44:51.1100000' AS DateTime2), 1, 67, NULL, N'0933729766', 21, 2, 44)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (20, NULL, N'Can ho tro y te khan cap', N'0933742834', N'121. Tinh trang: Can dieu tri y te.. Tinh trang: Can dieu tri y te.', CAST(10.814322 AS Decimal(9, 6)), CAST(106.799218 AS Decimal(9, 6)), N'150/4, Bưng Ông Thoàn, Phường Long Trường, Thủ Đức, Ho Chi Minh City, 71350, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-23T01:20:12.0000000' AS DateTime2), CAST(N'2026-03-25T10:45:08.0460000' AS DateTime2), 1, 21, NULL, N'0933742834', 7, 11, 3)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (21, 30, N'Yeu cau cuu ho khan cap', N'0384365204', N'212', CAST(10.781385 AS Decimal(9, 6)), CAST(106.718725 AS Decimal(9, 6)), N'Khu phố 39, Phường An Khánh, Thủ Đức, Ho Chi Minh City, 71006, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-23T01:32:55.5010000' AS DateTime2), CAST(N'2026-03-25T10:44:46.4210000' AS DateTime2), 1, 20, NULL, N'0384365204', 4, 11, 5)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (22, NULL, N'Can ho tro y te khan cap', N'0384365400', N'11. Tinh trang: Sap nha; Can dieu tri y te.', CAST(10.784757 AS Decimal(9, 6)), CAST(106.728334 AS Decimal(9, 6)), N'Luong Dinh Cua, Khu phố 39, Phường An Khánh, Thủ Đức, Ho Chi Minh City, 71108, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-24T04:19:36.9040000' AS DateTime2), CAST(N'2026-03-25T08:47:45.4370000' AS DateTime2), 1, 22, NULL, N'0384365400', 8, 12, 2)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (23, NULL, N'Can ho tro y te khan cap', N'0933729762', N'112. Tinh trang: Can dieu tri y te; Ngap duoi 1m.. Tinh trang: Can dieu tri y te; Ngap duoi 1m.', CAST(10.695745 AS Decimal(9, 6)), CAST(106.734682 AS Decimal(9, 6)), N'Hẻm 1806/127/9 Huỳnh Tấn Phát, Xã Nhà Bè, Ho Chi Minh City, 72915, Vietnam', 1, NULL, N'Assigned', CAST(N'2026-03-24T06:46:12.0590000' AS DateTime2), CAST(N'2026-03-31T11:44:13.0070000' AS DateTime2), 1, 22, NULL, N'0933729762', 9, 2, 11)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (24, NULL, N'Can ho tro y te khan cap', N'0933777775', N'Tinh trang: Can dieu tri y te; Ngap duoi 1m.', CAST(10.734726 AS Decimal(9, 6)), CAST(106.740129 AS Decimal(9, 6)), N'Cầu Bà Bướm, Phường Phú Thuận, Ho Chi Minh City, 71150, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-24T07:24:00.1920000' AS DateTime2), CAST(N'2026-03-31T11:44:15.8780000' AS DateTime2), 1, 22, NULL, N'0933777775', 1, 11, 10)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (25, 4, N'Can ho tro y te khan cap', N'0904234567', N'11. Tinh trang: Het nhu yeu pham; Can dieu tri y te.', CAST(10.767182 AS Decimal(9, 6)), CAST(106.712890 AS Decimal(9, 6)), N'Quan Tea, Đường N19, Khu phố 36, Phường An Khánh, Thủ Đức, Ho Chi Minh City, 72806, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-24T11:51:49.5970000' AS DateTime2), CAST(N'2026-03-25T09:32:28.8990000' AS DateTime2), 1, 22, NULL, N'0904234567', 6, 11, 5)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (26, NULL, N'Can ho tro y te khan cap', N'0384365477', N'Tinh trang: Can dieu tri y te; Ngap duoi 1m.. Tinh trang: Sap nha; Can dieu tri y te; Ngap duoi 1m.', CAST(10.781922 AS Decimal(9, 6)), CAST(106.704208 AS Decimal(9, 6)), N'Căn tin, Chu Mạnh Trinh, Khu phố 2, Saigon, Thủ Đức, Ho Chi Minh City, 71006, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-24T12:10:26.4980000' AS DateTime2), CAST(N'2026-03-25T09:32:13.0900000' AS DateTime2), 1, 22, NULL, N'0384365477', 5, 12, 5)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (27, NULL, N'Can ho tro y te khan cap', N'0933729777', N'Tinh trang: Can dieu tri y te; Ngap duoi 1m.', CAST(10.789416 AS Decimal(9, 6)), CAST(106.715534 AS Decimal(9, 6)), N'Nguyen Huu Canh Street, Khu phố 33, Phường Thạnh Mỹ Tây, Thủ Đức, Ho Chi Minh City, 71108, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-24T17:11:44.5520000' AS DateTime2), CAST(N'2026-03-25T10:45:12.7520000' AS DateTime2), 1, 12, NULL, N'0933729777', 7, 2, 3)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (28, NULL, N'Cần hỗ trợ y tế khẩn cấp', N'0384365444', N'36. Tình trạng: Cần điều trị y tế.', CAST(10.758412 AS Decimal(9, 6)), CAST(106.709801 AS Decimal(9, 6)), N'Hẻm 576 Đoàn Văn Bơ, Phường Khánh Hội, Thủ Đức, Ho Chi Minh City, 72800, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-24T17:20:15.4110000' AS DateTime2), CAST(N'2026-03-31T11:44:10.5610000' AS DateTime2), 1, 5, NULL, N'0384365444', 1, 3, 1)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (29, NULL, N'Cần hỗ trợ y tế khẩn cấp', N'0384365434', N'11w1wasw. Tình trạng: Cần điều trị y tế.', CAST(10.748219 AS Decimal(9, 6)), CAST(106.733321 AS Decimal(9, 6)), N'Hẻm 136 Đường Bùi Văn Ba, Khu phố 9, Phường Tân Thuận, Ho Chi Minh City, 72800, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-25T06:48:16.6170000' AS DateTime2), CAST(N'2026-03-31T11:44:07.6840000' AS DateTime2), 1, 22, NULL, N'0384365434', 9, 11, 2)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (30, NULL, N'Yêu cầu cứu hộ khẩn cấp', N'0384365402', N'', CAST(10.780388 AS Decimal(9, 6)), CAST(106.725459 AS Decimal(9, 6)), N'Thanh Hóa', 3, NULL, N'Cancelled', CAST(N'2026-03-25T07:50:19.9260000' AS DateTime2), CAST(N'2026-03-25T09:32:53.8480000' AS DateTime2), 1, 22, NULL, N'0384365402', 11, 2, 9)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (31, NULL, N'Cần hỗ trợ y tế khẩn cấp', N'0384365402', N'13. Tình trạng: Cần điều trị y tế; Ngập dưới 1m.', CAST(10.781684 AS Decimal(9, 6)), CAST(106.713233 AS Decimal(9, 6)), N'Đường N11, Khu phố 37, Phường An Khánh, Thủ Đức, Ho Chi Minh City, 00084, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-25T08:46:44.9370000' AS DateTime2), CAST(N'2026-03-25T09:34:01.4400000' AS DateTime2), 1, 22, NULL, N'0384365402', 18, 1, 3)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (32, NULL, N'Cần hỗ trợ y tế khẩn cấp', N'0384365402', N'jenwosmxwd3nw. Tình trạng: Cần điều trị y tế.', CAST(10.762460 AS Decimal(9, 6)), CAST(106.714949 AS Decimal(9, 6)), N'Bến Thương Khẩu, Cảng Sài Gòn Khánh Hội, Phường Xóm Chiếu, Thủ Đức, Ho Chi Minh City, 72800, Vietnam', 2, NULL, N'Cancelled', CAST(N'2026-03-25T09:02:53.8480000' AS DateTime2), CAST(N'2026-03-25T10:45:15.2240000' AS DateTime2), 1, 22, NULL, N'0384365402', 21, 1, 0)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (33, NULL, N'Cần hỗ trợ y tế khẩn cấp', N'0384365402', N'Tình trạng: Sập nhà; Cần điều trị y tế.', CAST(10.814922 AS Decimal(9, 6)), CAST(106.630765 AS Decimal(9, 6)), N'Bến Thương Khẩu, Cảng Sài Gòn Khánh Hội, Phường Xóm Chiếu, Thủ Đức, Ho Chi Minh City, 72800, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-25T09:07:12.4070000' AS DateTime2), CAST(N'2026-03-31T11:44:04.5420000' AS DateTime2), 1, 22, NULL, N'0384365402', 20, 1, 1)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (34, NULL, N'Yêu cầu cứu hộ khẩn cấp', N'0384365402', N'crtgvtdrxa', CAST(10.816160 AS Decimal(9, 6)), CAST(106.659526 AS Decimal(9, 6)), N'Phường Tân Sơn Hòa, Thuận An, Ho Chi Minh City, 72100, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-25T09:13:45.1910000' AS DateTime2), CAST(N'2026-03-31T11:44:01.1140000' AS DateTime2), 1, 22, NULL, N'0384365402', 10, 11, 1)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (35, 1030, N'Cần hỗ trợ y tế khẩn cấp', N'0384365402', N'Tình trạng: Cần điều trị y tế.', CAST(10.816150 AS Decimal(9, 6)), CAST(106.659511 AS Decimal(9, 6)), N'Phường Tân Sơn Hòa, Thuận An, Ho Chi Minh City, 72100, Vietnam', 2, NULL, N'Cancelled', CAST(N'2026-03-25T09:15:42.4450000' AS DateTime2), CAST(N'2026-03-31T11:44:01.0960000' AS DateTime2), 1, 2, NULL, N'0384365402', 1, 1, 0)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (36, 1030, N'Cần hỗ trợ y tế khẩn cấp', N'0384365402', N'Tình trạng: Cần điều trị y tế.', CAST(10.816202 AS Decimal(9, 6)), CAST(106.659541 AS Decimal(9, 6)), N'Phường Tân Sơn Hòa, Thuận An, Ho Chi Minh City, 72100, Vietnam', 2, NULL, N'Cancelled', CAST(N'2026-03-25T09:16:33.1000000' AS DateTime2), CAST(N'2026-03-31T11:43:55.3200000' AS DateTime2), 1, 2, NULL, N'0384365402', 1, 1, 0)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (37, 1030, N'Yêu cầu cứu hộ khẩn cấp', N'0384365402', N'', CAST(10.816173 AS Decimal(9, 6)), CAST(106.659564 AS Decimal(9, 6)), N'Phường Tân Sơn Hòa, Thuận An, Ho Chi Minh City, 72100, Vietnam', 3, NULL, N'Cancelled', CAST(N'2026-03-25T09:23:54.8720000' AS DateTime2), CAST(N'2026-03-25T09:54:14.7490000' AS DateTime2), 1, 5, NULL, N'0384365402', 5, 0, 0)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (38, NULL, N'Cần hỗ trợ y tế khẩn cấp', N'0384365402', N'Tình trạng: Cần điều trị y tế; Ngập dưới 1m.. Tình trạng: Cần điều trị y tế; Ngập dưới 1m.. Tình trạng: Cần điều trị y tế; Ngập dưới 1m.', CAST(10.816138 AS Decimal(9, 6)), CAST(106.659525 AS Decimal(9, 6)), N'Phường Tân Sơn Hòa, Thuận An, Ho Chi Minh City, 72100, Vietnam', 2, NULL, N'Cancelled', CAST(N'2026-03-25T09:27:45.1360000' AS DateTime2), CAST(N'2026-03-25T10:45:11.1890000' AS DateTime2), 1, 22, NULL, N'0384365401', 22, 0, 0)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (39, NULL, N'Sập nhà cần cứu hộ', N'0384365402', N'Tình trạng: Sập nhà.', CAST(10.816139 AS Decimal(9, 6)), CAST(106.659525 AS Decimal(9, 6)), N'Phường Tân Sơn Hòa, Thuận An, Ho Chi Minh City, 72100, Vietnam', 3, NULL, N'Cancelled', CAST(N'2026-03-25T09:34:43.8170000' AS DateTime2), CAST(N'2026-03-31T11:43:52.2840000' AS DateTime2), 1, 2, NULL, N'0384365402', 2, 0, 0)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (40, NULL, N'Yêu cầu cứu hộ khẩn cấp', N'0384365402', N'', CAST(10.816171 AS Decimal(9, 6)), CAST(106.659515 AS Decimal(9, 6)), N'Phường Tân Sơn Hòa, Thuận An, Ho Chi Minh City, 72100, Vietnam', 3, NULL, N'Cancelled', CAST(N'2026-03-25T09:51:59.4380000' AS DateTime2), CAST(N'2026-03-25T10:45:09.6130000' AS DateTime2), 1, 3, NULL, N'0384365402', 1, 2, 0)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (41, NULL, N'Yêu cầu cứu hộ khẩn cấp', N'0384365405', N'', CAST(10.816160 AS Decimal(9, 6)), CAST(106.659510 AS Decimal(9, 6)), N'Phường Tân Sơn Hòa, Thuận An, Ho Chi Minh City, 72100, Vietnam', 2, NULL, N'Cancelled', CAST(N'2026-03-25T10:48:26.9280000' AS DateTime2), CAST(N'2026-03-25T11:09:46.9430000' AS DateTime2), 1, 22, NULL, N'0384365402', 16, 4, 2)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (42, NULL, N'Sập nhà cần cứu hộ', N'0384365402', N'Tình trạng: Sập nhà.. Tình trạng: Sập nhà.', CAST(10.816196 AS Decimal(9, 6)), CAST(106.659503 AS Decimal(9, 6)), N'Phường Tân Sơn Hòa, Thuận An, Ho Chi Minh City, 72100, Vietnam', 3, NULL, N'Cancelled', CAST(N'2026-03-25T10:49:22.0400000' AS DateTime2), CAST(N'2026-03-25T11:09:44.5770000' AS DateTime2), 1, 22, NULL, N'0384365404', 22, 0, 0)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (43, NULL, N'Cần hỗ trợ y tế khẩn cấp', N'0384365402', N'Tình trạng: Cần điều trị y tế.', CAST(10.816129 AS Decimal(9, 6)), CAST(106.659546 AS Decimal(9, 6)), N'Phường Tân Sơn Hòa, Thuận An, Ho Chi Minh City, 72100, Vietnam', 3, NULL, N'Cancelled', CAST(N'2026-03-25T11:10:21.7540000' AS DateTime2), CAST(N'2026-03-31T11:43:48.2980000' AS DateTime2), 1, 22, NULL, N'0384365402', 22, 0, 0)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (44, NULL, N'Yêu cầu cứu hộ khẩn cấp', N'0384365402', N'Tình trạng: Hết nhu yếu phẩm.. Tình trạng: Hết nhu yếu phẩm.', CAST(10.816171 AS Decimal(9, 6)), CAST(106.659499 AS Decimal(9, 6)), N'Phường Tân Sơn Hòa, Thuận An, Ho Chi Minh City, 72100, Vietnam', 2, NULL, N'Cancelled', CAST(N'2026-03-25T11:11:04.9030000' AS DateTime2), CAST(N'2026-03-31T11:43:50.2160000' AS DateTime2), 1, 22, NULL, N'0384365403', 19, 3, 0)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (45, NULL, N'Yêu cầu cứu hộ khẩn cấp', N'0384365402', N'Tình trạng: Ngập dưới 1m.', CAST(10.774596 AS Decimal(9, 6)), CAST(106.698163 AS Decimal(9, 6)), N'Inspirée Vintage, 77, Ly Tu Trong Street, Khu phố 7, Ben Thanh, Thủ Đức, Ho Chi Minh City, 70000, Vietnam', 2, NULL, N'Cancelled', CAST(N'2026-03-25T17:12:14.3390000' AS DateTime2), CAST(N'2026-03-31T11:43:45.9290000' AS DateTime2), 1, 22, NULL, N'0384365402', 19, 1, 2)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (46, NULL, N'Ngập sâu cần cứu hộ', N'0384365402', N'Tình trạng: Hết nhu yếu phẩm; Ngập trên 1m.. Tình trạng: Hết nhu yếu phẩm; Ngập trên 1m.', CAST(10.780633 AS Decimal(9, 6)), CAST(106.704813 AS Decimal(9, 6)), N'15B8, Le Thanh Ton Street, Khu phố 3, Saigon, Thủ Đức, Ho Chi Minh City, 71006, Vietnam', 2, NULL, N'Assigned', CAST(N'2026-03-25T17:41:01.2070000' AS DateTime2), CAST(N'2026-03-25T17:48:57.0570000' AS DateTime2), 29, 2, NULL, N'0384365402', 1, 1, 0)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (47, NULL, N'Cần hỗ trợ y tế khẩn cấp', N'0384365402', N'Tình trạng: Cần điều trị y tế.', CAST(10.771092 AS Decimal(9, 6)), CAST(106.701908 AS Decimal(9, 6)), N'Ham Nghi Boulevard, Khu phố 7, Saigon, Thủ Đức, Ho Chi Minh City, 71010, Vietnam', 1, NULL, N'Cancelled', CAST(N'2026-03-25T18:12:46.4630000' AS DateTime2), CAST(N'2026-03-31T11:43:43.8700000' AS DateTime2), 1, 12, NULL, N'0384365402', 7, 3, 2)
INSERT [dbo].[rescue_requests] ([request_id], [citizen_id], [title], [phone], [description], [latitude], [longitude], [address], [priority_level_id], [team_id], [status], [created_at], [updated_at], [updated_by], [number_of_affected_people], [contact_name], [contact_phone], [adult_count], [elderly_count], [children_count]) VALUES (48, NULL, N'Yêu cầu cứu hộ khẩn cấp', N'0384365402', N'Tình trạng: Ngập dưới 1m.', CAST(10.771291 AS Decimal(9, 6)), CAST(106.699596 AS Decimal(9, 6)), N'Saigon Railway Passenger Transport Company, 136, Ham Nghi Boulevard, Khu phố 8, Ben Thanh, Thủ Đức, Ho Chi Minh City, 71010, Vietnam', 2, NULL, N'Verified', CAST(N'2026-03-31T11:46:58.0580000' AS DateTime2), CAST(N'2026-03-31T11:47:10.5300000' AS DateTime2), 3, 2, NULL, N'0384365402', 0, 1, 1)
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
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (19, 23, N'Leader', 1, CAST(N'2026-03-19T14:07:32.9770000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (19, 24, N'Member', 1, CAST(N'2026-03-19T14:07:32.9770000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (19, 25, N'Member', 1, CAST(N'2026-03-19T14:07:32.9770000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (20, 26, N'Leader', 1, CAST(N'2026-03-19T14:07:32.9800000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (20, 27, N'Member', 1, CAST(N'2026-03-19T14:07:32.9800000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (21, 28, N'Leader', 1, CAST(N'2026-03-19T14:07:32.9800000' AS DateTime2), NULL)
INSERT [dbo].[rescue_team_members] ([team_id], [user_id], [member_role], [is_active], [joined_at], [left_at]) VALUES (21, 29, N'Member', 1, CAST(N'2026-03-19T14:07:32.9830000' AS DateTime2), NULL)
GO
SET IDENTITY_INSERT [dbo].[rescue_teams] ON 

INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (6, N'Đội Cứu Hộ Alpha', CAST(N'2026-01-19T13:15:07.7030000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (9, N'Đội Cứu Hộ Beta', CAST(N'2026-01-19T13:15:07.7030000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (10, N'Đội Cứu Hộ Charlie', CAST(N'2026-01-19T13:15:07.7030000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (11, N'Đội Cứu Hộ Alpha - Đường Thủy', CAST(N'2026-01-15T08:00:00.0000000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (12, N'Đội Cứu Hộ Bravo - Đường Bộ', CAST(N'2026-01-15T08:00:00.0000000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (13, N'Đội Cứu Hộ Charlie - Tổng Hợp', CAST(N'2026-01-20T10:00:00.0000000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (14, N'Đội Y Tế Cơ Động Delta', CAST(N'2026-02-01T09:00:00.0000000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (15, N'Đội Hậu Cần Echo', CAST(N'2026-02-10T08:00:00.0000000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (16, N'Đội Cứu Hộ Alpha', CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (17, N'Đội Cứu Hộ Beta', CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (18, N'Đội Cứu Hộ Gamma', CAST(N'2026-02-24T20:24:32.5180000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (19, N'Đội Cứu Hộ Delta', CAST(N'2026-03-19T14:07:11.5370000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (20, N'Đội Cứu Hộ Echo', CAST(N'2026-03-19T14:07:11.5370000' AS DateTime2), NULL, NULL)
INSERT [dbo].[rescue_teams] ([team_id], [team_name], [created_at], [base_latitude], [base_longitude]) VALUES (21, N'Đội Cứu Hộ Foxtrot', CAST(N'2026-03-19T14:07:11.5370000' AS DateTime2), NULL, NULL)
SET IDENTITY_INSERT [dbo].[rescue_teams] OFF
GO
SET IDENTITY_INSERT [dbo].[stock_history] ON 

INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (1, N'IN', CAST(N'2026-02-24T22:47:14.6366667' AS DateTime2), N'1-5000,2-10000', N'Nhà cung cấp Minh Phát', N'Nhập lương thực đợt 1 đầu mùa lũ')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (2, N'IN', CAST(N'2026-02-25T22:47:14.6366667' AS DateTime2), N'5-15000,6-1000', N'Nhà cung cấp Aqua Việt', N'Nhập nước uống dự trữ')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (3, N'IN', CAST(N'2026-02-26T22:47:14.6366667' AS DateTime2), N'3-3000,4-2000', N'Nhà cung cấp Minh Phát', N'Nhập thịt hộp và sữa')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (4, N'IN', CAST(N'2026-02-27T22:47:14.6366667' AS DateTime2), N'7-500,8-300,9-400,10-2000', N'Kho Y Tế Quận 7', N'Nhập vật tư y tế đợt 1')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (5, N'IN', CAST(N'2026-02-28T22:47:14.6366667' AS DateTime2), N'11-3000,12-1500,13-2000', N'Nhà cung cấp Dệt May ABC', N'Nhập quần áo và đồ dùng')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (6, N'IN', CAST(N'2026-03-01T22:47:14.6366667' AS DateTime2), N'14-200,15-800', N'Tổ chức Cứu Trợ Quốc Tế', N'Nhận lều và bạt viện trợ')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (7, N'OUT', CAST(N'2026-03-02T22:47:14.6366667' AS DateTime2), N'1-200,5-100,3-50', N'Yêu cầu cứu hộ #1 - Q7', N'Xuất cứu trợ khẩn cấp cho 5 người')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (8, N'OUT', CAST(N'2026-03-03T22:47:14.6366667' AS DateTime2), N'2-300,5-200,1-150,12-20', N'Yêu cầu cứu hộ #2 - Q9', N'Xuất hàng cho khu vực bị cô lập')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (9, N'OUT', CAST(N'2026-03-04T22:47:14.6366667' AS DateTime2), N'7-50,8-30,9-40,10-100', N'Yêu cầu cứu hộ #3 - Thủ Đức', N'Xuất vật tư y tế cho người bị thương')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (10, N'OUT', CAST(N'2026-03-05T22:47:14.6366667' AS DateTime2), N'11-100,14-10,15-30', N'Điểm sơ tán tập trung Q1', N'Hỗ trợ khu sơ tán tập trung')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (11, N'IN', CAST(N'2026-03-06T22:47:14.6366667' AS DateTime2), N'1-2000,2-3000,5-5000', N'Ủy ban nhân dân TP.HCM', N'Nhận hàng bổ sung từ UBND thành phố')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (12, N'OUT', CAST(N'2026-03-06T22:47:14.6366667' AS DateTime2), N'4-100,9-50,10-200', N'Trại cứu trợ Quận 8', N'Xuất sữa và thuốc cho trại cứu trợ')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (13, N'IN', CAST(N'2026-03-17T17:08:21.7093996' AS DateTime2), N'12-36', N'Công ty TNHH Trang thiết bị Y tế Medico', N'789 Điện Biên Phủ, Phường 25, Bình Thạnh, TP.HCM')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (14, N'IN', CAST(N'2026-03-18T08:54:01.4064990' AS DateTime2), N'17-999', N'Công ty TNHH Trang thiết bị Y tế Medico', N'789 Điện Biên Phủ, Phường 25, Bình Thạnh, TP.HCM')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (15, N'IN', CAST(N'2026-03-18T08:55:11.4000378' AS DateTime2), N'10-69', N'Công ty TNHH Trang thiết bị Y tế Medico', N'789 Điện Biên Phủ, Phường 25, Bình Thạnh, TP.HCM')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (16, N'OUT', CAST(N'2026-03-18T08:56:02.6784331' AS DateTime2), N'14-69', N'28 Phạm Hùng, xã Bình Hưng, Bình Chánh, TP.HCM', N'Địa điểm nhận: 28 Phạm Hùng, xã Bình Hưng, Bình Chánh, TP.HCM | Ghi chú: Xuất cứu trợ cho Ban cứu trợ Bình Hưng')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (17, N'IN', CAST(N'2026-03-19T06:33:26.2681743' AS DateTime2), N'8-36', N'Công ty TNHH Trang thiết bị Y tế Medico', N'Nhận hàng bổ sung')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (18, N'OUT', CAST(N'2026-03-19T06:37:33.4275784' AS DateTime2), N'20-2', N'15 Hồng Hà, Phường 2, Tân Bình, TP.HCM', N'Địa điểm nhận: 15 Hồng Hà, Phường 2, Tân Bình, TP.HCM')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (19, N'IN', CAST(N'2026-04-01T10:00:00.0000000' AS DateTime2), N'1-1000,2-2000', N'Nhà hảo tâm Quận 1', N'Quyên góp từ cộng đồng')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (20, N'OUT', CAST(N'2026-04-02T11:30:00.0000000' AS DateTime2), N'6-50,8-20', N'Trạm cứu hộ Bình Chánh', N'Xuất hàng định kỳ')
INSERT [dbo].[stock_history] ([id], [type], [date], [body], [from_to], [note]) VALUES (21, N'IN', CAST(N'2026-04-03T09:15:00.0000000' AS DateTime2), N'3-500,4-300', N'Cửa hàng tiện lợi X', N'Tài trợ nhu yếu phẩm')
SET IDENTITY_INSERT [dbo].[stock_history] OFF
GO
SET IDENTITY_INSERT [dbo].[stock_units] ON 

INSERT [dbo].[stock_units] ([stock_unit_id], [unit_code], [unit_name], [unit_type], [region], [address], [supports_import], [supports_export], [is_active], [created_at], [updated_at]) VALUES (1, N'source-1', N'Công ty TNHH V?t tu C?u h? Á Châu', N'Công ty', N'Quận 1, TPHCM', N'123 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP.HCM', 1, 1, 1, CAST(N'2026-03-31T16:09:28.9960000' AS DateTime2), NULL)
INSERT [dbo].[stock_units] ([stock_unit_id], [unit_code], [unit_name], [unit_type], [region], [address], [supports_import], [supports_export], [is_active], [created_at], [updated_at]) VALUES (2, N'source-14', N'Kho Quận 2', N'Kho', N'Quận 2, TP.HCM', N'25 Mai Chí Thọ, Quận 2', 1, 1, 1, CAST(N'2026-03-31T16:27:36.7600000' AS DateTime2), NULL)
INSERT [dbo].[stock_units] ([stock_unit_id], [unit_code], [unit_name], [unit_type], [region], [address], [supports_import], [supports_export], [is_active], [created_at], [updated_at]) VALUES (3, N'source-15', N'Kho Quận 9', N'Kho', N'Quận 9, TP.HCM', N'88 Lê Văn Việt, Quận 9', 1, 1, 1, CAST(N'2026-03-31T16:27:36.7600000' AS DateTime2), NULL)
INSERT [dbo].[stock_units] ([stock_unit_id], [unit_code], [unit_name], [unit_type], [region], [address], [supports_import], [supports_export], [is_active], [created_at], [updated_at]) VALUES (4, N'source-16', N'UBND Phường Tân Định', N'UBND', N'Quận 1, TP.HCM', N'12 Hai Bà Trưng, Q1', 1, 0, 1, CAST(N'2026-03-31T16:27:36.7600000' AS DateTime2), NULL)
INSERT [dbo].[stock_units] ([stock_unit_id], [unit_code], [unit_name], [unit_type], [region], [address], [supports_import], [supports_export], [is_active], [created_at], [updated_at]) VALUES (5, N'source-17', N'Điểm phát hàng Quận 6', N'Trạm', N'Quận 6, TP.HCM', N'200 Hậu Giang, Quận 6', 0, 1, 1, CAST(N'2026-03-31T16:27:36.7600000' AS DateTime2), NULL)
INSERT [dbo].[stock_units] ([stock_unit_id], [unit_code], [unit_name], [unit_type], [region], [address], [supports_import], [supports_export], [is_active], [created_at], [updated_at]) VALUES (6, N'source-18', N'Trạm cứu trợ Quận 8', N'Trạm', N'Quận 8, TP.HCM', N'55 Phạm Thế Hiển, Q8', 1, 1, 1, CAST(N'2026-03-31T16:27:36.7600000' AS DateTime2), NULL)
INSERT [dbo].[stock_units] ([stock_unit_id], [unit_code], [unit_name], [unit_type], [region], [address], [supports_import], [supports_export], [is_active], [created_at], [updated_at]) VALUES (7, N'source-19', N'Kho Bình Tân', N'Kho', N'Bình Tân, TP.HCM', N'300 Tên Lửa, Bình Tân', 1, 1, 1, CAST(N'2026-03-31T16:27:36.7600000' AS DateTime2), NULL)
INSERT [dbo].[stock_units] ([stock_unit_id], [unit_code], [unit_name], [unit_type], [region], [address], [supports_import], [supports_export], [is_active], [created_at], [updated_at]) VALUES (8, N'source-20', N'UBND Phường Linh Trung', N'UBND', N'Thủ Đức, TP.HCM', N'10 Hoàng Diệu 2, Thủ Đức', 1, 0, 1, CAST(N'2026-03-31T16:27:36.7600000' AS DateTime2), NULL)
INSERT [dbo].[stock_units] ([stock_unit_id], [unit_code], [unit_name], [unit_type], [region], [address], [supports_import], [supports_export], [is_active], [created_at], [updated_at]) VALUES (9, N'source-21', N'Điểm phân phối Quận 3', N'Trạm', N'Quận 3, TP.HCM', N'77 Võ Văn Tần, Quận 3', 0, 1, 1, CAST(N'2026-03-31T16:27:36.7600000' AS DateTime2), NULL)
INSERT [dbo].[stock_units] ([stock_unit_id], [unit_code], [unit_name], [unit_type], [region], [address], [supports_import], [supports_export], [is_active], [created_at], [updated_at]) VALUES (10, N'source-22', N'Trung tâm hậu cần Tân Phú', N'Kho', N'Tân Phú, TP.HCM', N'90 Âu Cơ, Tân Phú', 1, 1, 1, CAST(N'2026-03-31T16:27:36.7600000' AS DateTime2), NULL)
SET IDENTITY_INSERT [dbo].[stock_units] OFF
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
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (21, N'tandat', N'$2a$11$Z4MAuoB8F26Ydr4rxXGiMOhesMKy5a.aTKUa.B3XOF3EGsRoqcSD2', N'Trịnh Tấn Thuận', N'0328485142', N'usesr@example.com', N'CITIZEN', 1, CAST(N'2026-02-26T05:42:47.2780000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (22, N'datdat', N'$2a$11$wMdwZLvJ1Pz5dHptA9kdde5ySYbOFTauAt8iSsKjPZgHsAuhInhgW', N'Vũ Văn Nam', N'0937600539', N'ddduser@example.com', N'CITIZEN', 1, CAST(N'2026-03-17T15:27:05.7440000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (23, N'delta_leader', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Leader Delta', N'0901000001', N'delta_leader@mail.com', N'RESCUE_TEAM', 1, CAST(N'2026-03-19T14:07:21.7130000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (24, N'delta_member1', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Member Delta 1', N'0901000002', N'delta_m1@mail.com', N'RESCUE_TEAM', 1, CAST(N'2026-03-19T14:07:21.7130000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (25, N'delta_member2', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Member Delta 2', N'0901000003', N'delta_m2@mail.com', N'RESCUE_TEAM', 1, CAST(N'2026-03-19T14:07:21.7130000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (26, N'echo_leader', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Leader Echo', N'0902000001', N'echo_leader@mail.com', N'RESCUE_TEAM', 1, CAST(N'2026-03-19T14:07:21.7130000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (27, N'echo_member1', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Member Echo 1', N'0902000002', N'echo_m1@mail.com', N'RESCUE_TEAM', 1, CAST(N'2026-03-19T14:07:21.7130000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (28, N'foxtrot_leader', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Leader Foxtrot', N'0903000001', N'foxtrot_leader@mail.com', N'RESCUE_TEAM', 1, CAST(N'2026-03-19T14:07:21.7130000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (29, N'foxtrot_member1', N'$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Member Foxtrot 1', N'0903000002', N'foxtrot_m1@mail.com', N'RESCUE_TEAM', 1, CAST(N'2026-03-19T14:07:21.7130000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (30, N'hjh', N'$2a$11$I/vlZHuGH7uyENnNWtg1U.dDUORT186vprZiKaglbkNilCX/3MsUe', N'Thuan nek', N'0384365204', N'trinhtanthuan33@gmail.com', N'CITIZEN', 1, CAST(N'2026-03-23T01:29:21.7190000' AS DateTime2), NULL)
INSERT [dbo].[users] ([user_id], [username], [password_hash], [full_name], [phone], [email], [role], [is_active], [created_at], [address]) VALUES (1030, N'trinhtanthuan22', N'$2a$11$Ro.qSdVfOFsG6i1aro2N1OdsZgsgCZpjk8.sEiziCpX1BG9/SmprO', N'Trịnh Tấn Thuận', N'0384365402', N'thuanttse162067@fpt.edu.vn', N'CITIZEN', 1, CAST(N'2026-03-25T09:15:12.0790000' AS DateTime2), NULL)
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

INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (3, N'BOAT-001', N'Xuồng Cao Tốc SR-01', 2, N'51S-0001', 12, N'AVAILABLE', CAST(N'2026-02-01' AS Date), NULL, NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (4, N'TRUCK-001', N'Xe Cứu Hộ Ford Ranger 4x4', 3, N'51A-1234', 6, N'AVAILABLE', CAST(N'2026-01-20' AS Date), CAST(N'2026-03-19T09:28:55.3090000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (5, N'HELI-001', N'Trực Thăng EC-135', 4, N'VN-8888', 4, N'AVAILABLE', CAST(N'2026-02-10' AS Date), CAST(N'2026-03-25T17:46:27.1570000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (6, N'AMPH-001', N'Xe Lưỡng Cư Sherp N1200', 5, N'51C-5678', 8, N'Maintenance', CAST(N'2025-12-15' AS Date), NULL, NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (7, N'DRONE-001', N'DJI Matrice 350 RTK', 6, N'51D-0099', 0, N'InUse', CAST(N'2026-02-18' AS Date), CAST(N'2026-02-25T08:43:53.1350000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (8, N'BOAT-002', N'Xuồng Composite SR-02', 2, N'51S-0002', 10, N'AVAILABLE', CAST(N'2026-03-01' AS Date), CAST(N'2026-03-31T17:30:27.9680000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (9, N'TRUCK-002', N'Xe Cứu Hộ Hyundai HD72', 3, N'51A-5678', 8, N'Available', CAST(N'2026-02-15' AS Date), CAST(N'2026-03-19T13:17:51.4600000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (10, N'HELI-002', N'Trực Thăng Bell 412', 4, N'VN-9999', 6, N'Available', CAST(N'2026-02-20' AS Date), CAST(N'2026-03-19T13:17:51.4600000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (11, N'AMPH-002', N'Xe Cấp Cứu Ford Transit', 5, N'51C-2222', 4, N'InUse', CAST(N'2026-02-25' AS Date), CAST(N'2026-03-19T06:19:59.0840000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (12, N'DRONE-002', N'DJI Matrice 300 RTK', 6, N'51D-0002', 0, N'InUse', CAST(N'2026-03-05' AS Date), CAST(N'2026-03-24T06:54:45.0210000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (13, N'BOAT-003', N'Xuồng Cao Tốc SR-03', 2, N'51S-0003', 12, N'AVAILABLE', CAST(N'2026-01-10' AS Date), CAST(N'2025-12-25T00:00:00.0000000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (14, N'TRUCK-003', N'Xe Cứu Hộ Isuzu D-Max', 3, N'51A-7777', 6, N'AVAILABLE', CAST(N'2026-02-01' AS Date), CAST(N'2025-11-01T00:00:00.0000000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (15, N'HELI-003', N'Trực Thăng Mi-17', 4, N'VN-7777', 10, N'AVAILABLE', CAST(N'2026-02-15' AS Date), CAST(N'2026-01-13T00:00:00.0000000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (16, N'AMPH-003', N'Xe Lưỡng Cư ARGO', 5, N'51C-9999', 8, N'AVAILABLE', CAST(N'2025-12-20' AS Date), CAST(N'2026-02-15T00:00:00.0000000' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[vehicles] ([vehicle_id], [vehicle_code], [vehicle_name], [vehicle_type_id], [license_plate], [capacity], [status], [last_maintenance], [updated_at], [latitude], [longitude], [current_location]) VALUES (17, N'DRONE-003', N'DJI Mavic 3 Enterprise', 6, N'51D-0003', 0, N'AVAILABLE', CAST(N'2026-03-01' AS Date), CAST(N'2026-03-17T00:00:00.0000000' AS DateTime2), NULL, NULL, NULL)
SET IDENTITY_INSERT [dbo].[vehicles] OFF
GO
/****** Object:  Index [IX_BlacklistedTokens_ExpiresAt]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE NONCLUSTERED INDEX [IX_BlacklistedTokens_ExpiresAt] ON [dbo].[blacklisted_tokens]
(
	[expires_at] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_BlacklistedTokens_Token]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE NONCLUSTERED INDEX [IX_BlacklistedTokens_Token] ON [dbo].[blacklisted_tokens]
(
	[token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_item_categories_category_code]    Script Date: 4/1/2026 1:05:33 AM ******/
ALTER TABLE [dbo].[item_categories] ADD  CONSTRAINT [UQ_item_categories_category_code] UNIQUE NONCLUSTERED 
(
	[category_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_RefreshTokens_Token]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_Token] ON [dbo].[refresh_tokens]
(
	[token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_RefreshTokens_UserId]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] ON [dbo].[refresh_tokens]
(
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_relief_items_item_code]    Script Date: 4/1/2026 1:05:33 AM ******/
ALTER TABLE [dbo].[relief_items] ADD  CONSTRAINT [UQ_relief_items_item_code] UNIQUE NONCLUSTERED 
(
	[item_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_operation]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE NONCLUSTERED INDEX [IX_rov_operation] ON [dbo].[rescue_operation_vehicles]
(
	[operation_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rov_vehicle]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE NONCLUSTERED INDEX [IX_rov_vehicle] ON [dbo].[rescue_operation_vehicles]
(
	[vehicle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_rescue_operations_request_id]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rescue_operations_request_id] ON [dbo].[rescue_operations]
(
	[request_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_rrsh_request_updatedat]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE NONCLUSTERED INDEX [IX_rrsh_request_updatedat] ON [dbo].[rescue_request_status_history]
(
	[request_id] ASC,
	[updated_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_rrsh_request_status]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rrsh_request_status] ON [dbo].[rescue_request_status_history]
(
	[request_id] ASC,
	[status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_rescue_requests_status_createdat_desc]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE NONCLUSTERED INDEX [IX_rescue_requests_status_createdat_desc] ON [dbo].[rescue_requests]
(
	[status] ASC,
	[created_at] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_rescue_requests_one_open_per_citizen]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_rescue_requests_one_open_per_citizen] ON [dbo].[rescue_requests]
(
	[citizen_id] ASC
)
WHERE ([status]<>'Completed' AND [status]<>'Cancelled' AND [status]<>'Duplicate' AND [citizen_id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_stock_units_unit_code]    Script Date: 4/1/2026 1:05:33 AM ******/
ALTER TABLE [dbo].[stock_units] ADD  CONSTRAINT [UQ_stock_units_unit_code] UNIQUE NONCLUSTERED 
(
	[unit_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_stock_units_active_export]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE NONCLUSTERED INDEX [IX_stock_units_active_export] ON [dbo].[stock_units]
(
	[is_active] ASC,
	[supports_export] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_stock_units_active_import]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE NONCLUSTERED INDEX [IX_stock_units_active_import] ON [dbo].[stock_units]
(
	[is_active] ASC,
	[supports_import] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_vehicles_status]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE NONCLUSTERED INDEX [IX_vehicles_status] ON [dbo].[vehicles]
(
	[status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_vehicles_license_plate]    Script Date: 4/1/2026 1:05:33 AM ******/
CREATE UNIQUE NONCLUSTERED INDEX [UQ_vehicles_license_plate] ON [dbo].[vehicles]
(
	[license_plate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_vehicles_vehicle_code]    Script Date: 4/1/2026 1:05:33 AM ******/
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
