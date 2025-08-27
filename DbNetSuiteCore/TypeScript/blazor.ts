// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
Blazor.addEventListener("enhancedload", function () {
var forms = document.querySelectorAll("form.dbnetsuite");
    forms.forEach(f => (f as any).requestSubmit());
});