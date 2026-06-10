namespace Lms.Modules.Assessments.Application;

public static class McqImportValidator
{
    public const int MaxRows = 200;

    public static McqImportPreviewRowDto ValidateRow(int rowNumber, McqImportRowInput row)
    {
        var errors = new List<string>();
        var stem = row.Stem?.Trim() ?? "";
        var a = row.OptionA?.Trim() ?? "";
        var b = row.OptionB?.Trim() ?? "";
        var c = row.OptionC?.Trim() ?? "";
        var d = row.OptionD?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(stem)) errors.Add("Question stem is required.");
        if (string.IsNullOrWhiteSpace(a)) errors.Add("Option A is required.");
        if (string.IsNullOrWhiteSpace(b)) errors.Add("Option B is required.");
        if (string.IsNullOrWhiteSpace(c)) errors.Add("Option C is required.");
        if (string.IsNullOrWhiteSpace(d)) errors.Add("Option D is required.");

        string? correctKey = null;
        if (!TryResolveCorrectKey(row.Correct, out correctKey))
            errors.Add("Correct answer must be A, B, C, D or 0–3.");

        if (row.IsPyq && row.PyqYear is null or < 1990 or > 2100)
            errors.Add("PYQ rows need a valid pyq_year.");

        return new McqImportPreviewRowDto(
            rowNumber,
            row with
            {
                Stem = stem,
                OptionA = a,
                OptionB = b,
                OptionC = c,
                OptionD = d,
                Explanation = string.IsNullOrWhiteSpace(row.Explanation) ? null : row.Explanation.Trim(),
                PyqExam = string.IsNullOrWhiteSpace(row.PyqExam) ? null : row.PyqExam.Trim()
            },
            errors.Count == 0,
            errors,
            correctKey);
    }

    public static McqImportPreviewDto Preview(IReadOnlyList<McqImportRowInput> rows)
    {
        if (rows.Count > MaxRows)
        {
            return new McqImportPreviewDto(
                rows.Count,
                0,
                rows.Count,
                [new McqImportPreviewRowDto(
                    0,
                    rows[0],
                    false,
                    [$"Maximum {MaxRows} rows per upload."],
                    null)]);
        }

        var previewRows = rows
            .Select((row, i) => ValidateRow(i + 1, row))
            .ToList();

        var valid = previewRows.Count(r => r.IsValid);
        return new McqImportPreviewDto(rows.Count, valid, rows.Count - valid, previewRows);
    }

    public static bool TryResolveCorrectKey(string? correct, out string? key)
    {
        key = null;
        if (string.IsNullOrWhiteSpace(correct)) return false;

        var normalized = correct.Trim().ToUpperInvariant();
        key = normalized switch
        {
            "A" or "0" => "0",
            "B" or "1" => "1",
            "C" or "2" => "2",
            "D" or "3" => "3",
            _ => null
        };
        return key is not null;
    }

    public static CreateQuestionRequest ToCreateRequest(McqImportPreviewRowDto row) =>
        new(
            row.Row.Stem,
            [row.Row.OptionA, row.Row.OptionB, row.Row.OptionC, row.Row.OptionD],
            row.CorrectKey!,
            row.Row.Explanation,
            row.Row.IsPyq,
            row.Row.PyqYear,
            row.Row.PyqExam);
}
