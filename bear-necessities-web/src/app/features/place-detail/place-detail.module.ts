import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { PlaceDetailRoutingModule } from './place-detail-routing.module';
import { PlaceDetailComponent } from './place-detail.component';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  declarations: [PlaceDetailComponent],
  imports: [
    CommonModule,
    PlaceDetailRoutingModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatChipsModule,
    SharedModule
  ]
})
export class PlaceDetailModule { }
