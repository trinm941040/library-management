# JWT RSA PEM key pair

## Goal

Replace the symmetric JWT secret with RS256 signing using a private/public RSA PEM key pair.

## Local paths

- Private signing key: `src/pem/jwt-private.pem`
- Public validation key: `src/pem/jwt-public.pem`

Configuration paths are relative to the API content root:

- `Jwt:PrivateKeyPemPath=../pem/jwt-private.pem`
- `Jwt:PublicKeyPemPath=../pem/jwt-public.pem`

Production should override both paths with environment variables pointing to read-only mounted secrets.

## Security behavior

- The private key is used only by the signing service.
- JWT Bearer validation uses only the public key.
- Access tokens are restricted to RS256.
- Issuer, audience, lifetime, signature, expiration, and algorithm are validated.
- Both keys receive the same configured `kid`.
- Startup fails when a file is missing, malformed, weak, or not a matching pair.
- RSA keys must be at least 2048 bits.

## Repository warning

The current private key was already committed in the repository's `Initialization` commit. Adding it to `.gitignore` does not remove it from Git history. Rotate the pair before using this application outside local development, remove the private key from tracking, and provide production keys through a secret manager or mounted secret.

## Acceptance criteria

- [x] HMAC signing is removed.
- [x] Tokens are signed with the private PEM using RS256.
- [x] Tokens are validated with the public PEM only.
- [x] Key strength and pair matching are validated.
- [x] Private-key patterns are added to `.gitignore`.
- [x] Automated JWT signing and validation tests pass.
- [x] Full solution build and tests pass (21 tests passed).
