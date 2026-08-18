using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.DTOs;

public class LoginResultDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool MustChangePassword { get; set; }
}
