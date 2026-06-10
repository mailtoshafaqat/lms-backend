using Lms.Shared.Common;

namespace Lms.Modules.QnA.Application;

public interface IDoubtTemplateService
{
    Task<IReadOnlyList<DoubtReplyTemplateDto>> ListAsync(CancellationToken ct = default);
    Task<Result<DoubtReplyTemplateDto>> CreateAsync(CreateDoubtReplyTemplateRequest request, CancellationToken ct = default);
    Task<Result<DoubtReplyTemplateDto>> UpdateAsync(Guid id, UpdateDoubtReplyTemplateRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
