interface Dictionary<T> {
    [Key: string]: T;
}

interface RowModification {
    modified: boolean;
    columns: Array<string>;
}

interface Selection {
    value: string;
    description: string;
    parentValues: Array<string>;
    parentDescriptions: Array<string>;
}
