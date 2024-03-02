﻿using SM.Models;
using SM.WEB.Models;
using SM.WEB.Services;
using SM.WEB.Shared;
using Microsoft.AspNetCore.Components;
using SM.Models.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using SM.WEB.Features.Pages;
using Telerik.Blazor.Components;

namespace SM.WEB.Features.Controllers
{
    public class IndexController : SMControllerBase
    {
        #region Dependency Injection

        [Inject] private ILogger<IndexController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] private IDateTimeService? _datetimeService { get; init; }
        [Inject] private NavigationManager? _navigationManager { get; init; }


        #endregion Dependency Injection

        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public SearchModel ItemFilter = new SearchModel();
        public List<UserModel>? ListUsers { get; set; }
        public List<CustomerModel>? ListCustomers { get; set; }
        public UserModel CurUser { get; set; } = new UserModel();
        public bool IsShowDialogEmp { get; set; }
        public DateTime FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime ToDate = DateTime.Now;

        #endregion

        #region Override Functions
        protected async void ReLoadDataHandler()
        {
            try
            {
                IsInitialDataLoadComplete = false;
                await getDataCustomers();
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
        protected void OnRowDoubleClickHandler(GridRowClickEventArgs args) => OnOpenDialogHandler(args.Item as UserModel);
        protected void OnOpenDialogHandler(UserModel? pItemDetails = null)
        {
            try
            {
                ItemFilter.SearchUserId = pItemDetails.Id;
                IsShowDialogEmp = false;
                getDataCustomers();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "BranchController", "OnOpenDialogHandler");
                ShowError(ex.Message);
            }
        }


        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();
                CurUser.EmpNo = "-1";
                CurUser.FullName = "Đang cập nhật";
                CurUser.PhoneNumber = "Đang cập nhật";
                CurUser.Email = "Đang cập nhật";
                CurUser.Address = "Đang cập nhật";
                ListBreadcrumbs = new List<BreadcrumbModel>
                {
                    new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
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
                    ItemFilter.CurDate = _datetimeService!.GetCurrentVietnamTime();
                    await _progressService!.SetPercent(0.4);
                    ListUsers = await _masterDataService!.GetDataUsersAsync();
                    var oUser = ListUsers?.FirstOrDefault(m=>m.Id == pUserId);
                    if(oUser != null)
                    {
                        CurUser.EmpNo = oUser.EmpNo;
                        CurUser.FullName = oUser.FullName;
                        CurUser.PhoneNumber = oUser.PhoneNumber;
                        CurUser.Email = oUser.Email;
                        CurUser.Address = oUser.Address;
                    }
                    ListCustomers = await _masterDataService!.GetCustomersAsync(ItemFilter);
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

        #endregion Override Functions

        #region Private Functions
        private async Task getDataCustomers()
        {
            ItemFilter.FromDate = FromDate;
            ItemFilter.ToDate = ToDate;
            ListCustomers = await _masterDataService!.GetCustomersAsync(ItemFilter);
        }
        #endregion
    }
}
