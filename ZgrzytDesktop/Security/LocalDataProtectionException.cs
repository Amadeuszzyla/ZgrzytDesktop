using System;

namespace ZgrzytDesktop.Security;

public sealed class LocalDataProtectionException : Exception
{
    public LocalDataProtectionException(string message)
        : base(message)
    {
    }

    public LocalDataProtectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
