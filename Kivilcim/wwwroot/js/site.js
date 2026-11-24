// Kıvılcım istemci tarafı: beğeni, "daha fazla göster" ve görsel önizleme.
(function () {
    "use strict";

    function token() {
        var el = document.querySelector('#antiforgery input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    // Beğeni: sayfayı yenilemeden aç/kapat. JS kapalıysa form normal gönderilir.
    document.addEventListener("submit", function (e) {
        var form = e.target;
        if (!form.classList || !form.classList.contains("like-form")) return;
        e.preventDefault();

        var btn = form.querySelector(".like-btn");
        btn.disabled = true;
        fetch(form.action, {
            method: "POST",
            headers: { "RequestVerificationToken": token(), "X-Requested-With": "fetch" },
            credentials: "same-origin"
        })
            .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
            .then(function (data) {
                btn.classList.toggle("liked", data.liked);
                btn.setAttribute("aria-pressed", data.liked ? "true" : "false");
                btn.querySelector(".heart").textContent = data.liked ? "♥" : "♡";
                btn.querySelector(".like-count").textContent = data.count;
                if (data.liked) {
                    btn.classList.add("pop");
                    setTimeout(function () { btn.classList.remove("pop"); }, 180);
                }
            })
            .catch(function () { form.submit(); })
            .finally(function () { btn.disabled = false; });
    });

    // "Daha fazla göster": sonraki sayfayı aynı listeye ekle.
    document.addEventListener("click", function (e) {
        var link = e.target.closest && e.target.closest(".load-more-btn");
        if (!link) return;
        e.preventDefault();

        var url = link.getAttribute("href");
        url += (url.indexOf("?") >= 0 ? "&" : "?") + "partial=true";
        link.textContent = "Yükleniyor...";
        link.classList.add("disabled");

        fetch(url, { headers: { "X-Requested-With": "fetch" }, credentials: "same-origin" })
            .then(function (r) { if (!r.ok) throw new Error(r.status); return r.text(); })
            .then(function (html) {
                var wrapper = link.closest(".load-more");
                var tmp = document.createElement("div");
                tmp.innerHTML = html;
                wrapper.replaceWith.apply(wrapper, Array.prototype.slice.call(tmp.childNodes));
            })
            .catch(function () { window.location.href = link.getAttribute("href"); });
    });

    // ---- Anlık bildirim ve mesajlar (SignalR) ----
    function setBadge(id, count) {
        var badge = document.getElementById(id);
        if (!badge) return;
        badge.textContent = count;
        badge.classList.toggle("d-none", !count);
    }

    function showToast(title, text, url) {
        var area = document.getElementById("toastArea");
        if (!area || !window.bootstrap) return;
        var el = document.createElement("div");
        el.className = "toast";
        el.setAttribute("role", "status");
        var header = document.createElement("div");
        header.className = "toast-header";
        var strong = document.createElement("strong");
        strong.className = "me-auto";
        strong.textContent = title;
        var close = document.createElement("button");
        close.type = "button"; close.className = "btn-close"; close.setAttribute("data-bs-dismiss", "toast");
        close.setAttribute("aria-label", "Kapat");
        header.appendChild(strong); header.appendChild(close);
        var body = document.createElement("a");
        body.className = "toast-body d-block text-reset text-decoration-none";
        body.href = url;
        body.textContent = text;
        el.appendChild(header); el.appendChild(body);
        area.appendChild(el);
        el.addEventListener("hidden.bs.toast", function () { el.remove(); });
        new bootstrap.Toast(el, { delay: 6000 }).show();
    }

    function timeLabel(iso) {
        var d = new Date(iso);
        return ("0" + d.getHours()).slice(-2) + ":" + ("0" + d.getMinutes()).slice(-2);
    }

    var chatBody = document.getElementById("chatBody");

    function appendBubble(mine, text, iso) {
        if (!chatBody) return;
        var empty = document.getElementById("chatEmpty");
        if (empty) empty.remove();
        var row = document.createElement("div");
        row.className = "bubble-row " + (mine ? "mine" : "theirs");
        var bubble = document.createElement("div");
        bubble.className = "bubble";
        text.split("\n").forEach(function (line, i) {
            if (i > 0) bubble.appendChild(document.createElement("br"));
            bubble.appendChild(document.createTextNode(line));
        });
        var time = document.createElement("span");
        time.className = "bubble-time";
        time.textContent = timeLabel(iso);
        bubble.appendChild(time);
        row.appendChild(bubble);
        chatBody.appendChild(row);
        chatBody.scrollTop = chatBody.scrollHeight;
    }

    if (chatBody) {
        chatBody.scrollTop = chatBody.scrollHeight;
        var chatForm = document.getElementById("chatForm");
        chatForm.addEventListener("submit", function (e) {
            e.preventDefault();
            var input = chatForm.querySelector("input[name=body]");
            var text = input.value.trim();
            if (!text) return;
            var data = new FormData(chatForm);
            input.value = "";
            fetch(chatForm.action, {
                method: "POST", body: data,
                headers: { "X-Requested-With": "fetch" }, credentials: "same-origin"
            })
                .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
                .then(function (m) { appendBubble(true, m.body, m.time); })
                .catch(function () { input.value = text; showToast("Mesaj gönderilemedi", "Lütfen tekrar deneyin.", "#"); });
        });
    }

    if (document.body.dataset.signedIn === "true" && window.signalR) {
        var connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/notifications")
            .withAutomaticReconnect()
            .build();

        connection.on("notification", function (n) {
            setBadge("notifBadge", n.unread);
            showToast("Yeni bildirim", n.text, n.url);
        });

        connection.on("message", function (m) {
            if (chatBody && chatBody.dataset.other === m.fromBlog) {
                appendBubble(false, m.body, m.time);
                // Açık sohbette gelen mesaj okunmuş sayılır.
                fetch("/messages/" + encodeURIComponent(m.fromBlog) + "/read", {
                    method: "POST", headers: { "RequestVerificationToken": token() }, credentials: "same-origin"
                });
                setBadge("msgBadge", Math.max(0, m.unread - 1));
            } else {
                setBadge("msgBadge", m.unread);
                showToast(m.fromName + " sana mesaj gönderdi", m.body, "/messages/" + encodeURIComponent(m.fromBlog));
            }
        });

        connection.start().catch(function () { /* bağlantı kurulamazsa sayfa yine çalışır */ });
        window.kivilcimHub = connection;
    }

    // Video gönderisinde seçilen videonun önizlemesi.
    var videoInput = document.querySelector('input[type="file"][name="Video"]');
    var videoPreview = document.getElementById("videoPreview");
    if (videoInput && videoPreview) {
        videoInput.addEventListener("change", function () {
            var file = videoInput.files && videoInput.files[0];
            if (!file) { videoPreview.classList.add("d-none"); return; }
            videoPreview.src = URL.createObjectURL(file);
            videoPreview.classList.remove("d-none");
        });
    }

    // Fotoğraf gönderisinde seçilen görselin önizlemesi.
    var input = document.querySelector('input[type="file"][name="Image"]');
    var preview = document.getElementById("imagePreview");
    if (input && preview) {
        input.addEventListener("change", function () {
            var file = input.files && input.files[0];
            if (!file) { preview.classList.add("d-none"); return; }
            preview.src = URL.createObjectURL(file);
            preview.classList.remove("d-none");
        });
    }
})();
