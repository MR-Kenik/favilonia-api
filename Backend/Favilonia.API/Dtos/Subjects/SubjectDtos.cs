using System.ComponentModel.DataAnnotations;

namespace Favilonia.API.Dtos.Subjects;

public class CreateSubjectRequest
{
    [Required(ErrorMessage = "Название предмета обязательно.")]
    [MaxLength(200, ErrorMessage = "Название предмета не может быть длиннее 200 символов.")]
    public string Name { get; set; } = string.Empty;
}

public class UpdateSubjectRequest
{
    [Required(ErrorMessage = "Название предмета обязательно.")]
    [MaxLength(200, ErrorMessage = "Название предмета не может быть длиннее 200 символов.")]
    public string Name { get; set; } = string.Empty;
}

public class SubjectResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
