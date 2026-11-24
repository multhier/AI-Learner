document.addEventListener("DOMContentLoaded", function () {
  var rows = document.querySelectorAll("tbody tr").length;
  var el = document.getElementById("son-guncelleme");
  if (el) {
    el.textContent = "Toplam " + rows + " öğe listeleniyor.";
  }
});
