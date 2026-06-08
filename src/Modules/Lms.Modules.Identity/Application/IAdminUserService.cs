using Lms.Shared.Common;

namespace Lms.Modules.Identity.Application;

public interface IAdminUserService
{
    Task<Result<CreatedStudentDto>> CreateStudentAsync(CreateStudentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<StudentListItemDto>> ListStudentsAsync(CancellationToken ct = default);
}
