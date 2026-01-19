-- Kiểm tra và xóa database nếu đã tồn tại
IF DB_ID('RescueManagementSystem') IS NOT NULL
BEGIN
    PRINT N'Database đã tồn tại. Đang xóa database cũ...';
    ALTER DATABASE RescueManagementSystem SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RescueManagementSystem;
    PRINT N'Đã xóa database cũ.';
END
GO

-- Tạo database
CREATE DATABASE RescueManagementSystem;
GO

USE RescueManagementSystem;
GO

-- Bảng users: Quản lý tất cả người dùng trong hệ thống
CREATE TABLE users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name NVARCHAR(100) NOT NULL,
    phone VARCHAR(20),
    email VARCHAR(100),
    role VARCHAR(20) NOT NULL CHECK (role IN ('CITIZEN', 'RESCUE_TEAM', 'COORDINATOR', 'MANAGER', 'ADMIN')),
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE(),
    is_active BIT DEFAULT 1
);

CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_role ON users(role);
CREATE INDEX idx_users_phone ON users(phone);
GO

-- Bảng priority_levels: Danh mục mức độ ưu tiên
CREATE TABLE priority_levels (
    priority_id INT IDENTITY(1,1) PRIMARY KEY,
    level_name VARCHAR(20) UNIQUE NOT NULL CHECK (level_name IN ('CRITICAL', 'HIGH', 'MEDIUM', 'LOW')),
    priority_order INT NOT NULL,
    description NVARCHAR(255),
    color_code VARCHAR(7)
);
GO

-- Bảng rescue_requests: Yêu cầu cứu hộ từ công dân
CREATE TABLE rescue_requests (
    request_id INT IDENTITY(1,1) PRIMARY KEY,
    citizen_id INT NOT NULL,
    title NVARCHAR(200) NOT NULL,
    description NVARCHAR(1000),
    latitude DECIMAL(10,8) NOT NULL,
    longitude DECIMAL(11,8) NOT NULL,
    address NVARCHAR(300),
    priority_level_id INT,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'VERIFIED', 'ASSIGNED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED')),
    number_of_people INT DEFAULT 1,
    has_children BIT DEFAULT 0,
    has_elderly BIT DEFAULT 0,
    has_disabled BIT DEFAULT 0,
    special_notes NVARCHAR(500),
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (citizen_id) REFERENCES users(user_id),
    FOREIGN KEY (priority_level_id) REFERENCES priority_levels(priority_id)
);

CREATE INDEX idx_rescue_requests_citizen ON rescue_requests(citizen_id);
CREATE INDEX idx_rescue_requests_status ON rescue_requests(status);
CREATE INDEX idx_rescue_requests_priority ON rescue_requests(priority_level_id);
CREATE INDEX idx_location ON rescue_requests(latitude, longitude);
CREATE INDEX idx_rescue_requests_created ON rescue_requests(created_at);
GO

-- Bảng rescue_request_images: Hình ảnh đính kèm yêu cầu
CREATE TABLE rescue_request_images (
    image_id INT IDENTITY(1,1) PRIMARY KEY,
    request_id INT NOT NULL,
    image_url VARCHAR(500) NOT NULL,
    description NVARCHAR(255),
    uploaded_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (request_id) REFERENCES rescue_requests(request_id)
);

CREATE INDEX idx_images_request ON rescue_request_images(request_id);
GO

-- Bảng rescue_request_status_history: Lịch sử thay đổi trạng thái
CREATE TABLE rescue_request_status_history (
    status_id INT IDENTITY(1,1) PRIMARY KEY,
    request_id INT NOT NULL,
    status VARCHAR(20) NOT NULL,
    notes NVARCHAR(500),
    updated_by INT NOT NULL,
    updated_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (request_id) REFERENCES rescue_requests(request_id),
    FOREIGN KEY (updated_by) REFERENCES users(user_id)
);

CREATE INDEX idx_status_history_request ON rescue_request_status_history(request_id);
CREATE INDEX idx_status_history_updated ON rescue_request_status_history(updated_at);
GO

