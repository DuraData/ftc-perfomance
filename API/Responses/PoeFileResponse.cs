namespace FTCERP.Host.API.Responses;

public record PoeFileResponse(
    string Id,
    string SubmissionKind,
    string SubmissionId,
    string FileName,
    string? ContentType,
    long SizeInBytes,
    string UploadedByUserId,
    string? UploadedByName,
    DateTime UploadedAt,
    string Url);
