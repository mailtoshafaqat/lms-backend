using System.Globalization;
using Lms.Modules.Progress.Domain;
using Lms.Shared.Storage;
using Microsoft.Extensions.Configuration;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Lms.Modules.Progress.Application;

public sealed class CertificatePdfService : ICertificatePdfService
{
    private readonly IFileStorage _files;
    private readonly string _frontendBaseUrl;

    public CertificatePdfService(IFileStorage files, IConfiguration configuration)
    {
        _files = files;
        _frontendBaseUrl = (configuration["App:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
    }

    public async Task<byte[]> RenderAsync(
        CompletionCertificate certificate,
        CertificateTemplate template,
        string tenantSlug,
        CancellationToken ct = default)
    {
        var primary = ParseColor(template.PrimaryColor);
        var logo = await LoadImageAsync(template.LogoUrl, ct);
        var background = await LoadImageAsync(template.BackgroundUrl, ct);
        var signature = await LoadImageAsync(template.SignatureUrl, ct);
        var verifyUrl =
            $"{_frontendBaseUrl}/verify/{Uri.EscapeDataString(certificate.CertificateNumber)}?tenant={Uri.EscapeDataString(tenantSlug)}";
        byte[]? qr = template.ShowQrCode ? GenerateQr(verifyUrl) : null;

        var studentName = string.IsNullOrWhiteSpace(certificate.StudentName)
            ? "Student"
            : certificate.StudentName;
        var institute = string.IsNullOrWhiteSpace(certificate.InstituteName)
            ? "Institute"
            : certificate.InstituteName;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(12).FontColor(Colors.Grey.Darken3));

                page.Background().Layers(layers =>
                {
                    if (background is not null)
                        layers.Layer().Image(background).FitArea();
                    layers.PrimaryLayer().Border(3).BorderColor(primary).Padding(24);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    if (logo is not null)
                        col.Item().AlignCenter().Height(56).Image(logo).FitHeight();

                    col.Item().AlignCenter().Text(template.Title).FontSize(28).Bold().FontColor(primary);
                    col.Item().AlignCenter().Text(template.Subtitle).FontSize(14);

                    col.Item().PaddingTop(12).AlignCenter()
                        .Text(studentName).FontSize(26).Bold().FontColor(Colors.Black);

                    col.Item().AlignCenter().Text("has successfully completed").FontSize(13).Italic();

                    col.Item().PaddingTop(4).AlignCenter()
                        .Text(certificate.BundleTitle).FontSize(18).SemiBold().FontColor(primary);

                    col.Item().PaddingTop(8).AlignCenter()
                        .Text($"Issued {certificate.IssuedAt.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)}")
                        .FontSize(11);

                    col.Item().PaddingTop(16).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            if (signature is not null)
                                left.Item().Height(48).Image(signature).FitHeight();
                            left.Item().PaddingTop(4).Text(template.SignatureLabel).FontSize(10);
                            left.Item().PaddingTop(12).Text($"Certificate no. {certificate.CertificateNumber}")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                            left.Item().Text(institute).FontSize(10).SemiBold();
                        });

                        if (qr is not null)
                        {
                            row.ConstantItem(88).AlignRight().Column(right =>
                            {
                                right.Item().Width(72).Height(72).Image(qr);
                                right.Item().PaddingTop(2).AlignCenter()
                                    .Text("Scan to verify").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    private async Task<byte[]?> LoadImageAsync(string? url, CancellationToken ct)
    {
        var key = ExtractStorageKey(url);
        if (key is null) return null;
        await using var stream = await _files.OpenAsync(key, ct);
        if (stream is null) return null;
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    private static string? ExtractStorageKey(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var trimmed = url.Trim();
        const string marker = "/api/v1/files/";
        var idx = trimmed.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            return Uri.UnescapeDataString(trimmed[(idx + marker.Length)..].TrimStart('/'));
        if (!trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return trimmed.TrimStart('/');
        return null;
    }

    private static byte[] GenerateQr(string url)
    {
        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(5);
    }

    private static string ParseColor(string hex)
    {
        var c = hex?.Trim() ?? "#0b3d91";
        if (!c.StartsWith('#')) c = $"#{c}";
        return c.Length is 4 or 7 or 9 ? c : "#0b3d91";
    }
}
