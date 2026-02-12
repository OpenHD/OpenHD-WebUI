import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FilesComponent } from './files/files.component';
import { SystemComponent } from './system/system.component';
import { UpdateComponent } from './update/update.component';
import { SettingsComponent } from './settings/settings.component';
import { StatusComponent } from './status/status.component';
import { HardwareComponent } from './hardware/hardware.component';

const routes: Routes = 
[
  { path: '', redirectTo: 'status', pathMatch: 'full' },
  { path: 'status', component: StatusComponent },
  { path: 'files', component: FilesComponent },
  { path: 'settings', component: SettingsComponent },
  { path: 'hardware', component: HardwareComponent },
  { path: 'system', component: SystemComponent },
  { path: 'update', component: UpdateComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
