namespace Tgs.Payment.Background.Models;

public static class DateTimeExtensions
{
	public static DateTime CeilingFiveMinutesTime(this DateTime time)
		=> time.FloorFiveMinutesTime().AddMinutes(5D);

	public static DateTime FloorFiveMinutesTime(this DateTime time)
	{
		var floorMinutes = (int)Math.Floor(time.Minute / 5D) * 5;

		var timePoint = new TimeOnly(time.Hour, floorMinutes);

		return new DateTime(DateOnly.FromDateTime(time), timePoint, time.Kind);
	}

	public static DateTime FloorHourTime(this DateTime time)
	{
		var timePoint = new TimeOnly(time.Hour, 0);

		return new DateTime(DateOnly.FromDateTime(time), timePoint, time.Kind);
	}

	public static DateTime CeilingHourTime(this DateTime time)
		=> time.FloorHourTime().AddHours(1D);
}
