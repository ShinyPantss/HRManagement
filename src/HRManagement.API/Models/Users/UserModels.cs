namespace HRManagement.API.Models.Users;

/// <summary>
/// Hesap listesi/kartı. PasswordHash BİLİNÇLİ olarak yok — Domain entity'si
/// response'a sızmadığı için hash'in dışarı çıkma yolu da kapalı kalır.
/// </summary>
public sealed record UserResponse(
    int Id,
    string Username,
    string Email,
    int Role,
    bool IsActive);

/// <summary>
/// Hesap güncelleme. Username DEĞİŞTİRİLEMEZ (kimliğin sabit tutamağı),
/// parola bu uçtan geçmez. Role sayısal gelir (1=Admin 2=HR 3=Manager
/// 4=Employee 5=Intern). İşlemi yapan kişi gövdeden değil token'dan okunur.
/// </summary>
public sealed class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public int Role { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Var olan bir kişiye hesap açma isteği. Role sayısal gelir
/// (1=Admin 2=HR 3=Manager 4=Employee 5=Intern). EmployeeId/InternId'den
/// tam olarak biri dolu olmalıdır.
/// </summary>
public sealed class CreateUserForPersonRequest
{
    public CreateUserForPersonRequest(
        string username,
        string email,
        string password,
        int role,
        int? employeeId,
        int? internId)
    {
        Username = username;
        Email = email;
        Password = password;
        Role = role;
        EmployeeId = employeeId;
        InternId = internId;
    }

    public string Username { get; }
    public string Email { get; }
    public string Password { get; }
    public int Role { get; }
    public int? EmployeeId { get; }
    public int? InternId { get; }
}
