class SelectControl extends ComponentControl {
    constructor(selectId) {
        super(selectId);
    }
    afterRequest(evt) {
        let selectId = evt.target.closest("form").id;
        if (selectId.startsWith(this.controlId) == false) {
            return;
        }
        this.select = this.controlElement("select");
        let selectElements = this.controlElements("select");
        this.select.innerHTML = selectElements[1].innerHTML;
        selectElements[1].remove();
        if (this.triggerName(evt) == "initialload") {
            this.initialise();
        }
        this.selectChanged(this.select);
        this.checkForError();
    }
    initialise() {
        this.controlElement("select").addEventListener("change", (ev) => {
            this.selectChanged(ev.target);
        });
        this.invokeEventHandler('Initialised');
    }
    selectChanged(target) {
        this.updateLinkedSelects(target.value);
        this.invokeEventHandler('OptionSelected', { selectedOptions: target.selectedOptions });
    }
    updateLinkedSelects(primaryKey) {
        if (this.select.dataset.linkedselectids) {
            this.updateLinkedControls(this.select.dataset.linkedselectids, primaryKey);
        }
    }
    checkForError() {
        var select = this.controlElement("select");
        const error = select.querySelector("div");
        if (error) {
            select.parentElement.nextElementSibling.after(error);
        }
    }
}
