using System;

namespace BadmintonManager.DTOs;

public record DailyRevenueDto(
    DateTime Date,
    decimal TotalRevenue,
    int BookingCount
);

public record MonthlyRevenueDto(
    int Month,
    int Year,
    decimal TotalRevenue,
    int BookingCount
);

public record CourtUtilizationDto(
    int CourtId,
    string CourtName,
    int BranchId,
    string BranchName,
    double UtilizationRate,
    double BookedMinutes,
    double TotalOpenMinutes
);

public record TopCustomerDto(
    int CustomerId,
    string CustomerName,
    string Phone,
    int TotalBookings,
    decimal TotalSpent
);

public record BranchRevenueComparisonDto(
    int BranchId,
    string BranchName,
    decimal TotalRevenue,
    int BookingCount
);

public record BranchOccupancyComparisonDto(
    int BranchId,
    string BranchName,
    double UtilizationRate,
    double BookedHours,
    double TotalOpenHours
);

public record ReportOverviewSummaryDto(
    decimal TotalRevenue,
    int TotalBookings,
    double TotalBookedHours,
    double AverageUtilizationRate
);
