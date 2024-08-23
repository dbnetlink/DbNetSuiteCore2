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
        DbNetSuiteCore.gridControlArray[gridId].init(evt);
    });
}
class GridControl {
    gridId: string = "";
    gridControl: HTMLFormElement;
    gridContainer: HTMLElement;
    eventHandlers = {};
    private bgColourClass = "bg-cyan-600";
    private textColourClass = "text-zinc-100";
    private linkColourClass = "text-blue-500";

    constructor(gridId) {
        this.gridId = gridId;
        this.gridControl = document.querySelector(this.gridSelector())
        this.gridContainer = this.gridControl.parentElement
    }

    init(evt) {
        let gridId = evt.target.closest("form").id;
        if (gridId.startsWith(this.gridId) == false) {
            return
        }

        if (htmx.find(this.errorSelector())) {
            return
        }

        this.configureNavigation()
        this.configureSortIcon()

        if (gridId == this.gridId) {
            if (this.toolbarExists()) {
                this.getButton("copy").addEventListener("click", ev => this.copyTableToClipboard())
                this.getButton("export").addEventListener("click", ev => this.download())
            }

            this.configureNestedGrid(evt.target);
            this.invokeEventHandler('Initialised');
        }

        htmx.findAll(this.linkSelector()).forEach((e) => {
            e.classList.add(this.linkColourClass);
            e.classList.add("underline")
        });
        htmx.findAll(this.rowSelector()).forEach((e) => { e.addEventListener("click", ev => this.highlightRow((ev.target as HTMLElement).closest('tr'))) });
        let row: HTMLElement = document.querySelector(this.rowSelector());
        if (row) {
            row.click();
        }

        this.invokeEventHandler('PageLoaded');
    }

    invokeEventHandler(eventName, args = {}) {
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
        let tbody = this.gridControl.querySelector("tbody") as HTMLElement;
        let currentPage = parseInt(tbody.dataset.currentpage);
        let totalPages = parseInt(tbody.dataset.totalpages);

        if (totalPages == 0) {
            this.updateLinkedGrids('');
        }

        if (this.toolbarExists()) {
            if (totalPages == 0) {
                this.removeClass('#no-records', "hidden");
                this.addClass('#toolbar', "hidden");
            }
            else {
                this.addClass('#no-records', "hidden");
                this.removeClass('#toolbar', "hidden");
            }

            this.setPageNumber(currentPage);
            (this.selectGridElement('[data-type="total-pages"]') as HTMLInputElement).value = totalPages.toString();

            this.getButton("first").disabled = currentPage == 1;
            this.getButton("previous").disabled = currentPage == 1;
            this.getButton("next").disabled = currentPage == totalPages;
            this.getButton("last").disabled = currentPage == totalPages;
        }
    }

    removeClass(selector: string, className: string) {
        this.selectGridElement(selector).classList.remove(className);
    }

    addClass(selector: string, className: string) {
        this.selectGridElement(selector).classList.add(className);
    }

    setPageNumber(pageNumber: number) {
        (this.selectGridElement('[name="page"]') as HTMLSelectElement).value = pageNumber.toString();
    }

    toolbarExists() {
        return this.selectGridElement('#toolbar');
    }

    configureSortIcon() {
        if (this.gridControl.querySelectorAll(`th[data-key]`).length == 0) {
            return
        }
        let tbody = document.querySelector(this.tbodySelector()) as HTMLElement;
        let sortKey = tbody.dataset.sortkey;
        let sortIcon = tbody.querySelector("span#sortIcon").innerHTML;

        this.gridControl.querySelectorAll(`th[data-key] span`).forEach(e => e.innerHTML = '')

        let span = this.gridControl.querySelector(`th[data-key="${sortKey}"] span`)

        if (!span) {
            span = this.gridControl.querySelectorAll(`th[data-key] span`)[0]
        }

        span.innerHTML = sortIcon
    }

    configureNestedGrid(target: HTMLElement) {
        let tr = target.closest("tr")

        if (!tr || tr.classList.contains("nested-grid-row") == false) {
            return;
        }

        (tr.previousElementSibling as HTMLElement).click()
        let buttons = tr.previousElementSibling.firstElementChild.querySelectorAll("button")

        buttons[0].style.display = 'none'
        buttons[2].style.display = 'block'

        buttons[1].addEventListener("click", ev => this.showHideNestedGrid(ev, true));
        buttons[2].addEventListener("click", ev => this.showHideNestedGrid(ev, false));
    }

