-- ============================================================================
-- HỆ THỐNG QUẢN LÝ SÂN CẦU LÔNG - BADMINTON COURT MANAGEMENT
-- KỊCH BẢN KIỂM TRA ĐẠT TIÊU CHÍ HOÀN THÀNH NGÀY 1 (DoD - Definition of Done)
-- Mục đích: Mở và chạy trong SSMS để kiểm tra toàn bộ dữ liệu đã được seed
-- ============================================================================

USE [BadmintonDb];
GO

PRINT '============================================================================';
PRINT '         BÁO CÁO KIỂM TRA TỔNG QUAN TIÊU CHÍ NGÀY 1 (DoD)';
PRINT '============================================================================';

-- 1. Bảng tổng hợp số lượng bản ghi
SELECT 
    '1. Cơ sở (Branches)' AS [Hạng Mục], 
    COUNT(*) AS [Số Lượng Hiện Tại], 
    3 AS [Yêu Cầu Tối Thiểu],
    CASE WHEN COUNT(*) = 3 THEN 'PASS' ELSE 'FAIL' END AS [Trạng Thái]
FROM [dbo].[Branches]
UNION ALL
SELECT 
    '2. Sân cầu lông (Courts)', 
    COUNT(*), 
    18,
    CASE WHEN COUNT(*) = 18 THEN 'PASS' ELSE 'FAIL' END
FROM [dbo].[Courts]
UNION ALL
SELECT 
    '3. Tài khoản (AppUsers)', 
    COUNT(*), 
    10,
    CASE WHEN COUNT(*) >= 10 THEN 'PASS' ELSE 'FAIL' END
FROM [dbo].[AppUsers]
UNION ALL
SELECT 
    '4. Khách hàng (Customers)', 
    COUNT(*), 
    3,
    CASE WHEN COUNT(*) >= 3 THEN 'PASS' ELSE 'FAIL' END
FROM [dbo].[Customers]
UNION ALL
SELECT 
    '5. Hạng hội viên (MembershipTiers)', 
    COUNT(*), 
    3,
    CASE WHEN COUNT(*) = 3 THEN 'PASS' ELSE 'FAIL' END
FROM [dbo].[MembershipTiers]
UNION ALL
SELECT 
    '6. Khung giá gốc (PriceRules)', 
    COUNT(*), 
    6,
    CASE WHEN COUNT(*) >= 6 THEN 'PASS' ELSE 'FAIL' END
FROM [dbo].[PriceRules]
UNION ALL
SELECT 
    '7. Cấu hình hệ thống (SystemSettings)', 
    COUNT(*), 
    10,
    CASE WHEN COUNT(*) = 10 THEN 'PASS' ELSE 'FAIL' END
FROM [dbo].[SystemSettings]
UNION ALL
SELECT 
    '8. Đơn đặt sân mẫu (Bookings)', 
    COUNT(*), 
    50,
    CASE WHEN COUNT(*) >= 50 THEN 'PASS' ELSE 'FAIL' END
FROM [dbo].[Bookings]
UNION ALL
SELECT 
    '9. Giao dịch thanh toán (Payments)', 
    COUNT(*), 
    50,
    CASE WHEN COUNT(*) >= 50 THEN 'PASS' ELSE 'FAIL' END
FROM [dbo].[Payments];

-- 2. Chi tiết phân bố sân theo cơ sở (Mỗi cơ sở đúng 6 sân)
PRINT '';
PRINT '--- CHI TIẾT SÂN THEO CƠ SỞ (Yêu cầu: Mỗi cơ sở đúng 6 sân) ---';
SELECT 
    b.Id AS [Mã Cơ Sở],
    b.Name AS [Tên Cơ Sở],
    b.Phone AS [Hotline],
    COUNT(c.Id) AS [Số Sân Trực Thuộc],
    CASE WHEN COUNT(c.Id) = 6 THEN 'PASS' ELSE 'FAIL' END AS [Đạt Chuẩn]
FROM [dbo].[Branches] b
LEFT JOIN [dbo].[Courts] c ON b.Id = c.BranchId
GROUP BY b.Id, b.Name, b.Phone;

-- 3. Chi tiết tài khoản và ràng buộc BranchId (Section 4.1)
PRINT '';
PRINT '--- CHI TIẾT TÀI KHOẢN VÀ PHÂN QUYỀN (Ràng buộc BranchId) ---';
SELECT 
    u.Id,
    u.Username AS [Tên Đăng Nhập],
    u.FullName AS [Họ Và Tên],
    u.Role AS [Vai Trò],
    u.BranchId AS [Cơ Sở ID],
    b.Name AS [Cơ Sở Trực Thuộc],
    CASE 
        WHEN u.Role IN ('Manager', 'Staff') AND u.BranchId IS NOT NULL THEN 'PASS (Có BranchId)'
        WHEN u.Role IN ('Admin', 'Customer') AND u.BranchId IS NULL THEN 'PASS (BranchId Null)'
        ELSE 'FAIL (Sai ràng buộc)'
    END AS [Kiểm Tra Ràng Buộc BranchId]
FROM [dbo].[AppUsers] u
LEFT JOIN [dbo].[Branches] b ON u.BranchId = b.Id
ORDER BY u.Role, u.Id;

-- 4. Chi tiết bảng giá và kiểm tra phủ kín 06:00 - 24:00 (Section 4.1 & BR-10)
PRINT '';
PRINT '--- BẢNG GIÁ THUÊ SÂN (06:00 = 360p -> 24:00 = 1440p) ---';
SELECT 
    Id,
    DayType AS [Loại Ngày],
    CONVERT(VARCHAR(5), DATEADD(MINUTE, StartMinute, '00:00'), 108) + ' (' + CAST(StartMinute AS VARCHAR) + 'p)' AS [Giờ Bắt Đầu],
    CONVERT(VARCHAR(5), DATEADD(MINUTE, EndMinute, '00:00'), 108) + ' (' + CAST(EndMinute AS VARCHAR) + 'p)' AS [Giờ Kết Thúc],
    PricePerHour AS [Giá/Giờ (VND)],
    IsActive AS [Kích Hoạt]
FROM [dbo].[PriceRules]
ORDER BY DayType, StartMinute;

-- 5. Chi tiết cấu hình hệ thống
PRINT '';
PRINT '--- CẤU HÌNH HỆ THỐNG SYSTEM SETTING ---';
SELECT [Key] AS [Khóa Cấu Hình], [Value] AS [Giá Trị Thiết Lập]
FROM [dbo].[SystemSettings]
ORDER BY [Key];
GO
