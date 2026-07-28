namespace HRManagement.Domain.Enums;

/// <summary>
/// Çalışanın cinsiyeti. Sayılar veritabanında Employees.Gender sütununda saklanır;
/// değiştirilirse mevcut kayıtlar sessizce başka anlama gelir (Seniority ile aynı sözleşme).
/// "Belirtilmemiş" için ayrı değer yoktur: alan boş (null) bırakılır.
/// </summary>
public enum Gender
{
    Male = 1,     // Erkek
    Female = 2    // Kadın
}
