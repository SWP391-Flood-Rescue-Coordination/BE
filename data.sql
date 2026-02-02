-- =============================================
-- RESCUE MANAGEMENT SYSTEM - SAMPLE DATA
-- SQL Server Database Insert Statements
-- =============================================

USE RescueManagementDB;
GO

-- =============================================
-- INSERT DATA
-- =============================================

-- Insert Users
SET IDENTITY_INSERT users ON;
INSERT INTO users (user_id, username, password_hash, full_name, phone, email, role, is_active) VALUES
(1, 'admin', '$2a$10$abcdefghijklmnopqrstuv', N'Nguyễn Văn Admin', '0901234567', 'admin@rescue.vn', 'ADMIN', 1),
(2, 'coordinator1', '$2a$10$abcdefghijklmnopqrstuv', N'Trần Thị Điều Phối', '0902345678', 'coordinator1@rescue.vn', 'COORDINATOR', 1),
(3, 'coordinator2', '$2a$10$abcdefghijklmnopqrstuv', N'Lê Văn Phối Hợp', '0903456789', 'coordinator2@rescue.vn', 'COORDINATOR', 1),
(4, 'manager1', '$2a$10$abcdefghijklmnopqrstuv', N'Phạm Thị Quản Lý', '0904567890', 'manager1@rescue.vn', 'MANAGER', 1),
(5, 'team_leader1', '$2a$10$abcdefghijklmnopqrstuv', N'Hoàng Văn Đội Trưởng', '0905678901', 'leader1@rescue.vn', 'RESCUE_TEAM', 1),
(6, 'team_leader2', '$2a$10$abcdefghijklmnopqrstuv', N'Võ Thị Anh', '0906789012', 'leader2@rescue.vn', 'RESCUE_TEAM', 1),
(7, 'team_leader3', '$2a$10$abcdefghijklmnopqrstuv', N'Đặng Văn Cường', '0907890123', 'leader3@rescue.vn', 'RESCUE_TEAM', 1),
(8, 'member1', '$2a$10$abcdefghijklmnopqrstuv', N'Nguyễn Văn Thành Viên', '0908901234', 'member1@rescue.vn', 'RESCUE_TEAM', 1),
(9, 'member2', '$2a$10$abcdefghijklmnopqrstuv', N'Trần Thị Bình', '0909012345', 'member2@rescue.vn', 'RESCUE_TEAM', 1),
(10, 'member3', '$2a$10$abcdefghijklmnopqrstuv', N'Lê Văn Chiến', '0900123456', 'member3@rescue.vn', 'RESCUE_TEAM', 1),
(11, 'member4', '$2a$10$abcdefghijklmnopqrstuv', N'Phạm Thị Dung', '0911234567', 'member4@rescue.vn', 'RESCUE_TEAM', 1),
(12, 'member5', '$2a$10$abcdefghijklmnopqrstuv', N'Hoàng Văn Đức', '0912345678', 'member5@rescue.vn', 'RESCUE_TEAM', 1),
(13, 'citizen1', '$2a$10$abcdefghijklmnopqrstuv', N'Nguyễn Thị Lan', '0913456789', 'citizen1@gmail.com', 'CITIZEN', 1),
(14, 'citizen2', '$2a$10$abcdefghijklmnopqrstuv', N'Trần Văn Minh', '0914567890', 'citizen2@gmail.com', 'CITIZEN', 1),
(15, 'citizen3', '$2a$10$abcdefghijklmnopqrstuv', N'Lê Thị Nga', '0915678901', 'citizen3@gmail.com', 'CITIZEN', 1),
(16, 'citizen4', '$2a$10$abcdefghijklmnopqrstuv', N'Phạm Văn Phong', '0916789012', 'citizen4@gmail.com', 'CITIZEN', 1),
(17, 'citizen5', '$2a$10$abcdefghijklmnopqrstuv', N'Hoàng Thị Quỳnh', '0917890123', 'citizen5@gmail.com', 'CITIZEN', 1),
(18, 'citizen6', '$2a$10$abcdefghijklmnopqrstuv', N'Võ Văn Sơn', '0918901234', 'citizen6@gmail.com', 'CITIZEN', 1),
(19, 'citizen7', '$2a$10$abcdefghijklmnopqrstuv', N'Đặng Thị Thảo', '0919012345', 'citizen7@gmail.com', 'CITIZEN', 1),
(20, 'citizen8', '$2a$10$abcdefghijklmnopqrstuv', N'Bùi Văn Tùng', '0910123456', 'citizen8@gmail.com', 'CITIZEN', 1);
SET IDENTITY_INSERT users OFF;
GO

