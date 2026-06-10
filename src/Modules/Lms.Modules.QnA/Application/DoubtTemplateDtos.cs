namespace Lms.Modules.QnA.Application;

public sealed record DoubtReplyTemplateDto(
    Guid Id,
    string Title,
    string Body,
    int Order);

public sealed record CreateDoubtReplyTemplateRequest(string Title, string Body);

public sealed record UpdateDoubtReplyTemplateRequest(string Title, string Body);
