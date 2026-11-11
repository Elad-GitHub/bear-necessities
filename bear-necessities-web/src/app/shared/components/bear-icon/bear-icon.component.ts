import { Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { BEAR_ICONS } from './bear-icons';

/** One of the app's bear icons, drawn in the surrounding text colour. */
@Component({
  selector: 'app-bear-icon',
  template: `<svg viewBox="0 0 24 24" [attr.width]="size()" [attr.height]="size()" fill="currentColor"
    aria-hidden="true" focusable="false" [innerHTML]="markup()"></svg>`,
  styles: [':host { display: inline-flex; line-height: 0; flex-shrink: 0; }'],
  standalone: false
})
export class BearIconComponent {
  private sanitizer = inject(DomSanitizer);

  name = input.required<string>();
  size = input(20);

  // The markup is our own generated registry, never user input, so it is safe to trust
  markup = computed<SafeHtml>(() => this.sanitizer.bypassSecurityTrustHtml(BEAR_ICONS[this.name()] ?? BEAR_ICONS['paw']));
}
