using SM.Models;
using SM.WEB.Models;
using SM.WEB.Services;
using SM.WEB.Shared;
using Microsoft.AspNetCore.Components;
using SM.Models.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using SM.WEB.Features.Pages;
using Telerik.Blazor.Components;
using Newtonsoft.Json;

namespace SM.WEB.Features.Controllers
{
    public class IndexController : SMControllerBase
    {
        #region Dependency Injection

        [Inject] private ILogger<IndexController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] private IDateTimeService? _datetimeService { get; init; }
        [Inject] private NavigationManager? _navigationManager { get; init; }
        [Inject]  private  IDateTimeService _dateTimeService { get; init; }

        #endregion Dependency Injection

        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public SearchModel ItemFilter = new SearchModel();
        public List<ReportModel>? ListIndex { get; set; }
        public bool IsShowDialogEmp { get; set; }
        public TelerikGrid<ReportModel> GridRef { get; set; }

        #endregion

        #region Override Functions
        protected void OnNavHandler(ReportModel? pItemDetails = null)
        {
            try
            {
                if(pIsAdmin==false && pItemDetails.UserId != pUserId)
                {
                    ShowWarning("Bạn không thể truy cập vào xem khách hàng của người nhân viên khác !!!");
                    return;
                }
                Dictionary<string, string> pParams = new Dictionary<string, string>
                {
                    { "UserId", $"{pItemDetails.UserId}"},
                    { "IsAdmin", $"{pIsAdmin}" },
                };
                string key = EncryptHelper.Encrypt(JsonConvert.SerializeObject(pParams)); // mã hóa key
                _navigationManager!.NavigateTo($"/customer?key={key}");
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "IndexController", "OnNavHandler");
                ShowError(ex.Message);
            }
        }


        protected async void ReLoadDataHandler()
        {
            try
            {
                IsInitialDataLoadComplete = false;
                await getDataIndexs();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "IndexController", "ReLoadDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();
                ItemFilter.FromDate = _dateTimeService.GetCurrentVietnamDate000();
                ItemFilter.ToDate = _dateTimeService.GetCurrentVietnamTime();
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
                    await _progressService!.SetPercent(0.4);
                    ListIndex = await _masterDataService!.GetIndexsAsync(ItemFilter);
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
        private async Task getDataIndexs()
        {
            ListIndex = await _masterDataService!.GetIndexsAsync(ItemFilter);
            GridRef?.Rebind();
        }
        #endregion
    }
}
