interface Dictionary<T> {
    [Key: string]: T;
}

var DbNetSuiteCore: any = {};
var controlArray: Dictionary<ComponentControl> = {}
DbNetSuiteCore.controlArray = controlArray;
DbNetSuiteCore.createClientControl = function (controlId: string, clientEvents: object, deferredLoad: boolean = false) {
    document.addEventListener('htmx:afterRequest', function (evt) {
        DbNetSuiteCore.assignClientControl(controlId, clientEvents, deferredLoad);
        DbNetSuiteCore.controlArray[controlId].afterRequest(evt);
    });

    if (deferredLoad) {
        DbNetSuiteCore.assignClientControl(controlId, clientEvents, deferredLoad);
    }
}

DbNetSuiteCore.assignClientControl = function (controlId: string, clientEvents: object, deferredLoad: boolean = false) {
    if (!DbNetSuiteCore.controlArray[controlId]) {

        var clientControl = {}

        if (controlId.startsWith("Grid")) {
            clientControl = new GridControl(controlId, deferredLoad);
        }
        if (controlId.startsWith("Select")) {
            clientControl = new SelectControl(controlId);
        }
        if (controlId.startsWith("Form")) {
            clientControl = new FormControl(controlId);
        }
        for (const [key, value] of Object.entries(clientEvents)) {
            const functionNameParts: Array<String> = value.toString().split('.') as Array<string>;
            try {
                if (functionNameParts.length > 1) {
                    (clientControl as ComponentControl).eventHandlers[key] = {
                        type: window[functionNameParts[0].toString()][functionNameParts[1].toString()],
                        name: value.toString()
                    }
                }
                else {
                    (clientControl as ComponentControl).eventHandlers[key] = {
                        type: window[functionNameParts[0].toString()],
                        name: value.toString()
                    }
                }
            }
            catch (ex) {
                console.error(`Client-side event handler => ${value} not found`);
            }
        }
        DbNetSuiteCore.controlArray[controlId] = clientControl;
    }
}

class ComponentControl {
    public controlId: string = "";
    public form: HTMLFormElement;
    parentControl: ComponentControl;
    childControls: Dictionary<ComponentControl> = {};
    controlContainer: HTMLElement;
    eventHandlers = {};
    searchDialog: SearchDialog;
    public formBody: HTMLElement;
    formMessage: HTMLDivElement;
    currentValidationRow: HTMLTableRowElement;

    constructor(controlId) {
        this.controlId = controlId;
        this.form = document.querySelector(this.formSelector());
        this.form.style.display = '';
        this.controlContainer = this.form.parentElement;
    }

    public setCaption(text) {
        var caption = this.controlElement("div.caption");
        if (caption) {
            caption.innerText = text;
        }
    }

    protected isControlEvent(evt) {
        let formId = evt.target.closest("form").id;
        return formId.startsWith(this.controlId);
    }

    protected invokeEventHandler(eventName, args = {}) {
        //  window.dispatchEvent(new CustomEvent(`Grid${eventName}`, { detail: this.controlId }));
        if (this.eventHandlers.hasOwnProperty(eventName) == false) {
            return false;
        }
        if (typeof this.eventHandlers[eventName].type === 'function') {
            const functionNameParts: Array<String> = this.eventHandlers[eventName].name.split('.') as Array<string>;
            if (functionNameParts.length > 1) {
                (window as any)[functionNameParts[0].toString()][functionNameParts[1].toString()](this, args);
            }
            else {
                (window as any)[functionNameParts[0].toString()](this, args);
            }
        }
        else {
            this.toast(`Javascript function for event type '${eventName}' is not defined`, 'error', 3);
        }

        return true;
    }

    protected eventHandlerAttached(eventName, args = {}) {
        return (typeof this.eventHandlers[eventName] === 'function')
    }

