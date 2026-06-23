class DbNetSuiteCore {
    public static controlArray: Dictionary<any> = {}
    public static namedControlArray: Dictionary<any> = {}

    public static createClientControl(controlId: string, clientId: string, clientEvents: object, deferredLoad: boolean = false) {
        document.addEventListener('htmx:afterRequest', function (evt) {
            DbNetSuiteCore.assignClientControl(controlId, clientId, clientEvents, deferredLoad);
            DbNetSuiteCore.controlArray[controlId].afterRequest(evt);
        });

        document.getElementById(controlId)!.addEventListener('htmx:responseError', function (evt: any) {
            evt.currentTarget.innerHTML = evt.detail.xhr.responseText;
        });

        htmx.on("htmx:responseError", function (evt: any) {
            const requestConfig = evt.detail.requestConfig;
            const xhr = evt.detail.xhr;
            alert(`<b>${requestConfig.verb} ${requestConfig.path}</b> returned <b>${xhr.status} ${xhr.statusText}</b><br>${xhr.responseText}`)
        })

        if (deferredLoad) {
            DbNetSuiteCore.assignClientControl(controlId, clientId, clientEvents, deferredLoad);
        }
    }

    public static assignClientControl(controlId: string, clientId: string, clientEvents: object, deferredLoad: boolean = false) {
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
            if (controlId.startsWith("Tree")) {
                clientControl = new TreeControl(controlId);
            }
            for (const [key, value] of Object.entries(clientEvents)) {
                const functionNameParts: Array<string> = value.toString().split('.') as Array<string>;
                try {
                    if (functionNameParts.length > 1) {
                        (clientControl as any).eventHandlers[key] = {
                            type: window[functionNameParts[0].toString() as keyof Window][functionNameParts[1].toString() as keyof Window],
                            name: value.toString()
                        }
                    }
                    else {
                        (clientControl as any).eventHandlers[key] = {
                            type: window[functionNameParts[0].toString() as keyof Window],
                            name: value.toString()
                        }
                    }
                }
                catch (ex) {
                    console.error(`Client-side event handler => ${value} not found`);
                }
            }
            DbNetSuiteCore.controlArray[controlId] = clientControl;
            DbNetSuiteCore.namedControlArray[clientId] = clientControl;
        }
    }

    public static waitFor(conditionFn: any, interval: number = 50) {
        return new Promise<void>(resolve => {
            const check = () => {
                if (conditionFn()) {
                    resolve();
                } else {
                    setTimeout(check, interval);
                }
            };
            check();
        });
    }
}
class ComponentControl {
    public controlId: string = "";
    public form: HTMLFormElement;
    parentControl: ComponentControl | null = null;
    childControls: Dictionary<ComponentControl> = {};
    controlContainer: HTMLElement;
    eventHandlers: Dictionary<any> = {};
    searchDialog: SearchDialog | null = null;
    public formBody: HTMLElement | null = null;
    formMessage: HTMLDivElement | null = null;
    currentValidationRow: HTMLTableRowElement | null = null;
    loaded: boolean = false;

    constructor(controlId: string) {
        this.controlId = controlId;
        this.form = document.querySelector(this.formSelector()) as HTMLFormElement;
        this.form.style.display = '';
        this.controlContainer = this.form.parentElement as HTMLElement;
    }

    public setCaption(text:string) {
        var caption = this.controlElement("div.caption");
        if (caption) {
            caption.innerText = text;
        }
    }

    protected isControlEvent(evt:any) {
        let formId = evt.target.closest("form").id;
        return formId.startsWith(this.controlId);
    }

