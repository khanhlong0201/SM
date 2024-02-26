using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using SM.Models;
using SM.WEB.Models;
using SM.WEB.Services;
using SM.WEB.Shared;
using Telerik.Blazor.Components;

namespace SM.WEB.Features.Controllers
{
    public class CustomerController : SMControllerBase
    {
        #region Dependency Injection
        [Inject] private ILogger<CustomerController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] private NavigationManager? _navManager { get; init; }
        #endregion

        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<CustomerModel>? ListCustomers { get; set; }
        public IEnumerable<CustomerModel>? SelectedCustomers { get; set; } = new List<CustomerModel>();
        public CustomerModel CustomerUpdate { get; set; } = new CustomerModel();
        public SearchModel ItemSearch { get; set; } = new SearchModel();
        public EditContext? _EditContext { get; set; }
        public bool IsShowDialog { get; set; }
        public bool IsCreate { get; set; } = true;
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
                    new BreadcrumbModel() { Text = "Hồ sơ khách hàng" },
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
                    await getDataCustomers();

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
        private async Task getDataCustomers()
        {
            ListCustomers = new List<CustomerModel>();
            SelectedCustomers = new List<CustomerModel>();
            ListCustomers = await _masterDataService!.GetCustomersAsync(ItemSearch);
        }
        #endregion

        #region Protected Functions
        protected async void ReLoadDataHandler()
        {
            try
            {
                IsInitialDataLoadComplete = false;
                await getDataCustomers();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "CustomerController", "ReLoadDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await Task.Delay(50);
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void OnOpenDialogHandler(EnumType pAction = EnumType.Add, CustomerModel? pItemDetails = null)
        {
            try
            {
                if (pAction == EnumType.Add)
                {
                    IsCreate = true;
                    CustomerUpdate = new CustomerModel();
                }
                else
                {
                    CustomerUpdate.CusNo = pItemDetails!.CusNo;
                    CustomerUpdate.FullName = pItemDetails.FullName;
                    CustomerUpdate.PhoneNumber = pItemDetails.PhoneNumber;
                    CustomerUpdate.Email = pItemDetails.Email;
                    CustomerUpdate.Address = pItemDetails.Address;
                    CustomerUpdate.DateOfBirth = pItemDetails.DateOfBirth;
                    CustomerUpdate.NoteForAll = pItemDetails.NoteForAll;
                    CustomerUpdate.DateCreate = pItemDetails.DateCreate;
                    CustomerUpdate.UserCreate = pItemDetails.UserCreate;
                    IsCreate = false;
                }
                IsShowDialog = true;
                _EditContext = new EditContext(CustomerUpdate);
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "CustomerController", "OnOpenDialogHandler");
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// cập nhật thông tin khách hàng
        /// </summary>
        protected async void SaveDataHandler()
        {
            try
            {
                string sAction = IsCreate ? nameof(EnumType.Add) : nameof(EnumType.Update);
                var checkData = _EditContext!.Validate();
                if (!checkData) return;
                await ShowLoader();
                bool isSuccess = await _masterDataService!.UpdateCustomerAsync(JsonConvert.SerializeObject(CustomerUpdate), sAction, pUserId);
                if (isSuccess)
                {
                    await getDataCustomers();
                    IsShowDialog = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "CustomerController", "SaveDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                await Task.Delay(75);
                await ShowLoader(false);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected void OnRowDoubleClickHandler(GridRowClickEventArgs args) => OnOpenDialogHandler(EnumType.Update, args.Item as CustomerModel);
        #endregion
    }
}
