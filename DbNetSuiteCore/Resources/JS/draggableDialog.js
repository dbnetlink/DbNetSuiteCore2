class DraggableDialog {
    constructor(dialogId, dragHandleClass = 'dialog-header') {
        this.isDragging = false;
        this.initialX = 0;
        this.initialY = 0;
        this.xOffset = 0;
        this.yOffset = 0;
        this.dialog = document.getElementById(dialogId);
        if (!this.dialog) {
            throw new Error(`Dialog with id "${dialogId}" not found`);
        }
        this.dragHandle = this.dialog.querySelector(`.${dragHandleClass}`);
        if (!this.dragHandle) {
            throw new Error(`Drag handle with class "${dragHandleClass}" not found in the dialog`);
        }
        this.initDragEvents();
    }
    initDragEvents() {
        this.dragHandle.addEventListener('mousedown', this.startDragging.bind(this));
        document.addEventListener('mousemove', this.drag.bind(this));
        document.addEventListener('mouseup', this.stopDragging.bind(this));
        var rect = this.dialog.getBoundingClientRect();
        this.xOffset = rect.left - rect.width;
        this.yOffset = rect.top - rect.height;
        const computedStyle = window.getComputedStyle(this.dialog);
        const transformValue = computedStyle.transform;
        // Parse the transform matrix to extract tx and ty
        let tx = 0, ty = 0;
        if (transformValue && transformValue !== 'none') {
            const matrix = transformValue.match(/^matrix\((.+)\)$/);
            if (matrix) {
                const values = matrix[1].split(', ');
                tx = parseFloat(values[4]); // tx is the 5th value in the matrix
                ty = parseFloat(values[5]); // ty is the 6th value in the matrix
            }
        }
        this.xOffset = tx;
        this.yOffset = ty;
        this.setTranslate(this.xOffset, this.yOffset);
    }
    startDragging(e) {
        if (e.target.closest(`.${this.dragHandle.className}`) === this.dragHandle) {
            this.isDragging = true;
            this.initialX = e.clientX - (this.xOffset ? this.xOffset : (this.dialog.clientWidth / 2) * -1);
            this.initialY = e.clientY - (this.yOffset ? this.yOffset : (this.dialog.clientHeight / 2) * -1);
            this.dragHandle.style.cursor = 'move';
            document.body.style.userSelect = 'none'; // Prevent text selection during drag
        }
    }
    drag(e) {
        if (this.isDragging) {
            e.preventDefault();
            this.xOffset = e.clientX - this.initialX;
            this.yOffset = e.clientY - this.initialY;
            this.setTranslate(this.xOffset, this.yOffset);
        }
    }
    stopDragging() {
        this.isDragging = false;
        this.dragHandle.style.cursor = '';
        document.body.style.userSelect = ''; // Re-enable text selection
        document.removeEventListener('mousemove', this.drag);
        document.removeEventListener('mouseup', this.stopDragging);
    }
    setTranslate(xPos, yPos) {
        console.log(`xPos: ${xPos}, yPos: ${yPos}`);
        requestAnimationFrame(() => {
            this.dialog.style.transform = `translate3d(${xPos}px, ${yPos}px, 0)`;
        });
    }
}
