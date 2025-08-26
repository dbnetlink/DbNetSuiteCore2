Blazor.addEventListener("enhancedload", function () {
    var forms = document.querySelectorAll("form.dbnetsuite");
    forms.forEach(f => f.requestSubmit());
});