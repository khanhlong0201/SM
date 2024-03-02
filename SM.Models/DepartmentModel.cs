using System.ComponentModel.DataAnnotations;

namespace SM.Models;

public class DepartmentModel : Auditable
{
    public string? DepartmentName { get; set; }

    [Required(ErrorMessage = "Vui lòng điền Tên phòng ban")]

    public int DepartmentId { get; set; }

}