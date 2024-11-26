class FormControl extends ComponentControl {
    constructor(formId) {
        super(formId);
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
        this.formMessage = this.controlElement("#form-message");
        if (!this.formBody) {
            return;
        }
        switch (this.triggerName(evt)) {
            case "initialload":
                this.initialise();
                break;
        }
        window.setTimeout(() => { this.clearErrorMessage(); }, 3000);
        this.invokeEventHandler('RecordLoaded');
    }
    afterSettle(evt) {
        if (this.isControlEvent(evt) == false) {
            return false;
        }
        if (!this.formBody) {
            return;
        }
        switch (this.triggerName(evt)) {
            case "apply":
                if (this.formBody.dataset.validationpassed == "True") {
                    this.clientSideValidation();
                }
                break;
        }
    }
    triggerCommit() {
        let applyBtn = this.getButton("apply");
        htmx.trigger(applyBtn, "click");
    }
    clientSideValidation() {
        let args = { mode: this.formBody.dataset.mode, message: '' };
        this.invokeEventHandler("ValidateUpdate", args);
        var inError = Boolean(args.message != '' || this.errorHighlighted());
        this.controlElement("input[name='validationPassed']").value = (inError == false).toString();
        if (inError) {
            this.setMessage(args.message != '' ? args.message : 'Highlighted fields are in error', 'error');
        }
        else {
            this.triggerCommit();
        }
    }
    initialise() {
        document.body.addEventListener('htmx:configRequest', (ev) => { this.configRequest(ev); });
        document.body.addEventListener('htmx:beforeRequest', (ev) => { this.beforeRequest(ev); });
        document.body.addEventListener('htmx:confirm', (ev) => { this.confirmRequest(ev); });
        document.body.addEventListener('htmx:afterSettle', (ev) => { this.afterSettle(ev); });
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
    formControlValue(columnName) {
        var element = this.formControl(columnName);
        if (!element) {
            console.error(`Form control for column name ${columnName} not found`);
        }
        else {
            if (element.tagName == 'INPUT' && element.type == 'checkbox') {
                return element.checked;
            }
            switch (element.dataset.datatype.toLowerCase()) {
                case 'datetime':
                    return element.dataset.jsdate ? new Date(Number(element.dataset.jsdate)) : null;
                case 'string':
                    return element.value;
                default:
                    return element.value ? Number(element.value) : null;
            }
        }
    }
    highlightError(columnName) {
        var element = this.formControl(columnName);
        element.dataset.error = "true";
    }
    errorHighlighted() {
        let controlsInError = Array.from(this.controlElements(".fc-control")).filter((e) => { return (e.dataset.error == 'true'); }).length;
        return (controlsInError > 0);
    }
    formControl(columnName) {
        var element;
        this.controlElements(".fc-control").forEach((el) => {
            if (el.name.toLowerCase() == `_${columnName.toLowerCase()}`) {
                element = el;
            }
        });
        return element;
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
        this.formMessage.innerText = message;
        this.formMessage.dataset.highlight = type.toLowerCase();
        window.setTimeout(() => { this.clearErrorMessage(); }, 3000);
    }
    clearErrorMessage() {
        this.formMessage.innerHTML = "&nbsp";
        delete this.formMessage.dataset.highlight;
        this.controlElements(`.fc-control`).forEach((el) => { el.dataset.modified = false; el.dataset.error = false; });
    }
}
