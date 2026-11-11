import { Component, Input } from '@angular/core';

/** The credit a freely licensed photo (e.g. from Wikimedia Commons) must show, pinned to the photo's corner. */
@Component({
  selector: 'app-photo-credit',
  templateUrl: './photo-credit.component.html',
  styleUrls: ['./photo-credit.component.scss'],
  standalone: false
})
export class PhotoCreditComponent {
  @Input({ required: true }) credit!: string;
  @Input() creditUrl?: string;
}
