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
        DbNetSuiteCore.gridControlArray[gridId].init(evt);
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
        this.gridContainer = this.gridControl.parentElement;
    }
    init(evt) {
        let gridId = evt.target.closest("form").id;
        if (gridId.startsWith(this.gridId) == false || evt.detail.elt.name == "nestedGrid") {
            return;
        }
        //console.log(`init => event:${gridId} control:${this.gridId} trigger-name:${evt.detail.elt.name}`)
        if (htmx.find(this.errorSelector())) {
            return;
        }
        this.configureNavigation();
        this.configureSortIcon();
        if (gridId == this.gridId) {
            if (this.toolbarExists()) {
                this.getButton("copy").addEventListener("click", ev => this.copyTableToClipboard());
                this.getButton("export").addEventListener("click", ev => this.download());
            }
            this.invokeEventHandler('Initialised');
        }
        this.gridControl.querySelectorAll(".nested-buttons").forEach((div) => {
            let buttons = div.querySelectorAll("button");
            buttons[0].addEventListener("click", ev => this.showHideNestedGrid(ev, true));
            buttons[1].addEventListener("click", ev => this.showHideNestedGrid(ev, false));
        });
        htmx.findAll(this.cellSelector()).forEach((cell) => { this.invokeEventHandler('CellRendered', { cell: cell }); });
        htmx.findAll(this.linkSelector()).forEach((e) => {
            e.classList.remove("selected");
            e.classList.add("underline");
        });
        htmx.findAll(this.rowSelector()).forEach((e) => { e.addEventListener("click", ev => this.highlightRow(ev)); });
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
        if (totalPages == 0) {
            this.updateLinkedGrids('');
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
            this.selectGridElement('[data-type="total-pages"]').value = totalPages.toString();
            this.getButton("first").disabled = currentPage == 1;
            this.getButton("previous").disabled = currentPage == 1;
            this.getButton("next").disabled = currentPage == totalPages;
            this.getButton("last").disabled = currentPage == totalPages;
        }
    }
    removeClass(selector, className) {
        this.selectGridElement(selector).classList.remove(className);
    }
    addClass(selector, className) {
        this.selectGridElement(selector).classList.add(className);
    }
    setPageNumber(pageNumber, totalPages) {
        var select = this.selectGridElement('[name="page"]');
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
        return this.selectGridElement('#navigation');
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
    showHideNestedGrid(ev, show) {
        ev.stopPropagation();
        let tr = ev.target.closest("tr");
        let buttons = tr.firstElementChild.querySelectorAll("button");
        let siblingRow = tr.nextElementSibling;
        if (siblingRow && siblingRow.classList.contains("nested-grid-row")) {
            siblingRow.style.display = show ? null : "none";
        }
        else if (show) {
            htmx.trigger(buttons[2], "click");
        }
        buttons[0].style.display = show ? "none" : "block";
        buttons[1].style.display = show ? "block" : "none";
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
    highlightRow(ev) {
        let tr = ev.target.closest('tr');
        if (tr.classList.contains(this.bgColourClass)) {
            return;
        }
        // console.log(`current target => ${ev.currentTarget}`)
        // console.log(`target => ${ev.target}`)
        console.log(`highlight row => ${tr.querySelector("td[data-columnname]").innerText}`);
        this.clearHighlighting();
        tr.classList.add(this.bgColourClass);
        tr.classList.add(this.textColourClass);
        tr.querySelectorAll("a").forEach(e => e.classList.add("selected"));
        tr.querySelectorAll("td[data-columnname] > div > svg,td[data-isfolder='false'] > svg").forEach(e => e.setAttribute("fill", "#ffffff"));
        this.updateLinkedGrids(tr.dataset.id);
        this.invokeEventHandler('RowSelected', { selectedRow: tr });
    }
    updateLinkedGrids(primaryKey) {
        let table = this.gridControl.querySelector("table");
        if (table.dataset.linkedgridid) {
            this.isElementLoaded(`#${table.dataset.linkedgridid}`).then((selector) => {
                DbNetSuiteCore.gridControlArray[table.dataset.linkedgridid].loadFromParent(primaryKey);
            });
        }
    }
    clearHighlighting() {
        this.gridControl.querySelectorAll(this.rowSelector()).forEach(e => {
            let tr = e.closest("tr");
            tr.classList.remove(this.bgColourClass);
            tr.classList.remove(this.textColourClass);
            tr.querySelectorAll("a").forEach(e => e.classList.remove("selected"));
            tr.querySelectorAll("td[data-columnname] > div > svg,td[data-isfolder='false'] > svg").forEach(e => e.setAttribute("fill", "#666666"));
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
    tableSelector() {
        return `#${this.gridId} table`;
    }
    rowSelector() {
        return `#tbody${this.gridId} > tr.grid-row`;
    }
    cellSelector() {
        return `#tbody${this.gridId} td[data-columnname]`;
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
