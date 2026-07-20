using HRManagement.Application.DTOs;
using HRManagement.Domain.Entities;

namespace HRManagement.Application.Mapping;

/// <summary>
/// Entity → DTO dönüşümleri tek yerde. Birden fazla handler aynı dönüşümü
/// kullandığı için buraya alındı; alan eklendiğinde tek dosya değişir.
/// DİKKAT: PasswordHash bilerek dışarı verilmez — UserDto'ya asla eklenmemeli.
/// </summary>
public static class UserMapping
{
    public static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive
    };
}
