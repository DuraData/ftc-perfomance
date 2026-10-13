namespace FTCERP.Tests;

public class SecurityRegistryPersistenceGovernanceTests
{
    [Fact]
    public void Production_permission_construction_is_confined_to_governed_registry_and_controlled_seed_paths()
    {
        var root = FindRepositoryRoot();
        var productionFiles = new[] { "API", "Application", "Domain", "Infrastructure" }
            .Select(path => new DirectoryInfo(Path.Combine(root.FullName, path)))
            .Where(directory => directory.Exists)
            .SelectMany(directory => directory.GetFiles("*.cs", SearchOption.AllDirectories))
            .ToArray();
        var files = productionFiles
            .Where(file => File.ReadAllText(file.FullName).Contains("new Permission", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root.FullName, file.FullName).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        files.Should().Equal(
            "API/Controllers/SecurityAdministrationController.cs",
            "Infrastructure/Persistence/Seed/DbInitializer.cs",
            "Infrastructure/Persistence/Seed/SecurityRegistrySeeder.cs");

        var directBulkMutationFiles = productionFiles
            .Where(file =>
            {
                var source = File.ReadAllText(file.FullName);
                return source.Contains("Permissions.Update", StringComparison.Ordinal)
                    || source.Contains("Permissions.Remove", StringComparison.Ordinal)
                    || source.Contains("Permissions.ExecuteUpdate", StringComparison.Ordinal)
                    || source.Contains("Permissions.ExecuteDelete", StringComparison.Ordinal);
            })
            .Select(file => Path.GetRelativePath(root.FullName, file.FullName).Replace('\\', '/'))
            .ToArray();
        directBulkMutationFiles.Should().BeEmpty("production permission changes must pass through the tracked persistence guard");
    }

    [Theory]
    [InlineData(nameof(SecurityResource))]
    [InlineData(nameof(SecurityActionDefinition))]
    [InlineData(nameof(SecurityMemberDefinition))]
    [InlineData(nameof(SecurityNavigationItem))]
    public async Task Registry_definition_updates_require_same_transaction_audit(string definitionType)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var entity = Definition(definitionType);
        context.Add(entity);
        await context.SaveChangesAsync();

        ApplyAllowedChange(entity);

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*same-transaction reasoned before/after audit evidence*");
    }

    [Theory]
    [InlineData(nameof(SecurityResource))]
    [InlineData(nameof(SecurityActionDefinition))]
    [InlineData(nameof(SecurityMemberDefinition))]
    [InlineData(nameof(SecurityNavigationItem))]
    public async Task Registry_definition_identity_cannot_be_rewritten_even_with_audit(string definitionType)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var actor = IdpTestFixture.CreateUser($"registry-identity-{definitionType}");
        var entity = Definition(definitionType);
        context.AddRange(actor, entity);
        await context.SaveChangesAsync();

