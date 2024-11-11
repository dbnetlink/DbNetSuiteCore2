class SelectControl extends ComponentControl {
    select: HTMLSelectElement;
    constructor(selectId) {
        super(selectId)
    }

    afterRequest(evt) {
        let selectId = evt.target.closest("form").id;
        if (selectId.startsWith(this.controlId) == false) {
            return
        }

        this.select = this.controlElement("select");

        let selectElements = this.controlElements("select");
        this.select.innerHTML = selectElements[1].innerHTML;
        selectElements[1].remove();

        if (this.triggerName(evt) == "initialload") {
            this.initialise()
        }

        this.selectChanged(this.select);

        this.checkForError();
    }

    private initialise() {
        this.controlElement("select").addEventListener("change", (ev: Event) => {
            this.selectChanged(ev.target as HTMLSelectElement);
        })
        this.invokeEventHandler('Initialised');
    }

    private selectChanged(target: HTMLSelectElement) {
        this.updateLinkedSelects(target.value);
        this.invokeEventHandler('OptionSelected', { selectedOptions: target.selectedOptions });
    }

    private updateLinkedSelects(primaryKey: string) {
        if (this.select.dataset.linkedcontrolids) {
            this.updateLinkedControls(this.select.dataset.linkedcontrolids, primaryKey)
        }
    }

    private checkForError() {
        var select = this.controlElement("select") as HTMLSelectElement;
        const error = select.querySelector("div");
        if (error) {
            select.parentElement.nextElementSibling.after(error)
        }
    }
}