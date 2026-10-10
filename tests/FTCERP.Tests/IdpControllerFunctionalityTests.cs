namespace FTCERP.Tests;

public class IdpControllerFunctionalityTests
{
    [Fact]
    public async Task GetPlansPage_IsBoundedSearchableSortedAndTenantScoped()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var systemTenant = IdpTestFixture.Tenant(null, "system", true);
        var foreignPlanPublicId = Guid.NewGuid();
        await using (var setup = new ApplicationDbContext(options, systemTenant))
        {
            await setup.Database.EnsureCreatedAsync();
            var user = IdpTestFixture.CreateUser("idp-reader");
            setup.AddRange(
                user,
                new Municipality { Id = 71, Code = "M71", Name = "Municipality 71" },
                new Municipality { Id = 72, Code = "M72", Name = "Municipality 72" });
            setup.IdpPlans.AddRange(Enumerable.Range(1, 31).Select(index => new IdpPlan
            {
                MunicipalityId = 71,
                MunicipalityName = "Municipality 71",
                PlanCode = $"IDP-{index:000}",
                PlanTitle = $"Local plan {index:000}",
                StartFinancialYear = 2025 + index,
                EndFinancialYear = 2030 + index,
                EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(index),
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(index),
                CreatedByUserId = user.Id
            }));
            setup.IdpPlans.Add(new IdpPlan
            {
                PublicId = foreignPlanPublicId,
                MunicipalityId = 72,
                MunicipalityName = "Municipality 72",
                PlanCode = "IDP-FOREIGN",
                PlanTitle = "Foreign plan",
                StartFinancialYear = 2026,
                EndFinancialYear = 2031,
                CreatedByUserId = user.Id
            });
            await setup.SaveChangesAsync();
        }

        var tenant = IdpTestFixture.Tenant(71, "idp-reader");
        await using var context = new ApplicationDbContext(options, tenant);
        var controller = IdpTestFixture.CreateController(
            context,
            IdpTestFixture.CreateUserManagerMock(IdpTestFixture.CreateUser("idp-reader")).Object,
            Mock.Of<IWorkflowGovernanceService>(),
            "idp-reader",
            tenant);

        var request = new PagedQueryRequest
        {
            Page = 2,
            PageSize = 10,
            Search = "Local plan",
            SortBy = "planCode",
            SortDirection = "asc"
        };
        controller.GetPlansPage(request).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        var result = await controller.GetPlansPageV1(request);

