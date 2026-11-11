import { Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { BEAR_ICONS, BEAR_ICON_IMAGES } from './bear-icons';

/**
 * One of the app's bear icons, drawn in the surrounding text colour. Illustrated icons are PNG
 * silhouettes used as a CSS mask so they take the text colour; the rest are inline SVG.
 */
@Component({
  selector: 'app-bear-icon',
  template: `
    @if (imageUrl(); as url) {
      <span class="bear-mask" [style.width.px]="size()" [style.height.px]="size()"
        [style.mask-image]="url" [style.-webkit-mask-image]="url" aria-hidden="true"></span>
    } @else {
      <svg viewBox="0 0 24 24" [attr.width]="size()" [attr.height]="size()" fill="currentColor"
        aria-hidden="true" focusable="false" [innerHTML]="markup()"></svg>
    }`,
  styles: [`
    :host { display: inline-flex; line-height: 0; flex-shrink: 0; }
    .bear-mask {
      display: inline-block;
      background-color: currentColor;
      mask-size: contain; -webkit-mask-size: contain;
      mask-repeat: no-repeat; -webkit-mask-repeat: no-repeat;
      mask-position: center; -webkit-mask-position: center;
    }`],
  standalone: false
})
export class BearIconComponent {
  private sanitizer = inject(DomSanitizer);

  name = input.required<string>();
  size = input(20);

  imageUrl = computed(() => BEAR_ICON_IMAGES.has(this.name()) ? `url(brand/icons/${this.name()}.png)` : null);

  // The markup is our own generated registry, never user input, so it is safe to trust
  markup = computed<SafeHtml>(() => this.sanitizer.bypassSecurityTrustHtml(BEAR_ICONS[this.name()] ?? BEAR_ICONS['paw']));
}
