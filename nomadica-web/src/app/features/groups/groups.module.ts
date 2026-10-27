import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { GroupsComponent } from './groups.component';
import { GroupDetailComponent } from './group-detail/group-detail.component';
import { GroupsRoutingModule } from './groups-routing.module';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  declarations: [GroupsComponent, GroupDetailComponent],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    GroupsRoutingModule,
    SharedModule
  ]
})
export class GroupsModule { }
