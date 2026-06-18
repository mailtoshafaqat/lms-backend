using System.Security.Cryptography;
using System.Text;

namespace Lms.Modules.Payments.Application;

/// <summary>Easypaisa hosted checkout (InitialRequest) hash + form fields.</summary>
internal static class EasypaisaCheckoutHelper
{
    public static string SandboxUrl => "https://easypaystg.easypaisa.com.pk/easypay/Index.jsf";
    public static string ProductionUrl => "https://easypay.easypaisa.com.pk/easypay/Index.jsf";

    public static string FormatAmount(decimal amount) =>
        amount.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

    public static string OrderReference(Guid orderId) =>
        orderId.ToString("N")[..Math.Min(20, 20)];

    public static string TimestampPkt() =>
        DateTime.UtcNow.AddHours(5).ToString("yyyyMMdd HHmmss");

    public static IReadOnlyDictionary<string, string> BuildHostedFormFields(
        string storeId,
        string hashKey,
        string postBackUrl,
        string orderRefNum,
        decimal amount)
    {
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["amount"] = FormatAmount(amount),
            ["orderRefNum"] = orderRefNum,
            ["paymentMethod"] = "InitialRequest",
            ["postBackURL"] = postBackUrl,
            ["storeId"] = storeId,
            ["timeStamp"] = TimestampPkt(),
            ["mobileAccountNo"] = string.Empty
        };

        var encrypted = ComputeEncryptedHashRequest(fields, hashKey);
        fields["encryptedHashRequest"] = encrypted;
        return fields;
    }

    /// <summary>AES-128 ECB + PKCS7 padding, Base64 — matches common Easypaisa hosted integrations.</summary>
    public static string ComputeEncryptedHashRequest(
        IReadOnlyDictionary<string, string> fields,
        string hashKey)
    {
        var plain = string.Join("&",
            fields.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}={kv.Value}"));

        var key = NormalizeAesKey(hashKey);
        var padded = Pkcs7Pad(Encoding.UTF8.GetBytes(plain), 16);

        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key;

        var encrypted = aes.EncryptEcb(padded, PaddingMode.None);
        return Convert.ToBase64String(encrypted);
    }

    public static bool IsSuccessResponse(IReadOnlyDictionary<string, string> form)
    {
        if (form.TryGetValue("responseCode", out var code))
        {
            if (code is "0000" or "00" or "000") return true;
        }

        if (form.TryGetValue("transactionStatus", out var status)
            && status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
            return true;

        if (form.TryGetValue("status", out var s)
            && s.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static byte[] NormalizeAesKey(string hashKey)
    {
        var bytes = Encoding.UTF8.GetBytes(hashKey);
        return bytes.Length switch
        {
            16 or 24 or 32 => bytes,
            > 32 => bytes[..32],
            _ => Encoding.UTF8.GetBytes(hashKey.PadRight(16)[..16])
        };
    }

    private static byte[] Pkcs7Pad(byte[] data, int blockSize)
    {
        var pad = blockSize - (data.Length % blockSize);
        var result = new byte[data.Length + pad];
        Buffer.BlockCopy(data, 0, result, 0, data.Length);
        for (var i = data.Length; i < result.Length; i++)
            result[i] = (byte)pad;
        return result;
    }
}
