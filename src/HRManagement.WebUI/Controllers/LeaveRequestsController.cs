using ClosedXML.Excel;
using HRManagement.WebUI.Models;
using HRManagement.WebUI.Models.Api.LeaveRequests;
using HRManagement.WebUI.Models.LeaveRequests;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// UI controller'ı: iş yapmaz, Refit istemcileri üzerinden API'yi çağırır ve
/// dönen BaseResponse'u kullanıcıya gösterilecek biçime çevirir.
/// İzin talepleri için ayrıca çalışan listesine ihtiyaç var (dropdown + liste filtresi),
/// bu yüzden IEmployeeApi de enjekte edilir.
/// </summary>
public class LeaveRequestsController : Controller
{
    private readonly ILeaveRequestApi _leaveRequestApi;
    private readonly IEmployeeApi _employeeApi;

    public LeaveRequestsController(ILeaveRequestApi leaveRequestApi, IEmployeeApi employeeApi)
    {
        _leaveRequestApi = leaveRequestApi;
        _employeeApi = employeeApi;
    }

    public async Task<IActionResult> Index(string? range, DateTime? from, DateTime? to) =>
        View(await BuildListAsync(range, from, to));

    /// <summary>
    /// Analitik rapor. Ekranla AYNI filtreyi kullanır (parametreler birebir
    /// aktarılır), böylece "gördüğüm liste" ile "aldığım rapor" ayrışmaz.
    /// Ekranda okunmak üzere tasarlanmıştır; yazdırma bir seçenektir, amaç değil.
    /// </summary>
    public async Task<IActionResult> Report(string? range, DateTime? from, DateTime? to) =>
        View(BuildReport(await BuildListAsync(range, from, to)));

