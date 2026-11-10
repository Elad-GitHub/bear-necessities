import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TripPlannerComponent } from './trip-planner.component';
import { TripPlannerRoutingModule } from './trip-planner-routing.module';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  declarations: [TripPlannerComponent],
  imports: [
    CommonModule,
    RouterModule,
    TripPlannerRoutingModule,
    SharedModule
  ]
})
export class TripPlannerModule { }
