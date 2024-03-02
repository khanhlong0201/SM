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
public class ProductController : SMControllerBase
{
    #region Dependency Injection
    [Inject] private ILogger<ProductController>? _logger { get; init; }
    [Inject] private ICliMasterDataService? _masterDataService { get; init; }
    public TelerikGrid<ProductModel> GridRef { get; set; }
    #endregion

    #region Properties
    public bool IsInitialDataLoadComplete { get; set; } = true;
    public List<ProductModel>? ListProducts { get; set; }
    public IEnumerable<ProductModel>? SelectedProducts { get; set; } = new List<ProductModel>();
    public ProductModel ProductUpdate { get; set; } = new ProductModel();
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
                    new BreadcrumbModel() { Text = "Sản phẩm" }
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
                await getDataProducts();
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
    private async Task getDataProducts()
    {
        ListProducts = new List<ProductModel>();
        SelectedProducts = new List<ProductModel>();
        ListProducts = await _masterDataService!.GetDataProductsAsync();
        GridRef?.Rebind();
    }

    #endregion

    #region Protected Functions

    protected async void ReLoadDataHandler()
    {
        try
        {
            IsInitialDataLoadComplete = false;
            await getDataProducts();
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "ProductController", "ReLoadDataHandler");
            ShowError(ex.Message);
        }
        finally
        {
            IsInitialDataLoadComplete = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected void OnOpenDialogHandler(EnumType pAction = EnumType.Add, ProductModel? pItemDetails = null)
    {
        try
        {
            if (pAction == EnumType.Add)
            {
                IsCreate = true;
                ProductUpdate = new ProductModel();
            }
            else
            {
                ProductUpdate.ProductId = pItemDetails!.ProductId;
                ProductUpdate.ProductName = pItemDetails!.ProductName;
                ProductUpdate.Description = pItemDetails.Description;
                ProductUpdate.DateCreate = pItemDetails.DateCreate;
                ProductUpdate.UserCreate = pItemDetails.UserCreate;
                IsCreate = false;
            }
            IsShowDialog = true;
            _EditContext = new EditContext(ProductUpdate);
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "ProductController", "OnOpenDialogHandler");
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
            bool isSuccess = await _masterDataService!.UpdateProductAsync(JsonConvert.SerializeObject(ProductUpdate), sAction, pUserId);
            if (isSuccess)
            {
                await getDataProducts();
                IsShowDialog = false;
                return;
            }
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "ProductController", "SaveDataHandler");
            ShowError(ex.Message);
        }
        finally
        {
            await ShowLoader(false);
            await InvokeAsync(StateHasChanged);
        }
    }
    protected void OnRowDoubleClickHandler(GridRowClickEventArgs args) => OnOpenDialogHandler(EnumType.Update, args.Item as ProductModel);

    protected async void DeleteDataHandler()
    {
        try
        {
            if(SelectedProducts == null || !SelectedProducts.Any())
            {
                ShowWarning(DefaultConstants.MESSAGE_NO_CHOSE_DATA);
                return;
            }
            var confirm = await _rDialogs!.ConfirmAsync($" {DefaultConstants.MESSAGE_CONFIRM_DELETE} ");
            if (!confirm) return;
            await ShowLoader();
            bool isSuccess = await _masterDataService!.DeleteDataAsync(nameof(EnumTable.Products), "", string.Join(",", SelectedProducts.Select(m=>m.ProductId)), pUserId);
            if(isSuccess)
            {
                await getDataProducts();
            }
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "ProductController", "DeleteDataHandler");
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