-- Insert Priority Levels
SET IDENTITY_INSERT priority_levels ON;
INSERT INTO priority_levels (priority_id, level_name, priority_order, description, color_code) VALUES
(1, 'CRITICAL', 1, N'Nguy hiểm đến tính mạng, cần cứu hộ ngay lập tức', '#FF0000'),
(2, 'HIGH', 2, N'Tình huống nghiêm trọng, cần ưu tiên cao', '#FF6600'),
(3, 'MEDIUM', 3, N'Cần hỗ trợ trong thời gian hợp lý', '#FFCC00'),
(4, 'LOW', 4, N'Tình huống ổn định, có thể xử lý sau', '#00CC00');
SET IDENTITY_INSERT priority_levels OFF;
GO

-- Insert Rescue Requests
SET IDENTITY_INSERT rescue_requests ON;
INSERT INTO rescue_requests (request_id, citizen_id, title, description, latitude, longitude, address, priority_level_id, status, number_of_people, has_children, has_elderly, has_disabled, special_notes, created_at) VALUES
(1, 13, N'Nước ngập đến mái nhà', N'Gia đình 5 người đang mắc kẹt trên tầng 2, nước ngập nhanh', 10.77694400, 106.70083900, N'123 Nguyễn Văn Linh, Q7, TP.HCM', 1, 'COMPLETED', 5, 1, 1, 0, N'Có 2 trẻ em dưới 5 tuổi và 1 người già 80 tuổi', DATEADD(HOUR, -48, GETDATE())),
(2, 14, N'Bị cô lập do lũ', N'Không có thức ăn và nước uống, cần hỗ trợ khẩn cấp', 10.82305600, 106.62972200, N'456 Lê Văn Việt, Q9, TP.HCM', 1, 'IN_PROGRESS', 3, 0, 0, 1, N'Có 1 người khuyết tật cần xe lăn', DATEADD(HOUR, -36, GETDATE())),
(3, 15, N'Cần sơ tán khẩn cấp', N'Nhà bị sụt lún nghiêm trọng do mưa lũ', 10.85027800, 106.77194400, N'789 Phạm Văn Đồng, Thủ Đức, TP.HCM', 2, 'ASSIGNED', 4, 1, 0, 0, N'Có trẻ nhỏ 2 tuổi', DATEADD(HOUR, -24, GETDATE())),
(4, 16, N'Hết lương thực', N'Gia đình đã hết thức ăn 2 ngày, cần cứu trợ', 10.81638900, 106.63527800, N'321 Xa lộ Hà Nội, Q2, TP.HCM', 2, 'VERIFIED', 6, 1, 1, 0, N'Có người già bị bệnh tiểu đường', DATEADD(HOUR, -20, GETDATE())),
(5, 17, N'Mái nhà bị tốc', N'Mái nhà bị gió bão thổi bay, mưa vào nhà', 10.73972200, 106.67888900, N'654 Nguyễn Thị Minh Khai, Q3, TP.HCM', 3, 'PENDING', 2, 0, 0, 0, NULL, DATEADD(HOUR, -12, GETDATE())),
(6, 18, N'Cần nước sạch', N'Nguồn nước bị ô nhiễm sau lũ, cần nước uống', 10.76277800, 106.68194400, N'987 Trần Hưng Đạo, Q5, TP.HCM', 3, 'PENDING', 4, 1, 0, 0, NULL, DATEADD(HOUR, -8, GETDATE())),
(7, 19, N'Đường vào bị cô lập', N'Cần xuồng để đi ra ngoài lấy thuốc cho người bệnh', 10.80305600, 106.70888900, N'159 Võ Văn Ngân, Thủ Đức, TP.HCM', 2, 'PENDING', 3, 0, 1, 0, N'Người già cần thuốc tim mạch gấp', DATEADD(HOUR, -6, GETDATE())),
(8, 20, N'Cần hỗ trợ y tế', N'Có người bị thương do ngã khi sơ tán', 10.77500000, 106.70500000, N'753 Lý Thường Kiệt, Q10, TP.HCM', 2, 'PENDING', 2, 0, 0, 0, N'Chân bị gãy, cần xe cứu thương', DATEADD(HOUR, -4, GETDATE()));
SET IDENTITY_INSERT rescue_requests OFF;
GO

