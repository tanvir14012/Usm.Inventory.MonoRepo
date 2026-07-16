import { AbstractControl, ValidationErrors, ValidatorFn, AsyncValidatorFn } from '@angular/forms';
import { Observable, of, timer } from 'rxjs';
import { switchMap, map, catchError } from 'rxjs/operators';

/** Ensure two controls in a group match (e.g. password confirmation). */
export function mustMatch(controlName: string, matchingControlName: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const control = group.get(controlName);
    const matching = group.get(matchingControlName);
    if (!control || !matching) return null;
    if (matching.errors && !matching.errors['mustMatch']) return null;
    if (control.value !== matching.value) {
      matching.setErrors({ mustMatch: true });
    } else {
      matching.setErrors(null);
    }
    return null;
  };
}

/** Validates that value is a positive integer. */
export function positiveInteger(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value && control.value !== 0) return null;
    const v = Number(control.value);
    if (!Number.isInteger(v) || v <= 0) return { positiveInteger: true };
    return null;
  };
}

/** Saudi national ID validator (10-digit starting with 1 or 2). */
export function saudiNationalId(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) return null;
    const valid = /^[12]\d{9}$/.test(String(control.value));
    return valid ? null : { saudiNationalId: true };
  };
}

/** No whitespace-only input. */
export function noWhitespace(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (typeof control.value !== 'string') return null;
    const isBlank = control.value.trim().length === 0 && control.value.length > 0;
    return isBlank ? { whitespace: true } : null;
  };
}

/** Arabic text only. */
export function arabicOnly(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) return null;
    const valid = /^[\u0600-\u06FF\s\d]+$/.test(control.value);
    return valid ? null : { arabicOnly: true };
  };
}

/** File size validator for file input controls. */
export function maxFileSize(maxBytes: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const file = control.value as File;
    if (!file || !(file instanceof File)) return null;
    return file.size > maxBytes ? { maxFileSize: { max: maxBytes, actual: file.size } } : null;
  };
}
