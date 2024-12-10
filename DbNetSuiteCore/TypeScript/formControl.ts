class FormControl extends ComponentControl {
    form: HTMLFormElement;
    formMessage: HTMLDivElement;
    formBody: HTMLElement;
    formContainer: HTMLElement;
    confirmDialog: ConfirmDialog | null;
    cachedMessage: string | null;
    htmlEditorArray: Dictionary<HtmlEditor> = {};
    htmlEditorMissing = false;
    constructor(formId) {
        super(formId)
    }

    public afterRequest(evt) {
        if (this.isControlEvent(evt) == false) {
            return false
        }
        if (this.triggerName(evt) == "toolbar") {
            return;
        }

        this.formContainer = this.controlElement("div.form-container") as HTMLElement;
        this.formBody = this.controlElement("div.form-body") as HTMLElement;
        this.formMessage = this.controlElement("#form-message");

        if (!this.formBody) {
            return
        }

        this.notifyParent(this.formBody.dataset.mode.toLowerCase() == "update")


        switch (this.triggerName(evt)) {
            case "initialload":
                this.initialise();
                break;
            default:
                if (this.htmlEditorMissing == false) {
                    this.htmlEditorElements().forEach((el) => { this.htmlEditorArray[el.id].reset(el) });
                }
                break;
        }

        if (this.cachedMessage) {
            this.setMessage(this.cachedMessage);
        }

        this.updateLinkedChildControls(this.formBody.dataset.id)

        window.setTimeout(() => { this.clearErrorMessage() }, 3000)
        this.controlElements("select.fc-control.readonly").forEach((el) => { this.makeSelectReadonly(el) });
        this.controlElements("input.fc-control.readonly").forEach((el) => { this.makeCheckboxReadonly(el) });
        this.controlElements("input[data-texttransform]").forEach((el) => { this.transformText(el) });

        this.configureHtmlEditors();

        this.setFocus();
        this.invokeEventHandler('RecordLoaded');
    }

    public afterSettle(evt) {
        if (this.isControlEvent(evt) == false) {
            return false
        }

        if (!this.formBody) {
            return
        }

        this.cachedMessage = null;

        switch (this.triggerName(evt)) {
            case "apply":
                if (this.formBody.dataset.validationpassed == "True") {
                    this.clientSideValidation()
                }

                if (this.formBody.dataset.committype) {
                    if (this.parentControl) {
                        if (this.parentControl instanceof GridControl) {
                            this.cachedMessage = this.formMessage.innerText;
                            this.parentControl.refreshPage()
                        }
                    }
                }
                break;
        }
    }

    private triggerCommit() {
        let applyBtn = this.getButton("apply");
        htmx.trigger(applyBtn, "click");
    }

    private clientSideValidation() {
        let args = { mode: this.formBody.dataset.mode, message: '' }
        this.invokeEventHandler("ValidateUpdate", args);

        var inError = Boolean(args.message != '' || this.errorHighlighted());
        this.controlElement("input[name='validationPassed']").value = (inError == false).toString();
        if (inError) {
            this.setMessage(args.message != '' ? args.message : 'Highlighted fields are in error', 'error')
        }
        else {
            this.triggerCommit()
        }
    }

    private initialise() {
        document.body.addEventListener('htmx:configRequest', (ev) => { this.configRequest(ev) });
        document.body.addEventListener('htmx:beforeRequest', (ev) => { this.beforeRequest(ev) });
        document.body.addEventListener('htmx:confirm', (ev) => { this.confirmRequest(ev) });
        document.body.addEventListener('htmx:afterSettle', (ev) => { this.afterSettle(ev) });

        this.invokeEventHandler('Initialised');
    }

    private transformText(input) {
        input.addEventListener("input", (e) => {
            e.preventDefault();
            let el = e.target;
            el.value = el.dataset.texttransform == "Uppercase" ? el.value.toUpperCase() : el.value.toLowerCase();
        });
    }

    private updateLinkedChildControls(primaryKey: string) {
        if (this.formContainer.dataset.linkedcontrolids) {
            this.updateLinkedControls(this.formContainer.dataset.linkedcontrolids, primaryKey)
        }
    }

    private makeSelectReadonly(selectElement) {
        "change".split(" ").forEach(function (e) {
            selectElement.addEventListener(e, (e) => {
                e.preventDefault();
                selectElement.value = selectElement.dataset.value;
            });
        });
    }

    private makeCheckboxReadonly(checkboxElement) {
        "click".split(" ").forEach(function (e) {
            checkboxElement.addEventListener(e, (e) => {
                e.preventDefault();
                checkboxElement.checked = JSON.parse(checkboxElement.dataset.value);
            });
        });
    }

    public confirmRequest(evt) {
        if (this.isControlEvent(evt) == false || evt.target.hasAttribute('hx-confirm-dialog') == false) {
            return;
        }
        evt.preventDefault();
        if (!this.confirmDialog) {
            this.confirmDialog = new ConfirmDialog(this);
        }
        this.confirmDialog.show(evt, this.formBody);
    }

    public configRequest(evt) {
        if (this.isControlEvent(evt) == false) {
            return;
        }
        this.htmlEditorElements().forEach((el) => { this.htmlEditorArray[el.id].assignContent(evt) });

        this.controlElements(".fc-control").forEach((el) => {
            if (this.elementModified(el) == false) {
                delete evt.detail.parameters[el.name];
            }
            else if (evt.detail.parameters[el.name] == undefined) {
                evt.detail.parameters[el.name] = ''
            }
        });
    }

    public beforeRequest(evt) {
        if (this.isControlEvent(evt) == false)
            return;

        switch (this.triggerName(evt)) {
            case "apply":
                if (this.formModified() == false) {
                    evt.preventDefault();
                    return;
                }

                if (this.form.checkValidity() == false) {
                    this.form.reportValidity()
                    evt.preventDefault();
                    return;
                }

            case "cancel":
            case "primarykey":
                return;
        }

        this.controlElements(".fc-control").forEach((el) => { el.dataset.modified = this.elementModified(el) });
        let modified = this.controlElements(".fc-control[data-modified='true']");

        if (modified.length) {
            evt.preventDefault();
            this.setMessage(this.formBody.dataset.unappliedmessage, 'warning')
        }
    }

    public configureHtmlEditor(configuration: any, name:string) {
        this.invokeEventHandler('ConfigureHtmlEditor', { configuration: configuration, columnName: name });
    }

    private formControlValue(columnName: string) {
        var element: HTMLInputElement = this.formControl(columnName);

        if (!element) {
            console.error(`Form control for column name ${columnName} not found`)
        }
        else {
            if (element.tagName == 'INPUT' && element.type == 'checkbox') {
                return element.checked
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

    private highlightError(columnName: string) {
        var element: HTMLInputElement = this.formControl(columnName);
        element.dataset.error = "true";
    }

    private errorHighlighted() {
        let controlsInError = Array.from(this.controlElements(".fc-control")).filter((e) => { return (e.dataset.error == 'true'); }).length;
        return (controlsInError > 0);
    }

    private setFocus() {
        var selector = this.errorHighlighted() ? ".fc-control[data-error='true']" : ".fc-control"
        for (const el of this.controlElements(selector)) {
            if (el.readOnly == false && el.disabled == false) {
                el.focus();
                break;
            }
        }
    }

    private formControl(columnName: string) {
        var element: HTMLInputElement;
        this.controlElements(".fc-control").forEach((el) => {
            if (el.name.toLowerCase() == `_${columnName.toLowerCase()}`) { element = el; }
        });

        return element;
    }

    private formModified() {
        let modified = [];
        this.controlElements(".fc-control").forEach((el) => {
            if (this.elementModified(el)) { modified.push(el) }
        });

        return modified.length > 0;
    }

    private elementModified(el: HTMLFormElement) {
        if (el.tagName == 'INPUT' && el.type == 'checkbox') {
            return this.isBoolean(el.dataset.value) != el.checked;
        }
        else if (el.type == 'select-multiple') {
            var selectedValues = Array.from(el.selectedOptions).map(({ value }) => value);

            if (el.dataset.dbdatatype = 'Array') {
                return this.cleanString(el.dataset.value) != this.cleanString(selectedValues.join(''));
            }
            else {
                return el.dataset.value != selectedValues.join(',');
            }
        }
        else if (el.tagName == 'TEXTAREA') {
            return this.cleanString(el.dataset.value) != this.cleanString(el.value);
        }
        else {
            return el.dataset.value != el.value;
        }
    }

    private cleanString(value) {
        return value.replace("&amp;#xA;", "").replace(/[^a-z0-9\.]+/gi, "").trim()
    }

    private isBoolean(value: string) {
        return value == "1" || value.toLowerCase() == "true"
    }

    private setMessage(message: string, type: string = 'success') {
        this.formMessage.innerText = message;
        this.formMessage.dataset.highlight = type.toLowerCase();
        window.setTimeout(() => { this.clearErrorMessage() }, 3000)
    }

    private clearErrorMessage() {
        this.formMessage.innerHTML = "&nbsp";
        delete this.formMessage.dataset.highlight;
        this.controlElements(`.fc-control`).forEach((el) => { el.dataset.modified = false; el.dataset.error = false });
    }

    private htmlEditorElements(): NodeListOf<HTMLTextAreaElement> {
        return this.controlElements("textarea[data-htmleditor]")
    }

    private configureHtmlEditors() {
        let elements = this.htmlEditorElements();
        if (elements.length == 0) {
            return;
        }
        let editor = elements[0].dataset.htmleditor
        if (!HtmlEditor.editor(editor)) {
            this.setMessage(`${editor} library not available.`, "error");
            this.htmlEditorElements().forEach((el) => {
                HtmlEditor.removeElement(el);
                el.classList.remove("hidden");
                el.removeAttribute('data-htmleditor');
            });
            this.htmlEditorMissing = true;
            return;
        }

        window.setTimeout(() => { this.initHtmlEditor() }, 1)
    }

    private initHtmlEditor() {
        this.htmlEditorElements().forEach((el) => {
            this.htmlEditorArray[el.id] = new HtmlEditor(el, this);
        });
    }
}
