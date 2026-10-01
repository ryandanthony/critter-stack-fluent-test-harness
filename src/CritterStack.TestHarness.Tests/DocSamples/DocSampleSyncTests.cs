// -----------------------------------------------------------------------
// <copyright file="DocSampleSyncTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace CritterStack.TestHarness.Tests.DocSamples;

/// <summary>
/// Checks that every C# sample in the docs still matches the test that backs it, so a doc edit that isn't mirrored
/// in its test (or the reverse) fails the build.
/// </summary>
public sealed partial class DocSampleSyncTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    /// <summary>
    /// Gets every tutorial and how-to page.
    /// </summary>
    /// <value>Paths relative to the repo root.</value>
    public static TheoryData<string> DocPages
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var dir in new[] { "docs/tutorials", "docs/how-to" })
            {
                foreach (var file in Directory.GetFiles(Path.Combine(RepoRoot, dir), "*.md").Order(StringComparer.Ordinal))
                {
                    data.Add(Path.GetRelativePath(RepoRoot, file));
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(DocPages))]
    [Trait("business_value", "Doc code samples can't silently drift from the tests that prove them.")]
    public void Every_csharp_sample_line_appears_in_the_DocSamples_tests(string page)
    {
        var samples = Directory.GetFiles(Path.Combine(RepoRoot, "src/CritterStack.TestHarness.Tests/DocSamples"), "*.cs")
            .Select(File.ReadAllText)
            .SelectMany(text => text.Split('\n'))
            .Select(Normalize)
            .ToHashSet(StringComparer.Ordinal);

        var markdown = File.ReadAllText(Path.Combine(RepoRoot, page));
        var missing = new List<string>();
        foreach (Match block in SampleBlock().Matches(markdown))
        {
            foreach (var line in block.Groups["code"].Value.Split('\n').Select(Normalize))
            {
                if (line.Length > 0 && !samples.Contains(line))
                {
                    missing.Add($"[{block.Groups["test"].Value}] {line}");
                }
            }
        }

        Assert.True(missing.Count == 0, $"{page} has sample lines not found in DocSamples/:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    [Theory]
    [MemberData(nameof(DocPages))]
    [Trait("business_value", "Every Test marker in the docs points at a test that exists.")]
    public void Every_test_marker_names_an_existing_test(string page)
    {
        var tests = string.Concat(Directory.GetFiles(Path.Combine(RepoRoot, "src/CritterStack.TestHarness.Tests/DocSamples"), "*.cs").Select(File.ReadAllText));
        var markdown = File.ReadAllText(Path.Combine(RepoRoot, page));

        var unknown = TestMarker().Matches(markdown)
            .Select(m => m.Groups["test"].Value)
            .Where(name => !tests.Contains(name.Split('.')[^1], StringComparison.Ordinal))
            .ToList();

        Assert.True(unknown.Count == 0, $"{page} names tests that don't exist: {string.Join(", ", unknown)}");
    }

    private static string Normalize(string line) => line.Trim().TrimEnd('\r');

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "global.json")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not find the repo root (global.json).");
    }

    [GeneratedRegex(@"<!-- Test: (?<test>[\w.]+) -->\r?\n```csharp\r?\n(?<code>.*?)```", RegexOptions.Singleline, 1000)]
    private static partial Regex SampleBlock();

    [GeneratedRegex(@"<!-- Test: (?<test>[\w.]+) -->", RegexOptions.None, 1000)]
    private static partial Regex TestMarker();
}
