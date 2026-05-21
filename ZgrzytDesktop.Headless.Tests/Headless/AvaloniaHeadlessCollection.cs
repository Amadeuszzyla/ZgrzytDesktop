namespace ZgrzytDesktop.Headless.Tests.Headless;

[CollectionDefinition(Name, DisableParallelization = true)]
public class AvaloniaHeadlessCollection : ICollectionFixture<AvaloniaHeadlessFixture>
{
    public const string Name = "AvaloniaHeadless";
}

public sealed class AvaloniaHeadlessFixture
{
    public AvaloniaHeadlessFixture() => AvaloniaHeadlessTestHost.EnsureInitialized();
}
