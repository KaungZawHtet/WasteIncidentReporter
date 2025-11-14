export function formatTimestamp(value: string) {
    try {
        return new Intl.DateTimeFormat(undefined, {
            dateStyle: 'medium',
            timeStyle: 'short',
        }).format(new Date(value));
    } catch {
        return value;
    }
}

const INPUT_SLICE_LENGTH = 16;

const toLocalInputString = (date: Date) => {
    const offsetMinutes = date.getTimezoneOffset();
    const local = new Date(date.getTime() - offsetMinutes * 60 * 1000);
    return local.toISOString().slice(0, INPUT_SLICE_LENGTH);
};

export function toInputDate(value: string) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return toLocalInputString(new Date());
    }
    return toLocalInputString(date);
}

export function nowInputDate() {
    return toLocalInputString(new Date());
}