-- Insert Rescue Request Images
SET IDENTITY_INSERT rescue_request_images ON;
INSERT INTO rescue_request_images (image_id, request_id, image_url, description, uploaded_at) VALUES
(1, 1, '/uploads/rescue/req1_img1.jpg', N'Nước ngập đến tầng 1', DATEADD(HOUR, -48, GETDATE())),
(2, 1, '/uploads/rescue/req1_img2.jpg', N'Gia đình trên tầng 2', DATEADD(HOUR, -47, GETDATE())),
(3, 2, '/uploads/rescue/req2_img1.jpg', N'Đường vào nhà bị ngập', DATEADD(HOUR, -36, GETDATE())),
(4, 3, '/uploads/rescue/req3_img1.jpg', N'Nhà bị sụt lún', DATEADD(HOUR, -24, GETDATE())),
(5, 4, '/uploads/rescue/req4_img1.jpg', N'Tủ lạnh trống', DATEADD(HOUR, -20, GETDATE()));
SET IDENTITY_INSERT rescue_request_images OFF;
GO

-- Insert Rescue Teams
SET IDENTITY_INSERT rescue_teams ON;
INSERT INTO rescue_teams (team_id, team_name, team_code, leader_id, status, current_capacity, max_capacity) VALUES
(1, N'Đội Cứu Hộ Alpha', 'TEAM-ALPHA', 5, 'ON_MISSION', 5, 8),
(2, N'Đội Cứu Hộ Beta', 'TEAM-BETA', 6, 'AVAILABLE', 4, 8),
(3, N'Đội Cứu Hộ Gamma', 'TEAM-GAMMA', 7, 'AVAILABLE', 3, 6);
SET IDENTITY_INSERT rescue_teams OFF;
GO

-- Insert Rescue Team Members
SET IDENTITY_INSERT rescue_team_members ON;
INSERT INTO rescue_team_members (member_id, team_id, user_id, role, specialization, is_active) VALUES
(1, 1, 5, 'LEADER', N'Chỉ huy cứu hộ', 1),
(2, 1, 8, 'MEMBER', N'Lặn cứu hộ', 1),
(3, 1, 9, 'MEMBER', N'Y tế cấp cứu', 1),
(4, 1, 10, 'MEMBER', N'Vận hành xuồng', 1),
(5, 1, 11, 'MEMBER', N'Thông tin liên lạc', 1),
(6, 2, 6, 'LEADER', N'Chỉ huy cứu hộ', 1),
(7, 2, 12, 'MEMBER', N'Kỹ thuật cứu hộ', 1),
(8, 2, 8, 'MEMBER', N'Y tế', 1),
(9, 2, 9, 'MEMBER', N'Logistics', 1),
(10, 3, 7, 'LEADER', N'Chỉ huy', 1),
(11, 3, 10, 'MEMBER', N'Lặn', 1),
(12, 3, 11, 'MEMBER', N'Kỹ thuật', 1);
SET IDENTITY_INSERT rescue_team_members OFF;
GO

-- Insert Vehicle Types
SET IDENTITY_INSERT vehicle_types ON;
INSERT INTO vehicle_types (vehicle_type_id, type_name, description) VALUES
(1, 'BOAT', N'Xuồng cứu hộ'),
(2, 'TRUCK', N'Xe tải vận chuyển'),
(3, 'HELICOPTER', N'Trực thăng cứu hộ'),
(4, 'AMPHIBIOUS', N'Xe lội nước');
SET IDENTITY_INSERT vehicle_types OFF;
GO

