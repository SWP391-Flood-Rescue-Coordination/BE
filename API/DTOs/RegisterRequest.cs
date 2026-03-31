using System.ComponentModel.DataAnnotations;

namespace Flood_Rescue_Coordination.API.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "Ten dang nhap la bat buoc")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password la bat buoc")]
    [MinLength(5, ErrorMessage = "Password phai co it nhat 5 ky tu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ho ten la bat buoc")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "So dien thoai la bat buoc")]
    [Phone(ErrorMessage = "So dien thoai khong hop le")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email la bat buoc")]
    [EmailAddress(ErrorMessage = "Email khong hop le")]
    public string Email { get; set; } = string.Empty;
}
