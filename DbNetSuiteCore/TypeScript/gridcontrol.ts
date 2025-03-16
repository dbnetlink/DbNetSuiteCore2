class GridControl extends ComponentControl {
    private bgColourClass = "bg-cyan-600";
    private textColourClass = "text-zinc-100";
    viewDialog: ViewDialog;
    selectedRow: HTMLTableRowElement;
    jsonData: [any];
    deferredLoad: boolean = false;
    constructor(gridId: string, deferredLoad:boolean) {
        super(gridId)
        this.deferredLoad = deferredLoad;
        if (this.deferredLoad) {
            this.checkForVisibility();
        }
    }

    afterRequest(evt) {
        let gridId = evt.target.closest("form").id;
        if (gridId.startsWith(this.controlId) == false || evt.detail.elt.name == "nestedGrid") {
            return
        }

        let rowIndex = null;
        switch (this.triggerName(evt)) {
            case "viewdialogcontent":
                this.viewDialog.show();
                this.invokeEventHandler('ViewDialogUpdated', { viewDialog: this.viewDialog });
                return;
            case "refresh":
                let hxVals = JSON.parse(this.triggerElement(evt).getAttribute("hx-vals"));
                rowIndex = hxVals.rowIndex
        }

        if (!this.controlElement("tbody")) {
            return
        }

        this.configureNavigation()
        this.configureSortIcon()

        if (this.triggerName(evt) == "initialload") {
            this.initialise()
        }

        this.controlElements(".nested-icons").forEach((div) => {
            let icons = div.querySelectorAll("span")

            icons[0].addEventListener("click", ev => this.showHideNestedGrid(ev, true));
            icons[1].addEventListener("click", ev => this.showHideNestedGrid(ev, false));
        });

        this.controlElements("tr.grid-row").forEach((row: HTMLTableRowElement) => { this.invokeEventHandler('RowTransform', { row: row }) });
        this.controlElements("td[data-value]").forEach((cell: HTMLTableCellElement) => { this.invokeCellTransform(cell) });

        this.controlElements("tbody a").forEach((e) => {
            e.classList.remove("selected");
            e.classList.add("underline")
        });

        if (this.rowSelection() != "none") {
            if (this.controlElement(this.multiRowSelectAllSelector())) {
                this.controlElement(this.multiRowSelectAllSelector()).addEventListener("change", (ev) => { this.updateMultiRowSelect(ev) });
                this.controlElements(this.multiRowSelectSelector()).forEach((e) => {
                    e.addEventListener("change", (ev) => {
                        this.selectRow(ev.target, true);
                    })
                });
            }
            else {
                htmx.findAll(this.rowSelector()).forEach((e) => { e.addEventListener("click", (ev) => this.selectRow(ev.target as HTMLElement)) });
            }
        }

        let row: HTMLElement = document.querySelector(this.rowSelector(rowIndex > -1 ? rowIndex : null));
        if (row) {
            row.click();
        }

        this.controlElements("tr.lookup-refresh select").forEach((select: HTMLSelectElement) => {
            let filter: HTMLSelectElement = this.controlElement(`thead select[data-key="${select.dataset.key}"]`)
            if (filter) {
                filter.innerHTML = select.innerHTML;
            }
        });

        this.controlElements("thead input[data-key]").forEach((input: HTMLInputElement) => {
            input.title = "";
            input.style.backgroundColor = "";
        });

        this.controlElements("tr.column-filter-error span").forEach((span: HTMLElement) => {
            let input: HTMLInputElement = this.controlElement(`thead input[data-key="${span.dataset.key}"]`)
            input.title = span.innerText;
            input.style.backgroundColor = "rgb(252 165 165)";
        });

        let thead = this.controlElement("thead") as HTMLElement;
        if (thead.dataset.frozen.toLowerCase() == "true") {
            thead.style.top = `0px`;
            thead.style.position = 'sticky';
            thead.style.zIndex = '1';
        }

        var args = {}
        if (this.gridControlElement("#jsonData")) {
            this.jsonData = JSON.parse((this.gridControlElement("#jsonData") as HTMLTextAreaElement).value);
            args['json'] = this.jsonData;
        }
        this.invokeEventHandler('PageLoaded', args);
    }

    private initialise() {
        if (this.toolbarExists()) {
            this.getButton("copy").addEventListener("click", ev => this.copyTableToClipboard())
            this.getButton("export").addEventListener("click", ev => this.download())
        }

        var viewDialog = this.controlElement(".view-dialog");
        if (viewDialog) {
            this.viewDialog = new ViewDialog(viewDialog, this);
        }

        this.assignSearchDialog();

        document.body.addEventListener('htmx:beforeRequest', (ev) => { this.beforeRequest(ev) });

        this.invokeEventHandler('Initialised');
    }

    checkForVisibility() {
        let handleIntersection = function (entries) {
            for (let entry of entries) {
                if (entry.isIntersecting) {
                    let form = (entry.target as HTMLFormElement);
                    if (form.querySelectorAll("table").length == 0) {
                        htmx.trigger(form, "submit");
                    }
                }
            }
        }

        const observer = new IntersectionObserver(handleIntersection);
        observer.observe(this.form);
    }

    public beforeRequest(evt) {
        if (this.isControlEvent(evt) == false)
            return;

        if (this.validateSearchDialog(evt) == false) {
            return;
        }
    }

    public columnSeriesData(columnName: string) {
        let series = [];
        if (this.jsonData.length) {
            let propName = this.getPropertyName(this.jsonData[0], columnName);

            if (propName) {
                for (var i = 0; i < this.jsonData.length; i++) {
                    series.push(this.jsonData[i][propName])
                }
            }
        }
        return series;
    }

    private getPropertyName(object: any, columnName: string) {
        let propName = null;
        for (var name in object) {
            if (name.toLowerCase() == columnName.toLowerCase()) {
                propName = name;
                break;
            }
        }

        return propName;
    }

    public rowSeriesData(columnSeriesName: string, columnSeriesValue: string, columnNames:string[]) {
        let series = [];
        if (this.jsonData.length) {
            let columnSeriesValues = this.columnSeriesData(columnSeriesName);
            for (let r = 0; r < columnSeriesValues.length; r++) {
                if (columnSeriesValues[r] == columnSeriesValue) {
                    for (let c = 0; c < columnNames.length; c++) {
                        let propName = this.getPropertyName(this.jsonData[r], columnNames[c]);
                        if (propName) {
                            series.push(this.jsonData[r][propName])
                        }
                    }
                }
            }
        }
        return series;
    }

    public refreshPage() {
        let selector = `#${this.controlId} input[name="refresh"]`;
        let pk = htmx.find(selector) as HTMLInputElement;
        let rowIndex = this.selectedRow ? this.selectedRow.rowIndex : 1;
        this.form.setAttribute("hx-vals", JSON.stringify({ rowIndex: rowIndex  }));
        pk.setAttribute("hx-vals", JSON.stringify({ rowIndex: rowIndex }));
        htmx.trigger(selector, "changed",);
    }

    private invokeCellTransform(cell: HTMLTableCellElement) {
        var columnName = (this.controlElement("thead").children[0].children[cell.cellIndex] as HTMLTableCellElement).dataset.columnname
        var args = { cell: cell, columnName: columnName }
        this.invokeEventHandler('CellTransform', args)
    }

    private configureNavigation() {
        let tbody = this.controlElement("tbody") as HTMLElement;

        let currentPage = parseInt(tbody.dataset.currentpage);
        let totalPages = parseInt(tbody.dataset.totalpages);
        let rowCount = parseInt(tbody.dataset.rowcount);

        if (totalPages == 0) {
            this.updateLinkedGrids('');
        }

        this.notifyParent(rowCount > 0)

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
            (this.controlElement('[data-type="total-pages"]') as HTMLInputElement).value = totalPages.toString();
            (this.controlElement('[data-type="row-count"]') as HTMLInputElement).value = rowCount.toString();

            this.getButton("first").disabled = currentPage == 1;
            this.getButton("previous").disabled = currentPage == 1;
            this.getButton("next").disabled = currentPage == totalPages;
            this.getButton("last").disabled = currentPage == totalPages;
        }
    }

    private rowSelection() {
        let thead = this.controlElement("thead") as HTMLElement;
        return thead.dataset.rowselection.toLowerCase();
    }


    private configureSortIcon() {
        if (this.controlElements(`th[data-key]`).length == 0) {
            return
        }
        let tbody = this.controlElement("tbody") as HTMLElement;
        let sortKey = tbody.dataset.sortkey;
        let sortIcon = tbody.querySelector("span#sortIcon").innerHTML;

        this.controlElements(`th[data-key] span`).forEach(e => e.innerHTML = '')

        let span = this.controlElement(`th[data-key="${sortKey}"] span`)

        if (!span) {
            span = this.controlElements(`th[data-key] span`)[0]
        }

        span.innerHTML = sortIcon
    }

    private showHideNestedGrid(ev: Event, show) {
        ev.stopPropagation();
        let tr = (ev.target as HTMLElement).closest("tr") as HTMLTableRowElement

        let icons = tr.firstElementChild.querySelectorAll("span")

        let siblingRow = tr.nextElementSibling as HTMLElement;

        if (siblingRow && siblingRow.classList.contains("nested-grid-row")) {
            siblingRow.style.display = show ? null : "none"
        }
        else if (show) {
            htmx.trigger(icons[2], "click")
        }

        icons[0].style.display = show ? "none" : "block"
        icons[1].style.display = show ? "block" : "none"
    }

    private refresh() {
        let pageSelect = this.controlElement('[name="page"]')
        pageSelect.value = "1";
        htmx.trigger(pageSelect, "changed");
    }

    private updateMultiRowSelect(ev: Event) {
        let checked = (ev.target as HTMLInputElement).checked
        this.controlElements(this.multiRowSelectSelector()).forEach((e: HTMLInputElement) => {
            e.checked = checked;
            this.selectRow(e);
        });

        this.selectedValuesChanged();;
    }

    private selectRow(target: HTMLElement, multiSelect: boolean = false) {
        let tr = target.closest('tr')

        if (target.classList.contains("multi-select") == false) {
            if (tr.classList.contains(this.bgColourClass)) {
                return;
            }
            this.clearHighlighting(null);
        }
        else if ((target as HTMLInputElement).checked == false) {
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
            this.viewDialog.update()
        }

        this.invokeEventHandler('RowSelected', { selectedRow: tr });
        if (multiSelect) {
            this.selectedValuesChanged();
        }
    }

    private selectedValuesChanged() {
        this.invokeEventHandler('SelectedRowsUpdated', { selectedValues: this.selectedValues() });
    }

    private updateLinkedGrids(primaryKey: string) {
        let table = this.controlElement("table") as HTMLElement;

        if (table.dataset.linkedcontrolids) {
            this.updateLinkedControls(table.dataset.linkedcontrolids, primaryKey)
        }
    }

    private clearHighlighting(row: HTMLTableRowElement) {
        if (row) {
            this.clearRowHighlight(row);
        }
        else {
            this.controlElements(this.rowSelector()).forEach(e => {
                this.clearRowHighlight(e.closest("tr"));
            });
        }
    }

    private clearRowHighlight(tr: HTMLTableRowElement) {
        tr.classList.remove(this.bgColourClass, this.textColourClass);
        tr.querySelectorAll("a").forEach(e => e.classList.remove("selected"));
        tr.querySelectorAll("td[data-value] > div > svg,td[data-isfolder] svg,td > div.nested-icons svg").forEach(e => e.setAttribute("fill", "#666666"));
    }

    private copyTableToClipboard() {
        var table = this.controlElement("table");
        try {
            this.copyElementToClipboard(table);
            this.toast("Page copied to clipboard")
        } catch (e) {
            try {
                const content = table.innerHTML;
                const blobInput = new Blob([content], { type: 'text/html' });
                const clipboardItemInput = new ClipboardItem({ 'text/html': blobInput });
                navigator.clipboard.write([clipboardItemInput]);
                this.toast("Page copied to clipboard")
            }
            catch (e) {
                this.toast("Copy failed", "error", 5)
                return
            }
        }
    }

    private copyElementToClipboard(element) {
        window.getSelection().removeAllRanges();
        let range = document.createRange();
        range.selectNode(typeof element === 'string' ? document.getElementById(element) : element);
        window.getSelection().addRange(range);
        document.execCommand('copy');
        window.getSelection().removeAllRanges();
    }

    public previousRow() {
        (this.selectedRow.previousElementSibling as HTMLTableRowElement).click()
    }

    public nextRow() {
        (this.selectedRow.nextElementSibling as HTMLTableRowElement).click()
    }

    private download() {
        this.showIndicator()
        const data = new URLSearchParams();
        for (let [key, val] of new FormData(this.form)) {
            data.append(key, val as any);
        }

        var exportOption = (this.controlElement('[name="exportformat"]') as HTMLSelectElement).value

        fetch("gridcontrol.htmx", {
            method: 'post',
            body: data,
            headers: {
                'hx-trigger-name': 'download'
            },
        })
            .then((response) => {
                this.hideIndicator()
                if (!response.headers.has("error")) {
                    return response.blob()
                }
                else {
                    throw new Error(response.headers.get("error"))
                }
            })
            .then((blob) => {
                if (blob) {
                    if (exportOption == "html") {
                        this.openWindow(blob)
                    }
                    else {
                        this.downloadFile(blob, exportOption)
                    }
                }
            }).catch((e) => console.log(`Critical failure: ${e.message}`));
    }

    private openWindow(response) {
        const url = window.URL.createObjectURL(response);
        const tab = window.open();
        tab.location.href = url;
    }

    private downloadFile(response, extension) {
        const link = document.createElement("a");
        link.href = window.URL.createObjectURL(response);
        extension = (extension == "excel") ? "xlsx" : extension;
        link.download = `report_${new Date().getTime()}.${extension}`;
        this.invokeEventHandler('FileDownload', { link: link, extension: extension });
        link.click();
    }

    private showIndicator() {
        this.indicator().classList.add("htmx-request");
    }

    private hideIndicator() {
        this.indicator().classList.remove("htmx-request");
    }

    private indicator() {
        return this.controlContainer.children[1];
    }

    private rowSelector(rowIndex: number|null = null) {
        var tr = (rowIndex) ? `tr:nth-child(${rowIndex})` : 'tr.grid-row';
        return `#tbody${this.controlId} > ${tr}`
    }

    private multiRowSelectAllSelector() {
        return `th > input.multi-select`
    }

    private multiRowSelectSelector() {
        return `td > input.multi-select`
    }

    public gridControlElement(selector): HTMLElement {
        return this.controlElement(selector)
    }

    private selectedValues() {
        let selectedValues = [];
        this.controlElements(this.multiRowSelectSelector()).forEach((checkbox: HTMLInputElement) => {
            if (checkbox.checked) {
                let tr = checkbox.closest("tr") as HTMLTableRowElement;

                if (tr.dataset.id) {
                    selectedValues.push(tr.dataset.id)
                }
            }
        })

        return selectedValues;
    }

    public columnCells(columnName): NodeListOf<HTMLTableCellElement> {
        let th = this.heading(columnName);
        return this.controlElements(`td:nth-child(${(th.cellIndex + 1)})`)
    }

    public heading(columnName): HTMLTableCellElement {
        return this.controlElement(`th[data-columnname='${columnName.toLowerCase()}']`)
    }

    public columnCell(columnName: string, row: HTMLTableRowElement): HTMLTableCellElement {
        let th = this.heading(columnName);
        return th ? row.querySelector(`td:nth-child(${(th.cellIndex + 1)})`) : null;
    }

    public columnValue(columnName: string, row: HTMLTableRowElement) {
        if (!row) {
            row = this.selectedRow;
        }

        let datasetValue = row.dataset[columnName.toLowerCase()];
        if (datasetValue) {
            return datasetValue;
        }
        let cell = this.columnCell(columnName, row)
        return cell ? cell.dataset.value : null;
    }
}