    protected toast(text, style = 'info', delay = 1) {
        var toast = this.controlContainer.querySelector("#toastMessage") as HTMLElement;
        //toast.classList.add(`alert-${style}`)
        toast.querySelector("span").innerText = text;
        if (text == "") {
            toast.parentElement.style.marginLeft = `-${toast.parentElement.clientWidth / 2}px`;
            toast.parentElement.style.marginTop = `-${toast.parentElement.clientHeight / 2}px`;
            toast.parentElement.style.display = 'none';
            return;
        }
        toast.parentElement.style.display = 'block';
        let self = this;
        window.setTimeout(() => { self.toast(""); }, delay * 1000);
    }

    protected formSelector() {
        return `#${this.controlId}`;
    }

    protected controlElements(selector) {
        return this.form.querySelectorAll(selector);
    }

    public controlElement(selector) {
        return this.form.querySelector(selector);
    }

    protected triggerName(evt: any) {
        let headers = evt.detail.headers ? evt.detail.headers : evt.detail.requestConfig.headers;
        return headers["HX-Trigger-Name"] ? headers["HX-Trigger-Name"].toLowerCase() : "";
    }

    protected triggerElement(evt: any): HTMLElement {
        return evt.detail.requestConfig.elt;
    }

    protected updateLinkedControls(linkedIds: string, selectedIndex: string = null, url: string = null) {
        if (!linkedIds) {
            return;
        }
        var linkedIdArray = linkedIds.split(",");

        linkedIdArray.forEach(linkedId => {
            this.isElementLoaded(`#${linkedId}`).then((selector) => {
                var linkedControl = DbNetSuiteCore.controlArray[linkedId];
                if (!linkedControl) {
                    return;
                }
                linkedControl.parentControl = this;
                this.childControls[linkedId] = linkedControl;
                var summaryModel = null;
                var rowIndex = null;
                summaryModel = this.controlElement("input[name='summarymodel']").value;
                if (this instanceof GridControl)
                {   
                    var gridControl = this as GridControl;
                    if (gridControl.selectedRow) {
                        rowIndex = gridControl.selectedRow.dataset.idx;
                     }
                }
                if (this instanceof SelectControl) {
                    rowIndex = selectedIndex;
                }
                linkedControl.loadFromParent(summaryModel, rowIndex, url);
            });
        });
    }

    public notifyParent(records: boolean) {
        if (this.parentControl) {
            this.parentControl.childLoaded(records)
        }
    }

    public childLoaded(records: boolean) {
        if (this instanceof FormControl) {
            let deleteButton = this.getButton("delete")
            if (deleteButton) {
                deleteButton.disabled = records;
            }
        }
    }

    public dataSourceIsFileSystem() {
        return this.form.dataset.datasourcetype == "FileSystem";
    }

    protected loadFromParent(parentModel: string, rowIndex: string, url: string) {
        let selector = `#${this.controlId} input[name="primaryKey"]`;
        let pk = htmx.find(selector) as HTMLInputElement;

        this.form.setAttribute("hx-vals", JSON.stringify({ url: url ?? '', parentModel: parentModel, rowIndex: rowIndex ?? '' }));

        if (pk) {
            htmx.trigger(selector, "changed");
        }
        else {
            htmx.trigger(`#${this.controlId}`, "submit");
        }
    }

    protected toolbarExists() {
        return this.controlElement('#navigation');
    }

    protected isElementLoaded = async (selector) => {
        while (document.querySelector(selector) === null) {
            await new Promise(resolve => requestAnimationFrame(resolve));
        }
        return document.querySelector(selector);
    };

    protected removeClass(selector: string, className: string) {
        let e = this.controlElement(selector);
        if (e) {
            e.classList.remove(className);
        }
    }

    protected addClass(selector: string, className: string) {
        let e = this.controlElement(selector);
        if (e) {
            e.classList.add(className);
        }
    }

    public getButton(name): HTMLButtonElement {
        return this.controlElement(this.buttonSelector(name));
    }

    public buttonSelector(buttonType) {
        return `button[button-type="${buttonType}"]`;
    }

    protected setPageNumber(pageNumber: number, totalPages: number, name: string) {
        var select = this.controlElement(`[name="${name}"]`) as HTMLSelectElement;

        if (select.childElementCount != totalPages) {
            select.querySelectorAll('option').forEach(option => option.remove());
            for (var i = 1; i <= totalPages; i++) {
                var opt = document.createElement('option') as HTMLOptionElement;
                opt.value = i.toString();
                opt.text = i.toString();
                select.appendChild(opt);
            }
        }

        select.value = pageNumber.toString();
    }

