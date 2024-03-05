using System.ComponentModel.DataAnnotations;

namespace SM.Models;

public class ReportModel : Auditable
{

    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string KindName { get; set; }
    public string DepartmentName { get; set; }
    public int QtyCusContact { get; set; }//số lượng khách hàng liên hệ
    public int QtyCusPurchased { get; set; } // số lượng khách hàng đã mua hàng
    public List<object> ListValue { get; set; }
    public string[] ListTitle { get; set; }
    public string? Title { get; set; }
    public double Total_01 { get; set; }
    public double Total_02 { get; set; }
    public double Total_03 { get; set; }
    public double Total_04 { get; set; }
    public double Total_05 { get; set; }
    public double Total_06 { get; set; }
    public double Total_07 { get; set; }
    public double Total_08 { get; set; }
    public double Total_09 { get; set; }
    public double Total_10 { get; set; }
    public double Total_11 { get; set; }
    public double Total_12 { get; set; }
    public double LineTotal { get; set; }
    public string? Color_01 { get; set; }
    public string? Color_02 { get; set; }
    public string? Time { get; set; }
    public int UserId { get; set; }

}