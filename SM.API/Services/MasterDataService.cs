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
                    queryString = @"INSERT INTO [dbo].[Users]([EmpNo],[UserName],[Password],[LastPassword],[FullName],[PhoneNumber] ,[Email] ,[Address],[DateOfBirth],[IsAdmin],[DateCreate] ,[UserCreate],[Isdelete])
                                                        values (@EmpNo, @UserName , @Password , @LastPassword, @FullName, @PhoneNumber , @Email, @Address, @DateOfBirth, @IsAdmin, @DateTimeNow, @UserId, 0)";

                    string sPassword = EncryptHelper.Encrypt(oUser.Password + "");
                    sqlParameters = new SqlParameter[12];
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
                                 WHERE [Id] = @Id";

                    sqlParameters = new SqlParameter[10];
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
            data = await _context.GetDataAsync(@$"SELECT [Id]
                                                  ,[EmpNo]
                                                  ,[UserName]
                                                  ,[Password]
                                                  ,[LastPassword]
                                                  ,[FullName]
                                                  ,[PhoneNumber]
                                                  ,[Email]
                                                  ,[Address]
                                                  ,[DateOfBirth]
                                                  ,[IsAdmin]
                                                  ,[DateCreate]
                                                  ,[UserCreate]
                                                  ,[DateUpdate]
                                                  ,[UserUpdate]
                                              FROM [dbo].[Users] t0 where ISNULL(t0.IsDelete,0) = 0" // không lấy lên tk Support
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
            data = await _context.GetDataAsync(@$"select [CusNo], T0.[FullName], T0.[PhoneNumber], T0.[Email], T0.[Address], T0.[DateOfBirth]
                      ,T0.[NoteForAll],T0.[DateCreate],T0.[UserCreate],T0.[DateUpdate],T0.[UserUpdate]
					  ,T1.FullName as [UserNameCreate], T2.FullName as [UserNameUpdate]
			     from [dbo].[Customers] as T0 with(nolock)
		    left join [dbo].[Users] as T1 with(nolock) on T0.UserCreate = T1.Id
			left join [dbo].[Users] as T2 with(nolock) on T0.UserUpdate = T2.Id
					where T0.[IsDelete] = 0 order by [CusNo] desc"
                    , DataRecordToCustomerModel, commandType: CommandType.Text);
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
                    oCustomer.CusNo = await getVoucherNo(nameof(EnumTable.@Customers));
                    if (string.IsNullOrWhiteSpace(oCustomer.CusNo))
                    {
                        response.StatusCode = (int)HttpStatusCode.NoContent;
                        response.Message = "Không đánh được mã Khách hàng!. Vui lòng kiểm tra lại";
                        return response;
                    }
                    sqlParameters = getCustomerParams(oCustomer, pRequest.UserId);
                    queryString = @"Insert into [dbo].[Customers] ([CusNo],[FullName],[PhoneNumber],[Email]
                                    ,[Address],[DateOfBirth],[NoteForAll],[DateCreate],[UserCreate],[IsDelete])
                                    values (@CusNo, @FullName, @PhoneNumber, @Email
                                    ,@Address, @DateOfBirth, @NoteForAll, @DateTimeNow, @UserId, 0)";
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

    #endregion Public Funtions

    #region Private Funtions

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
        SqlParameter[] sqlParameters = new SqlParameter[9];
        sqlParameters[0] = new SqlParameter("@CusNo", oCustomer.CusNo);
        sqlParameters[1] = new SqlParameter("@FullName", oCustomer.FullName);
        sqlParameters[2] = new SqlParameter("@PhoneNumber", oCustomer.PhoneNumber ?? (object)DBNull.Value);
        sqlParameters[3] = new SqlParameter("@Email", oCustomer.Email ?? (object)DBNull.Value);
        sqlParameters[4] = new SqlParameter("@Address", oCustomer.Address ?? (object)DBNull.Value);
        sqlParameters[5] = new SqlParameter("@DateOfBirth", oCustomer.DateOfBirth ?? (object)DBNull.Value);
        sqlParameters[6] = new SqlParameter("@NoteForAll", oCustomer.NoteForAll ?? (object)DBNull.Value);
        sqlParameters[7] = new SqlParameter("@UserId", pUserId);
        sqlParameters[8] = new SqlParameter("@DateTimeNow", _dateTimeService.GetCurrentVietnamTime());
        return sqlParameters;
    }

    /// <summary>
    /// Đánh số chứng từ
    /// </summary>
    /// <param name="pTable"></param>
    /// <returns></returns>
    private async Task<string> getVoucherNo(string pTable = nameof(EnumTable.@Customers))
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
                    strPretrix = $"KH-{(getdate.Year + "").Substring(2)}{strMonth}-";
                    queryString = @$"select top 1 CusNo  from [dbo].[Customers] with(nolock)
                                  where CusNo like '%{strPretrix}%' order by CusNo desc";
                    // lấy mã
                    strNo = Convert.ToString(await _context.ExecuteScalarObjectAsync(queryString)) + "";
                    if (string.IsNullOrWhiteSpace(strNo))
                    {
                        strNo = $"{strPretrix}00001";
                    }
                    else
                    {
                        int iMax = int.Parse(strNo.Substring(strNo.Length - 5)) + 1; // tăng 1 đơn vị
                        strNo = $"00000{iMax}".Substring($"00000{iMax}".Length - 5);
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