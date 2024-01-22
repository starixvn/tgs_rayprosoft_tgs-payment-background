using Tgs.Payment.Background;
using Xunit;

namespace Payment.Background.Core.UnitTests;

public class DateTimeTests
{
	[Fact]
	public void DateTime_取得指定時間落在計算的5分時間區段起始時間()
	{
		// Arrange
		var sut = DateTime.ParseExact("2024-01-17T09:56:00.0000000Z", "o", null);

		var expected = DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null);

		// Act
		var actual = sut.FloorFiveMinutesTime();

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void DateTime_取得指定時間落在計算的5分時間區段結尾時間()
	{
		// Arrange
		var sut = DateTime.ParseExact("2024-01-17T09:56:00.0000000Z", "o", null);

		var expected = DateTime.ParseExact("2024-01-17T10:00:00.0000000Z", "o", null);

		// Act
		var actual = sut.CeilingFiveMinutesTime();

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void DateTime_整5分時間落在計算時間區段的起始時間()
	{
		// Arrange
		var sut = DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null);

		var expected = DateTime.ParseExact("2024-01-17T09:50:00.0000000Z", "o", null);

		// Act
		var actual = sut.FloorFiveMinutesTime();

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void DateTime_整5分時間落在計算時間區段的結尾時間()
	{
		// Arrange
		var sut = DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null);

		var expected = DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null);

		// Act
		var actual = sut.CeilingFiveMinutesTime();

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void DateTime_取得指定時間所在的小時區段()
	{
		// Arrange
		var sut = DateTime.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null);

		var expectedBegin = DateTime.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null);
		var expectedEnd = DateTime.ParseExact("2024-01-17T10:00:00.0000000Z", "o", null);

		// Act
		var actualBegin = sut.FloorHourTime();
		var actualEnd = sut.CeilingHourTime();

		// Assert
		Assert.Equal((expectedBegin, expectedEnd), (actualBegin, actualEnd));
	}

	[Fact]
	public void DateTime_取得整點時間所在的小時區段()
	{
		// Arrange
		var sut = DateTime.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null);

		var expectedBegin = DateTime.ParseExact("2024-01-17T08:00:00.0000000Z", "o", null);
		var expectedEnd = DateTime.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null);

		// Act
		var actualBegin = sut.FloorHourTime();
		var actualEnd = sut.CeilingHourTime();

		// Assert
		Assert.Equal((expectedBegin, expectedEnd), (actualBegin, actualEnd));
	}

	[Fact]
	public void DateTimeOffset_取得指定時間所在的小時區段()
	{
		// Arrange
		var sut = DateTimeOffset.ParseExact("2024-01-17T09:55:00.0000000Z", "o", null);

		var expectedBegin = DateTimeOffset.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null);
		var expectedEnd = DateTimeOffset.ParseExact("2024-01-17T10:00:00.0000000Z", "o", null);

		// Act
		var actualBegin = sut.FloorHourTime();
		var actualEnd = sut.CeilingHourTime();

		// Assert
		Assert.Equal((expectedBegin, expectedEnd), (actualBegin, actualEnd));
	}

	[Fact]
	public void DateTimeOffset_取得整點時間所在的小時區段()
	{
		// Arrange
		var sut = DateTimeOffset.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null);

		var expectedBegin = DateTimeOffset.ParseExact("2024-01-17T08:00:00.0000000Z", "o", null);
		var expectedEnd = DateTimeOffset.ParseExact("2024-01-17T09:00:00.0000000Z", "o", null);

		// Act
		var actualBegin = sut.FloorHourTime();
		var actualEnd = sut.CeilingHourTime();

		// Assert
		Assert.Equal((expectedBegin, expectedEnd), (actualBegin, actualEnd));
	}
}
