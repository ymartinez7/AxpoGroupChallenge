namespace AxpoGroupChallenge.Reports.Application.Interfaces
{
    public interface IClockService
    {
        DateTime GetNow(string timeZone);
        DateTime GetToday(string timeZone);
    }
}
