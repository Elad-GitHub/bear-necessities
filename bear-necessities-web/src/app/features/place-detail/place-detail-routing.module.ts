import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PlaceDetailComponent } from './place-detail.component';

const routes: Routes = [
  {
    path: '',
    component: PlaceDetailComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PlaceDetailRoutingModule { }
