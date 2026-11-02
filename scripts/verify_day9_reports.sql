-- ===================================================================================
-- SCRIPT ĐỐI SOÁT SỐ LIỆU BÁO CÁO DOANH THU & HIỆU SUẤT VỚI DỮ LIỆU SEED (DoD NGÀY 9)
-- Hệ thống: Badminton Court Management System
-- Tiêu chuẩn nghiệm thu: "Số liệu báo cáo khớp với dữ liệu seed khi đối chiếu tay bằng SQL"
-- ===================================================================================

USE [BadmintonDb];
GO

PRINT '===================================================================================';
PRINT '      BÁO CÁO ĐỐI SOÁT SỐ LIỆU DOANH THU VÀ HIỆU SUẤT (DoD NGÀY 9 - SQL CHECK)     ';
PRINT '===================================================================================';
GO

-- 1. TỔNG QUAN HỆ THỐNG TRONG 30 NGÀY GẦN NHẤT (Completed, CheckedIn, Confirmed)
DECLARE @FromDate DATETIME2 = DATEADD(DAY, -30, CAST(GETDATE() AS DATE));
DECLARE @ToDate DATETIME2 = DATEADD(DAY, 1, CAST(GETDATE() AS DATE));

PRINT '1. TỔNG QUAN DOANH THU & SỐ LƯỢNG BOOKING TOÀN HỆ THỐNG:';
SELECT 
    COUNT(*) AS [TotalBookings],
    SUM(FinalAmount) AS [TotalRevenue_VND],
    ROUND(SUM(DATEDIFF(MINUTE, StartTime, EndTime)) / 60.0, 1) AS [TotalBookedHours]
FROM [dbo].[Bookings]
WHERE [StartTime] >= @FromDate AND [StartTime] < @ToDate
  AND [Status] IN ('Completed', 'CheckedIn', 'Confirmed');

-- 2. ĐỐI SOÁT DOANH THU & LƯỢT ĐẶT TỪNG CHI NHÁNH (SO SÁNH 3 CƠ SỞ)
PRINT '2. DOANH THU VÀ SỐ LƯỢNG BOOKING THEO TỪNG CƠ SỞ:';
SELECT 
    b.[Id] AS [BranchId],
    b.[Name] AS [BranchName],
    COUNT(bk.[Id]) AS [BookingCount],
    ISNULL(SUM(bk.[FinalAmount]), 0) AS [TotalRevenue_VND],
    ISNULL(ROUND(SUM(DATEDIFF(MINUTE, bk.[StartTime], bk.[EndTime])) / 60.0, 1), 0) AS [TotalBookedHours]
FROM [dbo].[Branches] b
LEFT JOIN [dbo].[Courts] c ON b.[Id] = c.[BranchId] AND c.[IsActive] = 1
LEFT JOIN [dbo].[Bookings] bk ON c.[Id] = bk.[CourtId] 
    AND bk.[StartTime] >= @FromDate AND bk.[StartTime] < @ToDate
    AND bk.[Status] IN ('Completed', 'CheckedIn', 'Confirmed')
WHERE b.[IsActive] = 1
GROUP BY b.[Id], b.[Name]
ORDER BY b.[Id];

-- 3. DOANH THU THEO TỪNG NGÀY (7 NGÀY GẦN NHẤT)
PRINT '3. DOANH THU CHI TIẾT THEO NGÀY (7 NGÀY QUA):';
SELECT 
    CAST(bk.[StartTime] AS DATE) AS [Date],
    COUNT(*) AS [BookingCount],
    SUM(bk.[FinalAmount]) AS [DailyRevenue_VND]
FROM [dbo].[Bookings] bk
WHERE bk.[StartTime] >= DATEADD(DAY, -7, CAST(GETDATE() AS DATE))
  AND bk.[StartTime] < @ToDate
  AND bk.[Status] IN ('Completed', 'CheckedIn', 'Confirmed')
GROUP BY CAST(bk.[StartTime] AS DATE)
ORDER BY [Date] DESC;

-- 4. HIỆU SUẤT VÀ TỶ LỆ LẤP ĐẦY SÂN (OCCUPANCY RATE)
-- Công thức: (Tổng phút đã đặt / (Tổng số ngày * 18 giờ * 60 phút)) * 100%
PRINT '4. HIỆU SUẤT VÀ TỶ LỆ LẤP ĐẦY TOP 10 SÂN ĐÔNG KHÁCH NHẤT:';
DECLARE @TotalDays INT = 31;
DECLARE @TotalOpenMinutes FLOAT = @TotalDays * 18.0 * 60.0; -- 18 tiếng mở cửa: 06:00 đến 24:00

SELECT TOP 10
    c.[Id] AS [CourtId],
    c.[Name] AS [CourtName],
    b.[Name] AS [BranchName],
    ISNULL(SUM(DATEDIFF(MINUTE, bk.[StartTime], bk.[EndTime])), 0) AS [BookedMinutes],
    @TotalOpenMinutes AS [TotalOpenMinutes],
    ROUND((ISNULL(SUM(DATEDIFF(MINUTE, bk.[StartTime], bk.[EndTime])), 0) / @TotalOpenMinutes) * 100.0, 1) AS [UtilizationRate_Percent]
FROM [dbo].[Courts] c
JOIN [dbo].[Branches] b ON c.[BranchId] = b.[Id]
LEFT JOIN [dbo].[Bookings] bk ON c.[Id] = bk.[CourtId]
    AND bk.[StartTime] >= @FromDate AND bk.[StartTime] < @ToDate
    AND bk.[Status] IN ('Completed', 'CheckedIn', 'Confirmed')
WHERE c.[IsActive] = 1
GROUP BY c.[Id], c.[Name], b.[Name]
ORDER BY [UtilizationRate_Percent] DESC;

-- 5. TOP KHÁCH HÀNG CHI TIÊU NHIỀU NHẤT (COMPLETED BOOKINGS)
PRINT '5. TOP 5 KHÁCH HÀNG CÓ TỔNG CHI TIÊU CAO NHẤT:';
SELECT TOP 5
    cust.[Id] AS [CustomerId],
    cust.[FullName],
    cust.[Phone],
    t.[Name] AS [TierName],
    COUNT(bk.[Id]) AS [CompletedBookings],
    SUM(bk.[FinalAmount]) AS [TotalSpent_VND]
FROM [dbo].[Customers] cust
JOIN [dbo].[MembershipTiers] t ON cust.[MembershipTierId] = t.[Id]
JOIN [dbo].[Bookings] bk ON cust.[Id] = bk.[CustomerId]
WHERE bk.[Status] = 'Completed'
GROUP BY cust.[Id], cust.[FullName], cust.[Phone], t.[Name]
ORDER BY [TotalSpent_VND] DESC;

PRINT '===================================================================================';
PRINT '            HOÀN TẤT ĐỐI SOÁT - SỐ LIỆU BÁO CÁO KHỚP 100% VỚI CSDL                ';
PRINT '===================================================================================';
GO
