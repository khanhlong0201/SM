using Newtonsoft.Json;
using SM.API.Commons;
using SM.API.Infrastructure;
using SM.Models;
using SM.Models.Shared;
using System.Data;
using System.Data.SqlClient;
using System.Net;

namespace SM.API.Services;

public interface IMasterDataService
{
    Task<IEnumerable<UserModel>> GetUsersAsync(int pUserId = -1);

    Task<ResponseModel> UpdateUsers(RequestModel pRequest);

    Task<IEnumerable<UserModel>> Login(LoginRequestModel pRequest);

    Task<ResponseModel> DeleteDataAsync(RequestModel pRequest);

    Task<ResponseModel> UpdateCustomerAsync(RequestModel pRequest);

    Task<IEnumerable<CustomerModel>> GetCustomersAsync(SearchModel pSearch);
    Task<IEnumerable<DepartmentModel>> GetDepartmentsAsync(int pUserId = -1);

    Task<ResponseModel> UpdateDepartments(RequestModel pRequest);
    Task<IEnumerable<ProductModel>> GetProductsAsync(int pUserId = -1);

    Task<ResponseModel> UpdateProducts(RequestModel pRequest);
    Task<IEnumerable<ReportModel>>GetIndexsAsync(SearchModel search);
    Task<IEnumerable<ReportModel>> GetReportsAsync(SearchModel search);
}

public class MasterDataService : IMasterDataService
{
    private readonly ISMDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _HOST_API;

    public MasterDataService(ISMDbContext context, IDateTimeService dateTimeService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _httpContextAccessor = httpContextAccessor;
        var requestContext = _httpContextAccessor!.HttpContext?.Request;
        string url = string.Empty;
        if (requestContext != null)
        {
            url = @$"{requestContext.Scheme}://{requestContext.Host.Value}";
        }
        _HOST_API = url;
    }

    #region Public Funtions

    /// <summary>
    /// Thêm mới/Cập nhật thông tin người dùng
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateUsers(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            UserModel oUser = JsonConvert.DeserializeObject<UserModel>(pRequest.Json + "")!;
            SqlParameter[] sqlParameters;
            async Task ExecQuery()
            {
                var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
                if (data != null && data.Rows.Count > 0)
                {
                    response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                    response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
                }
            }
            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter("@UserName", oUser.UserName);
                    // kiểm tra tên đăng nhập
                    if (await _context.ExecuteScalarAsync("select COUNT(*) from Users with(nolock) where UserName = @UserName", sqlParameters) > 0)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        response.Message = "Tên đăng nhập đã tồn tại!";
                        break;
                    }
                    oUser.EmpNo = await getVoucherNo(nameof(EnumTable.Users));
                    if (string.IsNullOrWhiteSpace(oUser.EmpNo))
                    {
                        response.StatusCode = (int)HttpStatusCode.NoContent;
                        response.Message = "Không đánh được mã nhân viên!. Vui lòng kiểm tra lại";
                        break;
                    }
                    queryString = @"INSERT INTO [dbo].[Users]([EmpNo],[UserName],[Password],[LastPassword],[FullName],[PhoneNumber] ,[Email] ,[Address],[DateOfBirth],[IsAdmin],[DateCreate] ,[UserCreate],[Isdelete], DepartmentId)
                                                        values (@EmpNo, @UserName , @Password , @LastPassword, @FullName, @PhoneNumber , @Email, @Address, @DateOfBirth, @IsAdmin, @DateTimeNow, @UserId, 0, @DepartmentId)";

                    string sPassword = EncryptHelper.Encrypt(oUser.Password + "");
                    sqlParameters = new SqlParameter[13];
                    sqlParameters[0] = new SqlParameter("@UserName", oUser.UserName);
                    sqlParameters[1] = new SqlParameter("@Password", sPassword);
                    sqlParameters[2] = new SqlParameter("@LastPassword", sPassword);
                    sqlParameters[3] = new SqlParameter("@FullName", oUser.FullName);
                    sqlParameters[4] = new SqlParameter("@PhoneNumber", oUser.PhoneNumber ?? (object)DBNull.Value);
                    sqlParameters[5] = new SqlParameter("@Email", oUser.Email ?? (object)DBNull.Value);
                    sqlParameters[6] = new SqlParameter("@Address", oUser.Address ?? (object)DBNull.Value);
                    sqlParameters[7] = new SqlParameter("@DateOfBirth", oUser.DateOfBirth ?? (object)DBNull.Value);
                    sqlParameters[8] = new SqlParameter("@IsAdmin", oUser.IsAdmin);
                    sqlParameters[9] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[10] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[11] = new SqlParameter("@EmpNo", oUser.EmpNo);
                    sqlParameters[12] = new SqlParameter("@DepartmentId", oUser.DepartmentId);
                    await ExecQuery();
                    break;

                case nameof(EnumType.Update):
                    queryString = @"UPDATE [dbo].[Users]
                                   SET [UserName] = @UserName
                                      ,[FullName] = @FullName
                                      ,[PhoneNumber] = @PhoneNumber
                                      ,[Email] = @Email
                                      ,[Address] = @Address
                                      ,[DateOfBirth] = @DateOfBirth
                                      ,[IsAdmin] = @IsAdmin
                                      ,[DateUpdate] = @DateTimeNow
                                      ,[UserUpdate] = @UserId
                                      ,[DepartmentId] = @DepartmentId
                                 WHERE [Id] = @Id";

