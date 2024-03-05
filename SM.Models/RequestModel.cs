//using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace SM.Models;

public class RequestModel
{
    public int UserId { get; set; }
    public string? Json { get; set; }
    public string? Type { get; set; }
    public string? JsonDetail { get; set; }
    public string? VoucherNo { get; set; }
}

public class ResponseModel
{
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public ResponseModel()
    {
        StatusCode = -1;
        Message = string.Empty;
    }

    public ResponseModel(int status, string? message)
    {
        StatusCode = status;
        Message = message;
    }
}

public class ResponseModel<T>
{
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public T Data { get; set; }

    public ResponseModel()
    {
        StatusCode = 0;
        Message = string.Empty;
        Data = Activator.CreateInstance<T>(); //longtran Tạo một thể hiện
    }
}

public class ComboboxModel
{
    public string? Code { get; set; }
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsCheck{ get; set; }
}

public class SuppliesOutBoundModel
{
    public string? SuppliesCode { get; set; }
    public string? SuppliesName { get; set; }
    public decimal? Qty { get; set; }
    public decimal? QtyInv { get; set; }
    public string? EnumId { get; set; }
    public string? EnumName { get; set; }
    public int? BaseLine { get; set; }

}


public class SearchModel
{
    public int UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? StatusId { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CurDate { get; set; }
    public string? EmpNo { get; set; }
    public int SearchUserId { get; set; }
    public string? TypeTime { get; set; } // quí or tháng
    public string? Type { get; set; } // loại báo cáo ( doanh thu theo ldv hay doanh thu theo dv)
    public int Year { get; set; }
}

public enum EnumType
{
    @Add,
    @Update,
    @Delete,
    @SaveAndClose,
    @ChangePassWord,
    @Borrowed
}

public enum EnumTable
{
    @Users,
    @Customers,
    @Departments,
    @Products
}

public enum DocStatus
{
    @Pending,
    @ApprovalPending,
    @Closed,
    @All,
    @Cancled,
    @Borrowing
}

public enum TypeTime
{
    @Qui,
    @Thang
}

public enum ReportType
{
    @DoanhThuKhachHangMuaHangLienHe,
}

public enum Kind
{
    @QuiThang,
    @TuNgayDenNgay
}

public enum ServiceType
{
    @Purchase,
    @Contact
}


public enum ChartReportType
{
    @List,
    @Chart
}
