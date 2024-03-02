using System.ComponentModel.DataAnnotations;

namespace SM.Models;

public class ProductModel : Auditable
{
    public string? ProductName { get; set; }

    [Required(ErrorMessage = "Vui lòng điền Tên sản phẩm")]
    public int ProductId { get; set; }
   
    public string Description { get; set; }
}