        var page = Assert.IsType<ApiResponse<PagedResponse<IdpPlanSummaryResponse>>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).Data!;
        Assert.Equal(31, page.TotalCount);
        Assert.Equal(4, page.TotalPages);
        Assert.Equal(10, page.Items.Length);
        Assert.Equal("IDP-011", page.Items[0].PlanCode);
        Assert.DoesNotContain(page.Items, item => item.PlanCode == "IDP-FOREIGN");
        Assert.IsType<NotFoundObjectResult>((await controller.GetPlanVersionsPage(foreignPlanPublicId, new PagedQueryRequest())).Result);
        Assert.IsType<NotFoundObjectResult>((await controller.GetHierarchyPathsPage(foreignPlanPublicId, new PagedQueryRequest())).Result);
    }

    [Fact]
    public async Task GetPlansPage_RejectsUnknownSort()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("idp-reader");
        var controller = IdpTestFixture.CreateController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            Mock.Of<IWorkflowGovernanceService>(),
            user.Id);

        var result = await controller.GetPlansPageV1(new PagedQueryRequest { SortBy = "raw-sql" });

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        controller.GetPlans().Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    [Fact]
    public void PlanAndCollaborationContracts_ExposeOnlyStablePublicIdentity()
    {
        typeof(IdpPlanSummaryResponse).GetProperty("Id").Should().BeNull();
        typeof(IdpPlanVersionResponse).GetProperty("Id").Should().BeNull();
        typeof(IdpPlanVersionResponse).GetProperty("IdpPlanId").Should().BeNull();
        typeof(IdpCommunitySessionResponse).GetProperty("IdpPlanId").Should().BeNull();
        typeof(IdpWardInputResponse).GetProperty("IdpPlanId").Should().BeNull();
        typeof(IdpCommentResponse).GetProperty("IdpPlanId").Should().BeNull();
        typeof(IdpCommentResponse).GetProperty("IdpPlanVersionId").Should().BeNull();
        typeof(IdpTaskResponse).GetProperty("IdpPlanId").Should().BeNull();
        typeof(IdpTaskResponse).GetProperty("IdpPlanVersionId").Should().BeNull();
        typeof(CreateIdpTaskRequest).GetProperty("AssignedToUserId").Should().BeNull();

        typeof(IdpPlanVersionResponse).GetProperty("IdpPlanPublicId").Should().NotBeNull();
        typeof(CreateIdpCommentRequest).GetProperty("IdpPlanVersionPublicId").Should().NotBeNull();
        typeof(CreateIdpTaskRequest).GetProperty("AssignedToUserPublicId").Should().NotBeNull();
    }

    [Fact]
    public async Task PlanLinkedMutations_ResolvePublicIdsWithinTheSelectedTenantAndValidateVersionOwnership()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var actor = IdpTestFixture.CreateUser("idp-public-actor");
        actor.MunicipalityId = 71;
        var assignee = IdpTestFixture.CreateUser("idp-public-assignee");
        assignee.MunicipalityId = 71;
        IdpPlan localPlan;
        IdpPlanVersion localVersion;
        IdpPlan foreignPlan;
        IdpPlanVersion foreignVersion;

        await using (var setup = new ApplicationDbContext(options, IdpTestFixture.Tenant(null, "system", true)))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.AddRange(
                new Municipality { Id = 71, Code = "M71", Name = "Municipality 71" },
                new Municipality { Id = 72, Code = "M72", Name = "Municipality 72" },
                actor,
                assignee);
            localPlan = new IdpPlan
            {
                MunicipalityId = 71, MunicipalityName = "Municipality 71", PlanCode = "IDP-LOCAL",
                PlanTitle = "Local IDP", StartFinancialYear = 2026, EndFinancialYear = 2031,
                CreatedByUserId = actor.Id
            };
            foreignPlan = new IdpPlan
            {
                MunicipalityId = 72, MunicipalityName = "Municipality 72", PlanCode = "IDP-FOREIGN",
                PlanTitle = "Foreign IDP", StartFinancialYear = 2026, EndFinancialYear = 2031,
                CreatedByUserId = actor.Id
            };
            setup.IdpPlans.AddRange(localPlan, foreignPlan);
            await setup.SaveChangesAsync();
            localVersion = new IdpPlanVersion
            {
                IdpPlanId = localPlan.Id, VersionNumber = 1, VersionType = IdpVersionType.Original,
                VersionLabel = "Local original", CreatedByUserId = actor.Id
            };
            foreignVersion = new IdpPlanVersion
            {
                IdpPlanId = foreignPlan.Id, VersionNumber = 1, VersionType = IdpVersionType.Original,
                VersionLabel = "Foreign original", CreatedByUserId = actor.Id
            };
            setup.IdpPlanVersions.AddRange(localVersion, foreignVersion);
            await setup.SaveChangesAsync();
        }

        var tenant = IdpTestFixture.Tenant(71, actor.Id);
        await using var context = new ApplicationDbContext(options, tenant);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(actor, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "test", [], [], []));
        var workflow = new Mock<IWorkflowGovernanceService>();
        var controller = IdpTestFixture.CreateController(
            context,
            IdpTestFixture.CreateUserManagerMock(actor).Object,
            workflow.Object,
            actor.Id,
            tenant,
            access.Object);

        (await controller.CreateCommunitySession(new CreateIdpCommunitySessionRequest(
            foreignPlan.PublicId, "PublicMeeting", DateTime.UtcNow, "Foreign venue", null, 10, null, null))).Result
            .Should().BeOfType<NotFoundObjectResult>();
        (await controller.CreateComment(new CreateIdpCommentRequest(
            localPlan.PublicId, foreignVersion.PublicId, "IdpPlan", localPlan.PublicId.ToString(), "Invalid version"))).Result
            .Should().BeOfType<NotFoundObjectResult>();
        (await controller.CreateTask(new CreateIdpTaskRequest(
            localPlan.PublicId, foreignVersion.PublicId, "Invalid task", "Invalid version", assignee.PublicId, DateTime.UtcNow.AddDays(1)))).Result
            .Should().BeOfType<NotFoundObjectResult>();

        var community = Extract<IdpCommunitySessionResponse>((await controller.CreateCommunitySession(
            new CreateIdpCommunitySessionRequest(localPlan.PublicId, "PublicMeeting", DateTime.UtcNow, "Local venue", null, 25, null, null))).Result!);
        community.IdpPlanPublicId.Should().Be(localPlan.PublicId);

        var comment = Extract<IdpCommentResponse>((await controller.CreateComment(new CreateIdpCommentRequest(
            localPlan.PublicId, localVersion.PublicId, "IdpPlan", localPlan.PublicId.ToString(), "Governed review"))).Result!);
        comment.IdpPlanPublicId.Should().Be(localPlan.PublicId);
        comment.IdpPlanVersionPublicId.Should().Be(localVersion.PublicId);

        var task = Extract<IdpTaskResponse>((await controller.CreateTask(new CreateIdpTaskRequest(
            localPlan.PublicId, localVersion.PublicId, "Governed task", "Review the governed plan", assignee.PublicId, DateTime.UtcNow.AddDays(2)))).Result!);
        task.IdpPlanPublicId.Should().Be(localPlan.PublicId);
        task.IdpPlanVersionPublicId.Should().Be(localVersion.PublicId);
        task.AssignedToUserPublicId.Should().Be(assignee.PublicId);
        task.AssignedToUserPublicId.ToString().Should().NotBe(assignee.Id);
    }

    [Fact]
    public async Task CreatePlan_ShouldCreatePlanAndInitialVersionAndAudit()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id);

        var request = new CreateIdpPlanRequest("Blue Hills", "Integrated Development Plan", "IDP-2026", 2026, 2031);
        controller.CreatePlan(request).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        var actionResult = await controller.CreatePlanV1(request);

        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<IdpPlanSummaryResponse>>().Subject;

        payload.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.PlanCode.Should().Be("IDP-2026");
        payload.Data.PlanFamilyId.Should().NotBeEmpty();
        payload.Data.EffectiveFrom.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));

        (await context.IdpPlans.CountAsync()).Should().Be(1);
        (await context.IdpPlanVersions.CountAsync()).Should().Be(1);
        var initialVersion = await context.IdpPlanVersions.SingleAsync();
        initialVersion.PublicId.Should().NotBeEmpty();
        initialVersion.PredecessorVersionId.Should().BeNull();
        initialVersion.EffectiveFrom.Should().Be(payload.Data.EffectiveFrom);

        workflow.Verify(w => w.QueueAuditTrail(
            "IdpPlan",
            It.IsAny<string>(),
            "Create",
            null,
            It.IsAny<object>(),
            user.Id,
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreatePlan_WithPredecessor_ShouldContinuePlanFamilyLineage()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id);
        var firstEffectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        var firstAction = await controller.CreatePlanV1(new CreateIdpPlanRequest(
            "Blue Hills", "First cycle", "IDP-2026", 2026, 2031, EffectiveFrom: firstEffectiveFrom));
        var first = ((firstAction.Result as OkObjectResult)!.Value as ApiResponse<IdpPlanSummaryResponse>)!.Data!;

        var secondAction = await controller.CreatePlanV1(new CreateIdpPlanRequest(
            "Blue Hills",
            "Second cycle",
            "IDP-2031",
            2031,
            2036,
            first.PublicId,
            firstEffectiveFrom.AddYears(5),
            PublicationReference: "Council resolution 2031/42"));
        var second = ((secondAction.Result as OkObjectResult)!.Value as ApiResponse<IdpPlanSummaryResponse>)!.Data!;

        second.PlanFamilyId.Should().Be(first.PlanFamilyId);
        second.PredecessorPlanPublicId.Should().Be(first.PublicId);
        second.PublicationReference.Should().Be("Council resolution 2031/42");

        var persisted = await context.IdpPlans.Include(plan => plan.PredecessorPlan).SingleAsync(plan => plan.PublicId == second.PublicId);
        persisted.PredecessorPlan!.PublicId.Should().Be(first.PublicId);
    }

    [Fact]
    public async Task UpdatePlan_ShouldSetApprovedMetadata_WhenStatusApproved()
    {
        await using var context = IdpTestFixture.CreateContext();
        var creator = IdpTestFixture.CreateUser("creator");
        var approver = IdpTestFixture.CreateUser("approver", "Approver", "User");
        context.Users.AddRange(creator, approver);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "Original",
            PlanCode = "IDP-APPROVE",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = creator.Id
        };

        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(approver);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, approver.Id);

        var request = new UpdateIdpPlanRequest("Updated", 2027, 2032, "Approved");
        controller.UpdatePlan(plan.Id, request).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        var result = await controller.UpdatePlanByPublicId(plan.PublicId, request);

        result.Result.Should().BeOfType<OkObjectResult>();
        var updated = await context.IdpPlans.SingleAsync();

        updated.PlanTitle.Should().Be("Updated");
        updated.Status.Should().Be(IdpPlanStatus.Approved);
        updated.ApprovedByUserId.Should().Be(approver.Id);
        updated.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePlanVersion_ShouldIncrementVersionAndDeactivatePrevious()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-VERS",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CurrentVersionNumber = 1,
            CreatedByUserId = user.Id
        };

        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        context.IdpPlanVersions.Add(new IdpPlanVersion
        {
            IdpPlanId = plan.Id,
            VersionNumber = 1,
            VersionType = IdpVersionType.Original,
            VersionLabel = "Original",
            IsActive = true,
            CreatedByUserId = user.Id
        });
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) =>
                new AccessDecisionResult(code == "IDP_PLAN.VersionSummary.UPDATE", code == "IDP_PLAN.VersionSummary.UPDATE" ? "allowed" : "denied", [], [], []));
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id, accessControl: access.Object);

        var request = new CreateIdpPlanVersionRequest("AnnualReview", "Annual Review", "2026/2027", "Changes");
        controller.CreatePlanVersion(plan.Id, request).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        var action = await controller.CreatePlanVersionByPublicId(plan.PublicId, request);

        action.Result.Should().BeOfType<OkObjectResult>();

        var versions = await context.IdpPlanVersions.OrderBy(v => v.VersionNumber).ToListAsync();
        versions.Should().HaveCount(2);
        versions[0].IsActive.Should().BeFalse();
        versions[0].EffectiveTo.Should().Be(versions[1].EffectiveFrom);
        versions[1].IsActive.Should().BeTrue();
        versions[1].VersionNumber.Should().Be(2);
        versions[1].PublicId.Should().NotBeEmpty();
        versions[1].PredecessorVersionId.Should().Be(versions[0].Id);

        (await context.IdpPlans.SingleAsync()).CurrentVersionNumber.Should().Be(2);
    }

    [Fact]
    public async Task UpdatePlan_ShouldRequirePublicationReference_WhenPublishing()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("publisher");
        context.Users.Add(user);
        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-PUBLISH",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = user.Id
        };
        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var controller = IdpTestFixture.CreateController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            workflow.Object,
            user.Id);

        var rejected = await controller.UpdatePlanByPublicId(plan.PublicId, new UpdateIdpPlanRequest("IDP", 2026, 2031, "Published"));
        rejected.Result.Should().BeOfType<BadRequestObjectResult>();

        var accepted = await controller.UpdatePlanByPublicId(plan.PublicId, new UpdateIdpPlanRequest(
            "IDP", 2026, 2031, "Published", PublicationReference: "Council resolution 2026/17"));
        accepted.Result.Should().BeOfType<OkObjectResult>();

        var published = await context.IdpPlans.SingleAsync();
        published.Status.Should().Be(IdpPlanStatus.Published);
        published.PublishedAt.Should().NotBeNull();
        published.PublicationReference.Should().Be("Council resolution 2026/17");
    }

    [Fact]
    public async Task IdpLineage_ShouldEnforceRelationalUniquenessAndEffectiveDateConstraints_InSqlite()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);
        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-CONSTRAINT",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = user.Id,
            EffectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        var publicId = Guid.NewGuid();
        context.IdpPlanVersions.Add(new IdpPlanVersion
        {
            PublicId = publicId,
            IdpPlanId = plan.Id,
            VersionNumber = 1,
            VersionType = IdpVersionType.Original,
            VersionLabel = "Original",
            CreatedByUserId = user.Id,
            EffectiveFrom = plan.EffectiveFrom
        });
        await context.SaveChangesAsync();

        context.IdpPlanVersions.Add(new IdpPlanVersion
        {
            PublicId = publicId,
            IdpPlanId = plan.Id,
            VersionNumber = 2,
            VersionType = IdpVersionType.Revised,
            VersionLabel = "Duplicate public ID",
            CreatedByUserId = user.Id,
            EffectiveFrom = plan.EffectiveFrom.AddDays(1)
        });

        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
        context.ChangeTracker.Clear();

        var persistedPlan = await context.IdpPlans.SingleAsync();
        persistedPlan.EffectiveTo = persistedPlan.EffectiveFrom;
        await FluentActions.Invoking(() => context.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task HierarchyPathsAndVersions_AreBoundedSearchableAndLegacyAggregateIsRetired()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("creator");
        var owner = IdpTestFixture.CreateUser("owner", "Owner", "Planner");
        context.Users.AddRange(user, owner);

        var dept = new Department { Id = 7, Code = "PLN", Name = "Planning", Description = "Planning" };
        context.Departments.Add(dept);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-HIER",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = user.Id
        };
        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        var outcome = new IdpStrategicOutcome { IdpPlanId = plan.Id, Code = "SO1", Name = "Outcome", Description = "Desc", SortOrder = 1 };
        context.IdpStrategicOutcomes.Add(outcome);
        await context.SaveChangesAsync();

        var objective = new IdpStrategicObjective
        {
            IdpStrategicOutcomeId = outcome.Id,
            Code = "OBJ1",
            Name = "Objective",
            Description = "Desc",
            BaselineValue = 10,
            TargetValue = 20,
            ResponsibleDepartmentId = dept.Id,
            StrategicOwnerUserId = owner.Id,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            BudgetAllocation = 1000,
            SortOrder = 1
        };
        context.IdpStrategicObjectives.Add(objective);
        await context.SaveChangesAsync();

        var priority = new IdpDevelopmentPriority { IdpStrategicObjectiveId = objective.Id, PriorityCode = "PRI", Name = "Priority", Description = "Desc", SortOrder = 1 };
        context.IdpDevelopmentPriorities.Add(priority);
        await context.SaveChangesAsync();

        var programme = new IdpProgramme { IdpDevelopmentPriorityId = priority.Id, ProgrammeCode = "PRG1", Name = "Programme", Description = "Desc", PlannedBudget = 100, ApprovedBudget = 90, ActualExpenditure = 80, ResponsibleDepartmentId = dept.Id };
        context.IdpProgrammes.Add(programme);
        await context.SaveChangesAsync();

        var project = new IdpProject { IdpProgrammeId = programme.Id, ProjectCode = "PROJ1", ProjectName = "Project", Description = "Desc", Category = "Infrastructure", Budget = 200, FundingSource = "Grant", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddMonths(6), Status = IdpProjectStatus.Planned, DepartmentId = dept.Id };
        context.IdpProjects.Add(project);
        await context.SaveChangesAsync();

        var kpi = new IdpKpi { IdpProjectId = project.Id, KpiCode = "KPI-001", KpiName = "KPI 001", Description = "Desc", Formula = "x/y", Baseline = 1, AnnualTarget = 2, FiveYearTarget = 3, ResponsibleDepartmentId = dept.Id, DataSource = "System", ReportingFrequency = "Quarterly", IndicatorType = IdpKpiIndicatorType.Outcome, Circular88Linked = true, TreasuryTidLinked = true };
        context.IdpKpis.Add(kpi);
        context.IdpKpis.AddRange(Enumerable.Range(2, 30).Select(index => new IdpKpi
        {
            IdpProjectId = project.Id,
            KpiCode = $"KPI-{index:000}",
            KpiName = $"KPI {index:000}",
            Description = "Paged hierarchy KPI",
            Formula = "x/y",
            Baseline = 1,
            AnnualTarget = 2,
            FiveYearTarget = 3,
            DataSource = "System",
            ReportingFrequency = "Quarterly",
            IndicatorType = IdpKpiIndicatorType.Outcome
        }));
        context.IdpPlanVersions.AddRange(
            new IdpPlanVersion { IdpPlanId = plan.Id, VersionNumber = 1, VersionLabel = "Original council plan", VersionType = IdpVersionType.Original, EffectiveFrom = DateTime.UtcNow.AddYears(-2), IsActive = false, CreatedByUserId = user.Id },
            new IdpPlanVersion { IdpPlanId = plan.Id, VersionNumber = 2, VersionLabel = "Annual review one", VersionType = IdpVersionType.AnnualReview, EffectiveFrom = DateTime.UtcNow.AddYears(-1), IsActive = false, CreatedByUserId = user.Id },
            new IdpPlanVersion { IdpPlanId = plan.Id, VersionNumber = 3, VersionLabel = "Annual review current", VersionType = IdpVersionType.AnnualReview, EffectiveFrom = DateTime.UtcNow, IsActive = true, CreatedByUserId = user.Id });
        await context.SaveChangesAsync();

        context.IdpAnnualTargets.Add(new IdpAnnualTarget { IdpKpiId = kpi.Id, FinancialYear = 2026, TargetValue = 2, ActualValue = 1.8m, ProgressComment = "On track" });
        context.IdpAlignmentLinks.Add(new IdpAlignmentLink { IdpStrategicObjectiveId = objective.Id, FrameworkType = AlignmentFrameworkType.NationalDevelopmentPlan, FrameworkReferenceCode = "NDP-1", FrameworkReferenceTitle = "NDP Ref" });
        context.IdpRiskLinks.Add(new IdpRiskLink { IdpStrategicObjectiveId = objective.Id, RiskReference = "R1", RiskTitle = "Funding risk", RiskLevel = IdpRiskLevel.High });
        context.IdpBudgetSnapshots.Add(new IdpBudgetSnapshot { IdpStrategicObjectiveId = objective.Id, FinancialYear = 2026, PlannedBudget = 1000, ApprovedBudget = 900, ActualExpenditure = 500, SourceSystem = "FMS" });
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id);

        var pathsResult = await controller.GetHierarchyPathsPage(plan.PublicId, new PagedQueryRequest
        {
            Page = 2,
            PageSize = 10,
            Search = "KPI",
            SortBy = "kpi",
            SortDirection = "asc"
        });
        var paths = pathsResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<IdpHierarchyPathResponse>>>().Subject.Data!;
        paths.TotalCount.Should().Be(31);
        paths.TotalPages.Should().Be(4);
        paths.Items.Should().HaveCount(10);
        paths.Items[0].KpiCode.Should().Be("KPI-011");
        paths.Items[0].OutcomePublicId.Should().Be(outcome.PublicId);
        paths.Items[0].ObjectivePublicId.Should().Be(objective.PublicId);
        paths.Items[0].PriorityPublicId.Should().Be(priority.PublicId);
        paths.Items[0].ProgrammePublicId.Should().Be(programme.PublicId);
        paths.Items[0].ProjectPublicId.Should().Be(project.PublicId);
        paths.Items.Select(item => item.KpiPublicId).Should().OnlyHaveUniqueItems();

        var versionsResult = await controller.GetPlanVersionsPage(plan.PublicId, new PagedQueryRequest
        {
            Page = 1,
            PageSize = 1,
            Search = "Annual",
            SortBy = "versionNumber",
            SortDirection = "desc"
        }, active: true);
        var versions = versionsResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<IdpPlanVersionResponse>>>().Subject.Data!;
        versions.TotalCount.Should().Be(1);
        versions.Items.Should().ContainSingle().Which.VersionNumber.Should().Be(3);

        (await controller.GetHierarchyPathsPage(plan.PublicId, new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetPlanVersionsPage(plan.PublicId, new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        controller.GetHierarchy(plan.Id).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnComputedMetrics()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);

        var municipality = new Municipality { Id = 1, Code = "BLUE", Name = "Blue Hills" };
        var ward = new Ward { Id = 9, MunicipalityId = municipality.Id, Code = "W9", Name = "Ward 9", LegacyMunicipality = "Blue Hills", IsActive = true, CreatedAt = DateTime.UtcNow };
        context.AddRange(municipality, ward);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-DASH",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = user.Id
        };
        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        var outcome = new IdpStrategicOutcome { IdpPlanId = plan.Id, Code = "SO1", Name = "Outcome", Description = "Desc", SortOrder = 1 };
        context.IdpStrategicOutcomes.Add(outcome);
        await context.SaveChangesAsync();

        var objective = new IdpStrategicObjective
        {
            IdpStrategicOutcomeId = outcome.Id,
            Code = "OBJ1",
            Name = "Objective",
            Description = "Desc",
            BaselineValue = 10,
            TargetValue = 20,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            BudgetAllocation = 1000,
            SortOrder = 1
        };
        context.IdpStrategicObjectives.Add(objective);
        await context.SaveChangesAsync();

        var priority = new IdpDevelopmentPriority { IdpStrategicObjectiveId = objective.Id, PriorityCode = "PRI", Name = "Priority", Description = "Desc", SortOrder = 1 };
        context.IdpDevelopmentPriorities.Add(priority);
        await context.SaveChangesAsync();

        var programme = new IdpProgramme { IdpDevelopmentPriorityId = priority.Id, ProgrammeCode = "PRG1", Name = "Programme", Description = "Desc", PlannedBudget = 100, ApprovedBudget = 100, ActualExpenditure = 60 };
        context.IdpProgrammes.Add(programme);
        await context.SaveChangesAsync();

        var project = new IdpProject { IdpProgrammeId = programme.Id, ProjectCode = "PROJ1", ProjectName = "Project", Description = "Desc", Category = "Infra", Budget = 100, FundingSource = "Grant", StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddMonths(6), Status = IdpProjectStatus.InProgress };
        context.IdpProjects.Add(project);
        await context.SaveChangesAsync();

        var kpi = new IdpKpi { IdpProjectId = project.Id, KpiCode = "KPI1", KpiName = "KPI", Description = "Desc", Formula = "x", Baseline = 1, AnnualTarget = 10, FiveYearTarget = 50, DataSource = "System", ReportingFrequency = "Quarterly", IndicatorType = IdpKpiIndicatorType.Output };
        context.IdpKpis.Add(kpi);
        await context.SaveChangesAsync();

        context.IdpAnnualTargets.AddRange(
            new IdpAnnualTarget { IdpKpiId = kpi.Id, FinancialYear = 2026, TargetValue = 10, ActualValue = 12 },
            new IdpAnnualTarget { IdpKpiId = kpi.Id, FinancialYear = 2027, TargetValue = 10, ActualValue = 8 });

        context.IdpRiskLinks.Add(new IdpRiskLink { IdpStrategicObjectiveId = objective.Id, RiskReference = "R1", RiskTitle = "Funding Risk", RiskLevel = IdpRiskLevel.Critical });
        context.IdpBudgetSnapshots.Add(new IdpBudgetSnapshot { IdpProjectId = project.Id, FinancialYear = 2026, PlannedBudget = 1000, ApprovedBudget = 900, ActualExpenditure = 450, SourceSystem = "FMS" });

        var session = new IdpCommunitySession
        {
            IdpPlanId = plan.Id,
            ParticipationType = IdpParticipationType.PublicMeeting,
            SessionDate = DateTime.UtcNow,
            Venue = "Hall",
            WardId = ward.Id,
            ParticipantsCount = 100
        };
        context.IdpCommunitySessions.Add(session);
        await context.SaveChangesAsync();

        context.IdpCommunityNeeds.Add(new IdpCommunityNeed
        {
            IdpCommunitySessionId = session.Id,
            IssueCategory = "Water",
            Description = "Need access",
            PriorityLevel = "High"
        });

        context.IdpAlignmentLinks.Add(new IdpAlignmentLink
        {
            IdpStrategicObjectiveId = objective.Id,
            FrameworkType = AlignmentFrameworkType.NationalDevelopmentPlan,
            FrameworkReferenceCode = "NDP-1",
            FrameworkReferenceTitle = "NDP Ref"
        });

        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var dashboardAccess = new Mock<IAccessControlService>();
        dashboardAccess.Setup(item => item.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? _) =>
                new AccessDecisionResult(code is "IDP_INDICATOR.AnnualTargetValue.READ" or "IDP_INDICATOR.AnnualActualValue.READ"
                    or "IDP_PROJECT.BudgetSnapshotPlanned.READ" or "IDP_PROJECT.BudgetSnapshotApproved.READ"
                    or "IDP_PROJECT.BudgetSnapshotActual.READ", "test", [], [], []));
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id, accessControl: dashboardAccess.Object);

        controller.GetDashboard(plan.Id).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        var action = await controller.GetDashboardByPublicId(plan.PublicId);
        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<IdpDashboardResponse>>().Subject;

        payload.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Outcomes.Should().Be(1);
        payload.Data.Objectives.Should().Be(1);
        payload.Data.Projects.Should().Be(1);
        payload.Data.Kpis.Should().Be(1);
        payload.Data.CommunitySessions.Should().Be(1);
        payload.Data.Risks.Should().Be(1);
        payload.Data.TopRiskTitles.Should().Contain("Funding Risk");
        payload.Data.KpiAchievementRate.Should().Be(50);
        payload.Data.WardParticipation.Should().ContainSingle();
        payload.Data.AlignmentCount.Should().Be(1);
    }

    [Fact]
    public async Task Alignment_matrix_uses_public_id_search_filter_sort_and_server_paging()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var user = IdpTestFixture.CreateUser("alignment-reader");
        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills", PlanTitle = "Alignment Plan", PlanCode = "IDP-ALIGN",
            StartFinancialYear = 2026, EndFinancialYear = 2031, CreatedByUserId = user.Id
        };
        var outcome = new IdpStrategicOutcome { IdpPlan = plan, Code = "SO1", Name = "Outcome", Description = "Outcome", SortOrder = 1 };
        var objective = new IdpStrategicObjective
        {
            IdpStrategicOutcome = outcome, Code = "OBJ1", Name = "Objective", Description = "Objective",
            StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddYears(1), SortOrder = 1
        };
        context.AddRange(user, plan, outcome, objective);
        for (var index = 1; index <= 26; index++)
            context.IdpAlignmentLinks.Add(new IdpAlignmentLink
            {
                IdpStrategicObjective = objective,
                FrameworkType = index % 2 == 0 ? AlignmentFrameworkType.Circular88 : AlignmentFrameworkType.NationalDevelopmentPlan,
                FrameworkReferenceCode = $"REF{index:00}", FrameworkReferenceTitle = $"Reference {index:00}"
            });
        await context.SaveChangesAsync();
        var controller = IdpTestFixture.CreateController(context, IdpTestFixture.CreateUserManagerMock(user).Object,
            new Mock<IWorkflowGovernanceService>().Object, user.Id);

        var action = await controller.GetAlignmentMatrixPage(plan.PublicId, new PagedQueryRequest
        {
            Page = 2, PageSize = 10, Search = "Reference", SortBy = "reference", SortDirection = "asc"
        });
        var page = ((action.Result as OkObjectResult)!.Value as ApiResponse<PagedResponse<IdpAlignmentMatrixItemResponse>>)!.Data!;
        page.TotalCount.Should().Be(26);
        page.TotalPages.Should().Be(3);
        page.Items.Should().HaveCount(10);
        page.Items.First().FrameworkReferenceCode.Should().Be("REF11");

        var filtered = await controller.GetAlignmentMatrixPage(plan.PublicId,
            new PagedQueryRequest { PageSize = 100 }, "Circular88");
        var filteredPage = ((filtered.Result as OkObjectResult)!.Value as ApiResponse<PagedResponse<IdpAlignmentMatrixItemResponse>>)!.Data!;
        filteredPage.TotalCount.Should().Be(13);
        filteredPage.Items.Should().OnlyContain(item => item.FrameworkType == "Circular88");
        (await controller.GetAlignmentMatrixPage(plan.PublicId, new PagedQueryRequest { SortBy = "raw-sql" })).Result
            .Should().BeOfType<BadRequestObjectResult>();
        controller.GetAlignmentMatrix(plan.Id).Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    [Fact]
    public async Task GenerateReport_ShouldSupportPdfExcelWordFormats()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-RPT",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = user.Id
        };

        var outcome = new IdpStrategicOutcome { IdpPlan = plan, Code = "SO1", Name = "Inclusive growth", SortOrder = 1 };
        var objective = new IdpStrategicObjective
        {
            IdpStrategicOutcome = outcome, Code = "OBJ1", Name = "Reliable water", StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(5), SortOrder = 1
        };
        var priority = new IdpDevelopmentPriority { IdpStrategicObjective = objective, PriorityCode = "PRI1", Name = "Water", SortOrder = 1 };
        var programme = new IdpProgramme { IdpDevelopmentPriority = priority, ProgrammeCode = "PRG1", Name = "Water programme" };
        var project = new IdpProject
        {
            IdpProgramme = programme, ProjectCode = "PROJ1", ProjectName = "Pipeline upgrade", Budget = 1250000,
            StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddYears(2), Status = IdpProjectStatus.InProgress
        };
        var kpi = new IdpKpi
        {
            IdpProject = project, KpiCode = "KPI1", KpiName = "Households connected", AnnualTarget = 250,
            FiveYearTarget = 1250, ReportingFrequency = "Quarterly"
        };

        context.AddRange(plan, outcome, objective, priority, programme, project, kpi);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var access = new Mock<IAccessControlService>();
        access.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "Allowed", [], [], []));
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id, accessControl: access.Object);

        controller.GenerateReport(plan.Id, "annual", "pdf").Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
        var pdf = await controller.GenerateReportByPublicId(plan.PublicId, "annual", "pdf");
        var excel = await controller.GenerateReportByPublicId(plan.PublicId, "annual", "excel");
        var word = await controller.GenerateReportByPublicId(plan.PublicId, "annual", "word");

        var pdfPayload = ((pdf.Result as OkObjectResult)!.Value as ApiResponse<IdpReportDocumentResponse>)!;
        var excelPayload = ((excel.Result as OkObjectResult)!.Value as ApiResponse<IdpReportDocumentResponse>)!;
        var wordPayload = ((word.Result as OkObjectResult)!.Value as ApiResponse<IdpReportDocumentResponse>)!;

        pdfPayload.Data!.ContentType.Should().Be("application/pdf");
        excelPayload.Data!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        wordPayload.Data!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var pdfBytes = Convert.FromBase64String(pdfPayload.Data.ContentBase64);
        var excelBytes = Convert.FromBase64String(excelPayload.Data.ContentBase64);
        var wordBytes = Convert.FromBase64String(wordPayload.Data.ContentBase64);
        System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5).Should().Be("%PDF-");
        excelBytes.Take(2).Should().Equal((byte)'P', (byte)'K');
        wordBytes.Take(2).Should().Equal((byte)'P', (byte)'K');
        pdfPayload.Data.SizeInBytes.Should().Be(pdfBytes.LongLength);
        pdfPayload.Data.Sha256.Should().MatchRegex("^[0-9a-f]{64}$");

        using (var archive = new System.IO.Compression.ZipArchive(new MemoryStream(excelBytes), System.IO.Compression.ZipArchiveMode.Read))
        {
            archive.GetEntry("[Content_Types].xml").Should().NotBeNull();
            archive.GetEntry("xl/workbook.xml").Should().NotBeNull();
            var worksheet = archive.GetEntry("xl/worksheets/sheet1.xml");
            worksheet.Should().NotBeNull();
            using var reader = new StreamReader(worksheet!.Open());
            var xml = await reader.ReadToEndAsync();
            xml.Should().Contain("KPI1 - Households connected").And.Contain("1250000.00").And.Contain("250");
        }
        using (var archive = new System.IO.Compression.ZipArchive(new MemoryStream(wordBytes), System.IO.Compression.ZipArchiveMode.Read))
        {
            archive.GetEntry("[Content_Types].xml").Should().NotBeNull();
            archive.GetEntry("word/document.xml").Should().NotBeNull();
        }

        var deniedAccess = new Mock<IAccessControlService>();
        deniedAccess.Setup(service => service.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(false, "Denied", [], [], []));
        var deniedController = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id, accessControl: deniedAccess.Object);
        var maskedExcel = await deniedController.GenerateReportByPublicId(plan.PublicId, "annual", "excel");
        var maskedPayload = ((maskedExcel.Result as OkObjectResult)!.Value as ApiResponse<IdpReportDocumentResponse>)!;
        using (var archive = new System.IO.Compression.ZipArchive(
            new MemoryStream(Convert.FromBase64String(maskedPayload.Data!.ContentBase64)), System.IO.Compression.ZipArchiveMode.Read))
        {
            using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
            var xml = await reader.ReadToEndAsync();
            xml.Should().Contain("KPI1 - Households connected");
            xml.Should().NotContain(">1250000.00<").And.NotContain(">250<");
        }

        (await controller.GenerateReportByPublicId(plan.PublicId, "unsupported", "pdf")).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GenerateReportByPublicId(plan.PublicId, "annual", "txt")).Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateTask_AndCompleteTask_ShouldPersistAndNotify()
    {
        await using var context = IdpTestFixture.CreateRelationalContext();
        var creator = IdpTestFixture.CreateUser("creator");
        var assignee = IdpTestFixture.CreateUser("assignee", "Assigned", "Person");
        context.Users.AddRange(creator, assignee);

        var plan = new IdpPlan
        {
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-TASK",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = creator.Id
        };

        context.IdpPlans.Add(plan);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var usersById = new Dictionary<string, ApplicationUser>
        {
            [creator.Id] = creator,
            [assignee.Id] = assignee
        };

        var userManager = IdpTestFixture.CreateUserManagerMock(creator, usersById);
        var taskAccess = new Mock<IAccessControlService>();
        taskAccess.Setup(item => item.CheckPermissionAsync(creator, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync(new AccessDecisionResult(true, "test", [], [], []));
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, creator.Id, accessControl: taskAccess.Object);

        var createResult = await controller.CreateTask(new CreateIdpTaskRequest(
            plan.PublicId,
            null,
            "Review draft IDP",
            "Review and provide comments",
            assignee.PublicId,
            DateTime.UtcNow.AddDays(7)));

        var createPayload = createResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<IdpTaskResponse>>().Subject.Data!;
        var task = await context.IdpTaskAssignments.SingleAsync();
        task.IsCompleted.Should().BeFalse();
        createPayload.PublicId.Should().Be(task.PublicId);
        createPayload.IdpPlanPublicId.Should().Be(plan.PublicId);
        createPayload.IdpPlanVersionPublicId.Should().BeNull();
        createPayload.AssignedToUserPublicId.Should().Be(assignee.PublicId);
        createPayload.RowVersion.Should().Be(Convert.ToBase64String(task.RowVersion));

        workflow.Verify(w => w.CreateNotificationAsync(
            assignee.Id,
            NotificationType.Submission,
            It.IsAny<string>(),
            It.IsAny<string>(),
            "IdpTask",
            task.PublicId.ToString()), Times.Once);

        task.PublicId.Should().NotBeEmpty();
        task.RowVersion.Should().NotBeEmpty();
        var originalVersion = Convert.ToBase64String(task.RowVersion);
        var legacyResult = controller.CompleteTask(task.Id, new CompleteIdpTaskRequest(true, originalVersion, "Reviewed and accepted"));
        legacyResult.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);

        var completeResult = await controller.CompleteTaskByPublicId(task.PublicId,
            new CompleteIdpTaskRequest(true, originalVersion, "Reviewed and accepted"));
        completeResult.Result.Should().BeOfType<OkObjectResult>();

        var updated = await context.IdpTaskAssignments.SingleAsync();
        updated.IsCompleted.Should().BeTrue();
        updated.CompletedAt.Should().NotBeNull();
        updated.RowVersion.Should().NotEqual(Convert.FromBase64String(originalVersion));
        workflow.Verify(service => service.WriteAuditTrailAsync(
            "IdpTask", task.PublicId.ToString(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>(), creator.Id, It.IsAny<string?>()),
            Times.Exactly(2));

        var staleResult = await controller.CompleteTaskByPublicId(task.PublicId,
            new CompleteIdpTaskRequest(false, originalVersion, "Returned for further work"));
        staleResult.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateDomainRecords_ShouldReturnNotFound_WhenParentsMissing()
    {
        await using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser("creator");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var workflow = new Mock<IWorkflowGovernanceService>();
        var userManager = IdpTestFixture.CreateUserManagerMock(user);
        var controller = IdpTestFixture.CreateController(context, userManager.Object, workflow.Object, user.Id);

        var projectResult = await controller.CreateProject(new CreateIdpProjectRequest(
            IdpProgrammeId: -1,
            ProjectCode: "P1",
            ProjectName: "Test",
            Description: "desc",
            Category: "cat",
            DepartmentId: null,
            Budget: 100,
            FundingSource: "Grant",
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddMonths(1),
            Status: "Planned",
            CommunityNeedReference: null));

        var kpiResult = await controller.CreateKpi(new CreateIdpKpiRequest(
            IdpProjectId: -1,
            KpiCode: "K1",
            KpiName: "KPI",
            Description: "desc",
            Formula: "x",
            Baseline: 1,
            AnnualTarget: 2,
            FiveYearTarget: 3,
            ResponsibleDepartmentId: null,
            DataSource: "sys",
            ReportingFrequency: "Quarterly",
            IndicatorType: "Outcome",
            Circular88Linked: false,
            TreasuryTidLinked: false));

        projectResult.Result.Should().BeOfType<NotFoundObjectResult>();
        kpiResult.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void LegacyDocumentMetadataEndpoint_ShouldRejectClientSuppliedStoragePath()
    {
        using var context = IdpTestFixture.CreateContext();
        var user = IdpTestFixture.CreateUser();
        var controller = IdpTestFixture.CreateController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            Mock.Of<IWorkflowGovernanceService>(),
            user.Id);

        var result = controller.CreateDocument(new CreateIdpDocumentRequest(
            Guid.NewGuid(), null, "Governance", "Unsafe metadata", "proof.pdf", "../../outside.pdf",
            "application/pdf", 42, 1, true));

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status410Gone);
        context.IdpDocuments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDocumentsPage_ShouldBoundFilterAndReleaseOnlyCleanDocuments()
    {
        var user = IdpTestFixture.CreateUser();
        var tenant = IdpTestFixture.Tenant(71, user.Id);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        IdpPlan plan;
        IdpDocument clean;
        IdpDocument quarantined;
        IdpPlan outsidePlan;
        await using (var setup = new ApplicationDbContext(options, IdpTestFixture.Tenant(null, "system", true)))
        {
        await setup.Database.EnsureCreatedAsync();
        setup.Municipalities.AddRange(
            new Municipality { Id = 71, Code = "M71", Name = "Blue Hills" },
            new Municipality { Id = 72, Code = "M72", Name = "Other Municipality" });
        plan = new IdpPlan
        {
            MunicipalityId = 71,
            MunicipalityName = "Blue Hills",
            PlanTitle = "IDP",
            PlanCode = "IDP-DOC",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = user.Id
        };
        clean = new IdpDocument
        {
            IdpPlan = plan,
            Category = IdpDocumentCategory.Policy,
            Title = "Approved plan",
            FileName = "approved.pdf",
            UploadedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            Blob = new EvidenceBlob { MunicipalityId = 71, StorageKey = "idp/private-clean.pdf", ContentType = "application/pdf", SizeInBytes = 100, Sha256 = new string('a', 64), SignatureVerified = true, ScanStatus = "Clean", IsQuarantined = false, ScannerProvider = "ProtectedScanner", ScannerReference = "protected-reference", ScanDetail = "protected-detail" },
            UploadedByUserId = user.Id,
            UploadedByUser = user
        };
        quarantined = new IdpDocument
        {
            IdpPlan = plan,
            Category = IdpDocumentCategory.Governance,
            Title = "Pending plan",
            FileName = "pending.pdf",
            UploadedAt = new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Utc),
            Blob = new EvidenceBlob { MunicipalityId = 71, StorageKey = "idp/private-pending.pdf", ContentType = "application/pdf", SizeInBytes = 100, Sha256 = new string('b', 64), SignatureVerified = true, ScanStatus = "ScannerUnavailable", IsQuarantined = true },
            UploadedByUserId = user.Id,
            UploadedByUser = user
        };
        outsidePlan = new IdpPlan
        {
            MunicipalityId = 72,
            MunicipalityName = "Other Municipality",
            PlanTitle = "Outside IDP",
            PlanCode = "IDP-OUTSIDE",
            StartFinancialYear = 2026,
            EndFinancialYear = 2031,
            CreatedByUserId = user.Id
        };
        var outsideDocument = new IdpDocument
        {
            IdpPlan = outsidePlan,
            Category = IdpDocumentCategory.Policy,
            Title = "Outside plan",
            FileName = "outside.pdf",
            Blob = new EvidenceBlob { MunicipalityId = 72, StorageKey = "idp/outside.pdf", ContentType = "application/pdf", SizeInBytes = 100, Sha256 = new string('c', 64), SignatureVerified = true, ScanStatus = "Clean" },
            UploadedByUserId = user.Id,
            UploadedByUser = user
        };
        setup.AddRange(user, plan, clean, quarantined, outsidePlan, outsideDocument);
        await setup.SaveChangesAsync();
        }

        await using var context = new ApplicationDbContext(options, tenant);

        var allowedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var access = new Mock<IAccessControlService>();
        access.Setup(item => item.CheckPermissionAsync(user, It.IsAny<string>(), It.IsAny<AccessScopeContext?>()))
            .ReturnsAsync((ApplicationUser _, string code, AccessScopeContext? scope) =>
                new AccessDecisionResult(allowedCodes.Contains(code), allowedCodes.Contains(code) ? "allowed" : "denied", [], [], []));
        var controller = IdpTestFixture.CreateController(
            context,
            IdpTestFixture.CreateUserManagerMock(user).Object,
            Mock.Of<IWorkflowGovernanceService>(),
            user.Id,
            tenant,
            access.Object);
        controller.HttpContext.Request.Scheme = "https";
        controller.HttpContext.Request.Host = new HostString("opms.test");

        var result = await controller.GetDocumentsPage(plan.PublicId,
            new PagedQueryRequest { Page = 1, PageSize = 1, Search = "plan", SortBy = "title", SortDirection = "asc" });
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<PagedResponse<IdpDocumentResponse>>>().Subject;
        payload.Data.Should().NotBeNull();
        var page = payload.Data!;
        page.TotalCount.Should().Be(2);
        page.TotalPages.Should().Be(2);
        page.Items.Should().ContainSingle();
        page.Items[0].PublicId.Should().Be(clean.PublicId);
        page.Items[0].DownloadUrl.Should().Contain(clean.PublicId.ToString());
        page.Items[0].IdpPlanPublicId.Should().Be(plan.PublicId);
        page.Items[0].EvidenceBlobPublicId.Should().Be(clean.Blob.PublicId);
        page.Items[0].IsContentDeleted.Should().BeFalse();
        page.Items[0].UploadedByUserPublicId.Should().BeNull();
        page.Items[0].UploadedByName.Should().BeNull();
        page.Items[0].ScannerProvider.Should().BeNull();
        page.Items[0].ScannerReference.Should().BeNull();
        page.Items[0].ScanDetail.Should().BeNull();

        foreach (var member in new[] { "UploadedByUserId", "UploadedByName", "ScannerProvider", "ScannerReference", "ScanDetail" })
            allowedCodes.Add($"IDP_DOCUMENT.{member}.READ");
        var refreshedResult = await controller.GetDocumentsPage(plan.PublicId,
            new PagedQueryRequest { Page = 1, PageSize = 1, Search = "plan", SortBy = "title", SortDirection = "asc" });
        var refreshed = refreshedResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<IdpDocumentResponse>>>().Subject.Data!.Items.Single();
        refreshed.UploadedByUserPublicId.Should().Be(user.PublicId);
        refreshed.UploadedByName.Should().Be(user.FullName);
        refreshed.ScannerProvider.Should().Be("ProtectedScanner");
        refreshed.ScannerReference.Should().Be("protected-reference");
        refreshed.ScanDetail.Should().Be("protected-detail");
        typeof(IdpDocumentResponse).GetProperty("UploadedByUserId").Should().BeNull();

        var quarantineResult = await controller.GetDocumentsPage(plan.PublicId,
            new PagedQueryRequest { Page = 1, PageSize = 25, SortBy = "scanStatus" },
            category: "Governance", scanStatus: "ScannerUnavailable", quarantined: true);
        var quarantinePage = quarantineResult.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<PagedResponse<IdpDocumentResponse>>>().Subject.Data!;
        quarantinePage.Items.Should().ContainSingle(item => item.PublicId == quarantined.PublicId && item.DownloadUrl == string.Empty);
        quarantinePage.Items.Select(item => item.EvidenceBlobPublicId).Should().OnlyHaveUniqueItems();

        (await controller.GetDocumentsPage(plan.PublicId, new PagedQueryRequest { SortBy = "unsafe" })).Result.Should().BeOfType<BadRequestObjectResult>();
        (await controller.GetDocumentsPage(outsidePlan.PublicId, new PagedQueryRequest())).Result.Should().BeOfType<NotFoundObjectResult>();
        controller.GetDocuments(plan.PublicId).Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status410Gone);
    }

    private static T Extract<T>(ActionResult result) where T : class
    {
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        return ok.Value.Should().BeOfType<ApiResponse<T>>().Subject.Data!;
    }
}
