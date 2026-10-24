using NUnit.Framework;

[SetUpFixture]
public sealed class WinFormsTestEnvironment
{
    [OneTimeSetUp]
    public void ConfigureUnhandledExceptionMode()
    {
        MyDmsVn.BootstrapSourceGrid.Tests.WinFormsTestGuard.Configure();
    }
}
