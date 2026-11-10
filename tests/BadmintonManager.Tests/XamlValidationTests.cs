using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    [Fact]
    public void AllStaticResourcesInXaml_MustBeDefined()
    {
        string projectDir = GetProjectDirectory();
        var xamlFiles = Directory.GetFiles(projectDir, "*.xaml", SearchOption.AllDirectories);

        var keyRegex = new Regex(@"x:Key=""(?<key>[^""]+)""", RegexOptions.Compiled);
        var staticResRegex = new Regex(@"\{StaticResource\s+(?<name>[a-zA-Z0-9_.]+)", RegexOptions.Compiled);

        // Collect global keys from Colors.xaml and Styles.xaml
        var globalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string colorsFile = Path.Combine(projectDir, "Resources", "Colors.xaml");
        string stylesFile = Path.Combine(projectDir, "Resources", "Styles.xaml");

        if (File.Exists(colorsFile))
        {
            foreach (Match m in keyRegex.Matches(File.ReadAllText(colorsFile)))
            {
                globalKeys.Add(m.Groups["key"].Value.Trim());
            }
        }

        if (File.Exists(stylesFile))
        {
            foreach (Match m in keyRegex.Matches(File.ReadAllText(stylesFile)))
            {
                globalKeys.Add(m.Groups["key"].Value.Trim());
            }
        }

        var missingResources = new List<string>();

        foreach (var file in xamlFiles)
        {
            string fileName = Path.GetFileName(file);
            var content = File.ReadAllText(file);

            // Collect local keys in this file
            var localKeys = new HashSet<string>(globalKeys, StringComparer.OrdinalIgnoreCase);
            foreach (Match m in keyRegex.Matches(content))
            {
                localKeys.Add(m.Groups["key"].Value.Trim());
            }

            // Verify each StaticResource reference
            foreach (Match m in staticResRegex.Matches(content))
            {
                string resName = m.Groups["name"].Value.Trim();

                // Material Design resources and system resources are provided by external assemblies
                if (resName.StartsWith("MaterialDesign", StringComparison.OrdinalIgnoreCase) ||
                    resName.StartsWith("System", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!localKeys.Contains(resName))
                {
                    missingResources.Add($"File: {fileName}, Missing StaticResource: '{resName}'");
                }
            }
        }

        missingResources.Should().BeEmpty(string.Join(Environment.NewLine, missingResources));
    }

    [Fact]
    public void AllUserControlsAndPages_CanInitializeComponent_WithoutXamlException()
    {
        Exception? threadException = null;
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new System.Windows.Application();
                    app.Resources.MergedDictionaries.Add(new MaterialDesignThemes.Wpf.BundledTheme
                    {
                        BaseTheme = MaterialDesignThemes.Wpf.BaseTheme.Light,
                        PrimaryColor = MaterialDesignColors.PrimaryColor.Blue,
                        SecondaryColor = MaterialDesignColors.SecondaryColor.Amber
                    });
                    app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml", UriKind.Absolute)
                    });
                    app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/BadmintonManager;component/Resources/Colors.xaml", UriKind.Absolute)
                    });
                    app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/BadmintonManager;component/Resources/Styles.xaml", UriKind.Absolute)
                    });
                }

                var targetAssembly = typeof(BadmintonManager.Views.Pages.CustomerListPage).Assembly;
                var viewTypes = targetAssembly.GetTypes()
                    .Where(t => !t.IsAbstract && (typeof(System.Windows.Controls.UserControl).IsAssignableFrom(t) || typeof(System.Windows.Window).IsAssignableFrom(t)))
                    .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                    .ToList();

                viewTypes.Count.Should().BeGreaterThan(15, "There should be at least 15 view pages/windows in the application");

                foreach (var type in viewTypes)
                {
                    try
                    {
                        Activator.CreateInstance(type);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to instantiate view '{type.FullName}': {ex.InnerException?.Message ?? ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception($"WPF Page/Window initialization failed: {threadException.Message}", threadException);
        }
    }
}
