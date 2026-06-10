namespace Lms.Modules.Assessments.Application;

public sealed record McqImportRowInput(
    string Stem,
    string OptionA,
    string OptionB,
    string OptionC,
    string OptionD,
    string Correct,
    string? Explanation,
    bool IsPyq,
    int? PyqYear,
    string? PyqExam);

public sealed record McqImportPreviewRowDto(
    int RowNumber,
    McqImportRowInput Row,
    bool IsValid,
    IReadOnlyList<string> Errors,
    string? CorrectKey);

public sealed record McqImportPreviewDto(
    int TotalRows,
    int ValidCount,
    int InvalidCount,
    IReadOnlyList<McqImportPreviewRowDto> Rows);

public sealed record McqImportRequest(IReadOnlyList<McqImportRowInput> Rows);

public sealed record McqImportResultDto(
    int ImportedCount,
    int SkippedCount,
    IReadOnlyList<AdminQuestionDto> Questions);
