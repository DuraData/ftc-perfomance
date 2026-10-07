namespace FTCERP.Host.API.Responses;

public sealed record TidMemberAccess(
    bool CreatedByUserId,
    DocumentMetadataMemberAccess SourceDocument);
