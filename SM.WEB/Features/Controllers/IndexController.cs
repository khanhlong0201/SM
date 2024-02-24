﻿//using SM.Models;
//using SM.WEB.Models;
//using SM.WEB.Services;
//using SM.WEB.Shared;
//using Microsoft.AspNetCore.Components;

//namespace SM.WEB.Features.Controllers
//{
//    public class IndexController : SMControllerBase
//    {
//        #region Dependency Injection

//        [Inject] private ILogger<IndexController>? _logger { get; init; }
//        [Inject] private ICliDocumentService? _documentService { get; init; }
//        [Inject] private NavigationManager? _navigationManager { get; set; }

//        #endregion Dependency Injection

//        #region Properties
//        public bool IsInitialDataLoadComplete { get; set; } = true;
//        public List<BorrowOrderModel>? ListDocuments { get; set; }
//        public IEnumerable<BorrowOrderModel>? SelectedDocuments { get; set; } = new List<BorrowOrderModel>();
//        public SearchModel ItemFilter = new SearchModel();

//        public int intTotal { get; set; }
//        public int intBorrowing { get; set; }
//        public int intClosed { get; set; }
//        public int intDemurrage { get; set; }
//        public int intToday { get; set; }
//        public int intPending { get; set; }
//        #endregion

//        #region Override Functions

//        protected override async Task OnInitializedAsync()
//        {
//            try
//            {
//                await base.OnInitializedAsync();
//                ListBreadcrumbs = new List<BreadcrumLModel>
//                {
//                    new BreadcrumLModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
//                };
//                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
//            }
//            catch (Exception ex)
//            {
//                _logger!.LogError(ex, "OnInitializedAsync");
//            }
//        }

//        protected override async Task OnAfterRenderAsync(bool firstRender)
//        {
//            await base.OnAfterRenderAsync(firstRender);
//            if (firstRender)
//            {
//                try
//                {
//                    //StartDate = _dateTimeService!.GetCurrentVietnamTime();
//                    //StartTime = new DateTime(StartDate.Year, StartDate.Month, StartDate.Day, 23, 0, 0);
//                    await _progressService!.SetPercent(0.4);
//                    await showReport();
//                    await getDataDocuments();
//                }
//                catch (Exception ex)
//                {
//                    _logger!.LogError(ex, "OnAfterRenderAsync");
//                    ShowError(ex.Message);
//                }
//                finally
//                {
//                    await _progressService!.Done();
//                    await InvokeAsync(StateHasChanged);
//                }
//            }
//        }

//        #endregion Override Functions

//        #region Private Functions

//        private async Task getDataDocuments()
//        {
//            ListDocuments = new List<BorrowOrderModel>();
//            SelectedDocuments = new List<BorrowOrderModel>();
//            ItemFilter.UserId = pUserId;
//            ItemFilter.IsAdmin = pIsAdmin;
//            ItemFilter.Type = "ALL";
//            ItemFilter.StatusId = nameof(DocStatus.All);
//            ItemFilter.TypeBO = nameof(DocStatus.All);
//            ItemFilter.FromDate = new DateTime(2023, 1, 1);
//            ItemFilter.ToDate = _dateTimeService!.GetCurrentVietnamTime();
//            ListDocuments = await _documentService!.GetBorrowOrdersAsync(ItemFilter);
//        }


//        private async Task showReport()
//        {
//            Dictionary<string, int>? keyValues = await _documentService!.GetReportIndexAsync();
//            if (keyValues == null) return;
//            if (keyValues.ContainsKey("intTotal")) intTotal = keyValues["intTotal"];
//            if (keyValues.ContainsKey("intBorrowing")) intBorrowing = keyValues["intBorrowing"];
//            if (keyValues.ContainsKey("intClosed")) intClosed = keyValues["intClosed"];
//            if (keyValues.ContainsKey("intDemurrage")) intDemurrage = keyValues["intDemurrage"];
//            if (keyValues.ContainsKey("intToday")) intToday = keyValues["intToday"];
//            if (keyValues.ContainsKey("intPending")) intPending = keyValues["intPending"];
//        }    
//        #endregion
//    }
//}
