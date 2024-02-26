﻿using SM.Models;
using SM.Models.Shared;
using SM.WEB.Commons;
using SM.WEB.Components;
using SM.WEB.Models;
using SM.WEB.Services;
using SM.WEB.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using Telerik.Blazor.Components;

namespace SM.WEB.Features.Controllers;
public class UserController : SMControllerBase
{
    #region Dependency Injection
    [Inject] private ILogger<UserController>? _logger { get; init; }
    [Inject] private ICliMasterDataService? _masterDataService { get; init; }
    #endregion

    #region Properties
    public bool IsInitialDataLoadComplete { get; set; } = true;
    public List<UserModel>? ListUsers { get; set; }
    public IEnumerable<UserModel>? SelectedUsers { get; set; } = new List<UserModel>();
    public UserModel UserUpdate { get; set; } = new UserModel();
    public EditContext? _EditContext { get; set; }
    public bool IsShowDialog { get; set; }
    public bool IsCreate { get; set; } = true;
    public HConfirm? _rDialogs { get; set; }
    #endregion

    #region Override Functions
    protected override async Task OnInitializedAsync()
    {
        try
        {
            await base.OnInitializedAsync();
            ListBreadcrumbs = new List<BreadcrumbModel>
                {
                    new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
                    new BreadcrumbModel() { Text = "Nhân viên" }
                };
            await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "OnInitializedAsync");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            try
            {
                await _progressService!.SetPercent(0.4);
                await getDataUsers();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "OnAfterRenderAsync");
                ShowError(ex.Message);
            }
            finally
            {
                await _progressService!.Done();
                await InvokeAsync(StateHasChanged);
            }
        }
    }
    #endregion

    #region Private Functions
    private async Task getDataUsers()
    {
        ListUsers = new List<UserModel>();
        SelectedUsers = new List<UserModel>();
        ListUsers = await _masterDataService!.GetDataUsersAsync();
    }

    #endregion

    #region Protected Functions

    protected async void ReLoadDataHandler()
    {
        try
        {
            IsInitialDataLoadComplete = false;
            await getDataUsers();
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "UserController", "ReLoadDataHandler");
            ShowError(ex.Message);
        }
        finally
        {
            IsInitialDataLoadComplete = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected void OnOpenDialogHandler(EnumType pAction = EnumType.Add, UserModel? pItemDetails = null)
    {
        try
        {
            if (pAction == EnumType.Add)
            {
                IsCreate = true;
                UserUpdate = new UserModel();
            }
            else
            {
                UserUpdate.Id = pItemDetails!.Id;
                UserUpdate.EmpNo = pItemDetails!.EmpNo;
                UserUpdate.UserName = pItemDetails.UserName;
                UserUpdate.FullName = pItemDetails.FullName;
                UserUpdate.PhoneNumber = pItemDetails.PhoneNumber;
                UserUpdate.Email = pItemDetails.Email;
                UserUpdate.Address = pItemDetails.Address;
                UserUpdate.Password = EncryptHelper.Decrypt(pItemDetails.Password + "");
                UserUpdate.ReEnterPassword = UserUpdate.Password;
                UserUpdate.DateOfBirth = pItemDetails.DateOfBirth;;
                UserUpdate.IsAdmin = pItemDetails.IsAdmin;
                UserUpdate.DateCreate = pItemDetails.DateCreate;
                UserUpdate.UserCreate = pItemDetails.UserCreate;
                IsCreate = false;
            }
            IsShowDialog = true;
            _EditContext = new EditContext(UserUpdate);
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "UserController", "OnOpenDialogHandler");
            ShowError(ex.Message);
        }
    }

    protected async void SaveDataHandler(EnumType pEnum = EnumType.SaveAndClose)
    {
        try
        {
            string sAction = IsCreate ? nameof(EnumType.Add) : nameof(EnumType.Update);
            var checkData = _EditContext!.Validate();
            if (!checkData) return;
            if(UserUpdate.Password +"" != UserUpdate.ReEnterPassword + "")
            {
                ShowWarning("Nhập lại mật khẩu không đúng so với mật khẩu! Vui lòng nhập lại.");
                return;
            }
            await ShowLoader();
            bool isSuccess = await _masterDataService!.UpdateUserAsync(JsonConvert.SerializeObject(UserUpdate), sAction, pUserId);
            if (isSuccess)
            {
                await getDataUsers();
                IsShowDialog = false;
                return;
            }
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "UserController", "SaveDataHandler");
            ShowError(ex.Message);
        }
        finally
        {
            await ShowLoader(false);
            await InvokeAsync(StateHasChanged);
        }
    }
    protected void OnRowDoubleClickHandler(GridRowClickEventArgs args) => OnOpenDialogHandler(EnumType.Update, args.Item as UserModel);

    protected async void DeleteDataHandler()
    {
        try
        {
            if(SelectedUsers == null || !SelectedUsers.Any())
            {
                ShowWarning(DefaultConstants.MESSAGE_NO_CHOSE_DATA);
                return;
            }
            var confirm = await _rDialogs!.ConfirmAsync($" {DefaultConstants.MESSAGE_CONFIRM_DELETE} ");
            if (!confirm) return;
            await ShowLoader();
            bool isSuccess = await _masterDataService!.DeleteDataAsync(nameof(EnumTable.Users), "", string.Join(",", SelectedUsers.Select(m=>m.Id)), pUserId);
            if(isSuccess)
            {
                await getDataUsers();
            }
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "UserController", "DeleteDataHandler");
            ShowError(ex.Message);
        }
        finally
        {
            await ShowLoader(false);
            await InvokeAsync(StateHasChanged);
        }
    }
    #endregion
}
