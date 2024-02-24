using System.ComponentModel.DataAnnotations;

namespace SM.Models;

public class LoginRequestModel
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public bool IsLogin { get; set; }
}
