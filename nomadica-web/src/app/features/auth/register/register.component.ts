import { Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  standalone: false
})
export class RegisterComponent {
  registerForm: FormGroup;
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  pendingMessage = signal<string | null>(null);
  hidePassword = true;
  hideConfirmPassword = true;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      displayName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      socialProfileUrl: ['', [
        Validators.required,
        Validators.pattern(/^https?:\/\/(www\.|m\.)?(instagram\.com|facebook\.com)\/.+/)
      ]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    }, {
      validators: this.passwordsMatchValidator
    });
  }

  private passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if (!password || !confirmPassword) {
      return null;
    }

    return password.value === confirmPassword.value ? null : { passwordsMismatch: true };
  }

  onSubmit(): void {
    if (!this.registerForm.valid) {
      Object.keys(this.registerForm.controls).forEach(key => {
        this.registerForm.get(key)?.markAsTouched();
      });
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { displayName, email, password, socialProfileUrl } = this.registerForm.value;
    this.authService.register({ displayName, email, password, socialProfileUrl }).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        this.pendingMessage.set(response.message);
      },
      error: (err) => {
        console.error('Register error:', err);
        if (err.status === 422) {
          if (err.error?.errors?.email) {
            this.errorMessage.set('This email is already taken');
          } else {
            this.errorMessage.set('Please check your input and try again');
          }
        } else {
          this.errorMessage.set('Registration failed. Please try again.');
        }
        this.isLoading.set(false);
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
