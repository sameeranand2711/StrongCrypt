using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StrongCrypt.Encryption.Tests;

/// <summary>
/// Runtime counterpart to the declared project-reference guards: asserts the
/// compiled encryption assembly carries no dependency on decryption
/// capability, including via a transitive path (SPEC.md invariants 7 and 8).
/// </summary>
public sealed class EncryptionAssemblyBoundaryTests
{
    private const string EncryptionAssemblyName = "StrongCrypt.Encryption";
    private const string DecryptionAssemblyName = "StrongCrypt.Decryption";

    [Fact]
    public void Encryption_assembly_does_not_depend_on_Decryption()
    {
        string path = Path.Combine(AppContext.BaseDirectory, EncryptionAssemblyName + ".dll");
        Assert.True(File.Exists(path), $"Expected encryption assembly at '{path}'.");

        Assembly encryption = Assembly.LoadFrom(path);

        Assert.DoesNotContain(
            DecryptionAssemblyName,
            encryption.GetReferencedAssemblies().Select(static a => a.Name));
    }

    [Fact]
    public void Decryption_assembly_is_absent_from_encryption_test_output()
    {
        // An encryption-only consumer must not acquire decryption capability,
        // so the decryption assembly should not appear anywhere in this
        // project's dependency closure.
        string path = Path.Combine(AppContext.BaseDirectory, DecryptionAssemblyName + ".dll");

        Assert.False(
            File.Exists(path),
            $"Decryption assembly must not be present in the encryption test output: '{path}'.");
    }
}
