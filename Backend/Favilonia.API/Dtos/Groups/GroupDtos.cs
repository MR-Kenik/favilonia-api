using System.ComponentModel.DataAnnotations;

namespace Favilonia.API.Dtos.Groups;

public class CreateGroupRequest
{
    [Required(ErrorMessage = "Название группы обязательно.")]
    [MaxLength(100, ErrorMessage = "Название группы не может быть длиннее 100 символов.")]
    public string Name { get; set; } = string.Empty;
}

public class UpdateGroupRequest
{
    [Required(ErrorMessage = "Название группы обязательно.")]
    [MaxLength(100, ErrorMessage = "Название группы не может быть длиннее 100 символов.")]
    public string Name { get; set; } = string.Empty;
}

public class GroupResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GroupStudentResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
