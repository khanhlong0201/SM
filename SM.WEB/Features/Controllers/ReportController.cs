using SM.Models;
using SM.Models.Shared;
using SM.WEB.Commons;
using SM.WEB.Components;
using SM.WEB.Models;
using SM.WEB.Services;
using SM.WEB.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Telerik.Blazor.Components;

namespace SM.WEB.Features.Controllers
{
    public class ReportController : SMControllerBase, IDisposable
    {
        #region Dependency Injection
        [Inject] private ILogger<ReportController>? _logger { get; init; }
        [Inject] private NavigationManager? _navManager { get; init; }
        [Inject] private ICliMasterDataService? _masterDataService { get; init; }
        #endregion

        #region Properties
        public bool IsInitialDataLoadComplete { get; set; } = true;
        public List<ReportModel>? ListReports { get; set; }
        public IEnumerable<ReportModel>? SelectedReports { get; set; } = new List<ReportModel>();
        public List<ComboboxModel>? ListTypeTime { get; set; } // quí hay tháng 
        public SearchModel ItemFilter = new SearchModel();
        public TelerikGrid<ReportModel> Grid;
        public List<ComboboxModel>? ListTypeReports { get; set; } // loại báo cáo
        public List<ComboboxModel>? ListKinds { get; set; } // từ ngày - đến ngày hay quí - tháng
        public List<ComboboxModel>? ListServiceTypes { get; set; } // từ ngày - đến ngày hay quí - tháng
        public List<ComboboxModel>? ListUserTypes { get; set; } // từ ngày - đến ngày hay quí - tháng

        public List<ComboboxModel>? ListAndCharts { get; set; } // lưới hay là biểu đồ

        public List<ReportModel> ListChart { get; set; } = new List<ReportModel>();

        public List<int>? ListYears { get; set; }
        public int pYearDefault { get; set; }
        public string TilteReport = "BIỂU ĐỒ SỐ LƯỢNG KÁCHH HÀNG ĐÃ MUA HÀNG";

        public string pKind = "";
        public string pServiceType = "";
        public string pUserType = "";
        public string pChart = "";



        public string pReportType = "";
        public string pReportTypeName = "";
        private string currentLocation = "";
        #endregion

