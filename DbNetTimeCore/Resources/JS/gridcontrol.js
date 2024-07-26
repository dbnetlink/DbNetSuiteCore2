class GridControl {
    constructor(gridId) {
        this.gridId = gridId;
        this.init();
        let self = this;
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

    columnCells(columnName) {
        let th = me(`#${this.gridId} th[data-columnname='${columnName.toLowerCase()}']`)
        return any(`#${this.gridId} td:nth-child(${(th.cellIndex + 1)})`)
    }

    init(evt) {
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
}