import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TripPlannerComponent } from './trip-planner.component';

const routes: Routes = [
  { path: '', component: TripPlannerComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TripPlannerRoutingModule { }