    protected invokeEventHandler(eventName:string, args = {}) {
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

    protected eventHandlerAttached(eventName:string) {
        return (typeof this.eventHandlers[eventName] === 'function')
    }

    protected toast(text: string, style: string = 'info', delay: number = 1) {
        const toast = this.controlContainer.querySelector("#toastMessage") as HTMLElement;
        const toastParent = toast.parentElement;

        if (!toast || !toastParent) {
            return;
        }

        let span = toast.querySelector("span") as HTMLSpanElement;
        span.innerText = text;
        if (text == "") {
            toastParent.style.marginLeft = `-${toastParent.clientWidth / 2}px`;
            toastParent.style.marginTop = `-${toastParent.clientHeight / 2}px`;
            toastParent.style.display = 'none';
            return;
        }
        toastParent.style.display = 'block';
        let self = this;
        window.setTimeout(() => { self.toast(""); }, delay * 1000);
    }

    protected formSelector() {
        return `#${this.controlId}`;
    }

    protected controlElements(selector: string): NodeListOf<HTMLElement> {
        return this.form.querySelectorAll(selector) as NodeListOf<HTMLElement>;
    }

    public controlElement(selector: string): HTMLElement {
        return this.form.querySelector(selector) as HTMLElement;
    }

    protected triggerName(evt: any) {
        let headers = evt.detail.headers ? evt.detail.headers : evt.detail.requestConfig.headers;
        return headers["HX-Trigger-Name"] ? headers["HX-Trigger-Name"].toLowerCase() : "";
    }

    protected triggerElement(evt: any): HTMLElement {
        return evt.detail.requestConfig.elt;
    }

    protected updateLinkedControls(linkedIds: string, selectedIndex: string| null = null, url: string|null = null) {
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
                summaryModel = (this.controlElement("input[name='summarymodel']") as HTMLInputElement).value;
                if (this instanceof GridControl) {
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

    protected isElementLoaded = async (selector:string) => {
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

    public getButton(name: string): HTMLButtonElement {
        return this.controlElement(this.buttonSelector(name)) as HTMLButtonElement;
    }

    public buttonSelector(buttonType:string) {
        return `button[button-type="${buttonType}"]`;
    }

    protected setPageNumber(pageNumber: number, totalPages: number, name: string) {
        var select = this.controlElement(`[name="${name}"]`) as HTMLSelectElement;

        if (!select) {
            return;
        }

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
            this.searchDialog = new SearchDialog(searchDialog as HTMLDialogElement, this);
        }
    }

    protected validateSearchDialog(evt:any) {
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
        return this.formBody?.dataset.mode?.toLowerCase();
    }

    public formModified() {
        if (this.formMode() == "empty") {
            return false;
        }
        let modified = [];
        this.controlElements(".fc-control").forEach((el) => {
            if (this.elementModified(el as HTMLFormElement)) { modified.push(el) }
        });

        return modified.length > 0;
    }

    protected elementModified(el: HTMLFormElement, unappliedCheck: boolean = false) {
        if (el.dataset.dbdatatype == "XmlType") {
            return false;
        }
        const value = el.dataset.value as string;
        if (el.tagName == 'INPUT' && el.type == 'checkbox') {
            return this.wasChecked(value) != el.checked;
        }
        if (el.type == 'select-multiple') {
            var selectedValues = [...el.options].map(opt => opt.value);

            if (el.dataset.dbdatatype == 'Array') {
                let dbValue = value.split(',').sort().join(',');
                return this.cleanString(dbValue) != this.cleanString(selectedValues.join(''));
            }
            else {
                return value != selectedValues.join(',');
            }
        }

        if (unappliedCheck) {
            return this.cleanString(value) != this.cleanString(el.value);;
        }

        if (el.tagName == 'TEXTAREA') {
            return this.cleanString(value) != this.cleanString(el.value);
        }
        else {
            return value != el.value;
        }
    }

    protected errorHighlighted(container: HTMLElement) {
        let controlsInError = 0;
        let selectors = [".fc-control"]

        if (this instanceof GridControl) {
            selectors.push("td")
        }

        selectors.forEach(s => { controlsInError += Array.from(container.querySelectorAll(s)).filter((e: Element) => { return ((e as HTMLElement).dataset.error == 'true'); }).length })
        return (controlsInError > 0);
    }

    protected getLinkedControlIds(): string {
        if (this instanceof GridControl) {
            let table = this.controlElement("table") as HTMLElement;
            return table.dataset.linkedcontrolids as string;
        }
        if (this instanceof FormControl) {
            return (this as FormControl).formContainer?.dataset.linkedcontrolids as string;
        }
        if (this instanceof SelectControl) {
            return (this as SelectControl).select?.dataset.linkedcontrolids as string;
        }
        if (this instanceof TreeControl) {
            return (this as TreeControl).treeContainer?.dataset.linkedcontrolids as string;
        }
        return "";
    }

    protected triggerCommit() {
        let applyBtn = this.getButton("apply");
        htmx.trigger(applyBtn, "click");
    }

    public formControlValue(columnName: string, row: HTMLTableRowElement) {
        return this.elementValue(columnName, false, row);
    }

    public formControlDbValue(columnName: string, row: HTMLTableRowElement) {
        return this.elementValue(columnName, true, row);
    }

    private elementValue(columnName: string, db: boolean, row: HTMLTableRowElement) {
        var el: HTMLFormElement = this.formControl(columnName, row);

        if (!el) {
            console.error(`Form control for column name ${columnName} not found`)
        }
        else {
            if (el.tagName == 'INPUT' && el.type == 'checkbox') {
                return db ? this.wasChecked(el.dataset.value as string) : el.checked
            }
            return db ? el.dataset.value : el.value;
        }
    }

    public formElementValue(columnName: string, row: HTMLTableRowElement) {
        return this.formControlValue(columnName, row);
    }

    public formElementDbValue(columnName: string, row: HTMLTableRowElement) {
        return this.formControlDbValue(columnName, row);
    }

    public formElement(columnName: string, row: HTMLTableRowElement) {
        return this.formControl(columnName, row);
    }

    public formControl(columnName: string, row: HTMLTableRowElement): HTMLFormElement {
        var element: HTMLFormElement | null = null;
        var container = row ? row : this.controlId.startsWith("Form") ? this.form : this.currentValidationRow;
        container?.querySelectorAll(".fc-control").forEach((el: Element) => {
            let name = this.getElementName(el as HTMLFormElement);
            if (name.toLowerCase() == `_${columnName.toLowerCase()}`) { element = el as HTMLFormElement; }
        });

        if (!element) {
            console.error(`Form control => ${columnName} not found`);
        }
        return element!;
    }

    protected getElementName(el: HTMLFormElement): string {
        let name = el.name;
        if (el.type == "checkbox") {
            name = this.nextInputElement(el)?.name ?? ''
        }

        return name;
    }

    protected cleanString(value:string) {
        return value.replace("&amp;#xA;", "").replace(/[^a-z0-9\.]+/gi, "").trim()
    }

    protected wasChecked(value: string) {
        return value == "1" || value.toLowerCase() == "true"
    }

    protected setMessage(message: string, type: string = 'success') {
        this.formMessage!.innerHTML = message;
        this.formMessage!.dataset.highlight = type.toLowerCase();
        window.setTimeout(() => { this.clearErrorMessage() }, 3000)
    }

    protected clearErrorMessage() {
        this.formMessage!.innerHTML = "&nbsp";
        delete this.formMessage!.dataset.highlight;
        this.controlElements(`.fc-control`).forEach((el) => { el.dataset.modified = "false"; el.dataset.error = "false" });

        if (this.controlId.startsWith("Grid")) {
            this.controlElements(`td`).forEach((el) => { el.dataset.error = "false" });
        }
    }

    protected reassignFormCheckboxValue() {
        this.controlElements('input[type="checkbox"].fc-control').forEach((cb) => {
            const next = this.nextInputElement(cb as HTMLFormElement);
            if (next) {
                next.value = (cb as HTMLInputElement).checked.toString();
            }
            cb.addEventListener('change', (ev) => {
                let cb = ev.target as HTMLFormElement;
                const next = this.nextInputElement(cb);
                if (next) {
                    next.value = cb.checked.toString();
                }
            });
        });
    }
    private nextInputElement(cb: HTMLFormElement): HTMLInputElement | null{
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
        container.querySelectorAll(".fc-control").forEach((el) => {
            if (this.elementModified(el as HTMLFormElement)) {
                rowModification.columns.push(this.getElementName(el as HTMLFormElement));
            }
        });
        rowModification.modified = rowModification.columns.length > 0;
        return rowModification;
    }

    protected warnIfFormModified(evt: Event | null = null): boolean {
        this.controlElements(".fc-control").forEach((el) => { el.dataset.modified = this.elementModified(el as HTMLFormElement, true).toString() });
        let modified = this.controlElements(".fc-control[data-modified='true']");

        if (modified.length) {
            if (evt) {
                evt.preventDefault();
            }
            this.setMessage(this.formBody?.dataset.unappliedmessage as string, 'warning')
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

    protected debounce(fn: Function, delay = 2000) {
        let timerId: ReturnType<typeof setTimeout> | null = null;
        return function (this: unknown, ...args: any[]) {
            if (timerId) {
                clearTimeout(timerId);
            }
            timerId = setTimeout(() => fn.apply(this, args), delay);
        };
    }

    protected updateFixedFilterParams(params: any) {
        this.updateParamValues("fixedFilterParameters", params);    
    }

    protected updateApiRequestParams(params: any) {
        this.updateParamValues("apiRequestParameters", params);    
    }

    private updateParamValues(name: string, params: any) {
        let input = this.controlElement(`input[name="${name}"]`) as HTMLInputElement;
        if (input) {
            input.value = JSON.stringify(params);

            if (this.loaded) {
                htmx.trigger(input, "changed",);
            }
        }
    }
}