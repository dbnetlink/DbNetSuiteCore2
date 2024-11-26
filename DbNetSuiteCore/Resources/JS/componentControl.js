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
