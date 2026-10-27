import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SubmitListingComponent } from './submit-listing.component';
import { SubmitListingRoutingModule } from './submit-listing-routing.module';

@NgModule({
  declarations: [SubmitListingComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    SubmitListingRoutingModule
  ]
})
export class SubmitListingModule { }
