using Jetar.Core.Common;
using Jetar.Core.Interfaces;
using Jetar.Core.Options;
using Microsoft.Extensions.Options;

namespace Jetar.Infrastructure.Services;

/// <summary>
/// Fayllarni server diskiga saqlaydi (wwwroot/uploads). Ishlab chiqarishda
/// MinIO / S3 ga o'tish uchun shu interfeysning boshqa implementatsiyasi ro'yxatdan o'tkaziladi.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" };

    private readonly StorageOptions _opt;

    public LocalFileStorage(IOptions<StorageOptions> opt) => _opt = opt.Value;

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(ext))
            throw new AppException("Faqat PNG, JPG yoki WEBP formatdagi rasmlar qabul qilinadi.", 400, "invalid_file_type");

        var root = Path.GetFullPath(_opt.LocalRoot);
        var folder = Path.Combine(root, DateTime.UtcNow.ToString("yyyy'/'MM"));
        Directory.CreateDirectory(folder);

        var safeName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, safeName);

        await using (var fs = File.Create(fullPath))
        {
            await content.CopyToAsync(fs, ct);
        }

        var relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
        return $"{_opt.PublicBaseUrl.TrimEnd('/')}/{relative}";
    }

    public Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var prefix = _opt.PublicBaseUrl.TrimEnd('/') + "/";
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;

        var root = Path.GetFullPath(_opt.LocalRoot);
        var relative = url[prefix.Length..];
        var fullPath = Path.GetFullPath(Path.Combine(root, relative));

        // Papkadan chiqib ketishga urinishlarni bloklaymiz.
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;

        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
