class FormControl extends ComponentControl {
    form: HTMLFormElement;
    formContainer: HTMLElement;
    confirmDialog: ConfirmDialog | null;
    cachedMessage: string | null;
    cachedMessageType: string | null;
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

        this.notifyParent(this.formMode() == "update")

        switch (this.triggerName(evt)) {
            case "initialload":
                this.initialise();
                break;
            default:
                if (this.searchDialog) {
                    this.searchDialog.bindSearchButton();
                }
                if (this.htmlEditorMissing == false) {
                    this.htmlEditorElements().forEach((el) => { this.htmlEditorArray[el.id].reset(el) });
                }
                break;
        }

        if (this.cachedMessage) {
            this.setMessage(this.cachedMessage, this.cachedMessageType);
        }

        this.updateLinkedChildControls(this.formBody.dataset.id)

        window.setTimeout(() => { this.clearErrorMessage() }, 3000)
        this.controlElements("select.fc-control.readonly").forEach((el) => { this.makeSelectReadonly(el) });
        this.controlElements("input.fc-control.readonly").forEach((el) => { this.makeCheckboxReadonly(el) });
        this.controlElements("input[data-texttransform]").forEach((el) => { this.transformText(el) });
        this.reassignFormCheckboxValue();

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
                    this.validateUpdate()
                }

                if (this.formBody.dataset.committype) {
                    this.refreshParent();
                }
                break;
            case "delete":
                this.refreshParent();
                break;
        }
    }

    private refreshParent() {
        if (this.parentControl) {
            if (this.parentControl instanceof GridControl) {
                this.cachedMessage = this.formMessage.innerText;
                this.cachedMessageType = this.formMessage.dataset.highlight;
                this.parentControl.refreshPage()
            }
        }
    }

    private validateUpdate() {
        let args = { mode: this.formBody.dataset.mode, message: '' }
        this.invokeEventHandler("ValidateUpdate", args);

        var inError = Boolean(args.message != '' || this.errorHighlighted(this.form));
        this.controlElement("input[name='validationPassed']").value = (inError == false).toString();
        if (inError) {
            this.setMessage(args.message != '' ? args.message : 'Highlighted fields are in error', 'error')
        }
        else {
            this.triggerCommit()
        }
    }

    private async validateDelete() {
        let args = { message: '' }
        if (this.invokeEventHandler("ValidateDelete", args) == false) {
            return true;
        }

        var inError = Boolean(args.message != '' || this.errorHighlighted(this.form));
        if (inError) {
            this.setMessage(args.message != '' ? args.message : 'Deletion not allowed', 'error')
            return false;
        }
        return true;
    }

    private initialise() {
        document.body.addEventListener('htmx:configRequest', (ev) => { this.configRequest(ev) });
        document.body.addEventListener('htmx:beforeRequest', (ev) => { this.beforeRequest(ev) });
        document.body.addEventListener('htmx:confirm', (ev) => { this.confirmRequest(ev) });
        document.body.addEventListener('htmx:afterSettle', (ev) => { this.afterSettle(ev) });
        this.assignSearchDialog();

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

    public highlightError(columnName: string, row: HTMLTableRowElement = null) {
        var element: HTMLFormElement = this.formControl(columnName, row);
        if (element) {
            element.dataset.error = "true";
        }
    }

    public confirmRequest(evt) {
        if (this.isControlEvent(evt) == false || evt.target.hasAttribute('hx-confirm-dialog') == false) {
            return;
        }
        evt.preventDefault();
        if (!this.confirmDialog) {
            let prompt = evt.target.getAttribute('hx-confirm-dialog')
            this.confirmDialog = new ConfirmDialog(this, prompt);
        }

        if (evt.srcElement.name == "delete") {
            if (!this.validateDelete()) {
                return;
            }
        }
        this.confirmDialog.open(evt);
    }

    public configRequest(evt) {
        if (this.isControlEvent(evt) == false) {
            return;
        }
        this.htmlEditorElements().forEach((el) => { this.htmlEditorArray[el.id].assignContent(evt) });

        evt.detail.parameters["modifiedform"] = JSON.stringify(this.getFormModification(this.formBody));

        this.controlElements(".fc-control").forEach((el) => {
            if (evt.detail.parameters[el.name] == undefined) {
                evt.detail.parameters[el.name] = ''
            }
        });
    }

    public beforeRequest(evt) {
        if (this.isControlEvent(evt) == false)
            return;

        if (this.validateSearchDialog(evt) == false) {
            return;
        }

        switch (this.triggerName(evt)) {
            case "apply":
                if (this.formModified() == false) {
                    evt.preventDefault();
                }
                else if (this.form.checkValidity() == false) {
                    this.form.reportValidity()
                    evt.preventDefault();
                }
                return
            case "cancel":
            case "primarykey":
                return;
        }

        if (this.formMode() != "empty") {
            this.controlElements(".fc-control").forEach((el) => { el.dataset.modified = this.elementModified(el, true) });
            let modified = this.controlElements(".fc-control[data-modified='true']");

            if (modified.length) {
                evt.preventDefault();
                this.setMessage(this.formBody.dataset.unappliedmessage, 'warning')
            }
        }
    }

    public configureHtmlEditor(configuration: any, name: string) {
        this.invokeEventHandler('ConfigureHtmlEditor', { configuration: configuration, columnName: name });
    }

    private setFocus() {
        var selector = this.errorHighlighted(this.form) ? ".fc-control[data-error='true']" : ".fc-control"
        for (const el of this.controlElements(selector)) {
            if (el.readOnly == false && el.disabled == false) {
                el.focus();
                break;
            }
        }
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
