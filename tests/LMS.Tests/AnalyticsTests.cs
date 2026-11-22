using FluentAssertions;
using LMS.Application.Analytics;

namespace LMS.Tests;

public class AnalyticsTests
{
    [Theory]
    [InlineData("/teacher/courses/89ca2f72-1d15-413f-bc60-07ca7cd8f9d3/students?x=1", "/teacher/courses/:id/students")]
    [InlineData("/Student/Dashboard", "/student/dashboard")]
    [InlineData("/verify/LMS-4C5301A6", "/verify/:id")]
    [InlineData("/admin/users/12345", "/admin/users/:id")]
    [InlineData("/", "/")]
    [InlineData("/learn-arabic/lesson", "/learn-arabic/lesson")]
    [InlineData("https://evil.example/x", null)]
    [InlineData("//evil.example", null)]
    [InlineData("/<script>", null)]
    [InlineData("", null)]
    public void Normalize_collapses_ids_and_rejects_non_paths(string raw, string? expected)
        => PagePath.Normalize(raw).Should().Be(expected);

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) Mobile/15E148", "mobile")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) Chrome/120 Mobile Safari/537.36", "mobile")]
    [InlineData("Mozilla/5.0 (Linux; Android 13; SM-X700) Chrome/120 Safari/537.36", "tablet")]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X)", "tablet")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120", "desktop")]
    [InlineData(null, "desktop")]
    public void Device_is_read_from_the_user_agent(string? ua, string expected)
        => PagePath.Device(ua).Should().Be(expected);
}
