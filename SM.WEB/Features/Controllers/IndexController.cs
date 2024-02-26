using SM.Models;
using SM.WEB.Models;
using SM.WEB.Services;
using SM.WEB.Shared;
using Microsoft.AspNetCore.Components;

namespace SM.WEB.Features.Controllers
{
    public class IndexController : SMControllerBase
    {
        #region Dependency Injection

        [Inject] private ILogger<IndexController>? _logger { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        [Inject] private NavigationManager? _navigationManager { get; init; }

        #endregion Dependency Injection

        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public SearchModel ItemFilter = new SearchModel();
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

        #endregion
    }
}
