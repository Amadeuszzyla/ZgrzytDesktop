using System;

namespace ZgrzytDesktop.Security;

public static class AppRuntime
{
#if DEBUG
    public static bool IsDevelopmentMode => true;
#else
    public static bool IsDevelopmentMode =>
        string.Equals(Environment.GetEnvironmentVariable("ZGRZYT_DEV"), "1", StringComparison.Ordinal);
#endif

    public static bool AllowLocalHttpApi => IsDevelopmentMode;
}
