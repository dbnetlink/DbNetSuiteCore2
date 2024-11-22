class FormControl extends ComponentControl {
    form: HTMLFormElement;
    formMessage: HTMLDivElement;

    constructor(formId) {
        super(formId)
        this.formMessage = this.controlElement("#form-message");
    }

    public afterRequest(evt) {
        if (this.isControlEvent(evt) == false) {
            return false
        }

        if (!this.controlElement("div.form-body")) {
            return
        }

        this.configureNavigation()
       
        this.form = this.controlElement("form");

        if (this.triggerName(evt) == "initialload") {
            this.initialise()
        }

        this.invokeEventHandler('FormLoaded');
    }

    private initialise() {
        document.body.addEventListener('htmx:configRequest', (ev) => { this.configRequest(ev) });
        document.body.addEventListener('htmx:beforeRequest', (ev) => { this.beforeRequest(ev) });

        this.invokeEventHandler('Initialised');
    }

    private setMessage(message:string) {
        this.formMessage.innerText = message;
        let self = this
        window.setTimeout(() => { self.clearErrorMessage() }, 3000)
    }

    private configureNavigation() {
        let formBody = this.controlElement("div.form-body") as HTMLElement;

        let currentRecord = parseInt(formBody.dataset.currentrecord);
        let recordCount = parseInt(formBody.dataset.recordcount);
        let message = formBody.dataset.message;

        if (message != '') {
            this.setMessage(message);
        }

        if (this.toolbarExists()) {

            if (recordCount == 0) {
                this.removeClass('#no-records', "hidden");
                this.addClass('#navigation', "hidden");
            }
            else {
                this.addClass('#no-records', "hidden");
                this.removeClass('#navigation', "hidden");
            }

            ["apply", "cancel"].forEach(n => this.getButton(n).style.display = recordCount == 0 ? "none" : "");
            ["first", "previous"].forEach(n => this.getButton(n).disabled = currentRecord == 1);
            ["next", "last"].forEach(n => this.getButton(n).disabled = currentRecord == recordCount);

            this.setPageNumber(currentRecord, recordCount,"record");
            (this.controlElement('[data-type="record-count"]') as HTMLInputElement).value = recordCount.toString();
        }
    }

    public configRequest(evt) {
        if (this.isControlEvent(evt) == false || evt.detail.headers["HX-Trigger-Name"] == "apply") {
            return;
        }

        for (var p in evt.detail.parameters) {
            if (typeof (evt.detail.parameters[p]) == 'string' && p.startsWith("_")) {
                delete evt.detail.parameters[p];
            };
        }
    }

    public beforeRequest(evt) {
        if (this.isControlEvent(evt) == false)
            return;

        switch (evt.detail.requestConfig.headers["HX-Trigger-Name"])
        {
            case "apply":
            case "cancel":
                return;
        }

        var modified = false;
        this.controlElements(".form-control").forEach((el) => {
            if (el.dataset.value != el.value) {
                modified = true;
            }
        });

        if (modified) {
            evt.preventDefault();
            this.setMessage("You have unapplied changes. Please apply or cancel.")
        }
    }

    private clearErrorMessage() {
        this.formMessage.innerHTML = "&nbsp";
        this.controlElements(`.bg-red-400`).forEach((el) => { el.classList.remove("bg-red-400") });
    }
}