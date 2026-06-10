using Lms.Shared.Common;

namespace Lms.Modules.Identity.Application;

public interface IAdminUserService
{
    Task<Result<CreatedStudentDto>> CreateStudentAsync(CreateStudentRequest request, CancellationToken ct = default);
    Task<PagedResult<StudentListItemDto>> ListStudentsAsync(
        PagedListQuery query, CancellationToken ct = default);
    Task<Result<StudentListItemDto>> SetStudentStatusAsync(
        Guid userId, bool isActive, CancellationToken ct = default);
    Task<Result<ResetStudentPasswordDto>> ResetStudentPasswordAsync(
        Guid userId, CancellationToken ct = default);

    Task<Result<CreatedTeacherDto>> CreateTeacherAsync(CreateTeacherRequest request, CancellationToken ct = default);
    Task<PagedResult<TeacherListItemDto>> ListTeachersAsync(
        PagedListQuery query, CancellationToken ct = default);
    Task<Result<TeacherListItemDto>> SetTeacherStatusAsync(
        Guid userId, bool isActive, CancellationToken ct = default);
    Task<Result<ResetTeacherPasswordDto>> ResetTeacherPasswordAsync(
        Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentGuardianDto>> ListGuardiansAsync(
        Guid studentUserId, CancellationToken ct = default);

    Task<Result<StudentGuardianDto>> CreateGuardianAsync(
        Guid studentUserId, CreateStudentGuardianRequest request, CancellationToken ct = default);

    Task<Result<StudentGuardianDto>> UpdateGuardianAsync(
        Guid guardianId, UpdateStudentGuardianRequest request, CancellationToken ct = default);

    Task<bool> DeleteGuardianAsync(Guid guardianId, CancellationToken ct = default);

    Task<Result<SendGuardianReportResultDto>> SendGuardianReportAsync(
        Guid studentUserId, Guid guardianId, CancellationToken ct = default);
}
