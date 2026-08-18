// Main entry point - bundled into a single global IIFE.
// All exported classes/objects are exposed on the global `DbNetSuite` namespace
// (and DbNetSuiteCore retains its original global shape for backward compatibility).

export { ComponentControl } from './ComponentControl.js';
export { GridControl } from './GridControl.js';
export { FormControl } from './FormControl.js';
export { SelectControl } from './SelectControl.js';
export { TreeControl } from './TreeControl.js';
export { Dialog } from './Dialog.js';
export { DraggableDialog } from './DraggableDialog.js';
export { ConfirmDialog } from './ConfirmDialog.js';
export { LookupDialog } from './LookupDialog.js';
export { SearchDialog } from './SearchDialog.js';
export { ViewDialog } from './ViewDialog.js';
export { HtmlEditor } from './HtmlEditor.js';
export type { Dictionary, RowModification, Selection } from './types.js';
export { DbNetSuiteCore } from './DbNetSuiteCore.js';