-- Bảng rescue_teams: Đội cứu hộ
CREATE TABLE rescue_teams (
    team_id INT IDENTITY(1,1) PRIMARY KEY,
    team_name NVARCHAR(100) NOT NULL,
    team_code VARCHAR(20) UNIQUE NOT NULL,
    leader_id INT,
    status VARCHAR(20) NOT NULL DEFAULT 'AVAILABLE' CHECK (status IN ('AVAILABLE', 'ON_MISSION', 'RESTING', 'UNAVAILABLE')),
    current_capacity INT DEFAULT 0,
    max_capacity INT DEFAULT 10,
    created_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (leader_id) REFERENCES users(user_id)
);

CREATE INDEX idx_teams_code ON rescue_teams(team_code);
CREATE INDEX idx_teams_status ON rescue_teams(status);
CREATE INDEX idx_teams_leader ON rescue_teams(leader_id);
GO

-- Bảng rescue_team_members: Thành viên đội cứu hộ
CREATE TABLE rescue_team_members (
    member_id INT IDENTITY(1,1) PRIMARY KEY,
    team_id INT NOT NULL,
    user_id INT NOT NULL,
    role VARCHAR(20) NOT NULL CHECK (role IN ('LEADER', 'MEMBER')),
    specialization NVARCHAR(100),
    joined_at DATETIME2 DEFAULT GETDATE(),
    is_active BIT DEFAULT 1,
    FOREIGN KEY (team_id) REFERENCES rescue_teams(team_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);

CREATE INDEX idx_team_members_team ON rescue_team_members(team_id);
CREATE INDEX idx_team_members_user ON rescue_team_members(user_id);
CREATE UNIQUE INDEX uk_team_user ON rescue_team_members(team_id, user_id);
GO

-- Bảng vehicle_types: Danh mục loại phương tiện
CREATE TABLE vehicle_types (
    vehicle_type_id INT IDENTITY(1,1) PRIMARY KEY,
    type_name VARCHAR(50) UNIQUE NOT NULL CHECK (type_name IN ('BOAT', 'TRUCK', 'HELICOPTER', 'AMPHIBIOUS')),
    description NVARCHAR(255)
);
GO

-- Bảng vehicles: Phương tiện cứu hộ
CREATE TABLE vehicles (
    vehicle_id INT IDENTITY(1,1) PRIMARY KEY,
    vehicle_code VARCHAR(20) UNIQUE NOT NULL,
    vehicle_name NVARCHAR(100) NOT NULL,
    vehicle_type_id INT NOT NULL,
    license_plate VARCHAR(20),
    capacity INT,
    status VARCHAR(20) NOT NULL DEFAULT 'AVAILABLE' CHECK (status IN ('AVAILABLE', 'IN_USE', 'MAINTENANCE', 'UNAVAILABLE')),
    fuel_level DECIMAL(5,2),
    current_location NVARCHAR(300),
    last_maintenance DATE,
    created_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (vehicle_type_id) REFERENCES vehicle_types(vehicle_type_id)
);

CREATE INDEX idx_vehicles_code ON vehicles(vehicle_code);
CREATE INDEX idx_vehicles_status ON vehicles(status);
CREATE INDEX idx_vehicles_type ON vehicles(vehicle_type_id);
GO

-- Bảng vehicle_assignments: Phân bổ phương tiện cho đội
CREATE TABLE vehicle_assignments (
    vehicle_assignment_id INT IDENTITY(1,1) PRIMARY KEY,
    vehicle_id INT NOT NULL,
    team_id INT NOT NULL,
    assigned_at DATETIME2 DEFAULT GETDATE(),
    returned_at DATETIME2,
    status VARCHAR(20) NOT NULL DEFAULT 'ASSIGNED' CHECK (status IN ('ASSIGNED', 'RETURNED')),
    notes NVARCHAR(500),
    FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id),
    FOREIGN KEY (team_id) REFERENCES rescue_teams(team_id)
);

CREATE INDEX idx_vehicle_assignments_vehicle ON vehicle_assignments(vehicle_id);
CREATE INDEX idx_vehicle_assignments_team ON vehicle_assignments(team_id);
CREATE INDEX idx_vehicle_assignments_assigned ON vehicle_assignments(assigned_at);
GO

