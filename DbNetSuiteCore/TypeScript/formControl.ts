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
        document.body.addEventListener('htmx:configRequest', (ev) => { this.filterRequest(ev) });
        this.invokeEventHandler('Initialised');
    }

    private configureNavigation() {
        let formBody = this.controlElement("div.form-body") as HTMLElement;

        let currentRecord = parseInt(formBody.dataset.currentrecord);
        let recordCount = parseInt(formBody.dataset.recordcount);
        let message = formBody.dataset.message;

        if (message != '') {
            this.formMessage.innerText = message;
            let self = this
            window.setTimeout(() => { self.clearErrorMessage() }, 3000)
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

            this.setPageNumber(currentRecord, recordCount,"record");
            (this.controlElement('[data-type="record-count"]') as HTMLInputElement).value = recordCount.toString();

            this.getButton("first").disabled = currentRecord == 1;
            this.getButton("previous").disabled = currentRecord == 1;
            this.getButton("next").disabled = currentRecord == recordCount;
            this.getButton("last").disabled = currentRecord == recordCount;
        }
    }

    public filterRequest(evt) {
        if (this.isControlEvent(evt) == false || evt.detail.headers["HX-Trigger-Name"] == "apply") {
            return;
        }

        for (var p in evt.detail.parameters) {
            if (typeof (evt.detail.parameters[p]) == 'string' && p.startsWith("_")) {
                delete evt.detail.parameters[p];
            };
        }
    }

    private clearErrorMessage() {
        this.formMessage.innerHTML = "&nbsp";
        this.controlElements(".bg-red-400").forEach((el) => { el.classList.remove("bg-red-400") });
    }
}