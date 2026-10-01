// -----------------------------------------------------------------------
// <copyright file="ApiReferenceCoverageTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;

namespace CritterStack.TestHarness.Tests.DocSamples;

/// <summary>
/// Checks that docs/reference/api.md mentions every public type and member of the harness, so a new public API
/// can't ship undocumented.
/// </summary>
public sealed class ApiReferenceCoverageTests
{
    [Fact]
    [Trait("business_value", "The API reference covers the whole public surface; a new public member without docs fails the build.")]
    public void Every_public_type_and_member_is_in_the_api_reference()
    {
        var api = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs/reference/api.md"));
        var options = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs/reference/harness-options.md"));
        var reference = api + options;

        var missing = new List<string>();
        foreach (var type in typeof(TestHarness).Assembly.GetExportedTypes())
        {
            var typeName = type.IsGenericType ? type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)] : type.Name;
            if (!reference.Contains($"`{typeName}", StringComparison.Ordinal))
            {
                missing.Add($"type {typeName}");
            }

            const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var members = type.IsEnum
                ? Enum.GetNames(type)
                : type.GetMembers(Declared)
                    .Where(m => m is MethodInfo { IsSpecialName: false } or PropertyInfo)
                    .Where(m => m.DeclaringType == type && !IsObjectOverride(m))
                    .Select(m => m.Name);

            foreach (var member in members.Distinct(StringComparer.Ordinal))
            {
                if (!reference.Contains($"`{member}", StringComparison.Ordinal))
                {
                    missing.Add($"{typeName}.{member}");
                }
            }
        }

        Assert.True(missing.Count == 0, $"Missing from docs/reference/api.md or harness-options.md: {string.Join(", ", missing)}");
    }

    private static bool IsObjectOverride(MemberInfo member) =>
        member.Name is nameof(ToString) or nameof(Equals) or nameof(GetHashCode) or "GetType";

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "global.json")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not find the repo root (global.json).");
    }
}