-- Bảng rescue_assignments: Phân công nhiệm vụ cứu hộ
CREATE TABLE rescue_assignments (
    assignment_id INT IDENTITY(1,1) PRIMARY KEY,
    request_id INT NOT NULL,
    team_id INT NOT NULL,
    assigned_by INT NOT NULL,
    vehicle_id INT,
    assigned_at DATETIME2 DEFAULT GETDATE(),
    started_at DATETIME2,
    completed_at DATETIME2,
    status VARCHAR(20) NOT NULL DEFAULT 'ASSIGNED' CHECK (status IN ('ASSIGNED', 'EN_ROUTE', 'ARRIVED', 'COMPLETED', 'FAILED')),
    notes NVARCHAR(500),
    FOREIGN KEY (request_id) REFERENCES rescue_requests(request_id),
    FOREIGN KEY (team_id) REFERENCES rescue_teams(team_id),
    FOREIGN KEY (assigned_by) REFERENCES users(user_id),
    FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id)
);

CREATE INDEX idx_assignments_request ON rescue_assignments(request_id);
CREATE INDEX idx_assignments_team ON rescue_assignments(team_id);
CREATE INDEX idx_assignments_assigned_by ON rescue_assignments(assigned_by);
CREATE INDEX idx_assignments_status ON rescue_assignments(status);
CREATE INDEX idx_assignments_assigned ON rescue_assignments(assigned_at);
GO

-- Bảng rescue_assignment_reports: Báo cáo kết quả cứu hộ
CREATE TABLE rescue_assignment_reports (
    report_id INT IDENTITY(1,1) PRIMARY KEY,
    assignment_id INT NOT NULL,
    people_rescued INT DEFAULT 0,
    situation_description NVARCHAR(1000),
    actions_taken NVARCHAR(1000),
    relief_items_distributed BIT DEFAULT 0,
    reported_by INT NOT NULL,
    reported_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (assignment_id) REFERENCES rescue_assignments(assignment_id),
    FOREIGN KEY (reported_by) REFERENCES users(user_id)
);

CREATE INDEX idx_reports_assignment ON rescue_assignment_reports(assignment_id);
CREATE INDEX idx_reports_reported ON rescue_assignment_reports(reported_at);
GO

-- Bảng item_categories: Danh mục phân loại hàng cứu trợ
CREATE TABLE item_categories (
    category_id INT IDENTITY(1,1) PRIMARY KEY,
    category_name NVARCHAR(100) UNIQUE NOT NULL CHECK (category_name IN (N'FOOD', N'WATER', N'MEDICINE', N'CLOTHING', N'SHELTER')),
    description NVARCHAR(255)
);
GO

-- Bảng relief_items: Hàng cứu trợ trong kho
CREATE TABLE relief_items (
    item_id INT IDENTITY(1,1) PRIMARY KEY,
    item_code VARCHAR(20) UNIQUE NOT NULL,
    item_name NVARCHAR(200) NOT NULL,
    category_id INT NOT NULL,
    unit VARCHAR(20) NOT NULL CHECK (unit IN ('KG', 'LITER', 'PIECE', 'BOX', 'PACK')),
    current_stock INT NOT NULL DEFAULT 0,
    min_stock_level INT DEFAULT 0,
    unit_price DECIMAL(15,2),
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (category_id) REFERENCES item_categories(category_id)
);

CREATE INDEX idx_relief_items_code ON relief_items(item_code);
CREATE INDEX idx_relief_items_category ON relief_items(category_id);
CREATE INDEX idx_relief_items_stock ON relief_items(current_stock);
GO

-- Bảng inventory_transactions: Lịch sử giao dịch nhập xuất
CREATE TABLE inventory_transactions (
    transaction_id INT IDENTITY(1,1) PRIMARY KEY,
    item_id INT NOT NULL,
    transaction_type VARCHAR(20) NOT NULL CHECK (transaction_type IN ('IN', 'OUT', 'ADJUSTMENT')),
    quantity INT NOT NULL,
    unit_price DECIMAL(15,2),
    reason NVARCHAR(500),
    created_by INT NOT NULL,
    transaction_date DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (item_id) REFERENCES relief_items(item_id),
    FOREIGN KEY (created_by) REFERENCES users(user_id)
);

