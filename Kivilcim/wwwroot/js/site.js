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
