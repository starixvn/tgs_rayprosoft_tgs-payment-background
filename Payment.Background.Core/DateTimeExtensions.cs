namespace Tgs.Payment.Background;

public static class DateTimeExtensions
{
	public static DateTime CeilingFiveMinutesTime(this DateTime time)
		=> time.FloorFiveMinutesTime().AddMinutes(5D);

	public static DateTime FloorFiveMinutesTime(this DateTime time)
	{

		var floorMinutes = (int)Math.Floor(time.Minute / 5D) * 5;

		var timePoint = new TimeOnly(time.Hour, floorMinutes);

		var calculateTime = new DateTime(DateOnly.FromDateTime(time), timePoint, time.Kind);

		return calculateTime < time
			? calculateTime
			: calculateTime.AddMinutes(-5D);
	}

	public static DateTime FloorHourTime(this DateTime time)
	{
		var timePoint = new TimeOnly(time.Hour, 0);

		var calculateTime = new DateTime(DateOnly.FromDateTime(time), timePoint, time.Kind);

		return calculateTime < time
			? calculateTime
			: calculateTime.AddHours(-1D);
	}

	public static DateTime CeilingHourTime(this DateTime time)
		=> time.FloorHourTime().AddHours(1D);

	public static DateTimeOffset FloorHourTime(this DateTimeOffset time)
	{
		var timePoint = new TimeOnly(time.Hour, 0);

		var calculateTime = new DateTimeOffset(DateOnly.FromDateTime(time.Date), timePoint, time.Offset);

		return calculateTime < time
			? calculateTime
			: calculateTime.AddHours(-1D);
	}

	public static DateTimeOffset CeilingHourTime(this DateTimeOffset time)
		=> time.FloorHourTime().AddHours(1D);
}
