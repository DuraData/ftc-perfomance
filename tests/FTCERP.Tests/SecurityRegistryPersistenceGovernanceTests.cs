namespace FTCERP.Tests;

public class SecurityRegistryPersistenceGovernanceTests
{
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
}
