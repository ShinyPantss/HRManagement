namespace HRManagement.Domain.Enums;

public static class SeniorityLevelExtensions
{
    /// <summary>
    /// Yönetici kademesi mi? Yalnızca bu seviyeler birine YÖNETİCİ (ManagerId hedefi)
    /// olabilir: GM, GMY, Müdür. Müdür Yardımcısı, Kıdemli Uzman ve Uzman bireysel
    /// katkıcıdır — kıdemce daha yüksek olsalar bile kimseye yönetici olamazlar.
    /// (Role'den bağımsızdır: bu şirket org kademesidir, hesap yetkisi değil.)
    /// </summary>
    public static bool IsManagerial(this SeniorityLevel level) =>
        level is SeniorityLevel.GenelMudur
              or SeniorityLevel.GenelMudurYardimcisi
              or SeniorityLevel.Mudur;
}
