import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { FeedbackService } from '../../core/services/feedback.service';
import { AuthService } from '../../core/services/auth.service';
import { FEEDBACK_MAX_LENGTH, FEEDBACK_MIN_LENGTH, FeedbackKind } from '../../core/models/feedback.model';

interface KindOption {
  kind: FeedbackKind;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-feedback',
  templateUrl: './feedback.component.html',
  styleUrls: ['./feedback.component.scss'],
  standalone: false
})
export class FeedbackComponent {
  readonly kinds: KindOption[] = [
    { kind: 'bug', label: 'Something broke', icon: 'bug_report' },
    { kind: 'idea', label: 'An idea', icon: 'lightbulb' },
    { kind: 'other', label: 'Something else', icon: 'chat' }
  ];
  readonly maxLength = FEEDBACK_MAX_LENGTH;

  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly feedbackService = inject(FeedbackService);
  readonly authService = inject(AuthService);

  isSending = signal(false);
  isSent = signal(false);

  feedbackForm = this.formBuilder.nonNullable.group({
    kind: ['bug' as FeedbackKind, Validators.required],
    message: ['', [Validators.required, Validators.minLength(FEEDBACK_MIN_LENGTH), Validators.maxLength(FEEDBACK_MAX_LENGTH)]],
    contactEmail: ['', Validators.email]
  });

  // The page the member was on when they opened the form, so a bug report says where it happened
  private readonly fromPage = this.route.snapshot.queryParamMap.get('from');

  selectKind(kind: FeedbackKind): void {
    this.feedbackForm.controls.kind.setValue(kind);
  }

  messagePlaceholder(): string {
    switch (this.feedbackForm.controls.kind.value) {
      case 'bug': return 'What did you do, and what happened instead?';
      case 'idea': return 'What would make Bear Necessities better for you?';
      default: return 'Tell us anything';
    }
  }

  sendFeedback(): void {
    if (this.feedbackForm.invalid) {
      this.feedbackForm.markAllAsTouched();
      return;
    }

    const { kind, message, contactEmail } = this.feedbackForm.getRawValue();
    this.isSending.set(true);
    this.feedbackService.submitFeedback({
      kind,
      message: message.trim(),
      pageUrl: this.fromPage,
      userAgent: navigator.userAgent,
      contactEmail: contactEmail.trim() || null
    }).subscribe({
      next: () => {
        this.isSending.set(false);
        this.isSent.set(true);
      },
      error: () => this.isSending.set(false)
    });
  }

  sendAnother(): void {
    this.feedbackForm.reset({ kind: 'bug', message: '', contactEmail: '' });
    this.isSent.set(false);
  }
}
