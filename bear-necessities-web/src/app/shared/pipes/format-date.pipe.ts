import { Pipe, PipeTransform } from '@angular/core';
import { DatePipe } from '@angular/common';

@Pipe({
  name: 'formatDate',
  standalone: false
})
export class FormatDatePipe implements PipeTransform {
  constructor(private datePipe: DatePipe) {}

  transform(value: string | Date, format: string = 'short'): string {
    if (!value) return '';
    return this.datePipe.transform(value, format) || '';
  }
}
