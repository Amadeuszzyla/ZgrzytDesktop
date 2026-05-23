using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Tests.Infrastructure;

public static class TestToastCallbacks
{
    public static ToastKeyCallback NoopKey => static (_, _, _) => { };

    public static Action<string, string> NoopRaw => static (_, _) => { };

    /// <summary>Resolves toast text at invocation time (mirrors dashboard ShowToastKey).</summary>
    public static ToastKeyCallback ResolveKeyTo(Action<string>? capture = null) =>
        (key, _, args) =>
        {
            var message = args.Length > 0
                ? AppStrings.GetFormat(key, args)
                : AppStrings.Get(key);
            capture?.Invoke(message);
        };
}
