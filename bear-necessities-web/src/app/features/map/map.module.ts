import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MapComponent } from './map.component';
import { HappeningNowPanelComponent } from './happening-now/happening-now-panel.component';
import { MapRoutingModule } from './map-routing.module';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  declarations: [MapComponent, HappeningNowPanelComponent],
  imports: [
    CommonModule,
    SharedModule,
    MapRoutingModule
  ]
})
export class MapModule { }
