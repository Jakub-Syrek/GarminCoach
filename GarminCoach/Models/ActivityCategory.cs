namespace GarminCoach.Models;

public enum ActivityCategory
{
    Running,
    Cycling,
    Gym,
    Climbing,
    Mountain,
    Swimming,
    Walking,
    Other
}

public static class ActivityCategoryExtensions
{
    public static string ToPolish(this ActivityCategory c) => c switch
    {
        ActivityCategory.Running => "Bieganie",
        ActivityCategory.Cycling => "Rower",
        ActivityCategory.Gym => "Siłownia",
        ActivityCategory.Climbing => "Wspinaczka",
        ActivityCategory.Mountain => "Góry/alpinizm",
        ActivityCategory.Swimming => "Pływanie",
        ActivityCategory.Walking => "Spacery",
        ActivityCategory.Other => "Inne",
        _ => c.ToString()
    };

    public static ActivityCategory Classify(string? typeKey)
    {
        if (string.IsNullOrWhiteSpace(typeKey)) return ActivityCategory.Other;
        var key = typeKey.ToLowerInvariant();

        if (key.Contains("running") || key == "track_running" || key == "obstacle_run")
            return ActivityCategory.Running;

        if (key.Contains("cycling") || key.Contains("biking") || key == "virtual_ride" ||
            key.StartsWith("e_bike") || key == "handcycling")
            return ActivityCategory.Cycling;

        if (key.Contains("climbing") || key == "bouldering")
            return ActivityCategory.Climbing;

        if (key == "mountaineering" || key.Contains("ski_touring") ||
            key.Contains("backcountry") || key == "alpine_skiing" || key == "ski_mountaineering")
            return ActivityCategory.Mountain;

        if (key == "hiking")
            return ActivityCategory.Mountain;

        if (key == "strength_training" || key == "indoor_cardio" || key == "cardio" ||
            key == "hiit" || key == "pilates" || key == "yoga" || key == "crossfit" ||
            key == "fitness_equipment")
            return ActivityCategory.Gym;

        if (key.Contains("swimming"))
            return ActivityCategory.Swimming;

        if (key.Contains("walking"))
            return ActivityCategory.Walking;

        return ActivityCategory.Other;
    }
}
