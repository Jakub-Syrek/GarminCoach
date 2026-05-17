# Security Policy

## Supported versions

The latest released version of GarminCoach receives security fixes. Older
releases are not patched separately; please upgrade to the latest version
before reporting a vulnerability.

## Reporting a vulnerability

Please report security issues privately by email to:

**jakubvonsyrek@gmail.com**

Include:

- A clear description of the issue and its impact.
- Steps to reproduce (configuration, payload, environment).
- Affected version (`GarminCoach.exe` displays the version in the title bar
  and in `About`).
- Any suggested mitigation if you have one.

Do **not** open a public GitHub issue for security-sensitive reports.

## Response expectations

- Acknowledgement within 72 hours.
- Initial assessment and severity rating within 7 days.
- Fix or mitigation plan communicated before public disclosure.

## Scope and threat model

GarminCoach is a single-user Windows desktop app. Secrets (Garmin
credentials and the Anthropic API key) are stored locally in
`%AppData%\GarminCoach\secrets.bin`, encrypted with Windows DPAPI under the
current user account. Issues considered in scope:

- Plaintext credential leakage on disk, in logs, or in process memory dumps.
- Disclosure of the Anthropic API key to third parties.
- Improper validation of data returned from Garmin Connect or Anthropic that
  enables code execution or path traversal.
- Insecure update or release artifact distribution.

Out of scope:

- Social engineering against the user's Windows account.
- Attacks that require local administrator privileges already in possession
  of the attacker.
- Vulnerabilities in third-party services (Garmin Connect, Anthropic API).
