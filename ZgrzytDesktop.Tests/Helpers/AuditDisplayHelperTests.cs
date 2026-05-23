using ZgrzytDesktop.Helpers;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;
using ZgrzytDesktop.Tests.ViewModels;

namespace ZgrzytDesktop.Tests.Helpers;

public class AuditDisplayHelperTests
{
    public AuditDisplayHelperTests() => ViewModelTestSetup.EnsureAppStrings();

    [Fact]
    public void GetDescriptionDisplay_KeyedEntry_CultureEn_ShowsEnglish()
    {
        AppStrings.ApplyCulture("en");

        var entry = new AuditLogEntry
        {
            Action = "CreateTicket",
            DetailsKey = "Audit_Desc_TicketCreated",
            ParametersJson = """["Printer issue"]"""
        };

        Assert.Equal("Created ticket: Printer issue", entry.DisplayDescription);
        Assert.Equal("Ticket created", entry.DisplayAction);
    }

    [Fact]
    public void GetDescriptionDisplay_KeyedEntry_CulturePl_ShowsPolish()
    {
        AppStrings.ApplyCulture("pl");

        var entry = new AuditLogEntry
        {
            Action = "SettingsSaved",
            DetailsKey = "Audit_Desc_SettingsSaved"
        };

        Assert.Equal("Zmieniono ustawienia aplikacji.", entry.DisplayDescription);
        Assert.Equal("Zapis ustawień", entry.DisplayAction);
    }

    [Fact]
    public void GetDescriptionDisplay_LegacyDescription_IsReturnedUnchanged()
    {
        AppStrings.ApplyCulture("en");

        var entry = new AuditLogEntry
        {
            Action = "Login",
            Description = "Stary wpis po polsku bez klucza."
        };

        Assert.Equal("Stary wpis po polsku bez klucza.", entry.DisplayDescription);
    }

    [Fact]
    public void GetDescriptionDisplay_LegacyDescription_DoesNotThrow()
    {
        var entry = new AuditLogEntry
        {
            Action = "Unknown",
            Description = "Legacy only",
            ParametersJson = "{invalid"
        };

        Assert.Equal("Legacy only", AuditDisplayHelper.GetDescriptionDisplay(entry));
    }
}