CREATE INDEX idx_transactions_item ON inventory_transactions(item_id);
CREATE INDEX idx_transactions_type ON inventory_transactions(transaction_type);
CREATE INDEX idx_transactions_date ON inventory_transactions(transaction_date);
CREATE INDEX idx_transactions_created ON inventory_transactions(created_by);
GO

-- Bảng relief_distributions: Phân phối hàng cứu trợ
CREATE TABLE relief_distributions (
    distribution_id INT IDENTITY(1,1) PRIMARY KEY,
    request_id INT,
    distributed_by INT NOT NULL,
    assignment_id INT,
    distribution_date DATETIME2 DEFAULT GETDATE(),
    recipient_name NVARCHAR(100),
    number_of_recipients INT DEFAULT 1,
    notes NVARCHAR(500),
    is_confirmed BIT DEFAULT 0,
    confirmed_at DATETIME2,
    FOREIGN KEY (request_id) REFERENCES rescue_requests(request_id),
    FOREIGN KEY (distributed_by) REFERENCES users(user_id),
    FOREIGN KEY (assignment_id) REFERENCES rescue_assignments(assignment_id)
);

CREATE INDEX idx_distributions_request ON relief_distributions(request_id);
CREATE INDEX idx_distributions_distributed ON relief_distributions(distributed_by);
CREATE INDEX idx_distributions_assignment ON relief_distributions(assignment_id);
CREATE INDEX idx_distributions_date ON relief_distributions(distribution_date);
GO

-- Bảng relief_distribution_items: Chi tiết phân phối
CREATE TABLE relief_distribution_items (
    distribution_item_id INT IDENTITY(1,1) PRIMARY KEY,
    distribution_id INT NOT NULL,
    item_id INT NOT NULL,
    quantity INT NOT NULL,
    notes NVARCHAR(255),
    FOREIGN KEY (distribution_id) REFERENCES relief_distributions(distribution_id),
    FOREIGN KEY (item_id) REFERENCES relief_items(item_id)
);

CREATE INDEX idx_distribution_items_distribution ON relief_distribution_items(distribution_id);
CREATE INDEX idx_distribution_items_item ON relief_distribution_items(item_id);
GO

-- Bảng notifications: Thông báo cho người dùng
CREATE TABLE notifications (
    notification_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    notification_type VARCHAR(30) NOT NULL CHECK (notification_type IN ('REQUEST_UPDATE', 'ASSIGNMENT', 'DISTRIBUTION', 'SYSTEM')),
    title NVARCHAR(200) NOT NULL,
    message NVARCHAR(1000),
    is_read BIT DEFAULT 0,
    related_entity_type VARCHAR(50),
    related_entity_id INT,
    created_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);

CREATE INDEX idx_notifications_user ON notifications(user_id);
CREATE INDEX idx_notifications_read ON notifications(is_read);
CREATE INDEX idx_notifications_type ON notifications(notification_type);
CREATE INDEX idx_notifications_created ON notifications(created_at);
GO

-- Bảng system_configs: Cấu hình hệ thống
CREATE TABLE system_configs (
    config_id INT IDENTITY(1,1) PRIMARY KEY,
    config_key VARCHAR(100) UNIQUE NOT NULL,
    config_value NVARCHAR(500),
    description NVARCHAR(500),
    updated_at DATETIME2 DEFAULT GETDATE(),
    updated_by INT,
    FOREIGN KEY (updated_by) REFERENCES users(user_id)
);

CREATE INDEX idx_configs_key ON system_configs(config_key);
GO

-- Bảng activity_logs: Nhật ký hoạt động
CREATE TABLE activity_logs (
    log_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    entity_type VARCHAR(50),
    entity_id INT,
    description NVARCHAR(500),
    ip_address VARCHAR(45),
    created_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);

CREATE INDEX idx_logs_user ON activity_logs(user_id);
CREATE INDEX idx_logs_action ON activity_logs(action_type);
CREATE INDEX idx_logs_entity ON activity_logs(entity_type);
CREATE INDEX idx_logs_created ON activity_logs(created_at);
GO

