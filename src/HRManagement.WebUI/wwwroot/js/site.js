// Mobilde sidebar aç/kapa: ☰ butonu açar, dışarı tıklamak kapatır.
document.addEventListener("click", (e) => {
    if (e.target.closest("#sidebarToggle")) {
        document.body.classList.toggle("sidebar-open");
    } else if (document.body.classList.contains("sidebar-open") && !e.target.closest("#sidebar")) {
        document.body.classList.remove("sidebar-open");
    }
});

// TempData bildirimlerini toast olarak göster (Bootstrap toast'lar elle başlatılır).
// 5 sn sonra otomatik kapanır; kullanıcı çarpıyla erken kapatabilir.
document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll(".app-toast").forEach((el) => {
        bootstrap.Toast.getOrCreateInstance(el, { delay: 5000 }).show();
    });
});
