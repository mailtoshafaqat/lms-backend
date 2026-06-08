namespace Lms.Modules.Platform.Application;

public sealed record PageSectionDto(
    Guid? Id,
    string SectionType,
    int SortOrder,
    string ContentJson,
    bool IsEnabled);

public sealed record LandingPageDto(string Slug, IReadOnlyList<PageSectionDto> Sections);

public sealed record UpdateLandingPageRequest(IReadOnlyList<PageSectionDto> Sections);

public static class LandingSectionTypes
{
    public const string Hero = "Hero";
    public const string Features = "Features";
    public const string Footer = "Footer";
    public const string Testimonials = "Testimonials";
    public const string CoursesShowcase = "CoursesShowcase";
    public const string Stats = "Stats";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Hero, Features, Footer, Testimonials, CoursesShowcase, Stats
    };
}
