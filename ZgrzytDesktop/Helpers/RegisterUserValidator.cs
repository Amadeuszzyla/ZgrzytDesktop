using System;
using ZgrzytDesktop.Models;
using ZgrzytDesktop.Resources;

namespace ZgrzytDesktop.Helpers;

public static class RegisterUserValidator
{
    public static string? Validate(
        string name,
        string login,
        string email,
        string password,
        string passwordConfirmation,
        RegisterUserRoleOption? selectedRole)
    {
        if (string.IsNullOrWhiteSpace(name))
            return AppStrings.Get("RequestAccount_ValidationName");

        if (string.IsNullOrWhiteSpace(login))
            return AppStrings.Get("Login_ProvideLogin");

        if (string.IsNullOrWhiteSpace(email))
            return AppStrings.Get("RequestAccount_ValidationEmail");

        if (string.IsNullOrWhiteSpace(password))
            return AppStrings.Get("RequestAccount_ValidationPassword");

        if (string.IsNullOrWhiteSpace(passwordConfirmation))
            return AppStrings.Get("RequestAccount_ValidationPasswordConfirm");

        if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
            return AppStrings.Get("RequestAccount_ValidationPasswordMismatch");

        if (selectedRole is null || !RegisterUserRoleOption.IsAllowedRole(selectedRole.Role))
            return AppStrings.Get("RegisterUser_ValidationRole");

        return null;
    }

    public static RegisterUserRequest BuildRequest(
        string name,
        string login,
        string email,
        string password,
        string passwordConfirmation,
        RegisterUserRoleOption selectedRole) =>
        new()
        {
            Name = name.Trim(),
            Login = login.Trim(),
            Email = email.Trim(),
            Password = password,
            PasswordConfirmation = passwordConfirmation,
            Role = selectedRole.Role
        };
}
