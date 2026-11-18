import { Pipe, PipeTransform } from '@angular/core';
import { sizedPhotoUrl } from '../../core/utils/photo-size';

@Pipe({
  name: 'sizedPhoto',
  standalone: false
})
export class SizedPhotoPipe implements PipeTransform {
  transform(url: string | null | undefined, widthPixels: number): string | null {
    return sizedPhotoUrl(url, widthPixels);
  }
}
