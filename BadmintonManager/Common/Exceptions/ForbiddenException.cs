using System;

namespace BadmintonManager.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Bạn không có quyền thực hiện thao tác này.") : base(message)
    {
    }
}
