using System.Reflection;
using NetArchTest.Rules;

namespace OpenBargain.ArchitectureTests;

// Enforces the boundary rules in docs/architecture.md for every module, including ones added later.
public class ModuleBoundaryTests
{
    private const string ModulePrefix = "OpenBargain.Modules.";

    public static TheoryData<string> Modules()
    {
        var data = new TheoryData<string>();
        foreach (var assembly in ModuleAssemblies())
        {
            data.Add(assembly.GetName().Name!);
        }
        return data;
    }

    [Fact]
    public void Modules_are_found() => Assert.NotEmpty(ModuleAssemblies());

    [Theory]
    [MemberData(nameof(Modules))]
    public void Module_references_only_other_modules_contracts(string module)
    {
        var references = Load(module).GetReferencedAssemblies()
            .Select(r => r.Name!)
            .Where(name => name.StartsWith(ModulePrefix, StringComparison.Ordinal));

        var forbidden = references.Where(name => !name.EndsWith(".Contracts", StringComparison.Ordinal));

        Assert.Empty(forbidden);
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_depends_on_nothing_outside(string module)
    {
        var result = Types.InAssembly(Load(module))
            .That().ResideInNamespace($"{module}.Domain")
            .Should().OnlyHaveDependenciesOn("System", $"{module}.Domain", "OpenBargain.SharedKernel")
            .GetResult();

        AssertSuccessful(result);
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Application_does_not_depend_on_infrastructure_or_endpoints(string module)
    {
        var result = Types.InAssembly(Load(module))
            .That().ResideInNamespace($"{module}.Application")
            .ShouldNot().HaveDependencyOnAny($"{module}.Infrastructure", $"{module}.Endpoints")
            .GetResult();

        AssertSuccessful(result);
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Endpoints_do_not_depend_on_infrastructure(string module)
    {
        var result = Types.InAssembly(Load(module))
            .That().ResideInNamespace($"{module}.Endpoints")
            .ShouldNot().HaveDependencyOn($"{module}.Infrastructure")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Contracts_do_not_reference_any_module()
    {
        var contracts = ModuleAssemblyNamesOnDisk()
            .Where(name => name.StartsWith(ModulePrefix, StringComparison.Ordinal) && name.EndsWith(".Contracts", StringComparison.Ordinal));

        foreach (var name in contracts)
        {
            var moduleReferences = Load(name).GetReferencedAssemblies()
                .Select(r => r.Name!)
                .Where(r => r.StartsWith(ModulePrefix, StringComparison.Ordinal) && !r.EndsWith(".Contracts", StringComparison.Ordinal));

            Assert.Empty(moduleReferences);
        }
    }

    // Module projects (not their Contracts), found next to the test assembly.
    private static Assembly[] ModuleAssemblies() =>
        ModuleAssemblyNamesOnDisk()
            .Where(name => name.StartsWith(ModulePrefix, StringComparison.Ordinal) && !name.EndsWith(".Contracts", StringComparison.Ordinal))
            .Select(Load)
            .ToArray();

    private static IEnumerable<string> ModuleAssemblyNamesOnDisk() =>
        Directory.GetFiles(AppContext.BaseDirectory, $"{ModulePrefix}*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>();

    private static Assembly Load(string name) => Assembly.Load(name);

    private static void AssertSuccessful(TestResult result) =>
        Assert.True(result.IsSuccessful,
            "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []));
}