    /// <summary>
    /// Rapor metriklerini TEK yerde hesaplar. View yalnızca gösterir — aynı sayının
    /// iki ekranda farklı çıkması bu sayede imkânsız.
    /// </summary>
    private static LeaveReportViewModel BuildReport(LeaveRequestListViewModel source)
    {
        // İki görünümün satır tipleri farklı (LeaveHistoryResponse / LeaveRequestResponse).
        // Hesaplar aynı olduğu için önce ORTAK bir şekle indiriyoruz; aksi hâlde her
        // metriği iki kez yazmak gerekirdi ve ikisi zamanla birbirinden kopardı.
        var rows = source.IsAllView
            ? source.AllRows.Select(r => new ReportRow(
                r.SubjectName, r.SubjectType, r.DepartmentName, r.Type, r.Status,
                r.StartDate, r.WorkingDays)).ToList()
            : source.Requests.Select(r => new ReportRow(
                "", "", null, r.Type, r.Status, r.StartDate, r.TotalDays)).ToList();

        var totalDays = rows.Sum(r => r.Days);
        var longest = rows.OrderByDescending(r => r.Days).FirstOrDefault();

        var byType = rows
            .GroupBy(r => r.Type)
            .Select(g => new LeaveReportViewModel.TypeSlice
            {
                Type = g.Key,
                Label = LeaveDisplay.TypeText(g.Key),
                BadgeClass = LeaveDisplay.TypeBadge(g.Key),
                Count = g.Count(),
                Days = g.Sum(x => x.Days),
                PercentOfDays = totalDays == 0 ? 0 : (int)Math.Round(100.0 * g.Sum(x => x.Days) / totalDays)
            })
            .OrderByDescending(t => t.Days)
            .ToList();

        var byStatus = rows
            .GroupBy(r => r.Status)
            .Select(g => new LeaveReportViewModel.StatusSlice
            {
                Status = g.Key,
                Label = LeaveDisplay.StatusBadgeShort(g.Key).Text,
                BadgeClass = LeaveDisplay.StatusBadgeShort(g.Key).Badge,
                Count = g.Count(),
                PercentOfCount = rows.Count == 0 ? 0 : (int)Math.Round(100.0 * g.Count() / rows.Count)
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        // Departman ve kişi kırılımı yalnızca şirket genelinde anlamlı: "İzinlerim"
        // görünümünde tek kişi vardır, kırılım kendini tekrar ederdi.
        var byDepartment = new List<LeaveReportViewModel.GroupSlice>();
        var topPeople = new List<LeaveReportViewModel.GroupSlice>();

        if (source.IsAllView)
        {
            byDepartment = BuildGroups(
                rows.GroupBy(r => r.DepartmentName ?? "Departmansız"),
                g => g.Select(x => x.Person).Distinct().Count());

            topPeople = BuildGroups(
                rows.GroupBy(r => r.Person),
                _ => 1,
                g => g.First().PersonType)
                .Take(10)
                .ToList();
        }

        // Ay kırılımı gerçek takvimden gelir: veri hangi aylara yayılıyorsa o aylar
        // listelenir. Sabit 12 kutu çizmek, aralık 3 aylıkken 9 boş sütun demekti —
        // kaldırılan grafiğin sorunlarından biri de buydu.
        var monthGroups = rows
            .GroupBy(r => new { r.Start.Year, r.Start.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count(),
                Days = g.Sum(x => x.Days)
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        var maxMonthDays = monthGroups.Count == 0 ? 0 : monthGroups.Max(m => m.Days);

        var byMonth = monthGroups.Select(m => new LeaveReportViewModel.MonthSlice
        {
            Year = m.Year,
            Month = m.Month,
            Label = new DateTime(m.Year, m.Month, 1).ToString("MMMM yyyy"),
            RequestCount = m.Count,
            Days = m.Days,
            PercentOfMax = maxMonthDays == 0 ? 0 : (int)Math.Round(100.0 * m.Days / maxMonthDays)
        }).ToList();

        return new LeaveReportViewModel
        {
            Source = source,
            TotalRequests = rows.Count,
            TotalDays = totalDays,
            PeopleCount = source.IsAllView ? rows.Select(r => r.Person).Distinct().Count() : (rows.Count > 0 ? 1 : 0),
            ApprovedCount = rows.Count(r => r.Status == "Approved"),
            ApprovedDays = rows.Where(r => r.Status == "Approved").Sum(r => r.Days),
            PendingCount = rows.Count(r => r.Status is "Pending" or "PendingHr"),
            RejectedCount = rows.Count(r => r.Status == "Rejected"),
            LongestLeaveDays = longest?.Days ?? 0,
            LongestLeaveOwner = source.IsAllView ? longest?.Person : null,
            ByType = byType,
            ByStatus = byStatus,
            ByDepartment = byDepartment,
            TopPeople = topPeople,
            ByMonth = byMonth
        };
    }

    /// <summary>
    /// Gruplanmış satırları rapor dilimlerine çevirir; çubuk oranı grubun EN BÜYÜĞÜNE
    /// göredir (toplama değil) — küçük gruplar görünmez ince çizgiye dönüşmesin.
    /// </summary>
    private static List<LeaveReportViewModel.GroupSlice> BuildGroups(
        IEnumerable<IGrouping<string, ReportRow>> groups,
        Func<IGrouping<string, ReportRow>, int> peopleCount,
        Func<IGrouping<string, ReportRow>, string?>? note = null)
    {
        var materialized = groups.ToList();
        var maxDays = materialized.Count == 0 ? 0 : materialized.Max(g => g.Sum(x => x.Days));

        return materialized
            .Select(g => new LeaveReportViewModel.GroupSlice
            {
                Name = g.Key,
                Note = note?.Invoke(g),
                PeopleCount = peopleCount(g),
                RequestCount = g.Count(),
                Days = g.Sum(x => x.Days),
                PercentOfMax = maxDays == 0 ? 0 : (int)Math.Round(100.0 * g.Sum(x => x.Days) / maxDays)
            })
            .OrderByDescending(x => x.Days)
            .ToList();
    }

    /// <summary>İki liste tipinin ortak paydası — yalnızca rapor hesabı için.</summary>
    private sealed record ReportRow(
        string Person, string PersonType, string? DepartmentName,
        string Type, string Status, DateTime Start, int Days);

    /// <summary>
    /// Excel (.xlsx) dışa aktarma — rapor ile aynı kapsam, çalışılabilir biçim.
    ///
    /// CSV yerine xlsx tercih edildi çünkü CSV her hücreyi METİN olarak taşır:
    /// gün sayıları toplanamaz, tarihler sıralanamaz, ayraç/kodlama Excel'in
    /// bölgesel ayarına bağlı kalırdı. Burada sayı sayı, tarih tarih olarak yazılır.
    ///
    /// İki sayfa: "Özet" (raporun metrikleri) ve "Kayıtlar" (ham döküm, pivot için).
    /// </summary>
    public async Task<IActionResult> ExportExcel(string? range, DateTime? from, DateTime? to)
    {
        var source = await BuildListAsync(range, from, to);
        var report = BuildReport(source);

        using var workbook = new XLWorkbook();

        WriteSummarySheet(workbook.AddWorksheet("Özet"), report);
        WriteRecordsSheet(workbook.AddWorksheet("Kayıtlar"), source);

        // MemoryStream: dosya diske hiç yazılmaz, doğrudan yanıta akar.
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"izin-raporu-{DateTime.Today:yyyyMMdd}.xlsx");
    }

    /// <summary>Özet sayfası: ekrandaki göstergelerin birebir aynısı.</summary>
    private static void WriteSummarySheet(IXLWorksheet sheet, LeaveReportViewModel report)
    {
        var row = 1;

        sheet.Cell(row, 1).Value = report.IsAllView ? "İzin Raporu" : "İzin Raporum";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 15;
        row++;

        sheet.Cell(row++, 1).Value = $"Aralık: {report.RangeText}";
        sheet.Cell(row++, 1).Value = $"Üretim: {DateTime.Now:dd.MM.yyyy HH:mm}";
        row++;

        row = Section(sheet, row, "GÖSTERGELER");
        row = Pair(sheet, row, "Toplam talep", report.TotalRequests);
        row = Pair(sheet, row, "Toplam izin (iş günü)", report.TotalDays);

        if (report.IsAllView)
        {
            row = Pair(sheet, row, "İzin kullanan kişi", report.PeopleCount);
            row = Pair(sheet, row, "Kişi başı ortalama (gün)", report.AverageDaysPerPerson);
        }

        row = Pair(sheet, row, "Talep başı ortalama (gün)", report.AverageDaysPerRequest);
        row = Pair(sheet, row, "Onay oranı (%)", report.ApprovalRate);
        row = Pair(sheet, row, "Hastalık payı (%)", report.SickSharePercent);
        row = Pair(sheet, row, "En uzun izin (gün)", report.LongestLeaveDays);
        row = Pair(sheet, row, "En yoğun dönem", report.BusiestMonth?.Label ?? "—");
        row++;

        row = Section(sheet, row, "TÜR KIRILIMI");
        row = Header(sheet, row, "Tür", "Talep", "İş günü", "Pay (%)");
        foreach (var t in report.ByType)
        {
            sheet.Cell(row, 1).Value = t.Label;
            sheet.Cell(row, 2).Value = t.Count;
            sheet.Cell(row, 3).Value = t.Days;
            sheet.Cell(row, 4).Value = t.PercentOfDays;
            row++;
        }
        row++;

        row = Section(sheet, row, "DURUM KIRILIMI");
        row = Header(sheet, row, "Durum", "Talep", "Pay (%)");
        foreach (var s in report.ByStatus)
        {
            sheet.Cell(row, 1).Value = s.Label;
            sheet.Cell(row, 2).Value = s.Count;
            sheet.Cell(row, 3).Value = s.PercentOfCount;
            row++;
        }
        row++;

        if (report.ByDepartment.Count > 0)
        {
            row = Section(sheet, row, "DEPARTMAN KIRILIMI");
            row = Header(sheet, row, "Departman", "Kişi", "Talep", "İş günü", "Kişi başı");
            foreach (var d in report.ByDepartment)
            {
                sheet.Cell(row, 1).Value = d.Name;
                sheet.Cell(row, 2).Value = d.PeopleCount;
                sheet.Cell(row, 3).Value = d.RequestCount;
                sheet.Cell(row, 4).Value = d.Days;
                sheet.Cell(row, 5).Value = d.AveragePerPerson;
                row++;
            }
            row++;
        }

        row = Section(sheet, row, "AYLIK DAĞILIM");
        row = Header(sheet, row, "Dönem", "Talep", "İş günü");
        foreach (var m in report.ByMonth)
        {
            sheet.Cell(row, 1).Value = m.Label;
            sheet.Cell(row, 2).Value = m.RequestCount;
            sheet.Cell(row, 3).Value = m.Days;
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    /// <summary>Kayıtlar sayfası: ham döküm. Excel'de süzülüp pivotlanmak için.</summary>
    private static void WriteRecordsSheet(IXLWorksheet sheet, LeaveRequestListViewModel source)
    {
        var row = 1;

        if (source.IsAllView)
        {
            Header(sheet, row++, "Kişi", "Kişi türü", "Departman", "Tür",
                                 "Başlangıç", "Bitiş", "İş günü", "Durum", "Açıklama");

            foreach (var r in source.AllRows)
            {
                sheet.Cell(row, 1).Value = r.SubjectName;
                sheet.Cell(row, 2).Value = r.SubjectType;
                sheet.Cell(row, 3).Value = r.DepartmentName ?? "";
                sheet.Cell(row, 4).Value = LeaveDisplay.TypeText(r.Type);
                sheet.Cell(row, 5).Value = r.StartDate;
                sheet.Cell(row, 6).Value = r.EndDate;
                sheet.Cell(row, 7).Value = r.WorkingDays;
                sheet.Cell(row, 8).Value = LeaveDisplay.StatusBadgeShort(r.Status).Text;
                sheet.Cell(row, 9).Value = r.Description ?? "";
                row++;
            }

            sheet.Range(2, 5, Math.Max(row - 1, 2), 6).Style.DateFormat.Format = "dd.MM.yyyy";
        }
        else
        {
            Header(sheet, row++, "Tür", "Başlangıç", "Bitiş", "İş günü", "Durum", "Açıklama");

            foreach (var r in source.Requests)
            {
                sheet.Cell(row, 1).Value = LeaveDisplay.TypeText(r.Type);
                sheet.Cell(row, 2).Value = r.StartDate;
                sheet.Cell(row, 3).Value = r.EndDate;
                sheet.Cell(row, 4).Value = r.TotalDays;
                sheet.Cell(row, 5).Value = LeaveDisplay.StatusBadgeShort(r.Status).Text;
                sheet.Cell(row, 6).Value = r.Description ?? "";
                row++;
            }

            sheet.Range(2, 2, Math.Max(row - 1, 2), 3).Style.DateFormat.Format = "dd.MM.yyyy";
        }

        // Başlık satırı ekranda sabit kalsın ve süzülebilsin — uzun listede
        // aşağı kaydırınca hangi sütuna baktığın kaybolmasın.
        sheet.SheetView.FreezeRows(1);
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.Columns().AdjustToContents();
    }

    /// <summary>Kalın bölüm başlığı yazar, bir sonraki satır numarasını döndürür.</summary>
    private static int Section(IXLWorksheet sheet, int row, string title)
    {
        sheet.Cell(row, 1).Value = title;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        return row + 1;
    }

    /// <summary>Kalın sütun başlıkları yazar, bir sonraki satır numarasını döndürür.</summary>
    private static int Header(IXLWorksheet sheet, int row, params string[] titles)
    {
        for (var i = 0; i < titles.Length; i++)
        {
            sheet.Cell(row, i + 1).Value = titles[i];
            sheet.Cell(row, i + 1).Style.Font.Bold = true;
        }
        return row + 1;
    }

    /// <summary>"Etiket | değer" satırı. Sayılar SAYI olarak yazılır ki Excel'de toplanabilsinler.</summary>
    private static int Pair(IXLWorksheet sheet, int row, string label, object value)
    {
        sheet.Cell(row, 1).Value = label;

        // XLCellValue örtük dönüşümü her tipi kabul etmediği için tip ayrımı elle.
        sheet.Cell(row, 2).Value = value switch
        {
            int i => i,
            double d => d,
            _ => value.ToString()
        };

        return row + 1;
    }

    /// <summary>
    /// Liste modelini kurar: rol kapısı → veri çekme → zaman filtresi.
    /// Index, Report ve ExportCsv aynı metodu çağırır; kapsamın üç yerde
    /// ayrı ayrı hesaplanması er ya da geç birbirinden kopardı.
    /// </summary>
    private async Task<LeaveRequestListViewModel> BuildListAsync(
        string? range, DateTime? from, DateTime? to)
    {
        var (effectiveFrom, effectiveTo) = ResolveRange(range, from, to);

        // Rol kapısı: HR/Admin TÜM izin geçmişini tek listede görür (çalışan seçmeden,
        // salt gözlem). Diğer roller yalnızca KENDİ izinlerini görür.
        var isBrowser = User.IsInRole("HR") || User.IsInRole("Admin");

        var model = new LeaveRequestListViewModel
        {
            IsAllView = isBrowser,
            // Seçim yoksa "Tümü". Tarihsiz "custom" da Tümü'ye düşer: kullanıcı
            // tarih girmeden Uygula'ya basarsa filtre uygulanmıyor ama hiçbir çip
            // de yanmıyordu — ekran hangi aralıkta olduğunu söylemez hâle geliyordu.
            Range = ResolveRangeKey(range, from, to),
            From = from,
            To = to,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        };

        if (isBrowser)
        {
            var all = await _leaveRequestApi.GetAllAsync();

            if (!all.IsSuccess)
            {
                TempData["Error"] = all.Message ?? "İzin geçmişi alınamadı.";
                return model;
            }

            var rows = all.Data ?? [];
            model.TotalBeforeFilter = rows.Count;
            model.AllRows = rows
                .Where(r => Overlaps(r.StartDate, r.EndDate, effectiveFrom, effectiveTo))
                .OrderByDescending(r => r.StartDate)
                .ToList();

            return model;
        }

        var me = await _employeeApi.GetMyProfileAsync();
        var currentEmployeeId = me.IsSuccess ? me.Data?.Id : null;

        model.SelectedEmployeeId = currentEmployeeId;
        model.CurrentEmployeeId = currentEmployeeId;

        // Çalışan kaydı yoksa (hesap kişiye bağlı değilse) liste boş kalır.
        if (currentEmployeeId is null)
            return model;

        var response = await _leaveRequestApi.GetByEmployeeAsync(currentEmployeeId.Value);

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message ?? "İzin talepleri alınamadı.";
            return model;
        }

        var mine = response.Data ?? [];
        model.TotalBeforeFilter = mine.Count;
        model.Requests = mine
            .Where(r => Overlaps(r.StartDate, r.EndDate, effectiveFrom, effectiveTo))
            .OrderByDescending(r => r.StartDate)
            .ToList();

        return model;
    }

    /// <summary>
    /// Hangi aralık seçili sayılacak. Tarihi olmayan "custom", filtre uygulamadığı
    /// için Tümü'ye eşdeğerdir — anahtarı da öyle olsun ki ekranda doğru çip yansın.
    /// </summary>
    private static string ResolveRangeKey(string? range, DateTime? from, DateTime? to)
    {
        if (string.IsNullOrWhiteSpace(range))
            return LeaveRangeOptions.All;

        if (range == LeaveRangeOptions.Custom && from is null && to is null)
            return LeaveRangeOptions.All;

        return range;
    }

    /// <summary>
    /// Hazır aralık anahtarını gerçek tarihlere çevirir. null sınır = sınırsız.
    /// </summary>
    private static (DateTime? From, DateTime? To) ResolveRange(
        string? range, DateTime? from, DateTime? to)
    {
        var today = DateTime.Today;

        return range switch
        {
            LeaveRangeOptions.Today => (today, today),

            // Hafta PAZARTESİ başlar (Türkiye). DayOfWeek Pazar'ı 0 saydığı için
            // +6 %7 ile kaydırıyoruz: Pzt→0, Sal→1 … Paz→6.
            LeaveRangeOptions.ThisWeek =>
                (today.AddDays(-(((int)today.DayOfWeek + 6) % 7)),
                 today.AddDays(-(((int)today.DayOfWeek + 6) % 7)).AddDays(6)),

            LeaveRangeOptions.ThisMonth =>
                (new DateTime(today.Year, today.Month, 1),
                 new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1)),

            LeaveRangeOptions.LastThreeMonths => (today.AddMonths(-3), today),
            LeaveRangeOptions.ThisYear => (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31)),

            // Özel aralıkta tek sınır verilmesi geçerlidir ("şu tarihten sonrası").
            // Ters girilmişse (bitiş < başlangıç) sessizce boş liste döndürmek
            // yerine takas ediyoruz: kullanıcı nedenini aramasın.
            LeaveRangeOptions.Custom when from > to && to is not null => (to, from),
            LeaveRangeOptions.Custom => (from, to),

            _ => (null, null)   // "all" ve tanınmayan değerler → sınırsız
        };
    }

    /// <summary>
    /// İzin aralığı filtre penceresiyle KESİŞİYOR mu? Başlangıç tarihine bakmak
    /// yetmez: 28 Haziran – 3 Temmuz izni "Temmuz" filtresinde de görünmeli.
    /// </summary>
    private static bool Overlaps(DateTime start, DateTime end, DateTime? from, DateTime? to) =>
        (from is null || end.Date >= from.Value.Date)
        && (to is null || start.Date <= to.Value.Date);

    // Tek talebin detayı + onay izi. Görüntüleme yetkisini API çözer; yetkisizse
    // (403/400) kullanıcı listeye döner. İK aşamasındaysa detaydan onay/red yapılabilir.
    public async Task<IActionResult> Details(int id)
    {
        var response = await _leaveRequestApi.GetDetailAsync(id);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "İzin detayı alınamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(response.Data);
    }

    /// <summary>
    /// Ekip izin takvimi (yalnızca yönetici). Önümüzdeki iki hafta boyunca kimin
    /// izinli olduğunu tek ekranda gösterir.
    ///
    /// Veri iki uçtan birleştirilir: görünür ekip /api/employees'ten, her kişinin
    /// izinleri /api/leaverequests/employee/{id}'den. "Tümünü tek çağrıda ver"
    /// diyen bir uç YOK (/all yalnızca İK/Admin'e açık), bu yüzden kişi başına
    /// bir istek atılır — paralel, ve ekip büyükse hiç çizilmez.
    /// </summary>
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Calendar()
    {
        const int dayCount = 14;
        const int maxTeamSize = 30;   // üstünde sayfa başına 30+ HTTP isteği olurdu

        var today = DateTime.Today;
        var model = new TeamCalendarViewModel
        {
            StartDate = today,
            DayCount = dayCount,
            Days = Enumerable.Range(0, dayCount).Select(i => today.AddDays(i)).ToList()
        };

        var me = await _employeeApi.GetMyProfileAsync();
        var myId = me.Data?.Id;
        var myManagerId = me.Data?.ManagerId;

        var employees = await _employeeApi.GetAllAsync();

        if (!employees.IsSuccess)
        {
            TempData["Error"] = employees.Message ?? "Ekip listesi alınamadı.";
            return View(model);
        }

        // Görünür liste kişinin BİR ÜST yöneticisini de içerir; onun izinlerini
        // çekme yetkimiz yok (API zincir-aşağı bakar), o yüzden listeden düşülür.
        var team = (employees.Data ?? [])
            .Where(e => e.IsActive && e.Id != myManagerId)
            .OrderByDescending(e => e.Id == myId)
            .ThenBy(e => e.Seniority ?? 99)
            .ThenBy(e => e.FirstName)
            .ToList();

        model.TeamSize = team.Count;

        if (team.Count > maxTeamSize)
        {
            model.TeamTooLarge = true;
            return View(model);
        }

        var endDate = today.AddDays(dayCount - 1);

        // Paralel: sıralı atsaydık 20 kişilik ekipte sayfa 20 gidiş-dönüş beklerdi.
        var fetches = team.Select(async e => (Employee: e, Leaves: await _leaveRequestApi.GetByEmployeeAsync(e.Id)));
        var results = await Task.WhenAll(fetches);

        foreach (var (employee, leaves) in results)
        {
            var row = new TeamCalendarRow
            {
                EmployeeId = employee.Id,
                FullName = $"{employee.FirstName} {employee.LastName}",
                Initials = Initials(employee.FirstName, employee.LastName),
                Subtitle = SeniorityDisplay.Label(employee.Seniority),
                IsSelf = employee.Id == myId
            };

            // Reddedilenler takvimde yer tutmaz; onaylı ve bekleyenler tutar
            // (bekleyen izin de planlamada görünmeli).
            var relevant = (leaves.Data ?? [])
                .Where(l => l.Status != "Rejected"
                            && l.StartDate.Date <= endDate
                            && l.EndDate.Date >= today)
                .ToList();

            foreach (var day in model.Days)
            {
                var hit = relevant.FirstOrDefault(l => l.StartDate.Date <= day && l.EndDate.Date >= day);
                var isWeekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                row.Cells.Add(new TeamCalendarCell
                {
                    Date = day,
                    IsWeekend = isWeekend,
                    IsToday = day == today,
                    Type = hit?.Type,
                    Status = hit?.Status
                });

                if (hit is not null && !isWeekend) row.LeaveDays++;
            }

            model.Rows.Add(row);
        }

        // Çakışma: aynı iş gününde 2+ kişi izinli.
        model.ClashDays = model.Days
            .Where(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .Where(d => model.Rows.Count(r => r.Cells.Any(c => c.Date == d && c.Type is not null)) >= 2)
            .ToList();

        return View(model);
    }

    private static string Initials(string first, string last) =>
        $"{(first.Length > 0 ? char.ToUpperInvariant(first[0]) : '?')}{(last.Length > 0 ? char.ToUpperInvariant(last[0]) : ' ')}".Trim();

    // Giriş yapanın ONAYINI BEKLEYEN talepler — tek listede, çalışan seçmeden.
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<IActionResult> Approvals()
    {
        var response = await _leaveRequestApi.GetPendingApprovalsAsync();

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message ?? "Onay bekleyenler alınamadı.";
            return View(new List<PendingApprovalResponse>());
        }

        return View(response.Data ?? []);
    }

    public IActionResult Create()
    {
        // Çalışan seçimi yok: talep, giriş yapan hesabın kendisi için açılır;
        // kimliği API, JWT claim'inden çözer.
        var form = new LeaveRequestFormViewModel { TypeOptions = GetTypeOptions() };

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveRequestFormViewModel form)
    {
        // Rapor zorunluluğu türe bağlı: yalnızca Hastalık (3) seçiliyse istenir.
        // [Required] türe göre koşullanamadığı için burada elle ekleniyor; nihai
        // otorite yine API + Application validator'ıdır (bu sadece erken UX geri bildirimi).
        const int sickType = 3;
        if (form.Type == sickType && string.IsNullOrWhiteSpace(form.MedicalReport))
            ModelState.AddModelError(nameof(form.MedicalReport), "Hastalık izni için rapor bilgisi zorunludur.");

        if (!ModelState.IsValid)
            return View(FillOptions(form));

        var response = await _leaveRequestApi.CreateAsync(new CreateLeaveRequestRequest
        {
            // ModelState geçerliyse [Required] alanlar dolu; bu yüzden .Value güvenli.
            Type = form.Type,
            StartDate = form.StartDate!.Value,
            EndDate = form.EndDate!.Value,
            Description = form.Description,
            // Rapor yalnızca hastalık izninde anlamlı; diğer türlerde boş gönderilir.
            MedicalReport = form.Type == sickType ? form.MedicalReport : null
        });

        if (!response.IsSuccess)
        {
            // API'nin iş kuralı reddetti (hak yetersiz, tarih çakışması vb.) — mesajı forma yansıt.
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            return View(FillOptions(form));
        }

        TempData["Success"] = response.Message ?? "İzin talebi oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    // returnTo: "Approvals" ise Onay Bekleyenler'e döner, aksi hâlde çalışan listesine.
    [HttpPost]
    [Authorize(Roles = "HR,Manager,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, int employeeId, string? returnTo)
    {
        var response = await _leaveRequestApi.ApproveAsync(id);

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Onaylama işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "İzin talebi onaylandı.";

        return RedirectAfterAction(returnTo, id, employeeId);
    }

    [HttpPost]
    [Authorize(Roles = "HR,Manager,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, int employeeId, string? reason, string? returnTo)
    {
        var response = await _leaveRequestApi.RejectAsync(id, new RejectLeaveRequestRequest
        {
            Reason = reason
        });

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Reddetme işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "İzin talebi reddedildi.";

        return RedirectAfterAction(returnTo, id, employeeId);
    }

    // Onay/Red sonrası nereye dönüleceği: geldiğin ekrana. "Details" ise o talebin
    // detayına (güncel durumu görürsün), "Approvals" ise Onay Bekleyenler'e, yoksa listeye.
    private IActionResult RedirectAfterAction(string? returnTo, int id, int employeeId) =>
        returnTo switch
        {
            "Details" => RedirectToAction(nameof(Details), new { id }),
            "Approvals" => RedirectToAction(nameof(Approvals)),
            _ => RedirectToAction(nameof(Index), new { employeeId })
        };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int employeeId)
    {
        var response = await _leaveRequestApi.DeleteAsync(id);

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "Silme işlemi başarısız.";
        else
            TempData["Success"] = response.Message ?? "İzin talebi silindi.";

        return RedirectToAction(nameof(Index), new { employeeId });
    }

    /// <summary>
    /// Form View'a geri dönerken dropdown TEKRAR doldurulmalı:
    /// POST gövdesinde seçenek listeleri gelmez, sadece seçilen değerler gelir.
    /// </summary>
    private static LeaveRequestFormViewModel FillOptions(LeaveRequestFormViewModel form)
    {
        form.TypeOptions = GetTypeOptions();
        return form;
    }

    // İzin türleri Domain enum'ının sayısal karşılıklarıdır; API bu değerleri bekler.
    private static IEnumerable<SelectListItem> GetTypeOptions() =>
    [
        new SelectListItem("Yıllık İzin", "1"),
        new SelectListItem("Ücretsiz İzin", "2"),
        new SelectListItem("Hastalık İzni", "3")
    ];
}
