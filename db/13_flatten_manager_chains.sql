/* =============================================================================
   13) Yönetici zincirlerini DÜZLEŞTİR — veri onarımı (2026-07-27)

   SORUN: 12_seed_org_kadro tohum verisi birim içinde merdiven kurmuştu:
     Uzman(6) → Kıdemli Uzman(5) → Müdür Yrd(4) → Müdür(3)
   Oysa şirket kuralı (SeniorityLevelExtensions.IsManagerial + ManagerAssignment):
   Müdür Yrd / Kıdemli Uzman / Uzman BİREYSEL KATKICIDIR, kimseye yönetici olamaz.
   Uygulama bu atamayı artık reddediyor; bu script kural öncesi kalan kayıtları
   düzeltir. SONUÇ: birimdeki herkes DOĞRUDAN birimin müdürüne bağlanır —
   "Doğrudan Ekibi" ekranında müdür, biriminin tamamını görür.

   Kural: yöneticisi yönetici kademesinde (1=GM, 2=GMY, 3=Müdür) OLMAYAN her
   aktif çalışan; önce KENDİ BİRİMİNİN, o yoksa DEPARTMANININ en yakın üst
   kademedeki yöneticisine bağlanır. Idempotent — tekrar çalıştırmak zararsız.
   ============================================================================= */

UPDATE e
SET e.ManagerId = pick.Id,
    e.UpdatedAt = SYSUTCDATETIME()
FROM dbo.Employees e
JOIN dbo.Employees mgr
    ON mgr.Id = e.ManagerId
   AND mgr.Seniority NOT IN (1, 2, 3)          -- yöneticisi bireysel katkıcı = bozuk kayıt
CROSS APPLY (
    SELECT TOP 1 cand.Id
    FROM dbo.Employees cand
    WHERE cand.IsActive = 1
      AND cand.Seniority IN (1, 2, 3)
      AND cand.Id <> e.Id
      AND cand.DepartmentId = e.DepartmentId
      AND (cand.UnitId = e.UnitId OR cand.UnitId IS NULL)   -- kendi birimi ya da departman seviyesi
    ORDER BY
        CASE WHEN e.UnitId IS NOT NULL AND cand.UnitId = e.UnitId THEN 0 ELSE 1 END, -- önce birim müdürü
        cand.Seniority DESC                                  -- sonra en YAKIN üst kademe (Müdür > GMY > GM)
) pick;
GO