    protected assignSearchDialog() {
        var searchDialog = this.controlElement(".search-dialog");
        if (searchDialog && this.getButton("search")) {
            this.searchDialog = new SearchDialog(searchDialog, this);
        }
    }

    protected validateSearchDialog(evt) {
        switch (this.triggerName(evt)) {
            case "searchdialog":
                if (this.form.checkValidity() == false) {
                    this.form.reportValidity()
                    evt.preventDefault();
                    return false;
                }
                break;
        }

        return true;
    }

    protected formMode() {
        return this.formBody.dataset.mode.toLowerCase();
    }

    public formModified() {
        if (this.formMode() == "empty") {
            return false;
        }
        let modified = [];
        this.controlElements(".fc-control").forEach((el) => {
            if (this.elementModified(el)) { modified.push(el) }
        });

        return modified.length > 0;
    }

    protected elementModified(el: HTMLFormElement, unappliedCheck: boolean = false) {
        if (el.dataset.dbdatatype == "XmlType") {
            return false;
        }
        if (el.tagName == 'INPUT' && el.type == 'checkbox') {
            return this.wasChecked(el.dataset.value) != el.checked;
        }
        if (el.type == 'select-multiple') {
            var selectedValues = Array.from(el.selectedOptions).map(({ value }) => value);

            if (el.dataset.dbdatatype = 'Array') {
                let dbValue = el.dataset.value.split(',').sort().join(',');
                return this.cleanString(dbValue) != this.cleanString(selectedValues.join(''));
            }
            else {
                return el.dataset.value != selectedValues.join(',');
            }
        }

        if (unappliedCheck) {
            return this.cleanString(el.dataset.value) != this.cleanString(el.value);;
        }

        if (el.tagName == 'TEXTAREA') {
            return this.cleanString(el.dataset.value) != this.cleanString(el.value);
        }
        else {
            return el.dataset.value != el.value;
        }
    }

    protected errorHighlighted(container: HTMLElement) {
        let controlsInError = 0;
        let selectors = [".fc-control"]

        if (this instanceof GridControl) {
            selectors.push("td")
        }

        selectors.forEach(s => { controlsInError += Array.from(container.querySelectorAll(s)).filter((e: HTMLElement) => { return (e.dataset.error == 'true'); }).length })
        return (controlsInError > 0);
    }

    protected getLinkedControlIds():string {
        if (this instanceof GridControl) {
            let table = this.controlElement("table") as HTMLElement;
            return table.dataset.linkedcontrolids;
        }
        if (this instanceof FormControl) {
            return (this as FormControl).formContainer.dataset.linkedcontrolids;
        }
        if (this instanceof SelectControl) {
            return (this as SelectControl).select.dataset.linkedcontrolids;
        }
        return "";
    }

    protected triggerCommit() {
        let applyBtn = this.getButton("apply");
        htmx.trigger(applyBtn, "click");
    }
  
    public formControlValue(columnName: string, row: HTMLTableRowElement = null) {
        return this.elementValue(columnName, false, row);
    }

    public formControlDbValue(columnName: string, row: HTMLTableRowElement = null) {
        return this.elementValue(columnName, true, row);
    }

    private elementValue(columnName: string, db: boolean, row: HTMLTableRowElement = null) {
        var el: HTMLFormElement = this.formControl(columnName, row);

        if (!el) {
            console.error(`Form control for column name ${columnName} not found`)
        }
        else {
            if (el.tagName == 'INPUT' && el.type == 'checkbox') {
                return db ? this.wasChecked(el.dataset.value) : el.checked
            }
            return db ? el.dataset.value : el.value;
        }
    }

    public formElementValue(columnName: string, row: HTMLTableRowElement = null) {
        return this.formControlValue(columnName, row);
    }

    public formElementDbValue(columnName: string, row: HTMLTableRowElement = null) {
        return this.formControlDbValue(columnName, row);
    }

