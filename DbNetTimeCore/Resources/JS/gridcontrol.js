class GridControl {
    constructor(gridId) {
        this.gridId = gridId;
        this.gridControl = document.querySelector(this.gridSelector())
        this.gridContainer = this.gridControl.parentElement
    }

    gridId = "";
    bgColourClass = "bg-cyan-600";
    textColourClass = "text-zinc-100";
    linkColourClass = "text-blue-500";

    tableSelector() {
        return `#${this.gridId} table`
    }

    rowSelector() {
        return `#${this.gridId} tbody tr.grid-row`
    }

    linkSelector() {
        return `#${this.gridId} tbody a`
    }

    linkSelector() {
        return `#${this.gridId} tbody a`
    }

    gridSelector() {
        return `#${this.gridId}`
    }

    selectGridElement(selector) {
        return document.querySelector(`#${this.gridId} ${selector}`); 
    }

    buttonSelector(buttonType) {
        return `#${this.gridId} button[button-type="${buttonType}"]`
    }

    columnCells(columnName) {
        let th = me(`#${this.gridId} th[data-columnname='${columnName.toLowerCase()}']`)
        return any(`#${this.gridId} td:nth-child(${(th.cellIndex + 1)})`)
    }

    getButton(name) {
        return document.querySelector(this.buttonSelector(name))
    }

    init(evt) {
        if (evt.detail.target.id != this.gridId) {
            return
        }

        me(this.getButton("copy")).on("click", ev => this.copyTableToClipboard())
        me(this.getButton("export")).on("click", ev => this.download())

        document.querySelectorAll(this.linkSelector()).forEach((e) => { me(e).classAdd(this.linkColourClass).classAdd("underline") });
        document.querySelectorAll(this.rowSelector()).forEach((e) => { me(e).on("click", ev => this.highlightRow(me(ev))) });
        let row = document.querySelector(this.rowSelector());
        if (row) {
            row.click();
        }
        const event = new CustomEvent(`${this.gridId}:pageLoaded`, { detail: { gridControl: this, htmxEvent: evt } });
        document.dispatchEvent(event);
    }

    highlightRow(tr) {
        this.clearHighlighting();
        me(tr).classAdd(this.bgColourClass).classAdd(this.textColourClass);
        any(me(tr).querySelectorAll("a")).classRemove(this.linkColourClass);
    }

    clearHighlighting() {
        any(this.rowSelector()).run(e => {
            let tr = e.closest("tr");
            me(tr).classRemove(this.bgColourClass).classRemove(this.textColourClass);
            any(me(tr).querySelectorAll("a")).classAdd(this.linkColourClass);
        });
    }

    copyTableToClipboard() {
        var table = document.querySelector(this.tableSelector());
        try {
            const range = document.createRange();
            range.selectNode(table);
            window.getSelection()?.addRange(range);
            document.execCommand('copy');
            window.getSelection()?.removeRange(range);
            this.message("Page copied to clipboard")
        } catch (e) {
            try {
                const content = table.innerHTML;
                const blobInput = new Blob([content], { type: 'text/html' });
                const clipboardItemInput = new ClipboardItem({ 'text/html': blobInput });
                navigator.clipboard.write([clipboardItemInput]);
            }
            catch (e) {
                this.error("Copy failed")
                return
            }
        }
    }

    download() {
        this.showIndicator()
        const data = new URLSearchParams();
        for (const pair of new FormData(this.gridControl)) {
            data.append(pair[0], pair[1]);
        }

        var exportOption = this.selectGridElement('[name="exportformat"]').value
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

    message(text) {
        var toast = this.gridContainer.querySelector(".toast")
        toast.querySelector("span").innerText = text;
        if (text == "") {
            toast.style.display = 'none'
            return
        }
        toast.style.display = 'block'
        let self = this
        window.setTimeout(() => { self.message("") }, 1000)
    }

    showIndicator() {
        this.indicator().classAdd("htmx-request");
    }

    hideIndicator() {
        this.indicator().classRemove("htmx-request");
    }

    indicator() {
        return me(this.gridContainer.children[1]);
    }

}