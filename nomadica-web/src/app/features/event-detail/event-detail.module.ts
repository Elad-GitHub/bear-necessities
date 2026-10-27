import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { EventDetailComponent } from './event-detail.component';
import { EventDetailRoutingModule } from './event-detail-routing.module';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  declarations: [EventDetailComponent],
  imports: [CommonModule, RouterModule, EventDetailRoutingModule, SharedModule]
})
export class EventDetailModule { }
