using Jetar.Application.Interfaces;

namespace Jetar.Infrastructure.Services;

/// <summary>
/// <see cref="IPasswordHasher"/>ning BCrypt implementatsiyasi. Hashlash algoritmi
/// Infrastructure'da qoladi — Application qatlami parol kutubxonasini bilmaydi.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
