// Semt seçici: bölge kutusu tüm semtlerini seçer/temizler
document.querySelectorAll('.area-toggle').forEach(function (box) {
    var hoods = document.querySelectorAll('input[name="Hoods"][data-area="' + box.dataset.area + '"]');
    box.checked = hoods.length > 0 && Array.prototype.every.call(hoods, function (h) { return h.checked; });
    box.addEventListener('change', function () { hoods.forEach(function (h) { h.checked = box.checked; }); });
});
var clear = document.getElementById('clearHoods');
if (clear) clear.addEventListener('click', function () {
    document.querySelectorAll('input[name="Hoods"], .area-toggle').forEach(function (h) { h.checked = false; });
});

// Harita görünümü (Leaflet + OpenStreetMap)
var mapEl = document.getElementById('map');
if (mapEl && window.L) {
    var points = JSON.parse(mapEl.dataset.points || '[]');
    var map = L.map(mapEl).setView([40.75, -73.97], 12);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19, attribution: '&copy; OpenStreetMap'
    }).addTo(map);
    var bounds = [];
    points.forEach(function (p) {
        var m = L.marker([p.lat, p.lng]).addTo(map);
        var a = document.createElement('a');
        a.href = '/properties/' + p.id;
        a.textContent = p.title + ' — ' + p.price;
        m.bindPopup(a);
        bounds.push([p.lat, p.lng]);
    });
    if (bounds.length) map.fitBounds(bounds, { padding: [30, 30] });
}
