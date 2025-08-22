import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';

@Component({
  selector: 'app-frontpage',
  templateUrl: './frontpage.component.html',
  styleUrls: ['./frontpage.component.scss']
})
export class FrontpageComponent {
  isLoginOpen = false;

  loginForm = this.fb.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]],
    remember: [true]
  });

  constructor(private fb: FormBuilder) {}

  openLogin() { this.isLoginOpen = true; }
  closeLogin() { this.isLoginOpen = false; }

  submitLogin() {
    if (this.loginForm.invalid) { return; }
    const { username, password, remember } = this.loginForm.value;
    // TODO: wire auth; show a toast on success/failure
    console.log('login', { username, password: '•••', remember });
    this.closeLogin();
  }
}
