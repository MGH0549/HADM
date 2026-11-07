export function localDate(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

export function localDateTimeInput(date = new Date()): string {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}

export function utcFromLocalInput(value: string): string {
  return new Date(value).toISOString();
}

export function reportStartDate(endDate: string, days = 7): string {
  const end = new Date(`${endDate}T12:00:00`);
  end.setDate(end.getDate() - days + 1);
  return localDate(end);
}
