var gridControlArray = [];
class GridControl {
    constructor(gridId) {
        this.gridId = "";
        this.eventHandlers = {};
        this.bgColourClass = "bg-cyan-600";
        this.textColourClass = "text-zinc-100";
        this.linkColourClass = "text-blue-500";
        this.gridId = gridId;
        this.gridControl = document.querySelector(this.gridSelector());
        this.gridContainer = this.gridControl.parentElement;
    }
    init(evt) {
        let gridId = evt.target.closest("form").id;
        if (gridId.startsWith(this.gridId) == false) {
            return;
        }
        if (document.querySelector(this.errorSelector())) {
            return;
        }
        this.configureNavigation();
        this.configureSortIcon();
        if (gridId == this.gridId) {
            if (this.toolbarExists()) {
                this.getButton("copy").addEventListener("click", ev => this.copyTableToClipboard());
                this.getButton("export").addEventListener("click", ev => this.download());
            }
            this.configureNestedGrid(evt.target);
            this.invokeEventHandler('Initialised');
        }
        document.querySelectorAll(this.linkSelector()).forEach((e) => {
            e.classList.add(this.linkColourClass);
            e.classList.add("underline");
        });
        document.querySelectorAll(this.rowSelector()).forEach((e) => { e.addEventListener("click", ev => this.highlightRow(ev.target.closest('tr'))); });
        let row = document.querySelector(this.rowSelector());
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
            this.eventHandlers[eventName](this, args);
        }
        else {
            this.message(`Javascript function for event type '${eventName}' is not defined`, 'error', 3);
        }
    }
    configureNavigation() {
        let tbody = this.gridControl.querySelector("tbody");
        let currentPage = parseInt(tbody.dataset.currentpage);
        let totalPages = parseInt(tbody.dataset.totalpages);
        if (this.toolbarExists()) {
            if (totalPages == 0) {
                this.selectGridElement('#no-records').classList.remove("hidden");
                this.selectGridElement('#toolbar').classList.add("hidden");
            }
            else {
                this.selectGridElement('#no-records').classList.add("hidden");
                this.selectGridElement('#toolbar').classList.remove("hidden");
            }
            this.selectGridElement('[name="page"]').value = currentPage.toString();
            this.selectGridElement('[data-type="total-pages"]').value = totalPages.toString();
            this.getButton("first").disabled = currentPage == 1;
            this.getButton("previous").disabled = currentPage == 1;
            this.getButton("next").disabled = currentPage == totalPages;
            this.getButton("last").disabled = currentPage == totalPages;
        }
    }
    toolbarExists() {
        return this.selectGridElement('#toolbar');
    }
    configureSortIcon() {
        if (this.gridControl.querySelectorAll(`th[data-key]`).length == 0) {
            return;
        }
        let tbody = document.querySelector(this.tbodySelector());
        let sortKey = tbody.dataset.sortkey;
        let sortIcon = tbody.querySelector("span#sortIcon").innerHTML;
        this.gridControl.querySelectorAll(`th[data-key] span`).forEach(e => e.innerHTML = '');
        let span = this.gridControl.querySelector(`th[data-key="${sortKey}"] span`);
        if (!span) {
            span = this.gridControl.querySelectorAll(`th[data-key] span`)[0];
        }
        span.innerHTML = sortIcon;
    }
    configureNestedGrid(target) {
        let tr = target.closest("tr");
        if (!tr || tr.classList.contains("nested-grid-row") == false) {
            return;
        }
        let buttons = tr.previousElementSibling.firstElementChild.querySelectorAll("button");
        buttons[0].style.display = 'none';
        buttons[2].style.display = 'block';
        buttons[1].addEventListener("click", ev => this.showHideNestedGrid(ev, true));
        buttons[2].addEventListener("click", ev => this.showHideNestedGrid(ev, false));
    }
    showHideNestedGrid(ev, show) {
        let tr = ev.target.closest("tr");
        tr.nextElementSibling.style.display = show ? null : "none";
        let buttons = tr.firstElementChild.querySelectorAll("button");
        buttons[1].style.display = show ? "none" : "block";
        buttons[2].style.display = show ? "block" : "none";
    }
    highlightRow(tr) {
        this.clearHighlighting();
        tr.classList.add(this.bgColourClass);
        tr.classList.add(this.textColourClass);
        tr.querySelectorAll("a").forEach(e => e.classList.remove(this.linkColourClass));
        tr.querySelectorAll("svg").forEach(e => e.setAttribute("fill", "#ffffff"));
        this.invokeEventHandler('RowSelected', { selectedRow: tr });
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
    download() {
        this.showIndicator();
        const data = new URLSearchParams();
        for (let [key, val] of new FormData(this.gridControl)) {
            data.append(key, val);
        }
        var exportOption = this.selectGridElement('[name="exportformat"]').value;
        console.log(exportOption);
        fetch("gridcontrol.htmx", {
            method: 'post',
            body: data,
            headers: {
                'hx-trigger-name': 'download'
            },
        })
            .then((response) => response.blob())
            .then((blob) => {
            this.hideIndicator();
            if (exportOption == "html") {
                this.openWindow(blob);
            }
            else {
                this.downloadFile(blob, exportOption);
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
        var toast = this.gridContainer.querySelector(".toast > div");
        toast.classList.add(`alert-${style}`);
        toast.querySelector("span").innerText = text;
        if (text == "") {
            toast.classList.remove(`alert-${style}`);
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
    tableSelector() {
        return `#${this.gridId} table`;
    }
    rowSelector() {
        return `#tbody${this.gridId} > tr.grid-row`;
    }
    linkSelector() {
        return `#${this.gridId} tbody a`;
    }
    tbodySelector() {
        return `#${this.gridId} tbody`;
    }
    gridSelector() {
        return `#${this.gridId}`;
    }
    errorSelector() {
        return `#${this.gridId} > div.alert-error`;
    }
    selectGridElement(selector) {
        return document.querySelector(`#${this.gridId} ${selector}`);
    }
    buttonSelector(buttonType) {
        return `#${this.gridId} button[button-type="${buttonType}"]`;
    }
    columnCells(columnName) {
        let th = document.querySelector(`#${this.gridId} th[data-columnname='${columnName.toLowerCase()}']`);
        return document.querySelectorAll(`#${this.gridId} td:nth-child(${(th.cellIndex + 1)})`);
    }
    getButton(name) {
        return document.querySelector(this.buttonSelector(name));
    }
}
