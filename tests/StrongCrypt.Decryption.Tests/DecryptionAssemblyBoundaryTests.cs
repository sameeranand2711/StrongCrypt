using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StrongCrypt.Decryption.Tests;

/// <summary>
/// Runtime counterpart to the declared project-reference guards: asserts the
/// compiled decryption assembly carries no dependency on encryption
/// capability, including via a transitive path (SPEC.md invariants 7 and 8).
/// </summary>
public sealed class DecryptionAssemblyBoundaryTests
{
    private const string EncryptionAssemblyName = "StrongCrypt.Encryption";
    private const string DecryptionAssemblyName = "StrongCrypt.Decryption";

    [Fact]
    public void Decryption_assembly_does_not_depend_on_Encryption()
    {
        string path = Path.Combine(AppContext.BaseDirectory, DecryptionAssemblyName + ".dll");
        Assert.True(File.Exists(path), $"Expected decryption assembly at '{path}'.");

        Assembly decryption = Assembly.LoadFrom(path);

        Assert.DoesNotContain(
            EncryptionAssemblyName,
            decryption.GetReferencedAssemblies().Select(static a => a.Name));
    }

    [Fact]
    public void Encryption_assembly_is_absent_from_decryption_test_output()
    {
        // A decryption-only consumer must not acquire encryption capability,
        // so the encryption assembly should not appear anywhere in this
        // project's dependency closure.
        string path = Path.Combine(AppContext.BaseDirectory, EncryptionAssemblyName + ".dll");

        Assert.False(
            File.Exists(path),
            $"Encryption assembly must not be present in the decryption test output: '{path}'.");
    }
}
