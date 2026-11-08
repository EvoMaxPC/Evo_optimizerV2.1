using EvoOptimizer.Infrastructure.Cleanup;

namespace EvoOptimizer.Core.Tests;

public sealed class AdvancedSystemCleanupServiceTests
{
    [Fact]
    public void IsPathWithinRoot_AcceptsDescendantAndRejectsRootItself()
    {
        string root = Path.Combine(Path.GetTempPath(), "EvoCleanupRoot");
        string child = Path.Combine(root, "nested", "cache.bin");

        Assert.True(AdvancedSystemCleanupService.IsPathWithinRoot(child, root));
        Assert.False(AdvancedSystemCleanupService.IsPathWithinRoot(root, root));
    }

    [Fact]
    public void IsPathWithinRoot_RejectsSiblingWithSharedPrefix()
    {
        string root = Path.Combine(Path.GetTempPath(), "EvoCleanupRoot");
        string sibling = Path.Combine(Path.GetTempPath(), "EvoCleanupRoot-Outside", "cache.bin");

        Assert.False(AdvancedSystemCleanupService.IsPathWithinRoot(sibling, root));
    }

    [Theory]
    [InlineData("", "C:\\Temp")]
    [InlineData("C:\\Temp\\file.tmp", "")]
    public void IsPathWithinRoot_RejectsEmptyPaths(string candidate, string root) =>
        Assert.False(AdvancedSystemCleanupService.IsPathWithinRoot(candidate, root));

    [Fact]
    public async Task CleanAsync_RejectsAgeBelowSafeMinimum()
    {
        var service = new AdvancedSystemCleanupService(Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "history.jsonl"));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.CleanAsync([CleanupCategoryId.UserTemporaryFiles], TimeSpan.FromHours(12)));
    }

    [Fact]
    public async Task CleanAsync_RequiresAtLeastOneSelectedCategory()
    {
        var service = new AdvancedSystemCleanupService(Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "history.jsonl"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CleanAsync([], TimeSpan.FromDays(7)));
    }

    [Fact]
    public async Task ScanAsync_ReportsRealCategoriesAndKeepsRegistryReadOnly()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var service = new AdvancedSystemCleanupService(Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "history.jsonl"));

        CleanupScanReport report = await service.ScanAsync();

        Assert.Equal(Enum.GetValues<CleanupCategoryId>().Length, report.Categories.Count);
        Assert.Equal(report.Categories.Sum(category => category.FileCount), report.FileCount);
        Assert.Equal(report.Categories.Sum(category => category.EstimatedBytes), report.EstimatedBytes);
        Assert.Equal(
            report.Categories.SelectMany(category => category.Files).Count(),
            report.Categories.SelectMany(category => category.Files)
                .Select(file => file.FullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());

        CleanupCategoryScanResult registry = Assert.Single(report.Categories,
            category => category.Id == CleanupCategoryId.RegistryDiagnostics);
        Assert.False(registry.IsCleanable);
        Assert.False(registry.IsAvailable);
        Assert.Contains("No existe un verificador", registry.Status, StringComparison.Ordinal);
    }
}