-- Insert Vehicles
SET IDENTITY_INSERT vehicles ON;
INSERT INTO vehicles (vehicle_id, vehicle_code, vehicle_name, vehicle_type_id, license_plate, capacity, status, fuel_level, current_location, last_maintenance) VALUES
(1, 'VH-BOAT-01', N'Xuồng Cứu Hộ 01', 1, NULL, 8, 'IN_USE', 85.50, N'Quận 7, TP.HCM', '2025-01-10'),
(2, 'VH-BOAT-02', N'Xuồng Cứu Hộ 02', 1, NULL, 10, 'AVAILABLE', 92.00, N'Kho Thủ Đức', '2025-01-08'),
(3, 'VH-BOAT-03', N'Xuồng Cứu Hộ 03', 1, NULL, 6, 'AVAILABLE', 78.30, N'Kho Quận 2', '2025-01-12'),
(4, 'VH-TRUCK-01', N'Xe Tải Cứu Trợ 01', 2, '51C-12345', 2000, 'AVAILABLE', 65.00, N'Kho Trung Tâm', '2025-01-05'),
(5, 'VH-TRUCK-02', N'Xe Tải Cứu Trợ 02', 2, '51C-67890', 3000, 'AVAILABLE', 88.00, N'Kho Trung Tâm', '2025-01-11'),
(6, 'VH-HELI-01', N'Trực Thăng CH-01', 3, NULL, 4, 'AVAILABLE', 95.00, N'Sân Bay Tân Sơn Nhất', '2025-01-13'),
(7, 'VH-AMP-01', N'Xe Lội Nước 01', 4, '51D-11111', 6, 'AVAILABLE', 70.50, N'Kho Quận 9', '2025-01-09');
SET IDENTITY_INSERT vehicles OFF;
GO

-- Insert Vehicle Assignments
SET IDENTITY_INSERT vehicle_assignments ON;
INSERT INTO vehicle_assignments (vehicle_assignment_id, vehicle_id, team_id, assigned_at, returned_at, status, notes) VALUES
(1, 1, 1, DATEADD(HOUR, -36, GETDATE()), NULL, 'ASSIGNED', N'Nhiệm vụ cứu hộ tại Q7'),
(2, 2, 2, DATEADD(HOUR, -12, GETDATE()), DATEADD(HOUR, -2, GETDATE()), 'RETURNED', N'Đã hoàn thành nhiệm vụ');
SET IDENTITY_INSERT vehicle_assignments OFF;
GO

-- Insert Rescue Assignments
SET IDENTITY_INSERT rescue_assignments ON;
INSERT INTO rescue_assignments (assignment_id, request_id, team_id, assigned_by, vehicle_id, assigned_at, started_at, completed_at, status, notes) VALUES
(1, 1, 1, 2, 1, DATEADD(HOUR, -42, GETDATE()), DATEADD(HOUR, -40, GETDATE()), DATEADD(HOUR, -36, GETDATE()), 'COMPLETED', N'Đã cứu thành công 5 người'),
(2, 2, 1, 2, 1, DATEADD(HOUR, -32, GETDATE()), DATEADD(HOUR, -30, GETDATE()), NULL, 'ARRIVED', N'Đang tiến hành cứu hộ'),
(3, 3, 2, 3, 2, DATEADD(HOUR, -20, GETDATE()), DATEADD(HOUR, -18, GETDATE()), NULL, 'EN_ROUTE', N'Đang trên đường đến hiện trường');
SET IDENTITY_INSERT rescue_assignments OFF;
GO

-- Insert Rescue Assignment Reports
SET IDENTITY_INSERT rescue_assignment_reports ON;
INSERT INTO rescue_assignment_reports (report_id, assignment_id, people_rescued, situation_description, actions_taken, relief_items_distributed, reported_by, reported_at) VALUES
(1, 1, 5, N'Nước ngập cao 2.5m, gia đình mắc kẹt tầng 2. Có 2 trẻ em và 1 người già.', N'Sử dụng xuồng tiếp cận, đưa toàn bộ 5 người lên xuồng an toàn. Phát nước uống và thực phẩm khẩn cấp.', 1, 5, DATEADD(HOUR, -35, GETDATE()));
SET IDENTITY_INSERT rescue_assignment_reports OFF;
GO