    public formElement(columnName: string, row: HTMLTableRowElement = null) {
        return this.formControl(columnName, row);
    }

    public formControl(columnName: string, row: HTMLTableRowElement = null) {
        var element: HTMLFormElement = null;
        var container = row ? row : ((this instanceof FormControl) ? this.form : this.currentValidationRow);
        container.querySelectorAll(".fc-control").forEach((el: HTMLFormElement) => {
            let name = this.getElementName(el);
            if (name.toLowerCase() == `_${columnName.toLowerCase()}`) { element = el; }
        });

        if (!element) {
            console.error(`Form control => ${columnName} not found`);
        }
        return element;
    }

    protected getElementName(el: HTMLFormElement): string {
        let name = el.name;
        if (el.attributes["type"].value == "checkbox") {
            name = this.nextInputElement(el).name
        }

        return name;
    }

    protected cleanString(value) {
        return value.replace("&amp;#xA;", "").replace(/[^a-z0-9\.]+/gi, "").trim()
    }

    protected wasChecked(value: string) {
        return value == "1" || value.toLowerCase() == "true"
    }

    protected setMessage(message: string, type: string = 'success') {
        this.formMessage.innerHTML = message;
        this.formMessage.dataset.highlight = type.toLowerCase();
        window.setTimeout(() => { this.clearErrorMessage() }, 3000)
    }

    protected clearErrorMessage() {
        this.formMessage.innerHTML = "&nbsp";
        delete this.formMessage.dataset.highlight;
        this.controlElements(`.fc-control`).forEach((el) => { el.dataset.modified = false; el.dataset.error = false });

        if (this instanceof GridControl) {
            this.controlElements(`td`).forEach((el) => { el.dataset.error = false });
        }
    }

    protected reassignFormCheckboxValue() {
        this.controlElements('input[type="checkbox"].fc-control').forEach((cb: HTMLFormElement) => {
            this.nextInputElement(cb).value = cb.checked.toString();
            cb.addEventListener('change', (ev) => {
                let cb = ev.target as HTMLFormElement;
                this.nextInputElement(cb).value = cb.checked.toString();
            });
        });
    }
    private nextInputElement(cb: HTMLFormElement): HTMLInputElement {
        let element = cb.nextElementSibling;
        while (element) {
            if (element.nodeName === "INPUT") {
                return element as HTMLInputElement;
            }
            element = element.nextElementSibling;
        }
        return null;
    }

    protected getFormModification(container: HTMLElement) {
        let rowModification: RowModification = { modified: false, columns: [] };
        container.querySelectorAll(".fc-control").forEach((el: HTMLFormElement) => {
            if (this.elementModified(el)) {
                rowModification.columns.push(this.getElementName(el));
            }
        });
        rowModification.modified = rowModification.columns.length > 0;
        return rowModification;
    }

    protected warnIfFormModified(evt:Event = null): boolean {
        this.controlElements(".fc-control").forEach((el) => { el.dataset.modified = this.elementModified(el, true) });
        let modified = this.controlElements(".fc-control[data-modified='true']");

        if (modified.length) {
            if (evt) {
                evt.preventDefault();
            }
            this.setMessage(this.formBody.dataset.unappliedmessage, 'warning')
        }

        return modified.length > 0;
    }

    protected warnIfLinkedFormModified(evt: Event): boolean {
        let table = this.controlElement("table");
        let linkedFormModified = false;
        let linkedControlIds = this.getLinkedControlIds();
        if (linkedControlIds) {
            var linkedIdArray = linkedControlIds.split(",");
            linkedIdArray.forEach(linkedId => {
                if (document.querySelector(`#${linkedId}`)) {
                    var linkedControl = DbNetSuiteCore.controlArray[linkedId];
                    if (linkedControl instanceof FormControl) {
                        var formControl = linkedControl as FormControl;
                        if (formControl.formBody && formControl.checkIfFormModfied(evt)) {
                            linkedFormModified = true;
                        }
                    }
                }
            });
        }

        return linkedFormModified;
    }
}