// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
Blazor.addEventListener("enhancedload", function () {
var forms = document.querySelectorAll("form.dbnetsuite:not([loaded])");
 //   forms.forEach(f => { htmx.trigger(f as HTMLFormElement, "submit"); (f as HTMLFormElement).setAttribute('loaded', ''); });
});