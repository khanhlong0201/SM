using System.ComponentModel.DataAnnotations;

namespace SM.Models;

public class CustomerModel : Auditable
{
    public string? CusNo { get; set; }

    [Required(ErrorMessage = "Vui lòng điền Tên khách hàng")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Vui lòng điền Số điện thoại")]
    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? NoteForAll { get; set; }
    public DateTime? DateOfBirth { get; set; }
}