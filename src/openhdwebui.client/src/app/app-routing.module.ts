import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FilesComponent } from './files/files.component';
import { SystemComponent } from './system/system.component';
import { UpdateComponent } from './update/update.component';

const routes: Routes = 
[
  { path: '', component: FilesComponent, pathMatch: 'full' },
  { path: 'files', component: FilesComponent },
  { path: 'system', component: SystemComponent },
  { path: 'update', component: UpdateComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