-- Insert Rescue Request Status History
SET IDENTITY_INSERT rescue_request_status_history ON;
INSERT INTO rescue_request_status_history (status_id, request_id, status, notes, updated_by, updated_at) VALUES
(1, 1, 'PENDING', N'Yêu cầu mới được tạo', 13, DATEADD(HOUR, -48, GETDATE())),
(2, 1, 'VERIFIED', N'Đã xác minh thông tin qua điện thoại', 2, DATEADD(HOUR, -46, GETDATE())),
(3, 1, 'ASSIGNED', N'Đã phân công đội Alpha', 2, DATEADD(HOUR, -42, GETDATE())),
(4, 1, 'IN_PROGRESS', N'Đội đang thực hiện cứu hộ', 5, DATEADD(HOUR, -40, GETDATE())),
(5, 1, 'COMPLETED', N'Hoàn thành, đã cứu 5 người an toàn', 5, DATEADD(HOUR, -36, GETDATE())),
(6, 2, 'PENDING', N'Yêu cầu mới', 14, DATEADD(HOUR, -36, GETDATE())),
(7, 2, 'VERIFIED', N'Đã xác minh', 2, DATEADD(HOUR, -34, GETDATE())),
(8, 2, 'ASSIGNED', N'Phân công đội Alpha', 2, DATEADD(HOUR, -32, GETDATE())),
(9, 2, 'IN_PROGRESS', N'Đang cứu hộ', 5, DATEADD(HOUR, -30, GETDATE())),
(10, 3, 'PENDING', N'Yêu cầu mới', 15, DATEADD(HOUR, -24, GETDATE())),
(11, 3, 'VERIFIED', N'Đã xác minh', 3, DATEADD(HOUR, -22, GETDATE())),
(12, 3, 'ASSIGNED', N'Phân công đội Beta', 3, DATEADD(HOUR, -20, GETDATE())),
(13, 4, 'PENDING', N'Yêu cầu mới', 16, DATEADD(HOUR, -20, GETDATE())),
(14, 4, 'VERIFIED', N'Đã xác minh qua ảnh', 2, DATEADD(HOUR, -18, GETDATE()));
SET IDENTITY_INSERT rescue_request_status_history OFF;
GO

-- Insert Item Categories
SET IDENTITY_INSERT item_categories ON;
INSERT INTO item_categories (category_id, category_name, description) VALUES
(1, N'FOOD', N'Thực phẩm và đồ ăn'),
(2, N'WATER', N'Nước uống'),
(3, N'MEDICINE', N'Thuốc men và vật tư y tế'),
(4, N'CLOTHING', N'Quần áo và đồ dùng cá nhân'),
(5, N'SHELTER', N'Lều bạt và dụng cụ trú ẩn');
SET IDENTITY_INSERT item_categories OFF;
GO

-- Insert Relief Items
SET IDENTITY_INSERT relief_items ON;
INSERT INTO relief_items (item_id, item_code, item_name, category_id, unit, current_stock, min_stock_level, unit_price) VALUES
(1, 'FOOD-001', N'Mì gói', 1, 'PACK', 5000, 1000, 3000.00),
(2, 'FOOD-002', N'Gạo', 1, 'KG', 10000, 2000, 20000.00),
(3, 'FOOD-003', N'Thịt hộp', 1, 'BOX', 3000, 500, 35000.00),
(4, 'FOOD-004', N'Sữa hộp', 1, 'BOX', 2000, 400, 25000.00),
(5, 'WATER-001', N'Nước khoáng chai', 2, 'LITER', 15000, 3000, 5000.00),
(6, 'WATER-002', N'Nước tinh khiết thùng', 2, 'BOX', 1000, 200, 80000.00),
(7, 'MED-001', N'Băng gạc y tế', 3, 'BOX', 500, 100, 50000.00),
(8, 'MED-002', N'Cồn sát trùng', 3, 'LITER', 300, 50, 60000.00),
(9, 'MED-003', N'Thuốc hạ sốt', 3, 'BOX', 400, 80, 45000.00),
(10, 'MED-004', N'Khẩu trang y tế', 3, 'BOX', 2000, 500, 150000.00),
(11, 'CLOTH-001', N'Áo mưa', 4, 'PIECE', 3000, 500, 15000.00),
(12, 'CLOTH-002', N'Chăn mỏng', 4, 'PIECE', 1500, 300, 80000.00),
(13, 'CLOTH-003', N'Áo phông', 4, 'PIECE', 2000, 400, 50000.00),
(14, 'SHELTER-001', N'Lều cứu trợ', 5, 'PIECE', 200, 50, 1500000.00),
(15, 'SHELTER-002', N'Bạt che mưa', 5, 'PIECE', 800, 150, 200000.00);
SET IDENTITY_INSERT relief_items OFF;
GO

