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
        let url = '';
        if (target.selectedOptions.length) {
            var dataset = target.selectedOptions[0].dataset;
            url = this.dataSourceIsFileSystem() && dataset.isdirectory && dataset.isdirectory.toLowerCase() == "true" ? dataset.path : '';
        }
        this.updateLinkedSelects(target.value, url);
        this.invokeEventHandler('OptionSelected', { selectedOptions: target.selectedOptions });
    }
    updateLinkedSelects(primaryKey, url) {
        if (this.select.dataset.linkedcontrolids) {
            this.updateLinkedControls(this.select.dataset.linkedcontrolids, primaryKey, url);
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
