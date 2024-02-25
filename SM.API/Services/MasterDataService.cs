﻿using SM.API.Commons;
using SM.API.Infrastructure;
using SM.Models;
using SM.Models.Shared;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NPOI.POIFS.Crypt.Dsig;
using SixLabors.ImageSharp;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Security.Policy;

namespace SM.API.Services;
public interface IMasterDataService
{
    Task<IEnumerable<UserModel>> GetUsersAsync(int pUserId = -1);
    Task<ResponseModel> UpdateUsers(RequestModel pRequest);
    Task<IEnumerable<UserModel>> Login(LoginRequestModel pRequest);
    Task<ResponseModel> DeleteDataAsync(RequestModel pRequest);
   
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
                    sqlParameters[0] = new SqlParameter("@Type", "Users");
                    queryString = @"INSERT INTO [dbo].[Users]([UserName],[Password],[LastPassword],[FullName],[PhoneNumber] ,[Email] ,[Address],[DateOfBirth],[IsAdmin],[DateCreate] ,[UserCreate],[Isdelete],[Type] )
                                                        values (@UserName , @Password , @LastPassword, @FullName, @PhoneNumber , @Email, @Address, @DateOfBirth, @IsAdmin, @DateTimeNow, @UserId, 0, 'Admin')";

                    string sPassword = EncryptHelper.Encrypt(oUser.Password + "");
                    sqlParameters = new SqlParameter[11];
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
    #endregion Public Functions

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
        if (!Convert.IsDBNull(record["UserName"])) user.UserName = Convert.ToString(record["UserName"]);
        if (!Convert.IsDBNull(record["FullName"])) user.FullName = Convert.ToString(record["FullName"]);
        if (!Convert.IsDBNull(record["IsAdmin"])) user.IsAdmin = Convert.ToBoolean(record["IsAdmin"]);
        if (!Convert.IsDBNull(record["IsDeleted"])) user.IsDeleted = Convert.ToBoolean(record["IsDeleted"]);
        return user;
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
    #endregion Private Funtions
}