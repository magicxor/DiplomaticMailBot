using System.Reflection;
using DiplomaticMailBot.Infra.Entities;

namespace DiplomaticMailBot.Tests.Unit.Entities;

[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
public sealed class EntityPropertiesTests
{
    [Test]
    public void AllEntityClasses_GettersAndSetters_ShouldWork()
    {
        // Arrange
        var entityNamespace = typeof(RegisteredChat).Namespace;
        var entityAssembly = typeof(RegisteredChat).Assembly;

        var entityTypes = entityAssembly.GetTypes()
            .Where(t => t.Namespace == entityNamespace
                        && t is { IsClass: true, IsInterface: false } and { IsAbstract: false, IsNested: false }
                        && t.GetMethods().All(m => m.Name != "<Clone>$"))
            .ToList();

        // Act & Assert
        foreach (var entityType in entityTypes)
        {
            TestContext.Progress.WriteLine($"Testing entity: {entityType.Name}");

            // Create an instance of the entity
            var entity = Activator.CreateInstance(entityType);
            Assert.That(entity, Is.Not.Null, $"Failed to create instance of {entityType.Name}");

            // Get all properties
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !(p.GetMethod?.IsVirtual ?? true)) // Ignore virtual properties
                .ToList();

            foreach (var property in properties)
            {
                TestContext.Progress.WriteLine($"  Testing property: {property.Name}");

                // Skip read-only properties (those without a setter)
                if (property.SetMethod == null || !property.SetMethod.IsPublic)
                {
                    TestContext.Progress.WriteLine($"    Skipping read-only property: {property.Name}");
                    continue;
                }

                // Skip properties with required attribute
                var isRequired = property.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), true).Length != 0;
                if (isRequired && !property.PropertyType.IsValueType && property.PropertyType != typeof(string))
                {
                    TestContext.Progress.WriteLine($"    Skipping required reference type property: {property.Name}");
                    continue;
                }

                try
                {
                    // Set a value to the property
                    var testValue = GetTestValue(property.PropertyType);
                    property.SetValue(entity, testValue);

                    // Get the value back and verify it matches
                    var retrievedValue = property.GetValue(entity);
                    Assert.That(retrievedValue, Is.EqualTo(testValue), $"Property {entityType.Name}.{property.Name} getter/setter failed");
                }
                catch (Exception ex) when (ex is not AssertionException)
                {
                    TestContext.Progress.WriteLine($"    Error testing property {property.Name}: {ex.Message}");
                    Assert.Fail($"Exception while testing {entityType.Name}.{property.Name}: {ex.Message}");
                }
            }
        }
    }

    private static object? GetTestValue(Type type)
    {
        if (type == typeof(string))
        {
            return "Test String";
        }

        if (type == typeof(int) || type == typeof(int?))
        {
            return 42;
        }

        if (type == typeof(long) || type == typeof(long?))
        {
            return 42L;
        }

        if (type == typeof(bool) || type == typeof(bool?))
        {
            return true;
        }

        if (type == typeof(DateTime) || type == typeof(DateTime?))
        {
            return new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        if (type == typeof(DateOnly) || type == typeof(DateOnly?))
        {
            return new DateOnly(2023, 1, 1);
        }

        if (type == typeof(TimeOnly) || type == typeof(TimeOnly?))
        {
            return new TimeOnly(12, 0, 0);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
            return Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]));
        }

        // For other types, return null
        return null;
    }
}
