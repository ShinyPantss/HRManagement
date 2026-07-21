// Mobilde sidebar aç/kapa: ☰ butonu açar, dışarı tıklamak kapatır.
document.addEventListener("click", (e) => {
    if (e.target.closest("#sidebarToggle")) {
        document.body.classList.toggle("sidebar-open");
    } else if (document.body.classList.contains("sidebar-open") && !e.target.closest("#sidebar")) {
        document.body.classList.remove("sidebar-open");
    }
});
