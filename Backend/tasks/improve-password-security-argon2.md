# Improve password security with Argon2id

## Goal

Replace the default ASP.NET Core Identity password hasher with Argon2id while preserving transparent migration for existing Identity password hashes.

## Acceptance criteria

- [x] Use the maintained `Konscious.Security.Cryptography.Argon2` package.
- [x] Generate a cryptographically random, per-password salt.
- [x] Store the algorithm version and work factors with the encoded hash.
- [x] Compare derived hashes in constant time.
- [x] Validate encoded parameters before allocating Argon2 memory.
- [x] Mark legacy Identity hashes for automatic rehash after a successful login.
- [x] Mark Argon2 hashes for rehash when configured work factors change.
- [x] Add automated tests for salting, verification, migration, and malformed input.
- [x] Restore, build, and run the full test suite (15 tests passed).

## Default parameters

- Variant: Argon2id
- Memory: 64 MiB
- Iterations: 3
- Parallelism: 2
- Salt: 16 bytes
- Derived hash: 32 bytes

The parameters are configurable under `PasswordHashing:Argon2`. Tune them against the production hardware and expected authentication concurrency before deployment.
