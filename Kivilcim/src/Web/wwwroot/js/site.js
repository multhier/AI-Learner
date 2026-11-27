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


    // ---- Sağ alttaki konuşma paneli ----
    function initChatPanel(connection) {
        var launcher = document.getElementById("chatLauncher");
        var panel = document.getElementById("chatPanel");
        if (!launcher || !panel) return;

        var badge = document.getElementById("chatBadge");
        var list = document.getElementById("chatList");
        var listEmpty = document.getElementById("chatListEmpty");
        var thread = document.getElementById("chatThread");
        var messages = document.getElementById("chatMessages");
        var back = document.getElementById("chatBack");
        var title = document.getElementById("chatTitle");
        var closeBtn = document.getElementById("chatClose");
        var form = document.getElementById("chatComposeForm");
        var input = document.getElementById("chatComposeInput");
        var currentBlog = null;

        function api(url, opts) {
            opts = opts || {};
            opts.credentials = "same-origin";
            opts.headers = Object.assign({ "X-Requested-With": "fetch" }, opts.headers || {});
            return fetch(url, opts).then(function (r) {
                if (!r.ok) throw new Error(r.status);
                return r.status === 204 ? null : r.json();
            });
        }
        function setTotal(n) {
            badge.textContent = n;
            badge.classList.toggle("d-none", !n);
        }
        function isOpen() { return !panel.hidden; }
        function showList() {
            thread.hidden = true; list.hidden = false; back.hidden = true;
            title.textContent = "Mesajlar"; currentBlog = null;
        }
        function chatBubble(mine, text, iso) {
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
            messages.appendChild(row);
            messages.scrollTop = messages.scrollHeight;
        }
        function loadList() {
            return api("/api/chat/conversations").then(function (items) {
                Array.prototype.slice.call(list.querySelectorAll(".chat-item")).forEach(function (n) { n.remove(); });
                listEmpty.hidden = items.length > 0;
                var total = 0;
                items.forEach(function (c) {
                    total += c.unreadCount;
                    var item = document.createElement("button");
                    item.type = "button";
                    item.className = "chat-item" + (c.unreadCount ? " unread" : "");
                    var avatar = document.createElement("span");
                    avatar.className = "chat-item-avatar";
                    avatar.textContent = (c.displayName || "?").charAt(0).toUpperCase();
                    var main = document.createElement("span");
                    main.className = "chat-item-main";
                    var name = document.createElement("strong");
                    name.textContent = c.displayName;
                    var last = document.createElement("span");
                    last.className = "chat-item-last";
                    last.textContent = (c.lastFromMe ? "Sen: " : "") + c.lastMessage;
                    main.appendChild(name); main.appendChild(last);
                    item.appendChild(avatar); item.appendChild(main);
                    if (c.unreadCount) {
                        var u = document.createElement("span");
                        u.className = "badge rounded-pill bg-danger";
                        u.textContent = c.unreadCount;
                        item.appendChild(u);
                    }
                    item.addEventListener("click", function () { openThread(c.blogName, c.displayName); });
                    list.appendChild(item);
                });
                setTotal(total);
            });
        }
        function openThread(blogName, displayName) {
            api("/api/chat/" + encodeURIComponent(blogName)).then(function (t) {
                currentBlog = blogName;
                title.textContent = displayName;
                list.hidden = true; thread.hidden = false; back.hidden = false;
                messages.innerHTML = "";
                t.messages.forEach(function (m) { chatBubble(m.fromMe, m.body, m.createdAt); });
                input.focus();
                return api("/api/chat/" + encodeURIComponent(blogName) + "/read", {
                    method: "POST", headers: { "RequestVerificationToken": token() }
                }).then(loadList);
            }).catch(function () { showToast("Konuşma", "Konuşma açılamadı.", "#"); });
        }
        function openPanel() {
            panel.hidden = false;
            launcher.setAttribute("aria-expanded", "true");
            showList();
            loadList().catch(function () { });
        }
        function closePanel() {
            panel.hidden = true;
            launcher.setAttribute("aria-expanded", "false");
        }

        launcher.addEventListener("click", function () { isOpen() ? closePanel() : openPanel(); });
        closeBtn.addEventListener("click", closePanel);
        back.addEventListener("click", function () { showList(); loadList().catch(function () { }); });

        form.addEventListener("submit", function (e) {
            e.preventDefault();
            var text = input.value.trim();
            if (!text || !currentBlog) return;
            input.value = "";
            api("/api/chat/" + encodeURIComponent(currentBlog), {
                method: "POST",
                headers: { "Content-Type": "application/json", "RequestVerificationToken": token() },
                body: JSON.stringify({ body: text })
            }).then(function (m) {
                chatBubble(true, m.body, m.time);
            }).catch(function () {
                input.value = text;
                showToast("Mesaj gönderilemedi", "Lütfen tekrar deneyin.", "#");
            });
        });

        // Canlı: yeni mesaj geldiğinde panel açıksa balon, değilse sayaç ve liste güncellenir.
        connection.on("message", function (m) {
            if (isOpen() && currentBlog === m.fromBlog) {
                chatBubble(false, m.body, m.time);
                api("/api/chat/" + encodeURIComponent(m.fromBlog) + "/read", {
                    method: "POST", headers: { "RequestVerificationToken": token() }
                }).then(loadList).catch(function () { });
            } else {
                setTotal(m.unread);
                if (isOpen()) loadList().catch(function () { });
            }
        });

        loadList().catch(function () { });
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

        // Gönderi sayfası: başkalarının yanıtları ve düzenlemeleri canlı gelir.
        connection.on("reply", function (r) {
            var list = document.getElementById("replyList");
            if (!list || list.querySelector('[data-reply-id="' + r.id + '"]')) return;
            var row = document.createElement("div");
            row.className = "d-flex gap-2 mb-3 reply-item";
            row.setAttribute("data-reply-id", r.id);
            var avatar = document.createElement("span");
            avatar.className = "avatar avatar-initial";
            avatar.style.width = "32px"; avatar.style.height = "32px"; avatar.style.fontSize = "16px";
            avatar.style.background = "#6b7686";
            avatar.textContent = (r.displayName || "?").charAt(0).toUpperCase();
            var body = document.createElement("div");
            var who = document.createElement("a");
            who.href = "/blog/" + encodeURIComponent(r.blogName);
            who.className = "fw-semibold text-reset";
            who.textContent = r.blogName;
            var when = document.createElement("span");
            when.className = "small text-muted";
            when.textContent = " · az önce";
            var text = document.createElement("div");
            r.body.split("\n").forEach(function (line, i) {
                if (i > 0) text.appendChild(document.createElement("br"));
                text.appendChild(document.createTextNode(line));
            });
            body.appendChild(who); body.appendChild(when); body.appendChild(text);
            row.appendChild(avatar); row.appendChild(body);
            list.appendChild(row);
            showToast("Yeni yanıt", r.blogName + " yanıt yazdı", "#yanitlar");
        });

        connection.on("postUpdated", function (p) {
            if (window.kivilcimPostId === p.id) {
                showToast("Gönderi güncellendi", "Sayfa yenileniyor…", "#");
                setTimeout(function () { window.location.reload(); }, 900);
            }
        });

        initChatPanel(connection);

        connection.start().then(function () {
            if (window.kivilcimPostId) connection.invoke("JoinPost", window.kivilcimPostId).catch(function () { });
        }).catch(function () { /* bağlantı kurulamazsa sayfa yine çalışır */ });
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

    // ---- Fotoğraf düzenleyici: çoklu dosya + internetten bağlantı (en fazla 30) ----
    var editor = document.getElementById("photoEditor");
    if (editor) {
        var max = parseInt(editor.dataset.max, 10) || 30;
        var fileInput = document.getElementById("Images");
        var previews = document.getElementById("photoPreviews");
        var urlRow = document.getElementById("urlRow");
        var urlInput = document.getElementById("urlInput");
        var urlInputs = document.getElementById("urlInputs");
        var counter = document.getElementById("photoCount");
        var drop = document.getElementById("photoDrop");
        var files = [];   // { file, src }
        var urls = Array.prototype.map.call(urlInputs.querySelectorAll("input"), function (i) { return i.value; });
        var canEditFileList = typeof DataTransfer === "function";

        function total() { return files.length + urls.length; }

        function warn(text) { showToast("Fotoğraf", text, "#"); }

        function syncInputs() {
            if (canEditFileList) {
                var dt = new DataTransfer();
                files.forEach(function (f) { dt.items.add(f.file); });
                fileInput.files = dt.files;
            }
            urlInputs.innerHTML = "";
            urls.forEach(function (u) {
                var h = document.createElement("input");
                h.type = "hidden"; h.name = "ImageUrls"; h.value = u;
                urlInputs.appendChild(h);
            });
        }

        function tile(src, label, onRemove, external) {
            var t = document.createElement("div");
            t.className = "photo-tile" + (external ? " external" : "");
            var img = document.createElement("img");
            img.src = src; img.alt = label; img.loading = "lazy";
            if (external) { img.referrerPolicy = "no-referrer"; }
            img.onerror = function () { t.classList.add("broken"); };
            var rm = document.createElement("button");
            rm.type = "button"; rm.className = "photo-tile-remove"; rm.setAttribute("aria-label", label + " kaldır");
            rm.innerHTML = "&times;";
            rm.addEventListener("click", onRemove);
            t.appendChild(img); t.appendChild(rm);
            if (external) {
                var badge = document.createElement("span");
                badge.className = "photo-tile-badge"; badge.textContent = "🌐";
                t.appendChild(badge);
            }
            return t;
        }

        function render() {
            previews.innerHTML = "";
            files.forEach(function (f, i) {
                previews.appendChild(tile(f.src, f.file.name, function () {
                    URL.revokeObjectURL(f.src); files.splice(i, 1); syncInputs(); render();
                }, false));
            });
            urls.forEach(function (u, i) {
                previews.appendChild(tile(u, "Bağlantı " + (i + 1), function () {
                    urls.splice(i, 1); syncInputs(); render();
                }, true));
            });
            counter.textContent = total() + " / " + max;
            counter.classList.toggle("full", total() >= max);
            editor.classList.toggle("has-photos", total() > 0);
        }

        function addFiles(list) {
            var skipped = 0;
            Array.prototype.forEach.call(list, function (file) {
                if (!/^image\/(jpeg|png|gif|webp)$/.test(file.type)) { skipped++; return; }
                if (total() >= max) { skipped++; return; }
                files.push({ file: file, src: URL.createObjectURL(file) });
            });
            if (skipped) warn(skipped + " dosya eklenmedi (sadece JPG, PNG, GIF, WEBP; en fazla " + max + " resim).");
            syncInputs(); render();
        }

        fileInput.addEventListener("change", function () {
            if (!canEditFileList) { return; } // Eski tarayıcı: seçim olduğu gibi gönderilir.
            var chosen = Array.prototype.slice.call(fileInput.files);
            addFiles(chosen);
        });

        document.getElementById("pickUrl").addEventListener("click", function () {
            urlRow.hidden = false;
            urlInput.focus();
        });

        function addUrl() {
            var u = urlInput.value.trim();
            if (!u) return;
            if (!/^https?:\/\/\S+$/i.test(u)) { warn("Geçerli bir http(s) bağlantısı gir."); return; }
            if (urls.indexOf(u) >= 0) { warn("Bu bağlantı zaten ekli."); return; }
            if (total() >= max) { warn("En fazla " + max + " resim ekleyebilirsin."); return; }
            urls.push(u); urlInput.value = ""; syncInputs(); render();
        }
        document.getElementById("urlAdd").addEventListener("click", addUrl);
        urlInput.addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); addUrl(); } });

        document.getElementById("photoClear").addEventListener("click", function () {
            files.forEach(function (f) { URL.revokeObjectURL(f.src); });
            files = []; urls = []; urlRow.hidden = true; syncInputs(); render();
        });

        // Sürükle-bırak
        ["dragenter", "dragover"].forEach(function (ev) {
            drop.addEventListener(ev, function (e) { e.preventDefault(); drop.classList.add("dragging"); });
        });
        ["dragleave", "drop"].forEach(function (ev) {
            drop.addEventListener(ev, function (e) { e.preventDefault(); drop.classList.remove("dragging"); });
        });
        drop.addEventListener("drop", function (e) { if (e.dataTransfer && e.dataTransfer.files) addFiles(e.dataTransfer.files); });

        render();
    }

    // ---- Etiket kutucukları: "#etiket ekle" ----
    var tagEditor = document.getElementById("tagEditor");
    var tagsHidden = document.getElementById("Tags");
    if (tagEditor && tagsHidden) {
        var tagInput = document.getElementById("tagInput");
        var tags = [];
        var MAX_TAGS = 10;

        function normalize(t) {
            return t.replace(/^#+/, "").replace(/\s+/g, " ").trim().toLocaleLowerCase("tr").slice(0, 50);
        }
        function syncTags() { tagsHidden.value = tags.join(", "); }
        function renderTags() {
            Array.prototype.slice.call(tagEditor.querySelectorAll(".tag-chip")).forEach(function (c) { c.remove(); });
            tags.forEach(function (t, i) {
                var chip = document.createElement("span");
                chip.className = "tag-chip";
                chip.appendChild(document.createTextNode("#" + t));
                var x = document.createElement("button");
                x.type = "button"; x.setAttribute("aria-label", "#" + t + " etiketini kaldır"); x.innerHTML = "&times;";
                x.addEventListener("click", function () { tags.splice(i, 1); syncTags(); renderTags(); tagInput.focus(); });
                chip.appendChild(x);
                tagEditor.insertBefore(chip, tagInput);
            });
            tagInput.placeholder = tags.length ? "+ etiket" : "#etiket ekle";
            tagInput.hidden = tags.length >= MAX_TAGS;
        }
        function addTags(text) {
            text.split(/[,\n]/).forEach(function (raw) {
                var t = normalize(raw);
                if (t && tags.indexOf(t) < 0 && tags.length < MAX_TAGS) tags.push(t);
            });
            syncTags(); renderTags();
        }

        addTags(tagsHidden.value || "");
        tagEditor.addEventListener("click", function (e) { if (e.target === tagEditor) tagInput.focus(); });
        tagInput.addEventListener("keydown", function (e) {
            if (e.key === "Enter" || e.key === ",") {
                e.preventDefault();
                addTags(tagInput.value); tagInput.value = "";
            } else if (e.key === "Backspace" && !tagInput.value && tags.length) {
                tags.pop(); syncTags(); renderTags();
            }
        });
        tagInput.addEventListener("input", function () {
            if (tagInput.value.indexOf(",") >= 0) { addTags(tagInput.value); tagInput.value = ""; }
        });
        // Not: Yazılı kalan etiket odak kaybında değil, gönderirken eklenir. Odak kaybında eklemek
        // satırı kaydırıyor ve "Paylaş"a basma anında tıklamayı boşa düşürüyordu.
        tagEditor.closest("form").addEventListener("submit", function () {
            if (tagInput.value.trim()) { addTags(tagInput.value); tagInput.value = ""; }
        });
    }
})();
