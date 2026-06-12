namespace Lms.Modules.Progress.Application;

public interface IWeaknessQuizService
{
    Task<WeaknessQuizDto?> BuildAsync(Guid userId, int count = 10, CancellationToken ct = default);
    Task<WeaknessQuizResultDto> SubmitAsync(
        Guid userId, SubmitWeaknessQuizRequest request, CancellationToken ct = default);
}
