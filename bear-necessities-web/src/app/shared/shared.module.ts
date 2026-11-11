import { NgModule } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';

import { TimeAgoPipe } from './pipes/time-ago.pipe';
import { FormatDatePipe } from './pipes/format-date.pipe';
import { NavbarComponent } from './components/navbar/navbar.component';
import { CategoryChipsComponent } from './components/category-chips/category-chips.component';
import { PlaceCardComponent } from './components/place-card/place-card.component';
import { LoadingSpinnerComponent } from './components/loading-spinner/loading-spinner.component';
import { PhotoCreditComponent } from './components/photo-credit/photo-credit.component';
import { BearIconComponent } from './components/bear-icon/bear-icon.component';

@NgModule({
  declarations: [
    TimeAgoPipe,
    FormatDatePipe,
    NavbarComponent,
    CategoryChipsComponent,
    PlaceCardComponent,
    LoadingSpinnerComponent,
    PhotoCreditComponent,
    BearIconComponent
  ],
  imports: [
    CommonModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatDividerModule
  ],
  exports: [
    TimeAgoPipe,
    FormatDatePipe,
    NavbarComponent,
    CategoryChipsComponent,
    PlaceCardComponent,
    LoadingSpinnerComponent,
    PhotoCreditComponent,
    BearIconComponent
  ],
  providers: [DatePipe]
})
export class SharedModule { }
