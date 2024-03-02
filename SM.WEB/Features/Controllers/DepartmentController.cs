using SM.Models;
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
public class DepartmentController : SMControllerBase
{
    #region Dependency Injection
    [Inject] private ILogger<DepartmentController>? _logger { get; init; }
    [Inject] private ICliMasterDataService? _masterDataService { get; init; }
    public TelerikGrid<DepartmentModel> GridRef { get; set; }
    #endregion

    #region Properties
    public bool IsInitialDataLoadComplete { get; set; } = true;
    public List<DepartmentModel>? ListDepartments { get; set; }
    public IEnumerable<DepartmentModel>? SelectedDepartments { get; set; } = new List<DepartmentModel>();
    public DepartmentModel DepartmentUpdate { get; set; } = new DepartmentModel();
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
                    new BreadcrumbModel() { Text = "Bộ phận" }
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
                await getDataDepartments();
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
    private async Task getDataDepartments()
    {
        ListDepartments = new List<DepartmentModel>();
        SelectedDepartments = new List<DepartmentModel>();
        ListDepartments = await _masterDataService!.GetDataDepartmentsAsync();
        GridRef?.Rebind();
    }

    #endregion

    #region Protected Functions

    protected async void ReLoadDataHandler()
    {
        try
        {
            IsInitialDataLoadComplete = false;
            await getDataDepartments();
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "DepartmentController", "ReLoadDataHandler");
            ShowError(ex.Message);
        }
        finally
        {
            IsInitialDataLoadComplete = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected void OnOpenDialogHandler(EnumType pAction = EnumType.Add, DepartmentModel? pItemDetails = null)
    {
        try
        {
            if (pAction == EnumType.Add)
            {
                IsCreate = true;
                DepartmentUpdate = new DepartmentModel();
            }
            else
            {
                DepartmentUpdate.DepartmentId = pItemDetails!.DepartmentId;
                DepartmentUpdate.DepartmentName = pItemDetails!.DepartmentName; 
                DepartmentUpdate.DateCreate = pItemDetails.DateCreate;
                DepartmentUpdate.UserCreate = pItemDetails.UserCreate;
                IsCreate = false;
            }
            IsShowDialog = true;
            _EditContext = new EditContext(DepartmentUpdate);
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "DepartmentController", "OnOpenDialogHandler");
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
            await ShowLoader();
            bool isSuccess = await _masterDataService!.UpdateDepartmentAsync(JsonConvert.SerializeObject(DepartmentUpdate), sAction, pUserId);
            if (isSuccess)
            {
                await getDataDepartments();
                IsShowDialog = false;
                return;
            }
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "DepartmentController", "SaveDataHandler");
            ShowError(ex.Message);
        }
        finally
        {
            await ShowLoader(false);
            await InvokeAsync(StateHasChanged);
        }
    }
    protected void OnRowDoubleClickHandler(GridRowClickEventArgs args) => OnOpenDialogHandler(EnumType.Update, args.Item as DepartmentModel);

    protected async void DeleteDataHandler()
    {
        try
        {
            if(SelectedDepartments == null || !SelectedDepartments.Any())
            {
                ShowWarning(DefaultConstants.MESSAGE_NO_CHOSE_DATA);
                return;
            }
            var confirm = await _rDialogs!.ConfirmAsync($" {DefaultConstants.MESSAGE_CONFIRM_DELETE} ");
            if (!confirm) return;
            await ShowLoader();
            bool isSuccess = await _masterDataService!.DeleteDataAsync(nameof(EnumTable.Departments), "", string.Join(",", SelectedDepartments.Select(m=>m.DepartmentId)), pUserId);
            if(isSuccess)
            {
                await getDataDepartments();
            }
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "DepartmentController", "DeleteDataHandler");
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
