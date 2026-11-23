import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { SharedModule } from '../../shared/shared.module';
import { FeedbackComponent } from './feedback.component';

const routes: Routes = [
  { path: '', component: FeedbackComponent, title: 'Send feedback · Bear Necessities' }
];

@NgModule({
  declarations: [FeedbackComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule.forChild(routes),
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    SharedModule
  ]
})
export class FeedbackModule { }
