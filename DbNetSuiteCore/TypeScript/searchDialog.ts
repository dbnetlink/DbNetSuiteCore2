class SearchDialog extends Dialog {
    lookupDialog: LookupDialog;
    constructor(dialog: HTMLDialogElement, componentControl: ComponentControl) {
        super(dialog, componentControl);
        this.lookupDialog = new LookupDialog(componentControl.controlElement(".lookup-dialog"), componentControl);
        this.dependentDialog = this.lookupDialog;
        this.bindSearchButton();
        this.control.getButton("clear").addEventListener("click", this.clear.bind(this))
        dialog.querySelectorAll(".search-operator").forEach(e => e.addEventListener("change", (e) => this.operatorSelected(e)))
        dialog.querySelectorAll("input").forEach(e => "input,change".split(',').forEach(en => e.addEventListener(en, this.valueEntered.bind({ event: e }))))
        dialog.querySelectorAll("button[button-type='list']").forEach(e => e.addEventListener("click", (e) => this.showLookup(e)))
    }

    bindSearchButton() {
        this.control.getButton("search").addEventListener("click", this.show.bind(this))
    }

    operatorSelected(event: Event) {
        let select = event.target as HTMLSelectElement;
        let tr = select.closest('tr');

        tr.querySelectorAll(".first").forEach(e => e.classList.remove("hidden"))

        switch (select.value) {
            case "Between":
            case "NotBetween":
                tr.querySelectorAll(".between").forEach(e => e.classList.remove("hidden"));
                break;
            case "IsEmpty":
            case "IsNotEmpty":
                tr.querySelectorAll(".between").forEach(e => e.classList.add("hidden"));
                tr.querySelectorAll(".first").forEach(e => e.classList.add("hidden"));
                break;
            default:
                tr.querySelectorAll(".between").forEach(e => e.classList.add("hidden"));
                break;
        }

        tr.querySelectorAll("input").forEach(e => {
            if (e.type != 'hidden') { e.required = false }
        });

        if (select.value == '') {
            tr.querySelectorAll("input").forEach(e => {
                if (e.type != 'hidden') { e.value = '' }
            });
        }
        else {
            tr.querySelectorAll("input").forEach(e => { if (this.isVisible(e)) e.required = true; });
        }
    }

    isVisible(el) {
        return !!(el.offsetWidth || el.offsetHeight || el.getClientRects().length);
    }

    valueEntered(event: Event) {
        let input = event.target as HTMLInputElement;
        let tr = input.closest('tr');
        let select = tr.querySelector("select");

        if (input.value != '' && select.value == '') {
            select.options[1].selected = true;
        }
        else if (input.value == '') {
            select.value = "";
        }
    }

    showLookup(event: Event) {
        let button = (event.target as HTMLElement).closest("button");
        let input = button.closest("tr").querySelector("input") as HTMLInputElement;

        let select = null;
        if (this.control instanceof (GridControl))
        {
            select = this.control.controlElement(`tr.lookup-refresh select[data-key='${button.dataset.key}']`)
        }
        if (this.control instanceof (FormControl)) {
            select = this.control.controlElement(`div.lookup-refresh select[data-key='${button.dataset.key}']`)
        }

        if (!select) {
            select = this.dialog.querySelector(`select[data-key='${button.dataset.key}']`) as HTMLSelectElement;
        }
        this.control
        this.lookupDialog.open(select, input);
    }

    clear() {
        this.dialog.querySelectorAll(".search-operator").forEach((e: HTMLSelectElement) => { e.value = ''; e.dispatchEvent(new Event('change')); });
    }
}