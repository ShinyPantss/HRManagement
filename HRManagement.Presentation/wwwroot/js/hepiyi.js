(function () {
    "use strict";

    const THEME_KEY = "hepiyi-theme";

    // Tema, ilk boyamadan önce <head> içindeki küçük betikle uygulanır (FOUC'u önlemek için).
    // Buradaki iş, düğmeye tıklandığında tercihi değiştirip saklamaktır.
    function applyTheme(theme) {
        document.documentElement.setAttribute("data-theme", theme);
        try {
            localStorage.setItem(THEME_KEY, theme);
        } catch {
            /* localStorage kapalıysa tema yalnızca bu sayfa için geçerli olur. */
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        const themeToggle = document.querySelector("[data-theme-toggle]");
        if (themeToggle) {
            themeToggle.addEventListener("click", function () {
                const current = document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
                applyTheme(current === "dark" ? "light" : "dark");
            });
        }

        // Mobilde yan menüyü aç/kapat
        const sidebar = document.querySelector("[data-sidebar]");
        const scrim = document.querySelector("[data-scrim]");
        const sidebarToggle = document.querySelector("[data-sidebar-toggle]");

        function closeSidebar() {
            sidebar?.classList.remove("is-open");
            scrim?.classList.remove("is-open");
        }

        sidebarToggle?.addEventListener("click", function () {
            sidebar?.classList.toggle("is-open");
            scrim?.classList.toggle("is-open");
        });

        scrim?.addEventListener("click", closeSidebar);

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") closeSidebar();
        });

        // Giriş ekranındaki demo kullanıcı kısayolları formu doldursun.
        document.querySelectorAll("[data-demo-email]").forEach(function (button) {
            button.addEventListener("click", function () {
                const form = button.closest("form") || document.querySelector("form");
                const email = form?.querySelector("input[type='email']");
                const password = form?.querySelector("input[type='password']");

                if (email) email.value = button.getAttribute("data-demo-email") || "";
                if (password) password.value = button.getAttribute("data-demo-password") || "";
                email?.focus();
            });
        });

        // Filtre kutularında seçim değişince formu gönder (ayrı "Uygula" tıklaması gerekmesin).
        document.querySelectorAll("[data-autosubmit]").forEach(function (control) {
            control.addEventListener("change", function () {
                control.form?.submit();
            });
        });

        // Yıkıcı olmayan ama geri alınamayan işlemler için basit onay.
        document.querySelectorAll("[data-confirm]").forEach(function (element) {
            element.addEventListener("click", function (event) {
                if (!window.confirm(element.getAttribute("data-confirm"))) {
                    event.preventDefault();
                }
            });
        });
    });
})();
