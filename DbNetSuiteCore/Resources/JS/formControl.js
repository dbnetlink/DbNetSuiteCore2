class FormControl extends ComponentControl {
    constructor(formId) {
        super(formId);
        this.formMessage = this.controlElement("#form-message");
        this.toolbar = this.controlElement("#toolbar");
        this.confirmDialog = new ConfirmDialog(this);
    }
    afterRequest(evt) {
        if (this.isControlEvent(evt) == false) {
            return false;
        }
        if (this.triggerName(evt) == "toolbar") {
            return;
        }
        this.formBody = this.controlElement("div.form-body");
        if (!this.formBody) {
            return;
        }
        this.form = this.controlElement("form");
        if (this.triggerName(evt) == "initialload") {
            this.initialise();
        }
        let formBody = this.controlElement("div.form-body");
        this.setMessage(formBody.dataset.message, formBody.dataset.messagetype);
        this.invokeEventHandler('FormLoaded');
    }
    initialise() {
        document.body.addEventListener('htmx:configRequest', (ev) => { this.configRequest(ev); });
        document.body.addEventListener('htmx:beforeRequest', (ev) => { this.beforeRequest(ev); });
        document.body.addEventListener('htmx:confirm', (ev) => { this.confirmRequest(ev); });
        this.controlElements("select.fc-control.readonly").forEach((el) => { this.makeSelectReadonly(el); });
        this.controlElements("input.fc-control.readonly").forEach((el) => { this.makeCheckboxReadonly(el); });
        this.invokeEventHandler('Initialised');
    }
    makeSelectReadonly(selectElement) {
        "change".split(" ").forEach(function (e) {
            selectElement.addEventListener(e, (e) => {
                e.preventDefault();
                selectElement.value = selectElement.dataset.value;
            });
        });
    }
    makeCheckboxReadonly(checkboxElement) {
        "click".split(" ").forEach(function (e) {
            checkboxElement.addEventListener(e, (e) => {
                e.preventDefault();
                checkboxElement.checked = JSON.parse(checkboxElement.dataset.value);
            });
        });
    }
    confirmRequest(evt) {
        if (this.isControlEvent(evt) == false || evt.target.hasAttribute('hx-confirm-dialog') == false) {
            return;
        }
        evt.preventDefault();
        this.confirmDialog.show(evt, this.formBody);
    }
    configRequest(evt) {
        if (this.isControlEvent(evt) == false || this.triggerName(evt) == "apply") {
            return;
        }
        for (var p in evt.detail.parameters) {
            if (typeof (evt.detail.parameters[p]) == 'string' && p.startsWith("_")) {
                delete evt.detail.parameters[p];
            }
            ;
        }
    }
    beforeRequest(evt) {
        if (this.isControlEvent(evt) == false)
            return;
        switch (this.triggerName(evt)) {
            case "apply":
                if (this.formModified() == false) {
                    evt.preventDefault();
                    return;
                }
            case "cancel":
                return;
        }
        this.controlElements(".fc-control").forEach((el) => { el.dataset.modified = this.elementModified(el); });
        let modified = this.controlElements(".fc-control[data-modified='true']");
        if (modified.length) {
            evt.preventDefault();
            this.setMessage(this.formBody.dataset.unappliedmessage, 'warning');
        }
    }
    formModified() {
        let modified = [];
        this.controlElements(".fc-control").forEach((el) => {
            if (this.elementModified(el)) {
                modified.push(el);
            }
        });
        return modified.length > 0;
    }
    elementModified(el) {
        if (el.tagName == 'INPUT' && el.type == 'checkbox') {
            return Number(el.dataset.value) != el.checked;
        }
        else {
            return el.dataset.value != el.value;
        }
    }
    setMessage(message, type = 'success') {
        if (!message) {
            return;
        }
        this.formMessage.innerText = message;
        this.formMessage.dataset.highlight = type.toLowerCase();
        let self = this;
        window.setTimeout(() => { self.clearErrorMessage(); }, 3000);
    }
    clearErrorMessage() {
        this.formMessage.innerHTML = "&nbsp";
        delete this.formMessage.dataset.highlight;
        this.controlElements(`.fc-control`).forEach((el) => { el.dataset.modified = false; el.dataset.error = false; });
    }
}