    showHideNestedGrid(ev, show) {
        let tr = ev.target.closest("tr")
        tr.nextElementSibling.style.display = show ? null : "none"

        let buttons = tr.firstElementChild.querySelectorAll("button")

        buttons[1].style.display = show ? "none" : "block"
        buttons[2].style.display = show ? "block" : "none"
    }

    loadFromParent(primaryKey: string) {
        let selector = `#${this.gridId} input[name="primaryKey"]`
        let pk = htmx.find(selector) as HTMLInputElement

        console.log(primaryKey)
        this.gridControl.setAttribute("hx-vals", JSON.stringify({ primaryKey: primaryKey }))

        if (pk) {
            htmx.trigger(selector, "changed");
        }
        else {
            htmx.trigger(`#${this.gridId}`, "submit");
        }
    }

    isElementLoaded = async selector => {
        while (document.querySelector(selector) === null) {
            await new Promise(resolve => requestAnimationFrame(resolve))
        }
        return document.querySelector(selector);
    };

    highlightRow(tr) {
        this.clearHighlighting();
        tr.classList.add(this.bgColourClass);
        tr.classList.add(this.textColourClass);
        tr.querySelectorAll("a").forEach(e => e.classList.remove(this.linkColourClass));
        tr.querySelectorAll("svg").forEach(e => e.setAttribute("fill", "#ffffff"));
        this.updateLinkedGrids(tr.dataset.id);
        this.invokeEventHandler('RowSelected', { selectedRow: tr });
    }

    updateLinkedGrids(primaryKey: string) {
        let table = this.gridControl.querySelector("table") as HTMLElement;

        if (table.dataset.linkedgridid) {
            this.isElementLoaded(`#${table.dataset.linkedgridid}`).then((selector) => {
                DbNetSuiteCore.gridControlArray[table.dataset.linkedgridid].loadFromParent(primaryKey);
            })
        }
    }

    clearHighlighting() {
        this.gridControl.querySelectorAll(this.rowSelector()).forEach(e => {
            let tr = e.closest("tr");
            tr.classList.remove(this.bgColourClass);
            tr.classList.remove(this.textColourClass);
            tr.querySelectorAll("a").forEach(e => e.classList.add(this.linkColourClass));
            tr.querySelectorAll("svg").forEach(e => e.setAttribute("fill", "#666666"));
        });
    }

    copyTableToClipboard() {
        var table = this.gridControl.querySelector(this.tableSelector());
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

    download() {
        this.showIndicator()
        const data = new URLSearchParams();
        for (let [key, val] of new FormData(this.gridControl)) {
            data.append(key, val as any);
        }

        var exportOption = (this.selectGridElement('[name="exportformat"]') as HTMLSelectElement).value
        console.log(exportOption)

        fetch("gridcontrol.htmx", {
            method: 'post',
            body: data,
            headers: {
                'hx-trigger-name': 'download'
            },
        })
            .then((response) => response.blob())
            .then((blob) => {
                this.hideIndicator()
                if (exportOption == "html") {
                    this.openWindow(blob)
                }
                else {
                    this.downloadFile(blob, exportOption)
                }
            });
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
        var toast = this.gridContainer.querySelector(".toast > div")
        toast.classList.add(`alert-${style}`)
        toast.querySelector("span").innerText = text;
        if (text == "") {
            toast.classList.remove(`alert-${style}`)
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

    tableSelector() {
        return `#${this.gridId} table`
    }

    rowSelector() {
        return `#tbody${this.gridId} > tr.grid-row`
    }

    linkSelector() {
        return `#${this.gridId} tbody a`
    }

    tbodySelector() {
        return `#${this.gridId} tbody`
    }

    gridSelector() {
        return `#${this.gridId}`
    }

    errorSelector() {
        return `#${this.gridId} > div.alert-error`
    }

    selectGridElement(selector): HTMLElement {
        return document.querySelector(`#${this.gridId} ${selector}`);
    }

    buttonSelector(buttonType) {
        return `#${this.gridId} button[button-type="${buttonType}"]`
    }

    columnCells(columnName) {
        let th: HTMLTableCellElement = document.querySelector(`#${this.gridId} th[data-columnname='${columnName.toLowerCase()}']`)
        return document.querySelectorAll(`#${this.gridId} td:nth-child(${(th.cellIndex + 1)})`)
    }

    getButton(name): HTMLButtonElement {
        return document.querySelector(this.buttonSelector(name))
    }

}