        ApplyIdentityChange(entity);
        context.AuditTrails.Add(Audit(definitionType, PublicId(entity), actor.Id));

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*immutable*");
    }

    [Theory]
    [InlineData(nameof(SecurityResource))]
    [InlineData(nameof(SecurityActionDefinition))]
    [InlineData(nameof(SecurityMemberDefinition))]
    [InlineData(nameof(SecurityNavigationItem))]
    public async Task Registry_definition_history_cannot_be_hard_deleted(string definitionType)
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var entity = Definition(definitionType);
        context.Add(entity);
        await context.SaveChangesAsync();

        context.Remove(entity);

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Dynamic security registry history cannot be hard deleted*");
    }

    [Fact]
    public async Task Registry_definition_updates_accept_exact_atomic_audits_and_advance_concurrency_versions()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var actor = IdpTestFixture.CreateUser("registry-governance-actor");
        var resource = (SecurityResource)Definition(nameof(SecurityResource));
        var action = (SecurityActionDefinition)Definition(nameof(SecurityActionDefinition));
        var member = (SecurityMemberDefinition)Definition(nameof(SecurityMemberDefinition));
        var navigation = (SecurityNavigationItem)Definition(nameof(SecurityNavigationItem));
        context.AddRange(actor, resource, action, member, navigation);
        await context.SaveChangesAsync();
        var originalVersions = new[] { resource.RowVersion, action.RowVersion, member.RowVersion, navigation.RowVersion }
            .Select(value => Convert.ToBase64String(value)).ToArray();

        ApplyAllowedChange(resource);
        ApplyAllowedChange(action);
        ApplyAllowedChange(member);
        ApplyAllowedChange(navigation);
        context.AuditTrails.AddRange(
            Audit(nameof(SecurityResource), resource.PublicId, actor.Id),
            Audit(nameof(SecurityActionDefinition), action.PublicId, actor.Id),
            Audit(nameof(SecurityMemberDefinition), member.PublicId, actor.Id),
            Audit(nameof(SecurityNavigationItem), navigation.PublicId, actor.Id));

        await context.SaveChangesAsync();

        var currentVersions = new[] { resource.RowVersion, action.RowVersion, member.RowVersion, navigation.RowVersion }
            .Select(value => Convert.ToBase64String(value)).ToArray();
        currentVersions.Zip(originalVersions).Should().OnlyContain(pair => pair.First != pair.Second);
        (await context.AuditTrails.CountAsync(item => item.Action == "Update")).Should().Be(4);
    }

    [Fact]
    public async Task Permission_catalogue_projection_cannot_be_rewritten_directly()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var permission = ResourcePermission();
        context.Permissions.Add(permission);
        await context.SaveChangesAsync();

        permission.Description = "Uncontrolled rewrite";

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Permission catalogue projections may change only to the exact value derived from their governed security registry definition*");
    }

    [Fact]
    public async Task Permission_catalogue_identity_cannot_be_rewritten_with_registry_evidence()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var actor = IdpTestFixture.CreateUser("permission-identity-actor");
        var resource = (SecurityResource)Definition(nameof(SecurityResource));
        var permission = ResourcePermission();
        context.AddRange(actor, resource, permission);
        await context.SaveChangesAsync();

        resource.Name = "Updated Resource";
        permission.Code = "GOVERNED_RESOURCE.REWRITTEN";
        context.AuditTrails.Add(Audit(nameof(SecurityResource), resource.PublicId, actor.Id));

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Permission catalogue code and kind are immutable.*");
    }

    [Fact]
    public async Task Permission_catalogue_projection_accepts_atomic_registry_change()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var actor = IdpTestFixture.CreateUser("permission-projection-actor");
        var resource = (SecurityResource)Definition(nameof(SecurityResource));
        var permission = ResourcePermission();
        context.AddRange(actor, resource, permission);
        await context.SaveChangesAsync();

        resource.Name = "Updated Resource";
        permission.Description = "Read Updated Resource";
        context.AuditTrails.Add(Audit(nameof(SecurityResource), resource.PublicId, actor.Id));

        await context.SaveChangesAsync();

        (await context.Permissions.SingleAsync(item => item.Id == permission.Id)).Description.Should().Be("Read Updated Resource");
    }

    [Fact]
    public async Task Permission_catalogue_projection_rejects_forged_value_beside_registry_change()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var actor = IdpTestFixture.CreateUser("permission-forgery-actor");
        var resource = (SecurityResource)Definition(nameof(SecurityResource));
        var permission = ResourcePermission();
        context.AddRange(actor, resource, permission);
        await context.SaveChangesAsync();

        resource.Name = "Updated Resource";
        permission.Description = "Forged projection value";
        context.AuditTrails.Add(Audit(nameof(SecurityResource), resource.PublicId, actor.Id));

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Permission catalogue projections may change only to the exact value derived from their governed security registry definition*");
    }

    [Fact]
    public async Task Permission_catalogue_history_cannot_be_hard_deleted()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var permission = ResourcePermission();
        context.Permissions.Add(permission);
        await context.SaveChangesAsync();

        context.Permissions.Remove(permission);

        var save = () => context.SaveChangesAsync();
        await save.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Permission catalogue history cannot be hard deleted*");
    }

    private static object Definition(string definitionType) => definitionType switch
    {
        nameof(SecurityResource) => new SecurityResource
        {
            Code = "GOVERNED_RESOURCE", Name = "Governed Resource", ResourceType = "ENTITY", EntityTypeName = "GovernedEntity",
            ApiResourceName = "governed-resource", SupportsRead = true, SupportsUpdate = true
        },
        nameof(SecurityActionDefinition) => new SecurityActionDefinition
        {
            Code = "GOVERNED_RESOURCE.EXECUTE", Name = "Execute Governed Resource", ResourceCode = "GOVERNED_RESOURCE"
        },
        nameof(SecurityMemberDefinition) => new SecurityMemberDefinition
        {
            ResourceCode = "GOVERNED_RESOURCE", MemberCode = "ProtectedValue", DisplayName = "Protected Value", IsSensitive = true
        },
        nameof(SecurityNavigationItem) => new SecurityNavigationItem
        {
            Code = "NAV.GOVERNED.RESOURCE", Name = "Governed Resource", Route = "/governed-resource", DisplayOrder = 1
        },
        _ => throw new ArgumentOutOfRangeException(nameof(definitionType))
    };

    private static void ApplyAllowedChange(object entity)
    {
        switch (entity)
        {
            case SecurityResource resource:
                resource.Name = "Updated Resource";
                resource.SupportsExport = true;
                break;
            case SecurityActionDefinition action:
                action.Name = "Updated Action";
                action.IsActive = false;
                break;
            case SecurityMemberDefinition member:
                member.DisplayName = "Updated Member";
                member.IsSensitive = false;
                break;
            case SecurityNavigationItem navigation:
                navigation.Name = "Updated Navigation";
                navigation.DisplayOrder = 2;
                break;
        }
    }

    private static void ApplyIdentityChange(object entity)
    {
        switch (entity)
        {
            case SecurityResource resource:
                resource.Code = "REWRITTEN_RESOURCE";
                break;
            case SecurityActionDefinition action:
                action.ResourceCode = "REWRITTEN_RESOURCE";
                break;
            case SecurityMemberDefinition member:
                member.MemberCode = "RewrittenValue";
                break;
            case SecurityNavigationItem navigation:
                navigation.Code = "NAV.REWRITTEN.RESOURCE";
                break;
        }
    }

    private static Guid PublicId(object entity) => entity switch
    {
        SecurityResource resource => resource.PublicId,
        SecurityActionDefinition action => action.PublicId,
        SecurityMemberDefinition member => member.PublicId,
        SecurityNavigationItem navigation => navigation.PublicId,
        _ => throw new ArgumentOutOfRangeException(nameof(entity))
    };

    private static AuditTrail Audit(string entityName, Guid publicId, string actorId) => new()
    {
        EntityName = entityName,
        EntityId = publicId.ToString(),
        Action = "Update",
        OldValue = "{\"state\":\"before\"}",
        NewValue = "{\"state\":\"after\"}",
        ChangedBy = actorId,
        ChangedAt = DateTime.UtcNow,
        Reason = "Govern registry definition through the approved persistence boundary."
    };

    private static Permission ResourcePermission() => new()
    {
        Code = "GOVERNED_RESOURCE.READ",
        Module = "Resource",
        Feature = "GOVERNED_RESOURCE",
        Action = "Read",
        Kind = SecurityPermissionKind.Resource,
        ResourceCode = "GOVERNED_RESOURCE",
        Operation = SecurityOperation.Read,
        Description = "Read Governed Resource"
    };

    private static DirectoryInfo FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Program.cs"))
                && Directory.Exists(Path.Combine(directory.FullName, "API", "Controllers")))
                return directory;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }
}
