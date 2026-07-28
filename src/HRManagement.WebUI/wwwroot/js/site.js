/* HepiyiHR — kabuk davranışları.
   Çerçeve yok: sayfalar sunucuda render ediliyor, buradaki her şey ilerleyici
   iyileştirme. JS kapalıysa formlar yine çalışır (onay diyaloğu native'e düşer). */

(function () {
    "use strict";

    /* ── Mobil kenar çubuğu: ☰ açar, dışarı tıklamak veya Esc kapatır. ── */
    document.addEventListener("click", function (e) {
        if (e.target.closest("#sidebarToggle")) {
            document.body.classList.toggle("sidebar-open");
        } else if (document.body.classList.contains("sidebar-open") && !e.target.closest("#sidebar")) {
            document.body.classList.remove("sidebar-open");
        }
    });

    /* ── Toast: 5 sn sonra kapanır, çarpıyla erken kapatılabilir. ── */
    function closeToast(el) {
        el.classList.add("leaving");
        el.addEventListener("animationend", function () { el.remove(); }, { once: true });
    }

    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll(".toast").forEach(function (el) {
            var timer = setTimeout(function () { closeToast(el); }, 5000);
            var close = el.querySelector(".toast-close");
            if (close) {
                close.addEventListener("click", function () {
                    clearTimeout(timer);
                    closeToast(el);
                });
            }
        });
    });

    /* ── Üst bardaki arama: sayfadaki [data-search] satırlarını anlık süzer.
         Öyle satır yoksa kutu hiç gösterilmez.

         Sayfanın kendi filtresi varsa (ör. Çalışanlar ekranı: departman/birim/
         kıdem/durum) o filtre [data-page-filter] işaretiyle sahipliği alır ve
         arama kutusunu KENDİSİ okur — iki mekanizma aynı satırın görünürlüğüne
         dokunup birbirini ezmesin. ── */
    document.addEventListener("DOMContentLoaded", function () {
        var box = document.getElementById("globalSearchBox");
        var input = document.getElementById("globalSearch");
        if (!box || !input) return;

        var rows = Array.prototype.slice.call(document.querySelectorAll("[data-search]"));
        if (!rows.length) return;

        box.hidden = false;

        // Sayfa kendi filtresini kuruyorsa burada durup ona bırakıyoruz.
        if (document.querySelector("[data-page-filter]")) return;

        input.addEventListener("input", function () {
            // toLowerCase() bilinçli: data-search sunucuda ToLowerInvariant ile
            // üretiliyor; locale'e duyarlı çevrim I/İ'de iki tarafı ayrıştırırdı.
            var q = (input.value || "").trim().toLowerCase();
            var visible = 0;
            rows.forEach(function (row) {
                var hit = !q || (row.getAttribute("data-search") || "").indexOf(q) !== -1;
                row.hidden = !hit;
                if (hit) visible++;
            });
            var none = document.getElementById("noFilterResults");
            if (none) none.hidden = visible !== 0;
        });
    });

    /* ── Anahtar (switch): gizli checkbox'ı sarmalayan etiket. ── */
    document.addEventListener("change", function (e) {
        var input = e.target.closest(".switch input[type=checkbox]");
        if (!input) return;
        input.closest(".switch").classList.toggle("on", input.checked);
    });

    /* ── Diyalog: native confirm/prompt yerine tasarıma uyan kutu.
         Kullanım:
           <form data-confirm="Silinsin mi?">                → onay ister
           <form data-confirm="…" data-reason>               → onay + gerekçe alır
         Gerekçe, formdaki input[name=reason] alanına yazılır. ── */
    var dialog = null;

    function buildDialog() {
        var wrap = document.createElement("div");
        wrap.className = "modal-backdrop";
        wrap.hidden = true;
        wrap.innerHTML =
            '<div class="modal" role="dialog" aria-modal="true" aria-labelledby="modalText">' +
            '  <div class="modal-body">' +
            '    <div class="modal-text" id="modalText"></div>' +
            '    <textarea class="textarea modal-reason" rows="3" placeholder="Gerekçe (opsiyonel)"></textarea>' +
            '  </div>' +
            '  <div class="modal-actions">' +
            '    <button type="button" class="btn modal-cancel">Vazgeç</button>' +
            '    <button type="button" class="btn btn-primary modal-ok">Onayla</button>' +
            '  </div>' +
            '</div>';
        document.body.appendChild(wrap);
        return wrap;
    }

    function ask(message, wantsReason, okLabel, danger) {
        if (!dialog) dialog = buildDialog();

        var textEl = dialog.querySelector(".modal-text");
        var reasonEl = dialog.querySelector(".modal-reason");
        var okEl = dialog.querySelector(".modal-ok");
        var cancelEl = dialog.querySelector(".modal-cancel");

        textEl.textContent = message;
        reasonEl.hidden = !wantsReason;
        reasonEl.value = "";
        okEl.textContent = okLabel || "Onayla";
        okEl.classList.toggle("btn-danger-solid", !!danger);
        dialog.hidden = false;
        (wantsReason ? reasonEl : okEl).focus();

        return new Promise(function (resolve) {
            function done(result) {
                dialog.hidden = true;
                okEl.removeEventListener("click", onOk);
                cancelEl.removeEventListener("click", onCancel);
                dialog.removeEventListener("click", onBackdrop);
                document.removeEventListener("keydown", onKey);
                resolve(result);
            }
            function onOk() { done({ ok: true, reason: reasonEl.value }); }
            function onCancel() { done({ ok: false }); }
            function onBackdrop(e) { if (e.target === dialog) done({ ok: false }); }
            function onKey(e) { if (e.key === "Escape") done({ ok: false }); }

            okEl.addEventListener("click", onOk);
            cancelEl.addEventListener("click", onCancel);
            dialog.addEventListener("click", onBackdrop);
            document.addEventListener("keydown", onKey);
        });
    }

    document.addEventListener("submit", function (e) {
        var form = e.target;
        var message = form.getAttribute("data-confirm");
        if (!message || form.dataset.confirmed === "1") return;

        e.preventDefault();

        var wantsReason = form.hasAttribute("data-reason");
        var okLabel = form.getAttribute("data-confirm-ok");
        var danger = form.hasAttribute("data-danger");

        ask(message, wantsReason, okLabel, danger).then(function (res) {
            if (!res.ok) return;
            if (wantsReason) {
                var field = form.querySelector("input[name=reason]");
                if (field) field.value = res.reason || "";
            }
            form.dataset.confirmed = "1";
            form.submit();
        });
    });

    /* ── Kendiliğinden büyüyen metin kutusu + Enter ile gönderme.
         [data-autosize]      → içerik arttıkça yükseklik açılır
         [data-submit-enter]  → Enter gönderir, Shift+Enter alt satır ── */
    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll("textarea[data-autosize]").forEach(function (el) {
            function grow() {
                el.style.height = "auto";
                el.style.height = el.scrollHeight + "px";
            }
            el.addEventListener("input", grow);
            grow();
        });
    });

    document.addEventListener("keydown", function (e) {
        if (e.key !== "Enter" || e.shiftKey) return;
        var el = e.target;
        if (!el.matches || !el.matches("textarea[data-submit-enter]")) return;
        e.preventDefault();
        // requestSubmit: "required" doğrulaması atlanmasın.
        if (el.form) el.form.requestSubmit();
    });
})();
