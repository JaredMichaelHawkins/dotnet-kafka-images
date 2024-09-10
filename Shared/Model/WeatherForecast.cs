namespace Shared.Model;

public record WeatherForecast(Guid Id, DateTime Date, int TemperatureC, string? Summary, string PostCode)
{
}
