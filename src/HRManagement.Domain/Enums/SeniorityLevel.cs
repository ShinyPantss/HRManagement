namespace HRManagement.Domain.Enums;

/// <summary>
/// Çalışanın organizasyondaki kıdem/ünvan seviyesi. ManagerId'den (kişiden kişiye
/// raporlama zinciri) FARKLIDIR: bu bir SINIFLANDIRMADIR — kişinin rütbesi.
///
/// Sayı küçüldükçe kıdem YÜKSELİR (1 = en üst). "Müdür" bilinçli olarak geneldir:
/// farklı departmanların müdürleri için ayrı değer açılmaz.
/// Stajyerler bu enuma dahil değildir (ayrı tabloda, kıdem biriktirmezler).
/// </summary>
public enum SeniorityLevel
{
    GenelMudur = 1,              // GM — şirket geneli, departman üstü
    GenelMudurYardimcisi = 2,    // GMY — departman alanları buradan başlar
    Mudur = 3,                   // Müdür (genel; alt kırılımlar tek isimle)
    MudurYardimcisi = 4,         // Müdür Yardımcısı
    KidemliUzman = 5,            // Kıdemli Uzman
    Uzman = 6                    // Uzman
}
