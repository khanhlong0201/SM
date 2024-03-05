using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SM.Models.Shared;
public interface IDateTimeService
{
    DateTime GetCurrentVietnamTime();
    DateTime GetCurrentVietnamDate000();
}
public class VietnamDateTimeService : IDateTimeService
{
    private readonly TimeZoneInfo vietnamTimeZone;
    public VietnamDateTimeService()
    {
        // Lấy múi giờ của Việt Nam
        vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    }

    public DateTime GetCurrentVietnamTime()
    {
        // Lấy thời gian hiện tại theo múi giờ Việt Nam
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
    }


    //lấy ngày hiện tại 0 giờ 0 phút 0 giây
    public DateTime GetCurrentVietnamDate000()
    {
        // Lấy thời gian hiện tại theo múi giờ Việt Nam
        TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

        // Đặt giờ, phút và giây thành 0
        DateTime vietnamDate = new DateTime(vietnamTime.Year, vietnamTime.Month, vietnamTime.Day, 0, 0, 0, vietnamTime.Kind);

        return vietnamDate;
    }


}
