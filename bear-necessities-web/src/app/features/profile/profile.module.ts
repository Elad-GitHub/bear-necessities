import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ProfileComponent } from './profile.component';
import { ProfileRoutingModule } from './profile-routing.module';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  declarations: [ProfileComponent],
  imports: [CommonModule, RouterModule, ProfileRoutingModule, SharedModule]
})
export class ProfileModule { }
