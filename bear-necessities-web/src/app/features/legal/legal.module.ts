import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { TermsComponent } from './terms/terms.component';
import { PrivacyComponent } from './privacy/privacy.component';

const routes: Routes = [
  { path: 'terms', component: TermsComponent, title: 'Terms of Use · Bear Necessities' },
  { path: 'privacy', component: PrivacyComponent, title: 'Privacy Policy · Bear Necessities' },
  { path: '', pathMatch: 'full', redirectTo: 'terms' }
];

@NgModule({
  declarations: [TermsComponent, PrivacyComponent],
  imports: [CommonModule, RouterModule.forChild(routes), SharedModule]
})
export class LegalModule { }
