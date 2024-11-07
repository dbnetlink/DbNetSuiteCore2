class SelectControl extends ComponentControl {
    constructor(selectId) {
        super(selectId);
    }
    afterRequest(evt) {
        let selectId = evt.target.closest("form").id;
        if (selectId.startsWith(this.controlId) == false) {
            return;
        }
        if (this.triggerName(evt) == "initialload") {
            this.initialise();
        }
    }
    initialise() {
        this.controlElement("select").addEventListener("change", (ev) => {
            this.selectChanged(ev.target);
        });
        this.invokeEventHandler('Initialised');
    }
    selectChanged(target) {
        this.invokeEventHandler('OptionSelected', { selectedOptions: target.selectedOptions });
    }
}
