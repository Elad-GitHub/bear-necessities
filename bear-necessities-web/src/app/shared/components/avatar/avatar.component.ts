import { Component, input } from '@angular/core';

/**
 * A member's round photo. Without one, a bear head on honey yellow stands in,
 * so every member looks like part of the family instead of a pair of initials.
 */
@Component({
  selector: 'app-avatar',
  template: `
    @if (photoUrl(); as photo) {
      <img [src]="photo" [alt]="alt()" />
    } @else {
      <svg viewBox="0 0 100 100" role="img" [attr.aria-label]="alt()">
        <rect width="100" height="100" fill="#e8a33d" />
        <g fill="#5b3a1e">
          <circle cx="29" cy="31" r="12" />
          <circle cx="71" cy="31" r="12" />
          <circle cx="50" cy="57" r="29" />
        </g>
        <ellipse cx="50" cy="66" rx="11" ry="8.5" fill="#c98a3a" />
        <ellipse cx="50" cy="62" rx="4.5" ry="3.2" fill="#2e1c0e" />
      </svg>
    }`,
  styles: [`
    :host {
      display: inline-flex;
      border-radius: 50%;
      overflow: hidden;
      flex-shrink: 0;
      background: #e8a33d;
    }
    img, svg { width: 100%; height: 100%; display: block; }
    img { object-fit: cover; }`],
  standalone: false
})
export class AvatarComponent {
  photoUrl = input<string | null | undefined>(null);
  alt = input('Profile photo');
}
