var DbNetSuiteCore = {};
var controlArray = {};
DbNetSuiteCore.controlArray = controlArray;
DbNetSuiteCore.createClientControl = function (controlId, clientEvents) {
    document.addEventListener('htmx:afterRequest', function (evt) {
        if (!DbNetSuiteCore.controlArray[controlId]) {
            var clientControl = {};
            if (controlId.startsWith("Grid")) {
                clientControl = new GridControl(controlId);
            }
            if (controlId.startsWith("Select")) {
                clientControl = new SelectControl(controlId);
            }
            if (controlId.startsWith("Form")) {
                clientControl = new FormControl(controlId);
            }
            for (const [key, value] of Object.entries(clientEvents)) {
                clientControl.eventHandlers[key] = window[value.toString()];
            }
            DbNetSuiteCore.controlArray[controlId] = clientControl;
        }
        DbNetSuiteCore.controlArray[controlId].afterRequest(evt);
    });
};
class ComponentControl {
    constructor(controlId) {
        this.controlId = "";
        this.childControls = {};
        this.eventHandlers = {};
        this.isElementLoaded = async (selector) => {
            while (document.querySelector(selector) === null) {
                await new Promise(resolve => requestAnimationFrame(resolve));
            }
            return document.querySelector(selector);
        };
        this.controlId = controlId;
        this.form = document.querySelector(this.formSelector());
        this.form.style.display = '';
        this.controlContainer = this.form.parentElement;
    }
    setCaption(text) {
        var caption = this.controlElement("div.caption");
        if (caption) {
            caption.innerText = text;
        }
    }
    invokeEventHandler(eventName, args = {}) {
        //  window.dispatchEvent(new CustomEvent(`Grid${eventName}`, { detail: this.controlId }));
        if (this.eventHandlers.hasOwnProperty(eventName) == false) {
            return;
        }
        if (typeof this.eventHandlers[eventName] === 'function') {
            this.eventHandlers[eventName](this, args);
        }
        else {
            this.toast(`Javascript function for event type '${eventName}' is not defined`, 'error', 3);
        }
    }
    eventHandlerAttached(eventName, args = {}) {
        return (typeof this.eventHandlers[eventName] === 'function');
    }
    toast(text, style = 'info', delay = 1) {
        var toast = this.controlContainer.querySelector("#toastMessage");
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
    formSelector() {
        return `#${this.controlId}`;
    }
    controlElements(selector) {
        return this.form.querySelectorAll(selector);
    }
    controlElement(selector) {
        return this.form.querySelector(selector);
    }
    triggerName(evt) {
        let headers = evt.detail.headers ? evt.detail.headers : evt.detail.requestConfig.headers;
        return headers["HX-Trigger-Name"] ? headers["HX-Trigger-Name"].toLowerCase() : "";
    }
    triggerElement(evt) {
        return evt.detail.requestConfig.elt;
    }
    updateLinkedControls(linkedIds, primaryKey, url = null) {
        var linkedIdArray = linkedIds.split(",");
        linkedIdArray.forEach(linkedId => {
            this.isElementLoaded(`#${linkedId}`).then((selector) => {
                var linkedControl = DbNetSuiteCore.controlArray[linkedId];
                linkedControl.parentControl = this;
                this.childControls[linkedId] = linkedControl;
                if (url != null && linkedControl.dataSourceIsFileSystem()) {
                    primaryKey = url;
                }
                linkedControl.loadFromParent(primaryKey);
            });
        });
    }
    notifyParent(records) {
        if (this.parentControl) {
            this.parentControl.childLoaded(records);
        }
    }
    childLoaded(records) {
        if (this instanceof FormControl) {
            let deleteButton = this.getButton("delete");
            if (deleteButton) {
                deleteButton.disabled = records;
            }
        }
    }
    dataSourceIsFileSystem() {
        return this.form.dataset.datasourcetype == "FileSystem";
    }
    loadFromParent(primaryKey) {
        let selector = `#${this.controlId} input[name="primaryKey"]`;
        let pk = htmx.find(selector);
        this.form.setAttribute("hx-vals", JSON.stringify({ primaryKey: primaryKey }));
        if (pk) {
            htmx.trigger(selector, "changed");
        }
        else {
            htmx.trigger(`#${this.controlId}`, "submit");
        }
    }
    toolbarExists() {
        return this.controlElement('#navigation');
    }
    removeClass(selector, className) {
        let e = this.controlElement(selector);
        if (e) {
            e.classList.remove(className);
        }
    }
    addClass(selector, className) {
        let e = this.controlElement(selector);
        if (e) {
            e.classList.add(className);
        }
    }
    getButton(name) {
        return this.controlElement(this.buttonSelector(name));
    }
    buttonSelector(buttonType) {
        return `button[button-type="${buttonType}"]`;
    }
    setPageNumber(pageNumber, totalPages, name) {
        var select = this.controlElement(`[name="${name}"]`);
        if (select.childElementCount != totalPages) {
            select.querySelectorAll('option').forEach(option => option.remove());
            for (var i = 1; i <= totalPages; i++) {
                var opt = document.createElement('option');
                opt.value = i.toString();
                opt.text = i.toString();
                select.appendChild(opt);
            }
        }
        select.value = pageNumber.toString();
    }
    isControlEvent(evt) {
        let formId = evt.target.closest("form").id;
        return formId.startsWith(this.controlId);
    }
}

class GridControl extends ComponentControl {
    constructor(gridId) {
        super(gridId);
        this.bgColourClass = "bg-cyan-600";
        this.textColourClass = "text-zinc-100";
    }
    afterRequest(evt) {
        let gridId = evt.target.closest("form").id;
        if (gridId.startsWith(this.controlId) == false || evt.detail.elt.name == "nestedGrid") {
            return;
        }
        let rowIndex = null;
        switch (this.triggerName(evt)) {
            case "viewdialogcontent":
                this.viewDialog.show();
                this.invokeEventHandler('ViewDialogUpdated', { viewDialog: this.viewDialog });
                return;
            case "refresh":
                let hxVals = JSON.parse(this.triggerElement(evt).getAttribute("hx-vals"));
                rowIndex = hxVals.rowIndex;
        }
        if (!this.controlElement("tbody")) {
            return;
        }
        this.configureNavigation();
        this.configureSortIcon();
        if (this.triggerName(evt) == "initialload") {
            this.initialise();
        }
        this.controlElements(".nested-icons").forEach((div) => {
            let icons = div.querySelectorAll("span");
            icons[0].addEventListener("click", ev => this.showHideNestedGrid(ev, true));
            icons[1].addEventListener("click", ev => this.showHideNestedGrid(ev, false));
        });
        this.controlElements("tr.grid-row").forEach((row) => { this.invokeEventHandler('RowTransform', { row: row }); });
        this.controlElements("td[data-value]").forEach((cell) => { this.invokeCellTransform(cell); });
        this.controlElements("tbody a").forEach((e) => {
            e.classList.remove("selected");
            e.classList.add("underline");
        });
        if (this.rowSelection() != "none") {
            if (this.controlElement(this.multiRowSelectAllSelector())) {
                this.controlElement(this.multiRowSelectAllSelector()).addEventListener("change", (ev) => { this.updateMultiRowSelect(ev); });
                this.controlElements(this.multiRowSelectSelector()).forEach((e) => {
                    e.addEventListener("change", (ev) => {
                        this.selectRow(ev.target, true);
                    });
                });
            }
            else {
                htmx.findAll(this.rowSelector()).forEach((e) => { e.addEventListener("click", (ev) => this.selectRow(ev.target)); });
            }
        }
        let row = document.querySelector(this.rowSelector(rowIndex));
        if (row) {
            row.click();
        }
        this.controlElements("tr.column-filter-refresh select").forEach((select) => {
            let filter = this.controlElement(`thead select[data-key="${select.dataset.key}"]`);
            if (filter) {
                filter.innerHTML = select.innerHTML;
            }
        });
        this.controlElements("thead input[data-key]").forEach((input) => {
            input.title = "";
            input.style.backgroundColor = "";
        });
        this.controlElements("tr.column-filter-error span").forEach((span) => {
            let input = this.controlElement(`thead input[data-key="${span.dataset.key}"]`);
            input.title = span.innerText;
            input.style.backgroundColor = "rgb(252 165 165)";
        });
        let thead = this.controlElement("thead");
        if (thead.dataset.frozen.toLowerCase() == "true") {
            thead.style.top = `0px`;
            thead.style.position = 'sticky';
            thead.style.zIndex = '1';
        }
        this.invokeEventHandler('PageLoaded');
    }
    initialise() {
        if (this.toolbarExists()) {
            this.getButton("copy").addEventListener("click", ev => this.copyTableToClipboard());
            this.getButton("export").addEventListener("click", ev => this.download());
        }
        var viewDialog = this.controlElement(".view-dialog");
        if (viewDialog) {
            this.viewDialog = new ViewDialog(viewDialog, this);
        }
        this.invokeEventHandler('Initialised');
    }
    refreshPage() {
        let selector = `#${this.controlId} input[name="refresh"]`;
        let pk = htmx.find(selector);
        this.form.setAttribute("hx-vals", JSON.stringify({ rowIndex: this.selectedRow.rowIndex }));
        pk.setAttribute("hx-vals", JSON.stringify({ rowIndex: this.selectedRow.rowIndex }));
        htmx.trigger(selector, "changed");
    }
    invokeCellTransform(cell) {
        var columnName = this.controlElement("thead").children[0].children[cell.cellIndex].dataset.columnname;
        var args = { cell: cell, columnName: columnName };
        this.invokeEventHandler('CellTransform', args);
    }
    configureNavigation() {
        let tbody = this.controlElement("tbody");
        let currentPage = parseInt(tbody.dataset.currentpage);
        let totalPages = parseInt(tbody.dataset.totalpages);
        let rowCount = parseInt(tbody.dataset.rowcount);
        if (totalPages == 0) {
            this.updateLinkedGrids('');
        }
        this.notifyParent(rowCount > 0);
        if (this.viewDialog) {
            this.getButton("view").disabled = (rowCount == 0);
            if (rowCount == 0) {
                this.viewDialog.close();
            }
        }
        if (this.toolbarExists()) {
            let queryLimit = parseInt(this.controlElement("#query-limited").dataset.querylimit);
            if (totalPages == 0) {
                this.removeClass('#no-records', "hidden");
                this.addClass('#navigation', "hidden");
            }
            else {
                this.addClass('#no-records', "hidden");
                this.removeClass('#navigation', "hidden");
            }
            if (queryLimit > 0 && queryLimit == rowCount) {
                this.removeClass('#query-limited', "hidden");
            }
            else {
                this.addClass('#query-limited', "hidden");
            }
            this.setPageNumber(currentPage, totalPages, "page");
            this.controlElement('[data-type="total-pages"]').value = totalPages.toString();
            this.controlElement('[data-type="row-count"]').value = rowCount.toString();
            this.getButton("first").disabled = currentPage == 1;
            this.getButton("previous").disabled = currentPage == 1;
            this.getButton("next").disabled = currentPage == totalPages;
            this.getButton("last").disabled = currentPage == totalPages;
        }
    }
    rowSelection() {
        let thead = this.controlElement("thead");
        return thead.dataset.rowselection.toLowerCase();
    }
    configureSortIcon() {
        if (this.controlElements(`th[data-key]`).length == 0) {
            return;
        }
        let tbody = this.controlElement("tbody");
        let sortKey = tbody.dataset.sortkey;
        let sortIcon = tbody.querySelector("span#sortIcon").innerHTML;
        this.controlElements(`th[data-key] span`).forEach(e => e.innerHTML = '');
        let span = this.controlElement(`th[data-key="${sortKey}"] span`);
        if (!span) {
            span = this.controlElements(`th[data-key] span`)[0];
        }
        span.innerHTML = sortIcon;
    }
    showHideNestedGrid(ev, show) {
        ev.stopPropagation();
        let tr = ev.target.closest("tr");
        let icons = tr.firstElementChild.querySelectorAll("span");
        let siblingRow = tr.nextElementSibling;
        if (siblingRow && siblingRow.classList.contains("nested-grid-row")) {
            siblingRow.style.display = show ? null : "none";
        }
        else if (show) {
            htmx.trigger(icons[2], "click");
        }
        icons[0].style.display = show ? "none" : "block";
        icons[1].style.display = show ? "block" : "none";
    }
    refresh() {
        let pageSelect = this.controlElement('[name="page"]');
        pageSelect.value = "1";
        htmx.trigger(pageSelect, "changed");
    }
    updateMultiRowSelect(ev) {
        let checked = ev.target.checked;
        this.controlElements(this.multiRowSelectSelector()).forEach((e) => {
            e.checked = checked;
            this.selectRow(e);
        });
        this.selectedValuesChanged();
        ;
    }
    selectRow(target, multiSelect = false) {
        let tr = target.closest('tr');
        if (target.classList.contains("multi-select") == false) {
            if (tr.classList.contains(this.bgColourClass)) {
                return;
            }
            this.clearHighlighting(null);
        }
        else if (target.checked == false) {
            this.clearHighlighting(tr);
            if (multiSelect) {
                this.selectedValuesChanged();
            }
            return;
        }
        tr.classList.add(this.bgColourClass, this.textColourClass);
        tr.querySelectorAll("a").forEach(e => e.classList.add("selected"));
        tr.querySelectorAll("td[data-value] > div > svg,td[data-isfolder] svg,td > div.nested-icons svg").forEach(e => e.setAttribute("fill", "#ffffff"));
        this.updateLinkedGrids(tr.dataset.id);
        this.selectedRow = tr;
        if (this.viewDialog) {
            this.viewDialog.configureNavigation(tr);
            this.viewDialog.update();
        }
        this.invokeEventHandler('RowSelected', { selectedRow: tr });
        if (multiSelect) {
            this.selectedValuesChanged();
        }
    }
    selectedValuesChanged() {
        this.invokeEventHandler('SelectedRowsUpdated', { selectedValues: this.selectedValues() });
    }
    updateLinkedGrids(primaryKey) {
        let table = this.controlElement("table");
        if (table.dataset.linkedcontrolids) {
            this.updateLinkedControls(table.dataset.linkedcontrolids, primaryKey);
        }
    }
    clearHighlighting(row) {
        if (row) {
            this.clearRowHighlight(row);
        }
        else {
            this.controlElements(this.rowSelector()).forEach(e => {
                this.clearRowHighlight(e.closest("tr"));
            });
        }
    }
    clearRowHighlight(tr) {
        tr.classList.remove(this.bgColourClass, this.textColourClass);
        tr.querySelectorAll("a").forEach(e => e.classList.remove("selected"));
        tr.querySelectorAll("td[data-value] > div > svg,td[data-isfolder] svg,td > div.nested-icons svg").forEach(e => e.setAttribute("fill", "#666666"));
    }
    copyTableToClipboard() {
        var table = this.controlElement("table");
        try {
            this.copyElementToClipboard(table);
            this.toast("Page copied to clipboard");
        }
        catch (e) {
            try {
                const content = table.innerHTML;
                const blobInput = new Blob([content], { type: 'text/html' });
                const clipboardItemInput = new ClipboardItem({ 'text/html': blobInput });
                navigator.clipboard.write([clipboardItemInput]);
                this.toast("Page copied to clipboard");
            }
            catch (e) {
                this.toast("Copy failed", "error", 5);
                return;
            }
        }
    }
    copyElementToClipboard(element) {
        window.getSelection().removeAllRanges();
        let range = document.createRange();
        range.selectNode(typeof element === 'string' ? document.getElementById(element) : element);
        window.getSelection().addRange(range);
        document.execCommand('copy');
        window.getSelection().removeAllRanges();
    }
    previousRow() {
        this.selectedRow.previousElementSibling.click();
    }
    nextRow() {
        this.selectedRow.nextElementSibling.click();
    }
    download() {
        this.showIndicator();
        const data = new URLSearchParams();
        for (let [key, val] of new FormData(this.form)) {
            data.append(key, val);
        }
        var exportOption = this.controlElement('[name="exportformat"]').value;
        fetch("gridcontrol.htmx", {
            method: 'post',
            body: data,
            headers: {
                'hx-trigger-name': 'download'
            },
        })
            .then((response) => {
            this.hideIndicator();
            if (!response.headers.has("error")) {
                return response.blob();
            }
            else {
                throw new Error(response.headers.get("error"));
            }
        })
            .then((blob) => {
            if (blob) {
                if (exportOption == "html") {
                    this.openWindow(blob);
                }
                else {
                    this.downloadFile(blob, exportOption);
                }
            }
        }).catch((e) => console.log(`Critical failure: ${e.message}`));
    }
    openWindow(response) {
        const url = window.URL.createObjectURL(response);
        const tab = window.open();
        tab.location.href = url;
    }
    downloadFile(response, extension) {
        const link = document.createElement("a");
        link.href = window.URL.createObjectURL(response);
        extension = (extension == "excel") ? "xlsx" : extension;
        link.download = `report_${new Date().getTime()}.${extension}`;
        this.invokeEventHandler('FileDownload', { link: link, extension: extension });
        link.click();
    }
    showIndicator() {
        this.indicator().classList.add("htmx-request");
    }
    hideIndicator() {
        this.indicator().classList.remove("htmx-request");
    }
    indicator() {
        return this.controlContainer.children[1];
    }
    rowSelector(rowIndex = null) {
        var tr = (rowIndex) ? `tr:nth-child(${rowIndex - 1})` : 'tr.grid-row';
        return `#tbody${this.controlId} > ${tr}`;
    }
    multiRowSelectAllSelector() {
        return `th > input.multi-select`;
    }
    multiRowSelectSelector() {
        return `td > input.multi-select`;
    }
    gridControlElement(selector) {
        return this.controlElement(selector);
    }
    selectedValues() {
        let selectedValues = [];
        this.controlElements(this.multiRowSelectSelector()).forEach((checkbox) => {
            if (checkbox.checked) {
                let tr = checkbox.closest("tr");
                if (tr.dataset.id) {
                    selectedValues.push(tr.dataset.id);
                }
            }
        });
        return selectedValues;
    }
    columnCells(columnName) {
        let th = this.heading(columnName);
        return this.controlElements(`td:nth-child(${(th.cellIndex + 1)})`);
    }
    heading(columnName) {
        return this.controlElement(`th[data-columnname='${columnName.toLowerCase()}']`);
    }
    columnCell(columnName, row) {
        let th = this.heading(columnName);
        return th ? row.querySelector(`td:nth-child(${(th.cellIndex + 1)})`) : null;
    }
    columnValue(columnName, row) {
        if (!row) {
            row = this.selectedRow;
        }
        let datasetValue = row.dataset[columnName.toLowerCase()];
        if (datasetValue) {
            return datasetValue;
        }
        let cell = this.columnCell(columnName, row);
        return cell ? cell.dataset.value : null;
    }
}

class SelectControl extends ComponentControl {
    constructor(selectId) {
        super(selectId);
    }
    afterRequest(evt) {
        let selectId = evt.target.closest("form").id;
        if (selectId.startsWith(this.controlId) == false) {
            return;
        }
        this.select = this.controlElement("select");
        let selectElements = this.controlElements("select");
        this.select.innerHTML = selectElements[1].innerHTML;
        selectElements[1].remove();
        if (this.triggerName(evt) == "initialload") {
            this.initialise();
        }
        this.invokeEventHandler('OptionsLoaded');
        this.selectChanged(this.select);
        this.checkForError();
    }
    initialise() {
        this.controlElement("select").addEventListener("change", (ev) => {
            this.selectChanged(ev.target);
        });
        this.invokeEventHandler('Initialised');
    }
    selectChanged(target) {
        let url = '';
        if (target.selectedOptions.length) {
            var dataset = target.selectedOptions[0].dataset;
            url = this.dataSourceIsFileSystem() && dataset.isdirectory && dataset.isdirectory.toLowerCase() == "true" ? dataset.path : '';
        }
        this.updateLinkedChildControls(target.value, url);
        this.invokeEventHandler('OptionSelected', { selectedOptions: target.selectedOptions });
    }
    updateLinkedChildControls(primaryKey, url) {
        if (this.select.dataset.linkedcontrolids) {
            this.updateLinkedControls(this.select.dataset.linkedcontrolids, primaryKey, url);
        }
    }
    checkForError() {
        var select = this.controlElement("select");
        const error = select.querySelector("div");
        if (error) {
            select.parentElement.nextElementSibling.after(error);
        }
    }
}

class FormControl extends ComponentControl {
    constructor(formId) {
        super(formId);
    }
    afterRequest(evt) {
        if (this.isControlEvent(evt) == false) {
            return false;
        }
        if (this.triggerName(evt) == "toolbar") {
            return;
        }
        this.formContainer = this.controlElement("div.form-container");
        this.formBody = this.controlElement("div.form-body");
        this.formMessage = this.controlElement("#form-message");
        if (!this.formBody) {
            return;
        }
        this.notifyParent(this.formBody.dataset.mode.toLowerCase() == "update");
        switch (this.triggerName(evt)) {
            case "initialload":
                this.initialise();
                break;
        }
        if (this.cachedMessage) {
            this.setMessage(this.cachedMessage);
        }
        this.updateLinkedChildControls(this.formBody.dataset.id);
        window.setTimeout(() => { this.clearErrorMessage(); }, 3000);
        this.controlElements("select.fc-control.readonly").forEach((el) => { this.makeSelectReadonly(el); });
        this.controlElements("input.fc-control.readonly").forEach((el) => { this.makeCheckboxReadonly(el); });
        this.controlElements("input[data-texttransform]").forEach((el) => { this.transformText(el); });
        this.setFocus();
        this.invokeEventHandler('RecordLoaded');
    }
    afterSettle(evt) {
        if (this.isControlEvent(evt) == false) {
            return false;
        }
        if (!this.formBody) {
            return;
        }
        this.cachedMessage = null;
        switch (this.triggerName(evt)) {
            case "apply":
                if (this.formBody.dataset.validationpassed == "True") {
                    this.clientSideValidation();
                }
                if (this.formBody.dataset.committype) {
                    if (this.parentControl) {
                        if (this.parentControl instanceof GridControl) {
                            this.cachedMessage = this.formMessage.innerText;
                            this.parentControl.refreshPage();
                        }
                    }
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
        this.invokeEventHandler('Initialised');
    }
    transformText(input) {
        input.addEventListener("input", (e) => {
            e.preventDefault();
            let el = e.target;
            el.value = el.dataset.texttransform == "Uppercase" ? el.value.toUpperCase() : el.value.toLowerCase();
        });
    }
    updateLinkedChildControls(primaryKey) {
        if (this.formContainer.dataset.linkedcontrolids) {
            this.updateLinkedControls(this.formContainer.dataset.linkedcontrolids, primaryKey);
        }
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
        if (!this.confirmDialog) {
            this.confirmDialog = new ConfirmDialog(this);
        }
        this.confirmDialog.show(evt, this.formBody);
    }
    configRequest(evt) {
        if (this.isControlEvent(evt) == false) {
            return;
        }
        this.controlElements(".fc-control").forEach((el) => {
            if (this.elementModified(el) == false) {
                delete evt.detail.parameters[el.name];
            }
            else if (evt.detail.parameters[el.name] == undefined) {
                evt.detail.parameters[el.name] = '';
            }
        });
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
                if (this.form.checkValidity() == false) {
                    this.form.reportValidity();
                    evt.preventDefault();
                    return;
                }
            case "cancel":
            case "primarykey":
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
    setFocus() {
        var selector = this.errorHighlighted() ? ".fc-control[data-error='true']" : ".fc-control";
        for (const el of this.controlElements(selector)) {
            if (el.readOnly == false && el.disabled == false) {
                el.focus();
                break;
            }
        }
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
            return this.isBoolean(el.dataset.value) != el.checked;
        }
        else if (el.type == 'select-multiple') {
            var selectedValues = Array.from(el.selectedOptions).map(({ value }) => value);
            if (el.dataset.dbdatatype = 'Array') {
                console.log(this.cleanString(el.dataset.value));
                console.log(this.cleanString(selectedValues.join('')));
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
    cleanString(value) {
        return value.replace("&amp;#xA;", "").replace(/[^a-z0-9\.]+/gi, "").trim();
    }
    isBoolean(value) {
        return value == "1" || value.toLowerCase() == "true";
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

class DraggableDialog {
    constructor(dialogId, dragHandleClass = 'dialog-header', container) {
        this.isDragging = false;
        this.initialX = 0;
        this.initialY = 0;
        this.xOffset = 0;
        this.yOffset = 0;
        this.dialog = document.getElementById(dialogId);
        this.container = container;
        if (!this.dialog) {
            throw new Error(`Dialog with id "${dialogId}" not found`);
        }
        this.dragHandle = this.dialog.querySelector(`.${dragHandleClass}`);
        if (!this.dragHandle) {
            throw new Error(`Drag handle with class "${dragHandleClass}" not found in the dialog`);
        }
        this.initDragEvents();
    }
    initDragEvents() {
        this.dragHandle.addEventListener('mousedown', this.startDragging.bind(this));
        document.addEventListener('mousemove', this.drag.bind(this));
        document.addEventListener('mouseup', this.stopDragging.bind(this));
        this.xOffset = (0 - (this.container.clientWidth / 2)) + this.container.offsetLeft;
        this.yOffset = (0 - (this.container.clientHeight / 2)) + this.container.offsetTop;
        this.setTranslate(this.xOffset, this.yOffset);
    }
    startDragging(e) {
        if (e.target.closest(`.${this.dragHandle.className}`) === this.dragHandle) {
            this.isDragging = true;
            this.initialX = e.clientX - (this.xOffset ? this.xOffset : (this.dialog.clientWidth / 2) * -1);
            this.initialY = e.clientY - (this.yOffset ? this.yOffset : (this.dialog.clientHeight / 2) * -1);
            this.dragHandle.style.cursor = 'move';
            document.body.style.userSelect = 'none'; // Prevent text selection during drag
        }
    }
    drag(e) {
        if (this.isDragging) {
            e.preventDefault();
            this.xOffset = e.clientX - this.initialX;
            this.yOffset = e.clientY - this.initialY;
            this.setTranslate(this.xOffset, this.yOffset);
        }
    }
    stopDragging() {
        this.isDragging = false;
        this.dragHandle.style.cursor = '';
        document.body.style.userSelect = ''; // Re-enable text selection
        document.removeEventListener('mousemove', this.drag);
        document.removeEventListener('mouseup', this.stopDragging);
    }
    setTranslate(xPos, yPos) {
        requestAnimationFrame(() => {
            this.dialog.style.transform = `translate3d(${xPos}px, ${yPos}px, 0)`;
        });
    }
}

class ViewDialog {
    constructor(dialog, gridControl) {
        this.draggableDialog = null;
        this.dialog = dialog;
        this.gridControl = gridControl;
        let closeButtons = this.dialog.querySelectorAll(this.gridControl.buttonSelector("close"));
        closeButtons.forEach((e) => {
            e.addEventListener("click", () => this.dialog.close());
        });
        this.dialog.querySelector(this.gridControl.buttonSelector("previous")).addEventListener("click", () => this.gridControl.previousRow());
        this.dialog.querySelector(this.gridControl.buttonSelector("next")).addEventListener("click", () => this.gridControl.nextRow());
        this.gridControl.getButton("view").addEventListener("click", this.open.bind(this));
    }
    open() {
        this.getRecord();
    }
    close() {
        if (this.dialog && this.dialog.open) {
            this.close();
        }
    }
    show() {
        this.dialog.show();
        if (!this.draggableDialog) {
            this.draggableDialog = new DraggableDialog(this.dialog.id, "dialog-nav", this.gridControl.gridControlElement("tbody"));
        }
    }
    update() {
        if (this.dialog && this.dialog.open) {
            this.getRecord();
        }
    }
    getRecord() {
        let input = this.dialog.querySelector("input[hx-post]");
        input.value = this.gridControl.selectedRow.dataset.id;
        htmx.trigger(input, "changed");
    }
    configureNavigation(tr) {
        this.configureButton(tr.previousElementSibling, "previous");
        this.configureButton(tr.nextElementSibling, "next");
    }
    configureButton(sibling, buttonType) {
        this.dialog.querySelector(this.gridControl.buttonSelector(buttonType)).disabled = (!sibling || sibling.classList.contains("grid-row") == false);
    }
    columnCell(columnName) {
        return this.dialog.querySelector(`div[data-columnname='${columnName.toLowerCase()}']`);
    }
    columnValue(columnName) {
        let div = this.columnCell(columnName);
        return div ? div.dataset.value : null;
    }
}

class ConfirmDialog {
    constructor(control) {
        this.control = control;
        this.dialog = this.control.controlElement(".confirm-dialog");
        let closeButtons = this.dialog.querySelectorAll(this.control.buttonSelector("close"));
        closeButtons.forEach((e) => {
            e.addEventListener("click", () => this.dialog.close());
        });
        this.dialog.querySelector(this.control.buttonSelector("confirm")).addEventListener("click", () => this.confirm());
        this.dialog.querySelector(this.control.buttonSelector("cancel")).addEventListener("click", () => this.cancel());
    }
    show(event, container) {
        this.event = event;
        this.dialog.show();
        this.dialog.style.left = this.coordinate(container.offsetLeft, container.clientWidth, this.dialog.clientWidth);
        this.dialog.style.top = this.coordinate(container.offsetTop, container.clientHeight, this.dialog.clientHeight);
    }
    coordinate(offset, container, dialog) {
        let adj = container > dialog ? ((container - dialog) * 0.5) : 0;
        return `${offset + (dialog * 0.5) + adj}px`;
    }
    confirm() {
        this.event.detail.issueRequest(true);
        this.dialog.close();
    }
    cancel() {
        this.dialog.close();
    }
}
