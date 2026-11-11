# Authentication

Status: **implemented** in `AuthController`, `LoginService`, `TokenSessionValidator` and `Program.cs`.

## Login

`POST /api/v1/auth/login` checks the password (lockout after 5 failures) and issues an 8-hour JWT in
the `token` cookie (`HttpOnly`, `Secure`, `SameSite=Lax`). The response body never contains the token.
If the admin set the password, login returns 403 and the SPA calls `POST /api/v1/auth/password`.

The token carries the user id, email, `tenant_id`, membership role (plus `SuperAdmin`) and the user's
`security_stamp`.

## Revocation

On every request `TokenSessionValidator` rejects the token (401) unless the user exists, the security
stamp matches, and a membership with the token's tenant and role exists. So a token stops working when:

- the password is changed or reset by an admin (stamp rotates),
- the membership is removed or its role changes,
- the user is deleted.

Code that changes what a token grants must rotate the stamp (`UpdateSecurityStampAsync`) or be covered
by the membership check. Removing `SuperAdmin` is not covered and must rotate the stamp.

## Logout

Deletes the cookie only; the token stays valid until it expires. Rotating the stamp would log the user
out on every device, so it's not done.
