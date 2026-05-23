using System;
using System.Text.Json;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public static class AuditDisplayHelper
{
    public static string GetActionDisplay(string? action)
    {
        if (string.IsNullOrWhiteSpace(action))
            return string.Empty;

        return action.Trim() switch
        {
            "Login" => AppStrings.Get("Audit_Action_Login"),
            "Logout" => AppStrings.Get("Audit_Action_Logout"),
            "RequestAccount" => AppStrings.Get("Audit_Action_RequestAccount"),
            "RegisterUser" => AppStrings.Get("Audit_Action_RegisterUser"),
            "SettingsSaved" or "Zapis ustawień" => AppStrings.Get("Audit_Action_SettingsSaved"),
            "CreateTicket" => AppStrings.Get("Audit_Action_TicketCreated"),
            "UpdateTicket" => AppStrings.Get("Audit_Action_TicketUpdated"),
            "CloseTicket" => AppStrings.Get("Audit_Action_TicketClosed"),
            "DeleteTicket" => AppStrings.Get("Audit_Action_TicketDeleted"),
            "AssignToMe" => AppStrings.Get("Audit_Action_TicketAssigned"),
            "SendMessage" => AppStrings.Get("Audit_Action_MessageSent"),
            "BanUser" => AppStrings.Get("Audit_Action_UserBanned"),
            "ActivateUser" => AppStrings.Get("Audit_Action_UserActivated"),
            "UnbanUser" => AppStrings.Get("Audit_Action_UserUnbanned"),
            _ => action
        };
    }

    public static string GetDescriptionDisplay(AuditLogEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.DetailsKey))
        {
            var parameters = DeserializeParameters(entry.ParametersJson);

            return parameters.Length == 0
                ? AppStrings.Get(entry.DetailsKey)
                : AppStrings.GetFormat(entry.DetailsKey, parameters);
        }

        return entry.Description ?? string.Empty;
    }

    public static string FormatUserLogin(string? userLogin) =>
        string.IsNullOrWhiteSpace(userLogin)
            ? string.Empty
            : AppStrings.GetFormat("Audit_User", userLogin);

    public static string FormatTicketId(int? ticketId) =>
        ticketId is null or <= 0
            ? string.Empty
            : AppStrings.GetFormat("Audit_Ticket", ticketId.Value);

    private static object[] DeserializeParameters(string? parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<object[]>(parametersJson) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
