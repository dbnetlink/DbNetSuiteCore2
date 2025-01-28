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
        let row = document.querySelector(this.rowSelector(rowIndex > -1 ? rowIndex : null));
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
        var args = {};
        if (this.gridControlElement("#jsonData")) {
            this.jsonData = JSON.parse(this.gridControlElement("#jsonData").value);
            args['json'] = this.jsonData;
        }
        this.invokeEventHandler('PageLoaded', args);
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
        this.assignSearchDialog();
        document.body.addEventListener('htmx:beforeRequest', (ev) => { this.beforeRequest(ev); });
        this.invokeEventHandler('Initialised');
    }
    beforeRequest(evt) {
        if (this.isControlEvent(evt) == false)
            return;
        if (this.validateSearchDialog(evt) == false) {
            return;
        }
    }
    columnSeriesData(columnName) {
        let series = [];
        if (this.jsonData.length) {
            let propName = this.getPropertyName(this.jsonData[0], columnName);
            if (propName) {
                for (var i = 0; i < this.jsonData.length; i++) {
                    series.push(this.jsonData[i][propName]);
                }
            }
        }
        return series;
    }
    getPropertyName(object, columnName) {
        let propName = null;
        for (var name in object) {
            if (name.toLowerCase() == columnName.toLowerCase()) {
                propName = name;
                break;
            }
        }
        return propName;
    }
    rowSeriesData(columnSeriesName, columnSeriesValue, columnNames) {
        let series = [];
        if (this.jsonData.length) {
            let columnSeriesValues = this.columnSeriesData(columnSeriesName);
            for (let r = 0; r < columnSeriesValues.length; r++) {
                if (columnSeriesValues[r] == columnSeriesValue) {
                    for (let c = 0; c < columnNames.length; c++) {
                        let propName = this.getPropertyName(this.jsonData[r], columnNames[c]);
                        if (propName) {
                            series.push(this.jsonData[r][propName]);
                        }
                    }
                }
            }
        }
        return series;
    }
    refreshPage() {
        let selector = `#${this.controlId} input[name="refresh"]`;
        let pk = htmx.find(selector);
        let rowIndex = this.selectedRow ? this.selectedRow.rowIndex : 1;
        this.form.setAttribute("hx-vals", JSON.stringify({ rowIndex: rowIndex }));
        pk.setAttribute("hx-vals", JSON.stringify({ rowIndex: rowIndex }));
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
        var tr = (rowIndex) ? `tr:nth-child(${rowIndex})` : 'tr.grid-row';
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
