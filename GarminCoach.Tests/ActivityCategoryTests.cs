using GarminCoach.Models;

namespace GarminCoach.Tests;

public sealed class ActivityCategoryTests
{
    [Theory]
    [InlineData("running", ActivityCategory.Running)]
    [InlineData("trail_running", ActivityCategory.Running)]
    [InlineData("treadmill_running", ActivityCategory.Running)]
    [InlineData("cycling", ActivityCategory.Cycling)]
    [InlineData("road_biking", ActivityCategory.Cycling)]
    [InlineData("mountain_biking", ActivityCategory.Cycling)]
    [InlineData("indoor_cycling", ActivityCategory.Cycling)]
    [InlineData("e_bike_mountain", ActivityCategory.Cycling)]
    [InlineData("strength_training", ActivityCategory.Gym)]
    [InlineData("hiit", ActivityCategory.Gym)]
    [InlineData("yoga", ActivityCategory.Gym)]
    [InlineData("indoor_climbing", ActivityCategory.Climbing)]
    [InlineData("rock_climbing", ActivityCategory.Climbing)]
    [InlineData("bouldering", ActivityCategory.Climbing)]
    [InlineData("mountaineering", ActivityCategory.Mountain)]
    [InlineData("ski_touring", ActivityCategory.Mountain)]
    [InlineData("backcountry_skiing", ActivityCategory.Mountain)]
    [InlineData("hiking", ActivityCategory.Mountain)]
    [InlineData("lap_swimming", ActivityCategory.Swimming)]
    [InlineData("open_water_swimming", ActivityCategory.Swimming)]
    [InlineData("walking", ActivityCategory.Walking)]
    [InlineData("speed_walking", ActivityCategory.Walking)]
    [InlineData("badminton", ActivityCategory.Other)]
    [InlineData("", ActivityCategory.Other)]
    [InlineData(null, ActivityCategory.Other)]
    public void Classify_MapsTypeKeyToCategory(string? typeKey, ActivityCategory expected)
    {
        Assert.Equal(expected, ActivityCategoryExtensions.Classify(typeKey));
    }

    [Theory]
    [InlineData(ActivityCategory.Running, "Bieganie")]
    [InlineData(ActivityCategory.Cycling, "Rower")]
    [InlineData(ActivityCategory.Gym, "Siłownia")]
    [InlineData(ActivityCategory.Climbing, "Wspinaczka")]
    [InlineData(ActivityCategory.Mountain, "Góry/alpinizm")]
    public void ToPolish_ReturnsLocalizedLabel(ActivityCategory cat, string expected)
    {
        Assert.Equal(expected, cat.ToPolish());
    }
}
