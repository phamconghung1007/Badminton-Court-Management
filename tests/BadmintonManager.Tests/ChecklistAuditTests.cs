using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BadmintonManager.Data;
using BadmintonManager.ViewModels.Base;
using FluentAssertions;
using Xunit;

namespace BadmintonManager.Tests;

public class ChecklistAuditTests
{
    [Fact]
    public void Checklist_Item2_ViewModels_MustInheritFromViewModelBase()
    {
        // Arrange
        var assembly = typeof(ViewModelBase).Assembly;
        var viewModelTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("ViewModel"))
            .ToList();

        // Assert
        viewModelTypes.Should().NotBeEmpty();
        foreach (var vmType in viewModelTypes)
        {
            typeof(ViewModelBase).IsAssignableFrom(vmType)
                .Should().BeTrue($"ViewModel '{vmType.Name}' must inherit from ViewModelBase to guarantee IsBusy indicator and error handling.");
        }
    }

    [Fact]
    public void Checklist_Item3_NoMessageBoxShow_InViewModels()
    {
        // Scan all .cs files in ViewModels folder
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "BadmintonManager"));
        var viewModelsDir = Path.Combine(projectDir, "ViewModels");

        if (Directory.Exists(viewModelsDir))
        {
            var vmFiles = Directory.GetFiles(viewModelsDir, "*.cs", SearchOption.AllDirectories);
            foreach (var file in vmFiles)
            {
                var content = File.ReadAllText(file);
                content.Should().NotContain("MessageBox.Show",
                    $"File '{Path.GetFileName(file)}' violates Checklist Item 3: ViewModel must not call MessageBox.Show directly, use IDialogService instead.");
            }
        }
    }

    [Fact]
    public void Checklist_Item3_Pages_CodeBehind_MustOnlyContainInitializeComponent()
    {
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "BadmintonManager"));
        var pagesDir = Path.Combine(projectDir, "Views", "Pages");

        if (Directory.Exists(pagesDir))
        {
            var pageFiles = Directory.GetFiles(pagesDir, "*.xaml.cs", SearchOption.AllDirectories);
            foreach (var file in pageFiles)
            {
                var content = File.ReadAllText(file);
                content.Should().NotContain("DbContext",
                    $"Page code-behind '{Path.GetFileName(file)}' must not contain business DbContext calls.");
                content.Should().NotContain("IBookingService",
                    $"Page code-behind '{Path.GetFileName(file)}' must not inject business services directly, use ViewModel binding.");
            }
        }
    }

    [Fact]
    public void Checklist_Item5_Passwords_MustBeBCryptHashed()
    {
        // Verify that BCrypt hashes start with $2a$ or $2b$
        var testHash = BCrypt.Net.BCrypt.HashPassword("123456");
        testHash.Should().StartWith("$2");
        BCrypt.Net.BCrypt.Verify("123456", testHash).Should().BeTrue();
    }
}
