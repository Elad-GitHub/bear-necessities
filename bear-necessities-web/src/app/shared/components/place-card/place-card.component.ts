import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';
import { PlaceSummaryDto } from '../../../core/models/place.model';

@Component({
  selector: 'app-place-card',
  templateUrl: './place-card.component.html',
  styleUrls: ['./place-card.component.scss'],
  standalone: false
})
export class PlaceCardComponent {
  @Input() place!: PlaceSummaryDto;

  protected readonly Math = Math;

  constructor(private router: Router) {}

  navigateToPlace(): void {
    this.router.navigate(['/places', this.place.id]);
  }

  getPlaceImageUrl(): string {
    return this.place.imageUrl || '';
  }

  hasImage(): boolean {
    return !!this.place.imageUrl;
  }
}