-- ==============================================
-- INSERT DỮ LIỆU DANH MỤC
-- ==============================================

-- Insert dữ liệu cho priority_levels
INSERT INTO priority_levels (level_name, priority_order, description, color_code) VALUES
(N'CRITICAL', 1, N'Khẩn cấp - nguy hiểm tính mạng', '#FF0000'),
(N'HIGH', 2, N'Cao - cần xử lý nhanh', '#FF8C00'),
(N'MEDIUM', 3, N'Trung bình - ưu tiên bình thường', '#FFD700'),
(N'LOW', 4, N'Thấp - không khẩn cấp', '#90EE90');
GO

-- Insert dữ liệu cho vehicle_types
INSERT INTO vehicle_types (type_name, description) VALUES
(N'BOAT', N'Thuyền, xuồng cứu hộ'),
(N'TRUCK', N'Xe tải cứu hộ'),
(N'HELICOPTER', N'Trực thăng cứu hộ'),
(N'AMPHIBIOUS', N'Phương tiện lưỡng cư');
GO

-- Insert dữ liệu cho item_categories
INSERT INTO item_categories (category_name, description) VALUES
(N'FOOD', N'Thực phẩm, lương khô'),
(N'WATER', N'Nước uống, nước sinh hoạt'),
(N'MEDICINE', N'Thuốc men, dụng cụ y tế'),
(N'CLOTHING', N'Quần áo, chăn màn'),
(N'SHELTER', N'Lều bạt, vật dụng tạm trú');
GO

PRINT N'Đã tạo cấu trúc database và insert dữ liệu danh mục';
GO

-- ==============================================
-- INSERT DỮ LIỆU MẪU
-- ==============================================

