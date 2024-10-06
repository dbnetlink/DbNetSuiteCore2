var DbNetSuiteCore = {};
DbNetSuiteCore.gridControlArray = {};
DbNetSuiteCore.createGridControl = function (gridId, clientEvents) {
    document.addEventListener('htmx:afterRequest', function (evt) {
        if (!DbNetSuiteCore.gridControlArray[gridId]) {
            var gridControl = new GridControl(gridId);
            for (const [key, value] of Object.entries(clientEvents)) {
                gridControl.eventHandlers[key] = window[value.toString()];
            }
            DbNetSuiteCore.gridControlArray[gridId] = gridControl;
        }
        DbNetSuiteCore.gridControlArray[gridId].afterRequest(evt);
    });
};
class GridControl {
    constructor(gridId) {
        this.gridId = "";
        this.eventHandlers = {};
        this.bgColourClass = "bg-cyan-600";
        this.textColourClass = "text-zinc-100";
        this.isElementLoaded = async (selector) => {
            while (document.querySelector(selector) === null) {
                await new Promise(resolve => requestAnimationFrame(resolve));
            }
            return document.querySelector(selector);
        };
        this.gridId = gridId;
        this.gridControl = document.querySelector(this.gridSelector());
        this.gridControl.style.display = '';
        this.gridContainer = this.gridControl.parentElement;
    }
    afterRequest(evt) {
        let gridId = evt.target.closest("form").id;
        if (gridId.startsWith(this.gridId) == false || evt.detail.elt.name == "nestedGrid") {
            return;
        }
        if (this.triggerName(evt) == "viewdialogcontent") {
            this.viewDialog.show();
            this.invokeEventHandler('ViewDialogUpdated', { viewDialog: this.viewDialog });
            return;
        }
        if (!this.gridControlElement("tbody")) {
            return;
        }
        this.configureNavigation();
        this.configureSortIcon();
        if (this.triggerName(evt) == "initialload") {
            this.initialise();
        }
        this.gridControlElements(".nested-icons").forEach((div) => {
            let icons = div.querySelectorAll("span");
            icons[0].addEventListener("click", ev => this.showHideNestedGrid(ev, true));
            icons[1].addEventListener("click", ev => this.showHideNestedGrid(ev, false));
        });
        this.gridControlElements("tr.grid-row").forEach((row) => { this.invokeEventHandler('RowTransform', { row: row }); });
        this.gridControlElements("td[data-value]").forEach((cell) => { this.invokeCellTransform(cell); });
        this.gridControlElements("tbody a").forEach((e) => {
            e.classList.remove("selected");
            e.classList.add("underline");
        });
        if (this.rowSelection() != "none") {
            if (this.gridControlElement(this.multiRowSelectAllSelector())) {
                this.gridControlElement(this.multiRowSelectAllSelector()).addEventListener("change", (ev) => { this.updateMultiRowSelect(ev); });
                this.gridControlElements(this.multiRowSelectSelector()).forEach((e) => {
                    e.addEventListener("change", (ev) => {
                        this.selectRow(ev.target, true);
                    });
                });
            }
            else {
                htmx.findAll(this.rowSelector()).forEach((e) => { e.addEventListener("click", (ev) => this.selectRow(ev.target)); });
            }
        }
        let row = document.querySelector(this.rowSelector());
        if (row) {
            row.click();
        }
        this.gridControlElements("tr.column-filter-refresh select").forEach((select) => {
            let filter = this.gridControlElement(`thead select[data-key="${select.dataset.key}"]`);
            filter.innerHTML = select.innerHTML;
        });
        let thead = this.gridControlElement("thead");
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
        var viewDialog = this.gridControlElement(".view-dialog");
        if (viewDialog) {
            this.viewDialog = new ViewDialog(viewDialog, this);
        }
        this.invokeEventHandler('Initialised');
    }
    invokeCellTransform(cell) {
        var columnName = this.gridControlElement("thead").children[0].children[cell.cellIndex].dataset.columnname;
        var args = { cell: cell, columnName: columnName };
        this.invokeEventHandler('CellTransform', args);
    }
    invokeEventHandler(eventName, args = {}) {
        if (this.eventHandlers.hasOwnProperty(eventName) == false) {
            return;
        }
        if (typeof this.eventHandlers[eventName] === 'function') {
            this.eventHandlers[eventName](this, args);
        }
        else {
            this.message(`Javascript function for event type '${eventName}' is not defined`, 'error', 3);
        }
    }
    configureNavigation() {
        let tbody = this.gridControlElement("tbody");
        let currentPage = parseInt(tbody.dataset.currentpage);
        let totalPages = parseInt(tbody.dataset.totalpages);
        let rowCount = parseInt(tbody.dataset.rowcount);
        if (totalPages == 0) {
            this.updateLinkedGrids('');
        }
        if (this.viewDialog) {
            this.getButton("view").disabled = (rowCount == 0);
            if (rowCount == 0) {
                this.viewDialog.close();
            }
        }
        if (this.toolbarExists()) {
            if (totalPages == 0) {
                this.removeClass('#no-records', "hidden");
                this.addClass('#navigation', "hidden");
            }
            else {
                this.addClass('#no-records', "hidden");
                this.removeClass('#navigation', "hidden");
            }
            this.setPageNumber(currentPage, totalPages);
            this.gridControlElement('[data-type="total-pages"]').value = totalPages.toString();
            this.gridControlElement('[data-type="row-count"]').value = rowCount.toString();
            this.getButton("first").disabled = currentPage == 1;
            this.getButton("previous").disabled = currentPage == 1;
            this.getButton("next").disabled = currentPage == totalPages;
            this.getButton("last").disabled = currentPage == totalPages;
        }
    }
    rowSelection() {
        let thead = this.gridControlElement("thead");
        return thead.dataset.rowselection.toLowerCase();
    }
    removeClass(selector, className) {
        this.gridControlElement(selector).classList.remove(className);
    }
    addClass(selector, className) {
        this.gridControlElement(selector).classList.add(className);
    }
    setPageNumber(pageNumber, totalPages) {
        var select = this.gridControlElement('[name="page"]');
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
    toolbarExists() {
        return this.gridControlElement('#navigation');
    }
    configureSortIcon() {
        if (this.gridControlElements(`th[data-key]`).length == 0) {
            return;
        }
        let tbody = this.gridControlElement("tbody");
        let sortKey = tbody.dataset.sortkey;
        let sortIcon = tbody.querySelector("span#sortIcon").innerHTML;
        this.gridControlElements(`th[data-key] span`).forEach(e => e.innerHTML = '');
        let span = this.gridControlElement(`th[data-key="${sortKey}"] span`);
        if (!span) {
            span = this.gridControlElements(`th[data-key] span`)[0];
        }
        span.innerHTML = sortIcon;
    }
    gridControlElements(selector) {
        return this.gridControl.querySelectorAll(selector);
    }
    gridControlElement(selector) {
        return this.gridControl.querySelector(selector);
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
    loadFromParent(primaryKey) {
        let selector = `#${this.gridId} input[name="primaryKey"]`;
        let pk = htmx.find(selector);
        this.gridControl.setAttribute("hx-vals", JSON.stringify({ primaryKey: primaryKey }));
        if (pk) {
            htmx.trigger(selector, "changed");
        }
        else {
            htmx.trigger(`#${this.gridId}`, "submit");
        }
    }
    refresh() {
        let pageSelect = this.gridControlElement('[name="page"]');
        pageSelect.value = "1";
        htmx.trigger(pageSelect, "changed");
    }
    updateMultiRowSelect(ev) {
        let checked = ev.target.checked;
        this.gridControlElements(this.multiRowSelectSelector()).forEach((e) => {
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
        let table = this.gridControlElement("table");
        if (table.dataset.linkedgridid) {
            this.isElementLoaded(`#${table.dataset.linkedgridid}`).then((selector) => {
                DbNetSuiteCore.gridControlArray[table.dataset.linkedgridid].loadFromParent(primaryKey);
            });
        }
    }
    clearHighlighting(row) {
        if (row) {
            this.clearRowHighlight(row);
        }
        else {
            this.gridControlElements(this.rowSelector()).forEach(e => {
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
        var table = this.gridControlElement("table");
        try {
            this.copyElementToClipboard(table);
            this.message("Page copied to clipboard");
        }
        catch (e) {
            try {
                const content = table.innerHTML;
                const blobInput = new Blob([content], { type: 'text/html' });
                const clipboardItemInput = new ClipboardItem({ 'text/html': blobInput });
                navigator.clipboard.write([clipboardItemInput]);
                this.message("Page copied to clipboard");
            }
            catch (e) {
                this.message("Copy failed", "error", 5);
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
        for (let [key, val] of new FormData(this.gridControl)) {
            data.append(key, val);
        }
        var exportOption = this.gridControlElement('[name="exportformat"]').value;
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
        link.click();
    }
    message(text, style = 'info', delay = 1) {
        var toast = this.gridContainer.querySelector("#toastMessage");
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
        window.setTimeout(() => { self.message(""); }, delay * 1000);
    }
    showIndicator() {
        this.indicator().classList.add("htmx-request");
    }
    hideIndicator() {
        this.indicator().classList.remove("htmx-request");
    }
    indicator() {
        return this.gridContainer.children[1];
    }
    rowSelector() {
        return `#tbody${this.gridId} > tr.grid-row`;
    }
    multiRowSelectAllSelector() {
        return `th > input.multi-select`;
    }
    multiRowSelectSelector() {
        return `td > input.multi-select`;
    }
    gridSelector() {
        return `#${this.gridId}`;
    }
    buttonSelector(buttonType) {
        return `button[button-type="${buttonType}"]`;
    }
    columnCells(columnName) {
        let th = this.heading(columnName);
        return this.gridControlElements(`td:nth-child(${(th.cellIndex + 1)})`);
    }
    heading(columnName) {
        return this.gridControlElement(`th[data-columnname='${columnName.toLowerCase()}']`);
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
    getButton(name) {
        return this.gridControlElement(this.buttonSelector(name));
    }
    triggerName(evt) {
        var _a;
        return ((_a = evt.detail.requestConfig.headers['HX-Trigger-Name']) !== null && _a !== void 0 ? _a : '').toLowerCase();
    }
    selectedValues() {
        let selectedValues = [];
        this.gridControlElements(this.multiRowSelectSelector()).forEach((checkbox) => {
            if (checkbox.checked) {
                let tr = checkbox.closest("tr");
                if (tr.dataset.id) {
                    selectedValues.push(tr.dataset.id);
                }
            }
        });
        return selectedValues;
    }
}
