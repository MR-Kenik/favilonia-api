using System.Collections.Generic;

namespace Favilonia.API.Authorization;

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";

    // SuperAdmin — роль владельца всей платформы Favilonia (не учреждения).
    // На текущем этапе пользователей с этой ролью нет — задел под панель управления платформой.
    public const string SuperAdmin = "SuperAdmin";

    public static readonly IReadOnlyCollection<string> All = new[] { Admin, User, SuperAdmin };
}
