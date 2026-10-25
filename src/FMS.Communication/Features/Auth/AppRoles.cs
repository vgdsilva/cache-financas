namespace FMS.Communication.Features.Auth;

/// <summary>Roles da aplicação, enviadas no claim "role" do JWT.</summary>
public static class AppRoles
{
    public const string ClaimType = "role";

    public const string Admin = "Admin";
    public const string User  = "User";

    public static string For(bool isAdmin) => isAdmin ? Admin : User;
}
