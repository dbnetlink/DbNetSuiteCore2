class LookupDialog extends Dialog {
    select: HTMLSelectElement;
    input: HTMLInputElement;
    caption: string;
    constructor(dialog: HTMLDialogElement, componentControl: ComponentControl) {
        super(dialog, componentControl);
        this.select = dialog.querySelector("select");
        this.control.getButton("cancel").addEventListener("click", () => this.close())
        this.control.getButton("select").addEventListener("click", () => this.apply())
    }

    open(select: HTMLSelectElement, input: HTMLInputElement, label : string) {
        this.select.innerHTML = select.innerHTML;
        var selectedValues = input.value.split(',');
        selectedValues.forEach(value => {
            const options = this.select.options;
            for (let i = 0; i < options.length; i++) {
                if (options[i].value === value) {
                    options[i].selected = true;
                }
            }
        });

        this.caption = label;
        (this.dialog.querySelector(".caption") as HTMLElement).innerText = label;

        this.input = input;
        this.show();
        this.select.focus();
    }

    apply() {
        var selectedValues = Array.from(this.select.selectedOptions).map(({ value }) => value);
        this.input.value = selectedValues.join(',');
        this.input.dispatchEvent(new Event('change'));
        this.close();
    }
}