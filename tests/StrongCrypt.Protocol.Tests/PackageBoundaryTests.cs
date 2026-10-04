using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace StrongCrypt.Protocol.Tests;

/// <summary>
/// Structural guards for the V1 hard architecture boundary (SPEC.md).
/// These tests read the project files directly so the boundary is enforced by
/// declared build intent, not by whichever references the compiler happened to
/// emit into an assembly manifest.
/// </summary>
public sealed class PackageBoundaryTests
{
    private const string ProtocolProject = "StrongCrypt.Protocol.csproj";
    private const string EncryptionProject = "StrongCrypt.Encryption.csproj";
    private const string DecryptionProject = "StrongCrypt.Decryption.csproj";

    [Fact]
    public void Encryption_references_Protocol()
    {
        Assert.Contains(ProtocolProject, ProjectReferencesOf(EncryptionProject));
    }

    [Fact]
    public void Decryption_references_Protocol()
    {
        Assert.Contains(ProtocolProject, ProjectReferencesOf(DecryptionProject));
    }

    [Fact]
    public void Encryption_does_not_reference_Decryption()
    {
        Assert.DoesNotContain(DecryptionProject, ProjectReferencesOf(EncryptionProject));
    }

    [Fact]
    public void Decryption_does_not_reference_Encryption()
    {
        Assert.DoesNotContain(EncryptionProject, ProjectReferencesOf(DecryptionProject));
    }

    [Fact]
    public void Protocol_references_no_other_production_project()
    {
        Assert.Empty(ProjectReferencesOf(ProtocolProject));
    }

    [Theory]
    [InlineData(ProtocolProject)]
    [InlineData(EncryptionProject)]
    [InlineData(DecryptionProject)]
    public void Production_project_declares_only_approved_package_references(string projectFileName)
    {
        // V1 production runtime is BCL-only with one approved exception:
        // Microsoft.Extensions.DependencyInjection.Abstractions for optional
        // DI integration (approved 2026-10-05 for SC-T10).
        // Adding other runtime dependencies requires human approval under
        // AGENT_GUARDRAILS.md DEP-01.
        
        IEnumerable<string> packages = ProjectElements(projectFileName, "PackageReference")
            .Select(static e => e.Attribute("Include")?.Value ?? string.Empty);

        string[] approved = { "Microsoft.Extensions.DependencyInjection.Abstractions" };
        IEnumerable<string> unapproved = packages.Except(approved);

        Assert.Empty(unapproved);
    }

    private static List<string> ProjectReferencesOf(string projectFileName)
    {
        return ProjectElements(projectFileName, "ProjectReference")
            .Select(static e => e.Attribute("Include")?.Value ?? string.Empty)
            .Select(static include => Path.GetFileName(include.Replace('\\', '/')))
            .ToList();
    }

    private static IEnumerable<XElement> ProjectElements(string projectFileName, string elementName)
    {
        string projectDirectory = Path.GetFileNameWithoutExtension(projectFileName);
        string path = Path.Combine(RepositoryRoot(), "src", projectDirectory, projectFileName);

        Assert.True(File.Exists(path), $"Expected project file at '{path}'.");

        return XDocument.Load(path).Descendants(elementName);
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "StrongCrypt.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