        #region Override Functions
        public async void SelectedKind(string kind)
        {
            try
            {
                pKind = kind;
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async void SelectedChart(string chart)
        {
            try
            {
                pChart = chart;
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// chọn loại báo cáo
        /// </summary>
        /// <param name="supplies"></param>
        /// <param name="invetory"></param>
        public async void SelectedReportTypeChanged(string report)
        {
            try
            {
                if (report == null) return;
                pReportType = report; // gán loại báo cáo
                ItemFilter.Type = report;
                pReportTypeName = ListTypeReports.FirstOrDefault(m => m.Code == pReportType)?.Name + "";
                ListBreadcrumbs = new List<BreadcrumbModel>
                {
                    new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
                    new BreadcrumbModel() { Text = "Hệ thống" },
                    new BreadcrumbModel() { Text = "Báo cáo" },
                    new BreadcrumbModel() { Text =  pReportTypeName}
                };
                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
                _navManager.LocationChanged += LocationChanged;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// chọn quí tháng
        /// </summary>
        /// <param name="supplies"></param>
        /// <param name="invetory"></param>
        public async void SelectedTypeTimeChanged(string typeTime)
        {
            try
            {
                if (typeTime == null) return;
                ItemFilter.TypeTime = typeTime;
                await getDataReports();
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await base.OnInitializedAsync();
                ListTypeTime = new List<ComboboxModel>()
                        {
                            new ComboboxModel() {Code = nameof(TypeTime.Qui), Name = "Quí"},
                            new ComboboxModel() {Code = nameof(TypeTime.Thang), Name = "Tháng"},
                        };
                ListTypeReports = new List<ComboboxModel>()
                {

                    new ComboboxModel() {Code = nameof(ReportType.@DoanhThuKhachHangMuaHangLienHe), Name = "Số lượng khác hàng liên hệ - khách hàng mua hàng"},

                };

                ListKinds = new List<ComboboxModel>()
                {
                    new ComboboxModel() {Code = nameof(Kind.QuiThang), Name = "Quí - tháng"},
                    new ComboboxModel() {Code = nameof(Kind.TuNgayDenNgay), Name = "Từ ngày - đến ngày"},

                };

                ListServiceTypes = new List<ComboboxModel>()
                {
                    new ComboboxModel() {Code = nameof(ServiceType.Purchase), Name = "Khách Hàng Mua Hàng"},
                    new ComboboxModel() {Code = nameof(ServiceType.Contact), Name = "Khách Hàng Liên Hệ"},
                };

                ListAndCharts = new List<ComboboxModel>()
                {
                    new ComboboxModel() {Code = nameof(ChartReportType.List), Name = "Lưới"},
                    new ComboboxModel() {Code = nameof(ChartReportType.Chart), Name = "Biểu đồ"},
                };

                ListYears = new List<int>();
                pYearDefault = DateTime.Now.Year;
                for (int i = 2023; i < (DateTime.Now.Year + 1); i++)
                {
                    ListYears.Add(i);
                }

                pReportType = nameof(ReportType.@DoanhThuKhachHangMuaHangLienHe);
                var uri = _navManager!.ToAbsoluteUri(_navManager.Uri);
                switch (uri.AbsolutePath.ToString())
                {
                    case "/report-rurchase":
                        pReportType = nameof(ReportType.@DoanhThuKhachHangMuaHangLienHe);
                        break;
                }
                pReportTypeName = ListTypeReports.FirstOrDefault(m => m.Code == pReportType)?.Name + "";
                ListBreadcrumbs = new List<BreadcrumbModel>
                {
                    new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
                    new BreadcrumbModel() { Text = "Hệ thống" },
                    new BreadcrumbModel() { Text = "Báo cáo" },
                    new BreadcrumbModel() { Text =  pReportTypeName}
                };
                ItemFilter.Type = pReportType; // gán loại báo cáo

                pKind = nameof(Kind.QuiThang);
                pServiceType = nameof(ServiceType.Purchase);
                pChart = nameof(ChartReportType.List);

                await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
                _navManager.LocationChanged += LocationChanged;



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
                    ItemFilter.TypeTime = nameof(TypeTime.Thang);
                    ItemFilter.FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);// ngày đầu tháng
                    ItemFilter.ToDate = _dateTimeService!.GetCurrentVietnamTime(); //ngày hiện tại
                    ItemFilter.TypeTime = nameof(TypeTime.Thang);
                    // đọc giá tri câu query
                    var uri = _navManager?.ToAbsoluteUri(_navManager.Uri);
                    if (uri != null && QueryHelpers.ParseQuery(uri.Query).Count > 0)
                    {
                        string key = uri.Query.Substring(5); // để tránh parse lỗi;    
                        //Dictionary<string, string> pParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(EncryptHelper.Decrypt(key));
                        //if (pParams != null && pParams.Any() && pParams.ContainsKey("pStatusId")) ItemFilter.StatusId = pParams["pStatusId"];
                    }
                    //
                    await _progressService!.SetPercent(0.4);
                    await getDataReports();
                    Grid?.Rebind();
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
        void IDisposable.Dispose()
        {
            // Unsubscribe from the event when our component is disposed
            _navManager!.LocationChanged -= LocationChanged;
        }
        async Task setDataBreadCrumChanged(string location = "")
        {
            switch (location)
            {
                case "/report-purchase":
                    pReportType = nameof(ReportType.DoanhThuKhachHangMuaHangLienHe);
                    break;
            }
            ItemFilter.Type = pReportType;
            pReportTypeName = ListTypeReports?.FirstOrDefault(m => m.Code == pReportType)?.Name + "";
            ListBreadcrumbs = new List<BreadcrumbModel>
                    {
                        new BreadcrumbModel() { Text = "Trang chủ", IsShowIcon = true, Icon = "fa-solid fa-house-chimney" },
                        new BreadcrumbModel() { Text = "Báo cáo" },
                        new BreadcrumbModel() { Text = pReportTypeName }
                    };
            await NotifyBreadcrumb.InvokeAsync(ListBreadcrumbs);
            await getDataReports();
            await InvokeAsync(StateHasChanged);
        }

        async void LocationChanged(object sender, LocationChangedEventArgs e)
        {
            try
            {
                // Cập nhật giá trị khi location thay đổi
                currentLocation = e.Location;
                if (currentLocation != null)
                {
                    // currentLocation = currentLocation.ToUpper();
                    if (currentLocation.Contains("/report-lm"))
                    {
                        await setDataBreadCrumChanged("/report-lm");
                    }
                    else if (currentLocation.Contains("/report-book"))
                    {
                        await setDataBreadCrumChanged("/report-book");
                    }
                    else if (currentLocation.Contains("/report-kindbook"))
                    {
                        await setDataBreadCrumChanged("/report-kindbook");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ReportController", "LocationChanged");
                ShowError(ex.Message);
            }
        }

        private async Task getDataReports()
        {
            ListReports = new List<ReportModel>();
            ItemFilter.UserId = pUserId;

            if (pReportType == nameof(ReportType.@DoanhThuKhachHangMuaHangLienHe))
            {
                if (pServiceType == nameof(ServiceType.Purchase))
                {
                    if (pKind == nameof(Kind.QuiThang))
                    {
                        ItemFilter.Type = "BaoCaoQuiThangKhachHangMuaHang";
                        TilteReport = "BIỂU ĐỒ SỐ LƯỢNG KHÁCH HÀNG MUA HÀNG";
                    }
                    else if (pKind == nameof(Kind.TuNgayDenNgay))
                    {
                        ItemFilter.Type = "BaoCaoKhachHangMuaHang";
                    }
                }
                else if (pServiceType == nameof(ServiceType.Contact))
                {
                    if (pKind == nameof(Kind.QuiThang))
                    {
                        ItemFilter.Type = "BaoCaoQuiThangKhachHangLienHe";
                        TilteReport = "BIỂU ĐỒ SỐ LƯỢNG KHÁCH HÀNG LIÊN HỆ";
                    }
                    else if (pKind == nameof(Kind.TuNgayDenNgay))
                    {
                        ItemFilter.Type = "BaoCaoKhachHangLienHe";
                    }
                }

            }
            //ItemFilter.IsAdmin = pIsAdmin;
            ItemFilter.Year = pYearDefault;
            ListReports = await _masterDataService!.GetIndexsAsync(ItemFilter);


            ListChart = new List<ReportModel>();

            if (pChart == nameof(ChartReportType.Chart))
            {

                if (ListReports != null && ListReports.Count > 0)
                {
                    foreach (var item in ListReports)
                    {
                        List<object> val = new List<object>();
                        string[] xAxisItems = new string[] { };
                        if (pKind == nameof(Kind.QuiThang) && ItemFilter.TypeTime == nameof(TypeTime.Qui))
                        {
                            val.Add(item.Total_01);
                            val.Add(item.Total_02);
                            val.Add(item.Total_03);
                            val.Add(item.Total_04);
                            xAxisItems = new string[] { "Q1", "Q2", "Q3", "Q4" };
                        }
                        else if (pKind == nameof(Kind.QuiThang) && ItemFilter.TypeTime == nameof(TypeTime.Thang))
                        {
                            val.Add(item.Total_01);
                            val.Add(item.Total_02);
                            val.Add(item.Total_03);
                            val.Add(item.Total_04);
                            val.Add(item.Total_04);
                            val.Add(item.Total_05);
                            val.Add(item.Total_06);
                            val.Add(item.Total_07);
                            val.Add(item.Total_08);
                            val.Add(item.Total_09);
                            val.Add(item.Total_10);
                            val.Add(item.Total_11);
                            val.Add(item.Total_12);
                            xAxisItems = new string[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
                        }
                        if (pReportType == nameof(ReportType.DoanhThuKhachHangMuaHangLienHe))
                        {
                            if (pServiceType == nameof(ServiceType.Purchase))
                            {
                                ListChart.Add(new ReportModel { ListTitle = xAxisItems, ListValue = val, Title = item.UserName + " - " + item.FullName });
                            }
                            else if (pServiceType == nameof(ServiceType.Contact))
                            {
                                ListChart.Add(new ReportModel { ListTitle = xAxisItems, ListValue = val, Title = item.UserName + " - " + item.FullName });
                            }
                        }

                    }
                }
            }

            Grid?.Rebind();
            await InvokeAsync(StateHasChanged);
        }

        #endregion

        #region Protected Functions
        protected async void ReLoadDataHandler()
        {
            try
            {
                if (ItemFilter.FromDate.HasValue && ItemFilter.ToDate.HasValue
                    && ItemFilter.FromDate.Value.Date > ItemFilter.ToDate.Value.Date)
                {
                    ShowWarning("Dữ liệu tìm kiếm không hợp lệ. [Từ ngày] <= [Đến ngày]");
                    return;
                }
                IsInitialDataLoadComplete = false;
                await getDataReports();
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "ReportController", "ReLoadDataHandler");
                ShowError(ex.Message);
            }
            finally
            {
                IsInitialDataLoadComplete = true;
                await InvokeAsync(StateHasChanged);
            }
        }
        #endregion
    }
}
