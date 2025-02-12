import { Component } from '@angular/core';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { Subscription, finalize } from 'rxjs';

@Component({
  selector: 'app-update',
  templateUrl: './update.component.html',
  styleUrl: './update.component.css'
})
export class UpdateComponent {

  fileName = '';
  isFileSelected = false;
  file?:File;
  uploadProgress:number = 0;
  subscription?: Subscription;
  isUploaded = false;
  isUploadInprogress = false;

  constructor(private http: HttpClient) {}

  onFileSelected(event: Event) {
    this.isFileSelected = false;
    let typedTarget = event.target as HTMLInputElement;
    let files = typedTarget?.files;
    if(files){
      this.file = files[0];

      if (this.file) {

        this.fileName = this.file.name;
        this.isFileSelected = true;
      }
    }
  }

  onUploadClick(){
    if(!this.file) {
      return;
    }

    this.isUploadInprogress = true;    
    
    const upload$ = this.http.post(
      "/api/update/upload", 
      this.file,
    {
      reportProgress: true,
      observe: "events"
    })
    .pipe(
      finalize(() => this.reset())
    );

    this.subscription = upload$.subscribe(event => {
      if (event.type == HttpEventType.UploadProgress) {
        this.uploadProgress = Math.round(100 * (event.loaded / event.total!));
      }
    });
  }

  onRebootClick(){
    this.http.post('/api/system/run-command', {id : "sys-reboot"}).subscribe(result => {}, error => console.error(error));
  }

  reset() {
    this.subscription = undefined;
    this.isUploadInprogress = false;
    this.isUploaded = true;
  }
}
