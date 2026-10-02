using FTCERP.Host.Domain.Entities;

namespace FTCERP.Host.Application.Services;

public sealed record WorkflowStageDifference(string Change, string StageCode, int? FromSequence, int? ToSequence, string[] ChangedFields);

public static class WorkflowDefinitionComparer
{
    public static WorkflowStageDifference[] Compare(WorkflowDefinition from, WorkflowDefinition to)
    {
        var fromStages = from.Stages.ToDictionary(stage => stage.Code, StringComparer.OrdinalIgnoreCase);
        var toStages = to.Stages.ToDictionary(stage => stage.Code, StringComparer.OrdinalIgnoreCase);
        var codes = fromStages.Keys.Concat(toStages.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(code => code, StringComparer.OrdinalIgnoreCase);
        var differences = new List<WorkflowStageDifference>();
        foreach (var code in codes)
        {
            var hasFrom = fromStages.TryGetValue(code, out var prior);
            var hasTo = toStages.TryGetValue(code, out var current);
            if (!hasFrom) { differences.Add(new("Added", code, null, current!.Sequence, [])); continue; }
            if (!hasTo) { differences.Add(new("Removed", code, prior!.Sequence, null, [])); continue; }
            var changed = ChangedFields(prior!, current!);
            differences.Add(new(changed.Length == 0 ? "Unchanged" : "Modified", code, prior!.Sequence, current!.Sequence, changed));
        }
        return differences.ToArray();
    }

    private static string[] ChangedFields(WorkflowStageDefinition from, WorkflowStageDefinition to)
    {
        var changed = new List<string>();
        Add(nameof(WorkflowStageDefinition.Name), from.Name, to.Name);
        Add(nameof(WorkflowStageDefinition.Sequence), from.Sequence, to.Sequence);
        Add(nameof(WorkflowStageDefinition.RequiredActionCode), from.RequiredActionCode, to.RequiredActionCode);
        Add(nameof(WorkflowStageDefinition.RequiredPermissionCode), from.RequiredPermissionCode, to.RequiredPermissionCode);
        Add(nameof(WorkflowStageDefinition.IsOptional), from.IsOptional, to.IsOptional);
        Add(nameof(WorkflowStageDefinition.AllowBypass), from.AllowBypass, to.AllowBypass);
        Add(nameof(WorkflowStageDefinition.RequireDifferentActorFromSubmitter), from.RequireDifferentActorFromSubmitter, to.RequireDifferentActorFromSubmitter);
        Add(nameof(WorkflowStageDefinition.RequireDifferentActorFromPreviousStage), from.RequireDifferentActorFromPreviousStage, to.RequireDifferentActorFromPreviousStage);
        Add(nameof(WorkflowStageDefinition.IsTerminal), from.IsTerminal, to.IsTerminal);
        Add(nameof(WorkflowStageDefinition.RejectionStageCode), from.RejectionStageCode, to.RejectionStageCode);
        Add(nameof(WorkflowStageDefinition.RequiresRating), from.RequiresRating, to.RequiresRating);
        Add(nameof(WorkflowStageDefinition.RatingSchemeId), from.RatingSchemeId, to.RatingSchemeId);
        Add(nameof(WorkflowStageDefinition.IsActive), from.IsActive, to.IsActive);
        return changed.ToArray();

        void Add<T>(string name, T left, T right)
        {
            if (!EqualityComparer<T>.Default.Equals(left, right)) changed.Add(name);
        }
    }
}
