class SelectControl extends ComponentControl {
    constructor(selectId) {
        super(selectId)
    }

    afterRequest(evt) {
        let selectId = evt.target.closest("form").id;
        if (selectId.startsWith(this.controlId) == false) {
            return
        }

        if (this.triggerName(evt) == "initialload") {
            this.initialise()
        }
    }

    private initialise() {
        this.controlElement("select").addEventListener("change", (ev: Event) => {
            this.selectChanged(ev.target as HTMLSelectElement);
        })
        this.invokeEventHandler('Initialised');
    }

    private selectChanged(target: HTMLSelectElement) {
        this.invokeEventHandler('OptionSelected', { selectedOptions: target.selectedOptions });
    }
}
