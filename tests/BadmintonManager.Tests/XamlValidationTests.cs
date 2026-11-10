using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using MaterialDesignThemes.Wpf;
using Xunit;

namespace BadmintonManager.Tests;

public class XamlValidationTests
{
    private string GetProjectDirectory()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null)
        {
            var target = Path.Combine(dir.FullName, "BadmintonManager");
            if (Directory.Exists(target))
            {
                return target;
            }
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("BadmintonManager project directory was not found.");
    }

    [Fact]
    public void AllPackIconsInXaml_MustBeValidPackIconKinds()
    {
        string projectDir = GetProjectDirectory();
        var xamlFiles = Directory.GetFiles(projectDir, "*.xaml", SearchOption.AllDirectories);
        var packIconRegex = new Regex(@"<materialDesign:PackIcon[^>]*?Kind=""(?<kind>[^""{}]+)""", RegexOptions.Compiled);

        var invalidIcons = new List<string>();

        foreach (var file in xamlFiles)
        {
            var content = File.ReadAllText(file);
            var matches = packIconRegex.Matches(content);
            foreach (Match match in matches)
            {
                string kind = match.Groups["kind"].Value.Trim();
                if (!Enum.TryParse<PackIconKind>(kind, out _))
                {
                    invalidIcons.Add($"File: {Path.GetFileName(file)}, Invalid Kind: '{kind}'");
                }
            }
        }

        invalidIcons.Should().BeEmpty(string.Join(Environment.NewLine, invalidIcons));
    }

    [Fact]
    public void AllMenuItemsIconsInShellViewModel_MustBeValidPackIconKinds()
    {
        string projectDir = GetProjectDirectory();
        string shellVmFile = Path.Combine(projectDir, "ViewModels", "ShellViewModel.cs");
        File.Exists(shellVmFile).Should().BeTrue();

        var content = File.ReadAllText(shellVmFile);
        var menuItemRegex = new Regex(@"new\s+MenuItem\s*\(\s*""[^""]+""\s*,\s*""(?<icon>[^""]+)""", RegexOptions.Compiled);

        var invalidIcons = new List<string>();
        var matches = menuItemRegex.Matches(content);
        foreach (Match match in matches)
        {
            string icon = match.Groups["icon"].Value.Trim();
            if (!Enum.TryParse<PackIconKind>(icon, out _))
            {
                invalidIcons.Add($"ShellViewModel MenuItem Invalid Icon: '{icon}'");
            }
        }

        invalidIcons.Should().BeEmpty(string.Join(Environment.NewLine, invalidIcons));
    }
}
