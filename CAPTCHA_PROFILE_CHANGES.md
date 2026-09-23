# Fake CAPTCHA + Profile redesign patch

## Apply
Copy the `pharmacare/` folder over your project (overwrite when asked), then rebuild.
No new migration needed (no model/schema change) - just `dotnet build` and run.

## What changed

### 1. Simple, real CAPTCHA (Services/SimpleCaptchaService.cs)
Replaces the removed real Google reCAPTCHA with a self-hosted math question
("What is 7 + 4?"). No API key, no external network call, works fully offline -
but unlike the ORIGINAL fake CAPTCHA (client-only JS check, anyone could call
/Account/ConfirmCaptcha directly for a free pass), this one is real:
- The question's answer is signed into a token (HMAC-SHA256) that round-trips
  through a hidden form field.
- Verification happens server-side on POST, in AccountController - the browser
  is never trusted for the answer.
- Tested in isolation: correct answer accepted, wrong answer rejected, tampered
  token rejected, missing/garbage token rejected, non-numeric input rejected.
- Wired into both Login and Register (every early-return path in both actions
  now regenerates a fresh question, so a validation error doesn't leave the
  form broken).

### 2. My Profile redesign (Views/Portal/Profile.cshtml)
Tabbed layout closer to the reference design:
- **Contact** - the same fields as before (name, phone, address, city, DOB,
  gender), phone shown with an "Unverified" badge (real phone verification
  is a bigger feature - not implemented, marked honestly rather than faked).
- **Family Members** - placeholder explaining this isn't built yet (linking
  dependent patient records is a real feature needing its own data model -
  didn't fake it with static content).
- **Preferences** - same, placeholder (notification settings need their own
  schema).
- **Security** - REAL change-password form. Added `PortalController.ChangePassword`
  using ASP.NET Identity's `UserManager.ChangePasswordAsync` + `SignInManager`
  refresh, so this one actually works end-to-end, not just UI.
- Left card gets a green "verified" checkmark badge on the avatar and a
  "View Points & Rewards" button linking to the existing Rewards page.

## What I deliberately did NOT fake
Family Members and Preferences tabs are honest placeholders, not working
buttons that do nothing. Phone "Verify" isn't a working button either - it's
labeled Unverified with no action, since a real SMS-verification flow needs
an SMS provider integration that's out of scope right now. If you want any
of these built for real, say so and I'll scope it properly (they'd need new
Customer/Family models, not just UI).

## Verify yourself
1. Login/Register pages: a "What is X + Y?" box now appears above the submit
   button. Wrong/blank answer blocks the submit with an error. Correct answer
   + correct credentials logs in normally.
2. Portal > My Profile: four tabs render, Contact tab still saves as before,
   Security tab's Change Password actually changes your password (test with
   the Customer demo account, log out, log back in with the new password).
