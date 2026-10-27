import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SubmitListingComponent } from './submit-listing.component';

const routes: Routes = [
  {
    path: '',
    component: SubmitListingComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SubmitListingRoutingModule { }
