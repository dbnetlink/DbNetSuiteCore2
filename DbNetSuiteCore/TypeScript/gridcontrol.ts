var DbNetSuiteCore: any = {};
DbNetSuiteCore.gridControlArray = {}

DbNetSuiteCore.createGridControl = function (gridId, clientEvents) {
    document.addEventListener('htmx:afterRequest', function (evt) {
        if (!DbNetSuiteCore.gridControlArray[gridId]) {
            var gridControl = new GridControl(gridId);

            for (const [key, value] of Object.entries(clientEvents)) {
                gridControl.eventHandlers[key] = window[value.toString()]
            }
            DbNetSuiteCore.gridControlArray[gridId] = gridControl;
        }
        DbNetSuiteCore.gridControlArray[gridId].afterRequest(evt);
    });
}
class GridControl {
    gridId: string = "";
    gridControl: HTMLFormElement;
    gridContainer: HTMLElement;
    eventHandlers = {};
    private bgColourClass = "bg-cyan-600";
    private textColourClass = "text-zinc-100";
    viewDialog: ViewDialog;
    selectedRow: HTMLTableRowElement;

    constructor(gridId) {
        this.gridId = gridId;
        this.gridControl = document.querySelector(this.gridSelector())
        this.gridControl.style.display = '';
        this.gridContainer = this.gridControl.parentElement
    }

    afterRequest(evt) {
        let gridId = evt.target.closest("form").id;
        if (gridId.startsWith(this.gridId) == false || evt.detail.elt.name == "nestedGrid") {
            return
        }

        if (this.triggerName(evt) == "viewdialogcontent") {
            this.viewDialog.show();
            this.invokeEventHandler('ViewDialogUpdated', {viewDialog:this.viewDialog});
            return
        }

        if (!this.gridControlElement("tbody")) {
            return
        }

        this.configureNavigation()
        this.configureSortIcon()

        if (this.triggerName(evt) == "initialload") {
            this.initialise()
        }

        this.gridControlElements(".nested-icons").forEach((div) => {
            let icons = div.querySelectorAll("span")

            icons[0].addEventListener("click", ev => this.showHideNestedGrid(ev, true));
            icons[1].addEventListener("click", ev => this.showHideNestedGrid(ev, false));
        });

        this.gridControlElements("tr.grid-row").forEach((row: HTMLTableRowElement) => { this.invokeEventHandler('RowTransform', { row: row }) });
        this.gridControlElements("td[data-value]").forEach((cell: HTMLTableCellElement) => { this.invokeCellTransform(cell) });

        this.gridControlElements("tbody a").forEach((e) => {
            e.classList.remove("selected");
            e.classList.add("underline")
        });

        if (this.rowSelection() != "none")
        {
            if (this.gridControlElement(this.multiRowSelectAllSelector())) {
                this.gridControlElement(this.multiRowSelectAllSelector()).addEventListener("change", (ev) => { this.updateMultiRowSelect(ev) });
                this.gridControlElements(this.multiRowSelectSelector()).forEach((e) => {
                    e.addEventListener("change", (ev) => {
                        this.selectRow(ev.target, true);
                    })
                });
            }
            else {
                htmx.findAll(this.rowSelector()).forEach((e) => { e.addEventListener("click", (ev) => this.selectRow(ev.target as HTMLElement)) });
            }
        }

        let row: HTMLElement = document.querySelector(this.rowSelector());
        if (row) {
            row.click();
        }

        this.gridControlElements("tr.column-filter-refresh select").forEach((select: HTMLSelectElement) => {
            let filter: HTMLSelectElement = this.gridControlElement(`thead select[data-key="${select.dataset.key}"]`)
            filter.innerHTML = select.innerHTML;
        });

        this.gridControlElements("thead input[data-key]").forEach((input: HTMLInputElement) => {
            input.title = "";
            input.style.backgroundColor = "";
        });

        this.gridControlElements("tr.column-filter-error span").forEach((span: HTMLElement) => {
            let input: HTMLInputElement = this.gridControlElement(`thead input[data-key="${span.dataset.key}"]`)
            input.title = span.innerText;
            input.style.backgroundColor = "rgb(252 165 165)";
        });

        let thead = this.gridControlElement("thead") as HTMLElement;
        if (thead.dataset.frozen.toLowerCase() == "true") {
            thead.style.top = `0px`;
            thead.style.position = 'sticky';
            thead.style.zIndex = '1';
        }

        this.invokeEventHandler('PageLoaded');
    }

    initialise() {
        if (this.toolbarExists()) {
            this.getButton("copy").addEventListener("click", ev => this.copyTableToClipboard())
            this.getButton("export").addEventListener("click", ev => this.download())
        }

        var viewDialog = this.gridControlElement(".view-dialog");
        if (viewDialog) {
            this.viewDialog = new ViewDialog(viewDialog, this);
        }

        this.invokeEventHandler('Initialised');
    }