-- Insert Inventory Transactions
SET IDENTITY_INSERT inventory_transactions ON;
INSERT INTO inventory_transactions (transaction_id, item_id, transaction_type, quantity, unit_price, reason, created_by, transaction_date) VALUES
(1, 1, 'IN', 5000, 3000.00, N'Nhập kho từ nhà cung cấp', 4, DATEADD(DAY, -10, GETDATE())),
(2, 2, 'IN', 10000, 20000.00, N'Nhập kho từ nhà cung cấp', 4, DATEADD(DAY, -10, GETDATE())),
(3, 5, 'IN', 15000, 5000.00, N'Nhập kho nước uống', 4, DATEADD(DAY, -9, GETDATE())),
(4, 1, 'OUT', -200, 3000.00, N'Xuất cứu trợ cho yêu cầu #1', 2, DATEADD(HOUR, -36, GETDATE())),
(5, 5, 'OUT', -100, 5000.00, N'Xuất cứu trợ cho yêu cầu #1', 2, DATEADD(HOUR, -36, GETDATE())),
(6, 3, 'IN', 3000, 35000.00, N'Nhập thịt hộp', 4, DATEADD(DAY, -8, GETDATE())),
(7, 11, 'IN', 3000, 15000.00, N'Nhập áo mưa', 4, DATEADD(DAY, -7, GETDATE())),
(8, 14, 'IN', 200, 1500000.00, N'Nhập lều cứu trợ', 4, DATEADD(DAY, -6, GETDATE()));
SET IDENTITY_INSERT inventory_transactions OFF;
GO

-- Insert Relief Distributions
SET IDENTITY_INSERT relief_distributions ON;
INSERT INTO relief_distributions (distribution_id, request_id, distributed_by, assignment_id, distribution_date, recipient_name, number_of_recipients, notes, is_confirmed, confirmed_at) VALUES
(1, 1, 5, 1, DATEADD(HOUR, -36, GETDATE()), N'Nguyễn Thị Lan', 5, N'Phát thực phẩm và nước uống khẩn cấp', 1, DATEADD(HOUR, -35, GETDATE())),
(2, NULL, 4, NULL, DATEADD(DAY, -2, GETDATE()), N'Trần Văn Bình', 10, N'Phát cứu trợ cho khu vực bị ngập', 1, DATEADD(DAY, -2, GETDATE()));
SET IDENTITY_INSERT relief_distributions OFF;
GO

-- Insert Relief Distribution Items
SET IDENTITY_INSERT relief_distribution_items ON;
INSERT INTO relief_distribution_items (distribution_item_id, distribution_id, item_id, quantity, notes) VALUES
(1, 1, 1, 50, N'Mì gói cho 5 người trong 2 ngày'),
(2, 1, 5, 30, N'Nước uống'),
(3, 1, 3, 10, N'Thịt hộp'),
(4, 1, 11, 5, N'Áo mưa'),
(5, 2, 2, 100, N'Gạo cho nhiều gia đình'),
(6, 2, 5, 200, N'Nước uống'),
(7, 2, 1, 150, N'Mì gói'),
(8, 2, 12, 20, N'Chăn mỏng');
SET IDENTITY_INSERT relief_distribution_items OFF;
GO

-- Insert Notifications
SET IDENTITY_INSERT notifications ON;
INSERT INTO notifications (notification_id, user_id, notification_type, title, message, is_read, related_entity_type, related_entity_id, created_at) VALUES
(1, 13, 'REQUEST_UPDATE', N'Yêu cầu của bạn đã được xử lý', N'Yêu cầu cứu hộ #1 đã được hoàn thành. Cảm ơn bạn đã tin tưởng.', 1, 'request', 1, DATEADD(HOUR, -36, GETDATE())),
(2, 5, 'ASSIGNMENT', N'Nhiệm vụ mới', N'Bạn được phân công nhiệm vụ cứu hộ #1', 1, 'assignment', 1, DATEADD(HOUR, -42, GETDATE())),