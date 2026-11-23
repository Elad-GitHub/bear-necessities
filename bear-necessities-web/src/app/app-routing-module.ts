import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';

const routes: Routes = [
  {
    path: '',
    redirectTo: '/map',
    pathMatch: 'full'
  },
  {
    path: 'map',
    loadChildren: () => import('./features/map/map.module').then(m => m.MapModule)
  },
  {
    path: 'places/:id',
    loadChildren: () => import('./features/place-detail/place-detail.module').then(m => m.PlaceDetailModule)
  },
  {
    path: 'events/:id',
    loadChildren: () => import('./features/event-detail/event-detail.module').then(m => m.EventDetailModule)
  },
  {
    // The auth module owns both /login and /register
    path: '',
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule)
  },
  {
    path: 'profile',
    canActivate: [AuthGuard],
    loadChildren: () => import('./features/profile/profile.module').then(m => m.ProfileModule)
  },
  {
    path: 'submit',
    canActivate: [AuthGuard],
    loadChildren: () => import('./features/submit-listing/submit-listing.module').then(m => m.SubmitListingModule)
  },
  {
    path: 'groups',
    loadChildren: () => import('./features/groups/groups.module').then(m => m.GroupsModule)
  },
  {
    path: 'legal',
    loadChildren: () => import('./features/legal/legal.module').then(m => m.LegalModule)
  },
  {
    // Open to signed-out visitors too: a broken sign-in is exactly what someone might want to report
    path: 'feedback',
    loadChildren: () => import('./features/feedback/feedback.module').then(m => m.FeedbackModule)
  },
  {
    path: 'admin',
    canActivate: [AuthGuard],
    loadChildren: () => import('./features/admin/admin.module').then(m => m.AdminModule)
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