-- Insert Users
-- Tất cả user có password: 12345 (đã hash bằng bcrypt)
-- LƯU Ý: Hash BCrypt phải được tạo từ ứng dụng C# hoặc tool BCrypt
-- Để tạo hash mới: 
--   1. Chạy ứng dụng
--   2. Gọi endpoint POST /api/auth/generate-hash với body {"username":"admin","password":"12345"}
--   3. Lấy hash từ response và cập nhật vào đây
-- 
-- Hash BCrypt hợp lệ cho password "12345" (work factor 10):
-- Hash: $2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe
-- Hash này được generate và verify với BCrypt.Net trong C# (HashGenerator tool)
-- Đã được verify và hoạt động đúng với password "12345"
INSERT INTO users (username, password_hash, full_name, phone, email, role, is_active) VALUES
('admin', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Nguyễn Văn An', '0901234567', 'admin@rescue.vn', 'ADMIN', 1),
('coordinator1', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Trần Thị Bình', '0902234567', 'coordinator1@rescue.vn', 'COORDINATOR', 1),
('coordinator2', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Lê Văn Cường', '0903234567', 'coordinator2@rescue.vn', 'COORDINATOR', 1),
('manager1', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Phạm Thị Dung', '0904234567', 'manager1@rescue.vn', 'MANAGER', 1),
('team_leader1', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Hoàng Văn Em', '0905234567', 'leader1@rescue.vn', 'RESCUE_TEAM', 1),
('team_leader2', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Vũ Thị Phương', '0906234567', 'leader2@rescue.vn', 'RESCUE_TEAM', 1),
('team_leader3', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Đặng Văn Giang', '0907234567', 'leader3@rescue.vn', 'RESCUE_TEAM', 1),
('member1', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Nguyễn Văn Hùng', '0908234567', 'member1@rescue.vn', 'RESCUE_TEAM', 1),
('member2', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Trần Thị Lan', '0909234567', 'member2@rescue.vn', 'RESCUE_TEAM', 1),
('member3', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Lê Văn Minh', '0910234567', 'member3@rescue.vn', 'RESCUE_TEAM', 1),
('member4', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Phạm Thị Nga', '0911234567', 'member4@rescue.vn', 'RESCUE_TEAM', 1),
('member5', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Hoàng Văn Oanh', '0912234567', 'member5@rescue.vn', 'RESCUE_TEAM', 1),
('member6', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Vũ Thị Phúc', '0913234567', 'member6@rescue.vn', 'RESCUE_TEAM', 1),
('member7', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Đặng Văn Quân', '0914234567', 'member7@rescue.vn', 'RESCUE_TEAM', 1),
('member8', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Nguyễn Thị Rạng', '0915234567', 'member8@rescue.vn', 'RESCUE_TEAM', 1),
('citizen1', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Trần Văn Sơn', '0916234567', 'citizen1@gmail.com', 'CITIZEN', 1),
('citizen2', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Lê Thị Tâm', '0917234567', 'citizen2@gmail.com', 'CITIZEN', 1),
('citizen3', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Phạm Văn Út', '0918234567', 'citizen3@gmail.com', 'CITIZEN', 1),
('citizen4', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Hoàng Thị Vân', '0919234567', 'citizen4@gmail.com', 'CITIZEN', 1),
('citizen5', '$2a$10$pthWamULpqnfKPe3EE.lcuUoJB6oQGicw/1lntSaLZw1HjRhaoIfe', N'Vũ Văn Xuân', '0920234567', 'citizen5@gmail.com', 'CITIZEN', 1);
GO

-- Insert Rescue Teams
INSERT INTO rescue_teams (team_name, team_code, leader_id, status, current_capacity, max_capacity) VALUES
(N'Đội Cứu Hộ Alpha', 'ALPHA-01', 5, 'AVAILABLE', 5, 10),
(N'Đội Cứu Hộ Bravo', 'BRAVO-02', 6, 'ON_MISSION', 4, 8),
(N'Đội Cứu Hộ Charlie', 'CHARLIE-03', 7, 'AVAILABLE', 3, 8);
GO

-- Insert Rescue Team Members
INSERT INTO rescue_team_members (team_id, user_id, role, specialization, is_active) VALUES
(1, 5, 'LEADER', N'Chỉ huy cứu hộ đường thủy', 1),
(1, 8, 'MEMBER', N'Lặn cứu hộ', 1),
(1, 9, 'MEMBER', N'Y tế cấp cứu', 1),
(1, 10, 'MEMBER', N'Vận hành thuyền cao tốc', 1),
(1, 11, 'MEMBER', N'Thông tin liên lạc', 1),
(2, 6, 'LEADER', N'Chỉ huy cứu hộ đường bộ', 1),
(2, 12, 'MEMBER', N'Lái xe địa hình', 1),
(2, 13, 'MEMBER', N'Y tế cấp cứu', 1),
(2, 14, 'MEMBER', N'Kỹ thuật thiết bị', 1),
(3, 7, 'LEADER', N'Chỉ huy cứu hộ tổng hợp', 1),
(3, 15, 'MEMBER', N'Lặn chuyên sâu', 1),
(3, 16, 'MEMBER', N'Hỗ trợ y tế', 1);
GO

-- Insert Vehicles
INSERT INTO vehicles (vehicle_code, vehicle_name, vehicle_type_id, license_plate, capacity, status, fuel_level, current_location) VALUES
('BOAT-001', N'Thuyền Cứu Hộ 01', 1, NULL, 15, 'AVAILABLE', 85.50, N'Bến Thủy Sài Gòn'),
('BOAT-002', N'Xuồng Cao Tốc 02', 1, NULL, 8, 'IN_USE', 62.30, N'Khu vực Bình Thạnh'),
('TRUCK-001', N'Xe Cứu Hộ 4x4', 2, '51A-12345', 6, 'AVAILABLE', 75.00, N'Trụ sở Quận 1'),
('TRUCK-002', N'Xe Vận Chuyển Cứu Trợ', 2, '51B-67890', 10, 'IN_USE', 90.00, N'Khu vực Bình Tân'),
('HELI-001', N'Trực Thăng Cứu Hộ', 3, NULL, 4, 'AVAILABLE', 55.00, N'Sân bay Tân Sơn Nhất'),
('AMPH-001', N'Xe Lưỡng Cư 01', 4, '51C-11111', 8, 'MAINTENANCE', 0, N'Xưởng bảo dưỡng');
GO

-- Insert Vehicle Assignments
INSERT INTO vehicle_assignments (vehicle_id, team_id, assigned_at, returned_at, status, notes) VALUES
(2, 2, DATEADD(HOUR, -3, GETDATE()), NULL, 'ASSIGNED', N'Nhiệm vụ cứu hộ tại Bình Thạnh'),
(4, 2, DATEADD(HOUR, -3, GETDATE()), NULL, 'ASSIGNED', N'Vận chuyển hàng cứu trợ'),
(1, 1, DATEADD(DAY, -2, GETDATE()), DATEADD(DAY, -1, GETDATE()), 'RETURNED', N'Hoàn thành nhiệm vụ'),
(3, 1, DATEADD(DAY, -5, GETDATE()), DATEADD(DAY, -4, GETDATE()), 'RETURNED', N'Hoàn thành nhiệm vụ');
GO

-- Insert Rescue Requests
INSERT INTO rescue_requests (citizen_id, title, description, latitude, longitude, address, priority_level_id, status, number_of_people, has_children, has_elderly, has_disabled, special_notes) VALUES
(16, N'Nước ngập cao cần sơ tán gấp', N'Nhà bị ngập 1.5m, có 2 trẻ em và 1 người già, cần hỗ trợ ngay', 10.7769, 106.7009, N'123 Đường Nguyễn Văn Linh, Quận 7, TP.HCM', 1, 'COMPLETED', 5, 1, 1, 0, N'Có người già bị bệnh tim'),
(17, N'Kẹt trên tầng 2 do nước lũ', N'Gia đình 3 người kẹt trên tầng 2, nước ngập tầng 1 hoàn toàn', 10.7625, 106.6821, N'456 Đường Lê Văn Lương, Quận 7, TP.HCM', 2, 'IN_PROGRESS', 3, 1, 0, 0, NULL),
(18, N'Cần cứu trợ lương thực', N'Nhà bị cô lập, hết lương thực và nước uống', 10.8231, 106.6297, N'789 Đường Tân Hương, Quận Tân Phú, TP.HCM', 3, 'PENDING', 2, 0, 0, 0, NULL);
GO

-- ==============================================
-- SCRIPT HELPER: CẬP NHẬT PASSWORD HASH
-- ==============================================
-- Nếu hash password không hoạt động, làm theo các bước sau:

-- BƯỚC 1: Test hash hiện tại
-- Chạy ứng dụng và gọi endpoint POST /api/auth/test-verify với body:
-- {
--   "password": "12345",
--   "hash": "$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy"
-- }
-- Nếu verifyResult = false, hash không đúng, cần tạo hash mới

-- BƯỚC 2: Generate hash mới
-- Gọi endpoint POST /api/auth/generate-hash với body:
-- {
--   "username": "admin",
--   "password": "12345"
-- }
-- Lấy hash từ response (field "hash")

-- BƯỚC 3: Cập nhật hash vào database
-- Chạy script SQL sau (thay <NEW_HASH_HERE> bằng hash mới từ BƯỚC 2):

-- Cập nhật cho tất cả users:
-- UPDATE users 
-- SET password_hash = '<NEW_HASH_HERE>' 
-- WHERE password_hash = '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy';

-- Hoặc cập nhật cho một user cụ thể:
-- UPDATE users 
-- SET password_hash = '<NEW_HASH_HERE>' 
-- WHERE username = 'admin';
GO

PRINT N'Đã hoàn tất tạo database và insert dữ liệu mẫu';
PRINT N'';
PRINT N'========================================';
PRINT N'LƯU Ý QUAN TRỌNG VỀ PASSWORD HASH:';
PRINT N'========================================';
PRINT N'Hash hiện tại có thể không khớp với password "12345"';
PRINT N'';
PRINT N'Để fix lỗi đăng nhập:';
PRINT N'1. Chạy ứng dụng';
PRINT N'2. Test hash hiện tại: POST /api/auth/test-verify';
PRINT N'3. Generate hash mới: POST /api/auth/generate-hash';
PRINT N'4. Cập nhật hash vào database bằng script helper ở trên';
PRINT N'';
PRINT N'Hoặc sử dụng endpoint generate-hash để lấy hash mới và cập nhật trực tiếp vào database';
GO