    invokeCellTransform(cell: HTMLTableCellElement) {
        var columnName = (this.gridControlElement("thead").children[0].children[cell.cellIndex] as HTMLTableCellElement).dataset.columnname
        var args = { cell: cell, columnName: columnName }
        this.invokeEventHandler('CellTransform', args)
    }

    invokeEventHandler(eventName, args = {}) {
        window.dispatchEvent(new CustomEvent(`Grid${eventName}`, { detail: this.gridId }));
        if (this.eventHandlers.hasOwnProperty(eventName) == false) {
            return;
        }
        if (typeof this.eventHandlers[eventName] === 'function') {
            this.eventHandlers[eventName](this, args)
        }
        else {
            this.message(`Javascript function for event type '${eventName}' is not defined`, 'error', 3)
        }
    }

    configureNavigation() {
        let tbody = this.gridControlElement("tbody") as HTMLElement;

        let currentPage = parseInt(tbody.dataset.currentpage);
        let totalPages = parseInt(tbody.dataset.totalpages);
        let rowCount = parseInt(tbody.dataset.rowcount);
        let queryLimit = parseInt(this.gridControlElement("#query-limited").dataset.querylimit);

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

            if (queryLimit > 0 && queryLimit == rowCount) {
                this.removeClass('#query-limited', "hidden");
            }
            else {
                this.addClass('#query-limited', "hidden");
            }

            this.setPageNumber(currentPage, totalPages);
            (this.gridControlElement('[data-type="total-pages"]') as HTMLInputElement).value = totalPages.toString();
            (this.gridControlElement('[data-type="row-count"]') as HTMLInputElement).value = rowCount.toString();

            this.getButton("first").disabled = currentPage == 1;
            this.getButton("previous").disabled = currentPage == 1;
            this.getButton("next").disabled = currentPage == totalPages;
            this.getButton("last").disabled = currentPage == totalPages;
        }
    }

    rowSelection() {
        let thead = this.gridControlElement("thead") as HTMLElement;
        return thead.dataset.rowselection.toLowerCase();
    }

    removeClass(selector: string, className: string) {
        this.gridControlElement(selector).classList.remove(className);
    }

    addClass(selector: string, className: string) {
        this.gridControlElement(selector).classList.add(className);
    }

    setPageNumber(pageNumber: number, totalPages: number) {
        var select = this.gridControlElement('[name="page"]') as HTMLSelectElement;

        if (select.childElementCount != totalPages) {
            select.querySelectorAll('option').forEach(option => option.remove())
            for (var i = 1; i <= totalPages; i++) {
                var opt = document.createElement('option') as HTMLOptionElement;
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
            return
        }
        let tbody = this.gridControlElement("tbody") as HTMLElement;
        let sortKey = tbody.dataset.sortkey;
        let sortIcon = tbody.querySelector("span#sortIcon").innerHTML;

        this.gridControlElements(`th[data-key] span`).forEach(e => e.innerHTML = '')

        let span = this.gridControlElement(`th[data-key="${sortKey}"] span`)

        if (!span) {
            span = this.gridControlElements(`th[data-key] span`)[0]
        }

        span.innerHTML = sortIcon
    }

    gridControlElements(selector) {
        return this.gridControl.querySelectorAll(selector);
    }

    gridControlElement(selector) {
        return this.gridControl.querySelector(selector);
    }

    showHideNestedGrid(ev: Event, show) {
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

    loadFromParent(primaryKey: string) {
        let selector = `#${this.gridId} input[name="primaryKey"]`
        let pk = htmx.find(selector) as HTMLInputElement

        this.gridControl.setAttribute("hx-vals", JSON.stringify({ primaryKey: primaryKey }))

        if (pk) {
            htmx.trigger(selector, "changed");
        }
        else {
            htmx.trigger(`#${this.gridId}`, "submit");
        }
    }

    refresh() {
        let pageSelect = this.gridControlElement('[name="page"]')
        pageSelect.value = "1";
        htmx.trigger(pageSelect, "changed");
    }

    isElementLoaded = async selector => {
        while (document.querySelector(selector) === null) {
            await new Promise(resolve => requestAnimationFrame(resolve))
        }
        return document.querySelector(selector);
    };

    updateMultiRowSelect(ev: Event) {
        let checked = (ev.target as HTMLInputElement).checked
        this.gridControlElements(this.multiRowSelectSelector()).forEach((e: HTMLInputElement) => {
            e.checked = checked;
            this.selectRow(e);
        });

        this.selectedValuesChanged();;
    }

    selectRow(target: HTMLElement, multiSelect:boolean = false) {
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

    selectedValuesChanged() {
        this.invokeEventHandler('SelectedRowsUpdated', { selectedValues: this.selectedValues() });
    }

    updateLinkedGrids(primaryKey: string) {
        let table = this.gridControlElement("table") as HTMLElement;

        if (table.dataset.linkedgridid) {
            this.isElementLoaded(`#${table.dataset.linkedgridid}`).then((selector) => {
                DbNetSuiteCore.gridControlArray[table.dataset.linkedgridid].loadFromParent(primaryKey);
            })
        }
    }

    clearHighlighting(row: HTMLTableRowElement) {
        if (row) {
            this.clearRowHighlight(row);
        }
        else {
            this.gridControlElements(this.rowSelector()).forEach(e => {
                this.clearRowHighlight(e.closest("tr"));
            });
        }
    }

    clearRowHighlight(tr: HTMLTableRowElement) {
        tr.classList.remove(this.bgColourClass, this.textColourClass);
        tr.querySelectorAll("a").forEach(e => e.classList.remove("selected"));
        tr.querySelectorAll("td[data-value] > div > svg,td[data-isfolder] svg,td > div.nested-icons svg").forEach(e => e.setAttribute("fill", "#666666"));
    }

    copyTableToClipboard() {
        var table = this.gridControlElement("table");
        try {
            this.copyElementToClipboard(table);
            this.message("Page copied to clipboard")
        } catch (e) {
            try {
                const content = table.innerHTML;
                const blobInput = new Blob([content], { type: 'text/html' });
                const clipboardItemInput = new ClipboardItem({ 'text/html': blobInput });
                navigator.clipboard.write([clipboardItemInput]);
                this.message("Page copied to clipboard")
            }
            catch (e) {
                this.message("Copy failed", "error", 5)
                return
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
        (this.selectedRow.previousElementSibling as HTMLTableRowElement).click()
    }

    nextRow() {
        (this.selectedRow.nextElementSibling as HTMLTableRowElement).click()
    }

    download() {
        this.showIndicator()
        const data = new URLSearchParams();
        for (let [key, val] of new FormData(this.gridControl)) {
            data.append(key, val as any);
        }

        var exportOption = (this.gridControlElement('[name="exportformat"]') as HTMLSelectElement).value

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
        this.invokeEventHandler('FileDownload', { link: link, extension : extension });
        link.click();
    }

    message(text, style = 'info', delay = 1) {
        var toast = this.gridContainer.querySelector("#toastMessage") as HTMLElement
        //toast.classList.add(`alert-${style}`)
        toast.querySelector("span").innerText = text;
        if (text == "") {
            toast.parentElement.style.marginLeft = `-${toast.parentElement.clientWidth / 2}px`
            toast.parentElement.style.marginTop = `-${toast.parentElement.clientHeight / 2}px`
            toast.parentElement.style.display = 'none'
            return
        }
        toast.parentElement.style.display = 'block'
        let self = this
        window.setTimeout(() => { self.message("") }, delay * 1000)
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
        return `#tbody${this.gridId} > tr.grid-row`
    }

    multiRowSelectAllSelector() {
        return `th > input.multi-select`
    }

    multiRowSelectSelector() {
        return `td > input.multi-select`
    }

    gridSelector() {
        return `#${this.gridId}`
    }

    buttonSelector(buttonType) {
        return `button[button-type="${buttonType}"]`
    }

    columnCells(columnName) {
        let th = this.heading(columnName);
        return this.gridControlElements(`td:nth-child(${(th.cellIndex + 1)})`)
    }

    heading(columnName): HTMLTableCellElement {
        return this.gridControlElement(`th[data-columnname='${columnName.toLowerCase()}']`)
    }

    columnCell(columnName: string, row: HTMLTableRowElement):HTMLTableCellElement {
        let th = this.heading(columnName);
        return th ? row.querySelector(`td:nth-child(${(th.cellIndex + 1)})`) : null;
    }

    columnValue(columnName: string, row: HTMLTableRowElement) {
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

    getButton(name): HTMLButtonElement {
        return this.gridControlElement(this.buttonSelector(name))
    }

    triggerName(evt: any) {
        return (evt.detail.requestConfig.headers['HX-Trigger-Name'] ?? '').toLowerCase() 
    }

    selectedValues() {
        let selectedValues = [];
        this.gridControlElements(this.multiRowSelectSelector()).forEach((checkbox: HTMLInputElement) => {
            if (checkbox.checked) {
                let tr = checkbox.closest("tr") as HTMLTableRowElement;

                if (tr.dataset.id) {
                    selectedValues.push(tr.dataset.id)
                }
            }
        })

        return selectedValues;
    }
}