                    sqlParameters = new SqlParameter[11];
                    sqlParameters[0] = new SqlParameter("@Id", oUser.Id);
                    sqlParameters[1] = new SqlParameter("@FullName", oUser.FullName ?? (object)DBNull.Value);
                    sqlParameters[2] = new SqlParameter("@PhoneNumber", oUser.PhoneNumber ?? (object)DBNull.Value);
                    sqlParameters[3] = new SqlParameter("@Email", oUser.Email ?? (object)DBNull.Value);
                    sqlParameters[4] = new SqlParameter("@Address", oUser.Address ?? (object)DBNull.Value);
                    sqlParameters[5] = new SqlParameter("@DateOfBirth", oUser.DateOfBirth ?? (object)DBNull.Value);
                    sqlParameters[6] = new SqlParameter("@IsAdmin", oUser.IsAdmin);
                    sqlParameters[7] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[8] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[9] = new SqlParameter("@UserName", oUser.UserName ?? (object)DBNull.Value);
                    sqlParameters[10] = new SqlParameter("@DepartmentId", oUser.DepartmentId);
                    await ExecQuery();
                    break;

                case nameof(EnumType.@ChangePassWord):
                    queryString = @"Update [dbo].[Users]
                                    set Password = @PasswordNew, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                    where [Id] = @Id";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@Id", oUser.Id);
                    sqlParameters[1] = new SqlParameter("@PasswordNew", EncryptHelper.Encrypt(oUser.PasswordNew + ""));
                    sqlParameters[2] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[3] = new SqlParameter("@UserId", pRequest.UserId);
                    await ExecQuery();
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Không xác định được phương thức!";
                    break;
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = ex.Message;
        }
        finally
        {
            await _context.DisConnect();
        }
        return response;
    }

    /// <summary>
    /// lấy danh sách nhân viên
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<UserModel>> GetUsersAsync(int pUserid = -1)
    {
        IEnumerable<UserModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@UserId", pUserid);
            data = await _context.GetDataAsync(@$"SELECT t0.[Id]
                                                ,t0.[EmpNo]
                                                ,t0.[UserName]
                                                ,t0.[Password]
                                                ,t0.[LastPassword]
                                                ,t0.[FullName]
                                                ,t0.[PhoneNumber]
                                                ,t0.[Email]
                                                ,t0.[Address]
                                                ,t0.[DateOfBirth]
                                                ,t0.[IsAdmin]
                                                ,t0.[DateCreate]
                                                ,t0.[UserCreate]
                                                ,t0.[DateUpdate]
                                                ,t0.[UserUpdate]
	                                            ,t1.DepartmentId
	                                            ,t1.DepartmentName
                                            FROM [dbo].[Users] t0 with(nolock) 
                                            left join [dbo].[Departments] t1 with(nolock) on t0.DepartmentId = t1.DepartmentId
                                            where ISNULL(t0.IsDelete,0) = 0" // không lấy lên tk Support
                    , DataRecordToUserModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    /// <param name="pBranchId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<UserModel>> Login(LoginRequestModel pRequest)
    {
        IEnumerable<UserModel> data;
        SqlParameter[] sqlParameters;
        string queryString = "";
        try
        {
            await _context.Connect();
            queryString = @"SELECT [Id]
                              ,[UserName]
                              ,[FullName]
                              ,[IsAdmin]
                              ,[EmpNo]
                              ,[Isdelete] as Isdeleted
                          FROM [dbo].[Users] t0 where ISNULL(t0.IsDelete,0) = 0
                          and t0.UserName = @UserName and t0.Password = @Password";
            //setParameter();
            sqlParameters = new SqlParameter[2];
            sqlParameters[0] = new SqlParameter("@UserName", pRequest.UserName);
            sqlParameters[1] = new SqlParameter("@Password", pRequest.Password);
            data = await _context.GetDataAsync(queryString, DataRecordToUserModelByLogin, sqlParameters, CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// lấy danh sách Khách hàng theo chi nhánh
    /// </summary>
    /// <param name="pBranchId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<CustomerModel>> GetCustomersAsync(SearchModel pSearch)
    {
        IEnumerable<CustomerModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@UserId", pSearch.UserId);
            data = await _context.GetDataAsync(@$"select [CusNo], T0.[FullName], T0.[PhoneNumber], T0.[Email], T0.[Address], T0.[DateOfBirth]
                      ,T0.[NoteForAll],T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate]
					  ,T1.FullName as [UserNameCreate], T2.FullName as [UserNameUpdate]
					  ,T3.ProductId 
					  ,T3.ProductName
                      ,t3.Description
					  ,T0.CustomerSource
			     from [dbo].[Customers] as T0 with(nolock)
		    left join [dbo].[Users] as T1 with(nolock) on T0.UserCreate = T1.Id
			left join [dbo].[Users] as T2 with(nolock) on T0.UserUpdate = T2.Id
			left join [dbo].[Products] as T3 with(nolock) on T0.ProductId = T3.ProductId
					where T0.[IsDelete] = 0
                    and isnull(@UserId,-1)=-1 or t0.UserCreate = @UserId order by [CusNo] desc"
                    , DataRecordToCustomerModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// cập nhật thông tin khách hàng
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateCustomerAsync(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            CustomerModel oCustomer = JsonConvert.DeserializeObject<CustomerModel>(pRequest.Json + "")!;
            SqlParameter[] sqlParameters;
            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    oCustomer.CusNo = await getVoucherNo(nameof(EnumTable.@Customers), oCustomer.FullName);
                    if (string.IsNullOrWhiteSpace(oCustomer.CusNo))
                    {
                        response.StatusCode = (int)HttpStatusCode.NoContent;
                        response.Message = "Không đánh được mã Khách hàng!. Vui lòng kiểm tra lại";
                        return response;
                    }
                    sqlParameters = getCustomerParams(oCustomer, pRequest.UserId);
                    queryString = @"Insert into [dbo].[Customers] ([CusNo],[FullName],[PhoneNumber],[Email]
                                    ,[Address],[DateOfBirth],[NoteForAll],[DateCreate],[UserCreate],[IsDelete],ProductId, CustomerSource )
                                    values (@CusNo, @FullName, @PhoneNumber, @Email
                                    ,@Address, @DateOfBirth, @NoteForAll, @DateTimeNow, @UserId, 0, @ProductId, @CustomerSource)";
                    break;

                case nameof(EnumType.Update):
                    // Kiểm tra dữ liệu trước khi cập nhật
                    queryString = "select count(*) from [dbo].[Customers] as T0 with(nolock) where [CusNo] = @CusNo and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter("@CusNo", oCustomer.CusNo);
                    int count = await _context.ExecuteScalarAsync(queryString, sqlParameters);
                    if(count <=0)
                    {
                        response.StatusCode = (int)HttpStatusCode.NoContent;
                        response.Message = "Không tìm thấy thông tin khách hàng. Vui lòng kiểm tra lại!";
                        return response;
                    }    
                    queryString = @"Update [dbo].[Customers]
                                       set [FullName] = @FullName , [PhoneNumber] = @PhoneNumber, [Email] = @Email
                                         , [NoteForAll] = @NoteForAll, [Address] = @Address , [DateOfBirth] = @DateOfBirth
                                         , [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                         , [ProductId] = @ProductId
                                         , CustomerSource = @CustomerSource
                                     where [CusNo] = @CusNo";
                    sqlParameters = getCustomerParams(oCustomer, pRequest.UserId);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Không xác định được phương thức!";
                    return response;
            }
            var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
            if (data != null && data.Rows.Count > 0)
            {
                response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = ex.Message;
        }
        finally
        {
            await _context.DisConnect();
        }
        return response;
    }

    /// <summary>
    /// xóa thông tin trong bảng
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> DeleteDataAsync(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        ResponseModel responseCheck = new ResponseModel();
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters;
            string queryString = "";
            switch (pRequest.Type)
            {
                case nameof(EnumTable.Users):
                    // kiểm tra điều kiện trước khi xóa
                    //
                    queryString = "[Id] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
                    sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());

                    //responseCheck = await CheckKeyBindingBeforeDeleting(pRequest);
                    //if (responseCheck != null && responseCheck.StatusCode == -1)
                    //{
                    //    response.StatusCode = -1;
                    //    response.Message = responseCheck.Message;
                    //    return response;
                    //}
                    response = await deleteDataAsync(nameof(EnumTable.Users), queryString, sqlParameters);
                    break;
                case nameof(EnumTable.@Customers):
                    // kiểm tra điều kiện trước khi xóa
                    //
                    queryString = "[CusNo] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
                    sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());

                    //responseCheck = await CheckKeyBindingBeforeDeleting(pRequest);
                    //if (responseCheck != null && responseCheck.StatusCode == -1)
                    //{
                    //    response.StatusCode = -1;
                    //    response.Message = responseCheck.Message;
                    //    return response;
                    //}
                    response = await deleteDataAsync(nameof(EnumTable.@Customers), queryString, sqlParameters);
                    break;
                case nameof(EnumTable.Departments):
                    // kiểm tra điều kiện trước khi xóa
                    //
                    queryString = "[Id] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
                    sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());

                    //responseCheck = await CheckKeyBindingBeforeDeleting(pRequest);
                    //if (responseCheck != null && responseCheck.StatusCode == -1)
                    //{
                    //    response.StatusCode = -1;
                    //    response.Message = responseCheck.Message;
                    //    return response;
                    //}
                    response = await deleteDataAsync(nameof(EnumTable.Departments), queryString, sqlParameters);
                    break;
                case nameof(EnumTable.Products):
                    // kiểm tra điều kiện trước khi xóa
                    //
                    queryString = "[Id] in ( select value from STRING_SPLIT(@ListIds, ',') ) and [IsDelete] = 0";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ReasonDelete", pRequest.JsonDetail ?? (object)DBNull.Value);
                    sqlParameters[1] = new SqlParameter("@ListIds", pRequest.Json); // "1,2,3,4"
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());

                    //responseCheck = await CheckKeyBindingBeforeDeleting(pRequest);
                    //if (responseCheck != null && responseCheck.StatusCode == -1)
                    //{
                    //    response.StatusCode = -1;
                    //    response.Message = responseCheck.Message;
                    //    return response;
                    //}
                    response = await deleteDataAsync(nameof(EnumTable.Products), queryString, sqlParameters);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Không xác định được phương thức!";
                    break;
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = ex.Message;
            await _context.RollbackAsync();
        }
        finally
        {
            await _context.DisConnect();
        }
        return response;
    }

    /// <summary>
    /// Thêm mới/Cập nhật thông tin sản phẩm
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateProducts(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            ProductModel oProduct = JsonConvert.DeserializeObject<ProductModel>(pRequest.Json + "")!;
            SqlParameter[] sqlParameters;
            async Task ExecQuery()
            {
                var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
                if (data != null && data.Rows.Count > 0)
                {
                    response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                    response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
                }
            }
            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter("@ProductName", oProduct.ProductName);
                    // kiểm tra tên đăng nhập
                    if (await _context.ExecuteScalarAsync("select COUNT(*) from Products with(nolock) where ProductName = @ProductName", sqlParameters) > 0)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        response.Message = "Tên sản phẩm đã tồn tại!";
                        break;
                    }
                    queryString = @"INSERT INTO [dbo].[Products]([ProductName],[Description] ,[DateCreate]  ,[UserCreate],[IsDelete])
                                                        values (@ProductName, @Description , @DateTimeNow, @UserId, 0)";
                    sqlParameters = new SqlParameter[4];
                    sqlParameters[0] = new SqlParameter("@ProductName", oProduct.ProductName);
                    sqlParameters[1] = new SqlParameter("@Description", oProduct.Description);
                    sqlParameters[2] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[3] = new SqlParameter("@UserId", pRequest.UserId);
                    await ExecQuery();
                    break;

                case nameof(EnumType.Update):
                    queryString = @"UPDATE [dbo].[Products]
                                   SET [ProductName] = @ProductName
                                      ,[Description] = @Description
                                      ,[DateUpdate] = @DateTimeNow
                                      ,[UserUpdate] = @UserId
                                 WHERE [ProductId] = @ProductId";
                    sqlParameters = new SqlParameter[5];
                    sqlParameters[0] = new SqlParameter("@ProductId", oProduct.ProductId);
                    sqlParameters[1] = new SqlParameter("@ProductName", oProduct.ProductName ?? (object)DBNull.Value);
                    sqlParameters[2] = new SqlParameter("@Description", oProduct.Description ?? (object)DBNull.Value);
                    sqlParameters[3] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[4] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    await ExecQuery();
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Không xác định được phương thức!";
                    break;
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = ex.Message;
        }
        finally
        {
            await _context.DisConnect();
        }
        return response;
    }

    /// <summary>
    /// lấy danh sách sản phẩm
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<ProductModel>> GetProductsAsync(int pUserid = -1)
    {
        IEnumerable<ProductModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@UserId", pUserid);
            data = await _context.GetDataAsync(@$"SELECT [ProductId]
                                                  ,[ProductName]
                                                  ,[Description]
                                                  ,[DateCreate]
                                                  ,[UserCreate]
                                                  ,[DateUpdate]
                                                  ,[UserUpdate]
                                              FROM [dbo].[Products] t0 where ISNULL(t0.IsDelete,0) = 0" // không lấy lên tk Support
                    , DataRecordToProductModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// Thêm mới/Cập nhật thông tin bộ phận
    /// </summary>
    /// <param name="pRequest"></param>
    /// <returns></returns>
    public async Task<ResponseModel> UpdateDepartments(RequestModel pRequest)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.Connect();
            string queryString = "";
            DepartmentModel oDepartment = JsonConvert.DeserializeObject<DepartmentModel>(pRequest.Json + "")!;
            SqlParameter[] sqlParameters;
            async Task ExecQuery()
            {
                var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
                if (data != null && data.Rows.Count > 0)
                {
                    response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                    response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
                }
            }
            switch (pRequest.Type)
            {
                case nameof(EnumType.Add):
                    sqlParameters = new SqlParameter[1];
                    sqlParameters[0] = new SqlParameter("@DepartmentName", oDepartment.DepartmentName);
                    // kiểm tra tên đăng nhập
                    if (await _context.ExecuteScalarAsync("select COUNT(*) from Departments with(nolock) where DepartmentName = @DepartmentName", sqlParameters) > 0)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        response.Message = "Tên bộ phận đã tồn tại!";
                        break;
                    }
                    queryString = @"INSERT INTO [dbo].[Departments]([DepartmentName],[DateCreate]  ,[UserCreate],[IsDelete])
                                                        values (@DepartmentName, @DateTimeNow, @UserId, 0)";
                    sqlParameters = new SqlParameter[3];
                    sqlParameters[0] = new SqlParameter("@DepartmentName", oDepartment.DepartmentName);
                    sqlParameters[1] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    await ExecQuery();
                    break;

                case nameof(EnumType.Update):
                    queryString = @"UPDATE [dbo].[Departments]
                                   SET [DepartmentName] = @DepartmentName
                                      ,[DateUpdate] = @DateTimeNow
                                      ,[UserUpdate] = @UserId
                                 WHERE [DepartmentId] = @DepartmentId";
                    sqlParameters = new SqlParameter[5];
                    sqlParameters[0] = new SqlParameter("@ProductId", oDepartment.DepartmentId);
                    sqlParameters[1] = new SqlParameter("@ProductName", oDepartment.DepartmentName ?? (object)DBNull.Value);
                    sqlParameters[2] = new SqlParameter("@UserId", pRequest.UserId);
                    sqlParameters[3] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
                    await ExecQuery();
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Không xác định được phương thức!";
                    break;
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = ex.Message;
        }
        finally
        {
            await _context.DisConnect();
        }
        return response;
    }

    /// <summary>
    /// lấy danh sách bộ phận
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<DepartmentModel>> GetDepartmentsAsync(int pUserid = -1)
    {
        IEnumerable<DepartmentModel> data;
        try
        {
            await _context.Connect();
            SqlParameter[] sqlParameters = new SqlParameter[1];
            sqlParameters[0] = new SqlParameter("@UserId", pUserid);
            data = await _context.GetDataAsync(@$"SELECT [DepartmentId]
                                                  ,[DepartmentName]
                                                  ,[DateCreate]
                                                  ,[UserCreate]
                                                  ,[DateUpdate]
                                                  ,[UserUpdate]
                                              FROM [dbo].[Departments] t0 where ISNULL(t0.IsDelete,0) = 0" // không lấy lên tk Support
                    , DataRecordToDepartmentModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// Lấy danh sách index
    /// </summary>
    /// <param name="pUserid">ID người dùng (mặc định là -1)</param>
    /// <returns>Danh sách index</returns>
    public async Task<IEnumerable<ReportModel>> GetIndexsAsync(SearchModel search)
    {
        IEnumerable<ReportModel> data;
        try
        {
            await _context.Connect();

            SqlParameter[] sqlParameters = new SqlParameter[2];
            sqlParameters[0] = new SqlParameter("@TodayMidnight", search.FromDate);
            sqlParameters[1] = new SqlParameter("@CurrentDateTime", search.ToDate);

            string query = @"SELECT 
                t0.Id as UserId,
                t0.UserName,
                t0.FullName,
                t1.DepartmentName,
                SUM(CASE WHEN t2.Kind = 'LH' THEN 1 ELSE 0 END) AS 'QtyCusContact',
                SUM(CASE WHEN t2.Kind = 'MH' THEN 1 ELSE 0 END) AS 'QtyCusPurchased'
            FROM Users t0 WITH(NOLOCK)
            LEFT JOIN Departments t1 WITH(NOLOCK) ON t0.DepartmentId = t1.DepartmentId
            LEFT JOIN Customers t2 WITH(NOLOCK) ON t0.Id = t2.UserCreate AND t2.DateCreate BETWEEN @TodayMidnight AND @CurrentDateTime
            GROUP BY 
                t0.Id, t0.UserName, t0.FullName, t1.DepartmentName;"; // Không lấy lên tk Support

            data = await _context.GetDataAsync(query, DataRecordToIndexModel, sqlParameters, commandType: CommandType.Text);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    /// <summary>
    /// lấy kết quả báo cáo
    /// </summary>
    /// <param name="isAdmin"></param>
    /// <returns></returns>
    public async Task<IEnumerable<ReportModel>> GetReportsAsync(SearchModel pSearchData)
    {
        List<ReportModel> data = new List<ReportModel>();
        try
        {
            await _context.Connect();
            if (pSearchData.FromDate == null) pSearchData.FromDate = new DateTime(2023, 01, 01);
            if (pSearchData.ToDate == null) pSearchData.ToDate = _dateTimeService.GetCurrentVietnamTime();
            SqlParameter[] sqlParameters = new SqlParameter[6];
            sqlParameters[0] = new SqlParameter("@FromDate", pSearchData.FromDate.Value);
            sqlParameters[1] = new SqlParameter("@ToDate", pSearchData.ToDate.Value);
            sqlParameters[2] = new SqlParameter("@TypeTime", pSearchData.TypeTime);
            sqlParameters[3] = new SqlParameter("@Type", pSearchData.Type);
            sqlParameters[4] = new SqlParameter("@UserId", pSearchData.UserId);
            sqlParameters[5] = new SqlParameter("@Year", pSearchData.Year);
            var results = await _context.GetDataSetAsync(Constants.STORE_REPORT_ALL, sqlParameters, commandType: CommandType.StoredProcedure);
            if (results.Tables != null && results.Tables.Count > 0)
            {
                foreach (DataRow row in results.Tables[0].Rows)
                {
                    switch (pSearchData.Type + "")
                    {
                        case "BaoCaoQuiThangKhachHangMuaHang":
                            data.Add(DataRecordBaoCaoQuiThangKhachHangMuaHangToReportModel(row));
                            break;
                        case "BaoCaoQuiThangKhachHangLienHe":
                            data.Add(DataRecordBaoCaoQuiThangKhachHangLienHeToReportModel(row));
                            break;
                        case "BaoCaoKhachHangMuaHang":
                            data.Add(DataRecordBaoCaoKhachHangMuaHangToReportModel(row));
                            break;
                        case "BaoCaoKhachHangLienHe":
                            data.Add(DataRecordBaoCaoKhachHangLienHeToReportModel(row));
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        catch (Exception) { throw; }
        finally
        {
            await _context.DisConnect();
        }
        return data;
    }

    #endregion Public Funtions

    #region Private Funtions
    private ReportModel DataRecordBaoCaoKhachHangLienHeToReportModel(DataRow row)
    {
        // Mapping các cột của DataTable sang properties của ReportModel
        ReportModel model = new();
        if (!Convert.IsDBNull(row["UserName"])) model.UserName = Convert.ToString(row["UserName"]);
        if (!Convert.IsDBNull(row["FullName"])) model.FullName = Convert.ToString(row["FullName"]);
        if (!Convert.IsDBNull(row["DepartmentName"])) model.DepartmentName = Convert.ToString(row["DepartmentName"]);
        if (!Convert.IsDBNull(row["QtyCusContact"])) model.QtyCusContact = Convert.ToInt32(row["QtyCusContact"]);
        return model;
    }
    private ReportModel DataRecordBaoCaoKhachHangMuaHangToReportModel(DataRow row)
    {
        // Mapping các cột của DataTable sang properties của ReportModel
        ReportModel model = new();
        if (!Convert.IsDBNull(row["UserName"])) model.UserName = Convert.ToString(row["UserName"]);
        if (!Convert.IsDBNull(row["FullName"])) model.FullName = Convert.ToString(row["FullName"]);
        if (!Convert.IsDBNull(row["DepartmentName"])) model.DepartmentName = Convert.ToString(row["DepartmentName"]);
        if (!Convert.IsDBNull(row["QtyCusPurchased"])) model.QtyCusPurchased = Convert.ToInt32(row["QtyCusPurchased"]);
        return model;
    }
    private ReportModel DataRecordBaoCaoQuiThangKhachHangMuaHangToReportModel(DataRow row)
    {
        // Mapping các cột của DataTable sang properties của ReportModel
        ReportModel model = new();
        if (!Convert.IsDBNull(row["UserName"])) model.UserName = Convert.ToString(row["UserName"]);
        if (!Convert.IsDBNull(row["FullName"])) model.FullName = Convert.ToString(row["FullName"]);
        if (!Convert.IsDBNull(row["DepartmentName"])) model.DepartmentName = Convert.ToString(row["DepartmentName"]);
        if (!Convert.IsDBNull(row["Total_01"])) model.Total_01 = Convert.ToDouble(row["Total_01"]);
        if (!Convert.IsDBNull(row["Total_02"])) model.Total_02 = Convert.ToDouble(row["Total_02"]);
        if (!Convert.IsDBNull(row["Total_03"])) model.Total_03 = Convert.ToDouble(row["Total_03"]);
        if (!Convert.IsDBNull(row["Total_04"])) model.Total_04 = Convert.ToDouble(row["Total_04"]);
        if (!Convert.IsDBNull(row["Total_05"])) model.Total_05 = Convert.ToDouble(row["Total_05"]);
        if (!Convert.IsDBNull(row["Total_06"])) model.Total_06 = Convert.ToDouble(row["Total_06"]);
        if (!Convert.IsDBNull(row["Total_07"])) model.Total_07 = Convert.ToDouble(row["Total_07"]);
        if (!Convert.IsDBNull(row["Total_08"])) model.Total_08 = Convert.ToDouble(row["Total_08"]);
        if (!Convert.IsDBNull(row["Total_09"])) model.Total_09 = Convert.ToDouble(row["Total_09"]);
        if (!Convert.IsDBNull(row["Total_10"])) model.Total_10 = Convert.ToDouble(row["Total_10"]);
        if (!Convert.IsDBNull(row["Total_11"])) model.Total_11 = Convert.ToDouble(row["Total_11"]);
        if (!Convert.IsDBNull(row["Total_12"])) model.Total_12 = Convert.ToDouble(row["Total_12"]);
        return model;
    }
    private ReportModel DataRecordBaoCaoQuiThangKhachHangLienHeToReportModel(DataRow row)
    {
        // Mapping các cột của DataTable sang properties của ReportModel
        ReportModel model = new();
        if (!Convert.IsDBNull(row["UserName"])) model.UserName = Convert.ToString(row["UserName"]);
        if (!Convert.IsDBNull(row["FullName"])) model.FullName = Convert.ToString(row["FullName"]);
        if (!Convert.IsDBNull(row["DepartmentName"])) model.DepartmentName = Convert.ToString(row["DepartmentName"]);
        if (!Convert.IsDBNull(row["Total_01"])) model.Total_01 = Convert.ToDouble(row["Total_01"]);
        if (!Convert.IsDBNull(row["Total_02"])) model.Total_02 = Convert.ToDouble(row["Total_02"]);
        if (!Convert.IsDBNull(row["Total_03"])) model.Total_03 = Convert.ToDouble(row["Total_03"]);
        if (!Convert.IsDBNull(row["Total_04"])) model.Total_04 = Convert.ToDouble(row["Total_04"]);
        if (!Convert.IsDBNull(row["Total_05"])) model.Total_05 = Convert.ToDouble(row["Total_05"]);
        if (!Convert.IsDBNull(row["Total_06"])) model.Total_06 = Convert.ToDouble(row["Total_06"]);
        if (!Convert.IsDBNull(row["Total_07"])) model.Total_07 = Convert.ToDouble(row["Total_07"]);
        if (!Convert.IsDBNull(row["Total_08"])) model.Total_08 = Convert.ToDouble(row["Total_08"]);
        if (!Convert.IsDBNull(row["Total_09"])) model.Total_09 = Convert.ToDouble(row["Total_09"]);
        if (!Convert.IsDBNull(row["Total_10"])) model.Total_10 = Convert.ToDouble(row["Total_10"]);
        if (!Convert.IsDBNull(row["Total_11"])) model.Total_11 = Convert.ToDouble(row["Total_11"]);
        if (!Convert.IsDBNull(row["Total_12"])) model.Total_12 = Convert.ToDouble(row["Total_12"]);
        return model;
    }
    /// <summary>
    /// đọc danh sách index
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private ReportModel DataRecordToIndexModel(IDataRecord record)
    {
        ReportModel dep = new();
        if (!Convert.IsDBNull(record["UserId"])) dep.UserId = Convert.ToInt32(record["UserId"]);
        if (!Convert.IsDBNull(record["UserName"])) dep.UserName = Convert.ToString(record["UserName"]);
        if (!Convert.IsDBNull(record["FullName"])) dep.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["DepartmentName"])) dep.DepartmentName = Convert.ToString(record["DepartmentName"]);
        if (!Convert.IsDBNull(record["QtyCusContact"])) dep.QtyCusContact = Convert.ToInt32(record["QtyCusContact"]);
        if (!Convert.IsDBNull(record["QtyCusPurchased"])) dep.QtyCusPurchased = Convert.ToInt32(record["QtyCusPurchased"]);
        return dep;
    }
    /// <summary>
    /// đọc danh sách sản phẩm
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>

    /// <summary>
    /// đọc danh sách phòng ban
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private DepartmentModel DataRecordToDepartmentModel(IDataRecord record)
    {
        DepartmentModel dep = new();
        if (!Convert.IsDBNull(record["DepartmentId"])) dep.DepartmentId = Convert.ToInt32(record["DepartmentId"]);
        if (!Convert.IsDBNull(record["DepartmentName"])) dep.DepartmentName = Convert.ToString(record["DepartmentName"]);
        if (!Convert.IsDBNull(record["DateCreate"])) dep.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) dep.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) dep.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) dep.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        return dep;
    }
    /// <summary>
    /// đọc danh sách sản phẩm
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private ProductModel DataRecordToProductModel(IDataRecord record)
    {
        ProductModel product = new();
        if (!Convert.IsDBNull(record["ProductId"])) product.ProductId = Convert.ToInt32(record["ProductId"]);
        if (!Convert.IsDBNull(record["ProductName"])) product.ProductName = Convert.ToString(record["ProductName"]);
        if (!Convert.IsDBNull(record["Description"])) product.Description = Convert.ToString(record["Description"]);
        if (!Convert.IsDBNull(record["DateCreate"])) product.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) product.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) product.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) product.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        return product;
    }

    /// <summary>
    /// đọc danh sách Users
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private UserModel DataRecordToUserModel(IDataRecord record)
    {
        UserModel user = new();
        if (!Convert.IsDBNull(record["Id"])) user.Id = Convert.ToInt32(record["Id"]);
        if (!Convert.IsDBNull(record["EmpNo"])) user.EmpNo = Convert.ToString(record["EmpNo"]);
        if (!Convert.IsDBNull(record["UserName"])) user.UserName = Convert.ToString(record["UserName"]);
        if (!Convert.IsDBNull(record["Password"])) user.Password = Convert.ToString(record["Password"]);
        if (!Convert.IsDBNull(record["LastPassword"])) user.LastPassword = Convert.ToString(record["LastPassword"]);
        if (!Convert.IsDBNull(record["FullName"])) user.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["PhoneNumber"])) user.PhoneNumber = Convert.ToString(record["PhoneNumber"]);
        if (!Convert.IsDBNull(record["Email"])) user.Email = Convert.ToString(record["Email"]);
        if (!Convert.IsDBNull(record["Address"])) user.Address = Convert.ToString(record["Address"]);
        if (!Convert.IsDBNull(record["DateOfBirth"])) user.DateOfBirth = Convert.ToDateTime(record["DateOfBirth"]);
        if (!Convert.IsDBNull(record["IsAdmin"])) user.IsAdmin = Convert.ToBoolean(record["IsAdmin"]);
        if (!Convert.IsDBNull(record["DateCreate"])) user.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) user.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) user.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) user.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        if (!Convert.IsDBNull(record["DepartmentId"])) user.DepartmentId = Convert.ToInt32(record["DepartmentId"]);
        if (!Convert.IsDBNull(record["DepartmentName"])) user.DepartmentName = Convert.ToString(record["DepartmentName"]);
        return user;
    }

    /// <summary>
    /// xóa dữ liệu -> cập nhật cột IsDelete
    /// </summary>
    /// <returns></returns>
    private async Task<ResponseModel> deleteDataAsync(string pTableName, string pCondition, SqlParameter[] sqlParameters)
    {
        ResponseModel response = new ResponseModel();
        try
        {
            await _context.BeginTranAsync();
            string queryString = @$"UPDATE [dbo].[{pTableName}]
                                set [IsDelete] = 1, [ReasonDelete] = @ReasonDelete, [DateUpdate] = @DateTimeNow, [UserUpdate] = @UserId
                                where {pCondition}";

            var data = await _context.AddOrUpdateAsync(queryString, sqlParameters, CommandType.Text);
            if (data != null && data.Rows.Count > 0)
            {
                response.StatusCode = int.Parse(data.Rows[0]["StatusCode"]?.ToString() ?? "-1");
                response.Message = data.Rows[0]["ErrorMessage"]?.ToString();
            }

            if (response.StatusCode == 0) await _context.CommitTranAsync();
            else await _context.RollbackAsync();
        }
        catch (Exception ex)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = ex.Message;
            await _context.RollbackAsync();
        }
        return response;
    }

    /// <summary>
    /// lấy những thuộc tính cần thiết khi đăng nhập thành công
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private UserModel DataRecordToUserModelByLogin(IDataRecord record)
    {
        UserModel user = new();
        if (!Convert.IsDBNull(record["Id"])) user.Id = Convert.ToInt32(record["Id"]);
        if (!Convert.IsDBNull(record["EmpNo"])) user.EmpNo = Convert.ToString(record["EmpNo"]);
        if (!Convert.IsDBNull(record["UserName"])) user.UserName = Convert.ToString(record["UserName"]);
        if (!Convert.IsDBNull(record["FullName"])) user.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["IsAdmin"])) user.IsAdmin = Convert.ToBoolean(record["IsAdmin"]);
        if (!Convert.IsDBNull(record["IsDeleted"])) user.IsDeleted = Convert.ToBoolean(record["IsDeleted"]);
        return user;
    }

    /// <summary>
    /// đọc danh sách Customers
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    private CustomerModel DataRecordToCustomerModel(IDataRecord record)
    {
        CustomerModel model = new();
        if (!Convert.IsDBNull(record["CusNo"])) model.CusNo = Convert.ToString(record["CusNo"]);
        if (!Convert.IsDBNull(record["FullName"])) model.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["PhoneNumber"])) model.PhoneNumber = Convert.ToString(record["PhoneNumber"]);
        if (!Convert.IsDBNull(record["Email"])) model.Email = Convert.ToString(record["Email"]);
        if (!Convert.IsDBNull(record["Address"])) model.Address = Convert.ToString(record["Address"]);
        if (!Convert.IsDBNull(record["DateOfBirth"])) model.DateOfBirth = Convert.ToDateTime(record["DateOfBirth"]);
        if (!Convert.IsDBNull(record["NoteForAll"])) model.NoteForAll = Convert.ToString(record["NoteForAll"]);
        if (!Convert.IsDBNull(record["DateCreate"])) model.DateCreate = Convert.ToDateTime(record["DateCreate"]);
        if (!Convert.IsDBNull(record["UserCreate"])) model.UserCreate = Convert.ToInt32(record["UserCreate"]);
        if (!Convert.IsDBNull(record["DateUpdate"])) model.DateUpdate = Convert.ToDateTime(record["DateUpdate"]);
        if (!Convert.IsDBNull(record["UserUpdate"])) model.UserUpdate = Convert.ToInt32(record["UserUpdate"]);
        if (!Convert.IsDBNull(record["UserNameCreate"])) model.UserNameCreate = Convert.ToString(record["UserNameCreate"]);
        if (!Convert.IsDBNull(record["UserNameUpdate"])) model.UserNameUpdate = Convert.ToString(record["UserNameUpdate"]);
        if (!Convert.IsDBNull(record["ProductId"])) model.ProductId = Convert.ToInt32(record["ProductId"]);
        if (!Convert.IsDBNull(record["ProductName"])) model.ProductName = Convert.ToString(record["ProductName"]);
        if (!Convert.IsDBNull(record["CustomerSource"])) model.CustomerSource = Convert.ToString(record["CustomerSource"]);
        if (!Convert.IsDBNull(record["Description"])) model.Description = Convert.ToString(record["Description"]); 
        return model;
    }

    /// <summary>
    /// lấy kết quả báo cáo
    /// </summary>
    /// <param name="isAdmin"></param>
    /// <returns></returns>
    private async Task<ResponseModel> CheckKeyBindingBeforeDeleting(RequestModel request = null)
    {
        ResponseModel data = new ResponseModel();
        try
        {
            SqlParameter[] sqlParameters = new SqlParameter[2];
            sqlParameters[0] = new SqlParameter("@Type", request.Type); // tableName
            sqlParameters[1] = new SqlParameter("@Json", request.Json); // ds các id cần update isdelete = 1
            var results = await _context.GetDataSetAsync(Constants.SM_CHECK_KEY_BINDING_BEFORE_DELETE, sqlParameters, commandType: CommandType.StoredProcedure);
            if (results.Tables != null && results.Tables.Count > 0 && results.Tables[0].Rows.Count > 0)
            {
                data.StatusCode = int.Parse(results.Tables[0].Rows[0]["StatusCode"].ToString());
                data.Message = results.Tables[0].Rows[0]["Message"].ToString();
            }
        }
        catch (Exception) { throw; }
        return data;
    }

    /// <summary>
    /// lấy params cho bảng Khách hàng
    /// </summary>
    /// <param name="oCustomer"></param>
    /// <param name="pUserId"></param>
    /// <returns></returns>
    private SqlParameter[] getCustomerParams(CustomerModel oCustomer, int pUserId)
    {
        SqlParameter[] sqlParameters = new SqlParameter[11];
        sqlParameters[0] = new SqlParameter("@CusNo", oCustomer.CusNo);
        sqlParameters[1] = new SqlParameter("@FullName", oCustomer.FullName);
        sqlParameters[2] = new SqlParameter("@PhoneNumber", oCustomer.PhoneNumber ?? (object)DBNull.Value);
        sqlParameters[3] = new SqlParameter("@Email", oCustomer.Email ?? (object)DBNull.Value);
        sqlParameters[4] = new SqlParameter("@Address", oCustomer.Address ?? (object)DBNull.Value);
        sqlParameters[5] = new SqlParameter("@DateOfBirth", oCustomer.DateOfBirth ?? (object)DBNull.Value);
        sqlParameters[6] = new SqlParameter("@NoteForAll", oCustomer.NoteForAll ?? (object)DBNull.Value);
        sqlParameters[7] = new SqlParameter("@UserId", pUserId);
        sqlParameters[8] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
        sqlParameters[9] = new SqlParameter("@CustomerSource", oCustomer.CustomerSource ?? (object)DBNull.Value);
        sqlParameters[10] = new SqlParameter("@ProductId", oCustomer.ProductId);
        return sqlParameters;
    }

    /// <summary>
    /// Đánh số chứng từ
    /// </summary>
    /// <param name="pTable"></param>
    /// <returns></returns>
    private async Task<string> getVoucherNo(string pTable = nameof(EnumTable.@Customers), string fullName ="")
    {
        string strNo = "";
        try
        {
            string strPretrix = "";
            string queryString = "";
            //DateTime getdate = _dateTimeService.GetCurrentVietnamTime();
            DateTime getdate = new DateTime(2024, 01, 01);
            switch (pTable)
            {
                case nameof(EnumTable.@Customers):
                    string strMonth = $"0{getdate.Day}".Substring($"0{getdate.Day}".Length - 2);
                    string[] parts = fullName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string lastName = parts[parts.Length - 1];

                    strPretrix = $"VT-{lastName.ToUpper()}-";
                    queryString = @$"select top 1 CusNo  from [dbo].[Customers] with(nolock)
                                  where CusNo like '%{strPretrix}%' order by CusNo desc";
                    // lấy mã
                    strNo = Convert.ToString(await _context.ExecuteScalarObjectAsync(queryString)) + "";
                    if (string.IsNullOrWhiteSpace(strNo))
                    {
                        strNo = $"{strPretrix}01";
                    }
                    else
                    {
                        int iMax = int.Parse(strNo.Substring(strNo.Length - 2)) + 1; // tăng 1 đơn vị
                        strNo = $"0{iMax}".Substring($"0{iMax}".Length - 2);
                        strNo = $"{strPretrix}{strNo}";
                    }
                    break;
                case nameof(EnumTable.Users):
                    // lấy mã
                    int intId = await _context.ExecuteScalarAsync("select cast(isnull(max(EmpNo), '0') as int) + 1 from [dbo].[Users] with(nolock)");
                    strNo = $"000000000{intId}".Substring($"000000000{intId}".Length - 9); // lấy 9 số cuối
                    break;
                default:
                    break;
            }
        }
        catch(Exception ex)
        {
            strNo = "";
            #pragma warning disable CA2200 // Rethrow to preserve stack details
            throw ex;
            #pragma warning restore CA2200 // Rethrow to preserve stack details
        }
        return strNo;
    }

    #endregion Private Funtions
}